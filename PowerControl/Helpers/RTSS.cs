﻿using RTSSSharedMemoryNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerControl.Helpers
{
    internal static class RTSS
    {
        public static bool IsOSDForeground()
        {
            return IsOSDForeground(out _);
        }

        public static bool IsOSDForeground(out int? processId)
        {
            try
            {
                processId = (int?)GetTopLevelProcessId();
                if (processId is null)
                    return false;

                foreach (var app in OSD.GetAppEntries(AppFlags.MASK))
                {
                    if (app.ProcessId == processId)
                        return true;
                }

                return false;
            }
            catch
            {
                processId = null;
                return false;
            }
        }

        public static bool GetProfileProperty<T>(string propertyName, out T value)
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            value = default;
            try
            {
                if (!GetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length))
                    return false;

                value = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                handle.Free();
            }
        }

        public static bool SetProfileProperty<T>(string propertyName, T value)
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
                return SetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length);
            }
            catch
            {
                return false;
            }
            finally
            {
                handle.Free();
            }
        }

        public static uint EnableFlag(uint flag, bool status)
        {
            var current = SetFlags(~flag, status ? flag : 0);
            UpdateSettings();
            return current;
        }

        public static void UpdateSettings()
        {
            PostMessage(WM_RTSS_UPDATESETTINGS, IntPtr.Zero, IntPtr.Zero);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll")]
        public static extern uint SetFlags(uint dwAND, uint dwXOR);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern void LoadProfile(string profile = GLOBAL_PROFILE);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern void SaveProfile(string profile = GLOBAL_PROFILE);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern void DeleteProfile(string profile = GLOBAL_PROFILE);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern bool GetProfileProperty(string propertyName, IntPtr value, uint size);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern bool SetProfileProperty(string propertyName, IntPtr value, uint size);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern void ResetProfile(string profile = GLOBAL_PROFILE);

        [DllImport("C:\\Program Files (x86)\\RivaTuner Statistics Server\\RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        public static extern void UpdateProfiles();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private static uint? GetTopLevelProcessId()
        {
            var hWnd = GetForegroundWindow();
            var result = GetWindowThreadProcessId(hWnd, out uint processId);
            if (result != 0)
                return processId;
            return null;
        }

        private static void PostMessage(uint Msg, IntPtr wParam, IntPtr lParam)
        {
            var hWnd = FindWindow(null, "RTSS");
            if (hWnd == IntPtr.Zero)
                hWnd = FindWindow(null, "RivaTuner Statistics Server");

            if (hWnd != IntPtr.Zero)
                PostMessage(hWnd, Msg, wParam, lParam);
        }

        public const uint WM_APP = 0x8000;
        public const uint WM_RTSS_UPDATESETTINGS = WM_APP + 100;
        public const uint WM_RTSS_SHOW_PROPERTIES = WM_APP + 102;

        public const uint RTSSHOOKSFLAG_OSD_VISIBLE = 1;
        public const uint RTSSHOOKSFLAG_LIMITER_DISABLED = 4;
        public const string GLOBAL_PROFILE = "";
    }
}
