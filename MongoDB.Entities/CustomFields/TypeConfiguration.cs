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
          .Option(o => o.Unique = true)
          .CreateAsync().Wait();
        
        DB.Index<TypeConfiguration>()
          .Key(e => e.CollectionName, KeyType.Text)
          .Option(o => o.Unique = true)
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
}
/// <summary>
/// TypeConfiguration for an embedded type in the main field
/// </summary>
public class EmbeddedTypeConfiguration : TypeConfiguration {
    /// <summary>
    /// PropertyName of the embedded property
    /// </summary>
    public string ChildProperty { get; set; } = null!;
}










