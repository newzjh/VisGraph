// dnSpy decompiler from Assembly-CSharp.dll class: AnimatedProjector2
using System;
using UnityEngine;


namespace UnityEngine.Rendering.PostProcessing
{

	[ExecuteInEditMode]
	public class GodPlane : MonoBehaviour
	{

		private void Update()
		{
			if (_mr == null)
				_mr = GetComponent<MeshRenderer>();

			if (_material == null && _mr != null)
			{
				if (Application.isPlaying)
					_material = _mr.material;
				else
					_material = _mr.sharedMaterial;
			}

			if (_material == null)
				return;

			_material.SetShaderPassEnabled("ForwardBase", rendered);

			if (!Application.isPlaying)
				return;

			this._elapsedTime += Time.deltaTime * 0.1f;
			float num = 1f / (float)this._fps;
			int num2 = (int)(this._elapsedTime / num);
			if (num2 == 0)
			{
				return;
			}
			this._currIndex += num2;
			if (this._colCount * this._rowCount < this._currIndex + 1)
			{
				this._currIndex %= this._colCount * this._rowCount;
			}
			while (num <= this._elapsedTime)
			{
				this._elapsedTime -= num;
			}
			int num3 = this._currIndex % this._colCount;
			int num4 = this._colCount - this._currIndex / this._colCount - 1;
			Vector2 vector = this._maxOffset - this._minOffset;
			vector.x /= (float)this._colCount;
			vector.y /= (float)this._rowCount;
			Vector2 minOffset = this._minOffset;
			minOffset.x += vector.x * (float)num3;
			minOffset.y += vector.y * (float)num4;
			_material.SetTextureOffset("_MainTex", minOffset);
		}

		[SerializeField]
		private int _fps = 60;

		private Material _material;
		private MeshRenderer _mr;

		[SerializeField]
		private int _colCount = 4;

		[SerializeField]
		private int _rowCount = 4;

		[SerializeField]
		private Vector2 _minOffset = Vector2.zero;

		[SerializeField]
		private Vector2 _maxOffset = Vector2.one;

		private int _currIndex;

		private float _elapsedTime;

		public bool rendered = false;
	}

}