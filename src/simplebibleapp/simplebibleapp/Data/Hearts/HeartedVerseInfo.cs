using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace simplebibleapp.Data.Hearts
{
    public class HeartedVerseInfo
    {
        [BsonId]
        public string Id { get; set; }
        public string BookAbbr { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string IP { get; set; }
        public string Agent { get; set; }
        public DateTime AddedOn { get; set; }
        public bool Selected { get; set; }
    }
}
