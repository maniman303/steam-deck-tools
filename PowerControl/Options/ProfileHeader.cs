using CommonHelpers;
using PowerControl.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerControl.Options
{
    public static class ProfileHeader
    {
        public static Menu.MenuItemHeader Instance = new Menu.MenuItemHeader()
        {
            CurrentTitle = () => ProfilesController.CurrentGame,
            IsVisible = () => DeviceManager.IsDeckOnlyDisplay &&
                ProfilesController.CurrentGame != ProfilesController.DefaultName,
        };
    }
}
