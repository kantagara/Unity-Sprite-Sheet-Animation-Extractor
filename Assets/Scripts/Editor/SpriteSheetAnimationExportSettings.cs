using System;
using UnityEditor;
using UnityEngine;

namespace UnityLab
{
    [Serializable]
    public class SpriteSheetAnimationExportSettings
    {
        [field:SerializeField]
        public DefaultAsset ExportFolder { get; private set; }
        [field:SerializeField]
        public string AnimationPrefix { get; private set; }
        [field:SerializeField]
        public Texture2D SpriteSheet { get; private set; }
    }
}