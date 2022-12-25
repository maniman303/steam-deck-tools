﻿using CommonHelpers;
using ExternalHelpers;
using hidapi;
using Microsoft.Win32;
using PowerControl.External;
using PowerControl.Helpers;
using RTSSSharedMemoryNET;
using System.ComponentModel;
using System.Diagnostics;
using WindowsInput;

namespace PowerControl
{
    internal class Controller : IDisposable
    {
        public const String Title = "Power Control";
        public static readonly String TitleWithVersion = Title + " v" + Application.ProductVersion.ToString();
        public const int KeyPressRepeatTime = 400;
        public const int KeyPressNextRepeatTime = 90;

        private SynchronizationContext? context;
        System.Windows.Forms.Timer contextTimer;

        Container components = new Container();
        System.Windows.Forms.NotifyIcon notifyIcon;
        StartupManager startupManager = new StartupManager(Title);

        Menu.MenuRoot rootMenu = MenuStack.Root;
        OSD osd;
        System.Windows.Forms.Timer osdDismissTimer;
        bool isOSDToggled = false;

        hidapi.HidDevice neptuneDevice = new hidapi.HidDevice(0x28de, 0x1205, 64);
        SDCInput neptuneDeviceState = new SDCInput();
        DateTime? neptuneDeviceNextKey;
        System.Windows.Forms.Timer neptuneTimer;

        System.Windows.Forms.Timer neptuneQuickSettingsTimer;
        DateTime? neptuneQuickSettingsNextKey;

        ProfilesController profilesController;

        SharedData<PowerControlSetting> sharedData = SharedData<PowerControlSetting>.CreateNew();

        static Controller()
        {
            Dependencies.ValidateHidapi(TitleWithVersion);
            Dependencies.ValidateRTSSSharedMemoryNET(TitleWithVersion);
        }

        public Controller()
        {
            Instance.OnUninstall(() =>
            {
                startupManager.Startup = false;
            });

            Instance.RunOnce(TitleWithVersion, "Global\\PowerControl");
            Instance.RunUpdater(TitleWithVersion);

            if (Instance.WantsRunOnStartup)
                startupManager.Startup = true;

            InitializeDisplayContext();
            contextTimer?.Start();

            SystemEvents.DisplaySettingsChanged += DisplayChangesHandler;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip(components);

            rootMenu.Init();
            rootMenu.Visible = false;
            rootMenu.Update();
            rootMenu.CreateMenu(contextMenu);
            rootMenu.VisibleChanged += delegate { updateOSD(); };
            contextMenu.Items.Add(new ToolStripSeparator());

            if (startupManager.IsAvailable)
            {
                var startupItem = new ToolStripMenuItem("Run On Startup");
                startupItem.Checked = startupManager.Startup;
                startupItem.Click += delegate
                {
                    startupManager.Startup = !startupManager.Startup;
                    startupItem.Checked = startupManager.Startup;
                };
                contextMenu.Items.Add(startupItem);
            }

            var checkForUpdatesItem = contextMenu.Items.Add("&Check for Updates");
            checkForUpdatesItem.Click += delegate { Instance.RunUpdater(TitleWithVersion, true); };

            var helpItem = contextMenu.Items.Add("&Help");
            helpItem.Click += delegate { System.Diagnostics.Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev"); };
            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = contextMenu.Items.Add("&Exit");
            exitItem.Click += ExitItem_Click;

            notifyIcon = new System.Windows.Forms.NotifyIcon(components);
            notifyIcon.Icon = WindowsDarkMode.IsDarkModeEnabled ? Resources.traffic_light_outline_light : Resources.traffic_light_outline;
            notifyIcon.Text = TitleWithVersion;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenu;

            // Fix first time context menu position
            contextMenu.Show();
            contextMenu.Close();

            osdDismissTimer = new System.Windows.Forms.Timer(components);
            osdDismissTimer.Interval = 3000;
            osdDismissTimer.Tick += delegate (object? sender, EventArgs e)
            {
                if (!isOSDToggled)
                {
                    hideOSD();
                }
            };

            var osdTimer = new System.Windows.Forms.Timer(components);
            osdTimer.Tick += OsdTimer_Tick;
            osdTimer.Interval = 250;
            osdTimer.Enabled = true;

            profilesController = new ProfilesController();
            profilesController.Initialize();

            GlobalHotKey.RegisterHotKey(Settings.Default.MenuUpKey, () =>
            {
                if (!RTSS.IsOSDForeground())
                    return;
                rootMenu.Next(-1);
                setDismissTimer();
                dismissNeptuneInput();
            }, true);

            GlobalHotKey.RegisterHotKey(Settings.Default.MenuDownKey, () =>
            {
                if (!RTSS.IsOSDForeground())
                    return;
                rootMenu.Next(1);
                setDismissTimer();
                dismissNeptuneInput();
            }, true);

            GlobalHotKey.RegisterHotKey(Settings.Default.MenuLeftKey, () =>
            {
                if (!RTSS.IsOSDForeground())
                    return;
                rootMenu.SelectNext(-1);
                setDismissTimer();
                dismissNeptuneInput();
            });

            GlobalHotKey.RegisterHotKey(Settings.Default.MenuRightKey, () =>
            {
                if (!RTSS.IsOSDForeground())
                    return;
                rootMenu.SelectNext(1);
                setDismissTimer();
                dismissNeptuneInput();
            });

            GlobalHotKey.RegisterHotKey(Settings.Default.MenuToggle, () =>
            {
                isOSDToggled = !rootMenu.Visible;

                if (!RTSS.IsOSDForeground())
                    return;

                if (isOSDToggled)
                {
                    showOSD();
                }
                else
                {
                    hideOSD();
                }
            }, true);

            if (Settings.Default.EnableNeptuneController)
            {
                neptuneTimer = new System.Windows.Forms.Timer(components);
                neptuneTimer.Interval = 1000 / 60;
                neptuneTimer.Tick += NeptuneTimer_Tick;
                neptuneTimer.Enabled = true;

                neptuneQuickSettingsTimer = new System.Windows.Forms.Timer(components);
                neptuneQuickSettingsTimer.Interval = 1000 / 30;
                neptuneQuickSettingsTimer.Tick += NeptuneQuickSettingsTimer_Tick;
                neptuneQuickSettingsTimer.Enabled = true;

                neptuneDevice.OnInputReceived += NeptuneDevice_OnInputReceived;
                neptuneDevice.OpenDevice();
                neptuneDevice.BeginRead();
            }

            if (Settings.Default.EnableVolumeControls)
            {
                GlobalHotKey.RegisterHotKey("VolumeUp", () =>
                {
                    if (neptuneDeviceState.buttons5.HasFlag(SDCButton5.BTN_QUICK_ACCESS))
                        rootMenu.Select("Brightness");
                    else
                        rootMenu.Select("Volume");
                    rootMenu.SelectNext(1);
                    setDismissTimer();
                    dismissNeptuneInput();
                });

                GlobalHotKey.RegisterHotKey("VolumeDown", () =>
                {
                    if (neptuneDeviceState.buttons5.HasFlag(SDCButton5.BTN_QUICK_ACCESS))
                        rootMenu.Select("Brightness");
                    else
                        rootMenu.Select("Volume");
                    rootMenu.SelectNext(-1);
                    setDismissTimer();
                    dismissNeptuneInput();
                });
            }
        }

        private void OsdTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                notifyIcon.Text = TitleWithVersion + ". RTSS Version: " + OSD.Version;
                notifyIcon.Icon = WindowsDarkMode.IsDarkModeEnabled ? Resources.traffic_light_outline_light : Resources.traffic_light_outline;
            }
            catch
            {
                notifyIcon.Text = TitleWithVersion + ". RTSS Not Available.";
                notifyIcon.Icon = Resources.traffic_light_outline_red;
            }

            updateOSD();
        }

        private Task NeptuneDevice_OnInputReceived(hidapi.HidDeviceInputReceivedEventArgs e)
        {
            var input = SDCInput.FromBuffer(e.Buffer);

            var filteredInput = new SDCInput()
            {
                buttons0 = input.buttons0,
                buttons1 = input.buttons1,
                buttons2 = input.buttons2,
                buttons3 = input.buttons3,
                buttons4 = input.buttons4,
                buttons5 = input.buttons5
            };

            if (!neptuneDeviceState.Equals(filteredInput))
            {
                neptuneDeviceState = filteredInput;
                neptuneDeviceNextKey = null;
            }

            // Consume only some events to avoid under-running SWICD
            if (!neptuneDeviceState.buttons5.HasFlag(SDCButton5.BTN_QUICK_ACCESS))
                Thread.Sleep(50);

            return new Task(() => { });
        }

        private void dismissNeptuneInput()
        {
            neptuneDeviceNextKey = DateTime.UtcNow.AddDays(1);
        }

        private void NeptuneQuickSettingsTimer_Tick(object? sender, EventArgs e)
        {
            var input = neptuneDeviceState;
            bool isClicked = input.buttons5 == SDCButton5.BTN_QUICK_ACCESS;

            if (isClicked && neptuneQuickSettingsNextKey == null)
            {
                string? currentApplication;

                RTSS.IsOSDForeground(out _, out currentApplication);

                if (currentApplication == null || (currentApplication?.ToLower().Contains("playnite") ?? false))
                {
                    neptuneQuickSettingsNextKey = DateTime.Now;
                }
            }

            var time = neptuneQuickSettingsNextKey;
            if (!isClicked && time != null)
            {
                TimeSpan diff = DateTime.Now - (time ?? DateTime.Now);
                int ms = diff.Milliseconds;
                int s = diff.Seconds;
                int mn = diff.Minutes;

                if (ms < 1000 && s == 0 && mn == 0)
                {
                    var simulator = new InputSimulator();
                    simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_A);
                }

                neptuneQuickSettingsNextKey = null;
            }
        }

        private void NeptuneTimer_Tick(object? sender, EventArgs e)
        {
            var input = neptuneDeviceState;

            if (neptuneDeviceNextKey == null)
                neptuneDeviceNextKey = DateTime.UtcNow.AddMilliseconds(KeyPressRepeatTime);
            else if (neptuneDeviceNextKey < DateTime.UtcNow)
                neptuneDeviceNextKey = DateTime.UtcNow.AddMilliseconds(KeyPressNextRepeatTime);
            else
                return; // otherwise it did not yet trigger

            // Reset sequence: 3 dots + L4|R4|L5|R5
            if (input.buttons0 == SDCButton0.BTN_L5 &&
                input.buttons1 == SDCButton1.BTN_R5 &&
                input.buttons2 == 0 &&
                input.buttons3 == 0 &&
                input.buttons4 == (SDCButton4.BTN_L4 | SDCButton4.BTN_R4) &&
                input.buttons5 == SDCButton5.BTN_QUICK_ACCESS)
            {
                rootMenu.Show();
                rootMenu.Reset();
                notifyIcon.ShowBalloonTip(3000, TitleWithVersion, "Settings were reset to default.", ToolTipIcon.Info);
                return;
            }

            if (!neptuneDeviceState.buttons5.HasFlag(SDCButton5.BTN_QUICK_ACCESS) || !RTSS.IsOSDForeground())
            {
                // schedule next repeat far in the future
                dismissNeptuneInput();
                hideOSD();
                return;
            }

            rootMenu.Show();
            setDismissTimer(false);

            if (input.buttons1 != 0 || input.buttons2 != 0 || input.buttons3 != 0 || input.buttons4 != 0)
            {
                return;
            }
            else if (input.buttons0 == SDCButton0.BTN_DPAD_LEFT)
            {
                rootMenu.SelectNext(-1);
            }
            else if (input.buttons0 == SDCButton0.BTN_DPAD_RIGHT)
            {
                rootMenu.SelectNext(1);
            }
            else if (input.buttons0 == SDCButton0.BTN_DPAD_UP)
            {
                rootMenu.Next(-1);
            }
            else if (input.buttons0 == SDCButton0.BTN_DPAD_DOWN)
            {
                rootMenu.Next(1);
            }
        }

        private void setDismissTimer(bool enabled = true)
        {
            osdDismissTimer.Stop();
            if (enabled)
                osdDismissTimer.Start();
        }

        private void hideOSD()
        {
            if (!rootMenu.Visible)
                return;

            Trace.WriteLine("Hide OSD");
            rootMenu.Visible = false;
            osdDismissTimer.Stop();
            updateOSD();
        }

        private void showOSD()
        {
            if (rootMenu.Visible)
                return;

            Trace.WriteLine("Show OSD");
            rootMenu.Update();
            rootMenu.Visible = true;
            updateOSD();
        }

        public void updateOSD()
        {
            sharedData.SetValue(new PowerControlSetting()
            {
                Current = rootMenu.Visible ? PowerControlVisible.Yes : PowerControlVisible.No
            });

            if (!rootMenu.Visible)
            {
                osdClose();
                return;
            }

            try
            {
                // recreate OSD if index 0
                if (OSDHelpers.OSDIndex("Power Control") == 0 && OSD.GetOSDCount() > 1)
                    osdClose();
                if (osd == null)
                {
                    osd = new OSD("Power Control");
                    Trace.WriteLine("Show OSD");
                }
                osd.Update(rootMenu.Render(null));
            }
            catch (SystemException)
            {
            }
        }

        private void ExitItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void Dispose()
        {
            components.Dispose();
            osdClose();
        }

        private void osdClose()
        {
            try
            {
                if (osd != null)
                {
                    osd.Dispose();
                    Trace.WriteLine("Close OSD");
                }
                osd = null;
            }
            catch (SystemException)
            {
            }
        }

        private void InitializeDisplayContext()
        {
            DeviceManager.LoadDisplays();
            contextTimer = new System.Windows.Forms.Timer();
            contextTimer.Interval = 200;
            contextTimer.Tick += (_, _) =>
            {
                context = SynchronizationContext.Current;
                contextTimer.Stop();
            };
        }

        private void DisplayChangesHandler(object? sender, EventArgs e)
        {
            if (DeviceManager.RefreshDisplays())
            {
                context?.Post((object? state) =>
                {
                    rootMenu.Update();
                    Options.RefreshRate.Instance?.Reset();
                    Options.FPSLimit.Instance?.Reset();
                }, null);
            }
        }
    }
}
