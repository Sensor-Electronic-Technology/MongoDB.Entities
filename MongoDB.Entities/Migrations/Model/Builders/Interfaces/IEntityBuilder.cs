using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;

public interface IEntityBuilder {
    public List<Field> Fields { get; set; }
    public IEntityBuilder HasObjectField(Action<IObjectFieldBuilder> configure);
    public IEntityBuilder HasValueField(Action<IValueFieldBuilder> configure);
    public IEntityBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure);
    public IEntityBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure);
    //public BsonDocument Build();
    
}

public class EntityBuilder:IEntityBuilder {
    public List<Field> Fields { get; set; } = [];
    public IEntityBuilder HasObjectField(Action<IObjectFieldBuilder> configure) {
        var builder=BuilderFactory.GetBuilder<IObjectFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasValueField(Action<IValueFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<IValueFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasSelectionField(Action<ISelectionFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<ISelectionFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    public IEntityBuilder HasCalculatedField(Action<ICalculatedFieldBuilder> configure){
        var builder=BuilderFactory.GetBuilder<ICalculatedFieldBuilder>();
        configure(builder);
        this.Fields.Add(builder.Field);

        return this;
    }

    /*public BsonDocument Build() {
        BsonDocument schema=new BsonDocument();

        foreach (var field in this.Fields) {
            
        }
        
        return schema;
    }

    private void AddFieldToSchema(ref BsonDocument schema, Field field) {
        var doc=new BsonDocument {
            { "BsonType", field.BsonType },
            { "TypeCode", field.TypeCode },
            { "FieldType", field.GetType().Name },
        };
        if (field is ObjectField objectField) {
            var bsonArray=new BsonArray();
            foreach (var subField in objectField.Fields) {
                var subDoc = new BsonDocument();
                this.AddFieldToSchema(ref subDoc, subField);
                bsonArray.Add(subDoc);
            }
            doc.Add("Fields", bsonArray);
        }else if (field is ValueField valueField) {
            doc.Add("DefaultValue", BsonValue.Create(valueField.DefaultValue));
            doc.Add("UnitName", valueField.UnitName);
            doc.Add("QuantityName",valueField.QuantityName);
        }else if (field is SelectionField selectField) {
            doc.Add("SelectionItems", BsonValue.Create(selectField.SelectionDictionary));
            doc.Add("DefaultValue",BsonValue.Create(selectField.DefaultValue));
        }else if (field is CalculatedField calculatedField) {
            doc.Add("DefaultValue", BsonValue.Create(calculatedField.DefaultValue));
            doc.Add("UnitName", calculatedField.UnitName);
            doc.Add("QuantityName", calculatedField.QuantityName);
            var bsonArray=new BsonArray();
            foreach (var variable in calculatedField.Variables ?? []) {
                var varDoc = new BsonDocument();
                varDoc.Add("VariableName",variable.VariableName);
                if (variable is ValueVariable valueVariable) {
                    varDoc.Add("Value",valueVariable.Value);
                }else if (variable is ReferenceVariable refVariable) {
                    varDoc.Add("ReferenceCollection",refVariable.ReferenceCollection);
                    varDoc.Add("Reference",refVariable.ReferenceCollection);
                }else if (variable is ReferenceSubVariable subVariable) {
                    
                }
            }
        }
        schema.Add(field.FieldName, doc);
    }*/
}