using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Entities;

using System.Reflection;

public static class ReflectionHelper {
    public static void SetNestedProperty(object obj, string propertyPath, object value) {
        try {
            string[] properties = propertyPath.Split('.');
            object? current = obj;

            for (int i = 0; i < properties.Length - 1; i++) {
                PropertyInfo? property = current.GetType().GetProperty(properties[i]);
                if (property == null)
                    return;

                object? propertyValue = property.GetValue(current);

                if (propertyValue == null) {
                    // Create instance based on property type
                    propertyValue = CreateInstanceForType(property.PropertyType);
                    property.SetValue(current, propertyValue);
                }

                current = propertyValue;
            }

            if (current == null)
                return;

            PropertyInfo? lastProperty = current.GetType().GetProperty(properties[^1]);
            if (lastProperty == null)
                return;

            object? convertedValue = ConvertValue(value, lastProperty.PropertyType);
            if (convertedValue == null)
                return;

            lastProperty.SetValue(current, convertedValue);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to set property {propertyPath}", ex);
        }
    }

    static void SetNestedPropertyOld(object obj, string propertyPath, object value) {
        string[] properties = propertyPath.Split('.');
        object current = obj;
        for (int i = 0; i < properties.Length - 1; i++) {
            PropertyInfo property = current.GetType().GetProperty(properties[i]);
            object propertyValue = property.GetValue(current);

            if (propertyValue == null) {
                // Create a new instance of the property type
                propertyValue = Activator.CreateInstance(property.PropertyType);

                // Set the new instance to the property
                property.SetValue(current, propertyValue);
            }

            current = propertyValue;
        }

        PropertyInfo lastProperty = current.GetType().GetProperty(properties[^1]);
        lastProperty.SetValue(current, value);
    }

    private static object? CreateInstanceForType(Type type) {
        if (type == typeof(string))
            return string.Empty;

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        if (type.IsClass)
            return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create instance of {type}");

        throw new ArgumentException($"Cannot create instance of type {type}");
    }

    private static object? ConvertValue(object? value, Type targetType) {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        // Handle enum conversion
        if (targetType.IsEnum) {
            if (value is string stringValue)
                return Enum.Parse(targetType, stringValue, true);

            return Enum.ToObject(targetType, value);
        }

        // Handle numeric conversions
        if (IsNumericType(targetType) && IsNumericType(value.GetType()))
            return Convert.ChangeType(value, targetType);

        // Handle string to number conversion
        if (IsNumericType(targetType) && value is string stringVal) {
            if (double.TryParse(stringVal, out double numericValue))
                return Convert.ChangeType(numericValue, targetType);
        }

        // Handle DateTime
        if (targetType == typeof(DateTime) && value is string dateString)
            return DateTime.Parse(dateString);

        // Default conversion
        return Convert.ChangeType(value, targetType);
    }

    private static bool IsNumericType(Type type) {
        switch (Type.GetTypeCode(type)) {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }
    
    public static IEnumerable<TInterface> GetInterfaceImplementingProperties<TInterface>(object instance)
        where TInterface : class {
        return GetPropertiesImplementInterface(instance, typeof(TInterface)).Cast<TInterface>();
    }
    public static IEnumerable<object> GetPropertiesImplementInterface(object instance, Type interfaceType) {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(interfaceType);
        if (!interfaceType.IsInterface)
            throw new ArgumentException("Provided type must be an interface");

        var result = new List<object>();
        var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props) {
            var propType = prop.PropertyType;
            var value = prop.GetValue(instance);

            if (value == null)
                continue;

            // Direct implementation
            if (interfaceType.IsAssignableFrom(propType)) {
                result.Add((object)value);
            }else if (propType.IsArray) {
                // Array case
                var elementType = propType.GetElementType();
                if (elementType == null || !interfaceType.IsAssignableFrom(elementType))
                    continue;

                var items = (IEnumerable)value;
                foreach (var item in items)
                    if (item != null)
                        result.Add(item);
            }else if (propType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propType)) {
                // Generic IEnumerable (e.g., List<T>)
                var genericArg = propType.GetGenericArguments().FirstOrDefault();
                if (genericArg == null || !interfaceType.IsAssignableFrom(genericArg))
                    continue;

                var items = (IEnumerable)value;
                foreach (var item in items)
                    if (item != null)
                        result.Add(item);
            }
        }
        return result;
    }
}