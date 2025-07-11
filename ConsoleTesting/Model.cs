﻿using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace ConsoleTesting;

public class TemplateRun : IDocumentEntity, ICreatedOn, IModifiedOn {
    
    [BsonId]
    public string WaferId { get; set; }
    
    public object GenerateNewID() => throw new NotImplementedException();

    public bool HasDefaultID() => false;
    
    public BsonDocument AdditionalData { get; set; }
    public DocumentVersion Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    
    public void UpdateEmbedded(IDocumentEntity entity) { }
}

public class TestEmbeddedNotArray : IEmbeddedEntity {
    public string? Name { get; set; }
    public BsonDocument? AdditionalData { get; set; }
    public async Task Migrate(Type parent)
        => throw new NotImplementedException();
}

[Collection("epi_runs")]
public class EpiRun : DocumentEntity,ICreatedOn,IModifiedOn {
    public DateTime TimeStamp { get; set; }
    public string WaferId { get; set; }
    public string RunTypeId { get; set; }
    public string SystemId { get; set; }
    public string TechnicianId { get; set; }
    public string RunNumber { get; set; }
    public string PocketNumber { get; set; }
    public TestEmbeddedNotArray? TestEmbeddedNotArray { get; set; }
    public Many<Monitoring,EpiRun> EpiRunMonitoring { get; set; }
    public One<QuickTest> QuickTest { get; set; }
    public One<XrdData> XrdData { get; set; }
    
    public EpiRun() {
        this.InitOneToMany(()=>EpiRunMonitoring);
    }

    static EpiRun() {
        DB.Index<EpiRun>()
          .Key(e => e.WaferId, KeyType.Descending)
          .Option(o => o.Unique = true)
          .CreateAsync()
          .Wait();
        DB.Index<EpiRun>()
          .Key(e=>e.RunNumber,KeyType.Descending)
          .Option(o=>o.Unique = false)
          .CreateAsync()
          .Wait();
        DB.Index<EpiRun>()
          .Key(e=>e.SystemId,KeyType.Descending)
          .Option(o=>o.Unique = false)
          .CreateAsync()
          .Wait();
        
        DB.Index<EpiRun>()
          .Key(e=>e.PocketNumber,KeyType.Descending)
          .Option(o=>o.Unique = false)
          .CreateAsync()
          .Wait();
    }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}

[Collection("quick_tests")]
public class QuickTest:DocumentEntity,ICreatedOn,IModifiedOn,IHasEmbedded {
    public string WaferId { get; set; }
    public DateTime TimeStamp { get; set; }
    public One<EpiRun> EpiRun { get; set; }
    public List<QtMeasurement> InitialMeasurements { get; set; } = [];
    public List<QtMeasurement> FinalMeasurements { get; set; } = [];
    
    static QuickTest() {
        DB.Index<QuickTest>()
          .Key(e=>e.WaferId,KeyType.Descending)
          .Option(o=>o.Unique=true)
          .CreateAsync().Wait();
    }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public void UpdateEmbedded(IDocumentEntity entity) {
        this.FinalMeasurements=((QuickTest)entity).FinalMeasurements;
        this.InitialMeasurements=((QuickTest)entity).InitialMeasurements;
    }

    public async Task ApplyEmbeddedMigrations() {
        await this.FinalMeasurements.ApplyEmbedded(typeof(QuickTest));
        await this.InitialMeasurements.ApplyEmbedded(typeof(QuickTest));   
    }
}

[Collection("xrd_data")]
public class XrdData : DocumentEntity,ICreatedOn,IModifiedOn {
    public One<EpiRun> EpiRun { get; set; }
    public string WaferId { get; set; }
    public ICollection<XrdMeasurement> XrdMeasurements { get; set; }

    static XrdData() {
        DB.Index<XrdData>()
          .Key(e=>e.WaferId,KeyType.Descending)
          .Option(o=>o.Unique=true)
          .CreateAsync().Wait();    
    }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}

public class QtMeasurement:IEmbeddedEntity {
    public DateTime TimeStamp { get; set; }
    public string Area { get; set; }
    public double Power { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Wavelength { get; set; }
    public BsonDocument? AdditionalData { get; set; }
    public Task Migrate(Type parent)
        => this.ApplyEmbedded(parent);
}

public class XrdMeasurement:IEmbeddedEntity {
    public DateTime TimeStamp { get; set; }
    public string XrdArea { get; set; }
    public double pGan { get; set; }
    public double AlGaNP1 { get; set; }
    public double AlGaNP0 { get; set; }
    public double Alpha_AlN { get; set; }
    public double Beta_AlN { get; set; }
    public double FWHM002 { get; set; }
    public double Omega { get; set; }
    public double dOmega { get; set; }
    public double FHWM102 { get; set; }

    public BsonDocument? AdditionalData { get; set; }
    public async Task Migrate(Type parent)
        => throw new NotImplementedException();
}

[Collection("run_monitoring")]
public class Monitoring:DocumentEntity,ICreatedOn,IModifiedOn {
    public One<EpiRun> EpiRun { get; set; }
    public string WaferId { get; set; }
    public double Weight1 { get; set; }
    public double Temperature { get; set; }
    static Monitoring() {
        DB.Index<Monitoring>()
          .Key(e => e.WaferId, KeyType.Descending)
          .Option(o => o.Unique = true)
          .CreateAsync().Wait();
    }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}