using System;
using System.Collections.Generic;
using UnityEditor;
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


        public int spriteSheetHeight = 49;
        public int spriteSheetWidth = 8;
        public int animationFrameRate = 12;
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
                    AnimationCreator.CreateAnimationsAndAnimatorController(mainAnimation, animationData, false,
                        originalAnimatorAssetPath, spriteSheetWidth, _exportSpritesDictionary, animationFrameRate);

                    AnimationCreator.CreateAnimationsAndAnimatorOverrideController(overrideAnimations, animationData,
                        originalAnimatorAssetPath, spriteSheetWidth, _exportSpritesDictionary, animationFrameRate);
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
        /// Method responsible for drawing out the animation export settings
        /// </summary>
        /// <param name="settings">Settings that are being drawn</param>
        /// <param name="serializedProperty">That setting, but as a serialized property (since we need that for drawing the setting itself in editor)</param>
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
                    ref _currentSpritePreviewScrollPosition, data.ColumnOffset);
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
                .Convert1DArrayInto2DArray(spriteSheetWidth, spriteSheetHeight);
        }

        [MenuItem("Window/Animation Creator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationCreatorWindow>("Animation Creator");
        }

        #endregion
    }
}