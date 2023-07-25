using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityLab
{
    public class AnimationCreatorWindow : EditorWindow
    {
        #region Class Fields And Properties

        private static readonly GUIContent MainAnimationGuiContent =
            new("Main Animations", "Settings for Original Animation. " +
                                   "This will end up creating all the clips and animator controller for the given sprite sheet.");

        private static readonly GUIContent AdditionalAnimationGuiContent =
            new("Additional Animations",
                "Settings for Additional Animations.\nHere you should place all the animations that accompany the " +
                "original animation (i.e. original animation is just the naked guy and you want to provide him with clothing, " +
                "this is where the clothing sprite sheet should go)."
                + "\nThese animations will be created with all the appropriate animation clips and, " +
                "instead of the regular animation controller, it will create animation override controller " +
                "(with all animations being matched to the original animation controller)");

        private static readonly GUIContent CommonAnimationGuiContent = new("Common Animations Data",
            "Data that will be used for creating all the animation clips for both original animation and additional animations\n" +
            "\nFor example: If your sprite sheet only contains animations for walking up and walking down, here, your array will have two elements," +
            " and animation name for the first element would be walk up, " +
            "for the second element it would be walk down, and column offset would be 0 and 1 respectively"
            + " so both original and additional animation settings will have that name " +
            "+ the additional data you provide for them in their settings");

        [Range(1, 1000)] public int spriteSheetHeight = 49;

        [Range(1, 1000)] public int spriteSheetWidth = 8;

        [Range(1, 240)] public int animationFrameRate = 12;

        public float spacing = 60;

        public SpriteSheetAnimationExportSettings mainAnimation;

        public SpriteSheetAnimationExportSettings[] overrideAnimations =
            Array.Empty<SpriteSheetAnimationExportSettings>();

        public AnimationData[] animationData;

        private Vector2 _scrollPosition;
        private SerializedObject _serializedObject;

        private bool _additionalAnimationFold;

        private Dictionary<(int, int), List<Sprite[,]>> _exportSpritesDictionary;

        private int _additionalAnimationsCount;

        private Vector2 _currentSpritePreviewScrollPosition;

        private bool _configurationIsErrorFree;

        #endregion

        #region Untiy Callbacks

        private void OnEnable()
        {
            if (_serializedObject == null)
                _serializedObject = new SerializedObject(this);

            _exportSpritesDictionary = new Dictionary<(int, int), List<Sprite[,]>>();
        }

        private void OnGUI()
        {
            _configurationIsErrorFree = true;
            GUILayout.Label("Animation Creator", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DisplayFields();

            if (_configurationIsErrorFree)
            {
                if (GUILayout.Button("Create Animations"))
                {
                    var originalAnimatorAssetPath = AssetDatabase.GetAssetPath(mainAnimation.ExportFolder);
                    CreateAnimationsAndAnimatorController(mainAnimation, false, originalAnimatorAssetPath);

                    CreateAnimationsAndAnimatorOverrideController(overrideAnimations, originalAnimatorAssetPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please fix the errors above before creating animations", MessageType.Error);
            }


            EditorGUILayout.EndScrollView();
            _serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Animation Creation

        public void CreateAnimationsAndAnimatorOverrideController(
            SpriteSheetAnimationExportSettings[] additionalAnimations,
            string originalAnimationExportFolderPath)
        {
            for (var i = 0; i < additionalAnimations.Length; i++)
            {
                var settings = additionalAnimations[i];
                EditorUtility.DisplayProgressBar("Creating Animations",
                    $"Creating Animations for {settings.SpriteSheet.name}",
                    (float)i / additionalAnimations.Length);

                CreateAnimationsAndAnimatorController(settings, false, originalAnimationExportFolderPath);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        ///     Creating Animations and Animator Controller for the given sprite sheet.
        /// </summary>
        public void CreateAnimationsAndAnimatorController(SpriteSheetAnimationExportSettings settings,
            bool isOverride, string originalAnimationExportFolderPath)
        {
            var spritesInMatrix = _exportSpritesDictionary[
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
                    CreateAnimationClip(sprites, settings.AnimationPrefix, data.AnimationName, data.RowOffset,
                        index.ToString(), spriteSheetWidth, settings.ExportFolder, animationFrameRate);
                }

                AssetDatabase.Refresh();

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
        }

        /// <summary>
        ///     Create the actual animation clip
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
        }

        #endregion


        #region Window creation and data display

        private void DisplayFields()
        {
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(spriteSheetHeight)));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(spriteSheetWidth)));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(animationFrameRate)));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(spacing)));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(animationData)),
                CommonAnimationGuiContent);


            DrawSingleAnimationExportSettings(mainAnimation, _serializedObject.FindProperty(nameof(mainAnimation)),
                MainAnimationGuiContent);

            _additionalAnimationFold = EditorGUILayout.Foldout(_additionalAnimationFold, AdditionalAnimationGuiContent);
            if (_additionalAnimationFold)
                DrawMultipleExportSettings();
        }

        private void DrawMultipleExportSettings()
        {
            var oldSize = overrideAnimations.Length;
            var newSize = EditorGUILayout.IntField("Array Size", overrideAnimations.Length);

            ResizeAndPopulateArray(newSize, oldSize);

            for (var i = 0; i < overrideAnimations.Length; i++)
                DrawSingleAnimationExportSettings(overrideAnimations[i],
                    _serializedObject.FindProperty(nameof(overrideAnimations)).GetArrayElementAtIndex(i));
        }

        private void ResizeAndPopulateArray(int newSize, int oldSize)
        {
            if (newSize == oldSize) return;

            Array.Resize(ref overrideAnimations, newSize);

            if (newSize > oldSize)
                for (var i = oldSize; i < newSize; i++)
                    overrideAnimations[i] = new SpriteSheetAnimationExportSettings();
            _serializedObject.Update();
        }


        /// <summary>
        ///     Method responsible for drawing out the animation export settings
        /// </summary>
        /// <param name="settings">Settings that are being drawn</param>
        /// <param name="serializedProperty">
        ///     That setting, but as a serialized property (since we need that for drawing the setting
        ///     itself in editor)
        /// </param>
        /// <param name="guiContent">Optional gui content parameter (used by main (i.e. original) animation)</param>
        private void DrawSingleAnimationExportSettings(SpriteSheetAnimationExportSettings settings,
            SerializedProperty serializedProperty, GUIContent guiContent = null)
        {
            if (settings == null || serializedProperty == null) return;

            (int? export, int? spriteSheet) previousValues =
                (settings.ExportFolder?.GetInstanceID(), settings.SpriteSheet?.GetInstanceID());

            EditorGUI.BeginChangeCheck();

            if (guiContent != null)
                EditorGUILayout.PropertyField(serializedProperty, guiContent);
            else
                EditorGUILayout.PropertyField(serializedProperty, true);

            //If the values have changed, we need to update the dictionary and the serialized object immediately,
            //Because we are using that new data later on in the code and we need to have it updated before that
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                UpdateDictionaryIfValueChanged(settings, previousValues);
            }


            if (settings.ExportFolder == null || settings.SpriteSheet == null)
            {
                _configurationIsErrorFree = false;
                EditorGUILayout.HelpBox("Both export folder and sprite sheet must be assigned", MessageType.Error);
                return;
            }

            var newInstanceIdTuple = (settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID());

            if (animationData.Length == 0)
            {
                _configurationIsErrorFree = false;
                EditorGUILayout.HelpBox("You must provide animation data in order to see the preview",
                    MessageType.Error);
                return;
            }


            EditorGUILayout.LabelField("Animation sprites preview");
            foreach (var data in animationData)
            {
                EditorGUILayout.LabelField($"Animation sprites preview for {data.AnimationName}");
                _configurationIsErrorFree &= AnimationPreview.DrawAnimationSprites(
                    _exportSpritesDictionary[newInstanceIdTuple],
                    ref _currentSpritePreviewScrollPosition, data.RowOffset);
            }
        }

        private void UpdateDictionaryIfValueChanged(SpriteSheetAnimationExportSettings settings,
            (int? export, int? spriteSheet) previousValues)
        {
            if (!previousValues.export.HasValue || !previousValues.spriteSheet.HasValue) return;

            var oldInstanceIdTuple = (previousValues.export.Value, previousValues.spriteSheet.Value);

            //If we removed export folder, or sprite sheet, immediately remove them from the dictionary
            if (settings.ExportFolder == null || settings.SpriteSheet == null)
            {
                _exportSpritesDictionary.Remove(oldInstanceIdTuple);
                return;
            }

            //If both values are the same, ignore removal
            if (previousValues.export.Value == settings.ExportFolder.GetInstanceID() &&
                previousValues.spriteSheet.Value == settings.SpriteSheet.GetInstanceID()) return;

            _exportSpritesDictionary.Remove(oldInstanceIdTuple);
            var newInstanceIdTuple = (settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID());

            //Regenerate Sprite Sheets
            _exportSpritesDictionary[newInstanceIdTuple] = settings.SpriteSheet.GetAllSpritesFromTexture()
                .Convert1DArrayInto3DArray(spriteSheetWidth, spriteSheetHeight);
        }

        [MenuItem("Window/Animation Creator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationCreatorWindow>("Animation Creator");
        }

        #endregion
    }
}