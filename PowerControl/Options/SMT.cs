using CommonHelpers;
using PowerControl.Helpers;

namespace PowerControl.Options
{
    public static class SMT
    {
        public static Menu.MenuItemWithOptions Instance = new Menu.MenuItemWithOptions()
        {
            Name = "SMT",
            ApplyDelay = 500,
            Options = { "No", "Yes" },
            ResetValue = () => { return "Yes"; },
            CurrentValue = delegate ()
            {
                if (!RTSS.IsOSDForeground(out var processId))
                    return null;
                if (!ProcessorCores.HasSMTThreads())
                    return null;

                return ProcessorCores.IsUsingSMT(processId.Value) ? "Yes" : "No";
            },
            ApplyValue = delegate (object selected)
            {
                if (!RTSS.IsOSDForeground(out var processId))
                    return null;
                if (!ProcessorCores.HasSMTThreads())
                    return null;

                ProcessorCores.SetProcessSMT(processId.Value, selected.ToString() == "Yes");

                return ProcessorCores.IsUsingSMT(processId.Value) ? "Yes" : "No";
            }
        };
    }
}
