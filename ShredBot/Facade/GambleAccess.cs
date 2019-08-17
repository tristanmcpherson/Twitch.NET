
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchNET;
using MongoDB.Driver;
using System;

namespace ShredBot.Facades
{
    public class GambleFacade : MongoFacade<GambleInfo>
    {
        public override string DbName => "gamble";

        private FilterDefinition<GambleInfo> GetUserFilter(string channel, string username)
        {
            return Filter.And(
                Filter.Eq(x => x.Channel, channel),
                Filter.Eq(x => x.Username, username));
        }

        public async Task<List<GambleInfo>> GetLeaderboard(string channel)
        {
            return await Collection.Find(Filter.Eq(x => x.Channel, channel)).SortByDescending(gi => gi.Points).Limit(5).ToListAsync();
        }

        public async Task<long> GetLeaderboardPosition(string channel, string username)
        {
            var info = await GetInfo(channel, username);
            return await Collection.CountDocumentsAsync(
                Filter.And(
                    Filter.Eq(x => x.Channel, channel),
                    Filter.Gt(x => x.Points, info.Points))) + 1;
        }

        public async Task<GambleInfo> GetInfo(string channel, string username)
        {
            return await Collection.Find(GetUserFilter(channel, username)).SingleOrDefaultAsync()
                ?? new GambleInfo { Channel = channel, Username = username };
        }

        public Task Update(GambleInfo info)
        {
            return UpdatePoints(info.Channel, info.Username, info.Points);
        }

        public async Task UpdatePoints(string channel, string username, long points)
        {
            await Collection.FindOneAndUpdateAsync(
                GetUserFilter(channel, username),
                base.Update.Set(x => x.Points, points));
        }

        public async Task AddPoints(string channel, long points)
        {
            await Collection.UpdateManyAsync(
                Filter.Eq(x => x.Channel, channel),
                base.Update.Inc(x => x.Points, points));
        }

        public async Task AddPoints(string channel, string username, long points)
        {
            await Collection.UpdateOneAsync(
                GetUserFilter(channel, username),
                base.Update.Inc(x => x.Points, points)
            );
        }

        public async Task RemovePoints(string channel, string username, long points)
        {
            await Collection.UpdateOneAsync(
                GetUserFilter(channel, username),
                base.Update.Inc(x => x.Points, -points)
            );
        }

        public async Task SetDefaultPoints(string channel, IEnumerable<string> usernames, long points = 0)
        {
            var usernameProject = Projection.Include(x => x.Username);
            var usersInChat = await Collection.Find(
                Filter.And(
                    Filter.Eq(x => x.Channel, channel),
                    Filter.In(x => x.Username, usernames))).Project<GambleInfo>(usernameProject).ToListAsync();
            var newUsers = usernames.Except(usersInChat.Select(u => u.Username));
            var newUserInfos = newUsers.Select(u => new GambleInfo { Channel = channel, Username = u, Points = points });
            if (newUserInfos.Count() < 1)
            {
                return;
            }
            await Collection.InsertManyAsync(newUserInfos);
        }
    }
}