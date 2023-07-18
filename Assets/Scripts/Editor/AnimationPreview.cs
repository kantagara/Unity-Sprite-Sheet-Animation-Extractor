using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityLab
{
    public static class AnimationPreview
    {
        private const int ScrollViewSpriteHeightMultiplier = 3;

        private static readonly GUIStyle GroupPreview = new()
        {
            padding = new RectOffset(20, 20, 10, 10)
        };

        private static readonly Dictionary<Sprite, Texture2D> SpriteToTextureDictionary = new();


        public static bool DrawAnimationSprites(List<Sprite[,]> spritesList, ref Vector2 scrollPosition,
            int columnOffset = 0)
        {
            //Since scrollview took unsually big space, I had to constrain it to use only a single row, and I achieved that
            //With using sprites height and multiplied it with a constant (Currently, 3 seems like a nice number)
            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition, false, false,
                    GUILayout.Height(spritesList[0][0, 0].rect.height * ScrollViewSpriteHeightMultiplier));

            EditorGUILayout.BeginHorizontal(GroupPreview);
            foreach (var sprites in spritesList)
            {
                var width = sprites.GetLength(1);

                if (sprites.GetLength(0) < columnOffset)
                {
                    EditorGUILayout.HelpBox("Column offset is bigger than the number of rows", MessageType.Error);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                    return false;
                }

                for (var j = 0; j < width; j++)
                {
                    var sprite = sprites[columnOffset, j];
                    if (!SpriteToTextureDictionary.ContainsKey(sprite))
                        SpriteToTextureDictionary[sprite] = GenerateTextureFromSprite(sprite);
                    GUILayout.Box(SpriteToTextureDictionary[sprite]);
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            return true;
        }


        private static Texture2D GenerateTextureFromSprite(Sprite aSprite)
        {
            var rect = aSprite.rect;
            var tex = new Texture2D((int)rect.width, (int)rect.height);
            var data = aSprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            tex.SetPixels(data);
            tex.Apply(true);
            return tex;
        }
    }
}