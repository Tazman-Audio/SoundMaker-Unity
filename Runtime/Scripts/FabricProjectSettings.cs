#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Fabric.Player
{
    public static class ScriptableObjectUtils
    {
        public static T Find<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }
            }
            return null;
        }
    }

    class FabricProjectSettings : ScriptableObject
    {
        public const string assetPath = "Assets/SoundMaker/Editor/FabricProjectSettings.asset";

        // relative path to fabric project from unity project Assets
        [HideInInspector]
        public string fabricProjectRelativePath = "";

        public static FabricProjectSettings GetInstance()
        {
            FabricProjectSettings ret = (FabricProjectSettings) AssetDatabase.LoadAssetAtPath(assetPath, typeof(FabricProjectSettings));
            return ret;
        }

        [MenuItem("Assets/SoundMaker/Project Settings")]
        public static void Create()
        {
            var obj = ScriptableObjectUtils.Find<FabricProjectSettings>();

            if (obj) return;

            CreateInternal();
        }

        static void CreateInternal()
        {
            if (!AssetDatabase.IsValidFolder("Assets/SoundMaker/Editor"))
                AssetDatabase.CreateFolder("Assets/SoundMaker", "Editor");

            FabricProjectSettings asset = ScriptableObject.CreateInstance<FabricProjectSettings>();
            AssetDatabase.CreateAsset(asset, assetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif