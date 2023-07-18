using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityLab
{
    /// <summary>
    /// Separated concerns of creating animations to a static class, thus some of the methods have a lot of parameters.
    /// </summary>
    public static class AnimationCreator
    {
        /// <summary>
        /// Creating Animations and Animator Override Controller for the given sprite sheet.
        /// </summary>
        public static void CreateAnimationsAndAnimatorOverrideController(
            SpriteSheetAnimationExportSettings[] additionalAnimations,
            AnimationData[] animationData, string originalAnimationExportFolderPath,
            int spriteSheetWidth, Dictionary<(int, int), List<Sprite[,]>> exportSpritesDictionary, float frameRate)
        {
            for (var i = 0; i < additionalAnimations.Length; i++)
            {
                var settings = additionalAnimations[i];
                EditorUtility.DisplayProgressBar("Creating Animations",
                    $"Creating Animations for {settings.SpriteSheet.name}",
                    (float)i / additionalAnimations.Length);

                CreateAnimationsAndAnimatorController(settings, animationData, false,
                    originalAnimationExportFolderPath, spriteSheetWidth, exportSpritesDictionary,
                    frameRate);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Creating Animations and Animator Controller for the given sprite sheet.
        /// </summary>
        public static void CreateAnimationsAndAnimatorController(SpriteSheetAnimationExportSettings settings,
            AnimationData[] animationData, bool isOverride, string originalAnimationExportFolderPath,
            int spriteSheetWidth, Dictionary<(int, int), List<Sprite[,]>> exportSpritesDictionary, float frameRate)
        {
            var spritesInMatrix =
                exportSpritesDictionary[
                    (settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID())];

            //Iterate trough list of all the sprites in the matrix (i.e. for clothing, spritesInMatrix will have list of
            // 10 different variations of the clothing color)
            for (var index = 0; index < spritesInMatrix.Count; index++)
            {
                var sprites = spritesInMatrix[index];

                foreach (var data in animationData)
                {
                    EditorUtility.DisplayProgressBar("Creating Animations",
                        $"Creating animation clips for sprite sheet {settings.SpriteSheet.name} {index}/{spritesInMatrix.Count}",
                        (float)index / spritesInMatrix.Count);
                    CreateAnimationClip(sprites, settings.AnimationPrefix, data.AnimationName, data.ColumnOffset,
                        index.ToString(), spriteSheetWidth, settings.ExportFolder, frameRate);
                }

                CreateAnimator(settings, index, isOverride, originalAnimationExportFolderPath);
            }
        }
        
        private static void CreateAnimator(SpriteSheetAnimationExportSettings settings, int index, bool isOverride,
            string originalAnimationExportFolderPath)
        {
            var animatorSaveLocation = settings.GetAnimatorSavePath(index);
            //We know that this array will never be empty because we always first create animation clips and later on 
            //we create the animator
            //We need them for two reasons:
            //1. For the original animator controller -> To add them to the animator controller
            //2. For the override controller -> To tell the override controller WHICH animation clips will be overriden (Zip method)
            var originalAnimationClips = EditorUtils.LoadAllAssetsOfTypeFromFolder<AnimationClip>(
                originalAnimationExportFolderPath);

            if (!isOverride)
            {
                var animatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorSaveLocation);
                //Adding all animations to the animator controller. First one that's added will be the default one.
                foreach (var animationClip in originalAnimationClips)
                    animatorController.AddMotion(animationClip);
                return;
            }

            CreateAnimationOverrideController(settings, index, originalAnimationClips, animatorSaveLocation,
                originalAnimationExportFolderPath);
        }

        private static void CreateAnimationOverrideController(SpriteSheetAnimationExportSettings settings, int index,
            IEnumerable<AnimationClip> originalAnimationClips, string animatorSaveLocation,
            string originalAnimationExportFolderPath)
        {
            var originalAnimator =
                EditorUtils.LoadAssetOfTypeFromFolder<AnimatorController>(originalAnimationExportFolderPath);

            var overrideController = new AnimatorOverrideController
            {
                runtimeAnimatorController = originalAnimator
            };

            //Since all animations at the end have the same suffix (which is the index in the list of matrices)
            //We can be sure that we're getting the correct animations always.
            //You can check that yourself by typing t: AnimationClip 1 for example in the search section of the Unity Editor project window 
            //It should give you list of all the animations that have 1 as their suffix.
            var overridenAnimations =
                EditorUtils.LoadAllAssetsOfTypeFromFolder<AnimationClip>(
                    AssetDatabase.GetAssetPath(settings.ExportFolder), index.ToString());

            //Given original animations and overriden animations, with zip function we create a kvp that we'll be used 
            //To match overriden animations to the original ones. Zip function essentially does this:
            //Given two arrays [1,2,3] [4,5,6] => [(1,4), (2,5), (3,6)]
            var pairs = originalAnimationClips.Zip(overridenAnimations,
                (original, overriden) => new KeyValuePair<AnimationClip, AnimationClip>(original, overriden));

            overrideController.ApplyOverrides(pairs.ToList());

            AssetDatabase.CreateAsset(overrideController, animatorSaveLocation);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    
        /// <summary>
        /// Create the actual animation clip
        /// </summary>
        /// <param name="sprites">2D array of all the sprites</param>
        /// <param name="prefix">prefix that will stand before the animation name</param>
        /// <param name="animationName">actual animation name (i.e. walk_down)</param>
        /// <param name="startingIndex">what is the starting row for this animation</param>
        /// <param name="suffix"></param>
        /// <param name="length">length is always the animation width</param>
        /// <param name="targetFolder">Where to export this</param>
        /// <param name="animationFrameRate">How fast do you want animation to be played (frames per second)</param>
        private static void CreateAnimationClip(Sprite[,] sprites, string prefix, string animationName,
            int startingIndex,
            string suffix, int length, DefaultAsset targetFolder, float animationFrameRate)
        {
            var clip = new AnimationClip
            {
                frameRate = animationFrameRate,
                wrapMode = WrapMode.Loop
            };

            //Since we can't access loopTime directly from the clip, we had to modify it like this.
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
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
                spriteKeyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[startingIndex, i]
                };

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);
            var path = $"{AssetDatabase.GetAssetPath(targetFolder)}/{prefix}_{animationName}_{suffix}.anim";
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}