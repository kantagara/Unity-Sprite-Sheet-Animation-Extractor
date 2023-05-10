using System.Linq;
using UnityEditor;

namespace UnityLab
{
    public class EditorUtils
    {

        public static T LoadAssetOfTypeFromFolder<T>(string path, string additionalQuery = "")
            where T : UnityEngine.Object

        {
            return LoadAllAssetsOfTypeFromFolder<T>(path, additionalQuery).FirstOrDefault();
        }

        public static T[] LoadAllAssetsOfTypeFromFolder<T>(string path, string additionalQuery = "")
            where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name} {additionalQuery}", new []{$"{path}"})
                .Select(x => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(x))).ToArray();
        }
    }
}