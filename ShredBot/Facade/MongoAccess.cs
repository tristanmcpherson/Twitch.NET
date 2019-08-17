using MongoDB.Driver;
using System;

namespace ShredBot.Facades
{
    public abstract class MongoFacade<T>
    {
        public static MongoClient MongoClient = new MongoClient();
        public FilterDefinitionBuilder<T> Filter = Builders<T>.Filter;
        public UpdateDefinitionBuilder<T> Update = Builders<T>.Update;
        public ProjectionDefinitionBuilder<T> Projection = Builders<T>.Projection;
        public IMongoCollection<T> Collection => MongoDb.GetCollection<T>(typeof(T).Name);

        private IMongoDatabase _mongoDb = null;
        public IMongoDatabase MongoDb { get => _mongoDb ?? (_mongoDb = MongoClient.GetDatabase(DbName)); }

        public abstract string DbName { get; }
    }
}
