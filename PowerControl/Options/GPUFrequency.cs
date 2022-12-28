using PowerControl.Helpers.AMD;

namespace PowerControl.Options
{
    public static class GPUFrequency
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "GPU",
            PersistentKey = Settings.Default.EnableExperimentalFeatures ? "GPUFreq" : null,
            Options = { "Default", "400MHz", "800MHz", "1200MHz", "1600MHz" },
            ApplyDelay = 1000,
            Visible = VangoghGPU.IsSupported,
            ActiveOption = "?",
            ResetValue = () => { return "Default"; },
            ApplyValue = (selected) =>
            {
                if (!Settings.Default.AckAntiCheat(
                    Controller.TitleWithVersion,
                    "GPU",
                    "Changing GPU frequency requires kernel access for a short period. Leave the game if it uses anti-cheat protection.")
                )
                    return null;

                return CommonHelpers.Instance.WithGlobalMutex<string>(200, () =>
                {
                    using (var sd = VangoghGPU.Open())
                    {
                        if (sd is null)
                            return null;

                        if (string.IsNullOrEmpty(selected))
                        {
                            selected = "Default";
                        }

                        if (selected == "Default")
                        {
                            sd.HardMinGfxClock = 200;
                            return selected;
                        }

                        uint value = uint.Parse(selected.Replace("MHz", ""));
                        value = Math.Min(Math.Max(value, 200), 1600);

                        sd.HardMinGfxClock = value;
                        return selected;
                    }
                });
            }
        };
    }
}
