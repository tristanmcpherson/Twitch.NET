using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TwitchBot
{
    public class GambleInfo
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Channel { get; set; }
        public string Username { get; set; }
        public long Points { get; set; }
    }
}
