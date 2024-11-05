using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the advance Fog effect with the all path.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(AdvanceFogRenderer), "Unity/AdvanceFog")]
    public sealed class AdvanceFog : PostProcessEffectSettings
    {

        [Range(0.0f, 5.0f)]
        public FloatParameter _FogDensity = new FloatParameter { value = 0.8f }; //雾的浓度
        [ColorUsage(false, true)]
        public ColorParameter _FogColor = new ColorParameter { value = Color.white };//雾的颜色
        [Range(-100.0f, 100.0f)]
        public FloatParameter _FogHeightStart = new FloatParameter { value = -40.0f }; //雾的起始高度
        [Range(-100.0f, 100.0f)]
        public FloatParameter _FogHeightEnd = new FloatParameter { value = 40.0f }; //雾的终止高度

        [Range(0.0f, 500.0f)]
        public FloatParameter _FogDepthStart = new FloatParameter { value = 40.0f }; //雾的起始深度
        [Range(0.0f, 500.0f)]
        public FloatParameter _FogDepthEnd = new FloatParameter { value = 40.0f }; //雾的终止深度

        public TextureParameter _CookieTex = new TextureParameter { value = null };

        [Range(-1.0f, 1.0f)]
        public FloatParameter _CookieSpeedU = new FloatParameter { value = 0.0f };
        [Range(-1.0f, 1.0f)]
        public FloatParameter _CookieSpeedV = new FloatParameter { value = 0.0f };
        [Range(0.0f, 10.0f)]
        public FloatParameter _CookieTilingU = new FloatParameter { value = 1.0f };
        [Range(0.0f, 10.0f)]
        public FloatParameter _CookieTilingV = new FloatParameter { value = 1.0f };
        [Range(-1.0f, 1.0f)]
        public FloatParameter _CookieOffsetU = new FloatParameter { value = 0.0f };
        [Range(-1.0f, 1.0f)]
        public FloatParameter _CookieOffsetV = new FloatParameter { value = 0.0f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && !RuntimeUtilities.scriptableRenderPipelineActive;
        }
    }

    [UnityEngine.Scripting.Preserve]
    internal sealed class AdvanceFogRenderer : PostProcessEffectRenderer<AdvanceFog>
    {
        public override DepthTextureMode GetCameraFlags(PostProcessRenderContext context)
        {
            return DepthTextureMode.Depth;
        }

        private Shader s = null;

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;

            if (s == null)
                s = Shader.Find("PostProcessing/AdvanceFog");

            if (s==null)
            {
                cmd.Blit(context.source, context.destination);
                return;
            }

            var sheet = context.propertySheets.Get(s);
            sheet.ClearKeywords();

            sheet.material.SetFloat("_FogDensity", settings._FogDensity.value);
            sheet.material.SetColor("_FogColor", settings._FogColor.value);
            sheet.material.SetFloat("_FogHeightStart", Mathf.Min(settings._FogHeightStart.value, settings._FogHeightEnd.value));
            sheet.material.SetFloat("_FogHeightEnd", Mathf.Max(settings._FogHeightEnd.value,settings._FogHeightStart.value));
            sheet.material.SetFloat("_FogDepthStart", Mathf.Min(settings._FogDepthStart.value, settings._FogDepthEnd.value));
            sheet.material.SetFloat("_FogDepthEnd", Mathf.Max(settings._FogDepthEnd.value, settings._FogDepthStart.value));

            sheet.material.SetFloat("_CookieSpeedU", settings._CookieSpeedU.value * 0.1f);
            sheet.material.SetFloat("_CookieSpeedV", settings._CookieSpeedV.value * 0.1f);
            sheet.material.SetFloat("_CookieTilingU", settings._CookieTilingU.value);
            sheet.material.SetFloat("_CookieTilingV", settings._CookieTilingV.value);
            sheet.material.SetFloat("_CookieOffsetU", settings._CookieOffsetU.value);
            sheet.material.SetFloat("_CookieOffsetV", settings._CookieOffsetV.value);
            if (settings._CookieTex.value!=null)
                sheet.material.SetTexture("_CookieTex", settings._CookieTex.value);
            else
                sheet.material.SetTexture("_CookieTex", Texture2D.whiteTexture);
            
            cmd.BeginSample("AdvanceFog");
            cmd.Blit(context.source, context.destination, sheet.material);
            cmd.EndSample("AdvanceFog");
        }
    }
}
