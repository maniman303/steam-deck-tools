using static CommonHelpers.Log;

namespace SteamController
{
    public partial class Context : IDisposable
    {
        public const double JoystickToMouseSensitivity = 1200;
        public const double PadToMouseSensitivity = 150;
        public const double PadToWhellSensitivity = 4;
        public const double ThumbToWhellSensitivity = 20;

        public Devices.SteamController Steam { get; private set; }
        public Devices.Xbox360Controller X360 { get; private set; }
        public Devices.KeyboardController Keyboard { get; private set; }
        public Devices.MouseController Mouse { get; private set; }

        public List<Profiles.Profile> Profiles { get; } = new List<Profiles.Profile>();
        public List<Managers.Manager> Managers { get; } = new List<Managers.Manager>();

        private int selectedProfile;

        public bool RequestEnable { get; set; } = true;
        public bool SteamUsesX360Controller { get; set; } = false;
        public bool SteamUsesSteamInput { get; set; } = false;

        public event Action<Profiles.Profile> ProfileChanged;
        public Action? SelectDefault;

        public bool Enabled
        {
            get { return RequestEnable; }
        }

        public Profiles.Profile? CurrentProfile
        {
            get
            {
                for (int i = 0; i < Profiles.Count; i++)
                {
                    var profile = Profiles[(selectedProfile + i) % Profiles.Count];
                    if (profile.Selected(this))
                        return profile;
                }

                return null;
            }
        }

        public Context()
        {
            Steam = new Devices.SteamController();
            X360 = new Devices.Xbox360Controller();
            Keyboard = new Devices.KeyboardController();
            Mouse = new Devices.MouseController();

            ProfileChanged += (_) => X360.Beep();
        }

        public void Dispose()
        {
            using (Steam) { }
            using (X360) { }
            using (Keyboard) { }
            using (Mouse) { }
        }

        public void Tick()
        {
            X360.Tick();

            foreach (var manager in Managers)
            {
                try
                {
                    manager.Tick(this);
                }
                catch (Exception e)
                {
                    TraceLine("Manager: {0}. Exception: {1}", manager, e);
                }
            }
        }

        public bool Update()
        {
            Steam.BeforeUpdate();
            X360.BeforeUpdate();
            Keyboard.BeforeUpdate();
            Mouse.BeforeUpdate();

            try
            {
                var profile = CurrentProfile;
                if (profile is not null)
                    profile.Run(this);

                return true;
            }
            catch (Exception e)
            {
                TraceLine("Controller: Exception: {0}", e);
                return false;
            }
            finally
            {
                Steam.Update();
                X360.Update();
                Keyboard.Update();
                Mouse.Update();
            }
        }

        public bool SelectProfile(String name)
        {
            lock (this)
            {
                for (int i = 0; i < Profiles.Count; i++)
                {
                    var profile = Profiles[i];
                    if (profile.Name != name)
                        continue;
                    if (!profile.Selected(this))
                        continue;

                    if (i != selectedProfile)
                    {
                        selectedProfile = i;
                        ProfileChanged(profile);
                    }
                    return true;
                }
            }

            return false;
        }

        public void SelectController()
        {
            var current = CurrentProfile;
            if (current is null)
                return;
            if (current.IsDesktop)
                SelectNext();
        }

        public bool SelectNext()
        {
            lock (this)
            {
                // Update selectedProfile index
                var current = CurrentProfile;
                if (current is null)
                    return false;
                selectedProfile = Profiles.IndexOf(current);

                for (int i = 1; i < Profiles.Count; i++)
                {
                    var idx = (selectedProfile + i) % Profiles.Count;
                    var profile = Profiles[idx];
                    if (profile.IsDesktop)
                        continue;
                    if (!profile.Selected(this))
                        continue;

                    selectedProfile = idx;
                    ProfileChanged(profile);
                    return true;
                }
            }

            return false;
        }

        public void BackToDefault()
        {
            if (SelectDefault is not null)
                SelectDefault();
        }
    }
}
