using PowerControl.Helper;
using PowerControl.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerControl.Options
{
    public static class ProfilesSwitch
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "Profile",
            PersistentKey = "ProfileSwitch",
            ApplyDelay = 0,
            OptionsValues = delegate ()
            {
                if (!DeviceManager.IsDeckOnlyDisplay ||
                    ProfilesController.CurrentGame == ProfilesController.DefaultName)
                {
                    return null;
                }

                return new string[] { "Off", "On" };
            },
            CurrentValue = delegate ()
            {
                string currentGame = ProfilesController.CurrentGame;

                if (!DeviceManager.IsDeckOnlyDisplay ||
                    currentGame == ProfilesController.DefaultName)
                {
                    return null;
                }

                bool isProfile = (new ProfileSettings(currentGame)).Exist;

                return isProfile ? "On" : "Off";
            },
            ApplyValue = delegate (string selected)
            {
                if (!DeviceManager.IsDeckOnlyDisplay)
                {
                    return null;
                }

                if (selected == "On")
                {
                    //GameProfilesController.CreateProfile(GameProfilesController.CurrentGame);
                }

                if (selected == "Off")
                {
                    //GameProfilesController.RemoveProfile(GameProfilesController.CurrentGame);
                }

                return selected;
            }
        };
    }
}
