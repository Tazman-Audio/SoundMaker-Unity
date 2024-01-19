using UnityEngine;
using UnityEditor;

namespace Fabric.Player.PanelTypes
{
    public class FabricLogOutputPanel : FabricPanelEditor
    {
        [MenuItem("Window/SoundMaker/Log Output")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(FabricLogOutputPanel), false, "SoundMaker Log").Show();
        }
        protected override string GetPanelName() { return "Log Output"; }
    }
}
