using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fabric.Player
{
    [ExecuteAlways]
    [AddComponentMenu("SoundMaker/AudioObject")]
    [RequireComponent(typeof(AudioSource))]
    public class FabricAudioObject : MonoBehaviour
    {
        public FabricAudioSourceProperties properties = null;

        AudioSource audioSource = null;
        int gameObjectID = -1;
        public int ID
        {
            get { return gameObjectID; }
        }

        public void OnEnable()
        {
            audioSource = GetComponent<AudioSource>();

            gameObjectID = gameObject.GetInstanceID();
            if (FabricPlayer.Instance)
            {
                API.RegisterGameObject(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID, gameObject.name);
            }

            if (audioSource && properties)
            {
                audioSource.clip = null;
                audioSource.outputAudioMixerGroup = properties.outputAudioMixerGroup;
                audioSource.velocityUpdateMode = properties.velocityUpdateMode;

                audioSource.ignoreListenerVolume = properties.ignoreListenerVolume;
                audioSource.playOnAwake = properties.playOnAwake;
                audioSource.ignoreListenerPause = properties.ignoreListenerPause;
                audioSource.bypassEffects = properties.bypassEffects;
                audioSource.bypassListenerEffects = properties.bypassListenerEffects;
                audioSource.bypassReverbZones = properties.bypassReverbZones;

                audioSource.spatialize = properties.spatialize;
                audioSource.spatializePostEffects = properties.spatializePostEffects;

                audioSource.mute = properties.mute;
                audioSource.volume = properties.volume;
                audioSource.pitch = properties.pitch;
                audioSource.loop = properties.loop;
                audioSource.panStereo = properties.panStereo;
                audioSource.spatialBlend = properties.spatialBlend;
                audioSource.reverbZoneMix = properties.reverbZoneMix;
                audioSource.dopplerLevel = properties.dopplerLevel;
                audioSource.spread = properties.spread;
                audioSource.priority = properties.priority;

                audioSource.rolloffMode = properties.rolloffMode;
                audioSource.minDistance = properties.minDistance;
                audioSource.maxDistance = properties.maxDistance;
            }

            audioSource.Play();
        }

        public void OnDisable()
        {
            if (FabricPlayer.Instance)
            {
                API.UnregisterGameObject(FabricPlayer.Instance.fabricPlayerInstanceId, gameObjectID);
            }

            audioSource.Stop();
        }

        public void Update()
        {
        }

#if UNITY_EDITOR
        public void OnGUISetup()
        {
        }

        public void OnGUIDraw()
        {
            properties = (FabricAudioSourceProperties)EditorGUILayout.ObjectField(new GUIContent("Properties Asset"), properties, typeof(FabricAudioSourceProperties), true);
        }
#endif
        // Perhaps move the process into a separate component 
        public void OnAudioFilterRead(float[] data, int channels)
        {
            if (FabricPlayer.Instance == null) return;

            API.ProcessGameObject(FabricPlayer.Instance.fabricPlayerInstanceId, data, data.Length / channels, channels, gameObjectID);
        }
    }
}