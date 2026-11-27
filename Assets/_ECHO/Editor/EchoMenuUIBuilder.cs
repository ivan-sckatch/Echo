using EchoGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoGame.Editor
{
    public static partial class EchoUIBuilder
    {
        static readonly Color MenuAccent = new Color(.31f, .72f, .73f);
        static RectTransform MenuContent(string name, UIPanelMotion motion, Vector2 size)
        {
            var content = Rect(name, motion.transform);
            Place(content, Vector2.one * .5f, Vector2.zero, size);
            content.gameObject.AddComponent<UIMenuLayout>().DesignSize = size;
            return content;
        }
        static UIPanelMotion MenuGroup(string name, Transform parent, float dim)
        {
            var motion = Group(name, parent);
            var background = motion.gameObject.AddComponent<Image>();
            background.color = new Color(.018f, .025f, .03f, dim);
            background.raycastTarget = true;
            return motion;
        }
        static void Title(Transform parent, string title, float x, float y, float width, float height)
        {
            var graphic = Rect(title + " title", parent).gameObject.AddComponent<UIStrokeTitle>();
            graphic.Title = title; graphic.color = new Color(.9f, .93f, .94f); graphic.raycastTarget = false;
            Place(graphic.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
        }
        static TMP_Text MenuLabel(Transform parent, string title, int size, float x, float y, float width, float height, Color? color = null)
        {
            var t = Label(parent, title, size, x, y, width, height, color);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var shadow = t.GetComponent<Shadow>(); shadow.effectDistance = new Vector2(0, -1);
            shadow.effectColor = new Color(0, 0, 0, .28f);
            return t;
        }
        static Button TextButton(Transform parent, string title, float x, float y, float width = 480, int size = 34)
        {
            // Transparent target retains the full row as a pointer hit area.
            var image = Image(title, parent, Color.clear); image.raycastTarget = true;
            Place(image.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, 64));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var text = MenuLabel(image.transform, title, size, 28, 0, width - 28, 64);
            var marker = Image("Selection marker", image.transform, MenuAccent);
            Place(marker.rectTransform, new Vector2(0, .5f), new Vector2(0, 0), new Vector2(3, 32));
            marker.enabled = false;
            var feedback = image.gameObject.AddComponent<UITextButton>(); feedback.Label = text; feedback.Marker = marker;
            return button;
        }
        static MainMenuUI BuildMain(Transform root)
        {
            var motion = MenuGroup("Main menu", root, .04f);
            var menu = motion.gameObject.AddComponent<MainMenuUI>(); menu.Motion = motion;
            var content = MenuContent("Main composition", motion, new Vector2(1680, 960));
            Title(content, "ECHO", 36, 112, 440, 130);
            menu.Continue = TextButton(content, "Продолжить", 20, 300, 520);
            menu.NewGame = TextButton(content, "Новая игра", 20, 375, 520);
            menu.LevelSelect = TextButton(content, "Выбор уровня", 20, 450, 520);
            menu.Settings = TextButton(content, "Настройки", 20, 525, 520);
            menu.Quit = TextButton(content, "Выход", 20, 600, 520);
            return menu;
        }
        static PauseMenuUI BuildPause(Transform root)
        {
            var motion = MenuGroup("Pause", root, .56f);
            var menu = motion.gameObject.AddComponent<PauseMenuUI>(); menu.Motion = motion;
            var content = MenuContent("Pause composition", motion, new Vector2(900, 960));
            Title(content, "ПАУЗА", 178, 132, 544, 132);
            menu.Resume = TextButton(content, "Продолжить", 270, 390, 510);
            menu.Restart = TextButton(content, "Перезапустить цикл", 270, 465, 510);
            menu.Settings = TextButton(content, "Настройки", 270, 540, 510);
            menu.MainMenu = TextButton(content, "Главное меню", 270, 615, 510);
            var foot = MenuLabel(content, "Esc — вернуться в игру", 25, 160, 860, 580, 44, UITheme.Muted);
            foot.alignment = TextAlignmentOptions.Center;
            return menu;
        }
        static Slider ThinSlider(Transform parent, float x, float y, float min, float max)
        {
            var hit = Image("Slider", parent, Color.clear); hit.raycastTarget = true;
            Place(hit.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(440, 44));
            var track = Image("Track", hit.transform, new Color(.35f, .42f, .44f, .6f));
            track.rectTransform.anchorMin = new Vector2(0, .5f); track.rectTransform.anchorMax = new Vector2(1, .5f);
            track.rectTransform.sizeDelta = new Vector2(-12, 4); track.rectTransform.anchoredPosition = Vector2.zero;
            var fill = Image("Fill", track.transform, MenuAccent); Stretch(fill.rectTransform);
            var area = Rect("Handle area", hit.transform);
            area.anchorMin = new Vector2(0, .5f); area.anchorMax = new Vector2(1, .5f);
            area.sizeDelta = new Vector2(-12, 0); area.anchoredPosition = Vector2.zero;
            var handle = Image("Handle", area, UITheme.Text); handle.rectTransform.sizeDelta = new Vector2(10, 36);
            var slider = hit.gameObject.AddComponent<Slider>();
            slider.minValue = min; slider.maxValue = max; slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            return slider;
        }
        static Toggle ThinToggle(Transform parent, string title, float x, float y)
        {
            var toggle = Toggle(parent, title, x, y, 590);
            var box = toggle.targetGraphic as Image; box.color = Color.clear;
            foreach (bool vertical in new[] { false, true }) foreach (float side in new[] { 0f, 1f })
            {
                var border = Image("Checkbox border", box.transform, new Color(.64f, .7f, .72f));
                border.rectTransform.anchorMin = vertical ? new Vector2(side, 0) : new Vector2(0, side);
                border.rectTransform.anchorMax = vertical ? new Vector2(side, 1) : new Vector2(1, side);
                border.rectTransform.sizeDelta = vertical ? new Vector2(1.3f, 0) : new Vector2(0, 1.3f);
            }
            toggle.graphic.color = MenuAccent;
            var label = toggle.GetComponentInChildren<TMP_Text>(); label.fontSize = 29;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return toggle;
        }
        static SettingsMenuUI BuildSettings(Transform root)
        {
            var motion = MenuGroup("Settings", root, .66f);
            var menu = motion.gameObject.AddComponent<SettingsMenuUI>(); menu.Motion = motion;
            var content = MenuContent("Settings composition", motion, new Vector2(1740, 960));
            Title(content, "НАСТРОЙКИ", 430, 34, 880, 105);
            const float left = 150, right = 970;
            MenuLabel(content, "ЗВУК", 31, left, 208, 600, 46, MenuAccent);
            MenuLabel(content, "Общая громкость", 29, left, 280, 600, 48);
            menu.Master = ThinSlider(content, left, 344, 0, 1);
            menu.MasterValue = MenuLabel(content, "100%", 29, left + 478, 342, 130, 48);
            MenuLabel(content, "УПРАВЛЕНИЕ", 31, left, 508, 600, 48, MenuAccent);
            MenuLabel(content, "Чувствительность мыши", 29, left, 576, 620, 48);
            menu.Sensitivity = ThinSlider(content, left, 638, .02f, .4f);
            menu.SensitivityValue = MenuLabel(content, "0,10", 29, left + 478, 636, 130, 48);
            MenuLabel(content, "ВИДЕО", 31, right, 208, 600, 46, MenuAccent);
            menu.Fullscreen = ThinToggle(content, "Полноэкранный режим", right, 276);
            menu.VSync = ThinToggle(content, "Вертикальная синхронизация", right, 332);
            MenuLabel(content, "Разрешение", 25, right, 404, 235, 48, UITheme.Muted);
            menu.Resolution = Dropdown(content, right + 245, 402);
            menu.Resolution.GetComponent<Image>().color = Color.clear;
            menu.Resolution.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 52);
            menu.Resolution.captionText.fontSize = 28;
            // Reposition the dropdown arrow after narrowing the field.
            var arrow = menu.Resolution.transform.Find("...").GetComponent<TMP_Text>();
            arrow.text = "v"; arrow.rectTransform.anchoredPosition = new Vector2(308, 0);
            arrow.color = MenuAccent;
            var rule = Image("Resolution underline", menu.Resolution.transform, new Color(.55f, .62f, .65f));
            rule.rectTransform.anchorMin = Vector2.zero; rule.rectTransform.anchorMax = Vector2.right;
            rule.rectTransform.pivot = Vector2.zero; rule.rectTransform.sizeDelta = new Vector2(0, 1);
            MenuLabel(content, "ИНТЕРФЕЙС", 31, right, 508, 600, 48, MenuAccent);
            MenuLabel(content, "Масштаб UI", 29, right, 576, 600, 48);
            menu.Scale = ThinSlider(content, right, 638, .75f, 1.25f);
            menu.ScaleValue = MenuLabel(content, "100%", 29, right + 478, 636, 130, 48);
            menu.Animations = ThinToggle(content, "Анимации интерфейса", right, 710);
            menu.Back = TextButton(content, "Назад", 40, 860, 290, 36);
            menu.Apply = TextButton(content, "Применить", 1350, 860, 350, 36);
            menu.Feedback = MenuLabel(content, "", 22, 400, 866, 900, 54, UITheme.Muted);
            menu.Feedback.alignment = TextAlignmentOptions.Center;
            return menu;
        }
    }
}
