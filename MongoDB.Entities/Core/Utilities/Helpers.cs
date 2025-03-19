using System.Collections.Generic;
using System.ComponentModel;

namespace MongoDB.Entities;

public static class Helpers {
    public static Dictionary<string, object> DynamicToDictionary(dynamic source) {
        var dictionary = new Dictionary<string, object>();
        foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(source)) {
            object obj = propertyDescriptor.GetValue(source);
            dictionary.Add(propertyDescriptor.Name, obj);
        }
        return dictionary;
    }
}