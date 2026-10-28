using System.Collections.Generic;
using EchoGame.UI;
using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class Level04Controller : MonoBehaviour, IUIContextSource, IResearchLevel
    {
        public TimeCycleManager Cycles;
        public SecurityCameraController[] Cameras;
        public PhaseNode FirstNode, ExitNode;
        public DoorController FirstDoor, ExitDoor;
        public TMP_Text Controls;
        public Transform BaitA, BaitB, BaitC, RoomSafe, ExitSafe, JumpStart, JumpEnd;
        readonly List<CaptureTarget> targets = new List<CaptureTarget>();
        public bool IsResearch { get; private set; }
        public EchoLimitModel LimitModel => EchoLimitModel.ConcurrentEchoLimit;
        public int ConcurrentEchoCapacity => 1;
        public int ActiveEchoCount { get { int n=0; foreach(var e in Cycles.Echoes.Echoes) if(e.IsActive) n++; return n; } }
        public RecordingRejection CurrentRejection { get; private set; }
        public bool CanExit => ExitNode.HasActivated;
        public void EnterResearch()
        {
            Cycles.Restart(false, false); IsResearch = true; Cycles.Echoes.Evaluate(-1);
            FirstDoor.SetCommand(true); ExitDoor.SetCommand(true);
            FirstDoor.Tick(5, Cycles.Player.Feet); ExitDoor.Tick(5, Cycles.Player.Feet);
            foreach (var camera in Cameras)
            {
                if (camera.Display) camera.Display.text = camera.CameraId + " · ИССЛЕДОВАНИЕ";
                if (camera.Cone) camera.Cone.enabled = false;
            }
            Cycles.Game.SetState(GameState.Research); RefreshControls();
            Cycles.Notify("ИССЛЕДОВАНИЕ · Enter — начать цикл. Камеры и механизмы отключены. ПКМ — анализ.");
        }
        public void StartCycle() { IsResearch = false; Cycles.Restart(false); }
        public void ResearchTick(float dt)
        {
            Cycles.Player.Simulate(dt); Cycles.Scanning.Analyze(); RefreshControls();
        }
        void RefreshControls()
        {
            if (!Controls) return;
            Controls.text = IsResearch ? "ИССЛЕДОВАНИЕ · Enter — начать цикл · ПКМ — анализ · Tab — фрагменты" :
                (Cycles.Player.Immobilized?"ЗАХВАТ · ":"")+(Cycles.IsRecording?"R — завершить и сохранить":"R — начать запись")+" · T — повтор\nПКМ — сканер · Space — прыжок · Tab — анализ · Del — удалить фрагмент · Backspace — исследование";
        }
        public void Reject(string reason,float start,float end,int id)
        { CurrentRejection=new RecordingRejection {Reason=reason,Start=start,End=end,RecordingId=id}; Cycles.Notify(reason+$" · Эхо {id:00}: {start:0.0}–{end:0.0} с"); }
        public void TickCameras()
        {
            targets.Clear();
            // An existing replay has reached this frame before the living player arrives.
            foreach(var echo in Cycles.Echoes.Echoes)
            {
                var target=echo.GetComponent<CaptureTarget>(); if(!target) { target=echo.gameObject.AddComponent<CaptureTarget>(); target.Echo=echo; }
                if(echo.IsActive) targets.Add(target);
            }
            targets.Add(Cycles.Player.GetComponent<CaptureTarget>());
            foreach(var camera in Cameras) camera.Tick(Cycles.Elapsed,targets,Cycles);
        }
        public void Tick(float dt)
        {
            FirstDoor.SetCommand(FirstNode.HasActivated); ExitDoor.SetCommand(ExitNode.HasActivated);
            FirstDoor.Tick(dt,Cycles.Player.Feet); ExitDoor.Tick(dt,Cycles.Player.Feet);
            RefreshControls();
        }
        public void ResetMechanisms()
        {
            IsResearch = false;
            foreach(var camera in Cameras) camera.ResetCamera();
            var player=Cycles.Player.GetComponent<CaptureTarget>(); if(player) player.ResetCapture();
            foreach(var echo in Cycles.Echoes.Echoes) { var target=echo.GetComponent<CaptureTarget>(); if(target) target.ResetCapture(); }
            FirstDoor.ResetMechanism(); ExitDoor.ResetMechanism(); CurrentRejection=null;
        }
        public void DeleteLast() { Cycles.Echoes.RemoveLastFragment(); Cycles.Notify("Последний фрагмент удалён."); }
    }
}
