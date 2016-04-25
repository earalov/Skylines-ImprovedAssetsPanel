using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ImprovedAssetsPanel
{

    public class Configuration
    {
        [XmlIgnore]
        private const string ConfigPath = "ImprovedAssetsPanelConfig.xml";

        [XmlIgnore]
        internal static Configuration Config { get; private set; } = new Configuration();

        [XmlIgnore]
        public Dictionary<ulong, bool> FavoriteAssets { get; private set; } = new Dictionary<ulong, bool>();

        public List<ulong> _favoriteAssets = new List<ulong>();

        public void OnPreSerialize()
        {
            _favoriteAssets = new List<ulong>();
            foreach (var item in FavoriteAssets)
            {
                _favoriteAssets.Add(item.Key);
            }
        }

        public void OnPostDeserialize()
        {
            FavoriteAssets = new Dictionary<ulong, bool>();
            foreach (var item in _favoriteAssets)
            {
                FavoriteAssets.Add(item, true);
            }
        }

        internal static void LoadConfig()
        {
            Config = Configuration.Deserialize(ConfigPath);
            if (Config != null)
            {
                return;
            }
            Config = new Configuration();
            SaveConfig();
        }

        internal static void SaveConfig()
        {
            if (Config != null)
            {
                Configuration.Serialize(ConfigPath, Config);
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
