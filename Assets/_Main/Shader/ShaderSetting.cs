using UnityEngine;

namespace BlobRunner.Rendering
{
    /// <summary>Default look of the blob (colour, smoothing, per-part radii). Applied by <see cref="ShaderSettingApplier"/>.</summary>
    [CreateAssetMenu(menuName = "Blob Runner/Shader Setting")]
    public class ShaderSetting : ScriptableObject
    {
        public Color TotalColor = Color.yellow;
        public float Smooth = 17.5F;
        public float HeadScale = 0;
        public float TorsoUpperScale = 0;
        public float TorsoLowerScale = 0;
        public float LeftArmUpperScale = 0;
        public float LeftArmLowerScale = 0;
        public float RightArmUpperScale = 0;
        public float RightArmLowerScale = 0;
        public float LeftLegUpperScale = 0;
        public float LeftLegLowerScale = 0;
        public float RightLegUpperScale = 0;
        public float RightLegLowerScale = 0;

        /// <summary>Looks up a "*Scale" shader property name. Unknown names return 0.</summary>
        public float ValueByString(string value)
        {
            switch (value)
            {
                case "_HeadScale":
                    return HeadScale;
                case "_TorsoUpperScale":
                    return TorsoUpperScale;
                case "_TorsoLowerScale":
                case "_TorsoMidScale": // legacy name used by older shader versions
                    return TorsoLowerScale;
                case "_LeftArmUpperScale":
                    return LeftArmUpperScale;
                case "_LeftArmLowerScale":
                    return LeftArmLowerScale;
                case "_RightArmUpperScale":
                    return RightArmUpperScale;
                case "_RightArmLowerScale":
                    return RightArmLowerScale;
                case "_LeftLegUpperScale":
                    return LeftLegUpperScale;
                case "_LeftLegLowerScale":
                    return LeftLegLowerScale;
                case "_RightLegUpperScale":
                    return RightLegUpperScale;
                case "_RightLegLowerScale":
                    return RightLegLowerScale;
                default:
                    return 0;
            }
        }
    }
}
