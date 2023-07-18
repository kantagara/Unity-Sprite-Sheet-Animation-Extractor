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
        
        public static Sprite[] GetAllSpritesFromTexture(this Texture2D texture)
        {
            var spriteSheetPath = AssetDatabase.GetAssetPath(texture);
            //LoadAllAssetsAtPath retrieves all the sprites from the texture who's path we've provided
            return AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>()
                .OrderBy(x => x.name, new ExtractedSpriteNumberComparer()).ToArray();
        }
        
        public static string GetAnimatorSavePath(this SpriteSheetAnimationExportSettings settings, int index)
        {
            return $"{AssetDatabase.GetAssetPath(settings.ExportFolder)}/{settings.AnimationPrefix}_{index}.controller";
        }
        
        public static Rect With(this Rect rect, float? newWidth=null,float? newHeight=null, float? x= null, float?y=null)
        {
            var newRect = new Rect(rect);
            newRect.width = newWidth ?? newRect.width;
            newRect.height = newHeight ?? newRect.height;
            newRect.x = x ?? newRect.x;
            newRect.y = y ?? newRect.y;

            return newRect;
        }
    }
}