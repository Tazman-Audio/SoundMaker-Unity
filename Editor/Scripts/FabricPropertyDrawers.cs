using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fabric.Player
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    [CustomPropertyDrawer(typeof(FabricEventAttribute))]
    public class FabricEventAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            List<string> eventsList = BuildEventsUIList();

            if (eventsList.Count == 0) return;
            int index = eventsList.IndexOf(prop.stringValue);
            int selected = EditorGUILayout.Popup(new GUIContent("Event Name"), index == -1 ? 0 : index, eventsList.ToArray());
            prop.stringValue = eventsList[selected];
        }

        private static List<string> BuildEventsUIList()
        {
            List<string> eventsList = new List<string>();

            const string fpkFileExtension = ".fpk";
            foreach (var eventKeyValue in FabricPlayer.Properties.eventList)
            {
                string fpkName = string.Empty;
                UInt64 fpkID = eventKeyValue.Key;

                if (fpkID != FPKInfo.INVALID_FPKID)
                {
                    if (FabricPlayer.Properties.FPKs.TryGetValue(fpkID, out FPKInfo fpkInfo))
                    {
                        fpkName = fpkInfo.fpkName;

                        int fileExtensionIndex = fpkName.LastIndexOf(fpkFileExtension);
                        fpkName = (fileExtensionIndex < 0) ? fpkName : fpkName.Remove(fileExtensionIndex, fpkFileExtension.Length);
                    }
                }

                foreach (string eventName in eventKeyValue.Value)
                {
                    if (string.IsNullOrEmpty(fpkName))
                    {
                        eventsList.Add(eventName);
                    }
                    else
                    {
                        eventsList.Add(fpkName + "/" + eventName);
                    }
                }
            }

            return eventsList;
        }
    }
}