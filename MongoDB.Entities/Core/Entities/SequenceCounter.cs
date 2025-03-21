﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[Collection("[SEQUENCE_COUNTERS]")]
class SequenceCounter : IEntity
{
    [BsonId]
    public string ID { get; set; } = null!;

    [BsonRepresentation(BsonType.Int64)]
    public ulong Count { get; set; }

    public object GenerateNewID()
        => throw new NotImplementedException();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}