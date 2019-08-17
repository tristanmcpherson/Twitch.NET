using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class GambleConfig
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public string Channel { get; set; }

    public int PointAwardInterval { get; set; }

    public long PointAwardAmount { get; set; }

    public string Currency { get; set; }
}
