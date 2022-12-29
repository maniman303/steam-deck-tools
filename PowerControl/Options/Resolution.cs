using PowerControl.Helpers;
using PowerControl.Helpers.AMD;

namespace PowerControl.Options
{
    public static class Resolution
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "Resolution",
            ApplyDelay = 1000,
            ResetValue = () =>
            {
                if (!GPUScaling.SafeResolutionChange && !Settings.Default.EnableExperimentalFeatures)
                    return null;
                return DisplayResolutionController.GetAllResolutions().Last().ToString();
            },
            OptionsValues = delegate ()
            {
                if (!Settings.Default.EnableOSDQuickSettings)
                {
                    return null;
                }

                var resolutions = DisplayResolutionController.GetAllResolutions();
                if (resolutions.Count() > 1)
                    return resolutions.Select(item => item.ToString()).ToArray();
                return null;
            },
            CurrentValue = delegate ()
            {
                if (!Settings.Default.EnableOSDQuickSettings)
                {
                    return null;
                }

                if (!GPUScaling.SafeResolutionChange && !Settings.Default.EnableExperimentalFeatures)
                    return null;
                return DisplayResolutionController.GetResolution().ToString();
            },
            ApplyValue = (selected) =>
            {
                if (!Settings.Default.EnableOSDQuickSettings)
                {
                    return null;
                }

                var selectedResolution = new DisplayResolutionController.DisplayResolution(selected);
                DisplayResolutionController.SetResolution(selectedResolution);
                // force refresh Refresh Rate
                RefreshRate.Instance.Update();
                // force reset and refresh of FPS limit
                FPSLimit.Instance.Reset();
                FPSLimit.Instance.Update();
                return DisplayResolutionController.GetResolution().ToString();
            }
        };
    }
}
