using System;

namespace MongoDB.Entities;

public static class BuilderFactory {
    public static TBuilder GetBuilder<TBuilder>() where TBuilder:IBuilder {
        return typeof(TBuilder) switch {
            { } t when t == typeof(IObjectFieldBuilder) => (TBuilder)(new ObjectFieldBuilder() as IFieldBuilder),
            { } t when t == typeof(IValueFieldBuilder) => (TBuilder)(new ValueFieldBuilder() as IFieldBuilder),
            { } t when t == typeof(ISelectionFieldBuilder) => (TBuilder)(new SelectionFieldBuilder() as IFieldBuilder),
            { } t when t == typeof(ICalculatedFieldBuilder) => (TBuilder)(new CalculatedFieldBuilder() as IFieldBuilder),
            { } t when t == typeof(IValueVariableBuilder) => (TBuilder)(new ValueVariableBuilder() as IVariableBuilder),
            { } t when t == typeof(IRefVariableBuilder) => (TBuilder)(new ReferenceVariableBuilder() as IVariableBuilder),
            { } t when t == typeof(IRefSubVariableBuilder) => (TBuilder)(new ReferenceSubVariableBuilder() as IVariableBuilder),
            _ => throw new NotSupportedException($"Builder for type {typeof(TBuilder).Name} is not supported.") };
    }
    
    public static IFieldBuilder GetFieldBuilderByType<TField>() where TField:Field {
        return typeof(TField) switch {
            { } t when t == typeof(ObjectField) => new ObjectFieldBuilder(),
            { } t when t == typeof(ValueField) => new ValueFieldBuilder(),
            { } t when t == typeof(SelectionField) => new SelectionFieldBuilder(),
            { } t when t == typeof(CalculatedField) => new CalculatedFieldBuilder(),
            _ => throw new NotSupportedException($"Builder for type {typeof(TField).Name} is not supported.") };
    }
}