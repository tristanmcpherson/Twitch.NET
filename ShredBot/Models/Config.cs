using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchNET
{
    public class Config
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string UserName { get; set; }

        public string ClientId { get; set; }
        public string OAuth { get; set; }
    }
}
