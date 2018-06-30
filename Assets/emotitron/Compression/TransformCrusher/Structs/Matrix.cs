//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

namespace emotitron.Compression
{
	/// <summary>
	/// A struct that holds TRS (Position / Rotation / Scale) values as well as a reference to the crusher that was used to
	/// restore it, and the RotationType enum to indicate if this is using Quaterion or Eulers for rotation.
	/// </summary>
	public struct Matrix
	{
		public TransformCrusher crusher;

		public RotationType rotationType;

		public Vector3 position;
		public Element rotation;
		public Vector3 scale;

		public Matrix(TransformCrusher crusher, Vector3 position, Element rotation, Vector3 scale) : this()
		{
			this.crusher = crusher;
			this.position = position;
			this.scale = scale;
			if (rotation.vectorType == Element.VectorType.Vector3)
			{
				this.rotationType = (rotation.vectorType == Element.VectorType.Vector3) ?
					RotationType.Euler :
					RotationType.Quaternion;
			}
			this.rotation = rotation;
		}

		/// <summary>
		/// Compress this matrix using the crusher it was previously created with.
		/// </summary>
		/// <returns></returns>
		public CompressedMatrix Compress()
		{
			return crusher.Compress(this);
		}

		/// <summary>
		/// Apply this TRS Matrix to the default transform, using the crusher that created this TRS Matrix. Unused Axes will be left unchanged.
		/// </summary>
		public void Apply()
		{
			crusher.Apply(this);
		}
		
		/// <summary>
		/// Apply this TRS Matrix to the supplied transform, using the crusher that created this TRS Matrix. Unused Axes will be left unchanged.
		/// </summary>
		public void Apply(Transform t)
		{
			crusher.Apply(t, this);
		}

		public override string ToString()
		{
			return "MATRIX pos: " + position + " rot: " + rotation  + " scale: " + scale + "  rottype: " + rotationType;
		}
	}

}

