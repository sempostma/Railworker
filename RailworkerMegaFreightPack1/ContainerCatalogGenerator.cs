using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RWLib.Graphics;

namespace RailworkerMegaFreightPack1
{
    public class ContainerCatalogGenerator
    {
        public StringBuilder FinalResult = new StringBuilder();
        private readonly string _thumbnailsBasePath;
        private readonly Dictionary<string, string> _iluKeyCompanyMap;

        public ContainerCatalogGenerator()
        {
            _thumbnailsBasePath = "thumbnails";
            _iluKeyCompanyMap = LoadILUKeys();

            var sb = this.FinalResult;

            // HTML header
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>Container Catalog</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
            sb.AppendLine("        h1, h2 { text-align: center; color: #333; }");
            sb.AppendLine("        small { display:block; text-align: center; color: #333; margin: 8px; font-weight: bold; }");
            sb.AppendLine("        .group-section { margin-bottom: 40px; }");
            sb.AppendLine("        .container-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 20px; }");
            sb.AppendLine("        .container-card { background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .container-image { width: 100%; height: auto; display: block; background-color: #eee; display: flex; align-items: center; justify-content: center; }");
            sb.AppendLine("        .container-image img { width: 100%; height: auto; object-fit: contain; }");
            sb.AppendLine("        .container-info { padding: 15px; }");
            sb.AppendLine("        .container-number { font-size: 18px; font-weight: bold; margin: 0; }");
            sb.AppendLine("        .container-details { color: #666; margin: 5px 0 0; }");
            sb.AppendLine("        .no-image { color: #999; text-align: center; }");
            sb.AppendLine("        .filter-container { margin: 20px auto; max-width: 600px; text-align: center; }");
            sb.AppendLine("        .filter-input { width: 100%; padding: 10px; font-size: 16px; border: 1px solid #ddd; border-radius: 4px; }");
            sb.AppendLine("        .filter-stats { margin-top: 10px; font-size: 14px; color: #666; }");
            sb.AppendLine("        .print-button { background-color: #4CAF50; color: white; border: none; padding: 10px 20px; text-align: center; ");
            sb.AppendLine("                       text-decoration: none; display: inline-block; font-size: 16px; margin: 10px 2px; cursor: pointer; border-radius: 4px; }");
            sb.AppendLine("        .hidden { display: none !important; }");
            sb.AppendLine("        /* Print mode styles */");
            sb.AppendLine("        @media print {");
            sb.AppendLine("            body { background-color: white; padding: 0; }");
            sb.AppendLine("            .filter-container, .print-button { display: none !important; }");
            sb.AppendLine("            .container-card { box-shadow: none; break-inside: avoid; }");
            sb.AppendLine("            .container-grid { display: grid; grid-template-columns: repeat(3, 1fr); }");
            sb.AppendLine("        }");
            sb.AppendLine("    </style>");
            sb.AppendLine("    <script>");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("            // Filter functionality");
            sb.AppendLine("            const filterInput = document.getElementById('filter-input');");
            sb.AppendLine("            const filterStats = document.getElementById('filter-stats');");
            sb.AppendLine("            const printButton = document.getElementById('print-button');");
            sb.AppendLine("            const allCards = document.querySelectorAll('.container-card');");
            sb.AppendLine("            const allSections = document.querySelectorAll('.group-section');");
            sb.AppendLine("");
            sb.AppendLine("            // Initialize stats");
            sb.AppendLine("            updateFilterStats(allCards.length, allCards.length);");
            sb.AppendLine("");
            sb.AppendLine("            // Filter event listener");
            sb.AppendLine("            filterInput.addEventListener('input', function() {");
            sb.AppendLine("                const filterValue = this.value.toLowerCase();");
            sb.AppendLine("                filterContainers(filterValue);");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            // Print button event listener");
            sb.AppendLine("            printButton.addEventListener('click', function() {");
            sb.AppendLine("                window.print();");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            // Filter function using DocumentFragment for performance");
            sb.AppendLine("            function filterContainers(filterValue) {");
            sb.AppendLine("                let visibleCount = 0;");
            sb.AppendLine("                const totalCount = allCards.length;");
            sb.AppendLine("");
            sb.AppendLine("                // Use DocumentFragment for better performance");
            sb.AppendLine("                const fragment = document.createDocumentFragment();");
            sb.AppendLine("");
            sb.AppendLine("                // Process all cards");
            sb.AppendLine("                allCards.forEach(card => {");
            sb.AppendLine("                    const cardText = card.textContent.toLowerCase();");
            sb.AppendLine("                    const isVisible = cardText.includes(filterValue);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Toggle visibility class");
            sb.AppendLine("                    if (isVisible) {");
            sb.AppendLine("                        card.classList.remove('hidden');");
            sb.AppendLine("                        visibleCount++;");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        card.classList.add('hidden');");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                // Update section visibility");
            sb.AppendLine("                allSections.forEach(section => {");
            sb.AppendLine("                    const visibleCardsInSection = section.querySelectorAll('.container-card:not(.hidden)').length;");
            sb.AppendLine("                    if (visibleCardsInSection === 0) {");
            sb.AppendLine("                        section.classList.add('hidden');");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        section.classList.remove('hidden');");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                // Update stats");
            sb.AppendLine("                updateFilterStats(visibleCount, totalCount);");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            // Update filter statistics");
            sb.AppendLine("            function updateFilterStats(visible, total) {");
            sb.AppendLine("                filterStats.textContent = `Showing ${visible} of ${total} containers`;");
            sb.AppendLine("            }");
            sb.AppendLine("        });");
            sb.AppendLine("    </script>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <h1>Container Catalog</h1>");
            sb.AppendLine("    <div class=\"filter-container\">");
            sb.AppendLine("        <input type=\"text\" id=\"filter-input\" class=\"filter-input\" placeholder=\"Filter containers by name, company, or any text...\">");
            sb.AppendLine("        <div id=\"filter-stats\" class=\"filter-stats\"></div>");
            sb.AppendLine("        <button id=\"print-button\" class=\"print-button\">Print Catalog</button>");
            sb.AppendLine("    </div>");
        }

        public void GenerateHtml(RandomSkinGroup group, List<Composition> compositions)
        {
            var sb = this.FinalResult;

            int cargoNumber = 1;

                sb.AppendLine($"    <div class=\"group-section\">");
                sb.AppendLine($"        <h2>Group: {group.Id}</h2>");

                foreach (var randomSkin in group.RandomSkins)
                {
                    var composition = compositions.FirstOrDefault(x => x.Id == randomSkin.Composition);
                    if (composition == null) continue;

                    if (group.RandomSkins.Count > 1)
                    {
                        sb.AppendLine($"        <small>{randomSkin.Name}</small>");
                    }
                    sb.AppendLine($"        <div class=\"container-grid\">");

                    foreach (var skin in randomSkin.Skins)
                        {
                        if (String.IsNullOrEmpty(skin.Texture)) continue;
                        string thumbnailPath = Path.Combine(_thumbnailsBasePath, group.Id, $"{cargoNumber}.png");

                        sb.AppendLine("            <div class=\"container-card\">");

                        // Image section
                        sb.AppendLine("                <div class=\"container-image\">");
                        sb.AppendLine($"                    <img src=\"{thumbnailPath}\" alt=\"Container {cargoNumber}\">");
                        sb.AppendLine("                </div>");

                        // Info section
                        sb.AppendLine("                <div class=\"container-info\">");
                        sb.AppendLine($"                    <p class=\"container-number\">Cargo #{cargoNumber}</p>");
                        sb.AppendLine($"                    <p class=\"container-details\">Name: {skin.Name}</p>");
                        sb.AppendLine($"                    <p class=\"container-details\">Rarity: {skin.Rarity}</p>");
                        
                        // Try to find company information based on the skin group
                        if (!string.IsNullOrEmpty(skin.Group))
                        {
                            string companyInfo = GetCompanyInfoFromILUKey(skin.Group);
                            if (!string.IsNullOrEmpty(companyInfo))
                            {
                                sb.AppendLine($"                    <p class=\"container-details\">Company: {companyInfo}</p>");
                            }
                        }
                        sb.AppendLine("                </div>");
                        sb.AppendLine("            </div>");

                        cargoNumber++;
                    }
                    sb.AppendLine("        </div>");

                }

            sb.AppendLine("    </div>");
            }

        private Dictionary<string, string> LoadILUKeys()
        {
            var result = new Dictionary<string, string>();
                // Read the ILUKeys.json file using the Utilities.ReadFile method
            string jsonContent = Utilities.ReadFile("ILUKeys.json");
            
            // Parse the JSON
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                JsonElement root = doc.RootElement;
                
                // Iterate through the array to build the dictionary
                foreach (JsonElement element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("key", out JsonElement keyElement))
                    {
                        // Safely get the key string, handling null case
                        string key = keyElement.ValueKind == JsonValueKind.Null ? string.Empty : keyElement.GetString();
                        
                        if (!string.IsNullOrEmpty(key))
                        {
                            // Safely get other properties, handling null or missing cases
                            string company = element.TryGetProperty("company", out JsonElement companyElement) && 
                                            companyElement.ValueKind != JsonValueKind.Null ? 
                                            companyElement.GetString() : string.Empty;
                                            
                            string country = element.TryGetProperty("country", out JsonElement countryElement) && 
                                            countryElement.ValueKind != JsonValueKind.Null ? 
                                            countryElement.GetString() : string.Empty;
                                            
                            string city = element.TryGetProperty("city", out JsonElement cityElement) && 
                                        cityElement.ValueKind != JsonValueKind.Null ? 
                                        cityElement.GetString() : string.Empty;
                            
                            // Build the company info string, handling empty values
                            string companyInfo = company;
                            if (!string.IsNullOrEmpty(city))
                            {
                                companyInfo += !string.IsNullOrEmpty(companyInfo) ? $", {city}" : city;
                            }
                            if (!string.IsNullOrEmpty(country))
                            {
                                companyInfo += !string.IsNullOrEmpty(companyInfo) ? $" ({country})" : country;
                            }
                            
                            result[key] = companyInfo;
                        }
                    }
                }
            }
            
            return result;
        }

        private string GetCompanyInfoFromILUKey(string group)
        {
            // Extract the first 4 characters as the key if the group is long enough
            string key = group.Length >= 4 ? group.Substring(0, 4) : group;
            
            // Look up the key in our pre-loaded dictionary
            if (_iluKeyCompanyMap.TryGetValue(key, out string companyInfo))
            {
                return companyInfo;
            }
            
            return group; // No match found
        }

        public override string ToString()
        {
            var sb = this.FinalResult;

            // HTML footer
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return FinalResult.ToString();
        }
    }
}
