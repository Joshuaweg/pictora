using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Collections.Generic;
using DotNetEnv;
using System.Diagnostics;
namespace Pictora.Services
{
    public interface IMongoDBService
    {
        Task<T> GetByIdAsync<T>(string collectionName, string id);
        Task<List<T>> GetAllAsync<T>(string collectionName);
        Task<T> CreateAsync<T>(string collectionName, T document);
        Task UpdateAsync<T>(string collectionName, string id, T document);
        Task DeleteAsync<T>(string collectionName, string id);
    }

    public class MongoDBService : IMongoDBService
    {
        private readonly IMongoDatabase _database;
        
        public MongoDBService()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            string connect = DotNetEnv.Env.GetString("MONGO_URI", "");
            Debug.WriteLine(connect);
            var client = new MongoClient(connect);
            _database = client.GetDatabase("main");
        }

        public async Task<T> GetByIdAsync<T>(string collectionName, string id)
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("id", Int32.Parse(id));
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetAllAsync<T>(string collectionName)
        {
            var collection = _database.GetCollection<T>(collectionName);
            foreach (var item in collection.Find(_ => true).ToList())
            {
                System.Console.WriteLine(item);
            }
            return await collection.Find(_ => true).ToListAsync();
        }
        // get max id from any collection and return next id
        public async Task<int> GetNextIdAsync<T>(string collectionName)
        {
            var collection = _database.GetCollection<T>(collectionName);
            var sort = Builders<T>.Sort.Descending("id");
            var max = await collection.Find(_ => true).Sort(sort).Limit(1).FirstOrDefaultAsync();
            if (max == null)
            {
                return 1;
            }
            return (int)max.GetType().GetProperty("id").GetValue(max) + 1;
        }

        public async Task<T> CreateAsync<T>(string collectionName, T document)
        {
            var collection = _database.GetCollection<T>(collectionName);
            await collection.InsertOneAsync(document);
            return document;
        }

        public async Task UpdateAsync<T>(string collectionName, string id, T document)
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            await collection.ReplaceOneAsync(filter, document);
        }

        public async Task DeleteAsync<T>(string collectionName, string id)
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            await collection.DeleteOneAsync(filter);
        }
    }
}