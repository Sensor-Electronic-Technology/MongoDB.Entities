using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Benchmark;

public abstract class BenchBase
{
    const string DBName = "mongodb-entities-benchmark";
    protected static IMongoCollection<Author> AuthorCollection { get; }
    protected static IMongoCollection<Book> BookCollection { get; }
    protected static IMongoDatabase Database { get; }

    static BenchBase()
    {
        DB.InitAsync(DBName,host:"172.20.3.41").GetAwaiter().GetResult();
        DB.Database(DBName).Client.DropDatabase(DBName);
        Database = DB.Database(default);
        AuthorCollection = DB.Collection<Author>();
        BookCollection = DB.Collection<Book>();

        Console.WriteLine();
        Console.WriteLine("SEEDING DATA...");
    }

    public abstract Task MongoDB_Entities();

    public abstract Task Official_Driver();
}
