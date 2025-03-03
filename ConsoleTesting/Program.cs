// See https://aka.ms/new-console-template for more information

using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using Z.Expressions;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using MongoDB.Driver.Linq;

/*var minValue = 1;
var maxValue = 10;
List<int> list = [0,2,6,4,2,4,6,10,3];
	
var expression = "return list.Where(x => x > minValue && x < maxValue)";
var list2a2 = Eval.Execute<List<int>>(expression, new { minValue, maxValue,list });
Console.WriteLine(string.Join(",", list2a2));*/


//await BuildDummyDatabase();

//var value=query.Select(e => e.Weight1).ToList().Average();

var field = new CalculatedField() {
    FieldName = "Average",
    BsonType = BsonType.Double,
    TypeCode = TypeCode.Double,
    Expression = @"
                    if(value>Target)
                        return 'Pass';
                    else
                        return 'Fail';
                    ",
    Variables = [
        new ReferenceVariable() {
            VariableName = "value",
            ReferenceProperty = "Weight1",
            ReferenceCollection = DB.CollectionName<Monitoring>(),
            QueryExpression = "Select(x=>x.Weight1).ToList().Average()",
        },
        new NumberVariable() {
            VariableName = "Target",
            Value = 5,
        }
    ]
};

var dataType = new Type [] { typeof(string)};
var genericBase = typeof(List<>);
var combinedType = genericBase.MakeGenericType(dataType);
var listStringInstance = Activator.CreateInstance(combinedType);

await DB.InitAsync("epi_system", "172.20.3.41");

var queryable = DB.Entity<EpiRun>().Queryable();
var run=await queryable.Where("entity=>entity.Name.Contains(\"W005\")").FirstOrDefaultAsync();
/*var weights =from monitor in run.EpiRunMonitoring.ChildrenQueryable()
            select monitor.Weight1;*/
/*var weights=query.Select(e => e.Weight1).ToListAsync();*/
var value=run.EpiRunMonitoring.ChildrenQueryable().Execute(field.Expression);
//var weights = query.Select(e => e.Weight1).ToList().Average();
//var weights=Eval.Execute<double>("@if(query.Select(e => e.Weight1).ToList().Average()>5){ return 10.0; }else{return 1.0;}",new {query});
/*var dict = new Dictionary<string, object>();
foreach(var variable in field.Variables) {
    if (variable is NumberVariable) {
        
    }else if (variable is ReferenceVariable) {
        
        var referenceVariable = variable as ReferenceVariable;
        var collection = DB.Collection<EpiRun>();
        var query = collection.Queryable();
        var value2 = query.Select(referenceVariable.QueryExpression).ToList().Average();
        dict.Add(variable.VariableName, value2);
    }
    dict.Add(variable.VariableName, variable.V);
}*/
//var weight = Eval.Execute(field.Expression, new { value,field.Variables[1].Value });



//Console.WriteLine(string.Join(",",weights));
//Console.WriteLine($"Average Weight: {weight}");



async Task BuildDummyDatabase() {
    await DB.InitAsync("epi_system", "172.20.3.41");
    Random rand = new Random();
    for (int i = 0; i < 200; i++) {
        EpiRun run = new EpiRun();
        if (i <10) {
            run.Name = $"W00{i}";
        }else if (i < 100) {
            run.Name = $"W0{i}";
        } else {
            run.Name= $"W{i}";
        }
        await run.SaveAsync();
        List<Monitoring> monitorings = new List<Monitoring>();
        for(int x=0;x<10;x++) {
            Monitoring monitoring = new Monitoring();
            monitoring.Name = $"Monitoring{x}";
            monitoring.EpiRun = run.ToReference();
            monitoring.Weight1 = rand.NextDouble() * 800;
            monitoring.Temperature = rand.NextDouble()*100;
            monitorings.Add(monitoring);

        }
        await monitorings.InsertAsync();
        await run.EpiRunMonitoring.AddAsync(monitorings);
    }
}

async Task TestEntityBuilder() {
    await DB.InitAsync("epi_system", "172.20.3.41");

    EntityBuilder builder = new EntityBuilder();

    builder.HasObjectField(
        builder => {
            builder.WithName("MoData");
            builder.HasObjectField(
                builder => {
                    builder.WithName("Temperatures");
                    builder.HasValueField(
                        builder => {
                            builder.WithName("Temp1")
                                   .HasDefaultValue(0.00, "Celcius", "Temperature");
                        });
                    builder.HasValueField(
                        builder => {
                            builder.WithName("Temp2")
                                   .HasDefaultValue(0.00, "Celcius", "Temperature");
                        });
                    builder.HasValueField(
                        builder => {
                            builder.WithName("Temp3")
                                   .HasDefaultValue(0.00, "Celcius", "Temperature");
                        });
                });
        });

    Model model = new Model();
    model.Fields = builder.Fields;
    model.CollectionName = "Collection";

    await DB.InsertAsync(model);
}

public static class CollectionFactory {
    
}

[Collection("test")]
public class TestObj : Entity {
    public string Name { get; set; }
    public BsonDocument AdditionalData { get; set; } = new BsonDocument();
    
}

[Collection("epi_runs")]
public class EpiRun : Entity {
    public string Name { get; set; }
    public string RunNumber { get; set; }
    public Many<Monitoring,EpiRun> EpiRunMonitoring { get; set; }
    public EpiRun() {
        this.InitOneToMany(()=>this.EpiRunMonitoring);
    }
    
}

[Collection("run_monitoring")]
public class Monitoring:Entity {
    public string Name { get; set; }
    public One<EpiRun> EpiRun { get; set; }
    public double Weight1 { get; set; }
    public double Temperature { get; set; }

    public Monitoring() {
         
    }
}

public class TestEntity  {
    public ObjectId _id { get; set; }
    public string Name { get; set; }
    public BsonDocument AdditionalData { get; set; } = new BsonDocument();
}