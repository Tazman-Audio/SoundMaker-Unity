using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fabric.Player
{
    [System.Serializable]
    public class ExporterGroupInfo
    {
        public bool includeOnExport = false;
        public bool includeOnExportSetForGroup = false;

        public int compressionQuality = -1;
        public bool compressionQualitySetForGroup = false;

        public int compressionTypeIndex = -1; // maps to enum Native.CompressionType -> -1 == unset
        public bool compressionTypeSetForGroup = false;

        public ExporterGroupInfo() { }
        public ExporterGroupInfo(ExporterGroupInfo other)
        {
            this.includeOnExport = other.includeOnExport;
            this.includeOnExportSetForGroup = other.includeOnExportSetForGroup;
            this.compressionQuality = other.compressionQuality;
            this.compressionQualitySetForGroup = other.compressionQualitySetForGroup;
            this.compressionTypeIndex = other.compressionTypeIndex;
            this.compressionTypeSetForGroup = other.compressionTypeSetForGroup;
        }

        public enum GroupSetting { IncludeOnExport = 0, CompressionQuality, CompressionType };

        public void EnableGroupSetting(GroupSetting groupType, object newValue)
        {
            switch (groupType)
            {
                case GroupSetting.IncludeOnExport:
                    Debug.Assert(newValue is bool);

                    if (newValue is bool)
                    {
                        includeOnExportSetForGroup = true;
                        includeOnExport = (bool)newValue;
                    }
                    break;

                case GroupSetting.CompressionQuality:
                    Debug.Assert(newValue is int);

                    if (newValue is int)
                    {
                        compressionQualitySetForGroup = true;
                        compressionQuality = (int)newValue;
                    }
                    break;

                case GroupSetting.CompressionType:
                    Debug.Assert(newValue is int);

                    if (newValue is int)
                    {
                        compressionTypeSetForGroup = true;
                        compressionTypeIndex = (int)newValue;
                    }
                    break;
            }
        }

        public void DisableGroupSetting(GroupSetting groupType)
        {
            switch (groupType)
            {
                case GroupSetting.IncludeOnExport: includeOnExportSetForGroup = false; break;
                case GroupSetting.CompressionQuality: compressionQualitySetForGroup = false; compressionQuality = -1; break;
                case GroupSetting.CompressionType: compressionTypeSetForGroup = false; break;
            }
        }
    }

    [System.Serializable]
    public struct FPKEventKey
    {
        public UInt64 fpkID;
        public string eventName;

        public FPKEventKey(UInt64 fpkID, string eventName)
        {
            this.fpkID = fpkID;
            this.eventName = eventName;
        }
    }

    public class FabricCustomExporterSettings : ScriptableObject
    {
        public bool exportOnBuild = true;
        public bool forceExportAll = false;
        public SerializableDictionary<UInt64, NestedList<EventInfo>> fpkAudioClips = new SerializableDictionary<UInt64, NestedList<EventInfo>>();
        public SerializableDictionary<UInt64, ExporterGroupInfo> fpkGroupInfo = null;
        public SerializableDictionary<FPKEventKey, ExporterGroupInfo> eventGroupInfo = null;
        public List<UInt64> foldedOutFPKs = new List<UInt64>();
        public List<FPKEventKey> foldedOutEvents = new List<FPKEventKey>();

        public List<ExporterGroupInfo> GetEventGroupsForFPK(UInt64 fpkID)
        {
            if (FabricPlayer.Instance == null) return new List<ExporterGroupInfo>();

            var eventGroups = new List<ExporterGroupInfo>();
            if (FabricPlayer.Properties.FPKs.TryGetValue(fpkID, out FPKInfo fpkInfo))
            {
                foreach (string eventName in fpkInfo.events.Keys)
                {
                    var fpkEventKey = new FPKEventKey(fpkID, eventName);

                    if (eventGroupInfo.TryGetValue(fpkEventKey, out ExporterGroupInfo groupInfo))
                    {
                        eventGroups.Add(groupInfo);
                    }
                }
            }

            return eventGroups;
        }

        public List<AudioClipInfo> GetAudioClipsForFPK(UInt64 fpkID)
        {
            var audioClips = new List<AudioClipInfo>();
            if (FabricPlayer.Instance == null) return audioClips;

            if (fpkAudioClips.TryGetValue(fpkID, out NestedList<EventInfo> events))
            {
                foreach (EventInfo eventInfo in events)
                {
                    audioClips.AddRange(eventInfo.audioClips);
                }
            }

            return audioClips;
        }
    }
}