using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework.Packaging;
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
            LastSubscribed = 2,
            Active = 3,
            Favorite = 4,
        }

        private enum AssetType
        {
            Favorite,
            All,
            Building,
            Residential,
            Commercial,
            Industrial,
            Office, 
            Prop,
            Tree,
            Intersection,
            Park,
            Electricity,
            WaterAndSewage,
            Garbage,
            Healthcare,
            Deathcare,
            FireDepartment,
            PoliceDepartment,
            Education,
            Transport,
            TransportBus,
            TransportMetro,
            TransportTrain,
            TransportShip,
            TransportPlane,
            UniqueBuilding,
            Monument,
            Unknown,
            ColorLUT,
        }

        private static readonly string configPath = "ImprovedAssetsPanelConfig.xml";
        private static Configuration config = new Configuration();

        private static void LoadConfig()
        {
            config = Configuration.Deserialize(configPath);
            if (config == null)
            {
                config = new Configuration();
                SaveConfig();
            }
        }

        private static void SaveConfig()
        {
            if (config != null)
            {
                Configuration.Serialize(configPath, config);
            }
        }

        private static bool bootstrapped;

        private static UIPanel sortDropDown;
        private static SortMode sortMode = SortMode.Alphabetical;
        private static AssetType filterMode = AssetType.All;

        private static readonly string kEntryTemplate = "EntryTemplate";
        private static readonly string kMapEntryTemplate = "MapEntryTemplate";
        private static readonly string kSaveEntryTemplate = "SaveEntryTemplate";
        private static readonly string kAssetEntryTemplate = "AssetEntryTemplate";

        private static string GetSpriteNameForAssetType(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.Favorite:
                    return "InfoIconHealth";
                case AssetType.Building:
                    return "BuildingIcon";
                case AssetType.Prop:
                    return "IconAssetProp";
                case AssetType.Tree:
                    return "IconPolicyForest";
                case AssetType.Intersection:
                    return "SubBarRoadsIntersection";
                case AssetType.Park:
                    return "SubBarBeautificationParksnPlazas";
                case AssetType.Electricity:
                    return "ToolbarIconElectricity";
                case AssetType.WaterAndSewage:
                    return "InfoIconWater";
                case AssetType.Garbage:
                    return "InfoIconGarbage";
                case AssetType.Healthcare:
                    return "ToolbarIconHealthcare";
                case AssetType.Deathcare:
                    return "ToolbarIconHealthcareDisabled";
                case AssetType.FireDepartment:
                    return "ToolbarIconFireDepartment";
                case AssetType.PoliceDepartment:
                    return "ToolbarIconPolice";
                case AssetType.Education:
                    return "ToolbarIconEducation";
                case AssetType.Transport:
                    return "IconPolicyFreePublicTransport";
                case AssetType.TransportBus:
                    return "SubBarPublicTransportBus";
                case AssetType.TransportMetro:
                    return "SubBarPublicTransportMetro";
                case AssetType.TransportTrain:
                    return "SubBarPublicTransportTrain";
                case AssetType.TransportShip:
                    return "SubBarPublicTransportShip";
                case AssetType.TransportPlane:
                    return "SubBarPublicTransportPlane";
                case AssetType.UniqueBuilding:
                    return "InfoIconLevelFocused";
                case AssetType.Monument:
                    return "FeatureMonumentLevel2";
                case AssetType.Residential:
                    return "BuildingIcon";
                case AssetType.Commercial:
                    return "BuildingIcon";
                case AssetType.Industrial:
                    return "BuildingIcon";
                case AssetType.Office:
                    return "BuildingIcon";
                case AssetType.ColorLUT:
                    return "BuildingIcon";
                case AssetType.Unknown:
                    return "BuildingIcon";
            }

            return "";
        }

        public static void Bootstrap()
        {
            try
            {
                LoadConfig();

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

        private static List<UIButton> assetTypeButtons = new List<UIButton>(); 

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
                ScrollAssetsList(0.0f);
                RefreshOnlyAssetsList();
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

            var uiView = FindObjectOfType<UIView>();

            var panel = uiView.AddUIComponent(typeof (UIPanel)) as UIPanel;
            panel.transform.parent = moarGroup.transform;
            panel.size = new Vector2(600.0f, 32.0f);

            var assetTypes = (AssetType[])Enum.GetValues(typeof (AssetType));

            float x = 0.0f;
            foreach (var assetType in assetTypes)
            {
                if (assetType == AssetType.Unknown)
                {
                    continue;
                }

                var button = uiView.AddUIComponent(typeof(UIButton)) as UIButton;
                button.size = new Vector2(32.0f, 32.0f);

                if (assetType != AssetType.ColorLUT && assetType != AssetType.All)
                {
                    button.normalFgSprite = GetSpriteNameForAssetType(assetType);
                }
                else if(assetType == AssetType.ColorLUT)
                {
                    button.text = "LUT";
                }
                else if (assetType == AssetType.All)
                {
                    button.text = "ALL";
                }

                button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
                button.scaleFactor = 1.0f;
                button.isVisible = true;
                button.transform.parent = panel.transform;
                button.AlignTo(panel, UIAlignAnchor.TopLeft);
                button.relativePosition = new Vector3(x, 0.0f);
                x += 34.0f;

                if (assetType == AssetType.Residential)
                {
                    button.color = Color.green;
                }
                else if (assetType == AssetType.Commercial)
                {
                    button.color = Color.blue;
                }
                else if (assetType == AssetType.Industrial)
                {
                    button.color = Color.yellow;
                }
                else if (assetType == AssetType.Office)
                {
                    button.color = new Color32(0, 255, 255, 255);
                }

                button.focusedColor = button.color;
                button.hoveredColor = button.color;
                button.disabledColor = button.color;
                button.pressedColor = button.color;

                button.opacity = 0.25f;
                if (assetType == AssetType.All)
                {
                    button.opacity = 1.0f;
                }

                var assetTypeCopy = assetType;
 
                button.eventClick += (component, param) =>
                {
                    filterMode = assetType;

                    foreach (var item in assetTypeButtons)
                    {
                        item.opacity = 0.25f;
                    }

                    button.opacity = 1.0f;
                    ScrollAssetsList(0.0f);
                    RefreshOnlyAssetsList();
                };

                assetTypeButtons.Add(button);
            }

            var customContentPanel = GameObject.Find("(Library) CustomContentPanel").GetComponent<CustomContentPanel>();
            var assetsList = GameObject.Find("Assets").GetComponent<UIComponent>().Find<UIScrollablePanel>("AssetsList");

            assetsList.eventScrollPositionChanged += (component, value) =>
            {
                while (value.y/226.0f >= currentPanelId - 3)
                {
                    if (currentPanelId <= _cachedAssets.Count/3)
                    {
                        DrawAssets(customContentPanel, currentPanelId);
                    }
                    else
                    {
                        break;
                    }
                }
            };
        }
        private static void ScrollAssetsList(float value)
        {
            var scrollbar =
                GameObject.Find("AssetsList")
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.value = value;
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
                case SortMode.Active:
                    return "Active";
                case SortMode.Favorite:
                    return "Favorite";
            }

            return "Unknown";
        }

        public static void RefreshAssets()
        {
            var customContentPanel = GameObject.Find("(Library) CustomContentPanel").GetComponent<CustomContentPanel>();
            RefreshAllAssetsList(customContentPanel);
            RefreshPackagesList(customContentPanel);
            RefreshMapsList(customContentPanel);
            RefreshSavesList(customContentPanel);
            RefreshAssetsList(customContentPanel, true);
        }

        public static void RefreshOnlyAssetsList()
        {
            var customContentPanel = GameObject.Find("(Library) CustomContentPanel").GetComponent<CustomContentPanel>();
            RefreshAssetsList(customContentPanel, false);
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

        private static Dictionary<Package.Asset, AssetType> _assetTypeCache = new Dictionary<Package.Asset, AssetType>(); 

        private static AssetType GetAssetType(Package.Asset asset)
        {
            if (_assetTypeCache.ContainsKey(asset))
            {
                return _assetTypeCache[asset];
            }

            CustomAssetMetaData customAssetMetaData = null;

            try
            {
                customAssetMetaData = asset.Instantiate<CustomAssetMetaData>();
            }
            catch (Exception)
            {
            }

            if (customAssetMetaData == null)
            {
                _assetTypeCache[asset] = AssetType.Unknown;
                return AssetType.Unknown;
            }

            if (customAssetMetaData.type == CustomAssetMetaData.Type.Prop)
            {
                _assetTypeCache[asset] = AssetType.Prop;
                return AssetType.Prop;
            }

            if (customAssetMetaData.type == CustomAssetMetaData.Type.Tree)
            {
                _assetTypeCache[asset] = AssetType.Tree;
                return AssetType.Tree;
            }

            if (customAssetMetaData.type == CustomAssetMetaData.Type.Unknown)
            {
                _assetTypeCache[asset] = AssetType.Unknown;
                return AssetType.Unknown;
            }

            var tags = customAssetMetaData.steamTags;

            if (ContainsTag(tags, "Residential"))
            {
                _assetTypeCache[asset] = AssetType.Residential;
                return AssetType.Residential;
            }

            if (ContainsTag(tags, "Commercial"))
            {
                _assetTypeCache[asset] = AssetType.Commercial;
                return AssetType.Commercial;
            }

            if (ContainsTag(tags, "Industrial"))
            {
                _assetTypeCache[asset] = AssetType.Industrial;
                return AssetType.Industrial;
            }

            if (ContainsTag(tags, "Office"))
            {
                _assetTypeCache[asset] = AssetType.Office;
                return AssetType.Office;
            }

            if (ContainsTag(tags, "Building"))
            {
                _assetTypeCache[asset] = AssetType.Building;
                return AssetType.Building;
            }

            if (ContainsTag(tags, "Intersection"))
            {
                _assetTypeCache[asset] = AssetType.Intersection;
                return AssetType.Intersection;
            }

            if (ContainsTag(tags, "Park"))
            {
                _assetTypeCache[asset] = AssetType.Park;
                return AssetType.Park;
            }

            if (ContainsTag(tags, "Electricity"))
            {
                _assetTypeCache[asset] = AssetType.Electricity;
                return AssetType.Electricity;
            }

            if (ContainsTag(tags, "WaterAndSewage"))
            {
                _assetTypeCache[asset] = AssetType.WaterAndSewage;
                return AssetType.WaterAndSewage;
            }

            if (ContainsTag(tags, "Garbage"))
            {
                _assetTypeCache[asset] = AssetType.Garbage;
                return AssetType.Garbage;
            }

            if (ContainsTag(tags, "Healthcare"))
            {
                _assetTypeCache[asset] = AssetType.Healthcare;
                return AssetType.Healthcare;
            }

            if (ContainsTag(tags, "Deathcare"))
            {
                _assetTypeCache[asset] = AssetType.Deathcare;
                return AssetType.Deathcare;
            }

            if (ContainsTag(tags, "FireDepartment"))
            {
                _assetTypeCache[asset] = AssetType.FireDepartment;
                return AssetType.FireDepartment;
            }

            if (ContainsTag(tags, "PoliceDepartment"))
            {
                _assetTypeCache[asset] = AssetType.PoliceDepartment;
                return AssetType.PoliceDepartment;
            }

            if (ContainsTag(tags, "Education"))
            {
                _assetTypeCache[asset] = AssetType.Education;
                return AssetType.Education;
            }

            if (ContainsTag(tags, "Transport"))
            {
                _assetTypeCache[asset] = AssetType.Transport;
                return AssetType.Transport;
            }

            if (ContainsTag(tags, "TransportBus"))
            {
                _assetTypeCache[asset] = AssetType.TransportBus;
                return AssetType.TransportBus;
            }

            if (ContainsTag(tags, "TransportMetro"))
            {
                _assetTypeCache[asset] = AssetType.TransportMetro;
                return AssetType.TransportMetro;
            }

            if (ContainsTag(tags, "TransportTrain"))
            {
                _assetTypeCache[asset] = AssetType.TransportTrain;
                return AssetType.TransportTrain;
            }

            if (ContainsTag(tags, "TransportShip"))
            {
                _assetTypeCache[asset] = AssetType.TransportShip;
                return AssetType.TransportShip;
            }

            if (ContainsTag(tags, "TransportPlane"))
            {
                _assetTypeCache[asset] = AssetType.TransportPlane;
                return AssetType.TransportPlane;
            }

            if (ContainsTag(tags, "UniqueBuilding"))
            {
                _assetTypeCache[asset] = AssetType.UniqueBuilding;
                return AssetType.UniqueBuilding;
            }

            if (ContainsTag(tags, "Monument"))
            {
                _assetTypeCache[asset] = AssetType.Monument;
                return AssetType.Monument;
            }

            _assetTypeCache[asset] = AssetType.Unknown;
            return AssetType.Unknown;
        }

        private static bool ContainsTag(string[] haystack, string needle)
        {
            foreach (var item in haystack)
            {
                if (item == needle)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Package.Asset> _cachedAssets = new List<Package.Asset>();

        private static void PreCacheAssets()
        {
            var assets = PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData).ToList();

            Dictionary<Package.Asset, AssetType> assetTypes = new Dictionary<Package.Asset, AssetType>();
            foreach (var asset in assets)
            {
                assetTypes[asset] = GetAssetType(asset);
            }

            if (filterMode == AssetType.All)
            {
                _cachedAssets = assets;
            }
            else if (filterMode == AssetType.Favorite)
            {
                _cachedAssets = assets.FindAll(asset => config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64));
            }
            else
            {
                _cachedAssets = assets.FindAll(asset => GetAssetType(asset) == filterMode);
            }
        }

        private static void SortCachedAssets()
        {
            if (sortMode == SortMode.Alphabetical)
            {
                _cachedAssets.Sort((a, b) =>
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
                _cachedAssets.Sort((a, b) => GetAssetLastModifiedDelta(a).CompareTo(GetAssetLastModifiedDelta(b)));
            }
            else if (sortMode == SortMode.LastSubscribed)
            {
                _cachedAssets.Sort((a, b) => GetAssetCreatedDelta(a).CompareTo(GetAssetCreatedDelta(b)));
            }
            else if (sortMode == SortMode.Active)
            {
                var active = new List<Package.Asset>();
                var inactive = new List<Package.Asset>();
                foreach (var asset in _cachedAssets)
                {
                    if(asset.isEnabled) active.Add(asset);
                    else inactive.Add(asset);
                }

                _cachedAssets.Clear();
                foreach (var asset in active) _cachedAssets.Add(asset);
                foreach (var asset in inactive) _cachedAssets.Add(asset);
            }
            else if (sortMode == SortMode.Favorite)
            {
                var favorite = new List<Package.Asset>();
                var nonfavorite = new List<Package.Asset>();
                foreach (var asset in _cachedAssets)
                {
                    if (config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64)) favorite.Add(asset);
                    else nonfavorite.Add(asset);
                }

                _cachedAssets.Clear();
                foreach (var asset in favorite) _cachedAssets.Add(asset);
                foreach (var asset in nonfavorite) _cachedAssets.Add(asset);
            }
        }

        private static int currentPanelId = 0;

        private static void DrawAssets(CustomContentPanel customContentPanel, int row)
        {
            var component = customContentPanel.Find("AssetsList");

            List<UIPanel> panels = new List<UIPanel>();
            for (int i = 0; i < component.transform.childCount; i++)
            {
                var panel = component.transform.GetChild(i).GetComponent<UIPanel>();
                if (panel != null)
                {
                    panels.Add(panel);
                }
            }

            var panelSize = new Vector2(1200.0f, 226.0f);

            UIPanel currentPanel = null;
            if (currentPanelId < panels.Count)
            {
                currentPanel = panels[currentPanelId++];
                currentPanel.isVisible = true;
            }
            else
            {
                currentPanel = component.AddUIComponent(typeof(UIPanel)) as UIPanel;
                currentPanelId++;
                currentPanel.size = panelSize;
            }

            var assetsCount = _cachedAssets.Count;

            float currentX = 0;
            for (int i = row * 3; i < Mathf.Min((row + 1) * 3, assetsCount); i++)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kAssetEntryTemplate);
                currentPanel.AttachUIComponent(packageEntry.gameObject);

                var current = _cachedAssets[i];

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

                var image = panel.Find<UITextureSprite>("Image");
                image.size = panel.size - new Vector2(4.0f, 2.0f);

                image.position = new Vector3(0.0f, image.position.y, image.position.z);

                var nameLabel = panel.Find<UILabel>("Name");

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
                nameLabel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
                delete.size = new Vector2(24.0f, 24.0f);
                delete.relativePosition = new Vector3(374.0f, 2.0f, delete.relativePosition.z);

                var active = panel.Find<UICheckBox>("Active");
                nameLabel.anchor = UIAnchorStyle.Bottom | UIAnchorStyle.Right;
                active.relativePosition = new Vector3(370.0f, 200.0f, active.relativePosition.z);
                active.zOrder = 2;

                var favButton = panel.AddUIComponent<UIButton>();
                favButton.anchor = UIAnchorStyle.Bottom | UIAnchorStyle.Right;
                favButton.normalFgSprite = "InfoIconHealth";
                favButton.size = new Vector2(36.0f, 36.0f);
                favButton.relativePosition = new Vector3(362.0f, 164.0f);
                favButton.opacity = config.favoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64) ? 1.0f : 0.5f;

                favButton.eventClick += (uiComponent, param) =>
                {
                    if (config.favoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64))
                    {
                        config.favoriteAssets.Remove(packageEntry.publishedFileId.AsUInt64);
                        favButton.opacity = 0.5f;
                    }
                    else
                    {
                        config.favoriteAssets.Add(packageEntry.publishedFileId.AsUInt64, true);
                        favButton.opacity = 1.0f;
                    }

                    SaveConfig();
                };

                var onOff = active.Find<UILabel>("OnOff");
                onOff.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                onOff.textColor = Color.white;
                onOff.textScale = 0.75f;
                onOff.text = "Active";
                onOff.relativePosition = new Vector3(-34.0f, 5.0f, onOff.relativePosition.z);

                var view = panel.Find<UIButton>("View");
                view.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                view.zOrder = 2;
                view.size = new Vector2(32.0f, 32.0f);
                view.textScale = 0.7f;
                view.relativePosition = new Vector3(4.0f, 192.0f, view.relativePosition.z);
                view.text = "";
                view.normalFgSprite = "Options";
                view.normalBgSprite = "";
                view.hoveredFgSprite = "OptionsHovered";
                view.hoveredBgSprite = "";
                view.focusedFgSprite = "OptionsFocused";
                view.focusedBgSprite = "";
                view.pressedFgSprite = "OptionsPressed";
                view.pressedBgSprite = "";

                var share = panel.Find<UIButton>("Share");
                share.zOrder = 2;
                share.size = new Vector2(view.size.x, 24.0f);
                share.textScale = 0.7f;
                share.relativePosition = new Vector3(4.0f + view.size.x, 198.0f, share.relativePosition.z);
            }
        }

        private static void RefreshAssetsList(CustomContentPanel customContentPanel, bool preCacheAssets)
        {
            UITemplateManager.ClearInstances(kAssetEntryTemplate);
            var component = customContentPanel.Find("AssetsList");

            if (filterMode == AssetType.ColorLUT)
            {
                for (int i = 0; i < component.transform.childCount; i++)
                {
                    var panel = component.transform.GetChild(i).GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.isVisible = false;
                    }
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

                return;
            }

            PreCacheAssets();
            SortCachedAssets();

            currentPanelId = 0;

            DrawAssets(customContentPanel, 0);
            DrawAssets(customContentPanel, 1);
            DrawAssets(customContentPanel, 2);
 
            List<UIPanel> panels = new List<UIPanel>();
            for (int i = 0; i < component.transform.childCount; i++)
            {
                var panel = component.transform.GetChild(i).GetComponent<UIPanel>();
                if (panel != null)
                {
                    panels.Add(panel);
                }
            }
            for (int i = currentPanelId; i < panels.Count; i++)
            {
                panels[i].isVisible = false;
            }

            if (filterMode != AssetType.All)
            {
                return;
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