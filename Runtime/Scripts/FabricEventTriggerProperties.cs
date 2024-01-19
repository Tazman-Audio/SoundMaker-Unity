using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fabric.Player
{
    public enum EventTriggerType
    {
        Awake,
        Start,
        Destroy,
        Update,
        Enable,
        Disable,
        TriggerEnter,
        TriggerExit,
        CollisionEnter,
        CollisionExit,
        MouseUp,
        MouseDown,
        MouseOver,
        MouseEnter,
        MouseExit,
        TriggerOnUpdate,
        TriggerEnter2D,
        TriggerExit2D,
        CollisionEnter2D,
        CollisionExit2D,
        OnMouseUpAsButton,
        OnParticleCollision,
        OnJointBreak
    };

    [Serializable]
    public class EventTriggerAction
    {
        public EventTriggerType triggerType;
        public Fabric.Native.EventAction eventAction;
        public float floatValue;
        public string stringValue;
    }

    [Serializable]
    public class EventTrigger
    {
        [FabricEventAttribute]
        [SerializeField]
        private string eventName = "(None)";

        [SerializeField]
        public List<EventTriggerAction> eventTriggerActions = new List<EventTriggerAction>();

        public string GetEventName()
        {
            string concatEventName = eventName;
            if (concatEventName.Contains("/"))
            {
                concatEventName = concatEventName.Substring(concatEventName.LastIndexOf("/") + 1);
            }

            return concatEventName;
        }

        public string GetFPKName()
        {
            if (eventName.Contains("/"))
            {
                return eventName.Substring(0, eventName.LastIndexOf("/"));
            }
            //else
            return string.Empty;
        }

        public UInt64 GetFPKID()
        {
            if (!FabricPlayer.Instance) return FPKInfo.INVALID_FPKID;

            string fpkName = GetFPKName();
            if (string.IsNullOrEmpty(fpkName)) return FPKInfo.INVALID_FPKID;

            return FPKInfo.GetFPKID(fpkName);
        }

#if UNITY_EDITOR
        public EventTriggerAction AddEventAction()
        {
            EventTriggerAction eventTrigger = new EventTriggerAction();
            eventTriggerActions.Add(eventTrigger);
            return eventTrigger;
        }

        public EventTriggerAction RemoveEventAction(int index)
        {
            EventTriggerAction eventTrigger = eventTriggerActions[index];
            if (eventTrigger != null)
            {
                eventTriggerActions.Remove(eventTrigger);
                return eventTrigger;
            }

            return null;
        }
#endif
    }

    public class FabricEventTriggerProperties : ScriptableObject
    {
        [SerializeField]
        public EventTrigger data = new EventTrigger();

#if UNITY_EDITOR
        [MenuItem("Assets/SoundMaker/Event Trigger")]
        public static void Create()
        {
            FabricEventTriggerProperties asset = ScriptableObject.CreateInstance<FabricEventTriggerProperties>();

            string assetPath = "Assets/FabricEventTrigger.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
