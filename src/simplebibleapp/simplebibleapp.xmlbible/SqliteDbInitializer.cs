using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible
{
    public static class SqliteDbInitializer
    {
        private class WordTracker
        {
            public int CurrentVerse { get; set; }
            public List<(string strongNumber, int verseNum)> Occurrences { get; } = new List<(string, int)>();
        }

        private static void ExtractWordOccurrences(NodeBase node, WordTracker tracker)
        {
            if (node is BeginVerseNode beginNode)
            {
                tracker.CurrentVerse = beginNode.Verse;
            }
            else if (node is WordNode wordNode)
            {
                if (!string.IsNullOrEmpty(wordNode.Lemma))
                {
                    var lemmas = wordNode.Lemma.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var lemma in lemmas)
                    {
                        var parts = lemma.Split(':');
                        if (parts.Length == 2 && parts[0] == "strong")
                        {
                            tracker.Occurrences.Add((parts[1], tracker.CurrentVerse));
                        }
                    }
                }
            }

            foreach (var subNode in node.SubNodes)
            {
                ExtractWordOccurrences(subNode, tracker);
            }
        }

        public static void EnsureDbCreated(string dataPath, IEnumerable<IState> states)
        {
            var dbPath = Path.Combine(dataPath, "bible.db");
            if (File.Exists(dbPath))
            {
                return; // Already exists
            }

            Console.WriteLine($"SQLite database not found. Generating it at: {dbPath}");

            var connString = $"Data Source={dbPath}";
            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();

                // Create Tables
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE books (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            abbr TEXT NOT NULL UNIQUE,
                            name TEXT NOT NULL,
                            testament TEXT NOT NULL
                        );

                        CREATE TABLE chapters (
                            book_abbr TEXT NOT NULL,
                            chapter INTEGER NOT NULL,
                            json_data TEXT NOT NULL,
                            has_prev_chapter INTEGER NOT NULL,
                            prev_book_abbr TEXT,
                            prev_chapter_number INTEGER NOT NULL,
                            has_next_chapter INTEGER NOT NULL,
                            next_book_abbr TEXT,
                            next_chapter_number INTEGER NOT NULL,
                            PRIMARY KEY (book_abbr, chapter)
                        );

                        CREATE TABLE word_occurrences (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            strong_number TEXT NOT NULL,
                            book_abbr TEXT NOT NULL,
                            chapter INTEGER NOT NULL,
                            verse_number INTEGER NOT NULL
                        );

                        CREATE TABLE word_book_aggregates (
                            strong_number TEXT NOT NULL,
                            book_abbr TEXT NOT NULL,
                            occurrence_count INTEGER NOT NULL,
                            PRIMARY KEY (strong_number, book_abbr)
                        );

                        CREATE INDEX idx_chapters_book_chapter ON chapters(book_abbr, chapter);
                        CREATE INDEX idx_word_occurrences_strong_book ON word_occurrences(strong_number, book_abbr);
                        CREATE INDEX idx_word_book_aggregates_strong ON word_book_aggregates(strong_number);
                    ";
                    cmd.ExecuteNonQuery();
                }

                // Insert Books
                var otPath = Path.Combine(dataPath, "OTBookNames.txt");
                var ntPath = Path.Combine(dataPath, "NTBookNames.txt");

                if (!File.Exists(otPath) || !File.Exists(ntPath))
                {
                    throw new FileNotFoundException("Required book name txt files are missing from data path.");
                }

                var otBooks = File.ReadAllLines(otPath)
                    .Select(line => line.Split('-'))
                    .Select(parts => new { Name = parts[0].Trim(), Abbr = parts[1].Trim(), Testament = "Old" });

                var ntBooks = File.ReadAllLines(ntPath)
                    .Select(line => line.Split('-'))
                    .Select(parts => new { Name = parts[0].Trim(), Abbr = parts[1].Trim(), Testament = "New" });

                var allBooks = otBooks.Concat(ntBooks).ToList();

                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var book in allBooks)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "INSERT INTO books (abbr, name, testament) VALUES (@abbr, @name, @testament)";
                            cmd.Parameters.AddWithValue("@abbr", book.Abbr);
                            cmd.Parameters.AddWithValue("@name", book.Name);
                            cmd.Parameters.AddWithValue("@testament", book.Testament);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }

                // Parse and Insert Chapters & Words
                var pathResolver = new DirectResolver(dataPath);
                var builder = new ChapterBuilder(states, pathResolver);
                var currentBook = "Gen";
                var currentChapter = 1;

                var serializerSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                using (var transaction = conn.BeginTransaction())
                {
                    var details = builder.GetChapter(currentBook, currentChapter);
                    while (details != null)
                    {
                        var json = JsonConvert.SerializeObject(details.CurrentNode, serializerSettings);

                        // Save Chapter
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                INSERT INTO chapters (
                                    book_abbr, chapter, json_data, 
                                    has_prev_chapter, prev_book_abbr, prev_chapter_number, 
                                    has_next_chapter, next_book_abbr, next_chapter_number
                                ) VALUES (
                                    @book_abbr, @chapter, @json_data, 
                                    @has_prev_chapter, @prev_book_abbr, @prev_chapter_number, 
                                    @has_next_chapter, @next_book_abbr, @next_chapter_number
                                )";
                            cmd.Parameters.AddWithValue("@book_abbr", currentBook);
                            cmd.Parameters.AddWithValue("@chapter", currentChapter);
                            cmd.Parameters.AddWithValue("@json_data", json);
                            cmd.Parameters.AddWithValue("@has_prev_chapter", details.HasPrevChapter ? 1 : 0);
                            cmd.Parameters.AddWithValue("@prev_book_abbr", (object)details.PrevBookAbbr ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@prev_chapter_number", details.PrevChapterNumber);
                            cmd.Parameters.AddWithValue("@has_next_chapter", details.HasNextChapter ? 1 : 0);
                            cmd.Parameters.AddWithValue("@next_book_abbr", (object)details.NextBookAbbr ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@next_chapter_number", details.NextChapterNumber);
                            cmd.ExecuteNonQuery();
                        }

                        // Extract and save words
                        var tracker = new WordTracker();
                        ExtractWordOccurrences(details.CurrentNode, tracker);

                        foreach (var occurrence in tracker.Occurrences)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO word_occurrences (
                                        strong_number, book_abbr, chapter, verse_number
                                    ) VALUES (
                                        @strong_number, @book_abbr, @chapter, @verse_number
                                    )";
                                cmd.Parameters.AddWithValue("@strong_number", occurrence.strongNumber);
                                cmd.Parameters.AddWithValue("@book_abbr", currentBook);
                                cmd.Parameters.AddWithValue("@chapter", currentChapter);
                                cmd.Parameters.AddWithValue("@verse_number", occurrence.verseNum);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (!details.HasNextChapter)
                        {
                            break;
                        }

                        currentBook = details.NextBookAbbr;
                        currentChapter = details.NextChapterNumber;
                        details = builder.GetChapter(currentBook, currentChapter);
                    }
                    transaction.Commit();
                }

                // Populate word book aggregates
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO word_book_aggregates (strong_number, book_abbr, occurrence_count)
                        SELECT strong_number, book_abbr, COUNT(*)
                        FROM word_occurrences
                        GROUP BY strong_number, book_abbr";
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine("SQLite database generation completed successfully!");
            }
        }

        private class DirectResolver : IXmlPathResolver
        {
            private readonly string _path;
            public DirectResolver(string path) { _path = path; }
            public string GetPath() => _path;
        }
    }
}
