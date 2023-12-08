using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWDisplayName
    {
        public XElement displayNameElement;

        public string? En { get => GetDisplayName("English"); set => SetDisplayName("English", value); }
        public string? Fr { get => GetDisplayName("French"); set => SetDisplayName("French", value); }
        public string? It { get => GetDisplayName("Italian"); set => SetDisplayName("Italian", value); }
        public string? De { get => GetDisplayName("German"); set => SetDisplayName("German", value); }
        public string? Es { get => GetDisplayName("Spanish"); set => SetDisplayName("Spanish", value); }
        public string? Nl { get => GetDisplayName("Dutch"); set => SetDisplayName("Dutch", value); }
        public string? Pl { get => GetDisplayName("Polish"); set => SetDisplayName("Polish", value); }
        public string? Ru { get => GetDisplayName("Russian"); set => SetDisplayName("Russian", value); }
        public string? Other { get => GetDisplayName("Other"); set => SetDisplayName("Other", value); }
        public string? Key { get => GetDisplayName("Key"); set => SetDisplayName("Key", value); }

        public RWDisplayName(XElement displayNameElement)
        {
            if (displayNameElement == null)
            {
                throw new ArgumentNullException("Arugment can not be null");
            }

            this.displayNameElement = displayNameElement;
        }

        public string? GetDisplayName(string elementName)
        {
            if (elementName == null) return null;

            return displayNameElement.Element("Localisation-cUserLocalisedString")?.Element(elementName)?.Value;
        }

        public void SetDisplayName(string elementName, string? value)
        {
            var elem = displayNameElement.Element("Localisation-cUserLocalisedString")?.Element(elementName);
            if (elem != null && value != null) elem.Value = value;
        }
    }
}
