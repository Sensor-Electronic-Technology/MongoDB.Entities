// See https://aka.ms/new-console-template for more information

using System.Collections.ObjectModel;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Z.Expressions;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using Community.CsharpSqlite;
using ConsoleTesting;
using Jint;
using MongoDB.Driver.Linq;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Scripting.Runtime;
using NCalcExtensions;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NLua;
using Expression = NCalc.Expression;

//await DB.InitAsync("epi_system","172.20.3.41");

//await GenerateEpiData();
//await CreateTypeConfiguration();
TestNCalc();

/*//var engine = new Engine(options => options.AllowClr());
var evalContext = new EvalContext();
evalContext.RegisterAssembly(typeof(EpiRun).Assembly, typeof(Monitoring).Assembly, typeof(Monitoring).Assembly);
evalContext.RegisterUsingDirective("MongoDB.Entities");
evalContext.RegisterUsingDirective("System.Linq.Dynamic.Core");
evalContext.RegisterUsingDirective("System.Linq");
evalContext.RegisterUsingDirective("MongoDB.Driver");

string refType = "Monitoring";
string query = "Where(e=>e.EpiRun.ID==\"67c6107d81555dcdab57529c\" && e.Weight1>2).Select(e=>e.Weight2).ToList()";
string id = "67c6107d81555dcdab57529c";
string param = "Weight1";


string linq = @"
        (from e in DB.Entity<{typeName}>().Queryable()
        where e.EpiRun.ID == {id} && e.{property} > {value}
        select e.{property}).ToList()";
var linqQuery = linq.Replace("{id}", "\"67c6107d81555dcdab57529c\"").
                    Replace("{param}", param)
                   .Replace("{value}", 2.ToString()).
                    Replace("{refType}", refType);
Console.WriteLine(linqQuery);
var script = CSharpScript.Create<List<double>>(linqQuery, 
    ScriptOptions.Default
                 .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                 .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq"));*/




/*var json=JsonSerializer.Serialize(result);
Console.WriteLine(json);*/



/*Dictionary<string,object> variables = new Dictionary<string, object>(); ;

foreach(var variable in field.Variables) {
    if(variable is ReferenceVariable referenceVariable) {
        
    }else if (variable is ValueVariable) {
        
    }else if(variable is ReferenceSubVariable refSubVariable) {

        
        
        CSharpScript.Create<object>($"", 
            ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                         .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));
    }
}

var cursorAsync = await DB.Entity<EpiRun>().Fluent().Match(_ => true).ToCursorAsync();

while(await cursorAsync.MoveNextAsync()) {
    var runs = cursorAsync.Current;
    foreach(var run in runs) {
    
    }
}*/



/*string type="EpiRun";
string item = "W001";

DB.Entity<EpiRun>().Queryable().Where("e=>e.Name.Contains(\"W001\")").FirstOrDefault();*/

/*var result = await CSharpScript.EvaluateAsync<EpiRun>($"DB.Entity<{type}>().Fluent().Match(e=>e.Name.Contains(\"{item}\")).FirstOrDefault()", 
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly).WithImports("MongoDB.Entities","MongoDB.Driver"));
Console.WriteLine($"Eval Find: {result.Name}");*/

/*var result = await CSharpScript.EvaluateAsync<object>($"DB.Entity<{type}>().Queryable().Where(\"e=>e.Name.Contains(\\\"{item}\\\")\").FirstOrDefault()", 
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                              .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));
Console.WriteLine($"Eval Find: {((EpiRun)result).Name}");*/

/*var script = CSharpScript.Create<object>($"DB.Entity<{type}>().Queryable().Where(\"e=>e.Name.Contains(\\\"{item}\\\")\").FirstOrDefault()", 
                 ScriptOptions.Default.WithReferences(typeof(DB).Assembly,typeof(EpiRun).Assembly)
                              .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core"));*/
/*script.Compile();
Console.WriteLine(JsonSerializer.Serialize(script, new JsonSerializerOptions() { WriteIndented = true }));*/

/*var result = await script.RunAsync();
Console.WriteLine($"Eval Find: {((EpiRun)result.ReturnValue).Name}");*/

//var run=engine.Evaluate("DB.Entity<EpiRun>().Fluent().Match(e=>e.Name.Contains(\"W001\")).FirstOrDefault()");

void TestNCalc() {
    var expression = new ExtendedExpression("median([powers])");
    expression.Parameters.Add("powers", new Collection<double>() { 1.0, 2.0, 3.0, 4.0, 5.0 });
    Console.WriteLine(expression.Evaluate());
}

async Task CreateTypeConfiguration() {
    /*TypeConfigurationMap configMap = new TypeConfigurationMap() {
        CollectionName = DB.CollectionName<QuickTest>(),
    };*/

    TypeConfiguration config = new TypeConfiguration() {
        CollectionName = DB.CollectionName<QuickTest>(),
    };
    
    ObjectField objField = new ObjectField() {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields=[
            new CalculatedField() {
                FieldName="Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionVariable() {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        CollectionFilter = string.Empty,
                    }
                ]
            },
            new CalculatedField() {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionVariable() {
                        Property = "Wavelengths",
                        VariableName = "wavelengths",
                        CollectionProperty = "Measurements",
                        CollectionFilter = string.Empty
                    },
                ]
            }
        ]
    };
    
    foreach(var field in objField.Fields) {
        if (field is ObjectField oField) {
            
        }else if (field is CalculatedField cField) {

        }else if (field is ValueField vField) {
            
        }else if (field is SelectionField sField) {
            
        }
    }
    
    

    /*var script = CSharpScript.Create<double>($"QueryObject.{((CollectionVariable)pField.Variables[0]).CollectionProperty}.Select(e => e.{((CollectionVariable)pField.Variables[0]).Property}).Average();",
        globalsType:typeof(ScriptInput<QuickTest>),
        options:ScriptOptions.Default
                             .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                             .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq"));
    script.Compile();
    var cursor = await DB.Fluent<QuickTest>().ToCursorAsync();

    while (await cursor.MoveNextAsync()) {
        foreach (var qt in cursor.Current) {
            var avg=qt.InitialMeasurements.Select(e => e.Power).Average();
            //var avg=(await script.RunAsync(new ScriptInput<QuickTest>(){QueryObject =qt})).ReturnValue;
            Console.WriteLine($"{qt.WaferId} Initial Power Avg: {avg}");
        }
    }*/
    /*var quickTests = await DB.Entity<EpiRun>().QuickTests.ChildrenFluent().ToListAsync();

    foreach (var quickTest in quickTests) {
        quickTest.InitialMeasurements.AsQueryable().Select("").Average();
    }*/
    /*var powers = quickTests.AsQueryable()
                       .SelectMany("e => e.InitialMeasurements.Select(e => e.Power)")
                       .ToListDynamic()
                       .Cast<double>();*/
    
    /*var script = CSharpScript.Create<double>($"queryObject.{}.AsQueryable().Select(\"\").Average() ?? 0.00",
        globalsType:typeof(ScriptInput<QuickTest>),
        options:ScriptOptions.Default
                             .WithReferences(typeof(DB).Assembly, typeof(EpiRun).Assembly, typeof(Monitoring).Assembly)
                             .WithImports("MongoDB.Entities","MongoDB.Driver", "System.Linq","System.Linq.Dynamic.Core","System.Linq.Dynamic"));*/
}

void TestMigrationBuilder() {
    MigrationBuilder builder = new MigrationBuilder();
    builder.AddField("", "", new Field());
}

async Task GenerateEpiData() {
    var rand = new Random();
    var now = DateTime.Now;
    List<EpiRun> epiRuns = [];
    List<QuickTest> quickTests = [];
    List<XrdData> xrdMeasurementData = [];
    for (int i = 1; i <= 100; i++) {
        for (int x = 1; x <= 10; x++) {
            EpiRun run = new EpiRun() {
                RunTypeId = (rand.NextDouble()>.5)? "Prod":"Rnd",
                SystemId = "B01",
                TechnicianId = (rand.NextDouble()>.5)? "RJ":"NC",
            };
            run.TimeStamp = now;
        
            string tempId = "";
            string ledId = "";
            string rlId = "";
            GenerateWaferIds(i,"A03","A02","B01", ref tempId, ref rlId, ref ledId);
        
            run.RunNumber = ledId.Substring(ledId.LastIndexOf('-') + 1);
            
            string tempId_P = tempId;
            string ledId_P = ledId;
            string rlId_P = rlId;
            if (x / 10 >= 1) {
                tempId_P += $"-{x}";
                rlId_P += $"-{x}";
                ledId_P += $"-{x}";
                run.PocketNumber = $"{x}";
            } else {
                tempId_P += $"-0{x}";
                rlId_P += $"-0{x}";
                ledId_P += $"-0{x}";
                run.PocketNumber = $"0{x}";
            }
            run.WaferId = ledId_P;
            epiRuns.Add(run);
            /*await run.SaveAsync();*/

            var quickTestData = new QuickTest() {
                WaferId=run.WaferId,
    
                TimeStamp = now,
                InitialMeasurements = new List<QtMeasurement>() {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                },
                FinalMeasurements = new List<QtMeasurement>() {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                }
            };
            quickTests.Add(quickTestData);

            /*await quickTestData.SaveAsync();
            await run.QuickTests.AddAsync(quickTestData);*/
            var xrdData = new XrdData() {
                WaferId = run.WaferId,
                XrdMeasurements = new List<XrdMeasurement>() {
                    GenerateXrdMeasurement(rand, "C", now),
                    GenerateXrdMeasurement(rand, "Edge", now)
                }
            };
            xrdMeasurementData.Add(xrdData);
            /*await xrdMeasurements.SaveAsync();
            await run.XrdMeasurements.AddAsync(xrdMeasurements);*/
        }//end pocked for loop
    }//end run number for loop
    await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();
    
    epiRuns.ForEach(
        run => {
            var qt=quickTests.FirstOrDefault(e => e.WaferId == run.WaferId);
            var xrd=xrdMeasurementData.FirstOrDefault(e => e.WaferId == run.WaferId);
            if (qt != null) {
                run.QuickTest=qt.ToReference();
                qt.EpiRun = run.ToReference();
            }

            if (xrd != null) {
                run.XrdData=xrd.ToReference();
                xrd.EpiRun = run.ToReference();
            }
        });
    
    await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();
    Console.WriteLine("Check Database");
}

XrdMeasurement GenerateXrdMeasurement(Random rand,string Area,DateTime now) {
    var xrd = new XrdMeasurement() {
        XrdArea = Area,
        TimeStamp = now,
        Alpha_AlN = NextDouble(rand, 35.937, 36.0211),
        Beta_AlN = NextDouble(rand, 35.9472, 36.0754),
        FHWM102 = NextDouble(rand, 180, 568.8),
        FWHM002 = NextDouble(rand, 7.2, 358.8),
        dOmega = NextDouble(rand, 0.0065, .3748),
        Omega = NextDouble(rand,16.2183,18.3815)
    };
    return xrd;
}

QtMeasurement GenerateQtMeasurement(Random rand,string Area,DateTime now) {
    var qt = new QtMeasurement() {
        Area = Area,
        TimeStamp = now,
        Current=20.0,
        Power= NextDouble(rand,700,1700),
        Voltage = NextDouble(rand,9.5,15.5),
        Wavelength= NextDouble(rand,270,279.9)
    };
    return qt;
}

void GenerateWaferIds(int i,string tempSystem,string rlSystem,string ledSystem,ref string tempId, ref string rlId,ref string ledId) {
    tempId = tempSystem;
    rlId = rlSystem;
    ledId = ledSystem;
    if (i / 1000 >= 1) {
        tempId+= $"-{i}";
        rlId+= $"-{i}";
        ledId+= $"-{i}";

            
    }else if (i / 100 >= 1) {
        tempId+= $"-0{i}";
        rlId+= $"-0{i}";
        ledId+= $"-0{i}";

    }else if (i / 10 >= 1) {
        tempId+= $"-00{i}";
        rlId+= $"-00{i}";
        ledId+= $"-00{i}";

    } else {
        tempId+= $"-000{i}";
        rlId += $"-000{i}";
        ledId += $"-000{i}";
    }
}

double NextDouble(Random rand,double min, double max)
    => rand.NextDouble() * (max - min) + min;

public class ScriptInput<TEntity> where TEntity:Entity {
    public TEntity QueryObject { get; set; }
    public string? SelectStatement { get; set; }
    public string? FilterStatement { get; set; }
}

/*var minValue = 1;
var maxValue = 10;
List<int> list = [0,2,6,4,2,4,6,10,3];

var expression = "return list.Where(x => x > minValue && x < maxValue)";
var list2a2 = Eval.Execute<List<int>>(expression, new { minValue, maxValue,list });
Console.WriteLine(string.Join(",", list2a2));*/


//await BuildDummyDatabase();

//var value=query.Select(e => e.Weight1).ToList().Average();

/*

*/