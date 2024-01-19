using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;

namespace Fabric.Player.PanelTypes
{
    public class FabricPlayerFPKExporterPanel : EditorWindow
    {
        private Vector2 scrollPos = Vector2.zero;

        private readonly string[] compressionTypeOptions = new string[] { Native.CompressionType.Wav.ToString()
                                                                        , Native.CompressionType.Vorbis.ToString() };

        private const float INDENT_SIZE = 20.0f;
        private const float SEPARATOR_SIZE = 10.0f;

        GUIStyle fpkFoldoutStyle = null;
        GUIStyle fpkUnavailableFoldoutStyle = null;
        GUIStyle eventFoldoutStyle = null;
        GUIStyle eventUnavailableFoldoutStyle = null;
        GUIStyle multiToggleStyle = null;
        GUIStyle resetButtonStyle = null;

        GUIContent tickIcon = null;

        [MenuItem("Window/SoundMaker/FPK Exporter")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(FabricPlayerFPKExporterPanel), false, "SoundMaker FPK Exporter").Show();
        }

        void OnGUI()
        {
            if (FPKExporter.customExporterSettings == null)
            {
                FPKExporter.LoadCustomExporterSettings();
            }

            SetGUIStyles();

            FabricViewportDrawer viewportDrawer = new FabricViewportDrawer(this);

            PrepareViewport(viewportDrawer);
            viewportDrawer.DrawViewport(ref scrollPos);
        }

        private void SetGUIStyles()
        {
            multiToggleStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(0, 0, 0, 0),
                stretchHeight = false,
                stretchWidth = false,
                fixedHeight = 14.5f,
                fixedWidth = 14.5f,
                alignment = TextAnchor.MiddleCenter
            };

            resetButtonStyle = new GUIStyle(EditorStyles.iconButton)
            {
                padding = new RectOffset(0, 0, 0, 0),
                stretchHeight = false,
                stretchWidth = false,
                fixedHeight = 14.0f,
                fixedWidth = 14.0f,
                alignment = TextAnchor.MiddleCenter
            };

            fpkFoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 225.0f
            };

            Color unavailableColour = Color.red;

            fpkUnavailableFoldoutStyle = new GUIStyle(fpkFoldoutStyle);
            ChangeStyleFontColour(fpkUnavailableFoldoutStyle, unavailableColour);

            eventFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 205.0f
            };

            eventUnavailableFoldoutStyle = new GUIStyle(eventFoldoutStyle);
            ChangeStyleFontColour(eventUnavailableFoldoutStyle, unavailableColour);

            tickIcon = EditorGUIUtility.IconContent("P4_CheckOutRemote");
        }

        private void ChangeStyleFontColour(GUIStyle style, Color unavailableColour)
        {
            style.normal.textColor = unavailableColour;
            style.onNormal.textColor = unavailableColour;
            style.hover.textColor = unavailableColour;
            style.onHover.textColor = unavailableColour;
            style.focused.textColor = unavailableColour;
            style.onFocused.textColor = unavailableColour;
            style.active.textColor = unavailableColour;
            style.onActive.textColor = unavailableColour;
        }

        private void PrepareViewport(FabricViewportDrawer viewportDrawer)
        {
            if (FabricPlayer.Instance == null || !API.IsInitialized(FabricPlayer.Instance.fabricPlayerInstanceId))
            {
                viewportDrawer.AddDrawAction(() =>
                {
                    GUILayout.Label("No Soundmaker Player component found in scene.");
                });
                return;
            }
            /*
            viewportDrawer.AddDrawAction(() =>
            {
                if (GUILayout.Button("Update"))
                {
                    ScanProjectForEvents();
                }
            });*/

            if (FPKExporter.customExporterSettings.fpkAudioClips.Count > 0)
            {
                DisplayAudioAssetsList(viewportDrawer);
            }
            else
            {
                viewportDrawer.AddDrawAction(() =>
                {
                    GUILayout.Label("No fpks detected in project - load them in via the Soundmaker Player panel.");
                });
            }
        }

        private void ScanProjectForEvents()
        {
            if (FPKExporter.customExporterSettings == null)
            {
                FPKExporter.LoadCustomExporterSettings();
            }

            if (FabricPlayer.Properties == null) return;

            EditorUtility.SetDirty(FPKExporter.customExporterSettings);
            FabricPlayer.Properties.projectEvents.Clear();

            FindEventTriggersInScriptableObjects();
            FindEventTriggerComponents();
            FindFabricPostEventsInScripts();

            EditorUtility.SetDirty(FabricPlayer.Properties);

			FPKExporter.RefreshExporterSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void DisplayAudioAssetsList(FabricViewportDrawer viewportDrawer)
        {
            DrawExportOptions(viewportDrawer);

            viewportDrawer.AddDrawAction(() => GUILayout.Label("Assets", EditorStyles.largeLabel));
            DrawMissingFPKsWarning(viewportDrawer);

            foreach (var fpkIDToEventsPair in FPKExporter.customExporterSettings.fpkAudioClips)
            {
                DrawFPK(viewportDrawer, fpkIDToEventsPair.Key, fpkIDToEventsPair.Value);
            }
        }

        private void DrawExportOptions(FabricViewportDrawer viewportDrawer)
        {
            viewportDrawer.AddSpace(SEPARATOR_SIZE);
            viewportDrawer.AddBeginVertical();

            // Vertical section
            {
                DrawExportToggleSettings(viewportDrawer);

                viewportDrawer.AddSpace(SEPARATOR_SIZE);

                DrawExportButton(viewportDrawer);
            }

            viewportDrawer.AddEndVertical();
        }

        private static void DrawMissingFPKsWarning(FabricViewportDrawer viewportDrawer)
        {
            FPKExporter.FPKAvailabilityStatus fpkAvailabilityStatus = FPKExporter.AllFPKsAreAvailableInSourcesFolder();

            if (fpkAvailabilityStatus == FPKExporter.FPKAvailabilityStatus.OneOrMoreMissing)
            {
                GUIStyle warningStyleBig = new GUIStyle(EditorStyles.boldLabel);
                GUIStyle warningStyleMini = new GUIStyle(EditorStyles.label);

                warningStyleBig.normal.textColor = Color.red;
                warningStyleMini.normal.textColor = Color.red;

                viewportDrawer.AddDrawAction(() => GUILayout.Label("WARNING: One or more FPKs are missing from the FPK source folder!", warningStyleBig));
                viewportDrawer.AddDrawAction(() => GUILayout.Label("Add the missing FPKs to the FPK source folder, and reload them in the Soundmaker Player panel.", warningStyleMini));

                viewportDrawer.AddDrawAction(() =>
                {
                    GUILayout.Space(SEPARATOR_SIZE);
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Remove missing FPK references", EditorStyles.miniButtonRight, GUILayout.Width(235.0f)))
                        {
                            FabricPlayer.Properties.CleanUpMissingFPKs();

                            FPKExporter.RefreshExporterSettings();
                        }
                    }
                    GUILayout.EndHorizontal();
                });
            }
        }

        private static void DrawExportToggleSettings(FabricViewportDrawer viewportDrawer)
        {
            viewportDrawer.AddDrawAction(() =>
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    FPKExporter.customExporterSettings.forceExportAll = GUILayout.Toggle(FPKExporter.customExporterSettings.forceExportAll, "Export all assets", GUILayout.ExpandWidth(false));
                    GUILayout.Space(INDENT_SIZE);
                    FPKExporter.customExporterSettings.exportOnBuild = GUILayout.Toggle(FPKExporter.customExporterSettings.exportOnBuild, "Export on build", GUILayout.ExpandWidth(false));
                    GUILayout.Space(INDENT_SIZE);
                }
                GUILayout.EndHorizontal();

            });
        }

        private static void DrawExportButton(FabricViewportDrawer viewportDrawer)
        {
            viewportDrawer.AddDrawAction(() =>
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(235.0f)))
                    {
                        FPKExporter.Export();
                    }
                    GUILayout.Space(INDENT_SIZE);
                }
                GUILayout.EndHorizontal();
            });
        }

        private void DrawFPK(FabricViewportDrawer viewportDrawer, UInt64 fpkID, NestedList<EventInfo> fpkEvents)
        {
            bool fpkAvailableToProject = API.GUI.FPKIsAvailableInFPKFolders(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkID);
            GUIStyle foldoutStyle = fpkAvailableToProject ? fpkFoldoutStyle : fpkUnavailableFoldoutStyle;

            bool fpkIsFoldedOut = FPKExporter.customExporterSettings.foldedOutFPKs.Contains(fpkID);

            viewportDrawer.AddSpace(SEPARATOR_SIZE);

            viewportDrawer.AddDrawAction(() =>
            {
                bool foundGroupInfo = FPKExporter.customExporterSettings.fpkGroupInfo.TryGetValue(fpkID, out ExporterGroupInfo groupInfo);
                if (!foundGroupInfo) return;

                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                
                fpkIsFoldedOut = DrawGroupFoldoutHeader(FPKInfo.GetFPKName(fpkID), foldoutStyle, INDENT_SIZE, fpkIsFoldedOut);

                DrawGroupOptions(groupInfo, null, FPKExporter.customExporterSettings.GetEventGroupsForFPK(fpkID));

                UpdateFoldoutState(FPKExporter.customExporterSettings.foldedOutFPKs, fpkID, fpkIsFoldedOut);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(FPKExporter.customExporterSettings);
                }
                GUILayout.EndHorizontal();
            });

            if (fpkIsFoldedOut)
            {
                foreach (var eventInfo in fpkEvents)
                {
                    DrawEvent(viewportDrawer, fpkID, eventInfo.eventName, eventInfo, fpkAvailableToProject);
                }
            }
        }

        private void DrawEvent(FabricViewportDrawer viewportDrawer, UInt64 fpkID, string eventName, EventInfo eventInfo, bool fpkAvailableToProject)
        {
            GUIStyle foldoutStyle = fpkAvailableToProject ? eventFoldoutStyle : eventUnavailableFoldoutStyle;

            var eventKey = new FPKEventKey(fpkID, eventName);

            bool eventIsFoldedOut = FPKExporter.customExporterSettings.foldedOutEvents.Contains(eventKey);

            viewportDrawer.AddSpace(SEPARATOR_SIZE);
            
            viewportDrawer.AddDrawAction(() =>
            {
                bool foundGroupInfo = FPKExporter.customExporterSettings.eventGroupInfo.TryGetValue(eventKey, out ExporterGroupInfo groupInfo);
                if (!foundGroupInfo) return;

                bool foundParentInfo = FPKExporter.customExporterSettings.fpkGroupInfo.TryGetValue(fpkID, out ExporterGroupInfo parentFPKGroupInfo);
                if (!foundParentInfo) return;

                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();

                eventIsFoldedOut = DrawGroupFoldoutHeader(eventName, foldoutStyle, INDENT_SIZE * 2.0f, eventIsFoldedOut);

                DrawGroupOptions(groupInfo, parentFPKGroupInfo);

                UpdateFoldoutState(FPKExporter.customExporterSettings.foldedOutEvents, eventKey, eventIsFoldedOut);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(FPKExporter.customExporterSettings);
                }
                GUILayout.EndHorizontal();
            });

            if (eventIsFoldedOut)
            {
                viewportDrawer.AddSpace(SEPARATOR_SIZE);

                foreach (AudioClipInfo clip in eventInfo.audioClips)
                {
                    DrawAudioClipInfo(viewportDrawer, eventName, clip, fpkAvailableToProject);
                }
            }
        }

        private static bool DrawGroupFoldoutHeader(string headerName, GUIStyle headerStyle, float indentSize, bool isFoldedOut)
        {
            GUILayout.Space(indentSize);

            isFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldedOut, headerName, headerStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.Space(INDENT_SIZE);
            return isFoldedOut;
        }

        private void DrawGroupOptions
        (
            ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo = null
            , List<ExporterGroupInfo> subGroupsInfo = null
        )
        {
            DrawGroupIncludeOnExportHeader(groupInfo, parentGroupInfo, subGroupsInfo);
            DrawGroupCompressionTypeHeader(groupInfo, parentGroupInfo, subGroupsInfo);
            DrawGroupCompressionQualityHeader(groupInfo, parentGroupInfo, subGroupsInfo);

            GUILayout.Space(INDENT_SIZE);
        }

        private static void UpdateFoldoutState<T>(List<T> foldedOutList, T fpkEventKey, bool isFoldedOut)
        {
            bool prevFoldedOut = foldedOutList.Contains(fpkEventKey);
            if (isFoldedOut && !prevFoldedOut)
            {
                foldedOutList.Add(fpkEventKey);
            }
            else if (!isFoldedOut && prevFoldedOut)
            {
                foldedOutList.Remove(fpkEventKey);
            }
        }

        private void DrawGroupIncludeOnExportHeader
        (
            ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo = null
            , List<ExporterGroupInfo> subGroupsInfo = null
        )
        {
            GUIStyle includeOnExportLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            includeOnExportLabelStyle.normal.textColor = groupInfo.includeOnExportSetForGroup && !FPKExporter.customExporterSettings.forceExportAll
                                                        ? Color.white
                                                        : Color.grey;

            EditorGUILayout.LabelField("Include on export (All)", includeOnExportLabelStyle, GUILayout.ExpandWidth(false));

            if (FPKExporter.customExporterSettings.forceExportAll)
            {
                InsertDisabledToggleBox();
                InsertEmptyButtonSpace();
            }
            else if (groupInfo.includeOnExportSetForGroup)
            {
                GUIContent toggleIcon = groupInfo.includeOnExport ? tickIcon : new GUIContent();

                if (GUILayout.Button(toggleIcon, multiToggleStyle, GUILayout.ExpandWidth(false)))
                {
                    bool newToggleValue = !groupInfo.includeOnExport;
                    ApplyGroupSetting(ExporterGroupInfo.GroupSetting.IncludeOnExport, groupInfo, parentGroupInfo, subGroupsInfo, newToggleValue);
                }

                DrawGroupResetButton(groupInfo, parentGroupInfo, subGroupsInfo, ExporterGroupInfo.GroupSetting.IncludeOnExport);
            }
            else
            {
                GUIContent icon = EditorGUIUtility.IconContent("Toolbar Minus");
                
                if (GUILayout.Button(icon, multiToggleStyle, GUILayout.ExpandWidth(false)))
                {
                    const bool newToggleValue = true;
                    ApplyGroupSetting(ExporterGroupInfo.GroupSetting.IncludeOnExport, groupInfo, parentGroupInfo, subGroupsInfo, newToggleValue);
                }

                InsertEmptyButtonSpace();
            }
        }

        private void DrawGroupCompressionTypeHeader
        (
            ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo = null
            , List<ExporterGroupInfo> subGroupsInfo = null
        )
        {
            GUIStyle compressionTypeLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            GUIStyle compressionTypePopupStyle = new GUIStyle(EditorStyles.miniPullDown);

            compressionTypeLabelStyle.normal.textColor = groupInfo.compressionTypeSetForGroup ? Color.white : Color.grey;
            compressionTypePopupStyle.normal.textColor = groupInfo.compressionTypeSetForGroup ? Color.white : Color.grey;
            EditorGUILayout.LabelField("Compression type (All)", compressionTypeLabelStyle, GUILayout.ExpandWidth(false));

            int newCompressionTypeIndex;
            if (groupInfo.compressionTypeSetForGroup)
            {
                newCompressionTypeIndex = EditorGUILayout.Popup(groupInfo.compressionTypeIndex
                                                               , compressionTypeOptions
                                                               , compressionTypePopupStyle
                                                               , GUILayout.ExpandWidth(false));

                DrawGroupResetButton(groupInfo, parentGroupInfo, subGroupsInfo, ExporterGroupInfo.GroupSetting.CompressionType);
            }
            else
            {
                // Add an additional empty option to the start of the compression type options
                // to make it clearer that the all option has been selected (or not)
                List<string> unsetGroupCompressionTypeOptions = compressionTypeOptions.ToList();
                unsetGroupCompressionTypeOptions.Insert(0, "Set in children");

                newCompressionTypeIndex = EditorGUILayout.Popup(0
                                                               , unsetGroupCompressionTypeOptions.ToArray()
                                                               , compressionTypePopupStyle
                                                               , GUILayout.ExpandWidth(false));

                newCompressionTypeIndex--;

                InsertEmptyButtonSpace();
            }

            if (newCompressionTypeIndex != groupInfo.compressionTypeIndex)
            {
                groupInfo.compressionTypeIndex = newCompressionTypeIndex;

                if (newCompressionTypeIndex != -1) // -1 means the compression type has been set in the children of this group
                {
                    ApplyGroupSetting(ExporterGroupInfo.GroupSetting.CompressionType, groupInfo, parentGroupInfo, subGroupsInfo, newCompressionTypeIndex);
                }
            }
        }

        private void DrawGroupCompressionQualityHeader
        (
            ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo = null
            , List<ExporterGroupInfo> subGroupsInfo = null
        )
        {
            GUIStyle compressionQualityLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            compressionQualityLabelStyle.normal.textColor = groupInfo.compressionQualitySetForGroup ? Color.white : Color.grey;
            EditorGUILayout.LabelField("Compression quality (All)", compressionQualityLabelStyle, GUILayout.ExpandWidth(false));
            int newCompressionQuality = EditorGUILayout.IntSlider(groupInfo.compressionQuality
                                                                    , groupInfo.compressionQualitySetForGroup ? 0 : -1
                                                                    , 100
                                                                    , GUILayout.ExpandWidth(false)
                                                                    , GUILayout.Width(150.0f));

            if (groupInfo.compressionQualitySetForGroup)
            {
                bool resetButtonClicked = DrawGroupResetButton(groupInfo
                                                                , parentGroupInfo
                                                                , subGroupsInfo
                                                                , ExporterGroupInfo.GroupSetting.CompressionQuality);
                if (resetButtonClicked)
                {
                    newCompressionQuality = -1;
                }
            }
            else
            {
                InsertEmptyButtonSpace();
            }

            if (newCompressionQuality != groupInfo.compressionQuality)
            {
                ApplyGroupSetting(ExporterGroupInfo.GroupSetting.CompressionQuality, groupInfo, parentGroupInfo, subGroupsInfo, newCompressionQuality);
            }
        }

        private static void ApplyGroupSetting
        (
            ExporterGroupInfo.GroupSetting groupSetting
            , ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo
            , List<ExporterGroupInfo> subGroupsInfo
            , object newValue
        )
        {
            groupInfo.EnableGroupSetting(groupSetting, newValue);

            if (parentGroupInfo != null)
            {
                parentGroupInfo.DisableGroupSetting(groupSetting);
            }

            if (subGroupsInfo != null)
            {
                foreach (var childInfo in subGroupsInfo)
                {
                    childInfo.EnableGroupSetting(groupSetting, newValue);
                }
            }
        }

        private bool DrawGroupResetButton
        (
            ExporterGroupInfo groupInfo
            , ExporterGroupInfo parentGroupInfo
            , List<ExporterGroupInfo> subGroupsInfo
            , ExporterGroupInfo.GroupSetting groupSetting
        )
        {
            GUIContent refreshIcon = EditorGUIUtility.IconContent("Refresh");
            if (GUILayout.Button(refreshIcon, resetButtonStyle, GUILayout.ExpandWidth(false)))
            {
                groupInfo.DisableGroupSetting(groupSetting);

                if (parentGroupInfo != null)
                {
                    parentGroupInfo.DisableGroupSetting(groupSetting);
                }

                if (subGroupsInfo != null)
                {
                    foreach (var childInfo in subGroupsInfo)
                    {
                        childInfo.DisableGroupSetting(groupSetting);
                    }
                }

                return true;
            }

            return false;
        }

        private void InsertDisabledToggleBox()
        {
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button(tickIcon, multiToggleStyle, GUILayout.ExpandWidth(false));
            EditorGUI.EndDisabledGroup();
        }

        private void InsertEmptyButtonSpace()
        {
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button(new GUIContent(), resetButtonStyle, GUILayout.ExpandWidth(false));
            EditorGUI.EndDisabledGroup();
        }

        private void DrawAudioClipInfo(FabricViewportDrawer viewportDrawer, string eventName, AudioClipInfo clipInfo, bool fpkAvailableToProject)
        {
            GUIStyle leftAlignedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 300.0f
            };
            GUIStyle rightAlignedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            GUIStyle includeOnExportLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            bool isIncludedOnExport = FPKExporter.IncludeClipOnExport(eventName, clipInfo);

            Color textColor = Color.white;
            if (!fpkAvailableToProject) textColor = Color.red;
            else if (!isIncludedOnExport) textColor = Color.grey;

            leftAlignedLabelStyle.normal.textColor = textColor;
            rightAlignedLabelStyle.normal.textColor = textColor;
            includeOnExportLabelStyle.normal.textColor = textColor;

            viewportDrawer.AddDrawAction(() =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(INDENT_SIZE * 3.0f);

                EditorGUILayout.LabelField(clipInfo.name, leftAlignedLabelStyle);

                EditorGUI.BeginChangeCheck();

                DrawAudioClipIncludeOnExport(eventName, clipInfo, includeOnExportLabelStyle, isIncludedOnExport);
                DrawAudioClipCompressionType(eventName, clipInfo, rightAlignedLabelStyle);
                DrawAudioClipCompressionQuality(eventName, clipInfo, rightAlignedLabelStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(FPKExporter.customExporterSettings);
                }

                GUILayout.Space(INDENT_SIZE);
                GUILayout.EndHorizontal();
            });
        }

        private void DrawAudioClipIncludeOnExport(string eventName, AudioClipInfo clipInfo, GUIStyle includeOnExportLabelStyle, bool includedOnExport)
        {
            EditorGUILayout.LabelField("Include on export", includeOnExportLabelStyle, GUILayout.ExpandWidth(false));

            if (FPKExporter.customExporterSettings.forceExportAll)
            {
                InsertDisabledToggleBox();
            }
            else
            {   
                GUIContent icon = includedOnExport ? tickIcon : new GUIContent();

                Color defaultGuiColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

                if (GUILayout.Button(icon, multiToggleStyle, GUILayout.ExpandWidth(false)))
                {
                    ResetDefaultIncludeOnExport(eventName, clipInfo);
                    clipInfo.defaultIncludeOnExport = !clipInfo.defaultIncludeOnExport;
                }

                GUI.backgroundColor = defaultGuiColor;
            }

            InsertEmptyButtonSpace();
        }

        private void DrawAudioClipCompressionType(string eventName, AudioClipInfo clipInfo, GUIStyle rightAlignedLabelStyle)
        {
            int compressionTypeIndex = (int)FPKExporter.GetClipCompressionType(eventName, clipInfo);

            EditorGUILayout.LabelField("Compression type", rightAlignedLabelStyle, GUILayout.ExpandWidth(false));
            int newCompressionTypeIndex = EditorGUILayout.Popup(compressionTypeIndex
                                                                , compressionTypeOptions
                                                                , EditorStyles.miniPullDown
                                                                , GUILayout.ExpandWidth(false));

            InsertEmptyButtonSpace();

            if (newCompressionTypeIndex != compressionTypeIndex)
            {
                ResetDefaultCompressionType(eventName, clipInfo);
                clipInfo.defaultCompressionType = (Native.CompressionType)newCompressionTypeIndex;
            }
        }

        private void DrawAudioClipCompressionQuality(string eventName, AudioClipInfo clipInfo, GUIStyle rightAlignedLabelStyle)
        {
            int compressionQuality = FPKExporter.GetClipCompressionQuality(eventName, clipInfo);

            EditorGUILayout.LabelField("Compression quality", rightAlignedLabelStyle, GUILayout.ExpandWidth(false));
            int newCompressionQuality = EditorGUILayout.IntSlider(compressionQuality
                                                                    , 0
                                                                    , 100
                                                                    , GUILayout.ExpandWidth(false)
                                                                    , GUILayout.Width(150.0f));

            InsertEmptyButtonSpace();

            if (newCompressionQuality != compressionQuality)
            {
                ResetDefaultCompressionQuality(eventName, clipInfo);
                clipInfo.defaultCompressionQuality = newCompressionQuality;
            }
        }


        private void ResetDefaultIncludeOnExport(string eventName, AudioClipInfo clipInfo)
        {
            bool resetDefaults = false;
            if (FPKExporter.customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out ExporterGroupInfo fpkGroupInfo))
            {
                if (fpkGroupInfo.includeOnExportSetForGroup)
                {
                    foreach (AudioClipInfo audioClip in GetFPKGroupAudioClips(clipInfo.fpkID))
                    {
                        audioClip.defaultIncludeOnExport = fpkGroupInfo.includeOnExport;
                    }

                    resetDefaults = true;
                }

                fpkGroupInfo.includeOnExport = false;
                fpkGroupInfo.includeOnExportSetForGroup = false;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (FPKExporter.customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out ExporterGroupInfo eventGroupInfo))
            {
                if (eventGroupInfo.includeOnExportSetForGroup && !resetDefaults)
                {
                    foreach (AudioClipInfo audioClip in GetEventGroupAudioClips(clipInfo.fpkID, eventName))
                    {
                        audioClip.defaultIncludeOnExport = eventGroupInfo.includeOnExport;
                    }
                }

                eventGroupInfo.includeOnExport = false;
                eventGroupInfo.includeOnExportSetForGroup = false;
            }
        }

        private void ResetDefaultCompressionType(string eventName, AudioClipInfo clipInfo)
        {
            bool resetDefaults = false;
            if (FPKExporter.customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out ExporterGroupInfo fpkGroupInfo))
            {
                if (fpkGroupInfo.compressionTypeSetForGroup)
                {
                    foreach (AudioClipInfo audioClip in GetFPKGroupAudioClips(clipInfo.fpkID))
                    {
                        audioClip.defaultCompressionType = (Native.CompressionType)fpkGroupInfo.compressionTypeIndex;
                    }

                    resetDefaults = true;
                }

                fpkGroupInfo.compressionTypeIndex = -1;
                fpkGroupInfo.compressionTypeSetForGroup = false;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (FPKExporter.customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out ExporterGroupInfo eventGroupInfo))
            {
                if (eventGroupInfo.compressionTypeSetForGroup && !resetDefaults)
                {
                    foreach (AudioClipInfo audioClip in GetEventGroupAudioClips(clipInfo.fpkID, eventName))
                    {
                        audioClip.defaultCompressionType = (Native.CompressionType)eventGroupInfo.compressionTypeIndex;
                    }
                }

                eventGroupInfo.compressionTypeIndex = -1;
                eventGroupInfo.compressionTypeSetForGroup = false;
            }
        }

        private void ResetDefaultCompressionQuality(string eventName, AudioClipInfo clipInfo)
        {
            bool resetDefaults = false;
            if (FPKExporter.customExporterSettings.fpkGroupInfo.TryGetValue(clipInfo.fpkID, out ExporterGroupInfo fpkGroupInfo))
            {
                if (fpkGroupInfo.compressionQualitySetForGroup)
                {
                    foreach (AudioClipInfo audioClip in GetFPKGroupAudioClips(clipInfo.fpkID))
                    {
                        audioClip.defaultCompressionQuality = fpkGroupInfo.compressionQuality;
                    }

                    resetDefaults = true;
                }

                fpkGroupInfo.compressionQuality = -1;
                fpkGroupInfo.compressionQualitySetForGroup = false;
            }

            var fpkEventKey = new FPKEventKey(clipInfo.fpkID, eventName);
            if (FPKExporter.customExporterSettings.eventGroupInfo.TryGetValue(fpkEventKey, out ExporterGroupInfo eventGroupInfo))
            {
                if (eventGroupInfo.compressionQualitySetForGroup && !resetDefaults)
                {
                    foreach (AudioClipInfo audioClip in GetEventGroupAudioClips(clipInfo.fpkID, eventName))
                    {
                        audioClip.defaultCompressionQuality = eventGroupInfo.compressionQuality;
                    }
                }

                eventGroupInfo.compressionQuality = -1;
                eventGroupInfo.compressionQualitySetForGroup = false;
            }
        }

        private List<AudioClipInfo> GetFPKGroupAudioClips(UInt64 fpkID)
        {
            var groupAudioClips = new List<AudioClipInfo>();
            if (FPKExporter.customExporterSettings.fpkAudioClips.TryGetValue(fpkID, out NestedList<EventInfo> events))
            {
                foreach (EventInfo eventInfo in events)
                {
                    groupAudioClips.AddRange(eventInfo.audioClips);
                }
            }

            return groupAudioClips;
        }

        private List<AudioClipInfo> GetEventGroupAudioClips(UInt64 fpkID, string eventName)
        {
            var groupAudioClips = new List<AudioClipInfo>();
            if (FPKExporter.customExporterSettings.fpkAudioClips.TryGetValue(fpkID, out NestedList<EventInfo> events))
            {
                EventInfo eventInfo = events.Find(predicate => predicate.eventName == eventName);

                if (eventInfo != null)
                {
                    foreach (AudioClipInfo eventClipInfo in eventInfo.audioClips)
                    {
                        groupAudioClips.AddRange(eventInfo.audioClips);
                    }
                }
            }

            return groupAudioClips;
        }

        private void FindEventTriggersInScriptableObjects()
        {
            string[] guids = AssetDatabase.FindAssets("t:FabricEventTriggerProperties");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FabricEventTriggerProperties so = AssetDatabase.LoadAssetAtPath<FabricEventTriggerProperties>(path);
                if (so != null)
                {
                    string eventName = so.data.GetEventName();

                    if (!FabricPlayer.Properties.projectEvents.Contains(eventName))
                    {
                        FabricPlayer.Properties.projectEvents.Add($"{so.data.GetEventName()}"); // ({path})");//$"{eventTrigger.name} ({path})");
                    }
                    //FabricEventTrigger[] eventTriggers = so..GetComponentsInChildren<FabricEventTrigger>(true);
                    //foreach (FabricEventTrigger eventTrigger in eventTriggers)
                    //{
                    //    string eventName;
                    //    if (eventTrigger.properties)
                    //    {
                    //        eventName = eventTrigger.properties.data.eventName;
                    //    }
                    //    else
                    //    {
                    //        eventName = eventTrigger.data.eventName;
                    //    }
                    //    projectEvents.Add($"{eventName} ({path})");//$"{eventTrigger.name} ({path})");
                    //}
                }
            }
        }

        private void FindEventTriggerComponents()
        {
            // Get all components in the currently loaded scenes
            FabricEventTrigger[] eventTriggers = GameObject.FindObjectsOfType<FabricEventTrigger>();
            foreach (var eventTrigger in eventTriggers)
            {
                string eventName = eventTrigger.data.GetEventName();

                if (!FabricPlayer.Properties.projectEvents.Contains(eventName))
                {
                    FabricPlayer.Properties.projectEvents.Add($"{eventTrigger.data.GetEventName()}");// ({eventTrigger.name})");
                }
            }
        }

        private void FindFabricPostEventsInScripts()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Contains("Fabric.Player.API"))
                    {
                        //Match match = Regex.Match(line, @"PostEvent\([""']([^""']+)[""']");
                        Match match = Regex.Match(line, @"(?:[a-zA-Z_]\w*\.)?PostEvent\([^,]+,\s*([^,]+),\s*([^,]+),");
                        if (match.Success)
                        {
                            // After you've detected that it's a variable (not a string literal)
                            string variableName = match.Groups[2].Value.Trim();
                            if (variableName.StartsWith("\"") && variableName.EndsWith("\""))
                            {
                                variableName = variableName.Replace("\"", "");
                                FabricPlayer.Properties.projectEvents.Add(variableName);
                            }
                            else
                            {
                                // Search for its declaration in the same script
                                string variableValuePattern = $@"\b{variableName}\s*=\s*[""']([^""']+)[""']";
                                Match valueMatch = Regex.Match(File.ReadAllText(path), variableValuePattern);

                                if (valueMatch.Success)
                                {
                                    // Extract the value of the variable
                                    string variableValue = valueMatch.Groups[1].Value;

                                    if (!FabricPlayer.Properties.projectEvents.Contains(variableValue))
                                    {
                                        FabricPlayer.Properties.projectEvents.Add($"{variableValue}");// ({path}: Line {i + 1})");
                                    }
                                }
                                else
                                {
                                    FindVariablesInComponents(variableName);
                                    // If the value is not initialized in the script (e.g., set at runtime or in the inspector), 
                                    // you won't be able to determine it through static code analysis.
                                    //projectEvents.Add($"Variable: {variableName} (Value not found or not initialized in script) ({path}: Line {i + 1})");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FindVariablesInComponents(string variableName)
        {
            // Get all components in the currently loaded scenes
            Component[] components = GameObject.FindObjectsOfType<Component>();

            foreach (Component component in components)
            {
                // Use reflection to get public and serialized private fields
                var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField)));

                foreach (var field in fields)
                {
                    // Check if the field name matches the variable name we're looking for
                    if (field.Name.Equals(variableName))
                    {
                        object value = field.GetValue(component);
                        if (value != null)
                        {
                            string entry = $"{value}";// $"Component: {component.GetType().Name}, Variable: {field.Name}, Value: {value}";

                            // Check if projectEvents contains the event name before adding it
                            if (!FabricPlayer.Properties.projectEvents.Contains(entry))
                            {
                                FabricPlayer.Properties.projectEvents.Add(entry);
                            }
                        }
                    }
                }
            }
        }
    }
}