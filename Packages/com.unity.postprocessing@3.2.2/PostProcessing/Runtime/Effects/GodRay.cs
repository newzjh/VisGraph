using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the advance Fog effect with the all path.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(GodRayRenderer), "Unity/GodRay")]
    public sealed class GodRay : PostProcessEffectSettings
    {

        [Range(1, 8)]
        public IntParameter blurIteration = new IntParameter { value = 4 }; //Blur��������
        [Range(1, 4)]
        public IntParameter downSample = new IntParameter { value = 2 }; //���ͷֱ��ʱ���

        [ColorUsage(false, true)]
        public ColorParameter colorThreshold = new ColorParameter { value = Color.black };//����������ȡ��ֵ
  

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled && GodLight.ActiveLightCount>0
                && !RuntimeUtilities.scriptableRenderPipelineActive;
        }
    }

    [UnityEngine.Scripting.Preserve]
    internal sealed class GodRayRenderer : PostProcessEffectRenderer<GodRay>
    {
        public override DepthTextureMode GetCameraFlags(PostProcessRenderContext context)
        {
            return DepthTextureMode.Depth;
        }

        private Shader s = null;

        private Vector4 lightRadius = Vector4.one;
        private Vector4 lightPowFactors = Vector4.one;
        private Vector4 lightFactors = Vector4.one;
        private Vector4 radialOffset = Vector4.zero;
        private Color[] lightColors = new Color[4] { Color.green, Color.red, Color.blue, Color.yellow };
        private Vector4[] lightPositions = new Vector4[4] { Vector4.zero, Vector4.zero, Vector4.zero, Vector4.zero };
        private bool[] lightEnables = new bool[4] { false, false, false, false };
        private string[] lightkeywords = new string[4] { "GODRAY0_ON", "GODRAY1_ON", "GODRAY2_ON", "GODRAY3_ON" };
        private string[] cookiekeywords = new string[4] { "COOKIEA_ON", "COOKIEB_ON", "COOKIEC_ON", "COOKIED_ON" };
        private Vector4 lightDepths = Vector4.zero;
        private Texture[] lightCookieTextures = new Texture[4] { null, null, null, null };
        private Vector4[] lightCookieScaleOffsets = new Vector4[4] { Vector4.one, Vector4.one, Vector4.one, Vector4.one };

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;

            if (s == null)
                s = Shader.Find("PostProcessing/GodRay");

            if (s==null)
            {
                cmd.Blit(context.source, context.destination);
                return;
            }

            if (GodLight.ActiveLightCount<=0)
            {
                cmd.Blit(context.source, context.destination);
                return;
            }

            var sheet = context.propertySheets.Get(s);
            sheet.ClearKeywords();

            cmd.BeginSample("GodRay");

            int rtWidth = context.width >> settings.downSample.value;
            int rtHeight = context.height >> settings.downSample.value;
            //RT�ֱ��ʰ���downSameple����
            RenderTexture temp1 = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, context.sourceFormat);

            for (int i = 0; i < 4; i++)
            {
                lightEnables[i] = false;
                lightCookieTextures[i] = null;
                lightCookieScaleOffsets[i].x = 1.0f;
                lightCookieScaleOffsets[i].y = 1.0f;
                lightCookieScaleOffsets[i].z = 0.0f;
                lightCookieScaleOffsets[i].w = 0.0f;
            }

            {
                int i = 0;
                foreach(var godlight in GodLight.lightset)
                {
                    if (!godlight.isActiveAndEnabled)
                        continue;
                    if (i >= 4)
                        break;
                    lightEnables[i] = true;
                    lightRadius[i] = godlight.radius;
                    lightPowFactors[i] = godlight.powFactor;
                    lightFactors[i] = godlight.intensity;
                    radialOffset[i] = godlight.radialOffset;
                    lightColors[i] = godlight.color;
                    lightPositions[i] = context.camera.WorldToViewportPoint(godlight.transform.position);
                    lightDepths[i] = godlight.depth;
                    lightCookieTextures[i] = godlight.cookieTex;
                    lightCookieScaleOffsets[i].x = godlight.cookieScale.x;
                    lightCookieScaleOffsets[i].y = godlight.cookieScale.y;
                    lightCookieScaleOffsets[i].z = godlight.cookieOffset.x;
                    lightCookieScaleOffsets[i].w = godlight.cookieOffset.y;
                    i++;
                }
            }

            //��shader������ΪPropertyId���Լ���float����Vector��һ�𴫵ݸ�Material���ʡһЩ��but������
            sheet.material.SetVector("_ColorThreshold", settings.colorThreshold.value);
            sheet.material.SetFloat("_ScreenRatio", (float)Screen.width / (float)Screen.height);

            sheet.material.SetVector("_ViewPortLightPos0", lightPositions[0]);
            sheet.material.SetVector("_ViewPortLightPos1", lightPositions[1]);
            sheet.material.SetVector("_ViewPortLightPos2", lightPositions[2]);
            sheet.material.SetVector("_ViewPortLightPos3", lightPositions[3]);

            sheet.material.SetVector("_LightRadius", lightRadius);
            sheet.material.SetVector("_LightDepths", lightDepths);
            sheet.material.SetVector("_PowFactors", lightPowFactors);

            for (int i = 0; i < 4; i++)
            {
                if (lightEnables[i])
                    sheet.material.EnableKeyword(lightkeywords[i]);
                else
                    sheet.material.DisableKeyword(lightkeywords[i]);
                if (lightCookieTextures[i])
                    sheet.material.EnableKeyword(cookiekeywords[i]);
                else
                    sheet.material.DisableKeyword(cookiekeywords[i]);
            }

            //������ֵ��ȡ��������,ʹ��pass0���и�����ȡ����Bloom��һ�������Դ�����޳���Դ��Χ��Ĳ���
            cmd.Blit(context.source, temp1, sheet.material, 0);
            sheet.material.SetVector("_ViewPortLightPos0", lightPositions[0]);
            sheet.material.SetVector("_ViewPortLightPos1", lightPositions[1]);
            sheet.material.SetVector("_ViewPortLightPos2", lightPositions[2]);
            sheet.material.SetVector("_ViewPortLightPos3", lightPositions[3]);
            sheet.material.SetVector("_LightRadius", lightRadius);
            //����ģ���Ĳ���uvƫ��ֵ
            Vector4 samplerOffset = radialOffset / context.width;
            //����ģ��������һ�飬��������
            for (int i = 0; i < settings.blurIteration.value; i++)
            {
                RenderTexture temp2 = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, context.sourceFormat);
                Vector4 offset = samplerOffset * (i * 2 + 1);
                sheet.material.SetVector("_offsets", offset);
                cmd.Blit(temp1, temp2, sheet.material, 1);
                offset = samplerOffset * (i * 2 + 2);
                sheet.material.SetVector("_offsets", offset);
                cmd.Blit(temp2, temp1, sheet.material, 1);
                RenderTexture.ReleaseTemporary(temp2);
            }
            sheet.material.SetTexture("_BlurTex", temp1);
            sheet.material.SetTexture("_CookieA", lightCookieTextures[0]);
            sheet.material.SetTexture("_CookieB", lightCookieTextures[1]);
            sheet.material.SetTexture("_CookieC", lightCookieTextures[2]);
            sheet.material.SetTexture("_CookieD", lightCookieTextures[3]);
            sheet.material.SetVector("_CookieA_ScaleOffset", lightCookieScaleOffsets[0]);
            sheet.material.SetVector("_CookieB_ScaleOffset", lightCookieScaleOffsets[1]);
            sheet.material.SetVector("_CookieC_ScaleOffset", lightCookieScaleOffsets[2]);
            sheet.material.SetVector("_CookieD_ScaleOffset", lightCookieScaleOffsets[3]);
            sheet.material.SetVector("_LightColorA", lightColors[0]);
            sheet.material.SetVector("_LightColorB", lightColors[1]);
            sheet.material.SetVector("_LightColorC", lightColors[2]);
            sheet.material.SetVector("_LightColorD", lightColors[3]);
            sheet.material.SetVector("_LightFactors", lightFactors);
            //���ջ�ϣ�������⾶��ģ��ͼ��ԭʼͼƬ��ϣ�pass2
            cmd.Blit(context.source, context.destination, sheet.material, 2);
            //�ͷ������RT
            RenderTexture.ReleaseTemporary(temp1);

            cmd.EndSample("GodRay");
        }
    }
}
