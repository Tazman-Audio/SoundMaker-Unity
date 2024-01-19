using Fabric.Player;
using Fabric.Player.PanelTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[CustomEditor(typeof(FabricPlayerProperties))]
public class FabricPlayerPropertiesEditor : Editor
{
    private FabricPlayerProperties properties = null;

    private GUIStyle styleNormal = GUIStyle.none;
    private GUIStyle unavailableStyleNormal = GUIStyle.none;
    private GUIStyle styleMini = GUIStyle.none;
    private GUIStyle unavailableStyleMini = GUIStyle.none;
    private GUIStyle messageStyleBig = GUIStyle.none;
    private GUIStyle messageStyleMini = GUIStyle.none;

    private const float SEPARATOR_SIZE = 10.0f;

    private void OnEnable()
    {
        properties = target as FabricPlayerProperties;
    }

    public override void OnInspectorGUI()
    {
        SetUpStyles();

        if (FabricPlayer.Instance == null || !API.IsInitialized(FabricPlayer.Instance.fabricPlayerInstanceId))
        {
            GUILayout.Label("No Soundmaker Player component found in scene.", messageStyleBig);
            return;
        }

        if (properties.FPKs.Count <= 0)
        {
            GUILayout.Label("No fpks detected in project.", messageStyleBig);
            GUILayout.Label("Load them in via the Soundmaker Player panel.", messageStyleMini);
            return;
        }

        DrawFPKSearchDirectoryField();
        DrawMissingFPKsWarning();
        DrawFPKs();
    }

    private void DrawFPKSearchDirectoryField()
    {
        EditorGUILayout.LabelField("The location of the fpk search directory relative to the Assets folder.", styleMini);
        
		EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("FPK Search Directory", styleNormal);
        properties.fpkSearchDirectory = EditorGUILayout.TextField(properties.fpkSearchDirectory);

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            FabricPlayer.Instance.SavePlayerProperties();
        }

        GUILayout.Space(SEPARATOR_SIZE);
    }

    private void SetUpStyles()
    {
        styleNormal = new GUIStyle(EditorStyles.boldLabel);

        unavailableStyleNormal = new GUIStyle(EditorStyles.boldLabel);
        ChangeStyleFontColour(unavailableStyleNormal, Color.red);

        styleMini = new GUIStyle(EditorStyles.miniLabel);
        styleMini.wordWrap = true;

        unavailableStyleMini = new GUIStyle(EditorStyles.miniLabel);
        unavailableStyleMini.wordWrap = true;
        ChangeStyleFontColour(unavailableStyleMini, Color.red);

        messageStyleBig = new GUIStyle(EditorStyles.boldLabel);
        messageStyleBig.wordWrap = true;

        messageStyleMini = new GUIStyle(EditorStyles.label);
        messageStyleMini.wordWrap = true;
    }

    private void DrawMissingFPKsWarning()
    {
        FPKExporter.FPKAvailabilityStatus fpkAvailabilityStatus = FPKExporter.AllFPKsAreAvailableInSourcesFolder();

        if (fpkAvailabilityStatus == FPKExporter.FPKAvailabilityStatus.OneOrMoreMissing)
        {
            GUIStyle warningStyleBig = new GUIStyle(EditorStyles.boldLabel);
            warningStyleBig.wordWrap = true;
            ChangeStyleFontColour(warningStyleBig, Color.red);

            GUIStyle warningStyleMini = new GUIStyle(EditorStyles.label);
            warningStyleMini.wordWrap = true;
            ChangeStyleFontColour(warningStyleMini, Color.red);

            GUILayout.Label("WARNING: One or more FPKs are missing from the FPK source folder!", warningStyleBig);
            GUILayout.Label("Add the missing FPKs to the FPK source folder, and reload them in the Soundmaker Player panel.", warningStyleMini);

            GUILayout.Space(SEPARATOR_SIZE);

            if (GUILayout.Button("Remove missing FPK references"))
            {
                properties.CleanUpMissingFPKs();

                FPKExporter.RefreshExporterSettings();
            }
        }
    }

    private void DrawFPKs()
    {
        GUILayout.Label("FPKs", new GUIStyle(EditorStyles.largeLabel));

        foreach (var fpkInfoPair in properties.FPKs)
        {
            UInt64 fpkID = fpkInfoPair.Key;
            bool fpkAvailableToProject = API.GUI.FPKIsAvailableInFPKFolders(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fpkID);
            GUIStyle fpkLabelStyle = fpkAvailableToProject ? styleNormal : unavailableStyleNormal;
            GUIStyle fpkContentsStyle = fpkAvailableToProject ? styleMini : unavailableStyleMini;

            GUILayout.Label(fpkInfoPair.Value.fpkName, fpkLabelStyle);

            DrawRow("ID : ", fpkID.ToString(), fpkContentsStyle);
            DrawRow("Num Events : ", fpkInfoPair.Value.events.Count.ToString(), fpkContentsStyle);
            DrawRow("Num Assets : ", fpkInfoPair.Value.GetAllAudioClipInfos().Count.ToString(), fpkContentsStyle);
            DrawRow("Path : ", fpkInfoPair.Value.path, fpkContentsStyle);

            GUILayout.Space(SEPARATOR_SIZE);
        }
    }

    private static void DrawRow(string lSide, string rSide, GUIStyle style)
    {
        GUIStyle rStyle = new GUIStyle(style);
        rStyle.alignment = TextAnchor.MiddleRight;

        GUILayout.BeginHorizontal();
        GUILayout.TextField(lSide, style);
        GUILayout.TextField(rSide, rStyle);
        GUILayout.EndHorizontal();
    }

    private static void ChangeStyleFontColour(GUIStyle style, Color colour)
    {
        style.normal.textColor = colour;
        style.onNormal.textColor = colour;
        style.hover.textColor = colour;
        style.onHover.textColor = colour;
        style.focused.textColor = colour;
        style.onFocused.textColor = colour;
        style.active.textColor = colour;
        style.onActive.textColor = colour;
    }
}
