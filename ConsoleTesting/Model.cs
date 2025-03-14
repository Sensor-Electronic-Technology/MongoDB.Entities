using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Entities;

namespace ConsoleTesting;



[Collection("epi_runs")]
public class EpiRun : Entity,ICreatedOn {
    public DateTime TimeStamp { get; set; }
    public string WaferId { get; set; }
    public string RunTypeId { get; set; }
    public string SystemId { get; set; }
    public string TechnicianId { get; set; }
    public string RunNumber { get; set; }
    public string PocketNumber { get; set; }
    public Many<Monitoring,EpiRun> EpiRunMonitoring { get; set; }
    
    public One<QuickTest> QuickTest { get; set; }
    public One<XrdData> XrdData { get; set; }
    
    public EpiRun() {
        this.InitOneToMany(()=>this.EpiRunMonitoring);
    }

    public DateTime CreatedOn { get; set; }
}

[Collection("quick_tests")]
public class QuickTest:Entity,ICreatedOn {
    public string WaferId { get; set; }
    public DateTime TimeStamp { get; set; }
    public One<EpiRun> EpiRun { get; set; }
    
    public ICollection<QtMeasurement> InitialMeasurements { get; set; } = new ObservableCollection<QtMeasurement>();
    public ICollection<QtMeasurement> FinalMeasurements { get; set; } = new ObservableCollection<QtMeasurement>();
    public QuickTest() {
        DB.Index<QuickTest>()
          .Key(e=>e.WaferId,KeyType.Descending)
          .Option(
              o => {
                  o.Unique = true;
              });
    }

    public DateTime CreatedOn { get; set; }
}

[Collection("xrd_data")]
public class XrdData : Entity,ICreatedOn {
    public One<EpiRun> EpiRun { get; set; }
    public string WaferId { get; set; }
    public ICollection<XrdMeasurement> XrdMeasurements { get; set; }
    public XrdData() {
        DB.Index<XrdData>()
          .Key(e=>e.WaferId,KeyType.Descending)
          .Option(
              o => {
                  o.Unique = true;
              });
    }

    public DateTime CreatedOn { get; set; }
}

public class QtMeasurement {
    public DateTime TimeStamp { get; set; }
    public string Area { get; set; }
    public double Power { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Wavelength { get; set; }
}


public class XrdMeasurement {
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


}

[Collection("run_monitoring")]
public class Monitoring:Entity {
    public One<EpiRun> EpiRun { get; set; }
    public string WaferId { get; set; }
    public double Weight1 { get; set; }
    public double Temperature { get; set; }

    public Monitoring() {
         DB.Index<Monitoring>()
           .Key(e=>e.WaferId,KeyType.Descending)
           .Option(
               o => {
                   o.Unique = true;
               });
    }
}

public class TestEntity  {
    public ObjectId _id { get; set; }
    public string Name { get; set; }
    public BsonDocument AdditionalData { get; set; } = new BsonDocument();
}