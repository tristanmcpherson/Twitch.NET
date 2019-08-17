
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ShredBot.Facades
{
    public class GambleConfigFacade : MongoFacade<GambleConfig>
    {
        public override string DbName => "gamble";

        public async Task<List<GambleConfig>> GetConfigurations()
        {
            return await Collection.Find(Filter.Empty).ToListAsync();
        }

        public async Task<GambleConfig> GetConfiguration(string channel)
        {
            return await Collection.Find(Filter.Eq(config => config.Channel, channel)).SingleOrDefaultAsync();
        }
    }
}