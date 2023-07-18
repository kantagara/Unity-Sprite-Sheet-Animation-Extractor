using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityLab
{
    public static class EditorUtils
    {
        public static T LoadAssetOfTypeFromFolder<T>(string path, string additionalQuery = "")
            where T : Object

        {
            return LoadAllAssetsOfTypeFromFolder<T>(path, additionalQuery).FirstOrDefault();
        }

        public static T[] LoadAllAssetsOfTypeFromFolder<T>(string path, string additionalQuery = "")
            where T : Object
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name} {additionalQuery}", new[] { $"{path}" })
                .Select(x => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(x))).ToArray();
        }
    }
}