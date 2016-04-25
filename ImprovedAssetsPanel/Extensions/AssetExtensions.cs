using System;
using System.IO;
using ColossalFramework.Packaging;

namespace ImprovedAssetsPanel.Extensions
{
    public static class AssetExtensions
    {
        public static TimeSpan GetAssetLastModifiedDelta(this Package.Asset asset)
        {
            try
            {
                return DateTime.Now - File.GetLastWriteTime(asset.package.packagePath);
            }
            catch (Exception)
            {
                return TimeSpan.FromSeconds(0);
            }
        }

        public static TimeSpan GetAssetCreatedDelta(this Package.Asset asset)
        {
            try
            {
                return DateTime.Now - File.GetCreationTime(asset.package.packagePath);
            }
            catch (Exception)
            {
                return TimeSpan.FromSeconds(0);
            }
        }

        public static int CompareNames(this Package.Asset a, Package.Asset b)
        {
            if (a.name == null)
            {
                return 1;
            }

            if (b.name == null)
            {
                return -1;
            }
            return string.Compare(a.name, b.name, StringComparison.InvariantCultureIgnoreCase);
        }


        public static bool IsMatch(this Package.Asset asset, string searchString)
        {
            if (!string.IsNullOrEmpty(searchString) && (asset.name.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) == -1))
            {
                return asset.package.packageAuthor.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) != -1;
            }
            return true;
        }

        public static bool IsFavorite(this Package.Asset asset)
        {
            return Configuration.Config.FavoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64);
        }
    }
}