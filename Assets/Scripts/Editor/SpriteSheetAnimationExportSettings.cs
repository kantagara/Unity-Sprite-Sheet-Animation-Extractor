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
        [field: SerializeField] public DefaultAsset ExportFolder { get; private set; }
        //If you want to indicate specific Animation Name
        //(like character for animations where you only have the character, beard for beard spritesheet etc)
        [field: SerializeField] public string AnimationPrefix { get; private set; }
        //Spritesheet that we get that gets split into individual sprite
        [field: SerializeField] public Texture2D SpriteSheet { get; private set; }
    }
}