using ComprehensiveRailworksArchiveNetwork;
using ComprehensiveRailworksArchiveNetwork.Drivers;
using ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem;
using ComprehensiveRailworksArchiveNetwork.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal class CRANUnitTests
    {
        [Test]
        public async Task TestSerializer()
        {
            var author = new Author
            {
                Guid = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test",
                Links = new List<AuthorLinks>
                {
                    { new AuthorLinks { Name = "Test", URL = "test@example.com/test-account" } }
                },
                Description = "Test",
                TrustLevel = Author.TrustLevelType.Verified
            };

            var addonVersion = new AddonVersion
            {
                FileList = new List<string>(),
                PendingApproval = false,
                Changes = new List<string> { "awesome new change #1", "awesome new change #2" },
                Dependencies = new List<Dependency>(),
                InstallerFiles = new List<ExeFile>(),
                PostInstallationTask = new List<InstallationTask> {
                    {
                        new ExecuteBat
                        {
                            DeleteAfterwards = false,
                            FilePathRelativeToAssetsFolder = Path.Combine("PierreG", "France", "Faccs", "installgeofiles.bat")
                        } 
                    }
                },
                PreInstallationTask = new List<InstallationTask> { },
                ReadmeFiles = new List<ReadmeFile> { },
                RWPFiles = new List<RWPFile> { },
                Submitted = true,
                VersionNumber = new VersionNumber
                {
                    Major = 1,
                    Minor = 0,
                    Patch = 0
                },
                Url = "https://example.com/download"
            };

            var variant = new AddonVariant
            {
                Description = "Test",
                Guid = Guid.NewGuid(),
                Label = "Test",
                Versions = new List<AddonVersion>
                {
                    { addonVersion }
                }
            };

            var addon = new Addon
            {
                Era = AddonEra.IV,
                Type = AddonType.Repaint,
                IsOptional = false,
                Author = author,
                Credits = new List<Collaborator>(),
                Description = "Test",
                Guid = Guid.NewGuid(),
                Name = "Faccs repaint",
                Variants = new List<AddonVariant>
                {
                    { variant }
                }
            };

            string filename = Path.Combine(Directory.GetCurrentDirectory(), "CRAN.Xml");

            var exists = File.Exists(filename);
            if (exists) File.Delete(filename);

            var driver = new FileSystemDriver(filename);

            Assert.IsTrue(File.Exists(filename));

            var oldSize = File.ReadAllText(filename).Length;

            Assert.Greater(oldSize, 0);

            await driver.CreateAddon(addon);

            var newSize = File.ReadAllText(filename).Length;

            Assert.Greater(newSize, oldSize);

            var searchResult = await driver.SearchForAddons("Faccs", new SearchOptions()).ToListAsync();

            Assert.NotNull(searchResult);
            Assert.Greater(searchResult.Count, 0);

            Assert.AreEqual(searchResult[0].Author.Name, author.Name);
        }

    }
}
