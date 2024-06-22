﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWDisplayName
    {
        [JsonIgnore]
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

        public static RWDisplayName FromString(string name)
        {
            var localised = new XElement("Localisation-cUserLocalisedString");
            var english = new XElement("English");
            english.Value = name;
            var languages = new List<XElement> {
                english,
                new XElement("French"),
                new XElement("Italian"),
                new XElement("German"),
                new XElement("Spanish"),
                new XElement("Dutch"),
                new XElement("Polish"),
                new XElement("Russian")
            };
            foreach (var lang in languages)
            {
                lang.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
                lang.Value += "";
            }
            languages.Add(new XElement("Other"));
            var key = new XElement("Key");
            key.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            key.Value = "";
            languages.Add(key);
            foreach (var lang in languages)
            {
                localised.Add(lang);
            }
            var root = new XElement("Root");
            root.Add(localised);
            return new RWDisplayName(root);
        }

        public XElement ToXml()
        {
            return displayNameElement.Element("Localisation-cUserLocalisedString")!;
        }
    }
}
