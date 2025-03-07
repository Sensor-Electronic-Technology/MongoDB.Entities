using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.Expressions;

/*public enum FunctionsBuiltIn {
    AVG,
    MIN,
    MAX,
    STDEV,
    SUM,
    COUNT,
    FIRST,
    LAST
}*/

public class FunctionsBuiltIn {
    public static double Avg(IEnumerable<double> list) {
        return list.Average();
    }

    public static double Sum(IEnumerable<double> list) {
        return list.Sum();
    }
    
    public static double Stdev(IEnumerable<double> list) {
        return Math.Sqrt(Variance(list));
    }
    
    public static double Variance(IEnumerable<double> values) {
        var mean = values.Average();
        return values.Average(x => Math.Pow(x - mean, 2));
    }
    
    public static double StandardDeviation(IEnumerable<double> values) {
        return Math.Sqrt(Variance(values));
    }
}