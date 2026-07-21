using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible
{
    public class SqliteChapterBuilder : IChapterBuilder
    {
        private readonly IXmlPathResolver _pathResolver;
        private BuilderState _selectedNode;

        public NodeBase CurrentNode => _selectedNode?.CurrentNode;
        public bool HasNextChapter => _selectedNode?.HasNextChapter ?? false;
        public bool HasPrevChapter => _selectedNode?.HasPrevChapter ?? false;
        public string NextBookAbbr => _selectedNode?.NextBookAbbr;
        public int NextChapterNumber => _selectedNode?.NextChapterNumber ?? 0;
        public string PrevBookAbbr => _selectedNode?.PrevBookAbbr;
        public int PrevChapterNumber => _selectedNode?.PrevChapterNumber ?? 0;

        public SqliteChapterBuilder(IXmlPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public BuilderState GetChapter(string bookAbbreviation, int chapter)
        {
            var dbPath = Path.Combine(_pathResolver.GetPath(), "bible.db");
            var connString = $"Data Source={dbPath}";

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT json_data, has_prev_chapter, prev_book_abbr, prev_chapter_number, 
                               has_next_chapter, next_book_abbr, next_chapter_number
                        FROM chapters
                        WHERE book_abbr = @book AND chapter = @chapter";
                    cmd.Parameters.AddWithValue("@book", bookAbbreviation);
                    cmd.Parameters.AddWithValue("@chapter", chapter);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var json = reader.GetString(0);
                            var hasPrev = reader.GetInt32(1) == 1;
                            var prevBook = reader.IsDBNull(2) ? null : reader.GetString(2);
                            var prevChapter = reader.GetInt32(3);
                            var hasNext = reader.GetInt32(4) == 1;
                            var nextBook = reader.IsDBNull(5) ? null : reader.GetString(5);
                            var nextChapter = reader.GetInt32(6);

                            var serializerSettings = new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto
                            };

                            var currentNode = JsonConvert.DeserializeObject<ChapterNode>(json, serializerSettings);

                            _selectedNode = new BuilderState(
                                currentNode,
                                hasPrev,
                                prevBook,
                                prevChapter,
                                hasNext,
                                nextBook,
                                nextChapter
                            );

                            return _selectedNode;
                        }
                    }
                }
            }

            throw new ApplicationException($"Chapter not found: {bookAbbreviation} {chapter}");
        }
    }
}
