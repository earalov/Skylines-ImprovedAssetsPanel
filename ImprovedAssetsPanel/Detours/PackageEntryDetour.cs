using System.Runtime.CompilerServices;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ImprovedAssetsPanel.Redirection;

namespace ImprovedAssetsPanel.Detours
{
    [TargetType(typeof(PackageEntry))]
    public class PackageEntryDetour : PackageEntry
    {
        [RedirectMethod]
        private void OnVisibilityChanged(UIComponent c, bool visible)
        {
            if (visible) {
                //begin mod
                if (this.m_DetailsPending)
                {
                    lock (this)
                    {
                        Steam.workshop.eventUGCRequestUGCDetailsCompleted -= this.OnDetailsReceived;
                        Steam.workshop.eventUGCRequestUGCDetailsCompleted += this.OnDetailsReceived;
                        Steam.workshop.RequestItemDetails(this.publishedFileId);
                    }
                }
                //end mod
                this.Update();
            }
            else
                this.m_WasVisible = false;
        }


        [RedirectMethod]
        public void RequestDetails()
        {
            if (this.publishedFileId != PublishedFileId.invalid)
            {
                this.m_DetailsPending = true;
                //begin mod
                if (!component.isVisible){
                    return;
                }
                lock (this)
                {
                    Steam.workshop.eventUGCRequestUGCDetailsCompleted -= this.OnDetailsReceived;
                    Steam.workshop.eventUGCRequestUGCDetailsCompleted += this.OnDetailsReceived;
                    Steam.workshop.RequestItemDetails(this.publishedFileId);
                }
                //end mod
            }
            else
            {
                this.GetLocalTimeInfo(ref this.m_WorkshopDetails.timeCreated, ref this.m_WorkshopDetails.timeUpdated);
                if (!((UnityEngine.Object) this.m_LastUpdateLabel != (UnityEngine.Object) null))
                    return;
                this.m_LastUpdateLabel.tooltip = this.FormatTimeInfo();
            }
        }

        [RedirectMethod]
        private void OnDetailsReceived(UGCDetails details, bool ioError) //TODO(earalov): cache details
        {
            if (!(this.publishedFileId == details.publishedFileId))
                return;
            this.m_DetailsPending = false;
            this.m_WorkshopDetails = details;
            if ((UnityEngine.Object)this.m_LastUpdateLabel != (UnityEngine.Object)null)
                this.m_LastUpdateLabel.tooltip = this.FormatTimeInfo();
            this.authorName = new Friend(details.creatorID).personaName;
            if ((UnityEngine.Object)this.m_ShareButton != (UnityEngine.Object)null)
                this.m_ShareButton.isVisible = Steam.steamID == this.m_WorkshopDetails.creatorID;
            //begin mod
            lock(this)
            {
                Steam.workshop.eventUGCRequestUGCDetailsCompleted -= this.OnDetailsReceived;
            }
            //end mod
            if (this.m_PluginInfo == null && this.m_Asset == (Package.Asset)null && this.m_Package == (Package)null)
                this.entryName = details.title;
            this.Update();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [RedirectReverse]
        private void GetLocalTimeInfo(ref uint created, ref uint updated)
        {
            UnityEngine.Debug.Log("Failed to redirect GetLocalTimeInfo()");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [RedirectReverse]
        private string FormatTimeInfo()
        {
            UnityEngine.Debug.Log("Failed to redirect FormatTimeInfo()");
            return null;
        }


    }
}