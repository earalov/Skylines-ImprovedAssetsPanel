using UnityEngine;

namespace ImprovedAssetsPanel
{
    public class UpdateHook : MonoBehaviour
    {
        public delegate void OnUnityUpdate();

        public delegate void OnUnityDestroy();

        public OnUnityUpdate onUnityUpdate = null;
        public OnUnityDestroy onUnityDestroy = null;
        public bool Once = true;

        void Update()
        {
            if (onUnityUpdate == null)
            {
                return;
            }
            onUnityUpdate();
            if (Once)
            {
                Destroy(this);
            }
        }

        void OnDestroy()
        {
            onUnityDestroy?.Invoke();
        }
    }
}
