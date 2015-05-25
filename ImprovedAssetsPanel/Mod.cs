using ICities;

namespace ImprovedAssetsPanel
{

    public class Mod : IUserMod
    {

        public string Name
        {
            get
            {
                ImprovedAssetsPanel.Bootstrap();
                return "ImprovedAssetsPanel [Fixed for v1.1]"; 
            }
        }

        public string Description
        {
            get { return "Redesigned assets list panel"; }
        }

    }
 
}
