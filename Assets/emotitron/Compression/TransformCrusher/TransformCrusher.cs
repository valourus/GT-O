//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

namespace emotitron.Compression
{

	[System.Serializable]
	//[AddComponentMenu("Crusher/Transform Crusher")]
	public class TransformCrusher
	{
		public Transform defaultTransform;
		// Set up the default Crushers so they add up to 64 bits
		public ElementCrusher posCrusher = new ElementCrusher(TRSType.Position, false);
		public ElementCrusher rotCrusher = new ElementCrusher(TRSType.Euler, false)
		{
			xcrusher = new FloatCrusher(BitPresets.Bits12, -90f, 90f, Axis.X, TRSType.Euler, true),
			ycrusher = new FloatCrusher(BitPresets.Bits12, -180f, 180f, Axis.Y, TRSType.Euler, true),
			zcrusher = new FloatCrusher(BitPresets.Disabled, -180f, 180f, Axis.Z, TRSType.Euler, true)
		};
		public ElementCrusher sclCrusher = new ElementCrusher(TRSType.Scale, false)
		{
			uniformAxes = ElementCrusher.UniformAxes.XYZ,
			ucrusher = new FloatCrusher(8, 0f, 2f, Axis.Uniform, TRSType.Scale, true)
		};

		#region Cached compression values

		private int cached_pBits, cached_rBits, cached_sBits;

		private bool cached;

		private void CacheValues()
		{
			this.
			cached_pBits = posCrusher.TallyBits();
			cached_rBits = rotCrusher.TallyBits();
			cached_sBits = sclCrusher.TallyBits();
			cached = true;
		}

		#endregion

		#region Byte[] Writers

		public CompressedMatrix Write(byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			return Write(defaultTransform, buffer, ref bitposition, bcl);
		}

		public CompressedMatrix Write(Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			return new CompressedMatrix(
				this,
				posCrusher.Write(transform, buffer, ref bitposition, bcl),
				rotCrusher.Write(transform, buffer, ref bitposition, bcl),
				sclCrusher.Write(transform, buffer, ref bitposition, bcl),
				cached_pBits, cached_rBits, cached_sBits);
		}

		#endregion

		#region Byte[] Readers

		public Matrix ReadAndDecompress(byte[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		// Skips intermediate step of creating a compressedMatrx
		public Matrix ReadAndDecompress(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			return new Matrix(
				this,
				posCrusher.Decompress(posCrusher.Read(array, ref bitposition, bcl)),
				rotCrusher.Decompress(rotCrusher.Read(array, ref bitposition, bcl)),
				sclCrusher.Decompress(sclCrusher.Read(array, ref bitposition, bcl))
				);
		}


		// UNTESTED
		public CompressedMatrix Read(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			CompressedElement cpos = posCrusher.Read(array, ref bitposition, bcl);
			CompressedElement crot = rotCrusher.Read(array, ref bitposition, bcl);
			CompressedElement cscl = sclCrusher.Read(array, ref bitposition, bcl);

			return new CompressedMatrix(
				this, cpos, crot, cscl, cached_pBits, cached_rBits, cached_sBits);
		}

		#endregion

		#region ULong Buffer Writers

		public CompressedMatrix Write(Transform transform, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			return new CompressedMatrix(
				this,
				posCrusher.Write(transform, ref buffer, ref bitposition, bcl),
				rotCrusher.Write(transform, ref buffer, ref bitposition, bcl),
				sclCrusher.Write(transform, ref buffer, ref bitposition, bcl),
				cached_pBits, cached_rBits, cached_sBits);
		}

		public CompressedMatrix Write(Transform transform, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			CompressedMatrix cm = Compress(transform);
			bitstream.Write(cm);
			return cm;
		}

		#endregion

		#region Read and Deompress

		public Matrix ReadAndDecompress(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(buffer, ref bitposition, bcl);
		}
		public Matrix ReadAndDecompress(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			CompressedMatrix cMatrix = Read(buffer, ref bitposition, bcl);
			return Decompress(cMatrix);
		}
		public Matrix ReadAndDecompress(ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			CompressedMatrix cm = Read(ref bitstream, bcl);
			return Decompress(cm);
		}
		#endregion

		#region ULong Buffer Readers

		public CompressedMatrix Read(ulong fragment0, ulong fragment1 = 0, ulong fragment2 = 0, ulong fragment3 = 0, uint fragment4 = 0, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Bitstream buffer = new Bitstream(fragment0, fragment1, fragment2, fragment3, fragment4);
			return Read(ref buffer, bcl);
		}

		public CompressedMatrix Read(ref Bitstream buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			CompressedElement cpos = posCrusher.Read(ref buffer, bcl);
			CompressedElement crot = rotCrusher.Read(ref buffer, bcl);
			CompressedElement cscl = sclCrusher.Read(ref buffer, bcl);

			return new CompressedMatrix(
				this, cpos, crot, cscl, cached_pBits, cached_rBits, cached_sBits);
		}

		public CompressedMatrix Read(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Read(buffer, ref bitposition, bcl);
		}
		public CompressedMatrix Read(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			CompressedElement cpos = posCrusher.Read(buffer, ref bitposition, bcl);
			CompressedElement crot = rotCrusher.Read(buffer, ref bitposition, bcl);
			CompressedElement cscl = sclCrusher.Read(buffer, ref bitposition, bcl);
			return new CompressedMatrix(
				this, cpos, crot, cscl, cached_pBits, cached_rBits, cached_sBits);
		}

		#endregion

		#region Compress

		/// <summary>
		/// Compress the transform of the default gameobject. (Only avavilable if this crusher is serialized in the editor).
		/// </summary>
		/// <returns></returns>
		public CompressedMatrix Compress()
		{
			DebugX.LogError(transformMissingError, !defaultTransform, true);
			return Compress(defaultTransform);
		}
		public CompressedMatrix Compress(Matrix matrix)
		{
			if (!cached)
				CacheValues();

			CompressedElement cpos = posCrusher.Compress(matrix.position);
			CompressedElement crot = rotCrusher.Compress(matrix.rotation);
			CompressedElement cscl = sclCrusher.Compress(matrix.scale);

			return new CompressedMatrix(
				this, cpos, crot, cscl, cached_pBits, cached_rBits, cached_sBits);
		}

		public CompressedMatrix Compress(Transform transform)
		{
			if (!cached)
				CacheValues();

			CompressedElement cpos = (cached_pBits > 0) ? posCrusher.Compress(transform) : CompressedElement.Empty;
			CompressedElement crot = (cached_rBits > 0) ? rotCrusher.Compress(transform) : CompressedElement.Empty;
			CompressedElement cscl = (cached_sBits > 0) ? sclCrusher.Compress(transform) : CompressedElement.Empty;

			//Debug.Log("<b>TC compress </b> \n" + cpos + "\n" + crot + "\n" + cscl);
			CompressedMatrix cm = new CompressedMatrix(
				this, cpos, crot, cscl, cached_pBits, cached_rBits, cached_sBits);

			return cm;
		}

		#endregion

		#region Decompress

		//public Matrix Decompress(CompositeBuffer buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		//{
		//	CompressedMatrix compMatrix = Read(buffer, bcl);
		//	return Decompress(compMatrix);
		//}

		public Matrix Decompress(ulong compressed, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			CompressedMatrix compMatrix = Read(compressed, bcl);
			return Decompress(compMatrix);
		}

		public Matrix Decompress(CompressedMatrix compMatrix)
		{
			if (!cached)
				CacheValues();

			return new Matrix(
				this,
				posCrusher.Decompress(compMatrix.cPos),
				rotCrusher.Decompress(compMatrix.cRot),
				sclCrusher.Decompress(compMatrix.cScl)
				);
		}

		#endregion

		#region Apply

		const string transformMissingError = "The 'defaultTransform' is null and has not be set in the inspector. " +
				"For non-editor usages of TransformCrusher you need to pass the target transform to this method.";

		public void Apply(ulong cvalue)
		{
			DebugX.LogError(transformMissingError, !defaultTransform, true);
			Apply(defaultTransform, cvalue);
		}

		public void Apply(Transform t, ulong cvalue)
		{
			Matrix matrix = Decompress(cvalue);
			Apply(t, matrix);
		}

		public void Apply(ulong u0, ulong u1, ulong u2, ulong u3, uint u4)
		{
			DebugX.LogError(transformMissingError, !defaultTransform, true);
			Apply(defaultTransform, u0, u1, u2, u3, u4);
		}

		public void Apply(Transform t, ulong u0, ulong u1, ulong u2, ulong u3, uint u4)
		{
			CompressedMatrix cmatrix = Read(u0, u1, u2, u3, u4);
			Apply(t, cmatrix);
		}

		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(CompressedMatrix cmatrix)
		{
			DebugX.LogError(transformMissingError, !defaultTransform, true);
			Apply(defaultTransform, cmatrix);
		}

		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(Transform t, CompressedMatrix cmatrix)
		{
			if (cached_pBits > 0)
				posCrusher.Apply(t, cmatrix.cPos);
			if (cached_rBits > 0)
				rotCrusher.Apply(t, cmatrix.cRot);
			if (cached_sBits > 0)
				sclCrusher.Apply(t, cmatrix.cScl);
		}


		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(Matrix matrix)
		{
			DebugX.LogError(transformMissingError, !defaultTransform, true);
			Apply(defaultTransform, matrix);
		}

		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(Transform transform, Matrix matrix)
		{
			if (cached_pBits > 0)
				posCrusher.Apply(transform, matrix.position);

			if (cached_rBits > 0)
			{
				//if (matrix.rotationType == RotationType.Quaternion)
				rotCrusher.Apply(transform, matrix.rotation);
				//else
				//	rotCrusher.Apply(transform, matrix.eulers);
			}

			if (cached_sBits > 0)
				sclCrusher.Apply(transform, matrix.scale);
		}

		#endregion

		/// <summary>
		/// Get the total number of bits this Transform is set to write.
		/// </summary>
		public int TallyBits(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			return posCrusher.TallyBits(bcl) + rotCrusher.TallyBits(bcl) + sclCrusher.TallyBits(bcl);
		}

	}

}
