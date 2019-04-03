using MongoDB.Driver;

namespace DitsyTwitch.Facades
{
    public abstract class MongoFacade
    {
        public static MongoClient MongoClient = new MongoClient();

        public static IMongoCollection<T> GetCollection<T>(IMongoDatabase db) {
            return db.GetCollection<T>(typeof(T).Name);
        }
    }
}
