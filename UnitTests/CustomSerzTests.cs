using Newtonsoft.Json;
using NUnit.Framework;
using RWLib;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace UnitTests
{
    public class CustomSerzTests
    {
        private RWLibrary rwLib;
        private string br101Bin;
        private string br101Geo;

        [SetUp]
        public void Setup()
        {
            this.rwLib = new RWLibrary(new RWLibOptions { Logger = new UnitTestLogger() });
            this.br101Bin = Path.Combine(rwLib.TSPath, "Assets\\Kuju\\RailSimulator\\RailVehicles\\Electric\\BR101\\Blue\\Engine\\br101.bin");
            this.br101Geo = Path.Combine(rwLib.TSPath, "Assets\\Kuju\\RailSimulator\\RailVehicles\\Electric\\BR101\\Blue\\Engine\\br101.GeoPcDx");
        }

        [Test]
        public async Task TestBin()
        {
            var result = await rwLib.Serializer.Deserialize("E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains\\RailSimulator\\RailVehicles\\Locomotives\\Diesel\\NS Class 6400\\Engine\\Version DB Cargo\\NS Class 6400 DB Cargo.bin");

            Assert.NotNull(result.Root);
        }

        [Test]
        public async Task TestGeoPcdx()
        {
            var result = await rwLib.Serializer.Deserialize("E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains\\NS Class 186 and ICRmh wagons\\RailVehicles\\CT NS ICRmh wagons\\Version_DCR_IC+\\ICRmh_IC+-a.GeoPcDx");

            Assert.NotNull(result.Root);
        }

        [Test]
        public async Task TestEqualityOfDeserializationOutput()
        {
            var files = new System.Collections.Generic.List<string>
            {
                br101Bin,
                br101Geo
            };

            foreach (var file in files)
            {
                var resultCustomSerz = (await rwLib.Serializer.Deserialize(file)).ToString();
                var resultNativeSerz = (await rwLib.Serializer.DeserializeWithSerzExe(file)).ToString();

                for (int i = 0; i < resultCustomSerz.Length; i++)
                {
                    if (resultCustomSerz[i] != resultNativeSerz[i])
                    {
                        var start = System.Math.Max(0, i - 50);
                        var end = System.Math.Min(resultCustomSerz.Length, System.Math.Min(resultNativeSerz.Length, i + 50));
                        var range1 = i - start;
                        var range2 = end - (i + 1);
                        var slicedCustom1 = resultCustomSerz.Substring(start, range1);
                        var slicedCustom2 = resultCustomSerz.Substring(i + 1, range2);

                        var slicedNative1 = resultNativeSerz.Substring(start, range1);
                        var slicedNative2 = resultNativeSerz.Substring(i + 1, range2);

                        var msg = $"Output of serzes are not equal [{Path.GetFileName(file)}]\n";
                        msg += $"[char diff = custom: {HttpUtility.JavaScriptStringEncode("" + resultCustomSerz[i])} ~= native: {HttpUtility.JavaScriptStringEncode("" + resultNativeSerz[i])}]\n";
                        msg += $"(character index: {i}): custom = \"{slicedCustom1 + ">" + resultCustomSerz[i] + "<" + slicedCustom2}\" vs native = \"{slicedNative1 + '>' + resultNativeSerz[i] + '<' + slicedNative2}\"";
                        Assert.Fail(msg);
                    }
                }
            }
        }


        [Test]
        public async Task TestEqualityOfBinToXmlOutput()
        {
            var files = new System.Collections.Generic.List<string>
            {
                br101Bin,
                br101Geo
            };

            foreach (var file in files)
            {
                var tempPath = Path.GetTempPath();
                var serzTempDir = Path.Combine(tempPath, "RWLib", "SerzTemp");
                Directory.CreateDirectory(serzTempDir); // ensure directory
                var tempFilename = Path.Combine(serzTempDir, Convert.ToString(Random.Shared.Next(), 16) + ".bin");
                File.Copy(file, tempFilename);
                var customSerzXml = await rwLib.Serializer.Deserialize(tempFilename);
                await rwLib.Serializer.DeserializeWithSerzExe(tempFilename);
                var nativeXmlFile = Path.ChangeExtension(tempFilename, ".xml");

                var ms = new MemoryStream();
                var settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "\t";
                settings.OmitXmlDeclaration = false;
                settings.Encoding = new UTF8Encoding(false);
                settings.NewLineHandling = NewLineHandling.None;
                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    customSerzXml.WriteTo(writer);
                }
                var resultCustomSerz = new UTF8Encoding(false).GetString(ms.ToArray()).Replace(" />", "/>");
                var resultNativeSerz = File.ReadAllText(nativeXmlFile, new UTF8Encoding(false));

                for (int i = 0; i < resultCustomSerz.Length; i++)
                {
                    if (resultCustomSerz[i] != resultNativeSerz[i])
                    {
                        var start = System.Math.Max(0, i - 50);
                        var end = System.Math.Min(resultCustomSerz.Length, System.Math.Min(resultNativeSerz.Length, i + 50));
                        var range1 = i - start;
                        var range2 = end - (i + 1);
                        var slicedCustom1 = resultCustomSerz.Substring(start, range1);
                        var slicedCustom2 = resultCustomSerz.Substring(i + 1, range2);

                        var slicedNative1 = resultNativeSerz.Substring(start, range1);
                        var slicedNative2 = resultNativeSerz.Substring(i + 1, range2);

                        var msg = $"Output of serzes are not equal [{Path.GetFileName(file)}]\n";
                        msg += $"[char diff = custom: {HttpUtility.JavaScriptStringEncode("" + resultCustomSerz[i])} ~= native: {HttpUtility.JavaScriptStringEncode("" + resultNativeSerz[i])}]\n";
                        msg += $"(character index: {i}): custom = \"{slicedCustom1 + ">" + resultCustomSerz[i] + "<" + slicedCustom2}\" vs native = \"{slicedNative1 + '>' + resultNativeSerz[i] + '<' + slicedNative2}\"";
                        Assert.Fail(msg);
                    }
                }
            }
        }
    }
}

