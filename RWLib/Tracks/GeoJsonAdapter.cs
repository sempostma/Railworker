using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Tracks
{
    public class GeoJsonAdapter
    {
        internal RWRouteOrigin origin;
        internal GeoJsonAdapterOptions options;
        private WSG84MercatorProjectionConverter projectionConverter;

        internal string FeatureCollectionHeader { get => "{" + options.NewLine + options.Indentation + "\"type\":" + options.Space + "\"FeatureCollection\"," + options.NewLine + options.Indentation + "\"features\":" + options.Space + "[" + options.NewLine; }

        internal string FeatureCollectionFooter { get => options.Indentation + "]" + options.NewLine + "}" + options.NewLine; }

        public class FeatureCollection
        {
            [JsonPropertyName("features")]
            public List<FeatureCollection> Features = new List<FeatureCollection>();
        }

        public class Geometry
        {
            [JsonPropertyName("type")]
            public required string Type { get; set; }
            [JsonPropertyName("coordinates")]
            public required List<double[]> Coordinates { get; set; }
        }

        public class Feature
        {
            [JsonPropertyName("type")]
            public string Type { get => "Feature"; }
            [JsonPropertyName("geometry")]
            public required Geometry Geometry { get; set; }
            [JsonPropertyName("properties")]
            public required Properties Properties { get; set; }
        }

        public class Properties
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        public GeoJsonAdapter(RWRouteOrigin origin)
        {
            this.origin = origin;
            this.options = new GeoJsonAdapterOptions();
            this.CreateProjectionConverter();
        }

        public GeoJsonAdapter(RWRouteOrigin origin, GeoJsonAdapterOptions options) {
            this.origin = origin;
            this.options = options;
            this.CreateProjectionConverter();
        }

        private void CreateProjectionConverter()
        {
            projectionConverter = new WSG84MercatorProjectionConverter(origin.Lat, origin.Long, origin.ZoneNumber);
        }

        public IEnumerable<Feature> ProcessRWTrackRibbon(TileTrackRibbon ribbon)
        {
            foreach (var curve in ribbon.Curves)
            {
                var startX = curve.Position.X;
                var startZ = curve.Position.Z;

                var angle = curve.Atan2; // radians
                var endX = startX + Math.Cos(angle) * curve.Length;
                var endZ = startZ + Math.Sin(angle) * curve.Length;

                switch (curve)
                {
                    case CurveStraight curveStraight:
                        {
                            (double latitude1, double longitude1) = projectionConverter.ConvertToLatitudeAndLongitude(startX, startZ);
                            (double latitude2, double longitude2) = projectionConverter.ConvertToLatitudeAndLongitude(endX, endZ);

                            yield return new Feature
                            {
                                Geometry = new Geometry
                                {
                                    Type = "LineString",
                                    Coordinates = new List<double[]>()
                                    {
                                        new double[2] { longitude1, latitude1 },
                                        new double[2] { longitude2, latitude2 }
                                    }
                                },
                                Properties = new Properties
                                {
                                    Id = curveStraight.Id.ToString(),
                                    Name = nameof(CurveStraight)
                                }
                            };
                            break;
                        }

                    case CurveArc curveArc:
                        {
                            var endPosition = curveArc.GetEndPosition();

                            var rad = curveArc.Radius.ToString(CultureInfo.InvariantCulture);
                            var circle = curveArc.GetReferenceCircleCenter();
                            var cx = circle.X.ToString(CultureInfo.InvariantCulture);
                            var cy = circle.Z.ToString(CultureInfo.InvariantCulture);

                            var arrayLength = (int)Math.Floor(curveArc.Length / options.Granularity) + 1;
                            if (arrayLength < 2) arrayLength = 2;
                            var coordinates = new List<double[]>(arrayLength);

                            for (int i = 0; i < arrayLength - 1; i++)
                            {
                                var pos = curveArc.GetPositionAt(i * options.Granularity);
                                (double latitude, double longitude) = projectionConverter.ConvertToLatitudeAndLongitude(pos.X, pos.Z);
                                coordinates.Add([longitude, latitude]);
                            }

                            var finalPos = curveArc.GetPositionAt(curveArc.Length);
                            (double latitudeF, double longitudeF) = projectionConverter.ConvertToLatitudeAndLongitude(finalPos.X, finalPos.Z);
                            coordinates.Add([longitudeF, latitudeF]);

                            yield return new Feature
                            {
                                Geometry = new Geometry
                                {
                                    Type = "LineString",
                                    Coordinates = coordinates,
                                },
                                Properties = new Properties
                                {
                                    Id = curveArc.Id.ToString(),
                                    Name = nameof(CurveArc)
                                }
                            };

                            break;
                        }

                    case CurveEasement curveEasement:
                        {
                            var arrayLength = (int)Math.Floor(curveEasement.Length / options.Granularity) + 1;
                            if (arrayLength < 2) arrayLength = 2;
                            var coordinates = new List<double[]>(arrayLength);

                            for (int i = 0; i < arrayLength - 1; i++)
                            {
                                var pos = curveEasement.EstimatePositionAt(i * options.Granularity);
                                (double latitude, double longitude) = projectionConverter.ConvertToLatitudeAndLongitude(pos.X, pos.Z);
                                coordinates.Add([longitude, latitude]);
                            }

                            var finalPos = curveEasement.EstimatePositionAt(curveEasement.Length);
                            (double latitudeF, double longitudeF) = projectionConverter.ConvertToLatitudeAndLongitude(finalPos.X, finalPos.Z);
                            coordinates.Add([longitudeF, latitudeF]);

                            yield return new Feature
                            {
                                Geometry = new Geometry
                                {
                                    Type = "LineString",
                                    Coordinates = coordinates,
                                },
                                Properties = new Properties
                                {
                                    Id = curveEasement.Id.ToString(),
                                    Name = nameof(CurveEasement)
                                }
                            };

                            break;
                        }

                    default:
                        break;
                }
            }
        }

        public class GeoJsonAdapterOptions
        {
            public double Granularity { get; set; } = 10;
            public JsonSerializerOptions serializerOptions { get; set; } = new JsonSerializerOptions();

            internal string Indentation { get => serializerOptions.WriteIndented ? "    " : ""; }
            internal string NewLine { get => serializerOptions.WriteIndented ? "\r\n" : ""; }
            internal string Space { get => serializerOptions.WriteIndented ? " " : ""; }
        }
    }
}
