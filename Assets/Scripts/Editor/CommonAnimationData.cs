using System;
using UnityEngine;

namespace UnityLab
{
    [Serializable]
    public class CommonAnimationData 
    {
       [field:SerializeField] public string AnimationName { get; private set; }
       [field:SerializeField] public int ColumnOffset { get; private set; }
       [field: SerializeField] public int Length { get; private set; } = 8;

    }
}
