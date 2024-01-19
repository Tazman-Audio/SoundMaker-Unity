using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fabric.Player
{
    public static class FPKExporter
    {
        public static FabricCustomExporterSettings customExporterSettings { get; private set; } = null;

        static FPKExporter()
        {
            Fabric.Player.FabricPlayerCallbacks.FPKAdded += OnFPKAdded;
            Fabric.Player.FabricPlayerCallbacks.FPKRemoved += OnFPKRemoved;
        }

        public static void Export()
        {
            List<Native.AudioClipInfo> convertedClipsList = new List<Native.AudioClipInfo>();

            foreach (var fpkToClipsListPair in customExporterSettings.fpkAudioClips)
            {
                foreach (var eventInfo in fpkToClipsListPair.Value)
                {
                    foreach (AudioClipInfo clipInfo in eventInfo.audioClips)
                    {
                        if (IncludeClipOnExport(eventInfo.eventName, clipInfo))
                        {
                            var convertedClipInfo = new Native.AudioClipInfo();
                            convertedClipInfo.name = clipInfo.name;
                            convertedClipInfo.fpkID = clipInfo.fpkID;
                            convertedClipInfo.compressionType = GetClipCompressionType(eventInfo.eventName, clipInfo);
                            convertedClipInfo.compressionQuality = GetClipCompressionQuality(eventInfo.eventName, clipInfo);

                            convertedClipsList.Add(convertedClipInfo);
                        }
                    }
                }
            }

            if (convertedClipsList.Count == 0) return;
            Fabric.Player.API.GUI.ExportRuntimeFPKWithCustomSettings(Fabric.Player.FabricPlayer.Instance.fabricPlayerGUIInstanceId, Application.streamingAssetsPath, convertedClipsList.ToArray(), convertedClipsList.Count(), customExporterSettings.forceExportAll);
        }

        public static bool IncludeClipOnExport(string eventName, AudioClipInfo clipInfo)
        {
            if (customExporterSettings.forceExportAll) return true;

            if (customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out var fpkGroupInfo))
            {
                if (fpkGroupInfo.includeOnExportSetForGroup) return fpkGroupInfo.includeOnExport;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out var eventGroupInfo))
            {
                if (eventGroupInfo.includeOnExportSetForGroup) return eventGroupInfo.includeOnExport;
            }

            return clipInfo.defaultIncludeOnExport;
        }

        public static Native.CompressionType GetClipCompressionType(string eventName, AudioClipInfo clipInfo)
        {
            if (customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out var fpkGroupInfo))
            {
                if (fpkGroupInfo.compressionTypeSetForGroup) return (Native.CompressionType)fpkGroupInfo.compressionTypeIndex;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out var eventGroupInfo))
            {
                if (eventGroupInfo.compressionTypeSetForGroup) return (Native.CompressionType)eventGroupInfo.compressionTypeIndex;
            }

            return clipInfo.defaultCompressionType;
        }

        public static int GetClipCompressionQuality(string eventName, AudioClipInfo clipInfo)
        {
            if (customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out var fpkGroupInfo))
            {
                if (fpkGroupInfo.compressionQualitySetForGroup) return fpkGroupInfo.compressionQuality;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out var eventGroupInfo))
            {
                if (eventGroupInfo.compressionQualitySetForGroup) return eventGroupInfo.compressionQuality;
            }

            return clipInfo.defaultCompressionQuality;
        }

        public static void LoadCustomExporterSettings()
        {
            if (FabricPlayer.Instance == null) return;
            if (CustomSettingsAreValid(customExporterSettings)) return;

            const string assetPath = "Assets/SoundMaker/Editor/FabricCustomExporterSettings.asset";

            if (!AssetDatabase.IsValidFolder("Assets/SoundMaker/Editor"))
            {
                AssetDatabase.CreateFolder("Assets/SoundMaker", "Editor");
            }

            var customExporterSettingsObj = (FabricCustomExporterSettings)AssetDatabase.LoadAssetAtPath(assetPath, typeof(FabricCustomExporterSettings));
            if (CustomSettingsAreValid(customExporterSettingsObj))
            {
                customExporterSettings = customExporterSettingsObj;
                RefreshExporterSettings();
            }
            else
            {
                customExporterSettings = ScriptableObject.CreateInstance<FabricCustomExporterSettings>();

                AssetDatabase.CreateAsset(customExporterSettings, assetPath);
                AssetDatabase.SaveAssets();

                BuildAudioClipsList();
                BuildFPKGroups();
                BuildEventGroups();
            }
        }

        public static void RefreshExporterSettings()
        {
            SerializableDictionary<UInt64, NestedList<EventInfo>> oldFpkAudioClips = DeepCopyAudioClipsList(customExporterSettings.fpkAudioClips);
            SerializableDictionary<UInt64, ExporterGroupInfo> oldFpkGroupInfo = DeepCopyGroupInfo(customExporterSettings.fpkGroupInfo);
            SerializableDictionary<FPKEventKey, ExporterGroupInfo> oldEventGroupInfo = DeepCopyGroupInfo(customExporterSettings.eventGroupInfo);

            BuildAudioClipsList();
            BuildFPKGroups();
            BuildEventGroups();

            CopyOldExporterAudioClips(oldFpkAudioClips);

            CopyOldExporterGroupSettings(oldFpkGroupInfo, customExporterSettings.fpkGroupInfo);
            CopyOldExporterGroupSettings(oldEventGroupInfo, customExporterSettings.eventGroupInfo);

            RefreshFoldedOutSettings(customExporterSettings.foldedOutFPKs, customExporterSettings.fpkGroupInfo);
            RefreshFoldedOutSettings(customExporterSettings.foldedOutEvents, customExporterSettings.eventGroupInfo);
        }

        public enum FPKAvailabilityStatus
        {
            AllAvailable = 0,
            OneOrMoreMissing,
            Unknown
        }

        public static FPKAvailabilityStatus AllFPKsAreAvailableInSourcesFolder()
        {
            if (customExporterSettings == null)
            {
                LoadCustomExporterSettings();

                if (customExporterSettings == null) return FPKAvailabilityStatus.Unknown;
            }

            foreach (UInt64 fpkID in customExporterSettings.fpkAudioClips.Keys)
            {
                if (fpkID == UInt64.MaxValue) continue;

                bool fpksIsAvailable = API.GUI.FPKIsAvailableInFPKFolders(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkID);
                if (!fpksIsAvailable) return FPKAvailabilityStatus.OneOrMoreMissing;
            }

            return FPKAvailabilityStatus.AllAvailable;
        }

        private static bool CustomSettingsAreValid(FabricCustomExporterSettings _customSettings)
        {
            return _customSettings != null && (_customSettings.fpkAudioClips.Count > 0);
        }

        private static void OnFPKAdded(UInt64 fpkID)
        {
            if (!CustomSettingsAreValid(customExporterSettings)) return;

            RefreshExporterSettings();
        }

        private static void OnFPKRemoved(UInt64 fpkID)
        {
            if (!CustomSettingsAreValid(customExporterSettings)) return;

            RefreshExporterSettings();
        }
        
        private static SerializableDictionary<UInt64, NestedList<EventInfo>> DeepCopyAudioClipsList
        (
            SerializableDictionary<UInt64, NestedList<EventInfo>> originalFPKAudioClips
        )
        {
            var copiedFPKAudioClips = new SerializableDictionary<UInt64, NestedList<EventInfo>>();
            foreach (var originalPair in originalFPKAudioClips)
            {
                var copiedEventInfos = new NestedList<EventInfo>();
                foreach (EventInfo originalEventInfo in originalPair.Value)
                {
                    var copiedAudioClipInfos = new List<AudioClipInfo>();

                    foreach (AudioClipInfo originalClipInfo in originalEventInfo.audioClips)
                    {
                        copiedAudioClipInfos.Add(new AudioClipInfo(originalClipInfo));
                    }
                    copiedEventInfos.Add(new EventInfo(originalEventInfo.eventName, copiedAudioClipInfos));
                }

                copiedFPKAudioClips.Add(originalPair.Key, copiedEventInfos);
            }

            return copiedFPKAudioClips;
        }

        private static SerializableDictionary<T, ExporterGroupInfo> DeepCopyGroupInfo<T>
        (
            SerializableDictionary<T, ExporterGroupInfo> originalGroupInfo
        )
        {
            var copiedGroupInfo = new SerializableDictionary<T, ExporterGroupInfo>();

            foreach (var originalPair in originalGroupInfo)
            {
                copiedGroupInfo.Add(originalPair.Key, new ExporterGroupInfo(originalPair.Value));
            }

            return copiedGroupInfo;
        }

        private static void BuildAudioClipsList()
        {
            customExporterSettings.fpkAudioClips = new SerializableDictionary<UInt64, NestedList<EventInfo>>();
            foreach (var fpkToEventsPair in FabricPlayer.Properties.eventList)
            {
                string[] eventNames = fpkToEventsPair.Value.ToArray();
                if (customExporterSettings.fpkAudioClips.TryGetValue(fpkToEventsPair.Key, out NestedList<EventInfo> events))
                {
                    events.AddRange(FabricPlayer.Properties.GetAudioClipsDeepCopy(eventNames));
                }
                else
                {
                    var newEvents = new NestedList<EventInfo>(FabricPlayer.Properties.GetAudioClipsDeepCopy(eventNames));
                    customExporterSettings.fpkAudioClips.Add(fpkToEventsPair.Key, newEvents);
                }
            }
        }

        private static void CopyOldExporterAudioClips(SerializableDictionary<UInt64, NestedList<EventInfo>> oldExporterSettings)
        {
            foreach (var oldFpkToClipList in oldExporterSettings)
            {
                UInt64 fpkID = oldFpkToClipList.Key;

                if (customExporterSettings.fpkAudioClips.TryGetValue(fpkID, out NestedList<EventInfo> newClipsList))
                {
                    CopyOldFPKSettings(oldFpkToClipList.Value, newClipsList);
                }
            }
        }

        private static void CopyOldFPKSettings(NestedList<EventInfo> oldClipList, NestedList<EventInfo> newClipsList)
        {
            foreach (EventInfo oldClips in oldClipList)
            {
                EventInfo newMatchingClipList = newClipsList.Find(predicate => predicate.eventName == oldClips.eventName);
                if (newMatchingClipList == null) continue;

                foreach (AudioClipInfo oldClipInfo in oldClips.audioClips)
                {
                    AudioClipInfo matchingNewClipInfo = newMatchingClipList.audioClips.Find(predicate => predicate.Equals(oldClipInfo));
                    if (matchingNewClipInfo == null) continue;

                    matchingNewClipInfo.fpkID = oldClipInfo.fpkID;
                    matchingNewClipInfo.defaultIncludeOnExport = oldClipInfo.defaultIncludeOnExport;
                    matchingNewClipInfo.defaultCompressionType = oldClipInfo.defaultCompressionType;
                    matchingNewClipInfo.defaultCompressionQuality = oldClipInfo.defaultCompressionQuality;
                }
            }
        }

        private static void CopyOldExporterGroupSettings<T>
        (
            SerializableDictionary<T, ExporterGroupInfo> oldGroupSettings
            , SerializableDictionary<T, ExporterGroupInfo> newGroupSettings
        )
        {
            foreach (var oldGroupPair in oldGroupSettings)
            {
                T key = oldGroupPair.Key;
                ExporterGroupInfo oldGroupInfo = oldGroupPair.Value;

                if (newGroupSettings.TryGetValue(key, out ExporterGroupInfo newGroupInfo))
                {
                    newGroupSettings[key] = new ExporterGroupInfo(oldGroupInfo);
                }
            }
        }

        private static void RefreshFoldedOutSettings<T>
        (
            List<T> oldFoldedOutSettings
            , SerializableDictionary<T, ExporterGroupInfo> exporterGroupSettings
        )
        {
            for (int index = oldFoldedOutSettings.Count - 1; index >= 0; index--)
            {
                if (!exporterGroupSettings.ContainsKey(oldFoldedOutSettings[index]))
                {
                    oldFoldedOutSettings.RemoveAt(index);
                }
            }
        }

        private static void BuildFPKGroups()
        {
            customExporterSettings.fpkGroupInfo = new SerializableDictionary<UInt64, ExporterGroupInfo>();

            foreach (UInt64 fpkID in FabricPlayer.Properties.FPKs.Keys)
            {
                if (!customExporterSettings.fpkGroupInfo.ContainsKey(fpkID))
                {
                    customExporterSettings.fpkGroupInfo.Add(fpkID, new ExporterGroupInfo());
                }
            }
        }

        private static void BuildEventGroups()
        {
            customExporterSettings.eventGroupInfo = new SerializableDictionary<FPKEventKey, ExporterGroupInfo>();

            foreach (FPKInfo fpkInfo in FabricPlayer.Properties.FPKs.Values)
            {
                foreach (var eventInfoPair in fpkInfo.events)
                {
                    var eventKey = new FPKEventKey(fpkInfo.FPKID, eventInfoPair.Key);
                    if (!customExporterSettings.eventGroupInfo.ContainsKey(eventKey))
                    {
                        customExporterSettings.eventGroupInfo.Add(eventKey, new ExporterGroupInfo());
                    }
                }
            }
        }
    }
}