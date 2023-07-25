using System;
using UnityEngine;

namespace UnityLab
{
    /// <summary>
    /// Data that all animations (so original and override) share between each other
    /// </summary>
    [Serializable]
    public class AnimationData
    {
        /// <summary>
        /// Actual animation name, walk_up, walk_down etc. Prefixes are added when generating the animation (see: SpriteSheetAnimationExportSettings)
        /// </summary>
        [field: SerializeField] public string AnimationName { get; private set; }
        /// <summary>
        /// What is the position of the animation in the sprite sheet
        /// </summary>
        [field: SerializeField] public int RowOffset { get; private set; }
       
    }
}