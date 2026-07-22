using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible.search
{
    public class SqliteVerseSearch : IVerseSearch
    {
        private readonly IXmlPathResolver _pathResolver;

        private static readonly HashSet<string> ExcludedStrongs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Greek Articles, Conjunctions, Prepositions
            "G3588", "G2532", "G1161", "G1722", "G1519", "G1537", "G4314", "G3756", "G3754", "G3767", "G1223", "G2596", "G3326", "G1909", "G575", "G5259", "G5228", "G4862", "G1437", "G2443", "G3739", "G5613", "G5620",
            // Hebrew Particles, Conjunctions, Prepositions
            "H0853", "H0413", "H05921", "H03588", "H05704", "H08033", "H05973", "H0310", "H08478", "H0854", "H01157", "H01107"
        };

        public SqliteVerseSearch(IXmlPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public IEnumerable<VerseInfo> GetGreekVersesByWordRef(int highlitedWordId, string bookAbbr = null)
        {
            return GetVersesByWordRef($"G{highlitedWordId}", bookAbbr);
        }

        public IEnumerable<VerseInfo> GetHebrewVersesByWordRef(int highlitedWordId, string bookAbbr = null)
        {
            return GetVersesByWordRef($"H0{highlitedWordId}", bookAbbr);
        }

        private IEnumerable<VerseInfo> GetVersesByWordRef(string strongsNumber, string bookAbbr = null)
        {
            if (ExcludedStrongs.Contains(strongsNumber))
            {
                return Enumerable.Empty<VerseInfo>();
            }

            var dbPath = Path.Combine(_pathResolver.GetPath(), "bible.db");
            var connString = $"Data Source={dbPath}";

            var occurrences = new List<(string BookAbbr, int Chapter, int VerseNumber)>();

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    var sql = @"
                        SELECT book_abbr, chapter, verse_number 
                        FROM word_occurrences 
                        WHERE strong_number = @strong_number";
                    if (!string.IsNullOrEmpty(bookAbbr))
                    {
                        sql += " AND book_abbr = @book_abbr";
                    }
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@strong_number", strongsNumber);
                    if (!string.IsNullOrEmpty(bookAbbr))
                    {
                        cmd.Parameters.AddWithValue("@book_abbr", bookAbbr);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            occurrences.Add((
                                reader.GetString(0),
                                reader.GetInt32(1),
                                reader.GetInt32(2)
                            ));
                        }
                    }
                }
            }

            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            // Cache loaded chapters in memory to avoid redundant file loads for multiple hits in same chapter
            var chapterCache = new Dictionary<string, ChapterNode>();

            var results = new List<VerseInfo>();

            foreach (var occ in occurrences)
            {
                var cacheKey = $"{occ.BookAbbr}:{occ.Chapter}";
                if (!chapterCache.TryGetValue(cacheKey, out var chapterNode))
                {
                    chapterNode = LoadChapterNode(dbPath, occ.BookAbbr, occ.Chapter, serializerSettings);
                    if (chapterNode != null)
                    {
                        chapterCache[cacheKey] = chapterNode;
                    }
                }

                if (chapterNode != null)
                {
                    var verseNodes = GetVerseNodes(chapterNode, occ.VerseNumber);
                    var highlightedText = GetHighlightedWordText(verseNodes, strongsNumber);
                    results.Add(new VerseInfo(occ.BookAbbr, occ.Chapter, occ.VerseNumber, highlightedText));
                }
            }

            return results;
        }

        private ChapterNode LoadChapterNode(string dbPath, string bookAbbr, int chapter, JsonSerializerSettings settings)
        {
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT json_data FROM chapters WHERE book_abbr = @book AND chapter = @chapter";
                    cmd.Parameters.AddWithValue("@book", bookAbbr);
                    cmd.Parameters.AddWithValue("@chapter", chapter);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return JsonConvert.DeserializeObject<ChapterNode>((string)result, settings);
                    }
                }
            }
            return null;
        }

        private List<NodeBase> GetVerseNodes(ChapterNode chapterNode, int verseNumber)
        {
            var nodes = new List<NodeBase>();
            var currentVerse = 0;
            var collect = false;

            foreach (var node in chapterNode.SubNodes)
            {
                if (node is BeginVerseNode beginNode)
                {
                    currentVerse = beginNode.Verse;
                    collect = (currentVerse == verseNumber);
                }
                else if (node is EndVerseNode)
                {
                    if (collect)
                    {
                        collect = false;
                    }
                }

                if (collect)
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        private string GetHighlightedWordText(IEnumerable<NodeBase> nodes, string strongsNumber)
        {
            var sb = new StringBuilder();
            var prevWasWord = false;
            foreach (var node in nodes)
            {
                var space = (prevWasWord && node.XmlNodeType != NodeType.Text && node.XmlNodeType != NodeType.Null) ? " " : "";
                
                if (node is WordNode wordNode)
                {
                    var lemmas = (wordNode.Lemma ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var isMatch = lemmas.Contains("strong:" + strongsNumber);

                    var text = wordNode.Text;
                    if (isMatch)
                    {
                        sb.Append($"{space}<em>{text}</em>");
                    }
                    else
                    {
                        sb.Append($"{space}{text}");
                    }
                    prevWasWord = true;
                }
                else
                {
                    if (node.XmlNodeType != NodeType.Null && node.XmlNodeType != NodeType.BeginVerse && node.XmlNodeType != NodeType.EndVerse)
                    {
                        sb.Append($"{space}{node.Text}");
                    }
                    prevWasWord = (node.XmlNodeType == NodeType.Word);
                }
            }
            return $"<verse>{sb.ToString()}</verse>";
        }

        public IEnumerable<BookOccurrence> GetWordBookAggregates(string id)
        {
            var results = new List<BookOccurrence>();
            
            string strongsNumber = id;
            if (id.StartsWith("H", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(id.Substring(1), out var num))
                {
                    strongsNumber = $"H0{num}";
                }
            }

            if (ExcludedStrongs.Contains(strongsNumber))
            {
                return results;
            }

            var dbPath = Path.Combine(_pathResolver.GetPath(), "bible.db");
            var connString = $"Data Source={dbPath}";

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT a.book_abbr, a.occurrence_count 
                        FROM word_book_aggregates a
                        JOIN books b ON a.book_abbr = b.abbr
                        WHERE a.strong_number = @strong_number
                        ORDER BY b.id ASC";
                    cmd.Parameters.AddWithValue("@strong_number", strongsNumber);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new BookOccurrence(
                                reader.GetString(0),
                                reader.GetInt32(1)
                            ));
                        }
                    }
                }
            }

            return results;
        }
    }
}
