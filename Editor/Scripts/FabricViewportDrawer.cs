using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fabric.Player.PanelTypes
{
    /*
     * FabricViewportDrawer
     * - Creates a viewport with a scrollview that is optimised to only draw the GUI elements visible within the window
     * - Prepare the viewport first by adding all of your GUILayout draw calls in order to AddDrawAction
     *   along with the height of the call
     * - When you are ready, call DrawViewport to draw the entire editor window
     */
    public class FabricViewportDrawer
    {
        public delegate void DrawCallback();
        private struct ViewportAction
        {
            public DrawCallback drawCallback;
            public float elementHeight;
        }

        private float accumulativeHeight = 0.0f;

        private float topBufferHeight = 0.0f;
        private float windowHeight = 0.0f;
        private float contentsHeight = 0.0f;

        private List<ViewportAction> drawActions = new List<ViewportAction>();

        public FabricViewportDrawer(EditorWindow _editor)
        {
            windowHeight = _editor.position.height;
        }

        public void AddDrawAction(DrawCallback callback, float? elementHeight = null)
        {
            if (!elementHeight.HasValue)
            {
                elementHeight = EditorGUIUtility.singleLineHeight;
            }

            drawActions.Add(new ViewportAction { drawCallback = callback, elementHeight = elementHeight.Value });
            contentsHeight += elementHeight.Value;
        }

        public void AddSpace(float? elementHeight = null)
        {
            if (!elementHeight.HasValue)
            {
                elementHeight = EditorGUIUtility.singleLineHeight;
            }

            drawActions.Add(new ViewportAction
            {
                drawCallback = () => GUILayout.Space(elementHeight.Value),
                elementHeight = elementHeight.Value
            });

            contentsHeight += elementHeight.Value;
        }

        public void AddBeginVertical()
        {
            drawActions.Add(new ViewportAction
            {
                drawCallback = () => GUILayout.BeginVertical(),
                elementHeight = 0.0f
            });
        }

        public void AddEndVertical()
        {
            drawActions.Add(new ViewportAction
            {
                drawCallback = () => GUILayout.EndVertical(),
                elementHeight = 0.0f
            });
        }

        public void DrawViewport(ref Vector2 scrollPos)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            float scrollMinValue = Mathf.Max(contentsHeight - windowHeight + 25.0f, 0.0f);
            scrollPos.y = Mathf.Min(scrollPos.y, scrollMinValue);

            topBufferHeight = scrollPos.y;
            float bottomBufferHeight = Mathf.Max(contentsHeight - scrollPos.y - windowHeight, 50.0f);

            GUILayout.Space(topBufferHeight);

            foreach (ViewportAction action in drawActions)
            {
                if (action.elementHeight == 0.0f || InViewport(accumulativeHeight))
                {
                    action.drawCallback?.Invoke();
                }
                accumulativeHeight += action.elementHeight;
            }

            GUILayout.Space(bottomBufferHeight);
            GUILayout.EndScrollView();
        }

        private bool InViewport(float height)
        {
            bool inViewport = height >= topBufferHeight && height <= (topBufferHeight + windowHeight);
            return inViewport;
        }
    }
}
