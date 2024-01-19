using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fabric.Player
{
    public class FabricPlayerProperties : ScriptableObject
    {
        public string fpkSearchDirectory = "../FPKs";

        public SerializableDictionary<UInt64, NestedList<string>> eventList = new SerializableDictionary<UInt64, NestedList<string>>();
        //public List<string> globalParameterList = new List<string>();

        public List<string> projectEvents = new List<string>();
        public SerializableDictionary<UInt64, FPKInfo> FPKs = new SerializableDictionary<UInt64, FPKInfo>();

        // NOTE: DO NOT call this inside an Awake MonoBehaviour function in the editor otherwise it might cause the editor to crash!!!!
        public static FabricPlayerProperties Get()
        {
#if UNITY_EDITOR
            FabricPlayerProperties instance = (FabricPlayerProperties)AssetDatabase.LoadAssetAtPath(assetPath, typeof(FabricPlayerProperties));
            if (instance == null)
            {
                instance = CreateInternal();
            }
#else
            FabricPlayerProperties instance = Resources.Load<FabricPlayerProperties>("FabricPlayerSettings");
            if(instance == null)
            {
                Debug.LogError("FabricPlayerProperties is missing!!!!");
            }
#endif
            return instance;
        }

#if UNITY_EDITOR
        public static string assetPath = "Assets/SoundMaker/FabricPlayerSettings.asset";

        public List<EventInfo> GetAudioClips(string eventName)
        {
            List<EventInfo> list = new List<EventInfo>();

            foreach (FPKInfo info in FPKs.Values)
            {
                EventInfo clipList = info.GetAudioClips(eventName);
                if (clipList != null)
                {
                    list.Add(clipList);
                }
            }

            return list;
        }

        public List<EventInfo> GetAudioClips(string[] eventNames)
        {
            List<EventInfo> list = new List<EventInfo>();

            foreach (FPKInfo info in FPKs.Values)
            {
                list.AddRange(info.GetAudioClips(eventNames));
            }

            return list;
        }

        public List<EventInfo> GetAudioClipsDeepCopy(string[] eventNames)
        {
            List<EventInfo> list = new List<EventInfo>();

            foreach (FPKInfo info in FPKs.Values)
            {
                list.AddRange(info.GetAudioClipsDeepCopy(eventNames));
            }

            return list;
        }

        public List<AudioClipInfo> GetAudioClipsInfo(string[] eventNames)
        {
            List<AudioClipInfo> list = new List<AudioClipInfo>();

            foreach (FPKInfo info in FPKs.Values)
            {
                list.AddRange(info.GetAudioClipsInfo(eventNames));
            }

            return list;
        }

        public void CleanUpMissingFPKs()
        {
            List<UInt64> fpksToRemove = new List<UInt64>();
            foreach (var fpkInfoPair in FPKs)
            {
                UInt64 fpkID = fpkInfoPair.Key;
                if (fpkID == UInt64.MaxValue) continue;

                bool fpkAvailableToProject = API.GUI.FPKIsAvailableInFPKFolders(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkID);
                if (!fpkAvailableToProject)
                {
                    fpksToRemove.Add(fpkID);
                }
            }

            foreach (UInt64 fpkID in fpksToRemove)
            {
                FPKs.Remove(fpkID);

                if (eventList.ContainsKey(fpkID))
                {
                    eventList.Remove(fpkID);
                }
            }

            FabricPlayer.Instance.SavePlayerProperties();
        }

        [MenuItem("Assets/SoundMaker/Player Settings")]
        public static void Create()
        {
            var obj = ScriptableObjectUtils.Find<FabricProjectSettings>();

            if (obj) return;

            CreateInternal();
        }

        static FabricPlayerProperties CreateInternal()
        {
            FabricPlayerProperties asset = ScriptableObject.CreateInstance<FabricPlayerProperties>();

            if (!AssetDatabase.IsValidFolder("Assets/SoundMaker"))
            {
                AssetDatabase.CreateFolder("Assets", "SoundMaker");
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}