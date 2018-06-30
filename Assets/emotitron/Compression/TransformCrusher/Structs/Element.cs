//Copyright 2018, Davin Carten, All rights reserved

using System.Runtime.InteropServices;
using UnityEngine;

namespace emotitron.Compression
{

	/// <summary>
	/// A struct that allows Quaternion and Vector types to be treated as the same.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct Element
	{
		public enum VectorType { Vector3 = 1, Quaternion = 2 }

		[FieldOffset(0)]
		public VectorType vectorType;

		[FieldOffset(4)]
		public Vector3 v;

		[FieldOffset(4)]
		public Quaternion quat;

		public Element(Vector3 v) : this()
		{
			vectorType = VectorType.Vector3;
			this.v = v;
		}

		public Element(Quaternion quat) : this()
		{
			vectorType = VectorType.Quaternion;
			this.quat = quat;
		}

		public static implicit operator Quaternion(Element e) { return e.quat; }
		public static implicit operator Vector3(Element e) { return e.v; }
		public static implicit operator Element(Quaternion q) { return new Element(q); }
		public static implicit operator Element(Vector3 v) { return new Element(v); }
	}
}
