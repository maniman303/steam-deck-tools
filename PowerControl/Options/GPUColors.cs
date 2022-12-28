using PowerControl.Helpers.AMD;

namespace PowerControl.Options
{
    public static class GPUColors
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "Colors",
            PersistentKey = Settings.Default.EnableExperimentalFeatures ? "Colors" : null,
            ApplyDelay = 1000,
            Options = Enum.GetNames<DCE.Mode>(),
            CurrentValue = delegate ()
            {
                return DCE.Current.ToString();
            },
            ApplyValue = (selected) =>
            {
                if (DCE.Current is null)
                    return null;

                if (string.IsNullOrEmpty(selected))
                {
                    selected = DCE.Mode.Normal.ToString();
                }

                DCE.Current = Enum.Parse<DCE.Mode>(selected);
                RadeonSoftware.Kill();
                return DCE.Current.ToString();
            }
        };
    }
}
