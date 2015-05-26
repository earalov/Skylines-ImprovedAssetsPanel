using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using ColossalFramework;
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
            [Description("Asc.")]
            Ascending = 0,
            [Description("Desc.")]
            Descending = 1
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
            ColorLUT,
            Vehicle,
            Unknown,
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

        private static UIPanel filterButtons;
        private static UIPanel sortDropDown;
        private static UILabel sortLabel;
        private static UIPanel orderDropDown;
        private static UILabel orderLabel;
        private static UIPanel additionalOptions;

        private static SortMode sortMode = SortMode.Alphabetical;
        private static AssetType filterMode = AssetType.All;
        private static SortOrder sortOrder = SortOrder.Ascending;

        private static readonly string kAssetEntryTemplate = "AssetEntryTemplate";

        private static List<UIButton> assetTypeButtons;
        private static Dictionary<AssetType, UILabel> assetTypeLabels = new Dictionary<AssetType,UILabel>(); 

        private static UIPanel newAssetsPanel;
        private static UIPanel[] assetRows;

        private static Dictionary<Package.Asset, AssetType> _assetTypeCache = new Dictionary<Package.Asset, AssetType>();
        private static Dictionary<Package.Asset, string> _assetPathCache = new Dictionary<Package.Asset, string>();
        private static List<Package.Asset> _assetCache = new List<Package.Asset>();

        private static float scrollPositionY = 0.0f;
        private static float maxScrollPositionY = 0.0f;

        private static string GetSpriteNameForAssetType(AssetType assetType, bool hovered = false)
        {
            switch (assetType)
            {
                case AssetType.Favorite:
                    if (hovered) return "InfoIconHealthHovered";
                    return "InfoIconHealth";
                case AssetType.Building:
                    if (hovered) return "InfoIconOutsideConnectionsHovered";
                    return "InfoIconOutsideConnectionsPressed";
                case AssetType.Prop:
                    if (hovered) return "ToolbarIconPropsHovered";
                    return "ToolbarIconProps";
                case AssetType.Tree:
                    if (hovered) return "IconPolicyForestHovered";
                    return "IconPolicyForest";
                case AssetType.Intersection:
                    if (hovered) return "ThumbnailJunctionsCloverFocused";
                    return "ThumbnailJunctionsClover";
                case AssetType.Park:
                    if (hovered) return "ToolbarIconBeautificationHovered";
                    return "ToolbarIconBeautification";
                case AssetType.Electricity:
                    if (hovered) return "InfoIconElectricityHovered";
                    return "InfoIconElectricity";
                case AssetType.WaterAndSewage:
                    if (hovered) return "InfoIconWaterHovered";
                    return "InfoIconWater";
                case AssetType.Garbage:
                    if (hovered) return "InfoIconGarbageHovered";
                    return "InfoIconGarbage";
                case AssetType.Healthcare:
                    if (hovered) return "ToolbarIconHealthcareHovered";
                    return "ToolbarIconHealthcare";
                case AssetType.Deathcare:
                    if (hovered) return "ToolbarIconHealthcareFocused";
                    return "ToolbarIconHealthcareDisabled";
                case AssetType.FireDepartment:
                    if (hovered) return "InfoIconFireSafetyHovered";
                    return "InfoIconFireSafety";
                case AssetType.PoliceDepartment:
                    if (hovered) return "ToolbarIconPoliceHovered";
                    return "ToolbarIconPolice";
                case AssetType.Education:
                    if (hovered) return "InfoIconEducationHovered";
                    return "InfoIconEducation";
                case AssetType.Transport:
                    if (hovered) return "ToolbarIconPublicTransportHovered";
                    return "ToolbarIconPublicTransport";
                case AssetType.TransportBus:
                    if (hovered) return "SubBarPublicTransportBusHovered";
                    return "SubBarPublicTransportBus";
                case AssetType.TransportMetro:
                    if (hovered) return "SubBarPublicTransportMetroHovered";
                    return "SubBarPublicTransportMetro";
                case AssetType.TransportTrain:
                    if (hovered) return "SubBarPublicTransportTrainHovered";
                    return "SubBarPublicTransportTrain";
                case AssetType.TransportShip:
                    if (hovered) return "SubBarPublicTransportShipHovered";
                    return "SubBarPublicTransportShip";
                case AssetType.TransportPlane:
                    if (hovered) return "SubBarPublicTransportPlaneHovered";
                    return "SubBarPublicTransportPlane";
                case AssetType.UniqueBuilding:
                    if(hovered) return "InfoIconLevelHovered";
                    return "InfoIconLevelFocused";
                case AssetType.Monument:
                    if (hovered) return "ToolbarIconMonumentsHovered";
                    return "ToolbarIconMonuments";
                case AssetType.Residential:
                    if (hovered) return "InfoIconOutsideConnectionsHovered";
                    return "InfoIconOutsideConnectionsPressed";
                case AssetType.Commercial:
                    if (hovered) return "InfoIconOutsideConnectionsHovered";
                    return "InfoIconOutsideConnectionsPressed";
                case AssetType.Industrial:
                    if (hovered) return "InfoIconOutsideConnectionsHovered";
                    return "InfoIconOutsideConnectionsPressed";
                case AssetType.Office:
                    if (hovered) return "InfoIconOutsideConnectionsHovered";
                    return "InfoIconOutsideConnectionsPressed";
                case AssetType.Vehicle:
                    if (hovered) return "InfoIconTrafficCongestionHovered";
                    return "InfoIconTrafficCongestion";
            }

            return "";
        }

        private static RedirectCallsState state;

        public static void Bootstrap()
        {
            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject == null)
            {
                new GameObject("ImprovedAssetsPanelSyncObject").AddComponent<UpdateHook>().onUnityDestroy = () =>
                {
                    Revert();
                };
            }
            else
            {
                return;
            }

            new GameObject().AddComponent<ImprovedAssetsPanel>().name = "ImprovedAssetsPanel";

            LoadConfig();

            Initialize();

            state = RedirectionHelper.RedirectCalls
            (
                typeof(ContentManagerPanel).GetMethod("Refresh",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(ImprovedAssetsPanel).GetMethod("RefreshAssets",
                    BindingFlags.Static | BindingFlags.Public)
            );

            var ContentManagerPanel = GameObject.Find("(Library) ContentManagerPanel").GetComponent<ContentManagerPanel>();
            ContentManagerPanel.gameObject.AddComponent<UpdateHook>().onUnityUpdate = () =>
            {
                RefreshAssets();
            };
        }

        public static void Revert()
        {
            RedirectionHelper.RevertRedirect(typeof (ContentManagerPanel).GetMethod("Refresh",
                        BindingFlags.Instance | BindingFlags.NonPublic), state);

            UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            assetsList.verticalScrollbar = scrollbar;
            assetsList.isVisible = true;

            Destroy(sortDropDown.gameObject);
            Destroy(sortLabel.gameObject);
            Destroy(filterButtons.gameObject);
            Destroy(additionalOptions.gameObject);
            Destroy(newAssetsPanel.gameObject);

            filterButtons = null;
            additionalOptions = null;
            sortDropDown = null;
            sortLabel = null;
            sortMode = SortMode.Alphabetical;
            filterMode = AssetType.All;
            newAssetsPanel = null;
            assetRows = null;

            _assetTypeCache = new Dictionary<Package.Asset, AssetType>();
            _assetPathCache = new Dictionary<Package.Asset, string>();
            _assetCache = new List<Package.Asset>();

            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject == null)
            {
                Destroy(syncObject);
            }
        }


        private static UIDropDown InitializeDropDown<T>(UIPanel dropDownPanel, string name)
        {
            var dropdown = dropDownPanel.Find<UIDropDown>("ShadowsQuality");
            dropdown.name = name;
            dropdown.size = new Vector2(150.0f, 24.0f);
            dropdown.textScale = 0.8f;
            dropdown.relativePosition = new Vector3(0.0f, 8.0f, 0.0f);

            var sprite = dropdown.Find<UIButton>("Sprite");
            sprite.foregroundSpriteMode = UIForegroundSpriteMode.Scale;

            var enumValues = Enum.GetValues(typeof(T));
            dropdown.items = new string[enumValues.Length];

            int i = 0;
            foreach (var value in enumValues)
            {
                dropdown.items[i] = Util.GetEnumDescription<T>((T)value);
                i++;
            }
            return dropdown;
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

            sortDropDown = GameObject.Instantiate(shadows);
            sortDropDown.gameObject.name = "AssetsSortBy";
            sortDropDown.transform.parent = moarGroup.transform;
            sortDropDown.name = "AssetsSortBy";
            sortDropDown.Find<UILabel>("Label").isVisible = false;
            sortDropDown.size = new Vector2(150.0f, 24.0f);
            sortDropDown.autoLayout = false;

            sortLabel = uiView.AddUIComponent(typeof(UILabel)) as UILabel;
            sortLabel.transform.parent = sortDropDown.transform;
            sortLabel.text = "Sort by";
            sortLabel.AlignTo(sortDropDown, UIAlignAnchor.TopLeft);
            sortLabel.relativePosition = new Vector3(0.0f, -2.0f, 0.0f);
            sortLabel.textColor = Color.white;
            sortLabel.textScale = 0.6f;

            var dropdown = InitializeDropDown<SortMode>(sortDropDown, "SortByDropDown");
            dropdown.eventSelectedIndexChanged += (component, value) =>
            {
                sortMode = (SortMode)value;
                ScrollAssetsList(0.0f);
                RefreshAssets();
            };

            filterButtons = uiView.AddUIComponent(typeof (UIPanel)) as UIPanel;
            filterButtons.transform.parent = moarGroup.transform;
            filterButtons.size = new Vector2(600.0f, 32.0f);

            var assetTypes = (AssetType[])Enum.GetValues(typeof(AssetType));

            UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            assetTypeButtons = new List<UIButton>();

            float x = 0.0f;
            foreach (var assetType in assetTypes)
            {
                if (assetType == AssetType.Unknown || assetType == AssetType.ColorLUT)
                {
                    continue;
                }

                var button = uiView.AddUIComponent(typeof(UIButton)) as UIButton;
                button.size = new Vector2(20.0f, 20.0f);
                button.tooltip = assetType.ToString();

                if (assetType != AssetType.All)
                {
                    button.normalFgSprite = GetSpriteNameForAssetType(assetType);
                    button.hoveredFgSprite = GetSpriteNameForAssetType(assetType, true);
                    button.focusedFgSprite = button.normalFgSprite;
                    button.pressedFgSprite = button.normalFgSprite;
                }
                else if (assetType == AssetType.All)
                {
                    button.text = "ALL";
                }
                button.textScale = 0.5f;
                button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
                button.scaleFactor = 1.0f;
                button.isVisible = true;
                button.transform.parent = filterButtons.transform;
                button.AlignTo(filterButtons, UIAlignAnchor.TopLeft);
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

                button.eventClick += (component, param) =>
                {
                    filterMode = assetType;

                    //TODO(earalov): call these three lines only on bootstrap
                    assetsList.isVisible = false;
                    assetsList.verticalScrollbar = null;
                    newAssetsPanel.isVisible = true;

                    foreach (var item in assetTypeButtons)
                    {
                        item.opacity = 0.25f;
                    }

                    button.opacity = 1.0f;
                    ScrollAssetsList(0.0f);
                    RefreshAssets();
                };

                assetTypeButtons.Add(button);

                var label = uiView.AddUIComponent(typeof (UILabel)) as UILabel;
                label.text = "99";
                label.AlignTo(button, UIAlignAnchor.TopRight);
                label.relativePosition = new Vector3(16.0f, 0.0f, 0.0f);
                label.zOrder = 7;
                label.textScale = 0.5f;
                label.textColor = Color.white;
                assetTypeLabels.Add(assetType, label);
            }

            additionalOptions = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            additionalOptions.transform.parent = moarGroup.transform;
            additionalOptions.size = new Vector2(120.0f, 32.0f);

            var activateAll = additionalOptions.AddUIComponent<UIButton>();
            activateAll.size = new Vector2(110.0f, 16.0f);
            activateAll.text = "Activate all";
            activateAll.textScale = 0.7f;
            activateAll.normalBgSprite = "ButtonMenu";
            activateAll.disabledBgSprite = "ButtonMenuDisabled";
            activateAll.hoveredBgSprite = "ButtonMenuHovered";
            activateAll.focusedBgSprite = "ButtonMenu";
            activateAll.pressedBgSprite = "ButtonMenuPressed";
            activateAll.AlignTo(additionalOptions, UIAlignAnchor.TopLeft);
            activateAll.relativePosition = new Vector3(4.0f, 0.0f);
            activateAll.eventClick += (component, param) =>
            {
                foreach (var item in _assetCache)
                {
                    item.isEnabled = true;
                }

                RefreshAssets();
            };

            var deactivateAll = additionalOptions.AddUIComponent<UIButton>();
            deactivateAll.size = new Vector2(110.0f, 16.0f);
            deactivateAll.text = "Deactivate all";
            deactivateAll.textScale = 0.7f;
            deactivateAll.normalBgSprite = "ButtonMenu";
            deactivateAll.disabledBgSprite = "ButtonMenuDisabled";
            deactivateAll.hoveredBgSprite = "ButtonMenuHovered";
            deactivateAll.focusedBgSprite = "ButtonMenu";
            deactivateAll.pressedBgSprite = "ButtonMenuPressed";
            deactivateAll.AlignTo(additionalOptions, UIAlignAnchor.TopLeft);
            deactivateAll.relativePosition = new Vector3(4.0f, 18.0f);
            deactivateAll.eventClick += (component, param) =>
            {
                foreach (var item in _assetCache)
                {
                    item.isEnabled = false;
                }

                RefreshAssets();
            };

            assetsList.verticalScrollbar = null;
            assetsList.isVisible = false;

            newAssetsPanel = assetsList.transform.parent.GetComponent<UIComponent>().AddUIComponent<UIPanel>();
            newAssetsPanel.anchor = assetsList.anchor;
            newAssetsPanel.size = assetsList.size;
            newAssetsPanel.relativePosition = assetsList.relativePosition;
            newAssetsPanel.name = "NewAssetsList";
            newAssetsPanel.clipChildren = true;

            newAssetsPanel.eventMouseWheel += (component, param) =>
            {
                if (rowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = scrollPositionY;
                scrollPositionY -= param.wheelDelta * 80.0f;
                scrollPositionY = Mathf.Clamp(scrollPositionY, 0.0f, maxScrollPositionY-newAssetsPanel.size.y);

                ScrollRows(originalScrollPos - scrollPositionY);
                SwapRows();

                SetScrollBar(scrollPositionY);
            };

            float y = 0.0f;

            assetRows = new UIPanel[4];
            for (int q = 0; q < 4; q++)
            {
                assetRows[q] = newAssetsPanel.AddUIComponent<UIPanel>();
                assetRows[q].name = "AssetRow" + q;
                assetRows[q].size = new Vector2(1200.0f, 173.0f);;
                assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += assetRows[q].size.y;
            }

            scrollbar.eventMouseUp += (component, param) =>
            {
                if (!newAssetsPanel.isVisible)
                {
                    return;
                }

                if (scrollbar.value != scrollPositionY)
                {
                    if (rowCount <= 2)
                    {
                        return;
                    }

                    var originalScrollPos = scrollPositionY;
                    scrollPositionY = scrollbar.value;
                    scrollPositionY = Mathf.Clamp(scrollPositionY, 0.0f, maxScrollPositionY - newAssetsPanel.size.y);

                    if (scrollPositionY != originalScrollPos)
                    {
                        var viewSize = newAssetsPanel.size.y;

                        var realRowIndex = (int)Mathf.Floor((scrollPositionY / viewSize) * (viewSize / (assetRows[0].size.y+2.0f)));
                        var diff = scrollPositionY - realRowIndex * (assetRows[0].size.y+2.0f);

                        float _y = 0.0f;
                        for (int q = 0; q < 4; q++)
                        {
                            assetRows[q].relativePosition = new Vector3(0.0f, _y, 0.0f);
                            _y += assetRows[q].size.y + 2.0f;
                        }

                        var rowsCount = (int)Mathf.Ceil(_assetCache.Count / 3.0f);

                        for (int q = 0; q < Mathf.Min(rowsCount, 4); q++)
                        {
                            DrawAssets(q, realRowIndex + q);
                        }

                        ScrollRows(-diff);
                        SetScrollBar(scrollPositionY);
                    }
                }
            };

            scrollbar.eventValueChanged += (component, value) =>
            {
                if (Input.GetMouseButton(0))
                {
                    return;
                }

                if (!newAssetsPanel.isVisible)
                {
                    return;
                }

                if (value != scrollPositionY)
                {
                    if (rowCount <= 2)
                    {
                        return;
                    }

                    var originalScrollPos = scrollPositionY;
                    scrollPositionY = value;
                    scrollPositionY = Mathf.Clamp(scrollPositionY, 0.0f, maxScrollPositionY - newAssetsPanel.size.y);

                    var diff = Mathf.Clamp(scrollPositionY - originalScrollPos, -(assetRows[0].size.y + 4), assetRows[0].size.y + 4);
                    scrollPositionY = originalScrollPos + diff;
                    ScrollRows(-diff);
                    SwapRows();

                    SetScrollBar(scrollPositionY);
                }
            };
        }

        private static void SetAssetCountLabels()
        {

            foreach (var assetType in assetTypeLabels.Keys)
            {
                var label = assetTypeLabels[assetType];
                if (assetType == AssetType.All)
                {
                    label.text = _assetTypeCache.Count.ToString();
                    continue;
                }
                if (assetType == AssetType.Favorite)
                {
                    label.text = config.favoriteAssets.Count.ToString();
                    continue;
                }
                int count = 0;

                foreach (var item in _assetTypeCache)
                {
                    if (item.Value == assetType)
                    {
                        count++;
                    }
                }
                label.text = count.ToString();
                
            }
        }
    

        private static int rowCount
        {
            get { return (int) Mathf.Ceil(_assetCache.Count/3.0f); }
        }

        private static void ScrollRows(float yOffset)
        {
            for (int i = 0; i < 4; i++)
            {
                var row = assetRows[i];
                row.relativePosition = new Vector3(row.relativePosition.x, row.relativePosition.y + yOffset, row.relativePosition.z);
            }
        }

        private static void SwapRows()
        {
            if (assetRows[0].relativePosition.y + assetRows[0].size.y + 2.0f < 0.0f)
            {
                assetRows[0].relativePosition = new Vector3(0.0f, assetRows[3].relativePosition.y + assetRows[3].size.y + 2.0f);
                var firstRealRow = (int)Mathf.Floor(scrollPositionY/(assetRows[0].size.y+2.0f));
                var lastRealRow = firstRealRow + 3;
                DrawAssets(0, lastRealRow);
                ShiftRowsUp();
            }
            else if (assetRows[0].relativePosition.y > 0.0f)
            {
                assetRows[3].relativePosition = new Vector3(0.0f, assetRows[0].relativePosition.y - assetRows[3].size.y - 2.0f);
                var firstRealRow = (int)Mathf.Floor(scrollPositionY / (assetRows[0].size.y + 2.0f));
                DrawAssets(3, firstRealRow);
                ShiftRowsDown();
            }
        }

        private static void ShiftRowsUp()
        {
            var tmp = assetRows[0];
            assetRows[0] = assetRows[1];
            assetRows[1] = assetRows[2];
            assetRows[2] = assetRows[3];
            assetRows[3] = tmp;
        }

        private static void ShiftRowsDown()
        {
            var tmp = assetRows[3];
            assetRows[3] = assetRows[2];
            assetRows[2] = assetRows[1];
            assetRows[1] = assetRows[0];
            assetRows[0] = tmp;
        }

        private static void ScrollAssetsList(float value)
        {
            UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.value = value;
        }

        private static void SetScrollBar(float maxValue, float scrollSize, float value = 0.0f)
        {
            UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
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
            UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            scrollbar.value = value;
        }

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

            var tags = customAssetMetaData.steamTags;

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

            if (ContainsTag(tags, "Water & Sewage"))
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

            if (ContainsTag(tags, "Fire Department"))
            {
                _assetTypeCache[asset] = AssetType.FireDepartment;
                return AssetType.FireDepartment;
            }

            if (ContainsTag(tags, "Police Department"))
            {
                _assetTypeCache[asset] = AssetType.PoliceDepartment;
                return AssetType.PoliceDepartment;
            }

            if (ContainsTag(tags, "Transport Bus"))
            {
                _assetTypeCache[asset] = AssetType.TransportBus;
                return AssetType.TransportBus;
            }

            if (ContainsTag(tags, "Transport Metro"))
            {
                _assetTypeCache[asset] = AssetType.TransportMetro;
                return AssetType.TransportMetro;
            }

            if (ContainsTag(tags, "Transport Train"))
            {
                _assetTypeCache[asset] = AssetType.TransportTrain;
                return AssetType.TransportTrain;
            }

            if (ContainsTag(tags, "Transport Ship"))
            {
                _assetTypeCache[asset] = AssetType.TransportShip;
                return AssetType.TransportShip;
            }

            if (ContainsTag(tags, "Transport Plane"))
            {
                _assetTypeCache[asset] = AssetType.TransportPlane;
                return AssetType.TransportPlane;
            }

            if (ContainsTag(tags, "Unique Building"))
            {
                _assetTypeCache[asset] = AssetType.UniqueBuilding;
                return AssetType.UniqueBuilding;
            }

            if (ContainsTag(tags, "Monument"))
            {
                _assetTypeCache[asset] = AssetType.Monument;
                return AssetType.Monument;
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

            if (customAssetMetaData.type == CustomAssetMetaData.Type.Vehicle)
            {
                _assetTypeCache[asset] = AssetType.Vehicle;
                return AssetType.Vehicle;
            }

            if (customAssetMetaData.type == CustomAssetMetaData.Type.Unknown)
            {
                _assetTypeCache[asset] = AssetType.Unknown;
                return AssetType.Unknown;
            }

            _assetTypeCache[asset] = AssetType.Unknown;
            return AssetType.Unknown;
        }

        private static bool ContainsTag(string[] haystack, string needle)
        {
            if (needle == null || haystack == null)
            {
                return false;
            }
            return haystack.Contains(needle);
        }

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
                _assetCache = assets;
            }
            else if (filterMode == AssetType.Favorite)
            {
                _assetCache = assets.FindAll(asset => config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64));
            }
            else
            {
                _assetCache = assets.FindAll(asset => GetAssetType(asset) == filterMode);
            }
        }

        private static void SortCachedAssets()
        {
            if (sortMode == SortMode.Alphabetical)
            {
                _assetCache.Sort((a, b) =>
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
                _assetCache.Sort((a, b) => GetAssetLastModifiedDelta(a).CompareTo(GetAssetLastModifiedDelta(b)));
            }
            else if (sortMode == SortMode.LastSubscribed)
            {
                _assetCache.Sort((a, b) => GetAssetCreatedDelta(a).CompareTo(GetAssetCreatedDelta(b)));
            }
            else if (sortMode == SortMode.Active)
            {
                var active = new List<Package.Asset>();
                var inactive = new List<Package.Asset>();
                foreach (var asset in _assetCache)
                {
                    if(asset.isEnabled) active.Add(asset);
                    else inactive.Add(asset);
                }

                _assetCache.Clear();
                foreach (var asset in active) _assetCache.Add(asset);
                foreach (var asset in inactive) _assetCache.Add(asset);
            }
            else if (sortMode == SortMode.Favorite)
            {
                var favorite = new List<Package.Asset>();
                var nonfavorite = new List<Package.Asset>();
                foreach (var asset in _assetCache)
                {
                    if (config.favoriteAssets.ContainsKey(asset.package.GetPublishedFileID().AsUInt64)) favorite.Add(asset);
                    else nonfavorite.Add(asset);
                }

                _assetCache.Clear();
                foreach (var asset in favorite) _assetCache.Add(asset);
                foreach (var asset in nonfavorite) _assetCache.Add(asset);
            }
            else if (sortMode == SortMode.Location)
            {
                _assetCache.Sort((a, b) =>
                {
                    var aIsWorkshop = a.package.packagePath.Contains("workshop");
                    var bIsWorkshop = b.package.packagePath.Contains("workshop");
                    if (aIsWorkshop && bIsWorkshop)
                    {
                        return a.name.CompareTo(b.name);
                    }

                    if (aIsWorkshop)
                    {
                        return 1;
                    }

                    if (bIsWorkshop)
                    {
                        return -1;
                    }

                    return a.name.CompareTo(b.name);
                });
            }
        }

        private static void DrawAssets(int virtualRow, int realRow)
        {
            if (virtualRow < 0 || virtualRow > 3)
            {
                Debug.LogError("DrawAssets(): virtualRow < 0 || virtualRow > 3 is true");
                return;
            }

            var numRows = (int)Mathf.Ceil(_assetCache.Count/3.0f);
            if (realRow > numRows - 1)
            {
                return;
            }

            UIPanel currentPanel = assetRows[virtualRow];
            for (int i = 0; i < currentPanel.transform.childCount; i++)
            {
                Destroy(currentPanel.transform.GetChild(i).gameObject);
            }

            var assetsCount = _assetCache.Count;

            float currentX = 0;
            for (int i = realRow*3; i < Mathf.Min((realRow+1)*3, assetsCount); i++)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(kAssetEntryTemplate);
                currentPanel.AttachUIComponent(packageEntry.gameObject);

                var current = _assetCache[i];

                packageEntry.entryName = String.Format("{0}.{1}\t({2})", current.package.packageName, current.name, current.type);

                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;
                packageEntry.publishedFileId = current.package.GetPublishedFileID();
                packageEntry.RequestDetails();

                var panel = packageEntry.gameObject.GetComponent<UIPanel>();
                var panelSizeX = 310.0f;
                var panelSizeY = 173.0f;
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

                var isFavorite = config.favoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64);
                favButton.opacity = isFavorite ? 1.0f : 0.5f;
                favButton.zOrder = 7;
                favButton.tooltip = "Set/ unset favorite";

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

        private static void RefreshColorCorrectionsList()
        {
            foreach (var current in PackageManager.FilterAssets(UserAssetType.ColorCorrection))
            {
                UITabContainer categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
                var assetsList = categoryContainer.Find("ColorCorrections").Find("Content");
                var packageEntry = UITemplateManager.Get<PackageEntry>(kAssetEntryTemplate);
                assetsList.AttachUIComponent(packageEntry.gameObject);
                packageEntry.entryName = string.Concat(current.package.packageName, ".", current.name, "\t(",
                    current.type, ")");
                packageEntry.entryActive = current.isEnabled;
                packageEntry.package = current.package;
                packageEntry.asset = current;

                packageEntry.RequestDetails();
            }
        }

        public static void RefreshAssets()
        {
            UITemplateManager.ClearInstances(kAssetEntryTemplate);

            RefreshColorCorrectionsList();
            PreCacheAssets();
            SortCachedAssets();
            SetAssetCountLabels();

            float y = 0.0f;

            for (int q = 0; q < 4; q++)
            {
                assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += assetRows[q].size.y + 2.0f;
            }

            scrollPositionY = 0.0f;
            maxScrollPositionY = (Mathf.Ceil(_assetCache.Count/3.0f))*(assetRows[0].size.y+2.0f);
            SetScrollBar(maxScrollPositionY, newAssetsPanel.size.y);

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