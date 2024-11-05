using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        public MongoDBService(string connectionString ="mongodb+srv://root:root@pictora.mgro1.mongodb.net/?retryWrites=true&w=majority&appName=pictora")
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("main");
        }

        public async Task<T> GetByIdAsync<T>(string collectionName, string id)
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetAllAsync<T>(string collectionName)
        {
            var collection = _database.GetCollection<T>(collectionName);
            return await collection.Find(_ => true).ToListAsync();
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