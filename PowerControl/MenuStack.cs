using CommonHelpers;
using PowerControl.Helpers;
using PowerControl.Helpers.AMD;
using System.Diagnostics;
using static PowerControl.Helpers.AMD.DCE;

namespace PowerControl
{
    internal class MenuStack
    {
        public static Menu.MenuRoot Root = new Menu.MenuRoot()
        {
            Name = String.Format("\r\n\r\nPower Control v{0}\r\n", Application.ProductVersion.ToString()),
            Items =
            {
                new Menu.MenuItemHeader()
                {
                    CurrentTitle = () => RTSS.GetCurrentGameName() ?? GameProfile.DefaultName,
                    IsVisible = () => GameProfilesController.IsSingleDisplay &&
                        GameProfilesController.CurrentGame != GameProfile.DefaultName,
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "Profile",
                    OptionsValues = delegate()
                    {
                        if (!GameProfilesController.IsSingleDisplay ||
                            GameProfilesController.CurrentGame == GameProfile.DefaultName)
                        {
                            return null;
                        }

                        return new string[]{ "Off", "On" };
                    },
                    CurrentValue = delegate()
                    {
                        if (!GameProfilesController.IsSingleDisplay ||
                            GameProfilesController.CurrentGame == GameProfile.DefaultName)
                        {
                            return null;
                        }

                        bool isProfile = GameProfilesController.CheckIfProfileExists(GameProfilesController.CurrentGame);

                        return isProfile ? "On" : "Off";
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (selected == "On")
                        {
                            GameProfilesController.CreateProfile(GameProfilesController.CurrentGame);
                        }

                        if (selected == "Off")
                        {
                            GameProfilesController.RemoveProfile(GameProfilesController.CurrentGame);
                        }

                        Root.Update();

                        return selected;
                    }

                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "Brightness",
                    Options = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 },
                    CycleOptions = false,
                    CurrentValue = delegate()
                    {
                        return Helpers.WindowsSettingsBrightnessController.Get(5.0);
                    },
                    ApplyValue = delegate(object selected)
                    {
                        Helpers.WindowsSettingsBrightnessController.Set((int)selected);

                        return Helpers.WindowsSettingsBrightnessController.Get(5.0);
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "Volume",
                    Options = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 },
                    CycleOptions = false,
                    CurrentValue = delegate()
                    {
                        try { return Helpers.AudioManager.GetMasterVolume(5.0); }
                        catch(Exception) { return null; }
                    },
                    ApplyValue = delegate(object selected)
                    {
                        try
                        {
                            Helpers.AudioManager.SetMasterVolumeMute(false);
                            Helpers.AudioManager.SetMasterVolume((int)selected);

                            return Helpers.AudioManager.GetMasterVolume(5.0);
                        }
                        catch(Exception)
                        {
                            // In some cases MasterVolume device is missing
                            return null;
                        }
                    }
                },
                new Menu.MenuItemSeparator(),
                new Menu.MenuItemWithOptions()
                {
                    Name = "Resolution",
                    ApplyDelay = 1000,
                    ResetValue = () => {
                        if (!GPUScaling.SafeResolutionChange && !Settings.Default.EnableExperimentalFeatures)
                            return null;
                        return DisplayResolutionController.GetAllResolutions().Last();
                    },
                    OptionsValues = delegate()
                    {
                        var resolutions = DisplayResolutionController.GetAllResolutions();
                        if (resolutions.Count() > 1)
                            return resolutions.Select(item => (object)item).ToArray();
                        return null;
                    },
                    CurrentValue = delegate()
                    {
                        if (!GPUScaling.SafeResolutionChange && !Settings.Default.EnableExperimentalFeatures)
                            return null;
                        return DisplayResolutionController.GetResolution();
                    },
                    ApplyValue = delegate(object selected)
                    {
                        DisplayResolutionController.SetResolution((DisplayResolutionController.DisplayResolution)selected);
                        // force refresh Refresh Rate
                        Root["Refresh Rate"].Update();
                        // force reset and refresh of FPS limit
                        Root["FPS Limit"].Reset();
                        Root["FPS Limit"].Update();
                        return DisplayResolutionController.GetResolution();
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "Refresh Rate",
                    Key = GameOptions.RefreshRate,
                    ApplyDelay = 1000,
                    ResetValue = () => { return DisplayResolutionController.GetRefreshRates().Max(); },
                    OptionsValues = delegate()
                    {
                        var refreshRates = DisplayResolutionController.GetRefreshRates();
                        if (refreshRates.Count() > 1)
                            return refreshRates.Select(item => (object)item).ToArray();
                        return null;
                    },
                    CurrentValue = delegate()
                    {
                        return DisplayResolutionController.GetRefreshRate();
                    },
                    ApplyValue = delegate(object selected)
                    {
                        DisplayResolutionController.SetRefreshRate((int)selected);
                        // force reset and refresh of FPS limit
                        Root["FPS Limit"].Reset();
                        Root["FPS Limit"].Update();
                        return DisplayResolutionController.GetRefreshRate();
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "FPS Limit",
                    Key = GameOptions.Fps,
                    ApplyDelay = 500,
                    ResetValue = () => { return "Off"; },
                    OptionsValues = delegate()
                    {
                        var refreshRate = DisplayResolutionController.GetRefreshRate();
                        return new object[]
                        {
                            refreshRate / 4, refreshRate / 2, refreshRate, "Off"
                        };
                    },
                    CurrentValue = delegate()
                    {
                        try
                        {
                            RTSS.LoadProfile();
                            if (RTSS.GetProfileProperty("FramerateLimit", out int framerate))
                                return (framerate == 0) ? "Off" : framerate;
                        }
                        catch
                        {
                        }
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        try
                        {
                            int framerate = 0;
                            if (selected != null && selected.ToString() != "Off")
                                framerate = (int)selected;

                            RTSS.LoadProfile();
                            if (!RTSS.SetProfileProperty("FramerateLimit", framerate))
                                return null;
                            if (!RTSS.GetProfileProperty("FramerateLimit", out framerate))
                                return null;
                            RTSS.SaveProfile();
                            RTSS.UpdateProfiles();
                            return (framerate == 0) ? "Off" : framerate;
                        }
                        catch
                        {
                        }
                        return null;
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "GPU Scaling",
                    ApplyDelay = 1000,
                    Options = Enum.GetValues<GPUScaling.ScalingMode>().Cast<object>().Prepend("Off").ToArray(),
                    CurrentValue = delegate()
                    {
                        if (!GPUScaling.IsSupported)
                            return null;
                        if (!GPUScaling.Enabled)
                            return "Off";
                        return GPUScaling.Mode;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!GPUScaling.IsSupported)
                            return null;

                        if (selected is GPUScaling.ScalingMode)
                            GPUScaling.Mode = (GPUScaling.ScalingMode)selected;
                        else
                            GPUScaling.Enabled = false;

                        // Since the RadeonSoftware will try to revert values
                        RadeonSoftware.Kill();

                        Root["Resolution"].Update();
                        Root["Refresh Rate"].Update();
                        Root["FPS Limit"].Reset();
                        Root["FPS Limit"].Update();

                        if (!GPUScaling.Enabled)
                            return "Off";
                        return GPUScaling.Mode;
                    }
                },
                #if DEBUG
                new Menu.MenuItemWithOptions()
                {
                    Name = "Sharpening",
                    ApplyDelay = 500,
                    Options = { "Off", "On" },
                    CurrentValue = delegate()
                    {
                        var value = ImageSharpening.Enabled;
                        if (value is null)
                            return null;
                        return value.Value ? "On" : "Off";
                    },
                    ApplyValue = delegate(object selected)
                    {
                        ImageSharpening.Enabled = (string)selected == "On";

                        var value = ImageSharpening.Enabled;
                        if (value is null)
                            return null;
                        return value.Value ? "On" : "Off";
                    }
                },
                #endif
                new Menu.MenuItemWithOptions()
                {
                    Name = "Colors",
                    ApplyDelay = 1000,
                    Options = Enum.GetValues<DCE.Mode>().Cast<object>().ToList(),
                    CurrentValue = delegate()
                    {
                        return DCE.Current;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (DCE.Current is null)
                            return null;

                        DCE.Current = (DCE.Mode)selected;
                        RadeonSoftware.Kill();
                        return DCE.Current;
                    }
                },
                new Menu.MenuItemSeparator(),
                new Menu.MenuItemWithOptions()
                {
                    Name = "TDP",
                    Options = { "3W", "4W", "5W", "6W", "7W", "8W", "10W", "12W", "15W" },
                    ApplyDelay = 1000,
                    ResetValue = () => { return "15W"; },
                    ActiveOption = "?",
                    ApplyValue = delegate(object selected)
                    {
                        uint mW = uint.Parse(selected.ToString().Replace("W", "")) * 1000;

                        if (VangoghGPU.IsSupported)
                        {
                            return Instance.WithGlobalMutex<object>(200, () =>
                            {
                                using (var sd = VangoghGPU.Open())
                                {
                                    if (sd is null)
                                        return null;

                                    sd.SlowTDP = mW;
                                    sd.FastTDP = mW;
                                }

                                return selected;
                            });
                        }
                        else
                        {
                            uint stampLimit = mW/10;

                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = "Resources/RyzenAdj/ryzenadj.exe",
                                ArgumentList = {
                                    "--stapm-limit=" + stampLimit.ToString(),
                                    "--slow-limit=" + mW.ToString(),
                                    "--fast-limit=" + mW.ToString(),
                                },
                                WindowStyle = ProcessWindowStyle.Hidden,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });

                            return selected;
                        }
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "GPU",
                    Options = { "Default", "400MHz", "800MHz", "1200MHz", "1600MHz" },
                    ApplyDelay = 1000,
                    Visible = VangoghGPU.IsSupported,
                    ActiveOption = "?",
                    ResetValue = () => { return "Default"; },
                    ApplyValue = delegate(object selected)
                    {
                        return Instance.WithGlobalMutex<object>(200, () =>
                        {
                            using (var sd = VangoghGPU.Open())
                            {
                                if (sd is null)
                                    return null;

                                if (selected.ToString() == "Default")
                                {
                                    sd.HardMinGfxClock = 200;
                                    return selected;
                                }

                                sd.HardMinGfxClock = uint.Parse(selected.ToString().Replace("MHz", ""));
                                return selected;
                            }
                        });
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "CPU",
                    Options = { "Default", "Power-Save", "Balanced", "Max" },
                    ApplyDelay = 1000,
                    ActiveOption = "?",
                    Visible = VangoghGPU.IsSupported,
                    ResetValue = () => { return "Default"; },
                    ApplyValue = delegate(object selected)
                    {
                        return Instance.WithGlobalMutex<object>(200, () =>
                        {
                            using (var sd = VangoghGPU.Open())
                            {
                                if (sd is null)
                                    return null;

                                switch(selected.ToString())
                                {
                                    case "Default":
                                        sd.MinCPUClock = 1400;
                                        sd.MaxCPUClock = 3500;
                                        break;

                                    case "Power-Save":
                                        sd.MinCPUClock = 1400;
                                        sd.MaxCPUClock = 1800;
                                        break;

                                    case "Balanced":
                                        sd.MinCPUClock = 2200;
                                        sd.MaxCPUClock = 2800;
                                        break;

                                    case "Max":
                                        sd.MinCPUClock = 3000;
                                        sd.MaxCPUClock = 3500;
                                        break;

                                    default:
                                        return null;
                                }
                                return selected;
                            }
                        });
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "SMT",
                    ApplyDelay = 500,
                    Options = { "No", "Yes" },
                    ResetValue = () => { return "Yes"; },
                    CurrentValue = delegate()
                    {
                        if (!RTSS.IsOSDForeground(out var processId))
                            return null;
                        if (!ProcessorCores.HasSMTThreads())
                            return null;

                        return ProcessorCores.IsUsingSMT(processId.Value) ? "Yes" : "No";
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!RTSS.IsOSDForeground(out var processId))
                            return null;
                        if (!ProcessorCores.HasSMTThreads())
                            return null;

                        ProcessorCores.SetProcessSMT(processId.Value, selected.ToString() == "Yes");

                        return ProcessorCores.IsUsingSMT(processId.Value) ? "Yes" : "No";
                    }
                },
                new Menu.MenuItemSeparator(),
                new Menu.MenuItemWithOptions()
                {
                    Name = "OSD",
                    ApplyDelay = 500,
                    OptionsValues = delegate()
                    {
                        return Enum.GetValues<OverlayEnabled>().Select(item => (object)item).ToArray();
                    },
                    CurrentValue = delegate()
                    {
                        if (SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                           return value.CurrentEnabled;
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                            return null;
                        value.DesiredEnabled =  (OverlayEnabled)selected;
                        if (!SharedData<OverlayModeSetting>.SetExistingValue(value))
                            return null;
                        return selected;
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "OSD Mode",
                    ApplyDelay = 500,
                    OptionsValues = delegate()
                    {
                        return Enum.GetValues<OverlayMode>().Select(item => (object)item).ToArray();
                    },
                    CurrentValue = delegate()
                    {
                        if (SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                           return value.Current;
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                            return null;
                        value.Desired = (OverlayMode)selected;
                        if (!SharedData<OverlayModeSetting>.SetExistingValue(value))
                            return null;
                        return selected;
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "OSD Kernel Drivers",
                    ApplyDelay = 500,
                    OptionsValues = delegate()
                    {
                        return Enum.GetValues<KernelDriversLoaded>().Select(item => (object)item).ToArray();
                    },
                    CurrentValue = delegate()
                    {
                        if (SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                           return value.KernelDriversLoaded;
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!SharedData<OverlayModeSetting>.GetExistingValue(out var value))
                            return null;
                        value.DesiredKernelDriversLoaded = (KernelDriversLoaded)selected;
                        if (!SharedData<OverlayModeSetting>.SetExistingValue(value))
                            return null;
                        return selected;
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "FAN",
                    ApplyDelay = 500,
                    OptionsValues = delegate()
                    {
                        return Enum.GetValues<FanMode>().Select(item => (object)item).ToArray();
                    },
                    CurrentValue = delegate()
                    {
                        if (SharedData<FanModeSetting>.GetExistingValue(out var value))
                           return value.Current;
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!SharedData<FanModeSetting>.GetExistingValue(out var value))
                            return null;
                        value.Desired = (FanMode)selected;
                        if (!SharedData<FanModeSetting>.SetExistingValue(value))
                            return null;
                        return selected;
                    }
                },
                new Menu.MenuItemWithOptions()
                {
                    Name = "Controller",
                    ApplyDelay = 500,
                    OptionsValues = delegate()
                    {
                        if (SharedData<SteamControllerSetting>.GetExistingValue(out var value))
                            return value.SelectableProfiles.SplitWithN();
                        return null;
                    },
                    CurrentValue = delegate()
                    {
                        if (SharedData<SteamControllerSetting>.GetExistingValue(out var value))
                            return value.CurrentProfile.Length > 0 ? value.CurrentProfile : null;
                        return null;
                    },
                    ApplyValue = delegate(object selected)
                    {
                        if (!SharedData<SteamControllerSetting>.GetExistingValue(out var value))
                            return null;
                        value.DesiredProfile = (String)selected;
                        if (!SharedData<SteamControllerSetting>.SetExistingValue(value))
                            return null;
                        return selected;
                    }
                }
            }
        };
    }
}
