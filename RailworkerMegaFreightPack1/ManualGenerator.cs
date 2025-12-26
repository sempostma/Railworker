using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RailworkerMegaFreightPack1
{
    public class ManualGenerator
    {
        /// <summary>
        /// Converts an HTML file to a PDF file (using PuppeteerSharp)
        /// </summary>
        public static async Task ConvertHtmlToPdfAsync(string htmlFilename, string pdfFilename)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), htmlFilename);

            await new BrowserFetcher().DownloadAsync();
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            await using var page = await browser.NewPageAsync();

            await page.GoToAsync("file:///" + fullPath, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });

            await page.PdfAsync(pdfFilename, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                Scale = 0.8m,
                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "0.5in",
                    Bottom = "0.5in",
                    Left = "0.5in",
                    Right = "0.5in"
                }
            });
        }
    }
}
