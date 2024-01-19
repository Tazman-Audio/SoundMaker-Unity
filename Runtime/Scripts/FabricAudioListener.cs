using System;
using UnityEditor;
using UnityEngine;

namespace Fabric.Player
{
    [ExecuteAlways]
    [AddComponentMenu("SoundMaker/AudioListener")]
    [RequireComponent(typeof(AudioListener))]
    public class FabricAudioListener : MonoBehaviour
    {
        int gameObjectID;
        Native.Position position = new Native.Position();
        Native.Vector pos = new Native.Vector();
        Native.Vector forward = new Native.Vector();
        Native.Vector up = new Native.Vector();

        void OnEnable()
        {
            gameObjectID = gameObject.GetInstanceID();

            if (FabricPlayer.Instance)
            {
                API.RegisterGameObject(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID, "AudioListener");
                API.SetListener(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID, pos, forward, up);
            }

#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
        }

        void OnDisable()
        {
            if (FabricPlayer.Instance)
            {
                API.UnregisterGameObject(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID);
            }

#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        private void OnAfterAssemblyReload()
        {
            var unityListener = GetComponent<AudioListener>();
            Debug.Assert(unityListener != null, "Soundmaker Listener game object requires a Unity AudioListener component!");

            if (unityListener == null) return;

            unityListener.enabled = false;
            unityListener.enabled = true;
        }
#endif

        void Update()
        {
            position.position.x = transform.position.x;
            position.position.y = transform.position.y;
            position.position.z = transform.position.z;

            if (FabricPlayer.Instance)
            {
                API.SetGameObjectPosition(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID, ref position);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (FabricPlayer.Instance)
            {
                API.Process(FabricPlayer.Instance.fabricPlayerInstanceId, data.Length / channels);

#if UNITY_EDITOR
                API.GUI.ProcessPlayerPanel(FabricPlayer.Instance.fabricPlayerInstanceId, data, data, data.Length / channels, channels);
#endif
            }
        }
    }
}
