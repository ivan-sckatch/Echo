using System;
using UnityEngine;
namespace EchoGame.UI
{
    [Serializable] public class UISettingsData
    {
        public float Master = 1, Sensitivity = .1f, Scale = 1;
        public bool Animations = true, Fullscreen = true, VSync = true;
        public int Width = 1920, Height = 1080;
    }
    public static class UISettingsManager
    {
        const string Key = "ECHO.UI.Settings.v1";
        static UISettingsData current;
        static bool initialized;
        public static event Action Changed;
        public static void Initialize() { if (initialized) return; initialized = true; Apply(true); }
        public static UISettingsData Current
        {
            get
            {
                if (current == null)
                {
                    try { current = JsonUtility.FromJson<UISettingsData>(PlayerPrefs.GetString(Key, "")); } catch (ArgumentException) { }
                    if (current == null) current = new UISettingsData { Width = Screen.width, Height = Screen.height, Fullscreen = Screen.fullScreen, VSync = QualitySettings.vSyncCount > 0 };
                    Validate(current);
                }
                return current;
            }
        }
        static void Validate(UISettingsData data)
        {
            data.Master = Mathf.Clamp01(data.Master); data.Sensitivity = Mathf.Clamp(data.Sensitivity, .02f, .4f);
            data.Scale = Mathf.Clamp(data.Scale, .75f, 1.25f);
            data.Width = Mathf.Max(640, data.Width); data.Height = Mathf.Max(480, data.Height);
        }
        public static void Save(UISettingsData data, bool applyVideo = true)
        {
            Validate(data); current = data;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data)); PlayerPrefs.Save();
            Apply(applyVideo); Changed?.Invoke();
        }
        public static void Apply(bool video = false)
        {
            AudioListener.volume = Current.Master;
            QualitySettings.vSyncCount = Current.VSync ? 1 : 0;
            if (video && !Application.isEditor)
                Screen.SetResolution(Current.Width, Current.Height, Current.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        public static void ApplyTo(PlayerController player) { if (player) player.MouseSensitivity = Current.Sensitivity; }
    }
}
