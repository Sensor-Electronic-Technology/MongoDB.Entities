using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DocumentMigrations {
    
    
    [TestMethod]
    public async Task migrating_object_field() {
        
        ObjectField objField = new ObjectField {
            FieldName = "Qt Summary",
            BsonType = BsonType.Document,
            TypeCode = TypeCode.Object,
            Fields = [
                new CalculatedField {
                    FieldName = "Avg. Initial Power",
                    BsonType = BsonType.Double,
                    DefaultValue = 0.00,
                    Expression = "avg([powers])",
                    Variables = [
                        new CollectionPropertyVariable {
                            Property = "Power",
                            VariableName = "powers",
                            CollectionProperty = "InitialMeasurements",
                            Filter = new() {
                                FieldName = nameof(QtMeasurement.Power),
                                CompareOperator = ComparisonOperator.LessThanOrEqual,
                                FilterLogicalOperator = LogicalOperator.And,
                                Value = 1100,
                                Filters = new List<Filter> {
                                    new() {
                                        FieldName = nameof(QtMeasurement.Power),
                                        CompareOperator = ComparisonOperator.GreaterThan,
                                        FilterLogicalOperator = LogicalOperator.And,
                                        Value = 500
                                    },
                                    new() {
                                        FieldName = "Wavelength",
                                        CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                        FilterLogicalOperator = LogicalOperator.And,
                                        Value = 270,
                                        Filters = new List<Filter> {
                                            new() {
                                                FieldName = "Wavelength",
                                                CompareOperator = ComparisonOperator.LessThanOrEqual,
                                                FilterLogicalOperator = LogicalOperator.Or,
                                                Value = 279
                                            }
                                        }
                                    }
                                }
                            },
                            VariableType = VariableType.LIST_NUMBER
                        }
                    ]
                },
                new CalculatedField {
                    FieldName = "Avg. Wl",
                    BsonType = BsonType.Double,
                    DefaultValue = 0.00,
                    Expression = "avg([wavelengths])",
                    Variables = [
                        new CollectionPropertyVariable {
                            Property = nameof(QtMeasurement.Wavelength),
                            VariableName = "wavelengths",
                            CollectionProperty = nameof(QuickTest.InitialMeasurements),
                            VariableType = VariableType.LIST_NUMBER,
                            Filter = new() {
                                FieldName = nameof(QtMeasurement.Power),
                                CompareOperator = ComparisonOperator.LessThanOrEqual,
                                FilterLogicalOperator = LogicalOperator.And,
                                Value = 1100,
                                Filters = new List<Filter> {
                                    new() {
                                        FieldName = nameof(QtMeasurement.Power),
                                        CompareOperator = ComparisonOperator.GreaterThan,
                                        FilterLogicalOperator = LogicalOperator.And,
                                        Value = 500
                                    },
                                    new() {
                                        FieldName = "Wavelength",
                                        CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                        FilterLogicalOperator = LogicalOperator.And,
                                        Value = 270,
                                        Filters = new List<Filter> {
                                            new() {
                                                FieldName = "Wavelength",
                                                CompareOperator = ComparisonOperator.LessThanOrEqual,
                                                FilterLogicalOperator = LogicalOperator.Or,
                                                Value = 279
                                            }
                                        }
                                    }
                                }
                            },
                        },
                    ]
                }
            ]
        };
        
    }
}