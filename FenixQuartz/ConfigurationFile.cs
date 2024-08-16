using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace FenixQuartz
{
    public class ConfigurationFile
    {
        private readonly Dictionary<string, string> appSettings = new();
        private XmlDocument xmlDoc = new();

        public string this[string key]
        {
            get => GetSetting(key);
            set => SetSetting(key, value);
        }

        public void LoadConfiguration()
        {
            xmlDoc = new();
            xmlDoc.LoadXml(File.ReadAllText(App.ConfigFilePath));

            XmlNode xmlSettings = xmlDoc.ChildNodes[1];
            appSettings.Clear();
            foreach(XmlNode child in xmlSettings.ChildNodes)
                appSettings.Add(child.Attributes["key"].Value, child.Attributes["value"].Value);
        }

        public void SaveConfiguration()
        {
            foreach (XmlNode child in xmlDoc.ChildNodes[1])
                child.Attributes["value"].Value = appSettings[child.Attributes["key"].Value];

            xmlDoc.Save(App.ConfigFilePath);
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            if (appSettings.TryGetValue(key, out string value))
                return value;
            else
            {
                XmlNode newNode = xmlDoc.CreateElement("add");

                XmlAttribute attribute = xmlDoc.CreateAttribute("key");
                attribute.Value = key;
                newNode.Attributes.Append(attribute);

                attribute = xmlDoc.CreateAttribute("value");
                attribute.Value = defaultValue;
                newNode.Attributes.Append(attribute);

                xmlDoc.ChildNodes[1].AppendChild(newNode);
                appSettings.Add(key, defaultValue);
                SaveConfiguration();

                return defaultValue;
            }
        }

        public void SetSetting(string key, string value)
        {
            if (appSettings.ContainsKey(key))
            {
                appSettings[key] = value;
                SaveConfiguration();
            }
        }
    }
}
