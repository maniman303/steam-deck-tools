using CommonHelpers;

namespace PowerControl
{
    internal sealed class Settings : BaseSettings
    {
        public static readonly Settings Default = new Settings();

        public Settings() : base("Settings")
        {
            TouchSettings = true;
        }

        public string MenuUpKey
        {
            get { return Get("MenuUpKey", "Ctrl+Win+Numpad8"); }
            set { Set("MenuUpKey", value); }
        }

        public string MenuDownKey
        {
            get { return Get("MenuDownKey", "Ctrl+Win+Numpad2"); }
            set { Set("MenuDownKey", value); }
        }

        public string MenuLeftKey
        {
            get { return Get("MenuLeftKey", "Ctrl+Win+Numpad4"); }
            set { Set("MenuLeftKey", value); }
        }

        public string MenuRightKey
        {
            get { return Get("MenuRightKey", "Ctrl+Win+Numpad6"); }
            set { Set("MenuRightKey", value); }
        }

        public string MenuToggle
        {
            get { return Get("MenuToggle", "Shift+F11"); }
            set { Set("MenuToggle", value); }
        }

        public bool EnableNeptuneController
        {
            get { return Get<bool>("EnableNeptuneController", true); }
            set { Set("EnableNeptuneController", value); }
        }

        public bool EnableVolumeControls
        {
            get { return Get<bool>("EnableVolumeControls", true); }
            set { Set("EnableVolumeControls", value); }
        }

        public bool EnableOSDQuickSettings
        {
            get { return Get<bool>("EnableOSDQuickSettings", false); }
            set { Set("EnableOSDQuickSettings", value); }
        }

        public bool EnableQuickSettings
        {
            get { return Get<bool>("EnableQuickSettings", true); }
            set { Set("EnableQuickSettings", value); }
        }

        public bool EnableExperimentalFeatures
        {
            get { return Get<bool>("EnableExperimental", false); }
        }

        public bool AckAntiCheat(String title, String name, String message)
        {
            if (Get<bool>("AckAntiCheat" + name, false) && Settings.Default.EnableExperimentalFeatures)
                return true;

            Application.DoEvents();

            var result = MessageBox.Show(
                new Form { TopMost = true },
                String.Join("\n",
                    "WARNING!!!!",
                    "",
                    message,
                    "This might result in kicking from the application or even be banned.",
                    "",
                    "CLICK YES TO ACKNOWLEDGE?",
                    "CLICK NO TO LEARN MORE."
                ), title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
            {
                Set<bool>("AckAntiCheat" + name, true);
                return true;
            }

            try { System.Diagnostics.Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev/#anti-cheat-and-antivirus-software"); }
            catch { }
            return false;
        }
    }
}
