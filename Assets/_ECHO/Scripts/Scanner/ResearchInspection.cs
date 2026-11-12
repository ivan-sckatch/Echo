using UnityEngine;
namespace EchoGame
{
    // Descriptions are derived from the live mechanism, so timings cannot drift from gameplay.
    public static class ResearchInspection
    {
        public static bool Describe(Collider hit, TimeCycleManager cycles, out string title, out string action)
        {
            title = action = "";
            if (cycles.GeometryPuzzle)
            {
                var terminal = hit.GetComponentInParent<GeometryTerminal>();
                if (terminal)
                {
                    title = terminal.Label.ToUpperInvariant();
                    action = terminal.Handle ? terminal.SequenceOrder>0 ? "Рычаг последовательной цепи. Состояние показывает рукоять и свет; отсчёт появляется после включения." :
                        "Рычаг переключает питание механизма. По окончании отсчёта возвращается в исходное положение. Действие записывается Эхо." : $"F — подаёт сигнал на {terminal.Window:0} с. Действие записывается Эхо.";
                    return true;
                }
                var mechanism = hit.GetComponentInParent<GeometryMechanism>();
                if (mechanism)
                {
                    var level=cycles.GeometryPuzzle;
                    title=mechanism==level.Middle[1] ? "ПОДВИЖНАЯ ПЕРЕГОРОДКА" :
                        mechanism==level.Lower[2] || mechanism==level.Middle[2] || mechanism==level.Upper[2] ? "ПОДЪЁМНАЯ СЕКЦИЯ" : "ВЫДВИЖНОЙ МОСТ";
                    action = "Меняет положение по сигналу. Контрольная точка фиксирует завершённое движение."; return true;
                }
                var plate = hit.GetComponentInParent<PressurePlate>();
                if (plate) { title = "НАЖИМНАЯ ПЛАТФОРМА " + (plate==cycles.GeometryPuzzle.PlateA ? "A" : "B"); action = "Управляет мостом, пока на ней находится игрок или Эхо."; return true; }
                var phase = hit.GetComponentInParent<PhaseNode>();
                if (phase) { title = phase.DisplayName; action = $"ПКМ — активация {phase.ActivationSeconds:0.#} с. Подаёт сигнал на выходной шлюз."; return true; }
                if (hit.GetComponentInParent<DoorController>()==cycles.GeometryPuzzle.ExitDoor)
                { title="ВЫХОДНОЙ ШЛЮЗ";action="Открывается после активации финального фазового узла.";return true; }
                var checkpoints=cycles.GeometryPuzzle.Checkpoints;
                for(int i=0;i<checkpoints.CheckpointVisuals.Length;i++)
                    if(hit.GetComponent<Renderer>()==checkpoints.CheckpointVisuals[i])
                    { title="КОНТРОЛЬНАЯ ТОЧКА "+(i+1);action="Фиксирует завершённую геометрию секции и начинает новую временную петлю.";return true; }
            }
            var button = hit.GetComponentInParent<PulseButton>();
            if (button)
            {
                int index = System.Array.IndexOf(cycles.TimedPuzzle.Buttons, button) + 1;
                title = "ИМПУЛЬСНАЯ КНОПКА · " + index;
                action = "F — открывает дверь " + index + (button.Door.LatchUntilReset ? " до сброса." : $" на {button.Door.Window:0.#} с.");
                if (button.Isolation) action += " Запирает вход в эту камеру до сброса.";
                return true;
            }
            var camera = hit.GetComponentInParent<SecurityCameraController>();
            if (camera)
            {
                title = "КАМЕРА НАБЛЮДЕНИЯ · " + (System.Array.IndexOf(cycles.SecurityPuzzle.Cameras, camera) + 1);
                action = "Захватывает первую видимую цель: игрока или эхо. Игрок остаётся до сброса, эхо — до конца фрагмента.";
                return true;
            }
            var node = hit.GetComponentInParent<PhaseNode>();
            if (node && cycles.SecurityPuzzle)
            {
                bool exit = node == cycles.SecurityPuzzle.ExitNode;
                title = exit ? "ФАЗОВЫЙ УЗЕЛ ВЫХОДА" : "ФАЗОВЫЙ УЗЕЛ ПРОХОДА";
                action = $"В цикле: ПКМ — обнаружение {node.DiscoverySeconds:0.#} с, активация {node.ActivationSeconds:0.#} с. " +
                    (exit ? "Открывает выход до сброса." : "Открывает дверь прохода до сброса.");
                return true;
            }
            var door = hit.GetComponentInParent<DoorController>();
            if (!door) return false;
            if (cycles.TimedPuzzle)
            {
                for (int i = 0; i < cycles.TimedPuzzle.Doors.Length; i++)
                {
                    var timed = cycles.TimedPuzzle.Doors[i];
                    if (timed.Door != door) continue;
                    title = "ИМПУЛЬСНАЯ ДВЕРЬ · " + (i + 1);
                    action = $"Открывается кнопкой {i + 1}" + (timed.LatchUntilReset ? " до сброса." : $" на {timed.Window:0.#} с, затем закрывается.");
                    return true;
                }
                foreach (var chamber in cycles.TimedPuzzle.Chambers)
                    if (chamber.Entrance == door)
                    { title = "ВХОД В КАМЕРУ"; action = "Закрывается после нажатия кнопки внутри. Сброс снова открывает вход."; return true; }
            }
            if (cycles.SecurityPuzzle)
            {
                bool exit = door == cycles.SecurityPuzzle.ExitDoor;
                title = exit ? "ДВЕРЬ ВЫХОДА" : "ДВЕРЬ ПРОХОДА";
                action = "В цикле открывается фазовым узлом " + (exit ? "выхода" : "прохода") + " до сброса.";
                return true;
            }
            return false;
        }
    }
}
