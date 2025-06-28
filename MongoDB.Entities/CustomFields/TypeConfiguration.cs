using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/// <summary>
/// Defines a DocumentEntity's custom fields 
/// </summary>
[Collection("type_configurations"),
 BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(EmbeddedTypeConfiguration))]
public class TypeConfiguration:Entity {
    public string CollectionName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string TypeName { get; set; } = null!;
    public Dictionary<string, FieldInfo> AvailableProperties { get; set; } = [];
    public DocumentVersion DocumentVersion { get; set; } = new(0,0,0);
    public IList<Field> Fields { get; set; } = [];
    public Many<DocumentMigration,TypeConfiguration> Migrations { get; set; }
    public TypeConfiguration() {
        this.InitOneToMany(() => Migrations);
    }
    
    static TypeConfiguration() {
        DB.Index<TypeConfiguration>()
          .Key(e => e.TypeName, KeyType.Text)
          .Option(o => o.Unique = false)
          .CreateAsync().Wait();
        
        DB.Index<TypeConfiguration>()
          .Key(e => e.CollectionName, KeyType.Text)
          .Option(o => o.Unique = false)
          .CreateAsync().Wait();
        
    }

    public static TypeConfiguration? Create<TEntity>(string collectionName, string databaseName) where TEntity : IDocumentEntity {
        var typeConfig= new TypeConfiguration {
            CollectionName = collectionName,
            DatabaseName = databaseName,
            Fields = [],
        };
        Type type= typeof(TEntity);
        var typeName=type.AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            typeConfig.AvailableProperties.Add(prop.Name, new(){TypeCode = prop.PropertyType.GetTypeCode()});
        }
        return typeConfig;
    }
    
    public static TypeConfiguration? CreateOnline<TEntity>() where TEntity : IDocumentEntity {
        var typeConfig= new TypeConfiguration {
            CollectionName = DB.CollectionName<TEntity>(),
            DatabaseName = DB.DatabaseName<TEntity>(),
            Fields = [],
        };
        Type type= typeof(TEntity);
        var typeName=type.AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;
            typeConfig.AvailableProperties.Add(prop.Name, new(){TypeCode = Type.GetTypeCode(prop.PropertyType)});
        }
        return typeConfig;
    }

    public void UpdateAvailableProperties() {
        var type=Type.GetType(TypeName);
        if (type == null) {
            return;
        }
        AvailableProperties.Clear();
        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;
            AvailableProperties.Add(prop.Name, new(){TypeCode = Type.GetTypeCode(prop.PropertyType)});
        }
        foreach (var field in Fields) {
            var pair=field.ToFieldInfo();
            AvailableProperties[pair.Key] = pair.Value;
        }
    }

    public Dictionary<string, object?> GetValueDictionary() {
        Dictionary<string, object?> additionalData = [];
        var valueFields=this.Fields.Where(e => e is not CalculatedField);
        foreach (var vField in valueFields) {
            if (vField is ObjectField objField) {
                var objPair=objField.GetValueDictionary();
                additionalData[objPair.Key] = objPair.Value;
            }else if (vField is ValueField valField) {
                var valPair=valField.GetValueDictionary();
                additionalData[valPair.Key] = valPair.Value;
            }else if (vField is SelectionField selField) {
                var selPair=selField.GetValueDictionary();
                additionalData[selPair.Key] = selPair.Value;
            }    
        }
        return additionalData;
    }
}
/// <summary>
/// TypeConfiguration for an embedded type in the main field
/// </summary>
[Collection("type_configurations")]
public class EmbeddedTypeConfiguration : TypeConfiguration {
    /*/// <summary>
    ///  ParentType
    /// </summary>
    public string ChildProperty { get; set; } = null!;*/
    public List<string> PropertyNames { get; set; } = [];
    public bool IsArray { get; set; } = false;
    public static EmbeddedTypeConfiguration? CreateOnline<TEntity,TEmbedded>(
            List<string> propertyNames,
            bool isArray = false
        ) where TEntity : IDocumentEntity where TEmbedded:IEmbeddedEntity {
        var typeConfig= new EmbeddedTypeConfiguration() {
            CollectionName = DB.CollectionName<TEntity>(),
            DatabaseName = DB.DatabaseName<TEntity>(),
            Fields = []
        };
        /*typeConfig.ChildProperty = childPropName;*/
        typeConfig.PropertyNames = propertyNames;
        typeConfig.IsArray = isArray;
        Type type= typeof(TEmbedded);
        var typeName=type.AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IEmbeddedEntity.AdditionalData))
                continue;
            typeConfig.AvailableProperties.Add(prop.Name, new(){TypeCode = Type.GetTypeCode(prop.PropertyType)});
        }
        return typeConfig;
    }
}










