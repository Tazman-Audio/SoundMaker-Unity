using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fabric.Player
{
    public class FabricAudioSourceProperties : ScriptableObject
    {
        public AudioMixerGroup outputAudioMixerGroup = null;
        public AudioVelocityUpdateMode velocityUpdateMode;

        public bool ignoreListenerVolume = false;
        public bool playOnAwake = false;
        public bool ignoreListenerPause = false;
        public bool bypassEffects = false;
        public bool bypassListenerEffects = false;
        public bool bypassReverbZones = false;

        public bool spatialize = false;
        public bool spatializePostEffects = false;

        public bool mute = false;
        public float volume = 1.0f;
        public float pitch = 1.0f;
        public bool loop = false;
        public float panStereo = 0.0f;
        public float spatialBlend = 0.0f;
        public float reverbZoneMix = 1.0f;
        public float dopplerLevel = 1.0f;
        public float spread = 0.0f;
        public int priority = 128;

        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        public float minDistance = 1.0f;
        public float maxDistance = 500.0f;

#if UNITY_EDITOR
        [MenuItem("Assets/SoundMaker/Audio Source")]
        public static void Create()
        {
            FabricAudioSourceProperties asset = ScriptableObject.CreateInstance<FabricAudioSourceProperties>();

            string assetPath = "Assets/FabricAudioSource.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}