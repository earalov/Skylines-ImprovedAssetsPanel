using System;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using ImprovedAssetsPanel.Redirection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImprovedAssetsPanel.Detours
{
    [TargetType(typeof (ContentManagerPanel))]
    public class ContentManagerPanelDetour : ContentManagerPanel
    {
        private static object redirectLock = new Object();

        [RedirectMethod]
        public static void PerformSearch(ContentManagerPanel contentManagerPanel, string search)
        {
            var categoriesContainer =
                typeof (ContentManagerPanel).GetInstanceField(contentManagerPanel, "m_CategoriesContainer") as
                    UITabContainer;
            if (categoriesContainer == null)
            {
                Debug.LogWarning("Perform search: Categories container is null!");
                return;
            }
            var categories =
                typeof (ContentManagerPanel).GetInstanceField(contentManagerPanel, "m_Categories") as UIListBox;
            if (categories == null)
            {
                Debug.LogWarning("Perform search: Categories are null!");
                return;
            }
            var index = categories.selectedIndex;
            var assetsList = categoriesContainer.components[index].Find("Content");
            if (assetsList == null)
            {
                Debug.LogWarning("Perform search: AssetsList is null!");
                return;
            }
            var notAssetPanel = index != 2;
            if (notAssetPanel)
            {
                foreach (var item in assetsList.components)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    var component = item.GetComponent<PackageEntry>();
                    item.isVisible = component == null || component.IsMatch(search);
                }
            }
            else
            {
                ImprovedAssetsPanel.RedrawAssets(search);
            }
        }

        [RedirectMethod]
        private void ToggleActiveCategory(bool active)
        {
            var categoriesContainer =
                typeof (ContentManagerPanel).GetInstanceField(this, "m_CategoriesContainer") as
                    UITabContainer;
            if (categoriesContainer == null)
            {
                Debug.LogWarning("Perform search: Categories container is null!");
                return;
            }

            UIComponent uiComponent1 = categoriesContainer.components[categoriesContainer.selectedIndex].Find("Content");
            if (!((UnityEngine.Object) uiComponent1 != (UnityEngine.Object) null))
                return;
            var categories =
                typeof (ContentManagerPanel).GetInstanceField(this, "m_Categories") as UIListBox;
            if (categories == null)
            {
                Debug.LogWarning("Perform search: Categories are null!");
                return;
            }
            var index = categories.selectedIndex;
            var notAssetPanel = index != 2;
            if (notAssetPanel)
            {
                for (int i = 0; i < uiComponent1.components.Count; ++i)
                {
                    UIComponent uiComponent2 = uiComponent1.components[i];
                    if ((UnityEngine.Object) uiComponent2 != (UnityEngine.Object) null)
                    {
                        PackageEntry component = uiComponent2.GetComponent<PackageEntry>();
                        if ((UnityEngine.Object) component != (UnityEngine.Object) null)
                            component.entryActive = active;
                    }
                }
            }
            else
            {
                foreach (var guid in ImprovedAssetsPanel._displayedAssets)
                {
                    ImprovedAssetsPanel._assetCache[guid].isEnabled = active;
                }
                ImprovedAssetsPanel.RedrawAssets();
            }
        }

        [RedirectMethod]
        internal static void RefreshType(ContentManagerPanel panel, Package.AssetType assetType, UIComponent container, string template, bool onlyMain)
        {
            lock (redirectLock)
            {
                //begin mod
                ImprovedAssetsPanel.Initialize();
                if (assetType == UserAssetType.CustomAssetMetaData)
                {
                    ImprovedAssetsPanel.RefreshAssetsOnly();
                }
                else
                {
                    try
                    {
                        Redirector<ContentManagerPanelDetour>.Revert();
                        RefreshType(panel, assetType, container, template, onlyMain);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    finally
                    {
                        Redirector<ContentManagerPanelDetour>.Deploy();
                    }
                }
                //end mod 
            }

        }
    }
}