using RWLib.Exceptions;
using RWLib.Interfaces;
using RWLib.Tracks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RWLib
{
    public class RWTracksBinParser : RWLibraryDependent
    {
        public RWRouteLoader rwRouteLoader;

        public double L0Tangent1 { get; private set; }
        public double L0Tangent2 { get; private set; }

        internal RWTracksBinParser(RWLibrary rWLib, RWRouteLoader rwRouteLoader) : base(rWLib)
        {
            this.rwRouteLoader = rwRouteLoader;
        }

        public async IAsyncEnumerable<TileTrackRibbon> ProcessTrackTile(TracksBinTile tile)
        {
            //var routeCoordX = tile.RouteCoordX;
            //var routeCoordY = tile.RouteCoordZ;

            var tracksBinFile = tile.TracksBinFile;
            if (tile.ZipArchiveEntry == null)
            {
                tracksBinFile = tile.TracksBinFile;
            } else
            {
                tracksBinFile = rwRouteLoader.serializer.ExtractToTemporaryFile(tile.ZipArchiveEntry);
            }
            
            var tracksBin = await rwRouteLoader.serializer.DeserializeWithSerzExe(tracksBinFile);

            var recordSet = tracksBin.Element("cRecordSet")?.Element("Record")!;

            var ribbons = RWUtils.CatchExceptions(recordSet.Elements("Network-cNetworkRibbonUnstreamed-cCurveContainerUnstreamed"), (Action<Exception>)((Exception ex) =>
            {
                this.rWLib.options.Logger?.Log(RWLogType.Warning, $"Failed to load a record while parsing {tracksBinFile}: " + ex.Message);
            }));

            foreach (var ribbon in ribbons)
            {
                string? ribbonId = ribbon.Element("RibbonID")?.Element("Network-cNetworkRibbon-cCurveContainer-cID")?.Element("RibbonID")?.Element("cGUID")?.Element("DevString")?.Value;
                string? networkTypeId = ribbon.Element("RibbonID")?.Element("Network-cNetworkRibbon-cCurveContainer-cID")?.Element("NetworkTypeID")?.Element("cGUID")?.Element("DevString")?.Value;

                if (ribbonId == null) throw new TrackTileRibbonIdIsNull("RibbonID is null");
                if (networkTypeId == null) throw new TrackTileNetworkTypeIdIsNull("NetworkTypeId is null");

                var curves = RWUtils.CatchExceptions(ribbon.Element("Curve")?.Elements() ?? new List<XElement>(), (Action<Exception>)((Exception ex) =>
                {
                    this.rWLib.options.Logger?.Log(RWLogType.Warning, $"Failed to load curves while parsing {tracksBinFile}: " + ex.Message);
                }));

                List<TrackCurve> trackCurves = new List<TrackCurve>(2);

                foreach (var curve in curves)
                {
                    var nodeName = curve.Name.ToString();

                    double? length = (double?)curve.Element("Length");
                    if (length == null) throw new TrackCurveLengthIsNull("Track curve length is null");

                    (double tangentX, double tangentY) = GetStartTangent(curve);
                    int? id = (int?)curve.Attribute(RWUtils.KujuNamspace + "id");
                    if (id == null) throw new TrackCurveIdDoesNotExist("Track curve id is null");

                    RWRouteCoord routeCoord = GetPosition(curve.Element("StartPos")?.Element("cFarVector2"));

                    TrackCurve trackCurve;

                    switch (nodeName)
                    {
                        case "cCurveStraight":
                            trackCurve = new CurveStraight
                            {
                                Id = (int)id,
                                Length = (double)length,
                                Tangent1 = tangentX,
                                Tangent2 = tangentY,
                                Position = routeCoord
                            };
                            break;
                        case "cCurveArc":
                            double curvature = (double)curve.Element("Curvature")!;
                            int sign = (int)curve.Element("CurveSign")!;

                            trackCurve = new CurveArc
                            {
                                Id = (int)id,
                                Length = (double)length,
                                Tangent1 = tangentX,
                                Tangent2 = tangentY,
                                Position = routeCoord,
                                Curvature = curvature,
                                Sign = sign
                            };
                            break;
                        case "cCurveEasement":
                            int sign2 = (int)curve.Element("CurveSign")!;
                            double sharpness = (double)curve.Element("Sharpness")!;
                            double offset = (double)curve.Element("Offset")!;
                            bool offsetIsZero = (bool)curve.Element("OffsetIsZero")!;
                            double l0 = (double)curve.Element("L0")!;
                            int traversalSign = (int)curve.Element("TraversalSign")!;
                            var l0OffsetParts = curve.Element("L0Offset")?.Value.Split(' ')!;
                            var l0TangentParts = curve.Element("L0Tangent")?.Value.Split(' ')!;

                            trackCurve = new CurveEasement
                            {
                                Id = (int)id,
                                Length = (double)length,
                                Tangent1 = tangentX,
                                Tangent2 = tangentY,
                                Position = routeCoord,
                                Sign = sign2,
                                Sharpness = sharpness,

                                Offset = offset,
                                OffsetIsZero = offsetIsZero,
                                L0 = l0,
                                TraversalSign = traversalSign,
                                L0Offset = (double.Parse(l0OffsetParts[0], CultureInfo.InvariantCulture), double.Parse(l0OffsetParts[1], CultureInfo.InvariantCulture), double.Parse(l0OffsetParts[2], CultureInfo.InvariantCulture), double.Parse(l0OffsetParts[3], CultureInfo.InvariantCulture)),
                                L0Tangent1 = double.Parse(l0TangentParts[0], CultureInfo.InvariantCulture),
                                L0Tangent2 = double.Parse(l0TangentParts[1], CultureInfo.InvariantCulture)
                            };

                            break;
                        default:
                            throw new UknownCurveTypeException("Uknown curve type: " + nodeName);
                    }

                    trackCurves.Add(trackCurve);
                }

                yield return new TileTrackRibbon
                {
                    Curves = trackCurves,
                    RibbonID = ribbonId,
                    NetworkTypeId = networkTypeId,
                    RouteGuid = tile.RouteGuid
                };
            }
        }

        public IEnumerable<TracksBinTile> GetTrackTiles(string routeGuid)
        {
            var routeDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", routeGuid);
            var trackTilesDir = Path.Combine(routeDir, "Networks", "Track Tiles");

            var trackTiles = rwRouteLoader.ListFiles(trackTilesDir);

            foreach (var trackTile in trackTiles)
            {
                var reg = new Regex("([+-][0-9]+)([-+][0-9]+)\\.bin");
                var matchResult = reg.Match(trackTile.Name!);
                var routeCoordX = int.Parse(matchResult.Groups[1].Value);
                var routeCoordY = int.Parse(matchResult.Groups[2].Value);

                var tracksBinFile = Path.Combine(trackTilesDir, trackTile.Name!);

                yield return new TracksBinTile
                {
                    RouteCoordX = routeCoordX,
                    RouteCoordZ = routeCoordY,
                    TracksBinFile = tracksBinFile,
                    ZipArchiveEntry = trackTile.ZipArchiveEntry,
                    RouteGuid = routeGuid
                };
            }
        }

        private RWRouteCoord GetPosition(XElement? farVector)
        {
            if (farVector == null)
            {
                throw new FailedToRetrieveCurvePosition("Curve is null");
            }

            var xRouteCoord = (int)farVector.Element("X")?.Element("cFarCoordinate")?.Element("RouteCoordinate")?.Element("cRouteCoordinate")?.Element("Distance")!;
            var zRouteCoord = (int)farVector.Element("Z")?.Element("cFarCoordinate")?.Element("RouteCoordinate")?.Element("cRouteCoordinate")?.Element("Distance")!;

            var xTileCoord = (double)farVector.Element("X")?.Element("cFarCoordinate")?.Element("TileCoordinate")?.Element("cTileCoordinate")?.Element("Distance")!;
            var zTileCoord = (double)farVector.Element("Z")?.Element("cFarCoordinate")?.Element("TileCoordinate")?.Element("cTileCoordinate")?.Element("Distance")!;

            return new RWRouteCoord
            {
                XRouteCoord = xRouteCoord,
                ZRouteCoord = zRouteCoord,

                XTileCoord = xTileCoord,
                ZTileCoord = zTileCoord
            };
        }

        private (double, double) GetStartTangent(XElement curve)
        {
            var startTangent = curve.Element("StartTangent");
            var containsElements = startTangent?.Elements().Count() > 0;

            if (startTangent == null)
            {
                rWLib.options.Logger?.Log(RWLogType.Warning, "Unable to parse cHcRVector2. Missing tangents");
                throw new FailedToRetrieveTangentPairs("Unable to parse cHcRVector2. Missing tangents");
            }

            if (containsElements)
            {
                var tang_e1 = startTangent?.Element("cHcRVector2")?.Element("Element")?.Element("e");
                var tang_e2 = tang_e1?.ElementsAfterSelf().FirstOrDefault();

                if (tang_e1 == null || tang_e2 == null)
                {
                    rWLib.options.Logger?.Log(RWLogType.Warning, "Unable to parse cHcRVector2. Missing tangents");
                    throw new FailedToRetrieveTangentPairs("Unable to parse cHcRVector2. Missing tangents");
                }
                else
                {
                    return ((double)tang_e1!, (double)tang_e2!);
                }
            } else
            {
                var split = startTangent.Value.Split(' ');
                return (double.Parse(split[0], CultureInfo.InvariantCulture), double.Parse(split[1], CultureInfo.InvariantCulture));
            }
        }

        public async IAsyncEnumerable<string> ProcessMainTracksBin(string routeGuid)
        {
            var routeDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", routeGuid);
            var tracksBinFile = Path.Combine(routeDir, "Networks", "Tracks.bin");

            var tracksBinFilePath = rwRouteLoader.OpenFile(tracksBinFile, true);
            var tracksBin = await rwRouteLoader.serializer.DeserializeWithSerzExe(tracksBinFilePath.FileName!);

            var ribbonsWithEx = tracksBin.Element("cRecordSet")?.Element("Record")?.Element("Network-cTrackNetwork")?.Element("RibbonContainer")?.Element("Network-cRibbonContainerUnstreamed")?.Element("Ribbon");

            if (ribbonsWithEx == null) { yield break; }

            int count = 0;

            var ribbons = RWUtils.CatchExceptions(ribbonsWithEx.Elements("Network-cTrackRibbon"), (Action<Exception>)((Exception ex) =>
            {
                this.rWLib.options.Logger?.Log(RWLogType.Warning, $"Failed to load a track ribbon while parsing {tracksBinFile}: " + ex.Message);
            }));

            foreach (var ribbonXML in ribbons)
            {
                count++;
                var ribbon = new Ribbon
                {
                    ID = ribbonXML?.Element("RibbonID")?.Element("cGUID")?.Element("DevString")?.Value?.ToString() ?? "",
                    Length = (double)ribbonXML?.Element("_length")!,
                    LockCounterWhenModified = (bool)ribbonXML?.Element("LockCounterWhenModified")!,
                    IsSuperElevated = (bool)ribbonXML?.Element("Superelevated")!
                };

                foreach (var heightPoint in ribbonXML?.Element("Height")?.Elements("Network-iRibbon-cHeight") ?? new List<XElement>())
                {
                    ribbon.HeightPoints.Add(new HeightPoint
                    {
                        Position = (double)heightPoint?.Element("_position")!,
                        Height = (double)heightPoint?.Element("_height")!,
                        Manual = (bool)heightPoint?.Element("_manual")!
                    });
                }

                yield return ribbon.ID + " " + ribbon.HeightPoints.Count;
            }

            var nodesWithEx = tracksBin.Element("cRecordSet")?.Element("Record")?.Element("Network-cTrackNetwork")?.Element("RibbonContainer")?.Element("Network-cRibbonContainerUnstreamed")?.Element("Node");

            if (nodesWithEx == null) { yield break; }

            int nodesCount = 0;

            var nodes = RWUtils.CatchExceptions(nodesWithEx.Elements("Network-cTrackNode"), (Action<Exception>)((Exception ex) =>
            {
                this.rWLib.options.Logger?.Log(RWLogType.Warning, $"Failed to load a track node while parsing {tracksBinFile}: " + ex.Message);
            }));

            foreach (var nodeXML in nodes)
            {
                nodesCount++;

                var connections = nodeXML.Element("Connection")?.Elements("Network-cNetworkNode-sRConnection") ?? new List<XElement>();

                var fixPat = nodeXML.Element("FixedPatternRef")?.Element("Network-cNetworkNode-sFixedPatternRef")?.Element("FixedPatternNodeIndex");

                // route vector (unused)

                var patternRefDescendents = nodeXML.Element("PatternRef")?.Element("Network-cTrackNode-sPatternRef")?.Elements() ?? new List<XElement>();

                int patternRefIndex = 0;

                foreach (var patternRefDescendentXML in patternRefDescendents)
                {
                    Pattern? pattern;
                    int nodeIndex = 0;
                    int? id = (int?)patternRefDescendentXML?.Attribute(RWUtils.KujuNamspace + "id")!;
                    String nodeName = patternRefDescendentXML?.Name?.ToString() ?? "";

                    if (nodeName == "d:nil")
                    {

                    }
                    else if (nodeName == "PatternNodeIndex")
                    {
                        int? result = (int?)patternRefDescendentXML;
                        if (result != null) nodeIndex = (int)result;
                    }
                    else if (nodeName.StartsWith("Network-c") &&
                            nodeName.EndsWith("Pattern"))
                    {
                        bool? manual = (bool?)patternRefDescendentXML?.Element("Manual");
                        string? state = (string?)patternRefDescendentXML?.Element("State");
                        double? transitionTime = (double?)patternRefDescendentXML?.Element("TransitionTime")?.Element("cNormFloat")?.Element("Position");
                        string? previousState = patternRefDescendentXML?.Element("TransitionTime")?.Element("cNormFloat")?.Element("PreviousState")?.ToString();

                        if (nodeName == "Network-cTurnoutPattern")
                        {
                            pattern = new TurnoutPattern();
                        }
                        else if (nodeName == "Network-c3WayPattern")
                        {
                            pattern = new ThreeWayPattern();
                        }
                        else if (nodeName == "Network-cSingleSlipPattern")
                        {
                            pattern = new SingleSlipPattern();
                        }

                        else if (nodeName == "Network-cDoubleSlipPattern")
                        {
                            pattern = new DoubleSlipPattern();
                        }
                        else if (nodeName == "Network-cCrossingPattern")
                        {
                            pattern = new CrossingPattern();
                        }
                        else
                        {
                            rWLib.options.Logger?.Log(RWLogType.Warning, "Unexpected track type: " + nodeName);
                        }

                        yield return "Node " + nodeName;
                    }

                    patternRefIndex++;
                }
            }

            yield return $"Done {count}";
        }
    }
}
