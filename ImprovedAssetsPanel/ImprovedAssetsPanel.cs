﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using UnityEngine;
using System.ComponentModel;

namespace ImprovedAssetsPanel
{
    public class ImprovedAssetsPanel : MonoBehaviour
    {
        private enum SortMode
        {
            [Description("Name")]
            Alphabetical = 0,
            [Description("Last updated")]
            LastUpdated = 1,
            [Description("Last subscribed")]
            LastSubscribed = 2,
            [Description("Active")]
            Active = 3,
            [Description("Favorite")]
            Favorite = 4,
            [Description("Location")]
            Location = 5,
        }


        private enum SortOrder
        {
            [Description("Ascending")]
            Ascending = 0,
            [Description("Descending")]
            Descending = 1
        }

        private enum AssetType
        {
            Favorite,
            All,
            Unknown,
            [Description("Building")]
            Building,
            [Description("Residential")]
            Residential,
            [Description("Commercial")]
            Commercial,
            [Description("Industrial")]
            Industrial,
            [Description("Office")]
            Office,
            [Description("Prop")]
            Prop,
            [Description("Tree")]
            Tree,
            [Description("Intersection")]
            Intersection,
            [Description("Park")]
            Park,
            [Description("Electricity")]
            Electricity,
            [Description("Water & Sewage")]
            WaterAndSewage,
            [Description("Garbage")]
            Garbage,
            [Description("Healthcare")]
            Healthcare,
            [Description("Deathcare")]
            Deathcare,
            [Description("Fire Department")]
            FireDepartment,
            [Description("Police Department")]
            PoliceDepartment,
            [Description("Education")]
            Education,
            [Description("Transport")]
            Transport,
            [Description("Transport Bus")]
            TransportBus,
            [Description("Transport Metro")]
            TransportMetro,
            [Description("Transport Train")]
            TransportTrain,
            [Description("Transport Ship")]
            TransportShip,
            [Description("Transport Plane")]
            TransportPlane,
            [Description("Unique Building")]
            UniqueBuilding,
            [Description("Monument")]
            Monument,
            [Description("Color Correction LUT")]
            ColorLut,
            [Description("Vehicle")]
            Vehicle
        }

        private const string ConfigPath = "ImprovedAssetsPanelConfig.xml";
        private static Configuration _config = new Configuration();

        private static void LoadConfig()
        {
            _config = Configuration.Deserialize(ConfigPath);
            if (_config != null) return;
            _config = new Configuration();
            SaveConfig();
        }

        private static void SaveConfig()
        {
            if (_config != null)
            {
                Configuration.Serialize(ConfigPath, _config);
            }
        }

        private static UIPanel _filterButtons;
        private static UIPanel _sortModePanel;
        private static UILabel _sortModeLabel;
        private static UIButton _sortOrderButton;
        private static UILabel _sortOrderLabel;
        private static UIPanel _sortOptions;
        private static UIPanel _additionalOptions;

        private static SortMode _sortMode = SortMode.Alphabetical;
        private static AssetType _filterMode = AssetType.All;
        private static SortOrder _sortOrder = SortOrder.Ascending;

        private const string KEntryTemplate = "EntryTemplate";
        private const string KMapEntryTemplate = "MapEntryTemplate";
        private const string KSaveEntryTemplate = "SaveEntryTemplate";
        private const string KAssetEntryTemplate = "AssetEntryTemplate";

        private static List<UIButton> _assetTypeButtons = new List<UIButton>();
        private static Dictionary<AssetType, UILabel> _assetTypeLabels = new Dictionary<AssetType, UILabel>();

        private static UIPanel _newAssetsPanel;
        private static UIPanel[] _assetRows;

        private static MultiMap<Package.Asset, AssetType> _assetTypeIndex = new MultiMap<Package.Asset, AssetType>();
        private static Dictionary<Package.Asset, string> _assetPathCache = new Dictionary<Package.Asset, string>();
        private static List<Package.Asset> _assetCache = new List<Package.Asset>();

        private static float _scrollPositionY;
        private static float _maxScrollPositionY;

        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }


        public static void OnCategoryChanged(UIComponent c, int idx)
        {
            var contentManagerPanel = GameObject.Find("(Library) ContentManagerPanel").GetComponent<ContentManagerPanel>();
            if (contentManagerPanel == null)
            {
                return;
            }
            var index = (GetInstanceField(typeof(ContentManagerPanel), contentManagerPanel, "m_Categories") as UIListBox).selectedIndex;
            var setContainerCategory = contentManagerPanel.GetType().GetMethod("SetContainerCategory", BindingFlags.NonPublic | BindingFlags.Instance);
            setContainerCategory.Invoke(contentManagerPanel, new object[] { index });

            var searchField = contentManagerPanel.Find<UITextField>("SearchField");
            var notAssetPanel = index != 2;
            searchField.isVisible = notAssetPanel;
            if (!notAssetPanel)
            {
                return;
            }
            var performSearch = contentManagerPanel.GetType().GetMethod("PerformSearch", BindingFlags.NonPublic | BindingFlags.Instance);
            performSearch.Invoke(contentManagerPanel, new object[] { searchField.text });
        }

        private static string GetSpriteNameForAssetType(AssetType assetType, bool hovered = false)
        {
            switch (assetType)
            {
                case AssetType.Favorite:
                    return hovered ? "InfoIconHealthHovered" : "InfoIconHealth";
                case AssetType.Building:
                    return hovered ? "InfoIconOutsideConnectionsHovered" : "InfoIconOutsideConnectionsPressed";
                case AssetType.Prop:
                    return hovered ? "ToolbarIconPropsHovered" : "ToolbarIconProps";
                case AssetType.Tree:
                    return hovered ? "IconPolicyForestHovered" : "IconPolicyForest";
                case AssetType.Intersection:
                    return hovered ? "ThumbnailJunctionsCloverFocused" : "ThumbnailJunctionsClover";
                case AssetType.Park:
                    return hovered ? "ToolbarIconBeautificationHovered" : "ToolbarIconBeautification";
                case AssetType.Electricity:
                    return hovered ? "InfoIconElectricityHovered" : "InfoIconElectricity";
                case AssetType.WaterAndSewage:
                    return hovered ? "InfoIconWaterHovered" : "InfoIconWater";
                case AssetType.Garbage:
                    return hovered ? "InfoIconGarbageHovered" : "InfoIconGarbage";
                case AssetType.Healthcare:
                    return hovered ? "ToolbarIconHealthcareHovered" : "ToolbarIconHealthcare";
                case AssetType.Deathcare:
                    return hovered ? "ToolbarIconHealthcareFocused" : "ToolbarIconHealthcareDisabled";
                case AssetType.FireDepartment:
                    return hovered ? "InfoIconFireSafetyHovered" : "InfoIconFireSafety";
                case AssetType.PoliceDepartment:
                    return hovered ? "ToolbarIconPoliceHovered" : "ToolbarIconPolice";
                case AssetType.Education:
                    return hovered ? "InfoIconEducationHovered" : "InfoIconEducation";
                case AssetType.Transport:
                    return hovered ? "ToolbarIconPublicTransportHovered" : "ToolbarIconPublicTransport";
                case AssetType.TransportBus:
                    return hovered ? "SubBarPublicTransportBusHovered" : "SubBarPublicTransportBus";
                case AssetType.TransportMetro:
                    return hovered ? "SubBarPublicTransportMetroHovered" : "SubBarPublicTransportMetro";
                case AssetType.TransportTrain:
                    return hovered ? "SubBarPublicTransportTrainHovered" : "SubBarPublicTransportTrain";
                case AssetType.TransportShip:
                    return hovered ? "SubBarPublicTransportShipHovered" : "SubBarPublicTransportShip";
                case AssetType.TransportPlane:
                    return hovered ? "SubBarPublicTransportPlaneHovered" : "SubBarPublicTransportPlane";
                case AssetType.UniqueBuilding:
                    return hovered ? "InfoIconLevelHovered" : "InfoIconLevelFocused";
                case AssetType.Monument:
                    return hovered ? "ToolbarIconMonumentsHovered" : "ToolbarIconMonuments";
                case AssetType.Residential:
                    return hovered ? "InfoIconOutsideConnectionsHovered" : "InfoIconOutsideConnectionsPressed";
                case AssetType.Commercial:
                    return hovered ? "InfoIconOutsideConnectionsHovered" : "InfoIconOutsideConnectionsPressed";
                case AssetType.Industrial:
                    return hovered ? "InfoIconOutsideConnectionsHovered" : "InfoIconOutsideConnectionsPressed";
                case AssetType.Office:
                    return hovered ? "InfoIconOutsideConnectionsHovered" : "InfoIconOutsideConnectionsPressed";
                case AssetType.Vehicle:
                    return hovered ? "InfoIconTrafficCongestionHovered" : "InfoIconTrafficCongestion";
            }

            return "";
        }

        private static RedirectCallsState _stateRefresh;
        private static RedirectCallsState _statePerformSearch;

        public static void Bootstrap()
        {
            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject == null)
            {
                new GameObject("ImprovedAssetsPanelSyncObject").AddComponent<UpdateHook>().onUnityDestroy = Revert;
            }
            else
            {
                return;
            }

            new GameObject().AddComponent<ImprovedAssetsPanel>().name = "ImprovedAssetsPanel";

            LoadConfig();

            Initialize();

            _stateRefresh = RedirectionHelper.RedirectCalls
            (
                typeof(ContentManagerPanel).GetMethod("Refresh",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(ImprovedAssetsPanel).GetMethod("Refresh",
                    BindingFlags.Static | BindingFlags.Public)
            );

            _statePerformSearch = RedirectionHelper.RedirectCalls
            (
                typeof(ContentManagerPanel).GetMethod("OnCategoryChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(ImprovedAssetsPanel).GetMethod("OnCategoryChanged",
                    BindingFlags.Static | BindingFlags.Public)
            );

            var contentManagerPanel = GameObject.Find("(Library) ContentManagerPanel").GetComponent<ContentManagerPanel>();
            contentManagerPanel.gameObject.AddComponent<UpdateHook>().onUnityUpdate = Refresh;
        }

        public static void Refresh()
        {

            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            RefreshAllAssets(categoryContainer);
            RefreshPackages(categoryContainer);
            RefreshStandardCategory(categoryContainer, "Maps", UserAssetType.MapMetaData, KMapEntryTemplate);
            RefreshStandardCategory(categoryContainer, "Saves", UserAssetType.SaveGameMetaData, KSaveEntryTemplate);
            RefreshAssets(categoryContainer);
        }

        private static void RefreshStandardCategory(UITabContainer categoryContainer, string categoryName, Package.AssetType packageAssetType, string entryTemplate)
        {
            UITemplateManager.ClearInstances(entryTemplate);
            var contentPanel = categoryContainer.Find(categoryName).Find("Content");
            foreach (var current in PackageManager.FilterAssets(packageAssetType))
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(entryTemplate);
                contentPanel.AttachUIComponent(packageEntry.gameObject);
                SetupAssetPackageEntry(packageEntry, current);
            }
        }

        private static void RefreshPackages(UITabContainer categoryContainer)
        {
            var component = categoryContainer.Find("Packages").Find("Content");
            if (!component.isEnabled)
            {
                return;
            }
            foreach (var current in PackageManager.allPackages)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(KEntryTemplate);
                component.AttachUIComponent(packageEntry.gameObject);
                packageEntry.entryName = current.packageName;
                packageEntry.entryActive = true;
                packageEntry.package = current;
            }
        }

        private static void RefreshAllAssets(UITabContainer categoryContainer)
        {
            UITemplateManager.ClearInstances(KEntryTemplate);
            var component = categoryContainer.Find("AllAssets").Find("Content");
            if (!component.isEnabled)
            {
                return;
            }
            foreach (var current in PackageManager.allPackages)
            {
                foreach (Package.Asset asset in current)
                {
                    var packageEntry = UITemplateManager.Get<PackageEntry>(KEntryTemplate);
                    component.AttachUIComponent(packageEntry.gameObject);
                    packageEntry.entryName = string.Concat(asset.package.packageName, ".", asset.name, "\t(",
                        asset.type, ")");
                    packageEntry.entryActive = true;
                    packageEntry.package = current;
                }
            }
        }

        public static void Revert()
        {
            RedirectionHelper.RevertRedirect(typeof(ContentManagerPanel).GetMethod("Refresh",
                        BindingFlags.Instance | BindingFlags.NonPublic), _stateRefresh);
            RedirectionHelper.RevertRedirect(typeof(ContentManagerPanel).GetMethod("OnCategoryChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic), _statePerformSearch);

            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            assetsList.verticalScrollbar = scrollbar;
            assetsList.isVisible = true;

            Destroy(_sortModePanel.gameObject);
            Destroy(_sortModeLabel.gameObject);
            Destroy(_sortOrderButton.gameObject);
            Destroy(_sortOrderLabel.gameObject);
            Destroy(_filterButtons.gameObject);
            Destroy(_additionalOptions.gameObject);
            Destroy(_sortOptions.gameObject);
            Destroy(_newAssetsPanel.gameObject);

            _filterButtons = null;
            _additionalOptions = null;
            _sortOptions = null;
            _sortModePanel = null;
            _sortModeLabel = null;
            _sortOrderButton = null;
            _sortOrderLabel = null;

            _sortMode = SortMode.Alphabetical;
            _filterMode = AssetType.All;
            _sortOrder = SortOrder.Ascending;
            _newAssetsPanel = null;
            _assetRows = null;

            _assetTypeIndex = new MultiMap<Package.Asset, AssetType>();
            _assetPathCache = new Dictionary<Package.Asset, string>();
            _assetCache = new List<Package.Asset>();

            _assetTypeButtons = new List<UIButton>();
            _assetTypeLabels = new Dictionary<AssetType, UILabel>();

            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject != null)
            {
                Destroy(syncObject);
            }
        }


        private static void Initialize()
        {
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

            var uiView = FindObjectOfType<UIView>();

            var moarLabel = moarGroup.Find<UILabel>("Moar");
            var moarButton = moarGroup.Find<UIButton>("Button");

            moarGroup.position = new Vector3(moarGroup.position.x, -6.0f, moarGroup.position.z);

            moarLabel.isVisible = false;
            moarButton.isVisible = false;

            _filterButtons = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _filterButtons.transform.parent = moarGroup.transform;
            _filterButtons.size = new Vector2(600.0f, 32.0f);

            var assetTypes = (AssetType[])Enum.GetValues(typeof(AssetType));

            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            var x = 0.0f;
            foreach (var assetType in assetTypes)
            {
                if (assetType == AssetType.ColorLut)
                {
                    continue;
                }

                var button = uiView.AddUIComponent(typeof(UIButton)) as UIButton;
                button.size = new Vector2(20.0f, 20.0f);
                button.tooltip = assetType.ToString();

                button.focusedFgSprite = button.normalFgSprite;
                button.pressedFgSprite = button.normalFgSprite; 
                if (assetType == AssetType.All)
                {
                    button.text = "ALL";
                }
                else if (assetType == AssetType.Unknown)
                {
                    button.text = "N/A"; 
                } else
                {
                    button.normalFgSprite = GetSpriteNameForAssetType(assetType);
                    button.hoveredFgSprite = GetSpriteNameForAssetType(assetType, true);
                }
                button.textScale = 0.5f;
                button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
                button.scaleFactor = 1.0f;
                button.isVisible = true;
                button.transform.parent = _filterButtons.transform;
                button.AlignTo(_filterButtons, UIAlignAnchor.TopLeft);
                button.relativePosition = new Vector3(x, 0.0f);
                x += 22.0f;

                if (assetType == AssetType.Residential)
                {
                    button.color = Color.green;
                }
                else if (assetType == AssetType.Commercial)
                {
                    button.color = new Color32(100, 100, 255, 255);
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

                var type = assetType;
                button.eventClick += (component, param) =>
                {
                    _filterMode = type;

                    foreach (var item in _assetTypeButtons)
                    {
                        item.opacity = 0.25f;
                    }

                    button.opacity = 1.0f;
                    ScrollAssetsList(0.0f);
                    RefreshAssetsOnly();
                };

                _assetTypeButtons.Add(button);

                var label = uiView.AddUIComponent(typeof(UILabel)) as UILabel;
                label.text = "N/A";
                label.AlignTo(button, UIAlignAnchor.TopRight);
                label.relativePosition = new Vector3(16.0f, 0.0f, 0.0f);
                label.zOrder = 7;
                label.textScale = 0.5f;
                label.textColor = Color.white;
                _assetTypeLabels.Add(assetType, label);
            }

            _additionalOptions = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _additionalOptions.transform.parent = moarGroup.transform;
            _additionalOptions.size = new Vector2(120.0f, 32.0f);

            var activateAll = _additionalOptions.AddUIComponent<UIButton>();
            activateAll.size = new Vector2(110.0f, 16.0f);
            activateAll.text = "Activate all";
            activateAll.textScale = 0.7f;
            activateAll.normalBgSprite = "ButtonMenu";
            activateAll.disabledBgSprite = "ButtonMenuDisabled";
            activateAll.hoveredBgSprite = "ButtonMenuHovered";
            activateAll.focusedBgSprite = "ButtonMenu";
            activateAll.pressedBgSprite = "ButtonMenuPressed";
            activateAll.AlignTo(_additionalOptions, UIAlignAnchor.TopLeft);
            activateAll.relativePosition = new Vector3(4.0f, 0.0f);
            activateAll.eventClick += (component, param) =>
            {
                foreach (var item in _assetCache)
                {
                    item.isEnabled = true;
                }

                RefreshAssetsOnly();
            };

            var deactivateAll = _additionalOptions.AddUIComponent<UIButton>();
            deactivateAll.size = new Vector2(110.0f, 16.0f);
            deactivateAll.text = "Deactivate all";
            deactivateAll.textScale = 0.7f;
            deactivateAll.normalBgSprite = "ButtonMenu";
            deactivateAll.disabledBgSprite = "ButtonMenuDisabled";
            deactivateAll.hoveredBgSprite = "ButtonMenuHovered";
            deactivateAll.focusedBgSprite = "ButtonMenu";
            deactivateAll.pressedBgSprite = "ButtonMenuPressed";
            deactivateAll.AlignTo(_additionalOptions, UIAlignAnchor.TopLeft);
            deactivateAll.relativePosition = new Vector3(4.0f, 18.0f);
            deactivateAll.eventClick += (component, param) =>
            {
                foreach (var item in _assetCache)
                {
                    item.isEnabled = false;
                }

                RefreshAssetsOnly();
            };

            _sortOptions = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _sortOptions.transform.parent = moarGroup.transform;
            _sortOptions.size = new Vector2(120.0f, 32.0f);

            _sortModePanel = Instantiate(shadows);
            _sortModePanel.gameObject.name = "AssetsSortMode";
            _sortModePanel.transform.parent = _sortOptions.transform;
            _sortModePanel.name = "AssetsSortMode";
            _sortModePanel.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortModePanel.Find<UILabel>("Label").isVisible = false;
            _sortModePanel.size = new Vector2(120.0f, 16.0f);
            _sortModePanel.autoLayout = false;

            _sortModeLabel = InitializeLabel(uiView, _sortModePanel, "Sort by");
            _sortModeLabel.relativePosition = new Vector3(0.0f, -2.0f, 0.0f);

            var sortModeDropDown = InitializeDropDown<SortMode>(_sortModePanel, "SortModeDropDown");
            sortModeDropDown.relativePosition = new Vector3(0.0f, 0.0f, 0.0f);
            sortModeDropDown.eventSelectedIndexChanged += (component, value) =>
            {
                _sortMode = (SortMode)value;
                ScrollAssetsList(0.0f);
                RefreshAssetsOnly();
            };

            _sortOrderButton = _sortModePanel.AddUIComponent<UIButton>();
            _sortOrderButton.transform.parent = _sortOptions.transform;
            _sortOrderButton.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortOrderButton.size = new Vector2(120.0f, 16.0f);
            _sortOrderButton.text = _sortOrder.GetEnumDescription();
            _sortOrderButton.textScale = 0.7f;
            _sortOrderButton.normalBgSprite = "ButtonMenu";
            _sortOrderButton.disabledBgSprite = "ButtonMenuDisabled";
            _sortOrderButton.hoveredBgSprite = "ButtonMenuHovered";
            _sortOrderButton.focusedBgSprite = "ButtonMenu";
            _sortOrderButton.pressedBgSprite = "ButtonMenuPressed";
            _sortOrderButton.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortOrderButton.relativePosition = new Vector3(0.0f, 16.0f);
            _sortOrderButton.eventClick += (component, param) =>
            {
                switch (_sortOrder)
                {
                    case SortOrder.Ascending:
                        _sortOrder = SortOrder.Descending;
                        break;
                    case SortOrder.Descending:
                        _sortOrder = SortOrder.Ascending;
                        break;
                }
                _sortOrderButton.text = _sortOrder.GetEnumDescription();
                RefreshAssetsOnly();
            };

            _sortOrderLabel = InitializeLabel(uiView, _sortOptions, "Sort Order");
            _sortOrderLabel.relativePosition = new Vector3(0.0f, 14.0f, 0.0f);


            assetsList.verticalScrollbar = null;
            assetsList.isVisible = false;

            _newAssetsPanel = assetsList.transform.parent.GetComponent<UIComponent>().AddUIComponent<UIPanel>();
            _newAssetsPanel.anchor = assetsList.anchor;
            _newAssetsPanel.size = assetsList.size;
            _newAssetsPanel.relativePosition = assetsList.relativePosition;
            _newAssetsPanel.name = "NewAssetsList";
            _newAssetsPanel.clipChildren = true;
            _newAssetsPanel.eventMouseWheel += (component, param) =>
            {
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY -= param.wheelDelta * 80.0f;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - _newAssetsPanel.size.y);

                ScrollRows(originalScrollPos - _scrollPositionY);
                SwapRows();

                SetScrollBar(_scrollPositionY);
            };

            var y = 0.0f;

            _assetRows = new UIPanel[4];
            for (var q = 0; q < 4; q++)
            {
                _assetRows[q] = _newAssetsPanel.AddUIComponent<UIPanel>();
                _assetRows[q].name = "AssetRow" + q;
                _assetRows[q].size = new Vector2(1200.0f, 173.0f);
                _assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += _assetRows[q].size.y;
            }

            scrollbar.eventMouseUp += (component, param) =>
            {
                if (!_newAssetsPanel.isVisible)
                {
                    return;
                }

                if (scrollbar.value == _scrollPositionY)
                {
                    return;
                }
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY = scrollbar.value;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - _newAssetsPanel.size.y);

                if (_scrollPositionY == originalScrollPos)
                {
                    return;
                }
                var viewSize = _newAssetsPanel.size.y;

                var realRowIndex = (int)Mathf.Floor((_scrollPositionY / viewSize) * (viewSize / (_assetRows[0].size.y + 2.0f)));
                var diff = _scrollPositionY - realRowIndex * (_assetRows[0].size.y + 2.0f);

                var _y = 0.0f;
                for (var q = 0; q < 4; q++)
                {
                    _assetRows[q].relativePosition = new Vector3(0.0f, _y, 0.0f);
                    _y += _assetRows[q].size.y + 2.0f;
                }

                var rowsCount = (int)Mathf.Ceil(_assetCache.Count / 3.0f);

                for (var q = 0; q < Mathf.Min(rowsCount, 4); q++)
                {
                    DrawAssets(q, realRowIndex + q);
                }

                ScrollRows(-diff);
                SetScrollBar(_scrollPositionY);
            };

            scrollbar.eventValueChanged += (component, value) =>
            {
                if (Input.GetMouseButton(0))
                {
                    return;
                }

                if (!_newAssetsPanel.isVisible)
                {
                    return;
                }

                if (value == _scrollPositionY)
                {
                    return;
                }
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY = value;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - _newAssetsPanel.size.y);

                var diff = Mathf.Clamp(_scrollPositionY - originalScrollPos, -(_assetRows[0].size.y + 4), _assetRows[0].size.y + 4);
                _scrollPositionY = originalScrollPos + diff;
                ScrollRows(-diff);
                SwapRows();

                SetScrollBar(_scrollPositionY);
            };
        }

        private static UILabel InitializeLabel(UIView uiView, UIComponent parent, string labelText)
        {
            var label = uiView.AddUIComponent(typeof(UILabel)) as UILabel;
            label.transform.parent = parent.transform;
            label.text = labelText;
            label.AlignTo(parent, UIAlignAnchor.TopLeft);
            label.textColor = Color.white;
            label.textScale = 0.5f;
            return label;
        }

        private static UIDropDown InitializeDropDown<T>(UIPanel dropDownPanel, string name)
        {
            var dropdown = dropDownPanel.Find<UIDropDown>("ShadowsQuality");
            dropdown.name = name;
            dropdown.size = new Vector2(120.0f, 16.0f);
            dropdown.textScale = 0.7f;

            var sprite = dropdown.Find<UIButton>("Sprite");
            sprite.foregroundSpriteMode = UIForegroundSpriteMode.Scale;

            var enumValues = Enum.GetValues(typeof(T));
            dropdown.items = new string[enumValues.Length];

            var i = 0;
            foreach (var value in enumValues)
            {
                dropdown.items[i] = ((T)value).GetEnumDescription();
                i++;
            }
            return dropdown;
        }

        private static void SetAssetCountLabels()
        {

            foreach (var assetType in _assetTypeLabels.Keys)
            {
                var label = _assetTypeLabels[assetType];
                switch (assetType)
                {
                    case AssetType.All:
                        label.text = _assetTypeIndex.Keys.Count().ToString();
                        continue;
                    case AssetType.Favorite:
                        label.text = _config.favoriteAssets.Count.ToString();
                        continue;
                }
                var count = _assetTypeIndex.Keys.Count(asset => _assetTypeIndex[asset].Contains(assetType));

                label.text = count.ToString();

            }
        }


        private static int RowCount
        {
            get { return (int)Mathf.Ceil(_assetCache.Count / 3.0f); }
        }

        private static void ScrollRows(float yOffset)
        {
            for (var i = 0; i < 4; i++)
            {
                var row = _assetRows[i];
                row.relativePosition = new Vector3(row.relativePosition.x, row.relativePosition.y + yOffset, row.relativePosition.z);
            }
        }

        private static void SwapRows()
        {
            if (_assetRows[0].relativePosition.y + _assetRows[0].size.y + 2.0f < 0.0f)
            {
                _assetRows[0].relativePosition = new Vector3(0.0f, _assetRows[3].relativePosition.y + _assetRows[3].size.y + 2.0f);
                var firstRealRow = (int)Mathf.Floor(_scrollPositionY / (_assetRows[0].size.y + 2.0f));
                var lastRealRow = firstRealRow + 3;
                DrawAssets(0, lastRealRow);
                ShiftRowsUp();
            }
            else if (_assetRows[0].relativePosition.y > 0.0f)
            {
                _assetRows[3].relativePosition = new Vector3(0.0f, _assetRows[0].relativePosition.y - _assetRows[3].size.y - 2.0f);
                var firstRealRow = (int)Mathf.Floor(_scrollPositionY / (_assetRows[0].size.y + 2.0f));
                DrawAssets(3, firstRealRow);
                ShiftRowsDown();
            }
        }

        private static void ShiftRowsUp()
        {
            var tmp = _assetRows[0];
            _assetRows[0] = _assetRows[1];
            _assetRows[1] = _assetRows[2];
            _assetRows[2] = _assetRows[3];
            _assetRows[3] = tmp;
        }

        private static void ShiftRowsDown()
        {
            var tmp = _assetRows[3];
            _assetRows[3] = _assetRows[2];
            _assetRows[2] = _assetRows[1];
            _assetRows[1] = _assetRows[0];
            _assetRows[0] = tmp;
        }

        private static void ScrollAssetsList(float value)
        {
            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.value = value;
        }

        private static void SetScrollBar(float maxValue, float scrollSize, float value = 0.0f)
        {
            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.maxValue = maxValue;
            scrollbar.scrollSize = scrollSize;
            scrollbar.value = value;
        }

        private static void SetScrollBar(float value = 0.0f)
        {
            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.value = value;
        }

        private static void IndexAssetType(Package.Asset asset)
        {
            if (_assetTypeIndex.Keys.Contains(asset))
            {
                return;
            }

            CustomAssetMetaData customAssetMetaData = null;

            try
            {
                customAssetMetaData = asset.Instantiate<CustomAssetMetaData>();
            }
            catch (Exception)
            {
                // ignored
            }

            if (customAssetMetaData == null)
            {
                _assetTypeIndex.Add(asset, AssetType.Unknown);
                return;
            }

            var tags = customAssetMetaData.steamTags;
            foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
            {
                if (assetType == AssetType.All || assetType == AssetType.Favorite || assetType == AssetType.Unknown)
                {
                    continue;
                }
                if (ContainsTag(tags, assetType.GetEnumDescription()))
                {
                    _assetTypeIndex.Add(asset, assetType);
                }
            }
            if (_assetTypeIndex[asset].Count == 0)
            {
                _assetTypeIndex.Add(asset, AssetType.Unknown);
            }
        }

        private static bool ContainsTag(string[] haystack, string needle)
        {
            if (needle == null || haystack == null)
            {
                return false;
            }
            return haystack.Contains(needle);
        }

        private static void ReIndexAssets(List<Package.Asset> assets)
        {
            _assetTypeIndex.Clear();
            foreach (var asset in assets)
            {
                IndexAssetType(asset);
            }
        }

        private static void PreCacheAssets()
        {
            var assets = PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData).ToList();
            ReIndexAssets(assets);

            switch (_filterMode)
            {
                case AssetType.All:
                    _assetCache = assets;
                    break;
                case AssetType.Favorite:
                    _assetCache = assets.FindAll(asset => _config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64));
                    break;
                default:
                    _assetCache = assets.FindAll(asset => _assetTypeIndex[asset].Contains(_filterMode));
                    break;
            }
        }

        private static bool IsFavorite(Package.Asset asset)
        {
            return _config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64);
        }

        private static void SortCachedAssets()
        {

            Func<Package.Asset, Package.Asset, int> comparerLambda;

            switch (_sortMode)
            {
                case SortMode.Alphabetical:
                    comparerLambda = (a, b) =>
                    {
                        if (a.name == null)
                        {
                            return 1;
                        }

                        if (b.name == null)
                        {
                            return -1;
                        }

                        return String.Compare(a.name, b.name, StringComparison.Ordinal);
                    };
                    break;
                case SortMode.LastUpdated:
                    comparerLambda = (a, b) => GetAssetLastModifiedDelta(a).CompareTo(GetAssetLastModifiedDelta(b));
                    break;
                case SortMode.LastSubscribed:
                    comparerLambda = (a, b) => GetAssetCreatedDelta(a).CompareTo(GetAssetCreatedDelta(b));
                    break;
                case SortMode.Active:
                    comparerLambda = (a, b) => b.isEnabled.CompareTo(a.isEnabled);
                    break;
                case SortMode.Favorite:
                    comparerLambda = (a, b) => IsFavorite(b).CompareTo(IsFavorite(a));
                    break;
                case SortMode.Location:
                    comparerLambda = (a, b) =>
                    {
                        var aIsWorkshop = a.package.packagePath.Contains("workshop");
                        var bIsWorkshop = b.package.packagePath.Contains("workshop");
                        if (aIsWorkshop && bIsWorkshop)
                        {
                            return String.Compare(a.name, b.name, StringComparison.Ordinal);
                        }

                        if (aIsWorkshop)
                        {
                            return 1;
                        }

                        if (bIsWorkshop)
                        {
                            return -1;
                        }

                        return String.Compare(a.name, b.name, StringComparison.Ordinal);
                    };
                    break;
                default:
                    return;
            }
            _assetCache.Sort(new FunctionalComparer<Package.Asset>(
                _sortOrder == SortOrder.Ascending ? comparerLambda :
                (a, b) => -comparerLambda(a, b)));

        }

        private static void DrawAssets(int virtualRow, int realRow)
        {
            if (virtualRow < 0 || virtualRow > 3)
            {
                Debug.LogError("DrawAssets(): virtualRow < 0 || virtualRow > 3 is true");
                return;
            }

            var numRows = (int)Mathf.Ceil(_assetCache.Count / 3.0f);
            if (realRow > numRows - 1)
            {
                return;
            }

            var currentPanel = _assetRows[virtualRow];
            for (var i = 0; i < currentPanel.transform.childCount; i++)
            {
                Destroy(currentPanel.transform.GetChild(i).gameObject);
            }

            var assetsCount = _assetCache.Count;

            float currentX = 0;
            for (var i = realRow * 3; i < Mathf.Min((realRow + 1) * 3, assetsCount); i++)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(KAssetEntryTemplate);
                currentPanel.AttachUIComponent(packageEntry.gameObject);

                var current = _assetCache[i];

                SetupAssetPackageEntry(packageEntry, current);

                var panel = packageEntry.gameObject.GetComponent<UIPanel>();
                const float panelSizeX = 310.0f;
                const float panelSizeY = 173.0f;
                panel.size = new Vector2(panelSizeX, panelSizeY);
                panel.relativePosition = new Vector3(currentX, 0.0f);
                panel.backgroundSprite = "";

                currentX += panel.size.x;

                var image = panel.Find<UITextureSprite>("Image");
                image.size = panel.size - new Vector2(4.0f, 2.0f);

                image.position = new Vector3(0.0f, image.position.y, image.position.z);

                var nameLabel = panel.Find<UILabel>("Name");
                nameLabel.isVisible = false;

                var newNameLabel = panel.AddUIComponent<UILabel>();
                newNameLabel.AlignTo(panel, UIAlignAnchor.TopLeft);
                newNameLabel.text = current.name;
                newNameLabel.zOrder = 7;
                newNameLabel.textColor = Color.white;
                newNameLabel.autoHeight = false;
                newNameLabel.autoSize = false;
                newNameLabel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                newNameLabel.textAlignment = UIHorizontalAlignment.Left;
                newNameLabel.verticalAlignment = UIVerticalAlignment.Top;
                newNameLabel.size = new Vector2(panelSizeX - 24.0f, panelSizeY - 2.0f);
                newNameLabel.relativePosition = new Vector3(24.0f, 4.0f, nameLabel.relativePosition.z);
                newNameLabel.isVisible = true;

                var delete = panel.Find<UIButton>("Delete");
                delete.size = new Vector2(24.0f, 24.0f);
                delete.relativePosition = new Vector3(panelSizeX - 28.0f, 2.0f, delete.relativePosition.z);
                delete.zOrder = 7;

                var active = panel.Find<UICheckBox>("Active");
                active.relativePosition = new Vector3(4.0f, 4.0f, active.relativePosition.z);
                active.zOrder = 7;
                active.tooltip = "Activate/ deactivate asset";

                var favButton = panel.AddUIComponent<UIButton>();
                favButton.anchor = UIAnchorStyle.Bottom | UIAnchorStyle.Right;
                favButton.normalFgSprite = "InfoIconHealth";
                favButton.hoveredFgSprite = "InfoIconHealthHovered";
                favButton.pressedFgSprite = "InfoIconHealthPressed";
                favButton.focusedFgSprite = "InfoIconHealth";
                favButton.size = new Vector2(36.0f, 36.0f);
                favButton.relativePosition = new Vector3(panelSizeX - 42.0f, panelSizeY - 62.0f);

                var isFavorite = _config.favoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64);
                favButton.opacity = isFavorite ? 1.0f : 0.5f;
                favButton.zOrder = 7;
                favButton.tooltip = "Set/ unset favorite";

                favButton.eventClick += (uiComponent, param) =>
                {
                    if (_config.favoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64))
                    {
                        _config.favoriteAssets.Remove(packageEntry.publishedFileId.AsUInt64);
                        favButton.opacity = 0.5f;
                    }
                    else
                    {
                        _config.favoriteAssets.Add(packageEntry.publishedFileId.AsUInt64, true);
                        favButton.opacity = 1.0f;
                    }

                    SaveConfig();
                    ReIndexAssets(_assetCache);
                    SetAssetCountLabels();
                };

                var onOff = active.Find<UILabel>("OnOff");
                onOff.enabled = false;

                var view = panel.Find<UIButton>("View");
                view.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                view.zOrder = 7;
                view.size = new Vector2(32.0f, 32.0f);
                view.relativePosition = new Vector3(4.0f, panelSizeY - 34.0f, view.relativePosition.z);

                var share = panel.Find<UIButton>("Share");
                share.zOrder = 7;
                share.size = new Vector2(80.0f, 24.0f);
                share.textScale = 0.7f;
                share.relativePosition = new Vector3(4.0f + view.size.x, panelSizeY - 28.0f, share.relativePosition.z);
            }
        }

        private static void SetupAssetPackageEntry(PackageEntry packageEntry, Package.Asset asset)
        {
            packageEntry.entryName = string.Concat(asset.package.packageName, ".", asset.name, "\t(",
                        asset.type, ")");
            packageEntry.entryActive = asset.isEnabled;
            packageEntry.package = asset.package;
            packageEntry.asset = asset;
            packageEntry.publishedFileId = asset.package.GetPublishedFileID();
            packageEntry.RequestDetails();
        }

        public static void RefreshAssetsOnly()
        {
            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            RefreshAssets(categoryContainer);
        }

        public static void RefreshAssets(UITabContainer categoryContainer)
        {
            UITemplateManager.ClearInstances(KAssetEntryTemplate);

            RefreshStandardCategory(categoryContainer, "ColorCorrections", UserAssetType.ColorCorrection, KAssetEntryTemplate);
            PreCacheAssets();
            SortCachedAssets();
            SetAssetCountLabels();

            var y = 0.0f;

            for (var q = 0; q < 4; q++)
            {
                _assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += _assetRows[q].size.y + 2.0f;
            }

            _scrollPositionY = 0.0f;
            _maxScrollPositionY = (Mathf.Ceil(_assetCache.Count / 3.0f)) * (_assetRows[0].size.y + 2.0f);
            SetScrollBar(_maxScrollPositionY, _newAssetsPanel.size.y);

            DrawAssets(0, 0);
            DrawAssets(1, 1);
            DrawAssets(2, 2);
            DrawAssets(3, 3);
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