using System;
using UnityEditor;
using UnityEngine;

namespace UnityLab
{
    /// <summary>
    /// Represents a set of settings used for exporting sprite sheet animations.
    /// </summary>
    [Serializable]
    public class SpriteSheetAnimationExportSettings
    {
        //Where do you want all animations to be exported.
        [field: SerializeField] public DefaultAsset ExportFolder { get;  set; }
        //If you want to indicate specific Animation Name
        //(like character for animations where you only have the character, beard for beard spritesheet etc)
        [field: SerializeField] public string AnimationPrefix { get;  set; }
        //Spritesheet that we get that gets split into individual sprite
        [field: SerializeField] public Texture2D SpriteSheet { get;  set; }
        
        //This is necessary only for Unity Editor, thus the preprocessor directive
        [field:SerializeField] public bool IsFolded { get; set; }
        
    }
}