using UnityEditor;

namespace Fabric.Player
{
    [CustomEditor(typeof(FabricAudioObject))]
    public class FabricAudioObjectEditor : Editor
    {
        FabricAudioObject audioObject;

        private void OnEnable()
        {
            audioObject = target as FabricAudioObject;

            audioObject.OnGUISetup();
        }

        public override void OnInspectorGUI()
        {
            audioObject.OnGUIDraw();

            DrawDefaultInspector();
        }
    }
}
