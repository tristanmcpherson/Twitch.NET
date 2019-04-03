
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DitsyTwitch;
using MongoDB.Driver;

namespace DitsyTwitch.Facades
{
    public class GambleFacade : MongoFacade
    {
        private static IMongoDatabase GambleDatabase = MongoClient.GetDatabase("gamble");
        private IMongoCollection<GambleInfo> GambleCollection = GambleDatabase.GetCollection<GambleInfo>("points");
        private FilterDefinitionBuilder<GambleInfo> FilterBuilder = Builders<GambleInfo>.Filter;
        private UpdateDefinitionBuilder<GambleInfo> UpdateBuilder = Builders<GambleInfo>.Update;
        private ProjectionDefinitionBuilder<GambleInfo> ProjectBuilder = Builders<GambleInfo>.Projection;

        private FilterDefinition<GambleInfo> GetUserFilter(string channel, string username)
        {
            return FilterBuilder.And(
                            FilterBuilder.Eq(x => x.Channel, channel),
                            FilterBuilder.Eq(x => x.Username, username));
        }

        public async Task<List<GambleInfo>> GetLeaderboard(string channel)
        {
            return await GambleCollection.Find(FilterBuilder.Eq(x => x.Channel, channel)).SortByDescending(gi => gi.Points).Limit(10).ToListAsync();
        }

        public async Task<long> GetLeaderboardPosition(string channel, string username)
        {
            var info = await GetInfo(channel, username);
            return await GambleCollection.CountDocumentsAsync(
                FilterBuilder.And(
                    FilterBuilder.Eq(x => x.Channel, channel),
                    FilterBuilder.Gt(x => x.Points, info.Points))) + 1;
        }

        public async Task<GambleInfo> GetInfo(string channel, string username)
        {
            return await GambleCollection.Find(GetUserFilter(channel, username)).SingleOrDefaultAsync()
                ?? new GambleInfo { Channel = channel, Username = username };
        }

        public Task Update(GambleInfo info)
        {
            return UpdatePoints(info.Channel, info.Username, info.Points);
        }

        public async Task UpdatePoints(string channel, string username, long points)
        {
            await GambleCollection.FindOneAndUpdateAsync(
                GetUserFilter(channel, username),
                UpdateBuilder.Set(x => x.Points, points));
        }

        public async Task AddPoints(string channel, long points)
        {
            await GambleCollection.UpdateManyAsync(
                FilterBuilder.Eq(x => x.Channel, channel),
                UpdateBuilder.Inc(x => x.Points, points));
        }

        public async Task AddPoints(string channel, string username, long points)
        {
            await GambleCollection.UpdateOneAsync(
                GetUserFilter(channel, username),
                UpdateBuilder.Inc(x => x.Points, points)
            );
        }

        public async Task RemovePoints(string channel, string username, long points)
        {
            await GambleCollection.UpdateOneAsync(
                GetUserFilter(channel, username),
                UpdateBuilder.Inc(x => x.Points, -points)
            );
        }

        public async Task SetDefaultPoints(string channel, IEnumerable<string> usernames, long points = 0)
        {
            var usernameProject = ProjectBuilder.Include(x => x.Username);
            var usersInChat = await GambleCollection.Find(
                FilterBuilder.And(
                    FilterBuilder.Eq(x => x.Channel, channel),
                    FilterBuilder.In(x => x.Username, usernames))).Project<GambleInfo>(usernameProject).ToListAsync();
            var newUsers = usernames.Except(usersInChat.Select(u => u.Username));
            var newUserInfos = newUsers.Select(u => new GambleInfo { Channel = channel, Username = u, Points = points });
            if (newUserInfos.Count() < 1)
            {
                return;
            }
            await GambleCollection.InsertManyAsync(newUserInfos);
        }
    }
}