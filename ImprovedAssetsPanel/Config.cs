using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework.Steamworks;

namespace ImprovedAssetsPanel
{

    public class Configuration
    {

        [XmlIgnore]
        public Dictionary<UInt64, bool> favoriteAssets = new Dictionary<UInt64, bool>();

        public List<UInt64> _favoriteAssets = new List<UInt64>();

        public void OnPreSerialize()
        {
            _favoriteAssets = new List<UInt64>();
            foreach (var item in favoriteAssets)
            {
                _favoriteAssets.Add(item.Key);
            }
        }

        public void OnPostDeserialize()
        {
            favoriteAssets = new Dictionary<UInt64, bool>();
            foreach (var item in _favoriteAssets)
            {
                favoriteAssets.Add(item, true);
            }
        }

        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            using (var writer = new StreamWriter(filename))
            {
                config.OnPreSerialize();
                serializer.Serialize(writer, config);
            }
        }

        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    config.OnPostDeserialize();
                    return config;
                }
            }
            catch { }

            return null;
        }
    }

}
