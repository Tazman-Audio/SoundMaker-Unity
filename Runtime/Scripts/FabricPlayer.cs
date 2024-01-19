using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fabric.Player
{
    public static class FabricPlayerCallbacks
    {
        public static System.Action OnPlayerUIInitialised;

#if UNITY_EDITOR
        public static System.Action<UInt64> FPKAdded;
        public static System.Action<UInt64> FPKRemoved;
#endif
    }

    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    [AddComponentMenu("SoundMaker/Player")]
    public class FabricPlayer : MonoBehaviour
    {
        private static FabricPlayer instance = null;
        public static FabricPlayer Instance
        {
            get
            {
                return instance;
            }
        }

#if UNITY_EDITOR
        GenericCallbackDelegate _delegate;
        public System.IntPtr genericCallbackPtr
        {
            get
            {
                if (_delegate == null)
                {
                    _delegate = new GenericCallbackDelegate(EditorCallbacks.GenericCallbackHandler);
                }
                return Marshal.GetFunctionPointerForDelegate(_delegate);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GenericCallbackDelegate([MarshalAs(UnmanagedType.LPStr)] string xmlData);
#endif
        [NonSerialized]
        public UInt64 fabricPlayerInstanceId = UInt64.MaxValue;

        [NonSerialized]
        private API.SystemCallbackHandlerFunc systemCallbackHandlerFunc = null;

        [SerializeField]
        [ReadOnly]
        FabricPlayerProperties properties = null;
        public static FabricPlayerProperties Properties
        {
            get
            {
                if (instance.properties == null)
                {
                    instance.properties = FabricPlayerProperties.Get();
#if UNITY_EDITOR
                    EditorUtility.SetDirty(instance);
#endif
                }

                return instance.properties;
            }
        }

#if UNITY_EDITOR
        [NonSerialized]
        public UInt64 fabricPlayerGUIInstanceId = UInt64.MaxValue;

        [SerializeField]
        [HideInInspector]
        private bool fpksAreLoaded = false;
#endif

        [MonoPInvokeCallback(typeof(API.FabricLogMessage))]
        static void LogMessage([MarshalAs(UnmanagedType.LPStr)] string message)
        {
            Debug.Log(message);
        }

        private void OnFabricPlayerSystemMessage(Fabric.Native.SystemCallbackType type, IntPtr info)
        {
            switch (type)
            {
                case Native.SystemCallbackType.onMountEnd:
                    {
                        Debug.Log("File mounted : " + Marshal.PtrToStringAnsi(info));
                        break;
                    }
                case Native.SystemCallbackType.onAssetsLoaded:
                    {
                        Debug.Log("Assets finished loading for fpk id : " + Marshal.PtrToStructure(info, typeof(UInt64)));
                        break;
                    }
                default: break;
            }
        }

        public void SavePlayerProperties()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(FabricPlayer.Properties);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public void Awake()
        {
            fabricPlayerInstanceId = API.GetDefaultInstanceID();

            instance = this;
#if UNITY_EDITOR
            fabricPlayerGUIInstanceId = API.GUI.GetDefaultInstanceID();
#endif

            if (!API.IsInitialized(fabricPlayerInstanceId))
            {
                AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
                fabricPlayerInstanceId = API.Create(AudioSettings.outputSampleRate, (uint)bufferLength / 2);

                if (systemCallbackHandlerFunc == null)
                {
                    systemCallbackHandlerFunc = OnFabricPlayerSystemMessage;
                    API.SystemCallbacks.RegisterSystemCallbackHandler(fabricPlayerInstanceId, systemCallbackHandlerFunc);
                }

#if !UNITY_EDITOR
                API.SetProjectRootPath(fabricPlayerInstanceId, Application.streamingAssetsPath);
#endif
                if (!Application.isEditor)
                {
                    string projectXML = Application.streamingAssetsPath + "/Output.xml";
                    API.Load(fabricPlayerInstanceId, projectXML);
                }

#if UNITY_EDITOR
                InitializeGUI();

                if (properties != null)
                {
                    foreach (var fpkInfoPair in FabricPlayer.Properties.FPKs)
                    {
                        API.GUI.LoadFPK(fabricPlayerGUIInstanceId, fpkInfoPair.Key);

                        // Reset the file paths for fpks the first time we get this object,
                        // as they may have changed since the editor was last open
                        const int maxFilePathLength = 256;
                        var path = new StringBuilder(maxFilePathLength);
                        API.GUI.GetFPKFilePath(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkInfoPair.Key, path, maxFilePathLength);

                        if (File.Exists(path.ToString()))
                        {
                            fpkInfoPair.Value.path = Path.GetDirectoryName(path.ToString());
                        }
                    }

                    fpksAreLoaded = true;
                }
                else
                {
                    fpksAreLoaded = false;
                }
#endif
            }
            else
            {
                API.StopAll(fabricPlayerInstanceId);
            }

            if (API.IsInitialized(fabricPlayerInstanceId))
            {
                if (systemCallbackHandlerFunc == null)
                {
                    systemCallbackHandlerFunc = OnFabricPlayerSystemMessage;
                    API.SystemCallbacks.RegisterSystemCallbackHandler(fabricPlayerInstanceId, systemCallbackHandlerFunc);
                }
            }

#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
            Undo.undoRedoPerformed += MyUndoCallback;

            API.GUI.SetEditorCallback(fabricPlayerGUIInstanceId, genericCallbackPtr);
#endif
        }

        void Start()
        {
#if UNITY_EDITOR
            AddDefaultFPKSearchDirectory();

            if (!fpksAreLoaded && API.IsInitialized(fabricPlayerInstanceId))
            {
                foreach (var fpkId in FabricPlayer.Properties.FPKs.Keys)
                {
                    API.GUI.LoadFPK(fabricPlayerGUIInstanceId, fpkId);
                }

                fpksAreLoaded = true;
            }
#endif
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;

                if (fabricPlayerInstanceId == UInt64.MaxValue)
                {
                    fabricPlayerInstanceId = API.GetDefaultInstanceID();
                }

                if (systemCallbackHandlerFunc == null)
                {
                    systemCallbackHandlerFunc = OnFabricPlayerSystemMessage;
                    API.SystemCallbacks.RegisterSystemCallbackHandler(fabricPlayerInstanceId, systemCallbackHandlerFunc);
                }

#if UNITY_EDITOR
                if (fabricPlayerGUIInstanceId == UInt64.MaxValue)
                {
                    fabricPlayerGUIInstanceId = API.GUI.GetDefaultInstanceID();

                    API.GUI.SetEditorCallback(fabricPlayerGUIInstanceId, genericCallbackPtr);
                }
#endif
            }
        }

        public void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            Undo.undoRedoPerformed -= MyUndoCallback;
            API.GUI.ClearEditorCallback(fabricPlayerInstanceId);
#endif
        }

#if UNITY_EDITOR
        void MyUndoCallback()
        {
            // Notify via API that an Undo action has performed
        }

        public void InitializeGUI()
        {
            if (!API.GUI.IsInitialized(fabricPlayerGUIInstanceId))
            {
                fabricPlayerGUIInstanceId = API.GUI.Initialise(Application.dataPath,
                                                           !EditorApplication.isPlayingOrWillChangePlaymode,
                                                           FabricProjectSettings.GetInstance() != null ? FabricProjectSettings.GetInstance().fabricProjectRelativePath : "",
                                                           fabricPlayerInstanceId,
                                                           IntPtr.Zero);
            }

            FabricPlayerCallbacks.OnPlayerUIInitialised?.Invoke();
        }

        public void EditorUpdate()
        {
            API.GUI.Poll(fabricPlayerGUIInstanceId);
            API.Update(fabricPlayerInstanceId, Time.deltaTime);
        }

        private void AddDefaultFPKSearchDirectory()
        {
            if (properties == null) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);

            int numFoldersUp = Regex.Matches(properties.fpkSearchDirectory, "../").Count;

            for (int count = 0; count < numFoldersUp; count++)
            {
                directoryInfo = directoryInfo.Parent;
            }

            string fpkRelativePath = properties.fpkSearchDirectory.Replace("../", "");
            string fpksAbsolutePath = Path.Combine(directoryInfo.FullName, fpkRelativePath);

            if (Directory.Exists(fpksAbsolutePath))
            {
                API.GUI.AddFPKSearchPath(fabricPlayerGUIInstanceId, fpksAbsolutePath);
            }
            else
            {
                Debug.LogErrorFormat("SoundmakerPlayer : No fpk search directory exists at path {0} - please create one or set the FPK Search Directory in FabricPlayerSettings", fpksAbsolutePath);
            }
        }

        //public void OnSaveEditor()
        //{
        //    if (isGUIInitialized)
        //    {
        //        FabricProjectSettings fabricProjectSettings = FabricProjectSettings.GetInstance();
        //        if (FabricProjectSettings.GetInstance() == null)
        //        {
        //            FabricProjectSettings.Create(false);
        //        }

        //        if (!string.IsNullOrEmpty(fabricProjectSettings.fabricProjectRelativePath))
        //        {
        //            if (!API.GUI.SaveProject(fabricPlayerGUIInstanceId))
        //            {
        //                Debug.LogError("Failed to save project " + fabricProjectSettings.fabricProjectRelativePath);
        //            }
        //        }
        //    }
        //}
#endif
    }
}
