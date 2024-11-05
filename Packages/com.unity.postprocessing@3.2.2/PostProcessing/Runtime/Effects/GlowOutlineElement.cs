using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


namespace UnityEngine.Rendering.PostProcessing
{
    [ExecuteInEditMode]
    public class GlowOutlineElement : MonoBehaviour
    {
        //[System.NonSerialized]
        public Color color = Color.red;

        private void OnEnable()
        {
            GlowOutlineRenderer.AddElement(this);
        }

        private void OnDisable()
        {
            GlowOutlineRenderer.RemoveElement(this);
        }
    }
 
}
