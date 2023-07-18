using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Rendering;
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
        public SpriteSheetAnimationExportSettings[] additionalAnimations;

        public AnimationData[] animationData;

        private Vector2 scrollPosition;
        private SerializedObject serializedObject;

        private bool mainAnimationFold;
        private bool additionalAnimationFold;
        
        private Dictionary<(int, int), List<Sprite[,]>> dictSpriteSheets;

        private Dictionary<Sprite, Texture2D> _spriteToTextureDict; 

        private GUIStyle _spritePreviewGuiStyle = new GUIStyle();
        
        private string OriginalAnimatonExportFolderPath =>
            AssetDatabase.GetAssetPath(mainAnimation.ExportFolder);

        #endregion

        #region Untiy Callbacks

        private void OnEnable()
        {
            if (serializedObject == null)
                serializedObject = new SerializedObject(this);
            dictSpriteSheets = new Dictionary<(int, int), List<Sprite[,]>>();
            _spritePreviewGuiStyle.padding = new RectOffset(10, 10, 5, 5);
            _spriteToTextureDict = new Dictionary<Sprite, Texture2D>();
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Creator", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DisplayFields();

            if (GUILayout.Button("Create Animations"))
            {
                CreateAnimationsAndAnimatorController(mainAnimation, false);
                CreateAnimationsAndAnimatorOverrideController();
            }

            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Window creation and data display

        private void DisplayFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(spriteSheetHeight)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(spriteSheetWidth)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(animationFrameRate)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(spacing)));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(animationData)),
                CommonAnimationGuiContent);

            mainAnimationFold = EditorGUILayout.Foldout(mainAnimationFold, MainAnimationGuiContent);

            if (mainAnimationFold)
            {
                EditorGUI.indentLevel++;
                DrawSingleExportSettings(mainAnimation, serializedObject.FindProperty("mainAnimation"));
                EditorGUI.indentLevel--;
            }

            additionalAnimationFold = EditorGUILayout.Foldout(additionalAnimationFold, AdditionalAnimationGuiContent);

            if (additionalAnimationFold)
            {
                EditorGUI.indentLevel++;
                DrawMultipleExportSettings(additionalAnimations);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawMultipleExportSettings(SpriteSheetAnimationExportSettings[] settings)
        {
        }

        private void DrawSingleExportSettings(SpriteSheetAnimationExportSettings settings, SerializedProperty serializedProperty)
        {
            (int? export, int? spriteSheet) previousValues = (settings.ExportFolder?.GetInstanceID(), settings.SpriteSheet?.GetInstanceID());

            EditorGUILayout.PropertyField(serializedProperty);
            serializedObject.ApplyModifiedProperties();
            
            UpdateDictionaryIfValueChanged(settings, previousValues);

            if (settings.ExportFolder == null || settings.SpriteSheet == null)
            {
                EditorGUILayout.HelpBox("Both export folder and sprite sheet must be assigned", MessageType.Error);
                return;
            }
            var newInstanceIdTuple = (settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID());
            DrawAnimationSprites(dictSpriteSheets[newInstanceIdTuple]);
        }
        
        private void DrawAnimationSprites(List<Sprite[,]> dictSpriteSheet)
        {
            float padding = 10;
            foreach (var sprites in dictSpriteSheet)
            {               
                int width = sprites.GetLength(1);
                int height = sprites.GetLength(0);

                for (int i = 0; i < height; i++)
                {
          
                    using (new EditorGUILayout.HorizontalScope(_spritePreviewGuiStyle))
                    {
                        for (int j = 0; j < width; j++)
                        {
                            Sprite sprite = sprites[i, j];
                            if (!_spriteToTextureDict.ContainsKey(sprite))
                                _spriteToTextureDict[sprite] = GenerateTextureFromSprite(sprite);
                            GUILayout.Box(_spriteToTextureDict[sprite]);
                        }
                    }
                    
                }
            }
        }
        
        Texture2D GenerateTextureFromSprite(Sprite aSprite)
        {
            var rect = aSprite.rect;
            var tex = new Texture2D((int)rect.width, (int)rect.height);
            var data = aSprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            tex.SetPixels(data);
            tex.Apply(true);
            return tex;
        }
        
        private void UpdateDictionaryIfValueChanged(SpriteSheetAnimationExportSettings settings, (int? export, int? spriteSheet) previousValues)
        {
            if(!previousValues.export.HasValue || !previousValues.spriteSheet.HasValue) return;
            
            var oldInstanceIdTuple = (previousValues.export.Value, previousValues.spriteSheet.Value);
            
            //If we removed export folder, or sprite sheet, immediately remove them from the dictionary
            if (settings.ExportFolder == null || settings.SpriteSheet == null)
            {
                dictSpriteSheets.Remove(oldInstanceIdTuple);
                return;
            }
            
            //If both values are the same, ignore removal
            if(previousValues.export.Value == settings.ExportFolder.GetInstanceID() &&
               previousValues.spriteSheet.Value == settings.SpriteSheet.GetInstanceID())return;
            
            dictSpriteSheets.Remove(oldInstanceIdTuple);
            var newInstanceIdTuple = (settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID());
            
            //Regenerate Sprite Sheets
            dictSpriteSheets[newInstanceIdTuple] = settings.SpriteSheet.GetAllSpritesFromTexture()
                .Convert1DArrayInto2DArray(spriteSheetWidth, spriteSheetHeight);
        }


        [MenuItem("Window/Animation Creator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationCreatorWindow>("Animation Creator");
        }

        #endregion

        #region Animation Creation

        private void CreateAnimationsAndAnimatorController(SpriteSheetAnimationExportSettings settings, bool isOverride)
        {
            var spritesInMatrix =
                dictSpriteSheets[(settings.ExportFolder.GetInstanceID(), settings.SpriteSheet.GetInstanceID())];

            //Iterate trough list of all the sprites in the matrix (i.e. for clothing, spritesInMatrix will have list of
            // 10 different matrices since we have 10 different variations of the clothing color)
            for (var index = 0; index < spritesInMatrix.Count; index++)
            {
                var sprites = spritesInMatrix[index];

                for (var i = 0; i < animationData.Length; i++)
                {
                    var data = animationData[i];
                    EditorUtility.DisplayProgressBar("Creating Animations",
                        $"Creating animation clips for sprite sheet {settings.SpriteSheet.name} {index}/{spritesInMatrix.Count}",
                        (float)index / spritesInMatrix.Count);
                    CreateAnimationClip(sprites, settings.AnimationPrefix, data.AnimationName, data.ColumnOffset,
                        index.ToString(), spriteSheetWidth, settings.ExportFolder);
                }

                CreateAnimator(settings, index, isOverride);
            }
        }

        private void CreateAnimationsAndAnimatorOverrideController()
        {
            for (var i = 0; i < additionalAnimations.Length; i++)
            {
                var settings = additionalAnimations[i];
                EditorUtility.DisplayProgressBar("Creating Animations",
                    $"Creating Animations for {settings.SpriteSheet.name}",
                    (float)i / additionalAnimations.Length);
                CreateAnimationsAndAnimatorController(settings, true);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        ///     Creating either the original animation controller or the override animation controller
        /// </summary>
        /// <param name="settings">Settings</param>
        /// <param name="index">Current index in the list of matrices</param>
        /// <param name="isOverride">Should we create original animator or an override controller</param>
        private void CreateAnimator(SpriteSheetAnimationExportSettings settings, int index, bool isOverride)
        {
            var animatorSaveLocation = settings.GetAnimatorSavePath(index);
            //We know that this array will never be empty because we always first create animation clips and later on 
            //we create the animator
            var originalAnimationClips = EditorUtils.LoadAllAssetsOfTypeFromFolder<AnimationClip>(
                OriginalAnimatonExportFolderPath);

            if (!isOverride)
            {
                var animatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorSaveLocation);
                //Adding all animations to the animator controller. First one that's added will be the default one.
                foreach (var animationClip in originalAnimationClips)
                    animatorController.AddMotion(animationClip);
                return;
            }

            CreateAnimationOverrideController(settings, index, originalAnimationClips, animatorSaveLocation);
        }

        private void CreateAnimationOverrideController(SpriteSheetAnimationExportSettings settings, int index,
            IEnumerable<AnimationClip> originalAnimationClips, string animatorSaveLocation)
        {
            var originalAnimator =
                EditorUtils.LoadAssetOfTypeFromFolder<AnimatorController>(OriginalAnimatonExportFolderPath);

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


        private void CreateAnimationClip(Sprite[,] sprites, string prefix, string animationName, int startingIndex,
            string suffix, int length, DefaultAsset targetFolder)
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
            CreateClipAndForceSaveAssets(path, clip);
        }

        private static void CreateClipAndForceSaveAssets(string path,
            AnimationClip clip)
        {
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion
    }
}