using UnityEngine;
using UnityEditor;

namespace Fabric.Player.PanelTypes
{
    public class FabricPlayerPanel : FabricPanelEditor
    {
        [MenuItem("Window/SoundMaker/Player")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(FabricPlayerPanel), false, "SoundMaker Player").Show();
        }
        protected override string GetPanelName() { return "FabricPlayer"; }
    }
}
