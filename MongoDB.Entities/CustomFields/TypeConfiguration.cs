using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[Collection("type_configurations"),
 BsonDiscriminator(RootClass = true),
 BsonKnownTypes(
     typeof(EmbeddedTypeConfiguration),
     typeof(DocumentTypeConfiguration))]
public class TypeConfiguration : Entity {
    public string TypeName { get; set; } = null!;
    public DocumentVersion DocumentVersion { get; set; } = new(0, 0, 0);

    static TypeConfiguration() {
        DB.Index<TypeConfiguration>()
          .Key(e => e.TypeName, KeyType.Text)
          .Option(o=>o.Unique=false).CreateAsync().Wait();
    }
}

[Collection("type_configurations")]
public class DocumentTypeConfiguration : TypeConfiguration {
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Dictionary<string, FieldInfo> AvailableProperties { get; set; } = [];
    public IList<Field> Fields { get; set; } = [];
    public Many<DocumentMigration, DocumentTypeConfiguration> Migrations { get; set; }

    public DocumentTypeConfiguration() {
        this.InitOneToMany(() => Migrations);
    }

    static DocumentTypeConfiguration() {
        DB.Index<DocumentTypeConfiguration>()
          .Key(e => e.CollectionName, KeyType.Text)
          .Option(o => o.Unique = false)
          .CreateAsync().Wait();
    }

    public static DocumentTypeConfiguration? Create<TEntity>(string collectionName, string databaseName)
        where TEntity : IDocumentEntity {
        var typeConfig = new DocumentTypeConfiguration {
            CollectionName = collectionName,
            DatabaseName = databaseName,
            Fields = [],
        };
        Type type = typeof(TEntity);
        var typeName = type.AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            typeConfig.AvailableProperties.Add(prop.Name, new() { TypeCode = prop.PropertyType.GetTypeCode() });
        }

        return typeConfig;
    }

    public static DocumentTypeConfiguration? CreateOnline<TEntity>() where TEntity : IDocumentEntity {
        var typeConfig = new DocumentTypeConfiguration {
            CollectionName = DB.CollectionName<TEntity>(),
            DatabaseName = DB.DatabaseName<TEntity>(),
            Fields = [],
        };
        Type type = typeof(TEntity);
        var typeName = type.AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;

            typeConfig.AvailableProperties.Add(prop.Name, new() { TypeCode = Type.GetTypeCode(prop.PropertyType) });
        }

        return typeConfig;
    }

    public void UpdateAvailableProperties() {
        var type = Type.GetType(TypeName);

        if (type == null) {
            return;
        }
        AvailableProperties.Clear();

        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;

            AvailableProperties.Add(prop.Name, new() { TypeCode = Type.GetTypeCode(prop.PropertyType) });
        }

        foreach (var field in Fields) {
            var pair = field.ToFieldInfo();
            AvailableProperties[pair.Key] = pair.Value;
        }
    }

    public Dictionary<string, object?> GetValueDictionary() {
        Dictionary<string, object?> additionalData = [];
        var valueFields = this.Fields.Where(e => e is not CalculatedField);

        foreach (var vField in valueFields) {
            if (vField is ObjectField objField) {
                var objPair = objField.GetValueDictionary();
                additionalData[objPair.Key] = objPair.Value;
            } else if (vField is ValueField valField) {
                var valPair = valField.GetValueDictionary();
                additionalData[valPair.Key] = valPair.Value;
            } else if (vField is SelectionField selField) {
                var selPair = selField.GetValueDictionary();
                additionalData[selPair.Key] = selPair.Value;
            }
        }

        return additionalData;
    }
}

/// <summary>
/// DocumentTypeConfiguration for an embedded type in the main field
/// </summary>
[Collection("type_configurations")]
public class EmbeddedTypeConfiguration : TypeConfiguration {
    //public List<string> PropertyNames { get; set; } = [];
    //public string ParentCollection { get; set; }
    //public Dictionary<string, List<Field>> FieldDefinitions { get; set; } = [];
    //public bool IsArray { get; set; } = false;
    public Dictionary<string,EmbeddedFieldDefinitions> FieldDefinitions { get; set; } = [];
    
    public Many<EmbeddedMigration,EmbeddedTypeConfiguration> EmbeddedMigrations { get; set; }

    public EmbeddedTypeConfiguration() {
        this.InitOneToMany(()=>EmbeddedMigrations);
    }

    public static async Task<EmbeddedTypeConfiguration?> CreateOnline<TEntity, TEmbedded>(List<string> propertyNames, bool isArray = false)
        where TEntity : IDocumentEntity where TEmbedded : IEmbeddedEntity {
        Type type = typeof(TEmbedded);
        Type parent = typeof(TEntity);
        var typeName = type.Name;
        var parentTypeName = parent.Name;

        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(parentTypeName)) {
            return null;
        }

        var typeConfig=await DB.Find<EmbeddedTypeConfiguration>()
                               .Match(e => e.TypeName == typeName)
                               .ExecuteSingleAsync() ??
                       new() {
            TypeName = typeName
        };

        if (!typeConfig.FieldDefinitions.TryGetValue(parentTypeName, out var fieldDef)) {
            fieldDef=new() {
                DatabaseName = DB.DatabaseName<TEntity>(),
                ParentCollection = DB.CollectionName<TEntity>(),
                PropertyNames = propertyNames,
                IsArray = isArray,
                Fields = [],
                AvailableProperties = []
            };
            typeConfig.FieldDefinitions[parentTypeName] = fieldDef;
        }
        
        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IEmbeddedEntity.AdditionalData))
                continue;
            fieldDef.AvailableProperties[prop.Name] = new() { TypeCode = Type.GetTypeCode(prop.PropertyType) };
        }
        return typeConfig;
    }
}

public class EmbeddedFieldDefinitions {
    public string DatabaseName { get; set; } = null!;
    public string ParentCollection { get; set; } = null!;
    public bool IsArray { get; set; } = false;
    public List<string> PropertyNames { get; set; } = [];
    public List<Field> Fields { get; set; } = [];
    public Dictionary<string, FieldInfo> AvailableProperties { get; set; } = [];
    public void UpdateAvailableProperties(string typeName) {
        var type = Type.GetType(typeName);

        if (type == null) {
            return;
        }
        AvailableProperties.Clear();

        foreach (var prop in type.GetProperties()) {
            if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;
            
            AvailableProperties.Add(prop.Name, new() { TypeCode = Type.GetTypeCode(prop.PropertyType) });
        }

        foreach (var pair in Fields.Select(field => field.ToFieldInfo())) {
            AvailableProperties[pair.Key] = pair.Value;
        }
    }
}