using UnityEngine;
using UnityEditor;

namespace Fabric.Player.PanelTypes
{
    public class FabricEventLogPanel : FabricPanelEditor
    {
        [MenuItem("Window/SoundMaker/Profile/EventLog")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(FabricEventLogPanel), false, "SoundMaker EventLog").Show();
        }
        protected override string GetPanelName() { return "Event Log"; }
    }
}
