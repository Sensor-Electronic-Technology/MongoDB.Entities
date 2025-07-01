using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestCategory("DocumentTypeConfiguration"),TestClass]
public class TypeConfigurationMap {

    [TestMethod]
    public async Task test_assembly_scan() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        var keys = TypeMap.GetTypeConfigurationKeys();
        Assert.Contains(typeof(EpiRun), keys);
    }

    [TestMethod]
    public async Task test_type_configuration_watcher_created() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        var watchers=DB.Watchers<DocumentTypeConfiguration>();
        Assert.IsNotEmpty(watchers);
    }
    
    [TestMethod]
    public async Task test_type_configuration_watcher_init() {
        await InitTest.InitTestDatabase( assemblies: typeof(EpiRun).Assembly);
        Assert.IsNull(DB.TypeConfiguration<EpiRun>());
    }

    [TestMethod]
    public async Task test_type_configuration_watcher_insert() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        DocumentTypeConfiguration? config = DocumentTypeConfiguration.CreateOnline<XrdData>();
        Assert.IsNotNull(config);
        await config.SaveAsync();
        await Task.Delay(500);
        Assert.AreEqual(config.ID, DB.TypeConfiguration<XrdData>().ID);
    }


    [TestMethod]
    public async Task test_type_configuration_watcher_update() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        var typeConfig = await DB.Find<DocumentTypeConfiguration>()
                                 .Match(x => x.CollectionName == DB.CollectionName<XrdData>())
                                 .ExecuteSingleAsync();
        Assert.IsNotNull(typeConfig);
        typeConfig.Fields.Add(new ValueField() {
            FieldName = "Field1",
            BsonType = BsonType.Double,
            TypeCode = TypeCode.Double,
            DefaultValue = 1.00
        });
        await typeConfig.SaveAsync();
        await Task.Delay(500);
        var internalConfig=DB.TypeConfiguration<XrdData>();
        Assert.IsNotNull(internalConfig);
        Assert.IsNotEmpty(internalConfig.Fields);
        Assert.AreEqual(typeConfig.ID, typeConfig.ID);
        Assert.AreEqual(typeConfig.Fields[0].FieldName, internalConfig.Fields[0].FieldName);
    }
    
    [TestMethod]
    public async Task test_type_configuration_watcher_delete() {
        await InitTest.InitTestDatabase(assemblies: typeof(EpiRun).Assembly);
        var typeConfig = await DB.Find<DocumentTypeConfiguration>()
                                 .Match(x => x.CollectionName == DB.CollectionName<XrdData>())
                                 .ExecuteSingleAsync();
        Assert.IsNotNull(typeConfig);
        await typeConfig.DeleteAsync();
        
        typeConfig = await DB.Collection<DocumentTypeConfiguration>().Find(e=>e.ID==typeConfig.ID).FirstOrDefaultAsync();
        Assert.IsNull(typeConfig);
        await Task.Delay(500); 
        Assert.IsNull(DB.TypeConfiguration<XrdData>());
    }
    
    
}