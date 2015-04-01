using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace ImprovedAssetsPanel
{
    public class ImprovedAssetsPanel : MonoBehaviour
    {
        private enum SortMode
        {
            Alphabetical = 0,
            LastUpdated = 1,
            LastSubscribed = 2
        }

        private static bool bootstrapped;

        private static UIPanel sortDropDown;
        private static SortMode sortMode = SortMode.Alphabetical;

        private static readonly string kEntryTemplate = "EntryTemplate";
        private static readonly string kMapEntryTemplate = "MapEntryTemplate";
        private static readonly string kSaveEntryTemplate = "SaveEntryTemplate";
        private static readonly string kAssetEntryTemplate = "AssetEntryTemplate";

        public static void Bootstrap()
        {
            try
            {
                InitializeAssetSortDropDown();

                if (bootstrapped)
                {
                    return;
                }

                var customContentPanel = GameObject.Find("(Library) CustomContentPanel").GetComponent<CustomContentPanel>();

                customContentPanel.gameObject.AddComponent<UpdateHook>().onUnityUpdate = () =>
                {
                    RefreshAssets();
                };

                RedirectionHelper.RedirectCalls
                (
                    typeof(CustomContentPanel).GetMethod("Refresh",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(ImprovedAssetsPanel).GetMethod("RefreshAssets",
                        BindingFlags.Static | BindingFlags.Public)
                );

                bootstrapped = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void Revert()
        {
            if (!bootstrapped)
            {
                return;
            }

            bootstrapped = false;
        }

        private static void InitializeAssetSortDropDown()
        {
            if (GameObject.Find("AssetsSortBy") != null)
            {
                return;
            }

            var shadows = GameObject.Find("Shadows").GetComponent<UIPanel>();

            if (shadows == null)
            {
                return;
            }

            var moarGroup = GameObject.Find("Assets").GetComponent<UIPanel>().Find<UIPanel>("MoarGroup");

            if (moarGroup == null)
            {
                return;
            }

            var moarLabel = moarGroup.Find<UILabel>("Moar");
            var moarButton = moarGroup.Find<UIButton>("Button");

            moarGroup.position = new Vector3(moarGroup.position.x, -6.0f, moarGroup.position.z);

            moarLabel.isVisible = false;
            moarButton.isVisible = false;

            sortDropDown = GameObject.Instantiate(shadows);
            sortDropDown.gameObject.name = "AssetsSortBy";
            sortDropDown.transform.parent = moarGroup.transform;
            sortDropDown.name = "AssetsSortBy";
            sortDropDown.Find<UILabel>("Label").isVisible = false;

            var dropdown = sortDropDown.Find<UIDropDown>("ShadowsQuality");
            dropdown.name = "SortByDropDown";
            dropdown.size = new Vector2(200.0f, 24.0f);
            dropdown.textScale = 0.8f;

            dropdown.eventSelectedIndexChanged += (component, value) =>
            {
                sortMode = (SortMode)value;
                RefreshAssets();
            };

            var sprite = dropdown.Find<UIButton>("Sprite");
            sprite.foregroundSpriteMode = UIForegroundSpriteMode.Scale;

            var enumValues = Enum.GetValues(typeof(SortMode));
            dropdown.items = new string[enumValues.Length];

            int i = 0;
            foreach (var value in enumValues)
            {
                dropdown.items[i] = String.Format("Sort by: {0}", EnumToString((SortMode)value));
                i++;
            }
        }

        private static string EnumToString(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.Alphabetical:
                    return "Name";
                case SortMode.LastSubscribed:
                    return "Last subscribed";
                case SortMode.LastUpdated:
                    return "Last updated";
            }

            return "Unknown";
        }

        public static void RefreshAssets()
        {
            Debug.LogWarning("Refreshing assets");
            var customContentPanel = GameObject.Find("(Library) CustomContentPanel").GetComponent<CustomContentPanel>();
            RefreshAllAssetsList(customContentPanel);
            RefreshPackagesList(customContentPanel);
            RefreshMapsList(customContentPanel);
            RefreshSavesList(customContentPanel);
            RefreshAssetsList(customContentPanel);
            Debug.LogWarning("Done!");
        }

        private static void RefreshAllAssetsList(CustomContentPanel customContentPanel)
        {
            UITemplateManager.ClearInstances(kEntryTemplate);
            var component = customContentPanel.Find("AllAssetsList");
            if (component.isEnabled)
            {
                foreach (var current in PackageManager.allPackages)
                {
                    foreach (Package.Asset asset in current)
                    {
                        var packageEntry = UITemplateManager.Get<PackageEntry>(kEntryTemplate);
                        component.AttachUIComponent(packageEntry.gameObject);
                        packageEntry.entryName = string.Concat(asset.package.packageName, ".", asset.name, "\t(",
                            asset.type, ")");
                        packageEntry.entryActive = true;
                        packageEntry.package = current;
                    }
                }
            }
        }

        private static void RefreshPackagesList(CustomContentPanel customContentPanel)
        {
            var component = customContentPanel.Find("PackagesList");
            if (component.isEnabled)
            {
                foreach (var current in PackageManager.allPackages)
                {
                    var packageEntry = UITemplateManager.Get<PackageEntry>(kEntryTemplate);
                    component.AttachUIComponent(packageEntry.gameObject);
                    packageEntry.entryName = current.packageName;
                    packageEntry.entryActive = true;
                    packageEntry.package = current;
                }
            }
        }

        private static void RefreshMapsList(CustomContentPanel customContentPanel)
        {
            UITemplateManager.ClearInstances(kMapEntryTemplate);
            var component = customContentPanel.Find("MapsList");
            foreach (var current in PackageManager.FilterAssets(UserAssetType.MapMetaData))
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kMapEntryTemplate);
                component.AttachUIComponent(packageEntry.gameObject);
                packageEntry.entryName = string.Concat(current.package.packageName, ".", current.name, "\t(",
                    current.type, ")");
                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;
                packageEntry.publishedFileId = current.package.GetPublishedFileID();
                packageEntry.RequestDetails();
            }
        }

        private static void RefreshSavesList(CustomContentPanel customContentPanel)
        {
            UITemplateManager.ClearInstances(kSaveEntryTemplate);
            var component = customContentPanel.Find("SavesList");
            foreach (var current in PackageManager.FilterAssets(UserAssetType.SaveGameMetaData))
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kSaveEntryTemplate);
                component.AttachUIComponent(packageEntry.gameObject);
                packageEntry.entryName = string.Concat(current.package.packageName, ".", current.name, "\t(",
                    current.type, ")");
                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;
                packageEntry.publishedFileId = current.package.GetPublishedFileID();
                packageEntry.RequestDetails();
            }
        }

        private static void RefreshAssetsList(CustomContentPanel customContentPanel)
        {
            UITemplateManager.ClearInstances(kAssetEntryTemplate);

            var component = customContentPanel.Find("AssetsList");

            var panels = component.GetComponentsInChildren<UIPanel>();
            foreach (var panel in panels)
            {
                Destroy(panel.gameObject);
            }

            float currentX = 0;

            var currentPanel = component.AddUIComponent(typeof (UIPanel)) as UIPanel;
            var panelSize = new Vector2(1200.0f, 226.0f); 
            currentPanel.size = panelSize;
            int currentPanelCount = 0;

            var assets = PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData).ToArray();

            if (sortMode == SortMode.Alphabetical)
            {
                Array.Sort(assets, (a, b) =>
                {
                    if (a.name == null)
                    {
                        return 1;
                    }

                    if (b.name == null)
                    {
                        return -1;
                    }

                    return a.name.CompareTo(b.name);
                });
            }
            else if (sortMode == SortMode.LastUpdated)
            {
                Array.Sort(assets, (a, b) =>
                {
                    return GetAssetLastModifiedDelta(a).CompareTo(GetAssetLastModifiedDelta(b));
                });
            }
            else if (sortMode == SortMode.LastSubscribed)
            {
                Array.Sort(assets, (a, b) =>
                {
                    return GetAssetCreatedDelta(a).CompareTo(GetAssetCreatedDelta(b));
                });
            }

            foreach (var current in assets)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kAssetEntryTemplate);
                currentPanel.AttachUIComponent(packageEntry.gameObject);
                currentPanelCount++;
                if (currentPanelCount >= 3)
                {
                    currentPanelCount = 0;
                    currentPanel = component.AddUIComponent(typeof(UIPanel)) as UIPanel;
                    currentPanel.size = panelSize;
                }

                packageEntry.entryName = String.Format("{0}.{1}\t({2})", current.package.packageName, current.name, current.type);

                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;
                packageEntry.publishedFileId = current.package.GetPublishedFileID();
                packageEntry.RequestDetails();

                var panel = packageEntry.gameObject.GetComponent<UIPanel>();
                panel.size = new Vector2(404.0f, 226.0f);
                panel.relativePosition = new Vector3(currentX, 0.0f);
                panel.backgroundSprite = "";

                currentX += panel.size.x;
                if (currentX >= panel.size.x*3.0f)
                {
                    currentX = 0.0f;
                }

                var image = panel.Find<UITextureSprite>("Image");
                image.size = panel.size - new Vector2(4.0f, 2.0f);
                image.position = new Vector3(0.0f, image.position.y, image.position.z);

                var nameLabel = panel.Find<UILabel>("Name");
                try
                {
                    nameLabel.text = nameLabel.text.Split('(')[1];
                }
                catch (Exception)
                {
                }

                nameLabel.zOrder = 2;
                nameLabel.textColor = Color.white;
                nameLabel.autoHeight = false;
                nameLabel.autoSize = false;
                nameLabel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                nameLabel.textAlignment = UIHorizontalAlignment.Left;
                nameLabel.verticalAlignment = UIVerticalAlignment.Top;
                nameLabel.size = new Vector2(380.0f, 224.0f);
                nameLabel.relativePosition = new Vector3(4.0f, 4.0f, nameLabel.relativePosition.z);

                var delete = panel.Find<UIButton>("Delete");
                delete.size = new Vector2(24.0f, 24.0f);
                delete.relativePosition = new Vector3(374.0f, 2.0f, delete.relativePosition.z);

                var active = panel.Find<UICheckBox>("Active");
                active.relativePosition = new Vector3(310.0f, 200.0f, active.relativePosition.z);
                active.zOrder = 2;

                var onOff = active.Find<UILabel>("OnOff");
                onOff.textColor = Color.black;

                var view = panel.Find<UIButton>("View");
                view.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                view.zOrder = 2;
                view.size = new Vector2(view.size.x, 24.0f);
                view.textScale = 0.7f;
                view.relativePosition = new Vector3(4.0f, 198.0f, view.relativePosition.z);
                view.text = "WORKSHOP";

                var share = panel.Find<UIButton>("Share");
                share.zOrder = 2;
                share.size = new Vector2(view.size.x, 24.0f);
                share.textScale = 0.7f;
                share.relativePosition = new Vector3(4.0f + view.size.x, 198.0f, share.relativePosition.z);
            }

            foreach (var current in PackageManager.FilterAssets(UserAssetType.ColorCorrection))
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kAssetEntryTemplate);
                component.AttachUIComponent(packageEntry.gameObject);
                packageEntry.entryName = string.Concat(current.package.packageName, ".", current.name, "\t(",
                    current.type, ")");
                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;

                packageEntry.RequestDetails();
            }
        }

        private static TimeSpan GetAssetLastModifiedDelta(Package.Asset asset)
        {
            try
            {
                return DateTime.Now - File.GetLastWriteTime(asset.pathOnDisk);
            }
            catch (Exception)
            {
                return TimeSpan.FromSeconds(0);
            }
        }

        private static TimeSpan GetAssetCreatedDelta(Package.Asset asset)
        {
            try
            {
                return DateTime.Now - File.GetCreationTime(asset.pathOnDisk);
            }
            catch (Exception)
            {
                return TimeSpan.FromSeconds(0);
            }
        }

    }
}