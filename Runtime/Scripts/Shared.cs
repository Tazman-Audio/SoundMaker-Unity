// This is a valid C, C++ and C# file :)
#if __cplusplus
#define public
#else
using System.Runtime.InteropServices;
using System;

namespace Fabric.Native
{
#endif
    public enum EventAction
    {
        // Trigger event
        PlaySound,
        PlayScheduled,
        PlayMIDI,
        StopSound,
        StopScheduled,
        PauseSound,
        UnpauseSound,

        // Parameter event type
        SetVolume,
        SetPitch,
        SetSwitch,
        SetParameter,
        SetFadeIn,
        SetFadeOut,
        SetPan,

        // Global Parameters
        SetGlobalParameter,
        SetGlobalSwitch,

        // Sequence
        AdvanceSequence,
        ResetSequence,

        // Asset management
        PrefetchedAudio,
        PinnedAudio,

        // DSP Effect
        SetDSPParameter,

        // Misc
        SetTime,
        StopAll,
        SetMarker,
        KeyOffMarker,
    };

    public enum EventCallbackType
	{
		OnFinished = 0,
		OnSequenceNextEntry,
		OnSequenceAdvance,
		OnSequenceEnd,
		OnSequenceLoop,
		OnSwitch,
		OnMarker,
		OnRegionSet,
		OnRegionQueued,
		OnRegionEnd
	};

    public enum SystemCallbackType
    {
        onMountEnd = 0,
        onAssetsLoaded,
    }
    
    public enum ComponentInstanceState
    {
        Active_Virtual = 0,
        Active_Physical,
        Inactive
    };

    public enum CompressionType
    {
        Wav = 0,
        Vorbis
    };

#if !__cplusplus
    [StructLayout(LayoutKind.Sequential)]
#endif
    public struct Vector
    {
        public float x;// = 0.0f;
        public float y;// = 0.0f;
        public float z;// = 0.0f;
    }
#if __cplusplus
;
#endif

#if !__cplusplus
    [StructLayout(LayoutKind.Sequential)]
#endif
    public struct Position
    {
        public Vector position;
        public Vector orientation;
    }
#if __cplusplus
;
#endif

#if !__cplusplus
    [StructLayout(LayoutKind.Sequential)]
#endif
    public struct ComponentInfo
    {
        public bool mIsActive;
        public int mAvailableInstancesCount;
        public int mActiveInstancesCount;
        public int mTotalInstancesCount;
    }
#if __cplusplus
;
#endif

#if !__cplusplus
    [StructLayout(LayoutKind.Sequential)]
#endif
    public struct AudioClipInfo
    {
        public string name;
        public ulong fpkID;
        public int compressionQuality;
        public Native.CompressionType compressionType;
    }

#if __cplusplus
;
#endif

#if __cplusplus
#undef public
#else
}
#endif
