using UnityEditor;

namespace Fabric.Player
{
    [CustomEditor(typeof(FabricEventTrigger))]
    public class FabricEventTriggerEditor : Editor
    {
        FabricEventTrigger eventTrigger;

        private void OnEnable()
        {
            eventTrigger = target as FabricEventTrigger;

            eventTrigger.OnGUISetup();
        }

        public override void OnInspectorGUI()
        {
            eventTrigger.OnGUIDraw();
        }
    }
    
    [CustomEditor(typeof(FabricEventTriggerProperties))]
    public class FabricEventTriggerPropertiesEditor : Editor
    {
        FabricEventTriggerProperties eventTriggerProperties;
        EventTriggerDataDrawGUI drawGUI = new EventTriggerDataDrawGUI();

        private void OnEnable()
        {
            eventTriggerProperties = target as FabricEventTriggerProperties;

            drawGUI.OnGUISetup(eventTriggerProperties);
        }

        public override void OnInspectorGUI()
        {
            drawGUI.OnGUIDraw();
        }
    }
}
