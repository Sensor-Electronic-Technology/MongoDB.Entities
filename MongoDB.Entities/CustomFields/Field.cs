using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;


/// <summary>
/// For the AvailableProperties property in <see cref="TypeConfiguration"/>
/// Useful for the UI layer when displaying custom fields in a table
/// </summary>
[BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(ObjectFieldInfo))]
public class FieldInfo {
    public TypeCode TypeCode { get; set; }
}

public class ObjectFieldInfo : FieldInfo {
    public Dictionary<string,FieldInfo> Fields { get; set; } = [];
}

/// <summary>
/// Defines a custom field.
/// ObjectField: Field that holds other fields, aka a entity
/// ValueField: Field that only holds a value
/// CalculatedField: Field that is calculated
/// SelectionField: Field that can only be from a given set of value, aka enum, drop down list, etc
/// </summary>
[BsonDiscriminator(RootClass = true),
 BsonKnownTypes(typeof(ObjectField), 
     typeof(ValueField), 
     typeof(SelectionField), 
     typeof(CalculatedField))]
public class Field:IEquatable<Field> {
    public string FieldName { get; set; } = string.Empty;
    public BsonType BsonType { get; set; }
    public TypeCode TypeCode { get; set; }
    
    public virtual KeyValuePair<string,FieldInfo> ToFieldInfo() {
        return new KeyValuePair<string, FieldInfo>(FieldName, new() { TypeCode = TypeCode });
    }

    public bool Equals(Field? other) {
        if (other is null)
            return false;
        return FieldName == other.FieldName;
    }

    public override bool Equals(object? obj) {
        if (obj is null)
            return false;
        
        return Equals((Field)obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(FieldName);
}

public class ObjectField : Field {
   public List<Field> Fields { get; set; } = [];

   public override KeyValuePair<string, FieldInfo> ToFieldInfo() {
       return ProcessField();
   }

   private KeyValuePair<string, FieldInfo> ProcessField() {
       ObjectFieldInfo objInfo = new ObjectFieldInfo {
           TypeCode = TypeCode,
           Fields = []
       };
       foreach (var subField in Fields) {
           var pair=subField.ToFieldInfo();
           objInfo.Fields.Add(pair.Key, pair.Value);
       }
       return new(FieldName, objInfo);
   }
}

public class ValueField : Field {
    public DataType DataType { get; set; }
    public object? DefaultValue { get; set; }
    public string? UnitName { get; set; } = string.Empty;
    public string? QuantityName { get; set; } = string.Empty;
}

public class SelectionField : Field {
    public DataType DataType { get; set; }
    public Dictionary<string,object> SelectionDictionary { get; set; } = new();
    public object? DefaultValue { get; set; }
}

public partial class CalculatedField:ValueField  {
    //Regex.Matches(expression, @"\[(.*?)\]", RegexOptions.Compiled);
    public string Expression { get; set; } = string.Empty;
    public List<Variable> Variables { get; set; } = [];
    public bool IsBooleanExpression { get; set; } = false;
    public object TrueValue { get; set; } = true;
    public object FalseValue { get; set; } = false;
    
    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex MyRegex();
    public bool IsValid() {
         var regex = MyRegex();
         var matches=regex.Matches(Expression);

         if (matches.Count!=Variables.Count) {
             return false;
         }
         bool isValid=true;
         var variables = Variables.Select(e => e.VariableName).ToArray();
         
         foreach (Match match in matches) {
             if (!variables.Contains(match.Groups[1].Value)) {
                 isValid = false;
                 break;
             }
         }
         return isValid;
    }
}

