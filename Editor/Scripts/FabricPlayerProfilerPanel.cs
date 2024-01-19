using UnityEngine;
using UnityEditor;

namespace Fabric.Player.PanelTypes
{
    public class FabricProfilerPanel : FabricPanelEditor
    {
        [MenuItem("Window/SoundMaker/Profile/Profiler")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(FabricProfilerPanel), false, "SoundMaker Profiler").Show();
        }
        protected override string GetPanelName() { return "Profiler"; }
    }
}
