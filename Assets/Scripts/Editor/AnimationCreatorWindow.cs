using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityLab
{
    public class AnimationCreatorWindow : EditorWindow
    {
        public int spriteSheetHeight = 49;
        public int spriteSheetWidth = 8;
        public int frameRate = 12;

        public SpriteSheetAnimationExportSettings originalAnimationExportSettings;
        public SpriteSheetAnimationExportSettings[] overrideSpriteSheetExportSettings;

        public CommonAnimationData[] commonAnimationData;


        [MenuItem("Window/Animation Creator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationCreatorWindow>("Animation Creator");
        }

        private Vector2 scrollPosition;
        private SerializedObject serializedObject;

        private void OnGUI()
        {
            GUILayout.Label("Animation Creator", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (serializedObject == null)
                serializedObject = new SerializedObject(this);


            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(spriteSheetHeight)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(spriteSheetWidth)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(frameRate)));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(originalAnimationExportSettings)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(overrideSpriteSheetExportSettings)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(commonAnimationData)));

            if (GUILayout.Button("Create Animations"))
            {
                CreateAnimationsAndAnimatorController(originalAnimationExportSettings, false);

                for (int i = 0; i < overrideSpriteSheetExportSettings.Length; i++)
                {
                    var settings = overrideSpriteSheetExportSettings[i];
                    EditorUtility.DisplayProgressBar("Creating Animations",
                        $"Creating Animations for {settings.SpriteSheet.name}",
                        (float)i / overrideSpriteSheetExportSettings.Length);
                    CreateAnimationsAndAnimatorController(settings, true);
                }

                EditorUtility.ClearProgressBar();
            }


            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateAnimationsAndAnimatorController(SpriteSheetAnimationExportSettings settings, bool isOverride)
        {
            var spriteSheetPath = AssetDatabase.GetAssetPath(settings.SpriteSheet);

            var allSprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>()
                .OrderBy(x => x.name, new SpriteNameComparer()).ToArray();
            var spritesInMatrix = allSprites.Convert1DArrayInto2DArray(spriteSheetWidth, spriteSheetHeight);

            for (int index = 0; index < spritesInMatrix.Count; index++)
            {
                var sprites = spritesInMatrix[index];

                for (var i = 0; i < commonAnimationData.Length; i++)
                {
                    var data = commonAnimationData[i];
                    EditorUtility.DisplayProgressBar("Creating Animations",
                        $"Creating animation clips for sprite sheet {settings.SpriteSheet.name} {index}/{spritesInMatrix.Count}",
                        (float)index / spritesInMatrix.Count);
                    CreateAnimationClip(sprites, settings.AnimationPrefix, data.AnimationName, data.ColumnOffset,
                        index.ToString(), data.Length, settings.ExportFolder);
                }

                CreateAnimator(settings, index, isOverride);
            }
        }

        private void CreateAnimator(SpriteSheetAnimationExportSettings settings, int index, bool isOverride)
        {
            var animatorControllerPath =
                $"{AssetDatabase.GetAssetPath(settings.ExportFolder)}/{settings.AnimationPrefix}_{index}.controller";
            var originalAnimationClips =
                EditorUtils.LoadAllAssetsOfTypeFromFolder<AnimationClip>(
                    $"{AssetDatabase.GetAssetPath(originalAnimationExportSettings.ExportFolder)}");

            if (!isOverride)
            {
                var animatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorControllerPath);
                foreach (var animationClip in originalAnimationClips)
                {
                    animatorController.AddMotion(animationClip);
                }

                return;
            }

            var originalAnimator =
                EditorUtils.LoadAssetOfTypeFromFolder<AnimatorController>(
                    AssetDatabase.GetAssetPath(originalAnimationExportSettings.ExportFolder));

            var overrideController = new AnimatorOverrideController
            {
                runtimeAnimatorController = originalAnimator
            };

            var overridenAnimations = EditorUtils.LoadAllAssetsOfTypeFromFolder<AnimationClip>(AssetDatabase.GetAssetPath(settings.ExportFolder), index.ToString());

            var pairs = originalAnimationClips.Zip(overridenAnimations,
                (original, overriden) => new KeyValuePair<AnimationClip, AnimationClip>(original, overriden));
            
            overrideController.ApplyOverrides(pairs.ToList());
            
            AssetDatabase.CreateAsset(overrideController, animatorControllerPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
        }

        private void CreateAnimationClip(Sprite[,] sprites, string prefix, string animationName, int startingIndex,
            string suffix, int length, DefaultAsset targetFolder)
        {
            var clip = new AnimationClip()
            {
                frameRate = frameRate,
                wrapMode = WrapMode.Loop
            };

            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            var spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var spriteKeyFrames = new ObjectReferenceKeyframe[length];

            for (var i = 0; i < length; i++)
            {
                spriteKeyFrames[i] = new ObjectReferenceKeyframe()
                {
                    time = i / clip.frameRate,
                    value = sprites[startingIndex, i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

            AssetDatabase.CreateAsset(clip,
                $"{AssetDatabase.GetAssetPath(targetFolder)}/{prefix}_{animationName}_{suffix}.anim");
            Debug.Log("CREATING NAIMATIONFASJIO");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}