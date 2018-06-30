//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

namespace emotitron.Compression
{
	/// <summary>
	/// This struct contains CompressedElement structs compressed Position, Rotation and Scale, 
	/// as well as the CompositeBuffer which contains the raw compressed fragments that can sent over the network.
	/// </summary>
	public struct CompressedMatrix
	{
		public TransformCrusher crusher;
		public Bitstream bitstream;
		public CompressedElement cPos;
		public CompressedElement cRot;
		public CompressedElement cScl;

		// Constructor
		public CompressedMatrix(TransformCrusher crusher, CompressedElement cPos, CompressedElement cRot, CompressedElement cScl, int pBits, int rBits, int sBits) : this()
		{
			this.crusher = crusher;
			this.cPos = cPos;
			this.cRot = cRot;
			this.cScl = cScl;
			
			this.bitstream = new Bitstream(cPos.bitstream, pBits, cRot.bitstream, rBits, cScl.bitstream, sBits);
		}

		public ulong this[int i]
		{
			get
			{
				return bitstream[i];
			}
		}

		public Matrix Decompress()
		{
			return crusher.Decompress(this);
		}

		public void Apply()
		{
			crusher.Apply(this);
		}
		public void Apply(Transform t)
		{
			crusher.Apply(t, this);
		}

		public static implicit operator Bitstream(CompressedMatrix cm)
		{
			return cm.bitstream;
		}

		public static implicit operator ulong(CompressedMatrix cm)
		{
			return cm.bitstream[0];
		}

		public override string ToString()
		{
			return "cpos: " + cPos + "\ncrot: " + cRot + "\nsrot " + cScl + "\n compressed matric composite: " + bitstream;
		}
	}
}


