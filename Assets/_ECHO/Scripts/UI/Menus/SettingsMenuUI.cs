using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class SettingsMenuUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Slider Master, Sensitivity, Scale;
        public Toggle Fullscreen, VSync, Animations;
        public TMP_Dropdown Resolution;
        public TMP_Text MasterValue, SensitivityValue, ScaleValue, Feedback;
        public Button Apply, Back;
        readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        public void Bind(UIManager ui)
        {
            foreach (var r in Screen.resolutions)
            { var size = new Vector2Int(r.width, r.height); if (r.width >= 1280 && r.height >= 720 && !resolutions.Contains(size)) resolutions.Add(size); }
            var current = new Vector2Int(UISettingsManager.Current.Width, UISettingsManager.Current.Height);
            if (!resolutions.Contains(current)) resolutions.Add(current);
            Resolution.ClearOptions(); var options = new List<string>();
            foreach (var r in resolutions) options.Add($"{r.x} × {r.y}"); Resolution.AddOptions(options);
            Master.onValueChanged.AddListener(_ => UpdateLabels()); Sensitivity.onValueChanged.AddListener(_ => UpdateLabels()); Scale.onValueChanged.AddListener(_ => UpdateLabels());
            Apply.onClick.AddListener(Save); Back.onClick.AddListener(ui.Back); LoadValues();
        }
        public void LoadValues()
        {
            var data = UISettingsManager.Current;
            Master.SetValueWithoutNotify(data.Master); Sensitivity.SetValueWithoutNotify(data.Sensitivity); Scale.SetValueWithoutNotify(data.Scale);
            Fullscreen.SetIsOnWithoutNotify(data.Fullscreen); VSync.SetIsOnWithoutNotify(data.VSync); Animations.SetIsOnWithoutNotify(data.Animations);
            int index = resolutions.IndexOf(new Vector2Int(data.Width, data.Height)); Resolution.SetValueWithoutNotify(Mathf.Max(0, index));
            Feedback.text = Application.isEditor ? "В Editor параметры экрана сохраняются для запуска игры." : "Изменения применяются кнопкой «Применить».";
            UpdateLabels();
        }
        void UpdateLabels() { MasterValue.text = $"{Master.value * 100:0}%"; SensitivityValue.text = Sensitivity.value.ToString("0.00"); ScaleValue.text = $"{Scale.value * 100:0}%"; }
        void Save()
        {
            Vector2Int resolution = resolutions[Mathf.Clamp(Resolution.value, 0, resolutions.Count - 1)];
            UISettingsManager.Save(new UISettingsData { Master = Master.value, Sensitivity = Sensitivity.value, Scale = Scale.value,
                Animations = Animations.isOn, Fullscreen = Fullscreen.isOn, VSync = VSync.isOn, Width = resolution.x, Height = resolution.y });
            Feedback.text = "Настройки сохранены";
        }
    }
}
