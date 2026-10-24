using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class ScannerHUD : MonoBehaviour
    {
        public UIPanelMotion ScannerPanel, PhasePanel;
        public TMP_Text Status, ProgressLabel, NodeA, NodeB, Synchronization;
        public UISmoothFill Progress;
        bool scannerVisible, phaseVisible;
        public void Apply(ScannerUIData data)
        {
            bool active = data != null && data.Enabled; bool phase = data != null && (active || data.NodeAActive || data.NodeBActive || data.Overlap > 0 && !data.DoorUnlocked);
            if (phaseVisible != phase) { phaseVisible = phase; PhasePanel.Show(phase); }
            if (scannerVisible != active) { scannerVisible = active; ScannerPanel.Show(active); }
            if (data == null) return;
            Status.text = data.Activated ? "ФАЗОВЫЙ УЗЕЛ АКТИВИРОВАН" : data.Discovered ? "АНОМАЛИЯ ОБНАРУЖЕНА" : "СКАНИРОВАНИЕ";
            ProgressLabel.text = !data.HasTarget ? "Наведите луч на аномалию" : data.Discovered ? $"{data.Target} — {data.Activation * 100:0}%" : $"ОБНАРУЖЕНИЕ — {data.Discovery * 100:0}%";
            Progress.Set(data.Discovered ? data.Activation : data.Discovery);
            NodeA.text = "УЗЕЛ A: " + data.NodeAOwner + (data.NodeAActive ? " — АКТИВЕН" : "");
            NodeB.text = "УЗЕЛ B: " + data.NodeBOwner + (data.NodeBActive ? " — АКТИВЕН" : "");
            NodeA.color = data.NodeAActive ? UITheme.Cyan : UITheme.Muted; NodeB.color = data.NodeBActive ? UITheme.Cyan : UITheme.Muted;
            Synchronization.text = data.DoorUnlocked ? "СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА\nДВЕРЬ РАЗБЛОКИРОВАНА" : $"СИНХРОНИЗАЦИЯ: {data.Overlap:0.0} / {data.RequiredOverlap:0.0}";
        }
    }
}
