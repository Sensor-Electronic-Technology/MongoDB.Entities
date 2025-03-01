using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Entities;

public class SelectionFieldBuilder : ISelectionFieldBuilder {
    public SelectionField Field { get; } = new SelectionField();
    //public ISelectionFieldBuilder Builder { get; set; } = new SelectionFieldBuilder();

    public ISelectionFieldBuilder WithName(string name) {
        this.Field.FieldName = name;
        return this;
    }

    public ISelectionFieldBuilder WithSelectionItems<TValue>(Dictionary<string, TValue> selectionItems) {
        this.Field.TypeCode = Type.GetTypeCode(typeof(TValue));
        this.Field.BsonType=BsonValue.Create(selectionItems.First().Value).BsonType;
        this.Field.SelectionDictionary = selectionItems.Select(
            e => new KeyValuePair<string,object>(e.Key, e.Value)).ToDictionary();
        return this;
    }

    public ISelectionFieldBuilder HasSelectionItem<TValue>(string key, TValue value) {
        if (this.Field.SelectionDictionary.Any()) {
            if (this.Field.TypeCode == Type.GetTypeCode(typeof(TValue))) {
                this.Field.SelectionDictionary[key] = value;
            } else {
                throw new ArgumentException("Invalid selection type");
            }
        } else {
            this.Field.TypeCode = Type.GetTypeCode(typeof(TValue));
            this.Field.BsonType=BsonValue.Create(value).BsonType;
            //TODO: Create some way to notify add failed.  Don't want to throw an exception
            this.Field.SelectionDictionary.TryAdd(key, value);
        }
        return this;
    }

    public Field Build()
        => this.Field;
}