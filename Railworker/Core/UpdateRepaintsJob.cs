using Microsoft.WindowsAPICodePack.Dialogs;
using RWLib;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Railworker.Windows
{
    public partial class RepaintUpdater
    {
        class UpdateRepaintsJob
        {
            private readonly List<IRWRailVehicleBlueprint> blueprints;
            private readonly KeepOrUpdateSettings csvNumberingSettings = new KeepOrUpdateSettings();
            private readonly Dictionary<string, KeepOrUpdateSettings> drivingCharacterisitcs = new Dictionary<string, KeepOrUpdateSettings>();
            private readonly KeepOrUpdateSettings driverPosition = new KeepOrUpdateSettings();
            private readonly RWLibrary lib;
            private readonly RepaintUpdaterPrompt prompt;
            private readonly IRWRailVehicleBlueprint templateBlueprint;

            public bool IsFinished { get; private set; }

            XDocument? newRepaintXDocument;
            IRWRailVehicleBlueprint? current;

            List<string>? drivingCharacateristics;
            int currentDcIdx = 0;
            XElement? newRailVehicleComponent;
            XElement? originalRailVehicleComponent;
            XElement? blueprintElement;


            public UpdateRepaintsJob(RWLibrary lib, RepaintUpdaterPrompt prompt, IRWRailVehicleBlueprint templateBlueprint, List<IRWRailVehicleBlueprint> blueprints)
            {
                this.lib = lib;
                this.prompt = prompt;
                this.templateBlueprint = templateBlueprint;
                this.blueprints = blueprints.Slice(0, blueprints.Count);

                Next();
            }

            void Prompt()
            {

            }

            void Finish()
            {
                IsFinished = true;
            }

            void Next()
            {
                if (blueprints.Count == 0)
                {
                    Finish();
                    return;
                }
                current = blueprints[blueprints.Count - 1];
                blueprints.RemoveAt(blueprints.Count - 1);

                Step1();
            }


            private XElement? GetCSVNumbering(XElement root)
            {
                return root.Descendants("RailVehicleComponent").First().Element("cEngineComponentBlueprint")?.Element("NumberingList")
                    ?.Element("cCSVContainer")?.Element("CsvFile");
            }


            void Step1()
            {
                var decleration = new XDeclaration("1.0", "utf-8", null);
                newRepaintXDocument = new XDocument(decleration);
                var newRepaitnXDocumentRoot = new XElement("cBlueprintLoader");
                blueprintElement = new XElement("Blueprint");
                newRepaitnXDocumentRoot.Add(blueprintElement);
                newRepaintXDocument.Add(newRepaitnXDocumentRoot);
                newRepaitnXDocumentRoot.Add(new XAttribute(XNamespace.Xmlns + "d", RWUtils.KujuNamspace));
                newRepaitnXDocumentRoot.Add(new XAttribute(RWUtils.KujuNamspace + "version", "1.0"));

                blueprintElement.Add(templateBlueprint.Xml);

                // replace the <BrowseInformation />
                var browseInformation = newRepaintXDocument.Descendants("BrowseInformation").First();
                browseInformation.ReplaceWith(current!.Xml.Descendants("BrowseInformation").First());

                var newRepaintCSVFile = GetCSVNumbering(newRepaintXDocument.Root!);
                var originalCSVFile = GetCSVNumbering(current.Xml);

                if (newRepaintCSVFile != null && originalCSVFile != null && newRepaintCSVFile.Value != originalCSVFile.Value)
                {
                    var question = Railworker.Language.Resources.keep_numbering_list + " (" + current.BlueprintId.ToString() + ")";

                    ShouldKeep(question, csvNumberingSettings, result =>
                    {
                        if (result)
                        {
                            newRepaintCSVFile.Value = originalCSVFile.Value;
                        }
                        Step2();
                    });
                } else
                {
                    Step2();
                }
            }

            void Step2()
            {
                drivingCharacateristics = new List<string> { "Mass", "EaseOfDerailment", "DragCoefficient", "RollingFrictionCoefficient", "DryFriction", "WetFriction", "SnowFriction", "SandFrictionMultiplier" };
                newRailVehicleComponent = newRepaintXDocument!.Descendants("RailVehicleComponent").First();
                originalRailVehicleComponent = current!.Xml.Descendants("RailVehicleComponent").First();

                currentDcIdx = 0;
                Step2RailVehicleComponent();
            }

            void Step2RailVehicleComponent()
            {
                if (currentDcIdx >= drivingCharacateristics!.Count)
                {
                    Step3();
                    return;
                }
                var currentDc = drivingCharacateristics[currentDcIdx];
                var newDC = newRailVehicleComponent!.Descendants(currentDc).First();
                var originalDC = originalRailVehicleComponent!.Descendants(currentDc).First();

                if (newDC != null && originalDC != null && newDC.Value != originalDC.Value)
                {
                    var question = String.Format(Railworker.Language.Resources.driving_characteristics_mismatch, currentDc) + " (" + current.BlueprintId.ToString() + ")";
                    var settings = drivingCharacterisitcs.GetValueOrDefault(currentDc, new KeepOrUpdateSettings());
                    drivingCharacterisitcs[currentDc] = settings;

                    ShouldKeep(question, settings, result =>
                    {
                        if (result)
                        {
                            newDC.Value = originalDC.Value;
                        }

                        currentDcIdx++;
                        Step2RailVehicleComponent();
                    });
                } else
                {
                    currentDcIdx++;
                    Step2RailVehicleComponent();
                }
            }

            void Step3()
            {
                // always copy remapper
                var newRemapper = newRailVehicleComponent!.Descendants("StopgoRemapper").FirstOrDefault();
                var originalRemapper = originalRailVehicleComponent!.Descendants("StopgoRemapper").FirstOrDefault();

                if (newRemapper != null && originalRemapper != null && originalRemapper.Value != newRemapper.Value)
                {
                    newRemapper.Value = originalRemapper.Value;
                }

                var newIntermediateRemapper = newRailVehicleComponent.Descendants("IntermediateRemapper").FirstOrDefault();
                var originalIntermediateRemapper = originalRailVehicleComponent.Descendants("IntermediateRemapper").FirstOrDefault();

                if (newIntermediateRemapper != null && originalIntermediateRemapper != null && originalIntermediateRemapper.Value != newIntermediateRemapper.Value)
                {
                    newIntermediateRemapper.Value = originalIntermediateRemapper.Value;
                }

                var newExpertRemapper = newRailVehicleComponent.Descendants("ExpertRemapper").FirstOrDefault();
                var originalExpertRemapper = originalRailVehicleComponent.Descendants("ExpertRemapper").FirstOrDefault();

                if (newExpertRemapper != null && originalExpertRemapper != null && originalExpertRemapper.Value != newExpertRemapper.Value)
                {
                    newExpertRemapper.Value = originalExpertRemapper.Value;
                }

                var newDriverPosition = newRailVehicleComponent.Descendants("DriverPosition").FirstOrDefault();
                var oldDriverPosition = originalRailVehicleComponent.Descendants("DriverPosition").FirstOrDefault();

                if (newDriverPosition != null && oldDriverPosition != null && newDriverPosition.ToString() != oldDriverPosition.ToString())
                {
                    var question = Railworker.Language.Resources.keep_driver_position;

                    ShouldKeep(question, driverPosition, result =>
                    {
                        if (result)
                        {
                            newDriverPosition.Value = oldDriverPosition.Value;
                        }
                        Step4();
                    });
                } else
                {
                    Step4();
                }
            }

            async void Step4()
            {
                var newRenderComponent = newRepaintXDocument!.Descendants("RenderComponent").First();
                var originalRenderComponent = current!.Xml.Descendants("RenderComponent").First();


                // always copy geo
                var newGeoID = newRenderComponent.Descendants("GeometryID").FirstOrDefault();
                var originalGeoID = originalRenderComponent.Descendants("GeometryID").FirstOrDefault();

                if (newGeoID != null && originalGeoID != null)
                {
                    newGeoID.ReplaceWith(originalGeoID);
                }

                var newColGeoID = newRenderComponent.Descendants("CollisionGeometryID").FirstOrDefault();
                var originalColGeoID = originalRenderComponent.Descendants("CollisionGeometryID").FirstOrDefault();

                if (newColGeoID != null && originalColGeoID != null)
                {
                    newColGeoID.ReplaceWith(originalColGeoID);
                }

                CommonSaveFileDialog dialog = new CommonSaveFileDialog();

                dialog.Filters.Add(new CommonFileDialogFilter("RW bin", "*.bin"));
                dialog.Filters.Add(new CommonFileDialogFilter("RW xml", "*.xml"));

                var result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    var path = dialog.FileName;
                    if (path != null)
                    {
                        if (Path.GetExtension(path) == ".xml")
                        {
                            newRepaintXDocument.Save(path);
                        } else
                        {
                            var filename = await lib.Serializer.SerializeWithSerzExe(newRepaintXDocument);
                            File.Copy(filename, path);
                        }
                    }
                }

                Next();
            }

            private void ShouldKeep(string question, KeepOrUpdateSettings settings, Action<bool> callback)
            {
                if (!settings.AlwaysKeep && !settings.AlwaysUpdate)
                {
                    prompt!.Prompt(question, result =>
                    {
                        if (result.DoForAll)
                        {
                            settings.AlwaysKeep = result.Keep;
                            settings.AlwaysUpdate = !result.Keep;
                        }
                        callback(result.Keep);
                    });
                }
                else
                {
                    callback(settings.AlwaysKeep);
                }
            }
        }
    }
}
