using UnityEngine;

namespace InspectionDemo
{
    /// <summary>
    /// 运行时创建材质的小工具。
    ///
    /// 注意：Shader.Find 在 Editor 里永远能拿到，但打包时 Unity 会把没用到的
    /// Shader 从包里剔除。所以 Resources/InspectionDefault.mat 是一份兜底资产，
    /// 保证 Standard 着色器会被打进构建包。
    /// </summary>
    public static class MaterialFactory
    {
        private const string FallbackMaterialResource = "InspectionDefault";
        private static Shader cachedShader;

        /// <summary>优先用 Resources 里的材质（打包时最稳），否则退回 Shader.Find。</summary>
        public static Material CreateOpaque(Color color, float metallic = 0f, float smoothness = 0.12f)
        {
            var template = Resources.Load<Material>(FallbackMaterialResource);
            Material material;
            if (template != null && template.shader != null && template.shader.name != "Hidden/InternalErrorShader")
            {
                material = new Material(template);
            }
            else
            {
                var shader = LitShader();
                if (shader == null) shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
            }
            SetColor(material, color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        /// <summary>写颜色。URP 用 _BaseColor，内置管线用 _Color，两个都设一遍最稳。</summary>
        public static void SetColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static Shader LitShader()
        {
            if (cachedShader != null) return cachedShader;

            cachedShader = Shader.Find("Standard");
            if (cachedShader == null) cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedShader == null) cachedShader = Shader.Find("HDRP/Lit");
            if (cachedShader == null) cachedShader = Shader.Find("Sprites/Default");
            return cachedShader;
        }
    }
}
