using System.Diagnostics;
using PowerControl.Helpers;
using PowerControl.Helpers.AMD;

namespace PowerControl.Options
{
    public static class TDP
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "TDP",
            PersistentKey = Settings.Default.EnableExperimentalFeatures ? "TDP" : null,
            OptionsValues = delegate ()
            {
                List<string> options = new List<string>() { "3W", "4W", "5W", "6W", "7W", "8W", "10W", "12W", "15W" };

                if (Settings.Default.EnableExperimentalFeatures)
                {
                    options.Add("16W");
                    options.Add("17W");
                }

                return options.ToArray();
            },
            ApplyDelay = 1000,
            ResetValue = () => { return "15W"; },
            ActiveOption = "?",
            ApplyValue = (selected) =>
            {
                if (!Settings.Default.AckAntiCheat(
                    Controller.TitleWithVersion,
                    "TDP",
                    "Changing TDP requires kernel access for a short period. Leave the game if it uses anti-cheat protection.")
                )
                    return null;

                if (string.IsNullOrEmpty(selected))
                {
                    selected = "15W";
                }

                uint mW = uint.Parse(selected.Replace("W", "")) * 1000;
                mW = Math.Max(Math.Min(mW, 17000), 3000);

                if (VangoghGPU.IsSupported)
                {
                    return CommonHelpers.Instance.WithGlobalMutex<string>(200, () =>
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
                    uint stampLimit = mW / 10;

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
        };
    }
}
