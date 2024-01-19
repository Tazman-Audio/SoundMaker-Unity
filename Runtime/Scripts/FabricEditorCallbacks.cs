using System.Runtime.InteropServices;
using System.Xml;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace Fabric.Player
{
#if UNITY_EDITOR
    class EditorCallbacks
    {
        // generic mechanism for calling into C# from native, packing
        //  the command arguments into a ValueTree.
        // the name of the root element is the command to execute,
        //  and arguments can either be attributes (aka properties) or
        //  children, as appropriate.
        public static void GenericCallbackHandler(string xmlString)
        {
            if(EditorApplication.isCompiling)
            {
                return;
            }

            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            XmlElement root = xml.DocumentElement;

            string command = root.Name;
            XmlAttributeCollection arguments = root.Attributes;

            if (command == "FPKLoaded")
            {
                string id = arguments.GetNamedItem("ID").Value;
                string path = arguments.GetNamedItem("Path").Value;

                UInt64 fpkID;
                if (!UInt64.TryParse(id, out fpkID))
                {
                    Debug.LogError("SoundMaker: Failed to parse FPK ID!!!");
                    return;
                }

                if (!FabricPlayer.Properties.eventList.ContainsKey(FPKInfo.INVALID_FPKID))
                {
                    FabricPlayer.Properties.eventList.Add(FPKInfo.INVALID_FPKID, new NestedList<string>(){ "(None)" });
                }

                if (FabricPlayer.Properties.FPKs.TryGetValue(fpkID, out FPKInfo info))
                {
                    // update the fpk path in case it has changed
                    info.path = Path.GetDirectoryName(path);
                }
                else
                {
                    FPKInfo fpkInfo = new FPKInfo(fpkID, path);
                    XmlElement eventList = FindChild(root, "EventList");
                    foreach (XmlNode eventListener in eventList.SelectNodes("EventListener"))
                    {
                        XmlNode nameNode = eventListener.SelectSingleNode("eventName");
                        string name = nameNode.Attributes.GetNamedItem("parameterValue").Value;
                        if (name.Length > 0)
                        {
                            if (FabricPlayer.Properties.eventList.TryGetValue(fpkID, out var events))
                            {
                                events.Add(name);
                            }
                            else
                            {
                                FabricPlayer.Properties.eventList.Add(fpkID, new NestedList<string>() { name });
                            }

                            fpkInfo.AddEvent(name);
                        }

                        if (eventListener.NextSibling != null)
                        {
                            foreach (XmlNode audioClip in eventListener.NextSibling)
                            {
                                string audioClipName = audioClip.Attributes.GetNamedItem("parameterValue").Value;
                                if (audioClipName.Length > 0)
                                {
                                    fpkInfo.AddAudioClip(name, audioClipName);
                                }
                            }
                        }
                    }

                    FabricPlayer.Properties.FPKs.Add(fpkID, fpkInfo);
                }

                //FabricPlayer.Properties.globalParameterList.Clear();
                //FabricPlayer.Properties.globalParameterList.Add("(None)");
                //XmlElement globalParametersList = FindChild(root, "GlobalParameters");
                //foreach (XmlNode globalParameter in globalParametersList.SelectNodes("globalParameter"))
                //{
                //    string name = globalParameter.Attributes.GetNamedItem("name").Value;
                //    if (name.Length > 0)
                //    {
                //        FabricPlayer.Properties.globalParameterList.Add(name);
                //    }
                //}

                FabricPlayer.Instance.SavePlayerProperties();

                FabricPlayerCallbacks.FPKAdded?.Invoke(fpkID);
            }
            else if (command == "FPKUnloaded")
            {
                string id = arguments.GetNamedItem("ID").Value;
                UInt64 fpkID = UInt64.Parse(id);

                if (FabricPlayer.Properties.eventList.ContainsKey(fpkID))
                {
                    FabricPlayer.Properties.eventList.Remove(fpkID);
                }

                //for (int i = FabricPlayer.Properties.globalParameterList.Count - 1; i >= 0; --i)
                //{
                //    var eventName = FabricPlayer.Properties.globalParameterList[i];
                //    String[] eventSplit = eventName.Split('/');
                //    if (eventSplit[0] == id)
                //    {
                //        FabricPlayer.Properties.globalParameterList.RemoveAt(i);
                //    }
                //}

                FabricPlayer.Properties.FPKs.Remove(fpkID);

                FabricPlayer.Instance.SavePlayerProperties();

                FabricPlayerCallbacks.FPKRemoved?.Invoke(fpkID);
            }

            else if (command == "SaveProjectRelativePath")
            {
                string path = arguments.GetNamedItem("path").Value;
                SaveUnityRelativeProjectPath(path);
            }
            else if (command == "RefreshAssets")
            {
                AssetDatabase.Refresh();
            }
            else if (command == "SetAudioMetadata")
            {
                string assetName = arguments.GetNamedItem("assetName").Value;
                XmlElement META = FindChild(root, "META");
                XmlElement UNITY = FindChild(META, "UNITY");
                SetAudioMetadata(assetName, UNITY);
            }
            else if (command == "FPKSearchDirectoryAdded")
            {
                // try reloading all fpks in case one of them has moved to a new source folder
                foreach (var fpkInfoPair in FabricPlayer.Properties.FPKs)
                {
                    API.GUI.LoadFPK(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkInfoPair.Key);
                }
            }
            else
            {
                Debug.LogWarning("Unknown EditorCallback: " + command);
            }
        }

        static void SaveUnityRelativeProjectPath(string path)
        {
            FabricProjectSettings.GetInstance().fabricProjectRelativePath = path;
            EditorUtility.SetDirty(FabricProjectSettings.GetInstance());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SetAudioMetadata(string assetName, XmlNode UNITY)
        {
            var importer =
                (AudioImporter)AssetImporter.GetAtPath("Assets/" + assetName);

            // this can happen e.g. when changing metadata on a file that
            //  hasn't been exported because it isn't used.
            if (!importer)
                return;

            var sampleSettings = importer.defaultSampleSettings;
            foreach (XmlNode setting in UNITY.ChildNodes)
            {
                string name = setting.Name;
                string parameterValue = setting.Attributes.GetNamedItem("parameterValue").Value;
                if (name == "forceToMono")
                    importer.forceToMono = int.Parse(parameterValue) != 0;
                else if (name == "normalize")
                {
                    var so = new SerializedObject(importer);
                    var normalize = so.FindProperty("m_Normalize");
                    bool newVal = (int.Parse(parameterValue) != 0);
                    if (normalize.boolValue != newVal)
                    {
                        normalize.boolValue = newVal;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(importer);
                    }
                }
                else if (name == "loadInBackground")
                    importer.loadInBackground = int.Parse(parameterValue) != 0;
                else if (name == "ambisonic")
                    importer.ambisonic = int.Parse(parameterValue) != 0;
                else if (name == "loadType")
                {
                    if (parameterValue == "Decompress on load")
                        sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                    else if (parameterValue == "Compressed in memory")
                        sampleSettings.loadType = AudioClipLoadType.CompressedInMemory;
                    else if (parameterValue == "Streaming")
                        sampleSettings.loadType = AudioClipLoadType.Streaming;
                    else
                        Debug.LogError("unknown load type: " + parameterValue);
                }
                else if (name == "compressionFormat")
                {
                    if (parameterValue == "PCM")
                        sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
                    else if (parameterValue == "Vorbis")
                        sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    else if (parameterValue == "ADPCM")
                        sampleSettings.compressionFormat = AudioCompressionFormat.ADPCM;
                    else
                        Debug.LogError("unknown compression format: " + parameterValue);
                }
                else if (name == "sampleRateSetting")
                {
                    if (parameterValue == "Preserve sample rate")
                        sampleSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    else if (parameterValue == "Optimize sample rate")
                        sampleSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                    else if (parameterValue == "Override sample rate")
                        sampleSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                    else
                        Debug.LogError("unknown sample rate setting: " + parameterValue);
                }
                else if (name == "targetSampleRate")
                {
                    uint rate = uint.Parse(parameterValue.Substring(0, parameterValue.IndexOf(' ')));
                    sampleSettings.sampleRateOverride = rate;
                }
                else if (name == "compressionQuality")
                    sampleSettings.quality = float.Parse(parameterValue) / 100;
                else
                    Debug.LogError("unknown audio meta setting: " + name);
            }
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
        }

        static XmlElement FindChild(XmlElement parent, string name)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.Name == name)
                    return (XmlElement)child;
            }
            return null;
        }
        /*
        static GenericCallbackDelegate _delegate;
        public static System.IntPtr genericCallbackPtr
        {
            get
            {
                if (_delegate == null)
                {
                    _delegate = new GenericCallbackDelegate(GenericCallbackHandler);
                }
                return Marshal.GetFunctionPointerForDelegate(_delegate);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GenericCallbackDelegate([MarshalAs(UnmanagedType.LPStr)] string xmlData);*/
    }
#endif
}
