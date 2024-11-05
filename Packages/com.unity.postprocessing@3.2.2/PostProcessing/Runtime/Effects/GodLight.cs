using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{

    [ExecuteInEditMode]
    public class GodLight : MonoBehaviour
    {
        //体积光颜色
        public Color color = Color.green;
        //光强度
        [Range(0.0f, 50.0f)]
        public float intensity = 10.0f;
        //径向模糊uv采样偏移值
        [Range(0.0f, 10.0f)]
        public float radialOffset = 1;
        //产生体积光的范围
        [Range(0.0f, 5.0f)]
        public float radius = 2.0f;
        //提取高亮结果Pow倍率，适当降低颜色过亮的情况
        [Range(1.0f, 6.0f)]
        public float powFactor = 3.0f;

        //光深度
        [Range(0.0f, 1.0f)]
        public float depth = 0.0f;

        public Texture cookieTex;

        public Vector2 cookieScale = Vector2.one;

        public Vector2 cookieOffset = Vector2.zero;

        private void OnDrawGizmos()
        {
            Color oldcolor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, 0.25f);
            Gizmos.color = oldcolor;
        }

        public static HashSet<GodLight> lightset = new HashSet<GodLight>();

        private void OnEnable()
        {
            lightset.Add(this);
        }

        private void OnDisable()
        {
            lightset.Remove(this);
        }

        public static int ActiveLightCount
        {
            get
            {
                int ret = 0;
                foreach (var light in lightset)
                {
                    if (light.isActiveAndEnabled)
                        ret++;
                }
                return ret;
            }
        }
    }

}