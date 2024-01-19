using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Fabric.Native;
using System.Text;
using System.Collections;
using System.IO;

namespace Fabric.Player
{
    public class Plugin
    {
#if UNITY_IOS
        public const string PLUGIN_NAME  = "__Internal";
#else
        public const string PLUGIN_NAME = "AudioPluginSoundMakerPlayer";
#endif
    }

    [Serializable]
    public class AudioClipInfo : IComparable<AudioClipInfo>
    {
        public string name;
        public UInt64 fpkID;
        public bool defaultIncludeOnExport = true;
        public int defaultCompressionQuality = 100;
        public Native.CompressionType defaultCompressionType = Native.CompressionType.Wav;

        public AudioClipInfo() { }
        public AudioClipInfo(AudioClipInfo other)
        {
            name = other.name;
            fpkID = other.fpkID;
            defaultIncludeOnExport = other.defaultIncludeOnExport;
            defaultCompressionQuality = other.defaultCompressionQuality;
            defaultCompressionType = other.defaultCompressionType;
        }

        public int CompareTo(AudioClipInfo other)
        {
            return fpkID.CompareTo(other.fpkID) + string.Compare(name, other.name);
        }

        public override bool Equals(object other)
        {
            var otherInfo = other as AudioClipInfo;
            if (otherInfo == null) return false;

            return fpkID == otherInfo.fpkID && name.Equals(otherInfo.name);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + fpkID.GetHashCode();
            hash = hash * 31 + name.GetHashCode();
            return hash;
        }
    }

    [Serializable]
    public class EventInfo
    {
        public string eventName;
        public List<AudioClipInfo> audioClips = new List<AudioClipInfo>();

        public EventInfo() { }
        public EventInfo(string e, List<AudioClipInfo> clips)
        {
            eventName = e;
            audioClips = clips;
        }
    }

    [Serializable]
    public class FPKInfo
    {
        public static UInt64 INVALID_FPKID = UInt64.MaxValue;

        public SerializableDictionary<string, EventInfo> events = new SerializableDictionary<string, EventInfo>();
        public UInt64 FPKID = UInt64.MaxValue;
        public string fpkName = string.Empty;
        public string path = string.Empty;

        public FPKInfo(UInt64 fpkID, string _path)
        {
            FPKID = fpkID;
            if (_path.Contains("\\"))
            {
                fpkName = _path.Substring(_path.LastIndexOf('\\') + 1);
            }
            else
            {
                fpkName = _path;
            }

            path = Path.GetDirectoryName(_path);
        }

        public static UInt64 GetFPKID(string fpkName)
        {
            if (FabricPlayer.Instance == null 
                || FabricPlayer.Properties == null 
                || FabricPlayer.Properties.FPKs.Count == 0)
            {
                Debug.LogError("FPKInfo.GetFPKID : Unable to get FPKID from FabricPlayer.Properties");
                return INVALID_FPKID;
            }

            foreach (var fpkIdToInfoPair in FabricPlayer.Properties.FPKs)
            {
                const string fpkFileExtension = ".fpk";
                string fpkNameWithExtension = fpkName + fpkFileExtension;
                if (fpkIdToInfoPair.Value.fpkName == fpkName || fpkIdToInfoPair.Value.fpkName == fpkNameWithExtension)
                {
                    return fpkIdToInfoPair.Key;
                }
            }

            Debug.LogError("FPKInfo.GetFPKID : No fpk loaded with the name " + fpkName);

            return INVALID_FPKID;
        }

        public static string GetFPKName(UInt64 fpkID)
        {
            if (FabricPlayer.Instance == null
                || FabricPlayer.Properties == null
                || FabricPlayer.Properties.FPKs.Count == 0)
            {
                Debug.LogError("FPKInfo.GetFPKName : Unable to get FPK name from FabricPlayer.Properties");
                return string.Empty;
            }

            if (FabricPlayer.Properties.FPKs.TryGetValue(fpkID, out FPKInfo fpkInfo))
            {
                return fpkInfo.fpkName;
            }

            Debug.LogError("FPKInfo.GetFPKName : No fpk loaded with the ID " + fpkID);

            return string.Empty;
        }

        public static List<UInt64> GetFPKIDsForEvent(string eventName)
        {
            List <UInt64> fpkIDs = new List<UInt64>();

            if (FabricPlayer.Instance == null
                || FabricPlayer.Properties == null
                || FabricPlayer.Properties.FPKs.Count == 0)
            {
                Debug.LogError("FPKInfo.GetFPKIDForEvent : Unable to get FPKID from FabricPlayer.Properties");
                return fpkIDs;
            }

            foreach (var fpkIdToInfoPair in FabricPlayer.Properties.FPKs)
            {
                if (fpkIdToInfoPair.Value.events.ContainsKey(eventName))
                {
                    fpkIDs.Add(fpkIdToInfoPair.Key);
                }
            }

            if (fpkIDs.Count <= 0)
            {
                Debug.LogError("FPKInfo.GetFPKIDForEvent : No fpk associated with the event name " + eventName);
            }

            return fpkIDs;
        }

        public EventInfo GetAudioClips(string eventName)
        {
            if (events.ContainsKey(eventName))
            {
                return new EventInfo(eventName, (List<AudioClipInfo>)events[(string)eventName].audioClips);
            }

            return null;
        }

        public EventInfo GetAudioClipsDeepCopy(string eventName)
        {
            if (events.ContainsKey(eventName))
            {
                var audioClipsList = new List<AudioClipInfo>();
                if (events.TryGetValue(eventName, out EventInfo storedEventInfo))
                {
                    foreach (var originalAudioClip in storedEventInfo.audioClips)
                    {
                        audioClipsList.Add(new AudioClipInfo(originalAudioClip));
                    }
                }

                var eventInfo = new EventInfo(eventName, audioClipsList);
                return eventInfo;
            }

            return null;
        }

        public List<EventInfo> GetAudioClips(string[] eventNames)
        {
            List<EventInfo> list = new List<EventInfo>();

            foreach (string e in eventNames)
            {
                EventInfo clipList = GetAudioClips(e);
                if (clipList != null)
                {
                    list.Add(clipList);
                }
            }

            return list;
        }

        public List<EventInfo> GetAudioClipsDeepCopy(string[] eventNames)
        {
            List<EventInfo> list = new List<EventInfo>();

            foreach (string e in eventNames)
            {
                EventInfo clipList = GetAudioClipsDeepCopy(e);
                if (clipList != null)
                {
                    list.Add(clipList);
                }
            }

            return list;
        }

        public List<AudioClipInfo> GetAudioClipsInfo(string[] eventNames)
        {
            List<AudioClipInfo> list = new List<AudioClipInfo>();

            foreach (string e in eventNames)
            {
                if (events.ContainsKey(e))
                {
                    list.AddRange(events[e].audioClips);
                }
            }

            return list;
        }

        public List<AudioClipInfo> GetAllAudioClipInfos()
        {
            List<AudioClipInfo> allAudioClipInfos = new List<AudioClipInfo>();

            foreach (var eventToClipsPair in events) 
            {
                allAudioClipInfos.AddRange(eventToClipsPair.Value.audioClips);
            }

            return allAudioClipInfos;
        }

        public void AddEvent(string eventName)
        {
            if (!events.ContainsKey(eventName))
            {
                EventInfo eventInfo = new EventInfo();

                eventInfo.eventName = eventName;
                eventInfo.audioClips = new List<AudioClipInfo>();

                events.Add(eventName, eventInfo);
            }
        }

        public void AddAudioClip(string eventName, string audioClipName)
        {
            if (events.ContainsKey(eventName))
            {
                EventInfo eventInfo = events[eventName];

                AudioClipInfo audioClipInfo = new AudioClipInfo();

                int lastIndex = audioClipName.LastIndexOf('/');
                if (lastIndex >= 0)
                {
                    audioClipInfo.name = audioClipName.Substring(lastIndex + 1);
                }
                else
                {
                    audioClipInfo.name = audioClipName;
                }

                audioClipInfo.fpkID = FPKID;

                eventInfo.audioClips.Add(audioClipInfo);
            }
        }
    }

    public static class API
    {
        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricIsInitialized", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool IsInitialized(UInt64 FabricInstanceId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricCreate", CallingConvention = CallingConvention.Cdecl)]
        extern static public UInt64 Create(float sampleRate, uint buffer);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricDestroy", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool Destroy(UInt64 FabricInstanceId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGetDefaultInstanceID", CallingConvention = CallingConvention.Cdecl)]
        extern static public UInt64 GetDefaultInstanceID();

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricStopAll", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool StopAll(UInt64 FabricInstanceId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetEventLogCallback", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetEventLogCallback(UInt64 system, UInt64 callback);


        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricLoad", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool Load(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)]string path);

		[DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricLoadFromMemory", CallingConvention = CallingConvention.Cdecl)]
		extern static public IntPtr LoadFromMemory(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)]string buffer);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricUnloadFromMemory", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool UnloadFromMemory(UInt64 FabricInstanceId, IntPtr buffer);

		[DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSave", CallingConvention = CallingConvention.Cdecl)]
		extern static public bool Save(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)]string path);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSaveToMemory", CallingConvention = CallingConvention.Cdecl)]
        extern static public IntPtr SaveToMemory(UInt64 FabricInstanceId, StringBuilder buffer, ref int bufLen);


        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricPostEvent", CallingConvention = CallingConvention.Cdecl)]
        extern static public void PostEvent(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, EventAction eventAction, int gameObjectId, int playingId = -1, EventCallback callback = null);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricPostEventParameter", CallingConvention = CallingConvention.Cdecl)]
        extern static public void PostEvent(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, EventAction eventAction, int gameObjectId, [MarshalAs(UnmanagedType.LPStr)]string parameter, int playingId = -1);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricPostEventValue", CallingConvention = CallingConvention.Cdecl)]
        extern static public void PostEvent(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, EventAction eventAction, int gameObjectId, float value, int playingId = -1);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetParameter", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetParameter(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, [MarshalAs(UnmanagedType.LPStr)]string parameterName, int gameObjectId, float value, int playingId = -1);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetGlobalParameter", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetGlobalParameter(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)]string parameterName, float value);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetGlobalSwitch", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetGlobalSwitch(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)]string globalSwitchName, [MarshalAs(UnmanagedType.LPStr)]string switchName);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetMarker", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetMarker(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, [MarshalAs(UnmanagedType.LPStr)]string markerName, int gameObjectId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricKeyOffMarker", CallingConvention = CallingConvention.Cdecl)]
        extern static public void KeyOffMarker(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, int gameObjectId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetVolume", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetVolume(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, int gameObjectId, float value);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetPitch", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetPitch(UInt64 FabricInstanceId, UInt64 fpkId, [MarshalAs(UnmanagedType.LPStr)]string eventName, int gameObjectId, float value);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricRegisterGameObject", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool RegisterGameObject(UInt64 FabricInstanceId, int gameObjectId, string gameObjectName);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricUnregisterGameObject", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool UnregisterGameObject(UInt64 FabricInstanceId, int gameObjectId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetGameObjectPosition", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool SetGameObjectPosition(UInt64 FabricInstanceId, int gameObjectId, ref Position position);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetListener", CallingConvention = CallingConvention.Cdecl)]
        extern static public void SetListener(UInt64 FabricInstanceId, int gameObjectId, Vector position, Vector forward, Vector up);


        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricUpdate", CallingConvention = CallingConvention.Cdecl)]
        extern static public void Update(UInt64 FabricInstanceId, float dt);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricProcess", CallingConvention = CallingConvention.Cdecl)]
        extern static public void Process(UInt64 FabricInstanceId, int numFrames);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricProcessGameObjectInterleaved", CallingConvention = CallingConvention.Cdecl)]
        extern static public void ProcessGameObject(UInt64 FabricInstanceId, float[] outBuffer, int outLength, int channels, int gameObjectId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricHasErrors", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool HasErrors(UInt64 FabricInstanceId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGetNextError", CallingConvention = CallingConvention.Cdecl)]
        extern static public UInt64 GetNextError(UInt64 FabricInstanceId);

        [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricSetProjectRootPath", CallingConvention = CallingConvention.Cdecl)]
        extern static public bool SetProjectRootPath(UInt64 FabricInstanceId, [MarshalAs(UnmanagedType.LPStr)] string projectRootPath);
        
        public class SystemCallbacks
        {
            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricRegisterSystemCallbackHandler", CallingConvention = CallingConvention.Cdecl)]
            extern static public void RegisterSystemCallbackHandler(UInt64 FabricInstanceId, SystemCallbackHandlerFunc callback);
        }

#if UNITY_EDITOR
        public class GUI
        {
            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIIsInitialised", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool IsInitialized(UInt64 FabricGUIInstanceId);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIInitialise", CallingConvention = CallingConvention.Cdecl)]
            extern static public UInt64 Initialise(string assetsPath, bool isEditMode, string fabricProjectRelativePath, UInt64 FabricInstanceId, IntPtr genericCallback);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIShutdown", CallingConvention = CallingConvention.Cdecl)]
            extern static public void Shutdown(UInt64 FabricGUIInstanceId, bool retainProject);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIGetDefaultInstanceID", CallingConvention = CallingConvention.Cdecl)]
            extern static public UInt64 GetDefaultInstanceID();

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIAddFPKSearchPath", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool AddFPKSearchPath(UInt64 FabricGUIInstanceId, string fabricProjectRelativePath);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUISetEditorCallback", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool SetEditorCallback(UInt64 FabricGUIInstanceId, System.IntPtr Callback);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIClearEditorCallback", CallingConvention = CallingConvention.Cdecl)]
            extern static public void ClearEditorCallback(UInt64 FabricGUIInstanceId);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUILoadFPK", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool LoadFPK(UInt64 FabricGUIInstanceId, [MarshalAs(UnmanagedType.LPStr)] UInt64 id);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIFPKIsAvailableInFPKFolders", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool FPKIsAvailableInFPKFolders(UInt64 FabricGUIInstanceId, [MarshalAs(UnmanagedType.LPStr)] UInt64 id);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIGetFPKFilePath", CallingConvention = CallingConvention.Cdecl)]
            extern static public void GetFPKFilePath(UInt64 FabricGUIInstanceId, [MarshalAs(UnmanagedType.LPStr)] UInt64 id, StringBuilder path, int pathLength);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIPoll", CallingConvention = CallingConvention.Cdecl)]
            extern static public void Poll(UInt64 FabricGUIInstanceId);


            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUICreatePanel", CallingConvention = CallingConvention.Cdecl)]
            public extern static UInt64 CreatePanel(UInt64 FabricPlayerGUIInstanceId, string panelName);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIGetOrCreatePersistentPanel", CallingConvention = CallingConvention.Cdecl)]
            public extern static UInt64 GetOrCreatePersistentPanel(UInt64 FabricPlayerGUIInstanceId, string panelName);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIClosePanel", CallingConvention = CallingConvention.Cdecl)]
            public extern static void ClosePanel(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIDrawPanel", CallingConvention = CallingConvention.Cdecl)]
            public extern static void DrawPanel(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIPanelNeedsRedraw", CallingConvention = CallingConvention.Cdecl)]
            public extern static int PanelNeedsRedraw(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUISetTexture", CallingConvention = CallingConvention.Cdecl)]
            public extern static void SetTexture(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, IntPtr texture, int width, int height);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUISetScreenBounds", CallingConvention = CallingConvention.Cdecl)]
            public extern static void SetScreenBounds(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y, float w, float h);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIMouseMove", CallingConvention = CallingConvention.Cdecl)]
            public extern static void MouseMove(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIMouseDown", CallingConvention = CallingConvention.Cdecl)]
            public extern static void MouseDown(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y, EventModifiers mods, int button);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIMouseDrag", CallingConvention = CallingConvention.Cdecl)]
            public extern static void MouseDrag(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y, EventModifiers mods, int button);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIMouseUp", CallingConvention = CallingConvention.Cdecl)]
            public extern static void MouseUp(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y, EventModifiers mods);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIMouseWheel", CallingConvention = CallingConvention.Cdecl)]
            public extern static void MouseWheel(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, float x, float y, EventModifiers mods, float dx, float dy);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIKeyEvent", CallingConvention = CallingConvention.Cdecl)]
            public extern static void KeyEvent(UInt64 FabricPlayerGUIInstanceId, UInt64 panelID, KeyCode code, EventModifiers mods, string name);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricExportRuntimeFPK", CallingConvention = CallingConvention.Cdecl)]
            extern static public void ExportRuntimeFPK(UInt64 FabricGUIInstanceId, string path, string[] audioClipInfo, int numOfEvents);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricExportRuntimeFPKWithCustomSettings", CallingConvention = CallingConvention.Cdecl)]
            extern static public void ExportRuntimeFPKWithCustomSettings(UInt64 FabricGUIInstanceId, string path, Native.AudioClipInfo[] audioClipInfo, int numOfEvents, bool forceExportAll);

            //// We need to know what we want to do with these!!!!
            //[DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUIExportForPlayMode", CallingConvention = CallingConvention.Cdecl)]
            //extern static public void ExportForPlayMode(UInt64 FabricGUIInstanceId);

            //[DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricGUISaveProject", CallingConvention = CallingConvention.Cdecl)]
            //extern static public bool SaveProject(UInt64 FabricGUIInstanceId);

            //[DllImport(Plugin.PLUGIN_NAME)]
            //private static extern UInt64 GetRenderEventFunc(UInt64 FabricPlayerGUIInstanceId);

            [DllImport(Plugin.PLUGIN_NAME, EntryPoint = "FabricProcessPlayerPanel", CallingConvention = CallingConvention.Cdecl)]
            extern static public bool ProcessPlayerPanel(UInt64 fabricGUIInstanceId, float[] inBuffer, float[] outBuffer, int outLength, int channels);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void FabricSetFPKIDCallback(UInt64 FPKID);
        }
#endif

        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FabricLogMessage([MarshalAs(UnmanagedType.LPStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FabricEventLogMessage([MarshalAs(UnmanagedType.LPStr)]string eventName, EventAction eventAction, float time);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventCallback(EventCallbackType type, IntPtr info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SystemCallbackHandlerFunc(SystemCallbackType type, IntPtr info);

        public static IntPtr AllocDelegateAndGetFunctionPointer(System.Delegate _Delegate)
        {
            return Marshal.GetFunctionPointerForDelegate(_Delegate);
        }
    }
}
