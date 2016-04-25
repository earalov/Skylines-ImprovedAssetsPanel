using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using UnityEngine;
using System.ComponentModel;
using ColossalFramework;
using ImprovedAssetsPanel.Detours;
using ImprovedAssetsPanel.Redirection;
using ImprovedAssetsPanel.Extensions;

namespace ImprovedAssetsPanel
{

    public class ImprovedAssetsPanel : MonoBehaviour
    {
        private const string KAssetEntryTemplate = "AssetEntryTemplate";

        private enum SortMode
        {
            [Description("Name")]
            Alphabetical = 0,
            [Description("File updated")]
            FileUpdated = 1,
            [Description("File created")]
            FileCreated = 2,
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

        private static UIPanel _buttonsPanel;
        private static UIPanel _sortModePanel;
        private static UILabel _sortModeLabel;
        private static UIButton _sortOrderButton;
        private static UILabel _sortOrderLabel;
        private static UIPanel _sortOptions;

        private static SortMode _sortMode = SortMode.Alphabetical;
        private static AssetType _filterMode = AssetType.All;
        private static SortOrder _sortOrder = SortOrder.Ascending;
        private static List<UIButton> _assetTypeButtons = new List<UIButton>();
        private static Dictionary<AssetType, UILabel> _assetTypeLabels = new Dictionary<AssetType, UILabel>();

        private static UIPanel _newAssetsPanel;
        private static UIPanel[] _assetRows;

        private static MultiMap<Guid, AssetType> _assetTypeIndex = new MultiMap<Guid, AssetType>();
        internal static Dictionary<Guid, Package.Asset> _assetCache = new Dictionary<Guid, Package.Asset>();
        internal static List<Guid> _displayedAssets = new List<Guid>();

        private static float _scrollPositionY;
        private static float _maxScrollPositionY;

        private static string GetSpriteNameForAssetType(AssetType assetType, bool isHovered = false)
        {
            var attribute = assetType.GetEnumDescription<AssetType, AssetTypeAttribute>();
            return isHovered ? attribute.SpriteName + " Hovered" : attribute.SpriteName;
        }

        private static bool _uiInitialized;

        public static void Initialize()
        {
            if (_uiInitialized)
            {
                return;
            }
            InitializeImpl();
            _uiInitialized = true;
        }

        public static void Bootstrap()
        {
            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject != null)
            {
                return;
            }
            syncObject = new GameObject("ImprovedAssetsPanelSyncObject");
            syncObject.AddComponent<UpdateHook>().onUnityDestroy = Revert;
            var updateHook = syncObject.AddComponent<UpdateHook>();
            updateHook.Once = false;
            updateHook.onUnityUpdate = () =>
            {
                if (Singleton<LoadingManager>.instance.m_loadedEnvironment == null)
                {
                    if (!_uiInitialized)
                    {
                        var contentManagerPanelGameObject = GameObject.Find("(Library) ContentManagerPanel");
                        var contentManagerPanel = contentManagerPanelGameObject?.GetComponent<ContentManagerPanel>();
                        if (contentManagerPanel == null)
                        {
                            return;
                        }
                        var categoryContainerGameObject = GameObject.Find("CategoryContainer");
                        var categoryContainer = categoryContainerGameObject?.GetComponent<UITabContainer>();
                        if (categoryContainer == null)
                        {
                            return;
                        }
                        var mods = categoryContainer.Find("Assets");
                        var modsList = mods?.Find("Content");
                        if (modsList == null)
                        {
                            return;
                        }
                        var moarGroupObj = GameObject.Find("MoarGroup");
                        if (moarGroupObj == null)
                        {
                            return;
                        }
                        contentManagerPanelGameObject.AddComponent<UpdateHook>().onUnityUpdate = () =>
                        {
                            ContentManagerPanelDetour.RefreshType(contentManagerPanel, UserAssetType.CustomAssetMetaData, categoryContainer, "", true);
                        };
                    }
                }
                else
                {
                    Destroy(syncObject);
                }
                updateHook.Once = true;
            };

            if (Singleton<LoadingManager>.instance.m_loadedEnvironment != null)
            {
                return;
            }
            Configuration.LoadConfig();

            Redirector<ContentManagerPanelDetour>.Deploy();
        }

        public static void Revert()
        {
            _uiInitialized = false;
            Redirector<ContentManagerPanelDetour>.Revert();

            var categoryContainerObj = GameObject.Find("CategoryContainer");
            if (categoryContainerObj != null)
            {
                var categoryContainer = categoryContainerObj.GetComponent<UITabContainer>();
                var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");
                var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

                assetsList.verticalScrollbar = scrollbar;
                assetsList.isVisible = true;
            }

            if (_sortModePanel != null)
            {
                Destroy(_sortModePanel.gameObject);
                _sortModePanel = null;
            }
            if (_sortModeLabel != null)
            {
                Destroy(_sortModeLabel.gameObject);
                _sortModeLabel = null;
            }
            if (_sortOrderButton != null)
            {
                Destroy(_sortOrderButton.gameObject);
                _sortOrderButton = null;
            }
            if (_sortOrderLabel != null)
            {
                Destroy(_sortOrderLabel.gameObject);
                _sortOrderLabel = null;
            }
            if (_buttonsPanel != null)
            {
                Destroy(_buttonsPanel.gameObject);
                _buttonsPanel = null;
            }
            if (_sortOptions != null)
            {
                Destroy(_sortOptions.gameObject);
                _sortOptions = null;
            }
            if (_newAssetsPanel != null)
            {
                Destroy(_newAssetsPanel.gameObject);
                _newAssetsPanel = null;
            }
            _sortMode = SortMode.Alphabetical;
            _filterMode = AssetType.All;
            _sortOrder = SortOrder.Ascending;

            _assetRows = null;

            _assetTypeIndex = new MultiMap<Guid, AssetType>();
            _assetCache = new Dictionary<Guid, Package.Asset>();
            _displayedAssets = new List<Guid>();

            _assetTypeButtons = new List<UIButton>();
            _assetTypeLabels = new Dictionary<AssetType, UILabel>();

            var syncObject = GameObject.Find("ImprovedAssetsPanelSyncObject");
            if (syncObject != null)
            {
                Destroy(syncObject);
            }

        }


        private static void InitializeImpl()
        {
            var moarGroup = GameObject.Find("Assets").GetComponent<UIPanel>().Find<UIPanel>("MoarGroup");
            var uiView = FindObjectOfType<UIView>();

            var moarLabel = moarGroup.Find<UILabel>("Moar");
            var moarButton = moarGroup.Find<UIButton>("Button");

            moarGroup.position = new Vector3(moarGroup.position.x, -6.0f, moarGroup.position.z);

            moarLabel.isVisible = false;
            moarButton.isVisible = false;

            const float buttonHeight = 18.0f;

            _buttonsPanel = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _buttonsPanel.transform.parent = moarGroup.transform;
            _buttonsPanel.size = new Vector2(400.0f, buttonHeight * 2);

            var assetTypes = (AssetType[])Enum.GetValues(typeof(AssetType));

            var categoryContainer = GameObject.Find("CategoryContainer").GetComponent<UITabContainer>();
            var assetsList = categoryContainer.Find("Assets").Find<UIScrollablePanel>("Content");

            var scrollbar =
                assetsList
                    .transform.parent.GetComponent<UIComponent>()
                    .Find<UIScrollbar>("Scrollbar");

            _assetTypeLabels = new Dictionary<AssetType, UILabel>();
            _assetTypeButtons = new List<UIButton>();
            var count = 0;
            var columnCount = ((assetTypes.Length - 1) / 2) + ((assetTypes.Length - 1) % 2); //-1 because no LUT
            foreach (var assetType in assetTypes)
            {
                if (assetType == AssetType.ColorLut)
                {
                    continue;
                }

                count++;
                var buttonsRow = count <= columnCount ? 0 : 1;
                var button = _buttonsPanel.AddUIComponent(typeof(UIButton)) as UIButton;
                button.size = new Vector2(buttonHeight, buttonHeight);
                button.tooltip = assetType.ToString();

                button.focusedFgSprite = button.normalFgSprite;
                button.pressedFgSprite = button.normalFgSprite;
                switch (assetType)
                {
                    case AssetType.All:
                        button.text = "ALL";
                        break;
                    case AssetType.Unknown:
                        button.text = "N/A";
                        break;
                    default:
                        button.normalFgSprite = GetSpriteNameForAssetType(assetType);
                        button.hoveredFgSprite = GetSpriteNameForAssetType(assetType, true);
                        break;
                }
                button.textScale = 0.5f;
                button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
                button.scaleFactor = 1.0f;
                button.isVisible = true;

                button.transform.parent = _buttonsPanel.transform;
                button.AlignTo(_buttonsPanel, UIAlignAnchor.TopLeft);


                button.relativePosition = new Vector3((buttonHeight + 2.0f) * ((count - 1) % columnCount), (buttonHeight * buttonsRow) - 2.0f);


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
                    RedrawAssets();
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

            _sortOptions = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _sortOptions.transform.parent = moarGroup.transform;
            _sortOptions.size = new Vector2(200.0f, 24.0f);

            _sortModePanel = uiView.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _sortModePanel.gameObject.name = "AssetsSortMode";
            _sortModePanel.transform.parent = _sortOptions.transform;
            _sortModePanel.name = "AssetsSortMode";
            _sortModePanel.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortModePanel.size = new Vector2(100.0f, 24.0f);
            _sortModePanel.autoLayout = false;

            var sortModeDropDown = UIUtil.CreateDropDownForEnum<SortMode>(_sortModePanel, "SortModeDropDown");
            sortModeDropDown.size = new Vector2(100.0f, 24.0f);
            sortModeDropDown.relativePosition = new Vector3(0.0f, 0.0f, 0.0f);
            sortModeDropDown.eventSelectedIndexChanged += (component, value) =>
            {
                sortModeDropDown.enabled = false;
                _sortMode = (SortMode)value;
                RedrawAssets();
                sortModeDropDown.enabled = true;
            };
            _sortModeLabel = UIUtil.CreateLabel(_sortModePanel, "Sort by");
            _sortModeLabel.relativePosition = new Vector3(0.0f, -2.0f, 0.0f);


            _sortOrderButton = _sortModePanel.AddUIComponent<UIButton>();
            _sortOrderButton.transform.parent = _sortOptions.transform;
            _sortOrderButton.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortOrderButton.size = new Vector2(100.0f, 24.0f);
            _sortOrderButton.text = _sortOrder.GetEnumDescription<SortOrder, DescriptionAttribute>().Description;
            _sortOrderButton.textScale = 0.7f;
            _sortOrderButton.normalBgSprite = "ButtonMenu";
            _sortOrderButton.disabledBgSprite = "ButtonMenuDisabled";
            _sortOrderButton.hoveredBgSprite = "ButtonMenuHovered";
            _sortOrderButton.focusedBgSprite = "ButtonMenu";
            _sortOrderButton.pressedBgSprite = "ButtonMenuPressed";
            _sortOrderButton.AlignTo(_sortOptions, UIAlignAnchor.TopLeft);
            _sortOrderButton.relativePosition = new Vector3(100.0f, 0.0f);
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
                _sortOrderButton.text = _sortOrder.GetEnumDescription<SortOrder, DescriptionAttribute>().Description;
                RedrawAssets();
            };

            _sortOrderLabel = UIUtil.CreateLabel(_sortOrderButton, "Sort Order");
            _sortOrderLabel.relativePosition = new Vector3(0.0f, -2.0f, 0.0f);


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

                var rowsCount = (int)Mathf.Ceil(_displayedAssets.Count / 3.0f);

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

                if (Math.Abs(value - _scrollPositionY) < 0.001f)
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
                        label.text = Configuration.Config.FavoriteAssets.Count.ToString();
                        continue;
                }
                var count = _assetTypeIndex.Keys.Count(asset => _assetTypeIndex[asset].Contains(assetType));

                label.text = count.ToString();

            }
        }


        private static int RowCount => (int)Mathf.Ceil(_assetCache.Count / 3.0f);

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

        private static void IndexAssetType(Guid guid, Package.Asset asset)
        {
            if (_assetTypeIndex.Keys.Contains(guid))
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
                _assetTypeIndex.Add(guid, AssetType.Unknown);
                return;
            }

            var tags = customAssetMetaData.steamTags;
            foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
            {
                if (assetType == AssetType.All || assetType == AssetType.Favorite || assetType == AssetType.Unknown)
                {
                    continue;
                }
                if (ContainsTag(tags, assetType.GetEnumDescription<AssetType, AssetTypeAttribute>().SteamTag))
                {
                    _assetTypeIndex.Add(guid, assetType);
                }
            }
            if (_assetTypeIndex[guid].Count == 0)
            {
                _assetTypeIndex.Add(guid, AssetType.Unknown);
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

        private static void ReIndexAssets()
        {
            _assetTypeIndex.Clear();
            foreach (var kvp in _assetCache.ToList())
            {
                IndexAssetType(kvp.Key, kvp.Value);
            }
        }

        private static List<Guid> FilterAssetsByName(Dictionary<Guid, Package.Asset> assets, string searchString)
        {
            return assets.Where(kvp => kvp.Value.IsMatch(searchString)).Select(entry => entry.Key).ToList();
        }


        private static void SortDisplayedAssets()
        {

            Func<Package.Asset, Package.Asset, int> comparerLambda;
            var alphabeticalSort = false;

            switch (_sortMode)
            {
                case SortMode.Alphabetical:
                    comparerLambda = AssetExtensions.CompareNames;
                    alphabeticalSort = true;
                    break;
                case SortMode.FileUpdated:
                    comparerLambda = (a, b) => a.GetAssetLastModifiedDelta().CompareTo(b.GetAssetLastModifiedDelta());
                    break;
                case SortMode.FileCreated:
                    comparerLambda = (a, b) => a.GetAssetCreatedDelta().CompareTo(b.GetAssetCreatedDelta());
                    break;
                case SortMode.Active:
                    comparerLambda = (a, b) => b.isEnabled.CompareTo(a.isEnabled);
                    break;
                case SortMode.Favorite:
                    comparerLambda = (a, b) => b.IsFavorite().CompareTo(a.IsFavorite());
                    break;
                case SortMode.Location:
                    comparerLambda = (a, b) =>
                    {
                        var aIsWorkshop = a.package.packagePath.Contains("workshop");
                        var bIsWorkshop = b.package.packagePath.Contains("workshop");
                        if (aIsWorkshop && bIsWorkshop)
                        {
                            return 0;
                        }

                        if (aIsWorkshop)
                        {
                            return 1;
                        }

                        if (bIsWorkshop)
                        {
                            return -1;
                        }

                        return 0;
                    };
                    break;
                default:
                    return;
            }
            _displayedAssets.Sort(new FunctionalComparer<Guid>((a, b) =>
            {
                var diff = (_sortOrder == SortOrder.Ascending ? comparerLambda : (arg1, arg2) => -comparerLambda(arg1, arg2))(_assetCache[a], _assetCache[b]);
                return diff != 0 || alphabeticalSort ? diff : _assetCache[a].CompareNames(_assetCache[b]);

            }));
        }

        private static void DrawAssets(int virtualRow, int realRow)
        {
            if (virtualRow < 0 || virtualRow > 3)
            {
                Debug.LogError("DrawAssets(): virtualRow < 0 || virtualRow > 3 is true");
                return;
            }

            var numRows = (int)Mathf.Ceil(_displayedAssets.Count / 3.0f);
            if (realRow > numRows - 1)
            {
                return;
            }

            var currentPanel = _assetRows[virtualRow];
            for (var i = 0; i < currentPanel.transform.childCount; i++)
            {
                Destroy(currentPanel.transform.GetChild(i).gameObject);
            }

            var assetsCount = _displayedAssets.Count;

            float currentX = 0;
            for (var i = realRow * 3; i < Mathf.Min((realRow + 1) * 3, assetsCount); i++)
            {
                var packageEntry = UITemplateManager.Get<PackageEntry>(KAssetEntryTemplate);
                currentPanel.AttachUIComponent(packageEntry.gameObject);

                var asset = _assetCache[_displayedAssets[i]];
                SetupAssetPackageEntry(packageEntry, asset);

                var panel = packageEntry.gameObject.GetComponent<UIPanel>();
                const float panelSizeX = 310.0f;
                const float panelSizeY = 173.0f;
                panel.size = new Vector2(panelSizeX, panelSizeY);
                panel.relativePosition = new Vector3(currentX, 0.0f);
                panel.backgroundSprite = "";

                currentX += panel.size.x;


                var image = panel.Find<UITextureSprite>("Image");
                image.size = panel.size - new Vector2(4.0f, 2.0f);
                image.position = new Vector3(0.0f, image.position.y + 13.0f, image.position.z);


                var active = panel.Find<UICheckBox>("Active");
                active.relativePosition = new Vector3(4.0f, 4.0f, active.relativePosition.z);
                active.zOrder = 7;
                active.tooltip = "Activate/ deactivate asset";


                var onOff = active.Find<UILabel>("OnOff");
                onOff.text = "";
                onOff.size = new Vector2(24.0f, 24.0f);

                var nameLabel = panel.Find<UILabel>("Name");
                nameLabel.AlignTo(onOff, UIAlignAnchor.TopRight);
                nameLabel.relativePosition = new Vector3(2, 4);

                var steamTags = panel.Find<UILabel>("SteamTags");
                if (steamTags != null)
                {
                    steamTags.textScale = 0.7f;
                    steamTags.zOrder = 7;
                    steamTags.AlignTo(panel, UIAlignAnchor.TopLeft);
                    steamTags.width = panelSizeX;
                    steamTags.height = 10;
                    steamTags.textColor = Color.white;
                    steamTags.relativePosition = new Vector3(4.0f, 24.0f);
                }

                var lastUpdateLabel = panel.Find<UILabel>("LastUpdate");
                if (lastUpdateLabel != null)
                {
                    lastUpdateLabel.textScale = 0.7f;
                    lastUpdateLabel.AlignTo(panel, UIAlignAnchor.TopLeft);
                    lastUpdateLabel.zOrder = 7;
                    lastUpdateLabel.relativePosition = new Vector3(4.0f, 60.0f);
                }

                var delete = panel.Find<UIButton>("Delete");
                delete.size = new Vector2(24.0f, 24.0f);
                delete.relativePosition = new Vector3(panelSizeX - 28.0f, 2.0f, delete.relativePosition.z);
                delete.zOrder = 7;



                var favButton = panel.AddUIComponent<UIButton>();
                favButton.AlignTo(panel, UIAlignAnchor.TopLeft);
                favButton.normalFgSprite = "InfoIconHealth";
                favButton.hoveredFgSprite = "InfoIconHealthHovered";
                favButton.pressedFgSprite = "InfoIconHealthPressed";
                favButton.focusedFgSprite = "InfoIconHealth";
                favButton.size = new Vector2(36.0f, 36.0f);
                favButton.relativePosition = new Vector3(panelSizeX - 42.0f, panelSizeY - 62.0f);
                favButton.zOrder = 7;
                favButton.tooltip = "Set/ unset favorite";
                favButton.eventClick += (uiComponent, param) =>
                {
                    if (Configuration.Config.FavoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64))
                    {
                        Configuration.Config.FavoriteAssets.Remove(packageEntry.publishedFileId.AsUInt64);
                        favButton.opacity = 0.25f;
                    }
                    else
                    {
                        Configuration.Config.FavoriteAssets.Add(packageEntry.publishedFileId.AsUInt64, true);
                        favButton.opacity = 1.0f;
                    }

                    Configuration.SaveConfig();
                    ReIndexAssets();
                    SetAssetCountLabels();
                };

                var view = panel.Find<UIButton>("View");
                view.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
                view.zOrder = 7;
                view.size = new Vector2(32.0f, 32.0f);
                view.relativePosition = new Vector3(4.0f, panelSizeY - 34.0f, view.relativePosition.z);

                var share = panel.Find<UIButton>("Share");
                share.zOrder = 7;
                share.AlignTo(panel, UIAlignAnchor.TopLeft);
                share.size = new Vector2(80.0f, 24.0f);
                share.textScale = 0.7f;
                share.relativePosition = new Vector3(4.0f + view.size.x, panelSizeY - 28.0f, share.relativePosition.z);

                var styleStuff = panel.Find<UIPanel>("StyleStuff");
                if (styleStuff != null)
                {
                    styleStuff.size = new Vector2(0, 0);
                    styleStuff.AlignTo(panel, UIAlignAnchor.TopLeft);
                    styleStuff.relativePosition = new Vector3();

                    var button = styleStuff.Find<UIButton>("Button");
                    button.zOrder = 7;
                    button.AlignTo(styleStuff, UIAlignAnchor.TopLeft);
                    button.relativePosition = new Vector3(88.0f + view.size.x, panelSizeY - 28.0f, button.relativePosition.z);

                    var count = styleStuff.Find<UILabel>("StyleCount");
                    count.textScale = 0.7f;
                    count.zOrder = 7;
                    count.width = panelSizeX;
                    count.AlignTo(styleStuff, UIAlignAnchor.TopLeft);
                    count.relativePosition = new Vector3(4.0f, 36.0f, count.relativePosition.z);

                    var label = panel.Find<UILabel>("UILabel");
                    if (label != null)
                    {
                        label.Hide();
                    }
                }

                var isFavorite = Configuration.Config.FavoriteAssets.ContainsKey(packageEntry.publishedFileId.AsUInt64);
                favButton.opacity = isFavorite ? 1.0f : 0.25f;
                packageEntry.component.Show();
                packageEntry.RequestDetails();
            }
        }

        private static void SetupAssetPackageEntry(PackageEntry packageEntry, Package.Asset asset)
        {
            packageEntry.entryName = string.Concat(asset.package.packageName, ".", asset.name, "\t(", asset.type, ")");
            packageEntry.entryActive = asset.isEnabled;
            packageEntry.package = asset.package;
            packageEntry.asset = asset;
            packageEntry.publishedFileId = asset.package.GetPublishedFileID();
        }

        public static void RefreshAssetsOnly()
        {
            var assets = PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData).ToList().Where(asset => asset.isMainAsset).ToList();
            _assetCache.Clear();
            foreach (var asset in assets)
            {
                _assetCache.Add(Guid.NewGuid(), asset);
            }
            ReIndexAssets();
            SetAssetCountLabels();
            RedrawAssets();
        }

        internal static void RedrawAssets(string searchString = null)
        {
            foreach (var row in _assetRows)
            {
                for (int i = row.components.Count - 1; i >= 0; i--)
                {
                    UIComponent child = row.components[i];
                    row.RemoveUIComponent(child);
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            switch (_filterMode)
            {
                case AssetType.All:
                    _displayedAssets = FilterAssetsByName(_assetCache, searchString);
                    break;
                case AssetType.Favorite:
                    _displayedAssets = FilterAssetsByName(_assetCache.Where(
                        kvp => Configuration.Config.FavoriteAssets.ContainsKey(kvp.Value.package.GetPublishedFileID().AsUInt64)).ToDictionary(p => p.Key, p => p.Value), searchString);
                    break;
                default:
                    _displayedAssets = FilterAssetsByName(_assetCache.Where(
                        kvp => _assetTypeIndex[kvp.Key].Contains(_filterMode)).ToDictionary(p => p.Key, p => p.Value), searchString);
                    break;
            }
            SortDisplayedAssets();

            ScrollAssetsList(0.0f);
            var y = 0.0f;
            for (var q = 0; q < 4; q++)
            {
                _assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += _assetRows[q].size.y + 2.0f;
            }
            _scrollPositionY = 0.0f;
            _maxScrollPositionY = (Mathf.Ceil(_displayedAssets.Count / 3.0f)) * (_assetRows[0].size.y + 2.0f);
            SetScrollBar(_maxScrollPositionY, _newAssetsPanel.size.y);
            DrawAssets(0, 0);
            DrawAssets(1, 1);
            DrawAssets(2, 2);
            DrawAssets(3, 3);
        }
    }
}