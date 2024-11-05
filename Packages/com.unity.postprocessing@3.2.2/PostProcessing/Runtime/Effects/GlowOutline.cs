using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


namespace UnityEngine.Rendering.PostProcessing
{

    /// <summary>
    /// This class holds settings for the advance Fog effect with the all path.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(GlowOutlineRenderer), "Unity/GlowOutline")]
    public sealed class GlowOutline : PostProcessEffectSettings
    {

        [Range(0.0f, 2.0f)]
        public FloatParameter _OutlineWidth = new FloatParameter { value = 1.0f };
        [Range(0.0f, 100.0f)]
        public FloatParameter _OutlineScale = new FloatParameter { value = 1.0f };
        [Range(0, 5)]
        public IntParameter _OutlineTimes = new IntParameter { value = 3 };


        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && !RuntimeUtilities.scriptableRenderPipelineActive;
        }
    }

    [UnityEngine.Scripting.Preserve]
    internal sealed class GlowOutlineRenderer : PostProcessEffectRenderer<GlowOutline>
    {
        public override DepthTextureMode GetCameraFlags(PostProcessRenderContext context)
        {
            return DepthTextureMode.None;
        }

        private Shader s = null;
        private static Dictionary<GlowOutlineElement,Renderer[]> renders = new Dictionary<GlowOutlineElement, Renderer[]>();
        private static Dictionary<Color, Material> colormaterials = new Dictionary<Color, Material>();

        public static void AddElement(GlowOutlineElement element)
        {
            Renderer[] rs = element.GetComponentsInChildren<Renderer>();
            renders[element] = rs;
        }

        public static void RemoveElement(GlowOutlineElement element)
        {
            if (renders.ContainsKey(element))
                renders.Remove(element);
        }

        public static int GetElementCount()
        {
            if (renders != null)
                return renders.Count;
            return 0;
        }

        private Color clearcolor = new Color(0, 0, 0, 0);
        private Vector4 glowoffset = new Vector4(1, 0, 0, 1);
        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;

            if (s == null)
                s = Shader.Find("PostProcessing/GlowOutline");

            if (s==null || renders==null || renders.Count<=0)
            {
                cmd.Blit(context.source, context.destination);
                return;
            }

            var sheet = context.propertySheets.Get(s);
            sheet.ClearKeywords();

            sheet.material.SetFloat("_OutlineScale", settings._OutlineScale.value);
            sheet.material.SetFloat("_OutlineWidth", settings._OutlineWidth.value);

            cmd.BeginSample("GlowOutline");

            int tempRT = Shader.PropertyToID("_GlowOutlineTemp");
            int blurRT = Shader.PropertyToID("_GlowOutlineBlur");
            int blurRT2 = Shader.PropertyToID("_GlowOutlineBlur2");
            context.GetScreenSpaceTemporaryRT(cmd, tempRT, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width, context.height);
            context.GetScreenSpaceTemporaryRT(cmd, blurRT, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width/2, context.height/2);
            context.GetScreenSpaceTemporaryRT(cmd, blurRT2, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width/2, context.height/2);
            cmd.SetRenderTargetWithLoadStoreAction(tempRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(true, true, clearcolor);

            if (renders!=null)
            {
                foreach(var pair in renders)
                {
                    GlowOutlineElement element = pair.Key;
                    Renderer[] rs = pair.Value;
                    if (rs == null)
                        continue;
                    Color col = element.color;

                    Material colormaterial = null;
                    if (colormaterials.ContainsKey(col))
                    {
                        colormaterial = colormaterials[col];
                    }
                    if (colormaterial==null)
                    {
                        colormaterial = new Material(sheet.material);
                        colormaterials[col] = colormaterial;
                        colormaterial.SetColor("_OutlineColor", col);
                    }

                    foreach (var r in rs)
                    {
                        if (r.isVisible)
                        {
                            if (r.sharedMaterials != null)
                            {
                                for (int i = 0; i < r.sharedMaterials.Length; i++)
                                {
                                    cmd.DrawRenderer(r, colormaterial, i, 0);
                                }
                            }
                        }
                    }
           
                }
            }

            sheet.material.SetVector("_Offset", glowoffset);

            int src = tempRT;
            int dest = blurRT;
            for (int i = 0; i < settings._OutlineTimes.value; i++)
            {
                cmd.Blit(src, blurRT2, sheet.material, 1);
                cmd.Blit(blurRT2, dest, sheet.material, 2);
                src = dest;
            }

            cmd.SetGlobalTexture(tempRT, tempRT);
            cmd.SetGlobalTexture(blurRT, blurRT);

            cmd.Blit(context.source, context.destination, sheet.material, 3);

            cmd.ReleaseTemporaryRT(tempRT);
            cmd.ReleaseTemporaryRT(blurRT);
            cmd.ReleaseTemporaryRT(blurRT2);

            cmd.EndSample("GlowOutline");
        }
    }
}
