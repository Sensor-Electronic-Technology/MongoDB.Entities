using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

public static class DataHelper {
    public static async Task GenerateEpiData() {
        var rand = new Random();
        var now = DateTime.Now;
        List<EpiRun> epiRuns = [];
        List<QuickTest> quickTests = [];
        List<XrdData> xrdMeasurementData = [];

        for (int i = 1; i <= 10; i++) {
            for (int x = 1; x <= 10; x++) {
                EpiRun run = new EpiRun {
                    RunTypeId = (rand.NextDouble() > .5) ? "Prod" : "Rnd",
                    SystemId = "B01",
                    TechnicianId = (rand.NextDouble() > .5) ? "RJ" : "NC",
                };
                run.TimeStamp = now;

                string tempId = "";
                string ledId = "";
                string rlId = "";
                GenerateWaferIds(i, "A03", "A02", "B01", ref tempId, ref rlId, ref ledId);

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

                var quickTestData = new QuickTest {
                    WaferId = run.WaferId,

                    TimeStamp = now,
                    InitialMeasurements = new List<QtMeasurement> {
                        GenerateQtMeasurement(rand, "A", now),
                        GenerateQtMeasurement(rand, "B", now),
                        GenerateQtMeasurement(rand, "C", now),
                        GenerateQtMeasurement(rand, "L", now),
                        GenerateQtMeasurement(rand, "R", now),
                        GenerateQtMeasurement(rand, "T", now),
                        GenerateQtMeasurement(rand, "G", now)
                    },
                    FinalMeasurements = new List<QtMeasurement> {
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
                var xrdData = new XrdData {
                    WaferId = run.WaferId,
                    XrdMeasurements = new List<XrdMeasurement> {
                        GenerateXrdMeasurement(rand, "C", now),
                        GenerateXrdMeasurement(rand, "Edge", now)
                    }
                };
                xrdMeasurementData.Add(xrdData);
                /*await xrdMeasurements.SaveAsync();
                await run.XrdMeasurements.AddAsync(xrdMeasurements);*/
            } //end pocked for loop
        }     //end run number for loop
        await epiRuns.SaveAsync();
        await quickTests.SaveAsync();
        await xrdMeasurementData.SaveAsync();

        epiRuns.ForEach(
            run => {
                var qt = quickTests.FirstOrDefault(e => e.WaferId == run.WaferId);
                var xrd = xrdMeasurementData.FirstOrDefault(e => e.WaferId == run.WaferId);

                if (qt != null) {
                    run.QuickTest = qt.ToReference();
                    qt.EpiRun = run.ToReference();
                }

                if (xrd != null) {
                    run.XrdData = xrd.ToReference();
                    xrd.EpiRun = run.ToReference();
                }
            });

        await epiRuns.SaveAsync();
        await quickTests.SaveAsync();
        await xrdMeasurementData.SaveAsync();
        Console.WriteLine("Check Database");
    }

    private static XrdMeasurement GenerateXrdMeasurement(Random rand, string Area, DateTime now) {
        var xrd = new XrdMeasurement {
            XrdArea = Area,
            TimeStamp = now,
            Alpha_AlN = NextDouble(rand, 35.937, 36.0211),
            Beta_AlN = NextDouble(rand, 35.9472, 36.0754),
            FHWM102 = NextDouble(rand, 180, 568.8),
            FWHM002 = NextDouble(rand, 7.2, 358.8),
            dOmega = NextDouble(rand, 0.0065, .3748),
            Omega = NextDouble(rand, 16.2183, 18.3815)
        };

        return xrd;
    }

    private static QtMeasurement GenerateQtMeasurement(Random rand, string Area, DateTime now) {
        var qt = new QtMeasurement {
            Area = Area,
            TimeStamp = now,
            Current = 20.0,
            Power = NextDouble(rand, 700, 1700),
            Voltage = NextDouble(rand, 9.5, 15.5),
            Wavelength = NextDouble(rand, 270, 279.9)
        };

        return qt;
    }

    private static void GenerateWaferIds(int i, string tempSystem, string rlSystem,
                                         string ledSystem,
                                         ref string tempId,
                                         ref string rlId,
                                         ref string ledId) {
        tempId = tempSystem;
        rlId = rlSystem;
        ledId = ledSystem;
        if (i / 1000 >= 1) {
            tempId += $"-{i}";
            rlId += $"-{i}";
            ledId += $"-{i}";
        } else if (i / 100 >= 1) {
            tempId += $"-0{i}";
            rlId += $"-0{i}";
            ledId += $"-0{i}";
        } else if (i / 10 >= 1) {
            tempId += $"-00{i}";
            rlId += $"-00{i}";
            ledId += $"-00{i}";
        } else {
            tempId += $"-000{i}";
            rlId += $"-000{i}";
            ledId += $"-000{i}";
        }
    }

    private static double NextDouble(Random rand, double min, double max)
        => rand.NextDouble() * (max - min) + min;
}