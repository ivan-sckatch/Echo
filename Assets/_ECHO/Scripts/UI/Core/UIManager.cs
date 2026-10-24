using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class UIManager : MonoBehaviour, ICancelHandler
    {
        public MonoBehaviour DataProvider;
        public bool IsMainMenu;
        public CanvasScaler Scaler;
        public HUDController HUD;
        public TimelineUI Timeline;
        public MainMenuUI Main;
        public PauseMenuUI Pause;
        public SettingsMenuUI Settings;
        public LevelSelectUI Levels;
        public LevelCompleteUI Complete;
        public UIState State { get; private set; }
        public IUIDataProvider Provider { get; private set; }
        public IUICommands Commands { get; private set; }
        UIState returnState;
        UISnapshot snapshot;
        bool hasState;
        void Start()
        {
            Provider = DataProvider as IUIDataProvider; Commands = DataProvider as IUICommands;
            if (!Scaler || !Settings || !Main || !Pause || !Levels || !Complete || !HUD || !Timeline)
            { Debug.LogError("ECHO UI: неполная конфигурация prefab UIManager.", this); enabled = false; return; }
            if (!IsMainMenu && (Provider == null || Commands == null))
            { Debug.LogError("ECHO UI: DataProvider должен реализовывать IUIDataProvider и IUICommands.", this); enabled = false; return; }
            Main.Bind(this); Pause.Bind(this); Settings.Bind(this); Levels.Bind(this); Complete.Bind(this);
            UISettingsManager.Initialize(); UISettingsManager.Changed += ApplySettings; ApplySettings();
            if (Provider != null)
            {
                Provider.Changed += OnData; Provider.Notice += HUD.Notice; Provider.Interaction += HUD.SetInteraction;
                Provider.InteractionOccurred += Timeline.AddInteraction; Provider.CycleRestarted += Timeline.ClearInteractions;
                Commands.EscapePressed += Escape;
                OnData(Provider.Current);
                if (!string.IsNullOrEmpty(Provider.CurrentNotice)) HUD.Notice(Provider.CurrentNotice, false);
            }
            else SetState(UIState.MainMenu);
        }
        void OnDestroy()
        {
            UISettingsManager.Changed -= ApplySettings;
            if (Provider == null) return;
            Provider.Changed -= OnData; Provider.Notice -= HUD.Notice; Provider.Interaction -= HUD.SetInteraction;
            Provider.InteractionOccurred -= Timeline.AddInteraction; Provider.CycleRestarted -= Timeline.ClearInteractions;
            if (Commands != null) Commands.EscapePressed -= Escape;
        }
        void ApplySettings()
        {
            Scaler.referenceResolution = new Vector2(1920, 1080) / UISettingsManager.Current.Scale;
            UISettingsManager.Apply();
            Commands?.SetSensitivity(UISettingsManager.Current.Sensitivity);
        }
        void OnData(UISnapshot data)
        {
            snapshot = data; HUD.Apply(data);
            UIState target = data.Phase == SessionPhase.Completed ? UIState.Completed : data.Phase == SessionPhase.Paused ? UIState.Paused : data.Phase == SessionPhase.Timeline ? UIState.Timeline : UIState.Gameplay;
            if ((!hasState || State != target) && (State != UIState.Settings || target != UIState.Paused)) SetState(target);
            if (State == UIState.Timeline) Timeline.Apply(data);
            if (State == UIState.Completed) Complete.Apply(data);
        }
        public void SetState(UIState state)
        {
            State = state;
            hasState = true;
            HUD.Show(state == UIState.Gameplay);
            Timeline.Motion.Show(state == UIState.Timeline);
            Main.Motion.Show(state == UIState.MainMenu);
            Pause.Motion.Show(state == UIState.Paused);
            Settings.Motion.Show(state == UIState.Settings);
            Levels.Motion.Show(state == UIState.LevelSelect);
            Complete.Motion.Show(state == UIState.Completed);
            if (state == UIState.Timeline && snapshot != null) Timeline.Apply(snapshot);
            // Select the first enabled control for keyboard navigation.
            var root = state == UIState.MainMenu ? Main.transform : state == UIState.Paused ? Pause.transform :
                state == UIState.Settings ? Settings.transform : state == UIState.LevelSelect ? Levels.transform : state == UIState.Completed ? Complete.transform : null;
            if (EventSystem.current)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (root != null) foreach (var selectable in root.GetComponentsInChildren<Selectable>())
                    if (selectable.IsInteractable()) { EventSystem.current.SetSelectedGameObject(selectable.gameObject); break; }
            }
        }
        public void OpenSettings() { returnState = State; Settings.LoadValues(); SetState(UIState.Settings); }
        public void OpenLevels() => SetState(UIState.LevelSelect);
        public void Back() => SetState(State == UIState.Settings ? returnState : UIState.MainMenu);
        public void Escape()
        {
            if (State == UIState.Settings || State == UIState.LevelSelect) { Back(); return; }
            if (State == UIState.Completed || State == UIState.MainMenu) return;
            if (State == UIState.Paused || State == UIState.Timeline) Commands.Resume(); else Commands.Pause();
        }
        public void OnCancel(BaseEventData data) { if (IsMainMenu) { Escape(); data.Use(); } }
    }
}
