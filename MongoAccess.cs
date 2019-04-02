using MongoDB.Driver;

namespace DitsyTwitch
{
    public abstract class MongoAccess
    {
        public static MongoClient MongoClient = new MongoClient();

        public static IMongoCollection<T> GetCollection<T>(IMongoDatabase db) {
            return db.GetCollection<T>(nameof(T));
        }
    }
}
