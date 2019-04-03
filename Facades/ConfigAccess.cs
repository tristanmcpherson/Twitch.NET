using System.Threading.Tasks;
using MongoDB.Driver;

namespace DitsyTwitch.Facades
{
    public class ConfigFacade : MongoFacade
    {
        private static IMongoDatabase Db = MongoClient.GetDatabase("config");
        private static IMongoCollection<Config> ConfigCollection = GetCollection<Config>(Db);
        
        public async Task<Config> GetConfig() {
            return await ConfigCollection.Find(Builders<Config>.Filter.Empty).FirstOrDefaultAsync();
        }
    }
}
