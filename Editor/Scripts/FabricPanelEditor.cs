using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;

namespace Fabric.Player
{
    public class FabricPanelEditor : EditorWindow
    {
        private Vector2 screenPosition;
        private Rect bounds;
        private Texture2D texture;
        private Color32[] pixels;
        private GCHandle pixelHandle;
        private UInt64 fabricPanelId = UInt64.MaxValue;

        protected void OnEnable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Fabric.Player.FabricPlayerCallbacks.OnPlayerUIInitialised -= OnPlayerUIInitialised;
            Fabric.Player.FabricPlayerCallbacks.OnPlayerUIInitialised += OnPlayerUIInitialised;

            if (FabricPlayer.Instance == null || !API.GUI.IsInitialized(FabricPlayer.Instance.fabricPlayerGUIInstanceId))
            {
                return;
            }

            fabricPanelId = API.GUI.GetOrCreatePersistentPanel(FabricPlayer.Instance.fabricPlayerGUIInstanceId, GetPanelName());
            Debug.AssertFormat
            (
                fabricPanelId != UInt64.MaxValue
                , "FabricPanelEditor::OnEnable - Unable to find or create a persistent panel for panel type {0} and fabricPlayerGUIInstanceId {1}"
                , GetPanelName()
                , FabricPlayer.Instance.fabricPlayerGUIInstanceId.ToString()
            );
            
            bounds = Rect.zero; // force texture to be recreated at next draw
            Repaint();

            EditorApplication.update -= checkRepaint;
            EditorApplication.update += checkRepaint;
        }

        private void OnPlayerUIInitialised()
        {
            OnEnable();
        }


        void OnGUI()
        {
            if (!FabricPlayer.Instance || !API.GUI.IsInitialized(FabricPlayer.Instance.fabricPlayerGUIInstanceId))
            {
                return;
            }

            Rect r = GUILayoutUtility.GetRect(0, 0, 100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            UnityEngine.Event currentEvent = UnityEngine.Event.current;
            EventType currentEventType = currentEvent.GetTypeForControl(controlID);

            if (currentEventType == EventType.Repaint)
            {
                repaint(r);
            }
            else if (currentEvent.isMouse || currentEventType == EventType.ScrollWheel)
            {
                handleMouseEvent(currentEventType);
            }
            else if (currentEvent.isKey)
            {
                handleKeyEvent(currentEventType);
            }
        }

        protected virtual string GetPanelName() { return "Invalid"; }

        void PlayModeStateChanged(PlayModeStateChange state)
        {
            bool entering = (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode);

            if (entering)
            {
                OnEnable();
            }
        }

        private void setupTexture()
        {
            if (pixelHandle.IsAllocated)
            {
                pixelHandle.Free();
            }

            texture = new Texture2D((int)bounds.width, (int)bounds.height, TextureFormat.ARGB32, false);

            pixels = texture.GetPixels32();
            pixelHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

            API.GUI.SetTexture(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, pixelHandle.AddrOfPinnedObject(), texture.width, texture.height);
        }

        private void checkRepaint()
        {
            if (API.GUI.PanelNeedsRedraw(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId) != 0)
            {
                Repaint();
            }
        }

        public void repaint(Rect r)
        {
            if (!API.GUI.IsInitialized(FabricPlayer.Instance.fabricPlayerGUIInstanceId))
            {
                return;
            }

            Vector2 newScreenPosition = GUIUtility.GUIToScreenPoint(r.position);

            if (bounds != r || screenPosition != newScreenPosition)
            {
                screenPosition = newScreenPosition;
                bounds = r;

                setupTexture();

                API.GUI.SetScreenBounds(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, screenPosition.x, screenPosition.y, bounds.width, bounds.height);
            } 
            else if (texture == null)
            {
                setupTexture();
            }

            // GL.IssuePluginEvent(GetRenderEventFunc(), panelID);
            API.GUI.DrawPanel(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId);

            texture.SetPixels32(pixels);
            texture.Apply();
            EditorGUI.DrawPreviewTexture(bounds, texture);
        }

        public bool handleMouseEvent(EventType eventType)
        {
            if (!API.GUI.IsInitialized(FabricPlayer.Instance.fabricPlayerGUIInstanceId))
            {
                return false;
            }

            Vector2 mousePos = UnityEngine.Event.current.mousePosition;
            EventModifiers mods = UnityEngine.Event.current.modifiers;

            if (!bounds.Contains(mousePos))
            {
                return false;
            }

            Vector2 relativePos = new Vector2(mousePos.x - bounds.x, mousePos.y - bounds.y);

            if (eventType == EventType.MouseMove)
            {
                API.GUI.MouseMove(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, relativePos.x, relativePos.y);
            }
            else if (eventType == EventType.MouseDown)
            {
                API.GUI.MouseDown(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, relativePos.x, relativePos.y, mods, UnityEngine.Event.current.button);
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }
            else if (eventType == EventType.MouseUp)
            {
                API.GUI.MouseUp(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, relativePos.x, relativePos.y, mods);
                GUIUtility.hotControl = 0;
            }
            else if (eventType == EventType.MouseDrag)
            {
                API.GUI.MouseDrag(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, relativePos.x, relativePos.y, mods, UnityEngine.Event.current.button);
            }
            else if (eventType == EventType.ScrollWheel)
            {
                API.GUI.MouseWheel(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, relativePos.x, relativePos.y, mods, UnityEngine.Event.current.delta.x, UnityEngine.Event.current.delta.y);
            }

            UnityEngine.Event.current.Use();

            return true;
        }

        public void handleKeyEvent(EventType eventType)
        {
            if (!API.GUI.IsInitialized(FabricPlayer.Instance.fabricPlayerGUIInstanceId))
            {
                return;
            }

            if (eventType == EventType.KeyDown)
            {
                KeyCode code = UnityEngine.Event.current.keyCode;

                if (code == KeyCode.None)
                {
                    return;
                }

                string key = code.ToString();
                if (!UnityEngine.Event.current.capsLock && !UnityEngine.Event.current.shift && code != KeyCode.Return)
                {
                    key = key.ToLower();
                }

                EventModifiers mods = UnityEngine.Event.current.modifiers;

                API.GUI.KeyEvent(FabricPlayer.Instance.fabricPlayerGUIInstanceId, fabricPanelId, code, mods, key);
            }
        }
    }
}
