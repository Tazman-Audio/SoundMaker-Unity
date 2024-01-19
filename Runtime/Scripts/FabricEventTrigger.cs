using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Fabric.Player
{
    [ExecuteAlways]
    [AddComponentMenu("SoundMaker/EventTrigger")]
    public class FabricEventTrigger : MonoBehaviour
    {
        public FabricEventTriggerProperties properties = null;
        public EventTrigger data = new EventTrigger();
        public FabricAudioObject audioObject = null;

        List<EventTriggerAction>[] eventActions = new List<EventTriggerAction>[(int)EventTriggerType.OnJointBreak];

        public void ProcessTriggerType(EventTriggerType type)
        {
            if (eventActions[(int)type] == null) return;
            if (Fabric.Player.FabricPlayer.Instance == null) return;

            EventTrigger triggerData = properties != null ? properties.data : data;
            string eventName = triggerData.GetEventName();
            UInt64 fpkID = triggerData.GetFPKID();

            if (fpkID == FPKInfo.INVALID_FPKID)
            {
                Debug.LogError("Could not find a valid FPK ID associated with the Fabric Event Trigger " + eventName + " on game object " + gameObject.name);
                return;
            }

            foreach (var eventAction in eventActions[(int)type])
            {
                if (eventAction.eventAction == Native.EventAction.PlaySound ||
                    eventAction.eventAction == Native.EventAction.PlayScheduled ||
                    eventAction.eventAction == Native.EventAction.StopSound ||
                    eventAction.eventAction == Native.EventAction.StopScheduled ||
                    eventAction.eventAction == Native.EventAction.SetVolume ||
                    eventAction.eventAction == Native.EventAction.SetPitch ||
                    eventAction.eventAction == Native.EventAction.SetParameter ||
                    eventAction.eventAction == Native.EventAction.SetFadeIn ||
                    eventAction.eventAction == Native.EventAction.SetFadeOut)
                {
                    API.PostEvent(FabricPlayer.Instance.fabricPlayerInstanceId, fpkID, eventName, eventAction.eventAction, audioObject.ID, eventAction.floatValue);
                }
                else if (eventAction.eventAction == Native.EventAction.SetSwitch)
                {
                    API.PostEvent(FabricPlayer.Instance.fabricPlayerInstanceId, fpkID, eventName, eventAction.eventAction, audioObject.ID, eventAction.stringValue);
                }
                else
                {
                    API.PostEvent(FabricPlayer.Instance.fabricPlayerInstanceId, fpkID, eventName, eventAction.eventAction, audioObject.ID);
                }
            }
        }

        public void Awake()
        {
            EventTrigger triggerData = properties != null ? properties.data : data;
            foreach (var action in triggerData.eventTriggerActions)
            {
                if (eventActions[(int)action.triggerType] == null)
                {
                    eventActions[(int)action.triggerType] = new List<EventTriggerAction>();
                }

                eventActions[(int)action.triggerType].Add(action);
            }

            if (audioObject == null)
            {
                audioObject = GetComponent<FabricAudioObject>();
            }

            if(audioObject)
            {
#if UNITY_EDITOR
                //TEMP: Don't auto play in the editor 
                if(EditorApplication.isPlaying)
                {
#endif
                    ProcessTriggerType(EventTriggerType.Awake);
#if UNITY_EDITOR
                }
#endif
            }
        }

        public void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                ProcessTriggerType(EventTriggerType.Start);
            }
        }

        public void Destroy()
        {
            ProcessTriggerType(EventTriggerType.Destroy);
        }

        public void Update()
        {
            ProcessTriggerType(EventTriggerType.Update);
        }

#if UNITY_EDITOR
        EventTriggerDataDrawGUI drawGUI = new EventTriggerDataDrawGUI();
        public void OnGUISetup()
        {
            drawGUI.OnGUISetup(this);
        }

        public void OnGUIDraw()
        {
            audioObject = (FabricAudioObject)EditorGUILayout.ObjectField(new GUIContent("Audio Object"), audioObject, typeof(FabricAudioObject), true);

            GUILayout.Space(2);

            FabricEventTriggerProperties newProperties = (FabricEventTriggerProperties) EditorGUILayout.ObjectField(new GUIContent("Properties Asset"), properties, typeof(FabricEventTriggerProperties), true);

            if(newProperties != properties)
            {
                properties = newProperties;
                drawGUI.OnGUISetup(this);
            }

            GUILayout.Space(2);

            drawGUI.OnGUIDraw();

            string eventName;
            if (properties)
            {
                eventName = properties.data.GetEventName();
            }
            else
            {
                eventName = data.GetEventName();
            }

            if (GUILayout.Button(new GUIContent("Post Event")))
            {
                EventTrigger triggerData = properties != null ? properties.data : data;

                foreach (var action in triggerData.eventTriggerActions)
                {
                    if (eventActions[(int)action.triggerType] != null)
                    {
                        eventActions[(int)action.triggerType].Clear();
                    }
                }

                foreach (var action in triggerData.eventTriggerActions)
                {
                    if (eventActions[(int)action.triggerType] == null)
                    {
                        eventActions[(int)action.triggerType] = new List<EventTriggerAction>();
                    }

                    eventActions[(int)action.triggerType].Add(action);
                }

                ProcessTriggerType(EventTriggerType.Start);
            }
        }
#endif
    }

#if UNITY_EDITOR
    public class EventTriggerDataDrawGUI
    {
        private SerializedObject serializedObject;

        SerializedProperty eventNameProperty;
        EventTrigger eventTrigger;

        ReorderableList eventActions;

      
        public void OnGUISetup(UnityEngine.Object obj)
        {
            SerializedProperty data = null;
            if (obj is FabricEventTrigger)
            {
                FabricEventTrigger d = obj as FabricEventTrigger;
                if (d.properties)
                {
                    serializedObject = new SerializedObject(d.properties);
                    eventTrigger = d.properties.data;
                }
                else
                {
                    serializedObject = new SerializedObject(obj);
                    eventTrigger = d.data;
                }
            }
            else if(obj is FabricEventTriggerProperties)
            {
                serializedObject = new SerializedObject(obj);

                eventTrigger = ((FabricEventTriggerProperties)obj).data;
            }

            data = serializedObject.FindProperty("data");

            eventNameProperty = data.FindPropertyRelative("eventName");

            eventActions = new ReorderableList(eventTrigger.eventTriggerActions, typeof(EventTriggerAction), true, false, true, true);
            eventActions.drawElementCallback =
              (Rect rect, int index, bool isActive, bool isFocused) =>
              {
                  EventTriggerAction action = eventTrigger.eventTriggerActions[index];

                  Rect orig = rect;

                  float width = 25.0f;
                  float offset = 0.0f;
                 
                  if (action != null)
                  {
                      action.triggerType = (EventTriggerType)EditorGUI.EnumPopup(new Rect(rect.x + offset, rect.y, 200, EditorGUIUtility.singleLineHeight), action.triggerType);
                      offset += rect.width / 2.0f;

                      action.eventAction = (Fabric.Native.EventAction)EditorGUI.EnumPopup(new Rect(rect.x + offset, rect.y, 200, EditorGUIUtility.singleLineHeight), action.eventAction);

                      offset = rect.x;

                      if (action.eventAction == Native.EventAction.PlayScheduled || action.eventAction == Native.EventAction.StopScheduled)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          action.floatValue = EditorGUI.FloatField(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Time: "), action.floatValue);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetVolume)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          action.floatValue = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Volume: "), action.floatValue, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetPitch)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          action.floatValue = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Pitch: "), action.floatValue, 0.0f, 3.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetSwitch)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 80.0f;
                          action.stringValue = EditorGUI.TextField(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Switch To: "), action.stringValue);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetParameter)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 80.0f;
                          action.floatValue = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Parameter: "), action.floatValue, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetFadeIn)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          action.floatValue = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Fade In: "), action.floatValue, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetFadeOut)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          action.floatValue = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Fade Out: "), action.floatValue, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetGlobalParameter)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          //action.value = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Volume: "), action.value, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }
                      else if (action.eventAction == Native.EventAction.SetGlobalSwitch)
                      {
                          float original = EditorGUIUtility.labelWidth;
                          EditorGUIUtility.labelWidth = 50.0f;
                          //action.value = EditorGUI.Slider(new Rect(rect.x, rect.y + 25.0f, 300.0f, EditorGUIUtility.singleLineHeight), new GUIContent("Volume: "), action.value, 0.0f, 1.0f);
                          EditorGUIUtility.labelWidth = original;
                      }

                      //SetDSPParameter,
                      //SetTime,
                      //SetMarker
                  }
              };

            eventActions.onAddCallback =
            (ReorderableList list) =>
            {
                eventTrigger.AddEventAction();
            };

            eventActions.onRemoveCallback =
            (ReorderableList list) =>
            {
                eventTrigger.RemoveEventAction(list.index);
            };

            eventActions.elementHeightCallback =
            (int index) =>
            {
                EventTriggerAction action = eventTrigger.eventTriggerActions[index];

                if (action.eventAction == Native.EventAction.SetVolume ||
                    action.eventAction == Native.EventAction.PlayScheduled || 
                    action.eventAction == Native.EventAction.StopScheduled ||
                    action.eventAction == Native.EventAction.SetPitch ||
                    action.eventAction == Native.EventAction.SetSwitch || 
                    action.eventAction == Native.EventAction.SetParameter ||
                    action.eventAction == Native.EventAction.SetFadeIn ||
                    action.eventAction == Native.EventAction.SetFadeOut ||
                    action.eventAction == Native.EventAction.SetGlobalParameter ||
                    action.eventAction == Native.EventAction.SetGlobalSwitch)
                {
                    return 50.0f;
                }

                return 25.0f;
            };

        }

        public void OnGUIDraw()
        {
            GUILayout.BeginVertical("box");
            {
                EditorGUILayout.PropertyField(eventNameProperty);
                
                eventActions.DoLayoutList();

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
            }

            GUILayout.EndVertical();
        }
    }
#endif
}
