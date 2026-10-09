using UnityEngine;

namespace Unisza
{
    /// <summary>Which input device currently drives the character.</summary>
    public enum ControlMode
    {
        Keyboard = 0,
        Joystick = 1,
        ClickToMove = 2,
        FollowMouse = 3
    }

    /// <summary>
    /// Reads/writes the chosen scheme in PlayerPrefs. The stored int is range-checked
    /// because PlayerPrefs survives across builds: an enum value that was later
    /// removed or reordered would otherwise be cast blindly and misbehave.
    /// </summary>
    public static class ControlModePrefs
    {
        const string Key = "Unisza.ControlMode";

        public static ControlMode Default
        {
            get { return Application.isMobilePlatform ? ControlMode.Joystick : ControlMode.Keyboard; }
        }

        public static ControlMode Load()
        {
            int raw = PlayerPrefs.GetInt(Key, (int)Default);
            if (raw < 0 || raw > (int)ControlMode.FollowMouse) return Default;
            return (ControlMode)raw;
        }

        public static void Save(ControlMode mode)
        {
            PlayerPrefs.SetInt(Key, (int)mode);
            PlayerPrefs.Save();
        }
    }
}
