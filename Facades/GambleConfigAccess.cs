
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DitsyTwitch;
using MongoDB.Driver;

namespace DitsyTwitch.Facades
{
    public class GambleConfigFacade : MongoFacade
    {
        private static IMongoDatabase GambleConfigDatabase = MongoClient.GetDatabase("gamble-config");
        private IMongoCollection<GambleConfiguration> GambleConfigurationCollection = GetCollection<GambleConfiguration>(GambleConfigDatabase);
        private FilterDefinitionBuilder<GambleConfiguration> FilterBuilder = Builders<GambleConfiguration>.Filter;
        private UpdateDefinitionBuilder<GambleConfiguration> UpdateBuilder = Builders<GambleConfiguration>.Update;
        private ProjectionDefinitionBuilder<GambleConfiguration> ProjectBuilder = Builders<GambleConfiguration>.Projection;

        public async Task<List<GambleConfiguration>> GetConfigurations()
        {
            return await GambleConfigurationCollection.Find(FilterBuilder.Empty).ToListAsync();
        }

        public async Task<GambleConfiguration> GetConfiguration(string channel)
        {
            return await GambleConfigurationCollection.Find(FilterBuilder.Eq(config => config.Channel, channel)).SingleOrDefaultAsync();
        }
    }
}