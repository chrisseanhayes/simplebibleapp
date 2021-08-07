using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace simplebibleapp.Data.Hearts
{
    public interface IHeartRepository
    {
        void Add(HeartedVerseInfo info);
    }

    internal class MongoHeartRepository : IHeartRepository
    {
        private string _simpleBibleAppDbName = "simplebibleapp";
        private const string heartCollectionName = "Hearts";
        private string _username;
        private string _password;
        private string _servername;

        public MongoHeartRepository(IMongoDbConfigSource config)
        {
            _username = config.UserName;
            _password = config.Password;
            _servername = config.ServerName;
            _simpleBibleAppDbName = config.DatabaseName;
        }
        public void Add(HeartedVerseInfo info)
        {
            var creds = MongoCredential.CreateCredential("admin", _username, _password);
            var settings = new MongoClientSettings
            {
                Credential = creds,
                Server = new MongoServerAddress(_servername, 27017)
            };
            var client = new MongoClient(settings);
            var db = client?.GetDatabase(_simpleBibleAppDbName);
            var collection = db.GetCollection<HeartedVerseInfo>(heartCollectionName);
            collection.InsertOne(info);
        }

        private string GetConnectionString()
        {
            return $"mongodb://{_username}:{_password}@{_servername}";
        }
    }

    public interface IMongoDbConfigSource
    {
        string UserName { get; }
        string Password { get; }
        string ServerName { get; }
        string DatabaseName { get; }
    }

    internal class EnvironmentMongoDbConfigSource : IMongoDbConfigSource
    {
        public string UserName => Environment.GetEnvironmentVariable("MONGODB_USER");
        public string Password => Environment.GetEnvironmentVariable("MONGODB_PASS");
        public string ServerName => Environment.GetEnvironmentVariable("MONGODB_SERVERNAME");
        public string DatabaseName => Environment.GetEnvironmentVariable("MONGODB_DATABASE");
    }
}
