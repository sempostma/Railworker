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
        private readonly List<(string groupId, string title)> _groupIndex = new List<(string, string)>();

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
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: white; }");
            sb.AppendLine("        h2 { margin: 20px 0 10px 0; page-break-after: avoid; }");
            sb.AppendLine("        .group-section { margin-bottom: 40px; page-break-inside: avoid; }");
            sb.AppendLine("        .container-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .table-title-row { background-color: inherit; }");
            sb.AppendLine("        .table-title { font-weight: bold; padding: 10px; margin: 0; font-size: 14px; page-break-after: avoid; border-bottom: 1px solid #dee2e6; }");
            sb.AppendLine("        .container-table th { background-color: #f8f9fa; padding: 12px; text-align: left; border-bottom: 2px solid #dee2e6; font-weight: bold; }");
            sb.AppendLine("        .container-table td { padding: 12px; border: 1px solid #dee2e6; vertical-align: middle; }");
            sb.AppendLine("        td.container-image { padding: 0px; }");
            sb.AppendLine("        td.container-image img { height: 50px; object-fit: contain; max-width: 300px; display: block; }");
            sb.AppendLine("        .container-number { font-weight: bold; }");
            sb.AppendLine("        .container-details { color: #666; font-size: 14px; }");
            sb.AppendLine("        .rarity-column { display: none; }");
            sb.AppendLine("        .no-image { color: #999; font-style: italic; }");
            sb.AppendLine("        .filter-container { margin: 20px auto; max-width: 600px; }");
            sb.AppendLine("        .filter-input { width: 100%; padding: 10px; font-size: 16px; border: 1px solid #ddd; border-radius: 4px; }");
            sb.AppendLine("        .filter-stats { margin-top: 10px; font-size: 14px; color: #666; }");
            sb.AppendLine("        .print-button { background-color: #4CAF50; color: white; border: none; padding: 10px 20px; ");
            sb.AppendLine("                       text-decoration: none; display: inline-block; font-size: 16px; margin: 10px 2px; cursor: pointer; border-radius: 4px; }");
            sb.AppendLine("        .hidden { display: none !important; }");
            sb.AppendLine("        /* Sub-group row background colors - light Excel-like modern colors */");
            sb.AppendLine("        .subgroup-1 { background-color: #e3f2fd; }");
            sb.AppendLine("        .subgroup-2 { background-color: #f3e5f5; }");
            sb.AppendLine("        .subgroup-3 { background-color: #e8f5e8; }");
            sb.AppendLine("        .subgroup-4 { background-color: #fff3e0; }");
            sb.AppendLine("        .subgroup-5 { background-color: #fce4ec; }");
            sb.AppendLine("        .subgroup-6 { background-color: #e0f2f1; }");
            sb.AppendLine("        .subgroup-7 { background-color: #f1f8e9; }");
            sb.AppendLine("        .subgroup-8 { background-color: #e8eaf6; }");
            sb.AppendLine("        .subgroup-9 { background-color: #fff8e1; }");
            sb.AppendLine("        .subgroup-10 { background-color: #fafafa; }");
            sb.AppendLine("        /* Sub-group header rows with matching background colors */");
            sb.AppendLine("        .subgroup-header.subgroup-1 { font-weight: bold; background-color: #e3f2fd; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-2 { font-weight: bold; background-color: #f3e5f5; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-3 { font-weight: bold; background-color: #e8f5e8; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-4 { font-weight: bold; background-color: #fff3e0; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-5 { font-weight: bold; background-color: #fce4ec; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-6 { font-weight: bold; background-color: #e0f2f1; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-7 { font-weight: bold; background-color: #f1f8e9; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-8 { font-weight: bold; background-color: #e8eaf6; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-9 { font-weight: bold; background-color: #fff8e1; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header.subgroup-10 { font-weight: bold; background-color: #fafafa; border-top: 2px solid #dee2e6; }");
            sb.AppendLine("        .subgroup-header td { padding: 8px 12px; font-size: 14px; }");
            sb.AppendLine("        /* Index styles */");
            sb.AppendLine("        .index-container { margin: 30px 0; }");
            sb.AppendLine("        /* Usage instructions styles */");
            sb.AppendLine("        .usage-container { margin: 30px 0; }");
            sb.AppendLine("        .usage-example { margin: 10px 0; font-family: 'Courier New', monospace; }");
            sb.AppendLine("        .usage-highlight { font-weight: bold; }");
            sb.AppendLine("        /* Rail vehicle number display with example */");
            sb.AppendLine("        .container-number-display { position: relative; }");
            sb.AppendLine("        /* Print mode styles */");
            sb.AppendLine("        @media print {");
            sb.AppendLine("            body { background-color: white; padding: 0; }");
            sb.AppendLine("            .filter-container, .print-button { display: none !important; }");
            sb.AppendLine("            .group-section { page-break-inside: avoid; margin-bottom: 20px; }");
            sb.AppendLine("            .container-table { page-break-inside: avoid; margin-bottom: 15px; }");
            sb.AppendLine("            h2 { page-break-after: avoid; margin-top: 15px; }");
            sb.AppendLine("            .table-title { page-break-after: avoid; }");
            sb.AppendLine("            .container-table tr { page-break-inside: avoid; }");
            sb.AppendLine("        }");
            sb.AppendLine("    </style>");
            sb.AppendLine("    <script>");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("            // Filter functionality");
            sb.AppendLine("            const filterInput = document.getElementById('filter-input');");
            sb.AppendLine("            const filterStats = document.getElementById('filter-stats');");
            sb.AppendLine("            const printButton = document.getElementById('print-button');");
            sb.AppendLine("            const allRows = document.querySelectorAll('.container-table tbody tr');");
            sb.AppendLine("            const allSections = document.querySelectorAll('.group-section');");
            sb.AppendLine("            const allTables = document.querySelectorAll('.container-table');");
            sb.AppendLine("");
            sb.AppendLine("            // Initialize stats");
            sb.AppendLine("            updateFilterStats(allRows.length, allRows.length);");
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
            sb.AppendLine("            // Filter function for table rows");
            sb.AppendLine("            function filterContainers(filterValue) {");
            sb.AppendLine("                let visibleCount = 0;");
            sb.AppendLine("                const totalCount = allRows.length;");
            sb.AppendLine("");
            sb.AppendLine("                // Process all table rows");
            sb.AppendLine("                allRows.forEach(row => {");
            sb.AppendLine("                    const rowText = row.textContent.toLowerCase();");
            sb.AppendLine("                    const isVisible = rowText.includes(filterValue);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Toggle visibility class");
            sb.AppendLine("                    if (isVisible) {");
            sb.AppendLine("                        row.classList.remove('hidden');");
            sb.AppendLine("                        visibleCount++;");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        row.classList.add('hidden');");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                // Update table visibility (hide tables with no visible rows)");
            sb.AppendLine("                allTables.forEach(table => {");
            sb.AppendLine("                    const visibleRowsInTable = table.querySelectorAll('tbody tr:not(.hidden)').length;");
            sb.AppendLine("                    if (visibleRowsInTable === 0) {");
            sb.AppendLine("                        table.classList.add('hidden');");
            sb.AppendLine("                        // Also hide the table title if it exists");
            sb.AppendLine("                        const tableTitle = table.previousElementSibling;");
            sb.AppendLine("                        if (tableTitle && tableTitle.classList.contains('table-title')) {");
            sb.AppendLine("                            tableTitle.classList.add('hidden');");
            sb.AppendLine("                        }");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        table.classList.remove('hidden');");
            sb.AppendLine("                        // Show the table title if it exists");
            sb.AppendLine("                        const tableTitle = table.previousElementSibling;");
            sb.AppendLine("                        if (tableTitle && tableTitle.classList.contains('table-title')) {");
            sb.AppendLine("                            tableTitle.classList.remove('hidden');");
            sb.AppendLine("                        }");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                // Update section visibility");
            sb.AppendLine("                allSections.forEach(section => {");
            sb.AppendLine("                    const visibleTablesInSection = section.querySelectorAll('.container-table:not(.hidden)').length;");
            sb.AppendLine("                    if (visibleTablesInSection === 0) {");
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
            sb.AppendLine("    ");
            sb.AppendLine("    <div class=\"index-container\">");
            sb.AppendLine("        <h2>Container Groups Index</h2>");
            sb.AppendLine("        <ul id=\"group-index\">");
            sb.AppendLine("            <!-- Index will be populated here -->");
            sb.AppendLine("        </ul>");
            sb.AppendLine("    </div>");
            sb.AppendLine("");
            sb.AppendLine("    <div class=\"usage-container\">");
            sb.AppendLine("        <h2>Quick Reference</h2>");
            sb.AppendLine("        <p>To assign specific container types to your rail vehicles, add container numbers to the end of your rail vehicle number using the colon (:) format.</p>");
            sb.AppendLine("        <div class=\"usage-example\">");
            sb.AppendLine("            <strong>Basic Format:</strong> [Rail Vehicle Number][Wagon Designator]:[Container Number]");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"usage-example\">");
            sb.AppendLine("            <strong>Examples:</strong><br>");
            sb.AppendLine("            • Single container: <span class=\"usage-highlight\">338449620107!c30:03</span><br>");
            sb.AppendLine("            • Multiple containers: <span class=\"usage-highlight\">123456789!c20:02:12:13:42</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <p><strong>Component Breakdown:</strong></p>");
            sb.AppendLine("        <ul>");
            sb.AppendLine("            <li><strong>338449620107</strong> - Your rail vehicle number</li>");
            sb.AppendLine("            <li><strong>!c30/!c20</strong> - Container wagon designator (varies: !c20, !c30, !c40, !c45, etc.)</li>");
            sb.AppendLine("            <li><strong>:03</strong> - Container catalog number from this catalog</li>");
            sb.AppendLine("        </ul>");
            sb.AppendLine("        <p><strong>Multiple Containers:</strong> For wagons with 2-4 cargo slots, use multiple colon-separated numbers (e.g., :02:12:13:42). Most groups are designed so containers from the same group work well together.</p>");
            sb.AppendLine("        <p><strong>Note:</strong> The wagon designator may vary or not be present. The key is adding :XX where XX is the container number from this catalog.</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("");
            sb.AppendLine("    <div class=\"filter-container\">");
            sb.AppendLine("        <input type=\"text\" id=\"filter-input\" class=\"filter-input\" placeholder=\"Filter containers by name, company, or any text...\">");
            sb.AppendLine("        <div id=\"filter-stats\" class=\"filter-stats\"></div>");
            sb.AppendLine("        <button id=\"print-button\" class=\"print-button\">Print Catalog</button>");
            sb.AppendLine("    </div>");
        }

        public void GenerateHtml(RandomSkinGroup group, KeyValuePair<string, List<RWLib.Packaging.FileItem>> randomSkinCargoInfo, string skinName, List<Composition> compositions)
        {
            var sb = this.FinalResult;

            int cargoNumber = 1;
            int subGroupColorIndex = 1;

            var provider = group.Destination?.Split("\\").FirstOrDefault() ?? "Unknown";
            var product = randomSkinCargoInfo.Key;
            
            // Add to index for navigation - create simpler, more reliable IDs
            string groupId = $"group-{_groupIndex.Count + 1}";
            string title = $"{skinName} ({provider}\\{product})";
            _groupIndex.Add((groupId, title));

            sb.AppendLine($"    <div class=\"group-section\" id=\"{groupId}\">");
            sb.AppendLine($"        <h2>{title}</h2>");

            // Start single combined table for the entire group
            sb.AppendLine($"        <table class=\"container-table\">");
            sb.AppendLine($"            <thead>");
            sb.AppendLine($"                <tr>");
            sb.AppendLine($"                    <th>Image</th>");
            sb.AppendLine($"                    <th style=\"width: 100px;\">Container #</th>");
            sb.AppendLine($"                    <th>Name</th>");
            sb.AppendLine($"                    <th style=\"width: 80px;\" class=\"rarity-column\">Rarity</th>");
            sb.AppendLine($"                    <th>Company</th>");
            sb.AppendLine($"                </tr>");
            sb.AppendLine($"            </thead>");
            sb.AppendLine($"            <tbody>");

            foreach (var randomSkin in group.RandomSkins)
            {
                var composition = compositions.FirstOrDefault(x => x.Id.StartsWith(randomSkin.Composition));
                if (composition == null) continue;

                // Add sub-group header row if there are multiple sub-groups
                if (group.RandomSkins.Count > 1)
                {
                    sb.AppendLine($"                <tr class=\"subgroup-header subgroup-{subGroupColorIndex}\">");
                    sb.AppendLine($"                    <td colspan=\"5\">{randomSkin.Name}</td>");
                    sb.AppendLine($"                </tr>");
                }

                foreach (var skin in randomSkin.Skins)
                {
                    if (String.IsNullOrEmpty(skin.Texture)) continue;
                    string thumbnailPath = Path.Combine(_thumbnailsBasePath, group.Id, $"{cargoNumber}.jpg");

                    sb.AppendLine($"                <tr class=\"subgroup-{subGroupColorIndex}\">");

                    // Image column
                    sb.AppendLine("                    <td class=\"container-image\">");
                    sb.AppendLine($"                        <img src=\"{thumbnailPath}\" alt=\"Container {cargoNumber}\">");
                    sb.AppendLine("                    </td>");

                    // Cargo number column - now in :XX format
                    sb.AppendLine("                    <td>");
                    sb.AppendLine($"                        <span class=\"container-number container-number-display\">:{cargoNumber:D2}</span>");
                    sb.AppendLine("                    </td>");

                    // Name column
                    sb.AppendLine("                    <td>");
                    sb.AppendLine($"                        <span class=\"container-details\">{skin.Name}</span>");
                    sb.AppendLine("                    </td>");

                    // Rarity column (hidden with CSS)
                    sb.AppendLine("                    <td class=\"rarity-column\">");
                    sb.AppendLine($"                        <span class=\"container-details\">{skin.Rarity}</span>");
                    sb.AppendLine("                    </td>");

                    // Company column
                    sb.AppendLine("                    <td>");
                    if (!string.IsNullOrEmpty(skin.Group))
                    {
                        string companyInfo = GetCompanyInfoFromILUKey(skin.Group);
                        if (!string.IsNullOrEmpty(companyInfo))
                        {
                            sb.AppendLine($"                        <span class=\"container-details\">{companyInfo}</span>");
                        }
                        else
                        {
                            sb.AppendLine($"                        <span class=\"container-details\">-</span>");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"                        <span class=\"container-details\">-</span>");
                    }
                    sb.AppendLine("                    </td>");

                    sb.AppendLine("                </tr>");

                    cargoNumber++;
                }

                // Cycle through colors (1-10) for next sub-group
                subGroupColorIndex = (subGroupColorIndex % 10) + 1;
            }

            // End combined table
            sb.AppendLine($"            </tbody>");
            sb.AppendLine($"        </table>");

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

            // Generate the index using JavaScript
            sb.AppendLine("    <script>");
            sb.AppendLine("        // Populate the group index");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("            const indexList = document.getElementById('group-index');");
            sb.AppendLine("            const groups = [");
            
            for (int i = 0; i < _groupIndex.Count; i++)
            {
                var (groupId, title) = _groupIndex[i];
                var comma = i < _groupIndex.Count - 1 ? "," : "";
                sb.AppendLine($"                {{ id: '{groupId}', title: '{title.Replace("'", "\\'").Replace("\\", "\\\\")}' }}{comma}");
            }
            
            sb.AppendLine("            ];");
            sb.AppendLine("            ");
            sb.AppendLine("            groups.forEach(group => {");
            sb.AppendLine("                const li = document.createElement('li');");
            sb.AppendLine("                const a = document.createElement('a');");
            sb.AppendLine("                a.href = '#' + group.id;");
            sb.AppendLine("                a.textContent = group.title;");
            sb.AppendLine("                li.appendChild(a);");
            sb.AppendLine("                indexList.appendChild(li);");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
            sb.AppendLine("    </script>");

            // HTML footer
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return FinalResult.ToString();
        }
    }
}
