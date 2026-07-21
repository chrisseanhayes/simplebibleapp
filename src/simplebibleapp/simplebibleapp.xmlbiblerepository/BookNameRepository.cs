using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;
using simplebibleapp.xmlbible;

namespace simplebibleapp.xmlbiblerepository
{
    public class BookNameRepository : IBookNameRepository
    {
        private readonly IXmlPathResolver _pathResolver;

        public BookNameRepository(IXmlPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public IEnumerable<BookListItem> GetBooks()
        {
            var dbPath = Path.Combine(_pathResolver.GetPath(), "bible.db");
            var connString = $"Data Source={dbPath}";
            var books = new List<BookListItem>();

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name, abbr, testament FROM books ORDER BY id";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(0);
                            var abbr = reader.GetString(1);
                            var testament = reader.GetString(2);

                            books.Add(new BookListItem(
                                testament: testament,
                                bookName: name,
                                searchAbbr: abbr,
                                isChecked: false
                            ));
                        }
                    }
                }
            }

            return books;
        }

        public IEnumerable<SelectableVerse> GetSelectableVerses(string bookAbbr)
        {
            var dbPath = Path.Combine(_pathResolver.GetPath(), "bible.db");
            var connString = $"Data Source={dbPath}";
            var list = new List<SelectableVerse>();

            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT chapter, json_data FROM chapters WHERE book_abbr = @book ORDER BY chapter";
                    cmd.Parameters.AddWithValue("@book", bookAbbr);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var chapter = reader.GetInt32(0);
                            var json = reader.GetString(1);
                            var chapterNode = JsonConvert.DeserializeObject<ChapterNode>(json, serializerSettings);

                            // Find all unique verse numbers in this chapter
                            var verses = chapterNode.SubNodes
                                .OfType<BeginVerseNode>()
                                .Select(v => v.Verse)
                                .Distinct();

                            foreach (var verse in verses)
                            {
                                list.Add(new SelectableVerse
                                {
                                    BookAbbreviation = bookAbbr,
                                    Chapter = chapter,
                                    Verse = verse
                                });
                            }
                        }
                    }
                }
            }

            return list;
        }
    }
}