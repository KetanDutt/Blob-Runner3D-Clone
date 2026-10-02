using UnityEngine;

namespace BlobRunner.Rendering
{
    /// <summary>
    /// Initialises the runtime material instance of the blob from a <see cref="ShaderSetting"/>.
    /// (The old implementation wrote the defaults back into the shared material asset in <c>OnDestroy</c> to
    /// undo gameplay changes; with per-instance materials that hack is no longer needed.)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ShaderSettingApplier : MonoBehaviour
    {
        private static readonly string[] ColorProperties =
        {
            "_HeadColor", "_TorsoColor", "_LeftArmColor", "_RightArmColor", "_LeftLegColor", "_RightLegColor"
        };

        private static readonly string[] ScaleProperties =
        {
            "_HeadScale", "_TorsoUpperScale", "_TorsoLowerScale", "_LeftArmUpperScale", "_LeftArmLowerScale",
            "_RightArmUpperScale", "_RightArmLowerScale", "_LeftLegUpperScale", "_LeftLegLowerScale",
            "_RightLegUpperScale", "_RightLegLowerScale"
        };

        [SerializeField] private ShaderSetting setting = null;

        private void Awake()
        {
            if (setting == null)
                return;

            var provider = GetComponent<TransformProvider>();
            Material material = provider != null ? provider.RuntimeMaterial : null;
            if (material == null)
            {
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    material = meshRenderer.material;
            }

            if (material != null)
                Apply(material, setting);
        }

        public static void Apply(Material material, ShaderSetting setting)
        {
            material.SetFloat("_Smooth", setting.Smooth);

            for (int i = 0; i < ScaleProperties.Length; i++)
                material.SetFloat(ScaleProperties[i], setting.ValueByString(ScaleProperties[i]));

            for (int i = 0; i < ColorProperties.Length; i++)
                material.SetColor(ColorProperties[i], setting.TotalColor);
        }
    }
}
