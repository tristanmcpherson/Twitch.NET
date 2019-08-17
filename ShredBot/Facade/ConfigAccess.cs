using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using TwitchNET;

namespace ShredBot.Facades
{
    public class ConfigFacade : MongoFacade<Config>
    {
        public override string DbName => "config";
        public async Task<Config> GetConfig() {
            return await Collection.Find(Filter.Empty).FirstOrDefaultAsync();
        }
    }
}
