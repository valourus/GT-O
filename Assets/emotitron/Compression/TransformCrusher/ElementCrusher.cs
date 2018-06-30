//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{

	[System.Serializable]
	public class ElementCrusher
	{
		public enum UniformAxes { NonUniform = 0, XY = 3, XZ = 5, YZ = 6, XYZ = 7 }

		[SerializeField] private TRSType _trsType;
		public TRSType TRSType
		{
			get { return _trsType; }
			set
			{
				_trsType = value;
				xcrusher.TRSType = value;
				ycrusher.TRSType = value;
				zcrusher.TRSType = value;
			}
		}

		[SerializeField] public Transform defaultTransform;

		[SerializeField] public UniformAxes uniformAxes;

		[SerializeField] public FloatCrusher xcrusher;
		[SerializeField] public FloatCrusher ycrusher;
		[SerializeField] public FloatCrusher zcrusher;
		[SerializeField] public FloatCrusher ucrusher;
		[SerializeField] public QuatCrusher qcrusher;
		[SerializeField] public bool local;

		[SerializeField] public bool enableTRSTypeSelector;

		#region Cached values

		// cache values
		[System.NonSerialized]
		private bool cached;
		private bool cache_xEnabled, cache_yEnabled, cache_zEnabled, cache_uEnabled, cache_qEnabled;
		private bool cache_isUniformScale;
		private int[] cache_xBits, cache_yBits, cache_zBits, cache_uBits, cache_TotalBits;
		private int cache_qBits;
		private bool cache_mustCorrectRotationX;

		public void CacheValues()
		{
			cache_xEnabled = xcrusher.Enabled;
			cache_yEnabled = ycrusher.Enabled;
			cache_zEnabled = zcrusher.Enabled;
			cache_uEnabled = ucrusher.Enabled;
			cache_qEnabled = qcrusher.enabled && qcrusher.Bits > 0;
			cache_xBits = new int[4];
			cache_yBits = new int[4];
			cache_zBits = new int[4];
			cache_uBits = new int[4];
			cache_qBits = qcrusher.Bits;
			cache_TotalBits = new int[4];

			for (int i = 0; i < 4; ++i)
			{
				cache_xBits[i] = xcrusher.GetBitsAtCullLevel((BitCullingLevel)i);
				cache_yBits[i] = ycrusher.GetBitsAtCullLevel((BitCullingLevel)i);
				cache_zBits[i] = zcrusher.GetBitsAtCullLevel((BitCullingLevel)i);
				cache_uBits[i] = ucrusher.GetBitsAtCullLevel((BitCullingLevel)i);
				cache_TotalBits[i] = TallyBits((BitCullingLevel)i);
			}

			cache_mustCorrectRotationX = _trsType == TRSType.Euler && xcrusher.UseHalfRangeX;
			cache_isUniformScale = _trsType == TRSType.Scale && uniformAxes != UniformAxes.NonUniform;

			cached = true;
		}

		/// <summary>
		/// Property that returns if this element crusher is effectively enabled (has any enabled float/quat crushers using bits > 0)
		/// </summary>
		public bool Enabled
		{
			get
			{
				if (TRSType == TRSType.Quaternion)
					return (qcrusher.enabled && qcrusher.Bits > 0);

				else if (TRSType == TRSType.Scale && uniformAxes != 0)
					return ucrusher.Enabled;

				return xcrusher.Enabled | ycrusher.Enabled | zcrusher.Enabled;
			}
		}

		#endregion

		public FloatCrusher this[int axis]
		{
			get
			{
				switch (axis)
				{
					case 0:
						return xcrusher;
					case 1:
						return ycrusher;
					case 2:
						return zcrusher;

					default:
						Debug.Log("AXIS " + axis + " should not be calling happening");
						return null;
				}
			}
		}

		#region Constructors

		// Constructor
		public ElementCrusher(bool enableTRSTypeSelector = true)
		{
			this._trsType = TRSType.Generic;
			Defaults(TRSType.Generic);

			this.enableTRSTypeSelector = enableTRSTypeSelector;
		}

		// Constructor
		public ElementCrusher(TRSType trsType, bool enableTRSTypeSelector = true)
		{
			this._trsType = trsType;
			Defaults(trsType);

			this.enableTRSTypeSelector = enableTRSTypeSelector;
		}

		private void Defaults(TRSType trs)
		{
			if (trs == TRSType.Quaternion || trs == TRSType.Euler)
			{
				xcrusher = new FloatCrusher(BitPresets.Bits10, -90f, 90f, Axis.X, TRSType.Euler, true);
				ycrusher = new FloatCrusher(BitPresets.Bits12, -180f, 180f, Axis.Y, TRSType.Euler, true);
				zcrusher = new FloatCrusher(BitPresets.Bits10, -180f, 180f, Axis.Z, TRSType.Euler, true);
				//ucrusher = new FloatCrusher(Axis.Uniform, TRSType.Scale, true);
				qcrusher = new QuatCrusher(true, false);
			}
			else if (trs == TRSType.Scale)
			{
				xcrusher = new FloatCrusher(BitPresets.Bits12, 0f, 2f, Axis.X, TRSType.Scale, true);
				ycrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Y, TRSType.Scale, true);
				zcrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Z, TRSType.Scale, true);
				ucrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Uniform, TRSType.Scale, true);
			}
			else
			{
				xcrusher = new FloatCrusher(BitPresets.Bits12, -20f, 20f, Axis.X, trs, true);
				ycrusher = new FloatCrusher(BitPresets.Bits10, -5f, 5f, Axis.Y, trs, true);
				zcrusher = new FloatCrusher(BitPresets.Bits10, -5f, 5f, Axis.Z, trs, true);
			}
		}

		#endregion

		#region Array Writers
		/// <summary>
		/// Automatically use the correct transform TRS element based on the TRSType and local settings of each Crusher.
		/// </summary>
		public CompressedElement Write(Transform trans, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
				case TRSType.Position:
					return Write((local) ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);

				case TRSType.Euler:
					return Write((local) ? trans.localEulerAngles : trans.eulerAngles, bytes, ref bitposition, bcl);

				case TRSType.Quaternion:
					return Write((local) ? trans.localRotation : trans.rotation, bytes, ref bitposition, bcl);

				case TRSType.Scale:
					return Write(trans.localScale, bytes, ref bitposition, bcl);

				default:
					DebugX.Log("You are sending a transform to be crushed, but the Element Type is Generic - did you want Position?");
					return Write((local) ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);
			}
		}

		public CompressedElement Write(CompressedElement ce, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cache_isUniformScale)
			{
				uint c = (uint)ucrusher.Write(ce.cUniform, bytes, ref bitposition, bcl);
				return new CompressedElement(this, c, cache_uBits[(int)bcl]);
			}

			else if (TRSType == TRSType.Quaternion)
			{
				ulong c = qcrusher.Write(ce.cQuat, bytes, ref bitposition);
				return new CompressedElement(this, c, cache_qBits);
			}

			//Debug.Log("CX write " + ce.cx);
			//else if (cache_mustCorrectRotationX)
			//	v3 = FloatCrusherUtilities.GetXCorrectedEuler(v3);
			//Debug.Log("Writing Scale " + ce.cx);
			return new CompressedElement(
				this,
				(uint)(cache_xEnabled ? (uint)xcrusher.Write(ce.cx, bytes, ref bitposition, bcl) : 0),
				(uint)(cache_yEnabled ? (uint)ycrusher.Write(ce.cy, bytes, ref bitposition, bcl) : 0),
				(uint)(cache_zEnabled ? (uint)zcrusher.Write(ce.cz, bytes, ref bitposition, bcl) : 0),
				cache_xBits[(int)bcl], cache_yBits[(int)bcl], cache_zBits[(int)bcl]);
		}

		public CompressedElement Write(Vector3 v3, byte[] bytes, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Write(v3, bytes, ref bitposition);
		}

		public CompressedElement Write(Vector3 v3, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cache_isUniformScale)
			{
				uint c = (uint)ucrusher.Write(uniformAxes == UniformAxes.YZ ? v3.y : v3.x, bytes, ref bitposition, bcl);
				return new CompressedElement(this, c, cache_uBits[(int)bcl]);
			}

			else if (TRSType == TRSType.Quaternion)
			{
				ulong c = qcrusher.Write(Quaternion.Euler(v3), bytes, ref bitposition);
				return new CompressedElement(this, c, cache_qBits);
			}

			else if (cache_mustCorrectRotationX)
				v3 = FloatCrusherUtilities.GetXCorrectedEuler(v3);

			return new CompressedElement(
				this,
				(uint)(cache_xEnabled ? (uint)xcrusher.Write(v3.x, bytes, ref bitposition, bcl) : 0),
				(uint)(cache_yEnabled ? (uint)ycrusher.Write(v3.y, bytes, ref bitposition, bcl) : 0),
				(uint)(cache_zEnabled ? (uint)zcrusher.Write(v3.z, bytes, ref bitposition, bcl) : 0),
				cache_xBits[(int)bcl], cache_yBits[(int)bcl], cache_zBits[(int)bcl]);
		}

		public CompressedElement Write(Quaternion quat, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			DebugX.LogError("You seem to be trying to compress a Quaternion with a crusher that is set up for " +
				System.Enum.GetName(typeof(TRSType), TRSType) + ".", TRSType != TRSType.Quaternion, true);

			return new CompressedElement(this, qcrusher.Write(quat, bytes, ref bitposition), cache_qBits);
		}

		#endregion

		#region Array Readers

		public CompressedElement Read(byte[] bytes, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Read(bytes, ref bitposition, bcl);
		}

		/// <summary>
		/// Reads out the commpressed value for this vector/quaternion from a buffer. Needs to be decompressed still to become vector3/quaterion.
		/// </summary>
		public CompressedElement Read(byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (TRSType == TRSType.Quaternion)
			{
				return new CompressedElement(this, bytes.ReadUInt64(cache_qBits, ref bitposition), cache_qBits);
			}

			else if (cache_isUniformScale)
			{
				return new CompressedElement(this, bytes.ReadUInt32(cache_uBits[(int)bcl], ref bitposition), cache_uBits[(int)bcl]);
			}

			int xbits = cache_xBits[(int)bcl];
			int ybits = cache_yBits[(int)bcl];
			int zbits = cache_zBits[(int)bcl];

			uint cx = cache_xEnabled ? bytes.ReadUInt32(xbits, ref bitposition) : 0;
			uint cy = cache_yEnabled ? bytes.ReadUInt32(ybits, ref bitposition) : 0;
			uint cz = cache_zEnabled ? bytes.ReadUInt32(zbits, ref bitposition) : 0;
			//Debug.Log("Read CX : " + cx);
			return new CompressedElement(this, cx, cy, cz, xbits, ybits, zbits);
		}

		#endregion

		#region ULong Buffer Writers

		/// <summary>
		/// Automatically use the correct transform element based on the TRSType for this Crusher.
		/// </summary>
		public CompressedElement Write(Transform trans, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
				case TRSType.Position:
					return Write((local) ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);

				case TRSType.Euler:
					return Write((local) ? trans.localEulerAngles : trans.eulerAngles, ref buffer, ref bitposition, bcl);

				case TRSType.Quaternion:
					return Write((local) ? trans.localRotation : trans.rotation, ref buffer, ref bitposition);

				case TRSType.Scale:
					return Write(trans.localScale, ref buffer, ref bitposition, bcl);

				default:
					DebugX.Log("You are sending a transform to be crushed, but the Element Type is Generic - did you want Position?");
					return Write((local) ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);
			}
		}

		public CompressedElement Write(Vector3 v3, ref ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Write(v3, ref buffer, ref bitposition, bcl);
		}

		public CompressedElement Write(Vector3 v3, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			uint cx = cache_xEnabled ? (uint)xcrusher.Write(v3.x, ref buffer, ref bitposition, bcl) : (uint)0;
			uint cy = cache_yEnabled ? (uint)ycrusher.Write(v3.y, ref buffer, ref bitposition, bcl) : (uint)0;
			uint cz = cache_zEnabled ? (uint)zcrusher.Write(v3.z, ref buffer, ref bitposition, bcl) : (uint)0;

			return new CompressedElement(this, cx, cy, cz, cache_xBits[(int)bcl], cache_yBits[(int)bcl], cache_zBits[(int)bcl]);
		}

		public CompressedElement Write(Quaternion quat, ref ulong buffer)
		{
			int bitposition = 0;
			return Write(quat, ref buffer, ref bitposition);
		}
		public CompressedElement Write(Quaternion quat, ref ulong buffer, ref int bitposition)
		{
			if (!cached)
				CacheValues();

			ulong cq = cache_qEnabled ? qcrusher.Write(quat, ref buffer, ref bitposition) : 0;

			return new CompressedElement(this, cq, cache_qBits);
		}

		public CompressedElement Write(CompressedElement compressed, ref ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Write(compressed, ref buffer, ref bitposition, bcl);
		}
		public CompressedElement Write(CompressedElement compressed, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			compressed.bitstream.Read(cache_TotalBits[(int)bcl]);
			return compressed;
		}

		#endregion

		#region ULong Buffer Readers

		public CompressedElement Read(ref Bitstream buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (TRSType == TRSType.Quaternion)
			{
				return new CompressedElement(this, buffer.Read(cache_qBits), cache_qBits);
			}

			else if (cache_isUniformScale)
			{
				return new CompressedElement(this, buffer.Read(cache_uBits[(int)bcl]), cache_uBits[(int)bcl]);
			}

			int xbits = cache_xBits[(int)bcl];
			int ybits = cache_yBits[(int)bcl];
			int zbits = cache_zBits[(int)bcl];

			uint cx = cache_xEnabled ? (uint)buffer.Read(xbits) : 0;
			uint cy = cache_yEnabled ? (uint)buffer.Read(ybits) : 0;
			uint cz = cache_zEnabled ? (uint)buffer.Read(zbits) : 0;

			return new CompressedElement(this, cx, cy, cz, xbits, ybits, zbits);
		}

		public CompressedElement Read(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Read(buffer, ref bitposition, bcl);
		}

		public CompressedElement Read(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (TRSType == TRSType.Quaternion)
			{
				return new CompressedElement(this, buffer.Extract(cache_qBits, ref bitposition), cache_qBits);
			}

			else if (cache_isUniformScale)
			{
				return new CompressedElement(this, buffer.Extract(cache_uBits[(int)bcl], ref bitposition), cache_uBits[(int)bcl]);
			}

			int xbits = cache_xBits[(int)bcl];
			int ybits = cache_yBits[(int)bcl];
			int zbits = cache_zBits[(int)bcl];

			uint cx = (uint)buffer.Extract(xbits, ref bitposition);
			uint cy = (uint)buffer.Extract(ybits, ref bitposition);
			uint cz = (uint)buffer.Extract(zbits, ref bitposition);

			return new CompressedElement(this, cx, cy, cz, xbits, ybits, zbits);
		}

		#endregion

		#region Compressors

		public CompressedElement Compress(Transform trans)
		{
			switch (TRSType)
			{
				case TRSType.Position:
					return Compress((local) ? trans.localPosition : trans.position);

				case TRSType.Euler:
					return Compress((local) ? trans.localEulerAngles : trans.eulerAngles);

				case TRSType.Quaternion:
					return Compress((local) ? trans.localRotation : trans.rotation);

				case TRSType.Scale:
					return Compress((local) ? trans.localScale : trans.lossyScale);

				default:
					DebugX.LogWarning("You are sending a transform to be crushed, but the Element Type is Generic?  Assuming position - change the crusher from Generic to the correct TRS.", true, true);
					return Compress((local) ? trans.localPosition : trans.position);
			}
		}

		public CompressedElement Compress(Element e)
		{
			if (TRSType == TRSType.Euler)
				return Compress(e.quat);
			else
				return Compress(e.v);
		}

		public CompressedElement Compress(Vector3 v)
		{
			if (!cached)
				CacheValues();

			if (_trsType == TRSType.Scale && uniformAxes != UniformAxes.NonUniform)
			{
				//Debug.Log(this + " Compress <b>UNIFORM</b>");
				ulong cu = (cache_uEnabled) ? ucrusher.Compress((uniformAxes == UniformAxes.YZ) ? v.y : v.x) : (ulong)0;
				return new CompressedElement(this, cu, cache_uBits[0]);

			}
			else if (_trsType == TRSType.Quaternion)
			{
				Debug.Log("We shouldn't be seeing this. Quats should not be getting compressed from Eulers!");
				return (cache_uEnabled) ? new CompressedElement(this, qcrusher.Compress(Quaternion.Euler(v)), cache_qBits) : new CompressedElement(this, (ulong)0, cache_qBits);
			}
			else
			{
				FloatCrusherUtilities.CheckBitCount(xcrusher.Bits + ycrusher.Bits + zcrusher.Bits, 64);

				uint cx = (cache_xEnabled ? (uint)xcrusher.Compress(v.x) : 0);
				uint cy = (cache_yEnabled ? (uint)ycrusher.Compress(v.y) : 0);
				uint cz = (cache_zEnabled ? (uint)zcrusher.Compress(v.z) : 0);

				return new CompressedElement(this, cx, cy, cz, cache_xBits[0], cache_yBits[0], cache_zBits[0]);
			}
		}

		/// <summary>
		/// Compress and bitpack the enabled vectors into a generic unsigned int.
		/// </summary>
		public CompressedElement Compress(Quaternion quat)
		{
			if (!cached)
				CacheValues();

			DebugX.LogError("You seem to be trying to compress a Quaternion with a crusher that is set up for " +
				System.Enum.GetName(typeof(TRSType), TRSType) + ".", TRSType != TRSType.Quaternion, true);

			return cache_xEnabled ? new CompressedElement(this, qcrusher.Compress(quat), cache_qBits) : CompressedElement.Empty;
		}

		#endregion

		#region Decompress

		/// <summary>
		/// Decode (decompresss) and restore an element that was compressed by this crusher.
		/// </summary>
		public Element Decompress(CompressedElement compressed)
		{
			if (!cached)
				CacheValues();

			if (_trsType == TRSType.Scale && uniformAxes != UniformAxes.NonUniform)
			{
				float val = ucrusher.Decompress((uint)compressed.cx);
				return new Vector3(val, val, val);
			}
			else if (_trsType == TRSType.Quaternion)
			{
				//Debug.Log("We should not see this! Quats should be getting called to DecompressToQuat");
				return qcrusher.Decompress(compressed.cQuat);
			}
			else
			{
				// Issue log error for trying to write more than 64 bits to the ulong buffer
				FloatCrusherUtilities.CheckBitCount(cache_TotalBits[0], 64);

				return new Vector3(
					cache_xEnabled ? (xcrusher.Decompress((uint)compressed.cx)) : 0,
					cache_yEnabled ? (ycrusher.Decompress((uint)compressed.cy)) : 0,
					cache_zEnabled ? (zcrusher.Decompress((uint)compressed.cz)) : 0
					);
			}
		}

		public Element Decompress(ulong cval)
		{
			if (!cached)
				CacheValues();

			if (_trsType == TRSType.Scale && uniformAxes != UniformAxes.NonUniform)
			{
				float val = ucrusher.Decompress((uint)cval);
				return new Vector3(val, val, val);
			}
			else if (_trsType == TRSType.Quaternion)
			{
				//Debug.Log("We should not see this! Quats should be getting called to DecompressToQuat");
				return qcrusher.Decompress(cval);
			}
			else
			{
				// Issue log error for trying to write more than 64 bits to the ulong buffer
				FloatCrusherUtilities.CheckBitCount(cache_TotalBits[0], 64);

				int bitposition = 0;
				return new Vector3(
					cache_xEnabled ? (xcrusher.ReadAndDecompress(ref cval, ref bitposition)) : 0,
					cache_yEnabled ? (ycrusher.ReadAndDecompress(ref cval, ref bitposition)) : 0,
					cache_zEnabled ? (zcrusher.ReadAndDecompress(ref cval, ref bitposition)) : 0
					);
			}
		}

		//public Quaternion DecompressToQuat(CompressedElement compressed)
		//{
		//	if (!cached)
		//		CacheValues();

		//	DebugX.LogError("You seem to be trying to decompress a Quaternion from a crusher that is set up for " +
		//		System.Enum.GetName(typeof(TRSType), TRSType) + ". This likely won't end well.", TRSType != TRSType.Quaternion, true);

		//	Quaternion quat = qcrusher.Decompress(compressed.cQuat);
		//	return quat;
		//}

		#endregion

		#region Apply

		/// <summary>
		/// Applies only the enabled axes to the transform, leaving the disabled axes untouched.
		/// </summary>
		public void Apply(Transform trans, CompressedElement cElement)
		{
			Apply(trans, Decompress(cElement));
		}

		/// <summary>
		/// Applies only the enabled axes to the transform, leaving the disabled axes untouched.
		/// </summary>
		public void Apply(Transform trans, Element e)
		{
			if (!cached)
				CacheValues();

			switch (_trsType)
			{
				case TRSType.Quaternion:

					if (cache_qEnabled)
					{
						if (local)
							trans.localRotation = e.quat;
						else
							trans.rotation = e.quat;
					}

					return;

				case TRSType.Position:

					if (local)
					{
						trans.localPosition = new Vector3(
							cache_xEnabled ? e.v.x : trans.localPosition.x,
							cache_yEnabled ? e.v.y : trans.localPosition.y,
							cache_zEnabled ? e.v.z : trans.localPosition.z
							);
					}
					else
					{
						trans.position = new Vector3(
							cache_xEnabled ? e.v.x : trans.position.x,
							cache_yEnabled ? e.v.y : trans.position.y,
							cache_zEnabled ? e.v.z : trans.position.z
							);
					}
					return;

				case TRSType.Euler:

					if (local)
					{
						trans.localEulerAngles = new Vector3(
							cache_xEnabled ? e.v.x : trans.localEulerAngles.x,
							cache_yEnabled ? e.v.y : trans.localEulerAngles.y,
							cache_zEnabled ? e.v.z : trans.localEulerAngles.z
							);
					}
					else
					{
						trans.eulerAngles = new Vector3(
							cache_xEnabled ? e.v.x : trans.eulerAngles.x,
							cache_yEnabled ? e.v.y : trans.eulerAngles.y,
							cache_zEnabled ? e.v.z : trans.eulerAngles.z
							);
					}
					return;

				default:
					if (local)
					{
						if (uniformAxes == UniformAxes.NonUniform)
						{
							trans.localScale = new Vector3(

							cache_xEnabled ? e.v.x : trans.localScale.x,
							cache_yEnabled ? e.v.y : trans.localScale.y,
							cache_zEnabled ? e.v.z : trans.localScale.z
							);
						}

						// Is a uniform scale
						else
						{
							float uniform = ((int)uniformAxes & 1) != 0 ? e.v.x : e.v.y;
							trans.localScale = new Vector3
								(
								((int)uniformAxes & 1) != 0 ? uniform : trans.localScale.x,
								((int)uniformAxes & 2) != 0 ? uniform : trans.localScale.y,
								((int)uniformAxes & 4) != 0 ? uniform : trans.localScale.z
								);
						}
					}
					else
					{
						if (uniformAxes == UniformAxes.NonUniform)
						{
							trans.localScale = new Vector3(

							cache_xEnabled ? e.v.x : trans.lossyScale.x,
							cache_yEnabled ? e.v.y : trans.lossyScale.y,
							cache_zEnabled ? e.v.z : trans.lossyScale.z
							);
						}

						// Is a uniform scale
						else
						{
							float uniform = ((int)uniformAxes & 1) != 0 ? e.v.x : e.v.y;
							trans.localScale = new Vector3
								(
								((int)uniformAxes & 1) != 0 ? uniform : trans.lossyScale.x,
								((int)uniformAxes & 2) != 0 ? uniform : trans.lossyScale.y,
								((int)uniformAxes & 4) != 0 ? uniform : trans.lossyScale.z
								);
						}
					}
					return;
			}
		}

		//public void Apply(Transform trans, Quaternion q)
		//{
		//	if (!cached)
		//		CacheValues();

		//	if (_trsType == TRSType.Quaternion)
		//	{
		//		if (cache_qEnabled)
		//			if (local)
		//				trans.rotation = q;
		//			else
		//				trans.localRotation = q;
		//		return;
		//	}

		//	DebugX.LogError("You seem to be trying to apply a Quaternion to " + System.Enum.GetName(typeof(TRSType), _trsType) + ".", true, true);
		//}

		#endregion

		/// <summary>
		/// Get the total number of bits this Vector3 is set to write.
		/// </summary>
		public int TallyBits(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (_trsType == TRSType.Scale && uniformAxes != UniformAxes.NonUniform)
			{
				return ucrusher.enabled ? ucrusher.GetBitsAtCullLevel(bcl) : 0;
			}
			else if (_trsType == TRSType.Quaternion)
			{
				return qcrusher.enabled ? qcrusher.Bits : 0;
			}
			else
			{
				return
					(xcrusher.GetBitsAtCullLevel(bcl)) +
					(ycrusher.GetBitsAtCullLevel(bcl)) +
					(zcrusher.GetBitsAtCullLevel(bcl));
			}
		}

		public override string ToString()
		{
			return "ElementCrusher [" + _trsType + "] ";
		}
	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(ElementCrusher))]
	[CanEditMultipleObjects]
	[AddComponentMenu("Crusher/Element Crusher")]

	public class ElementCrusherDrawer : CrusherDrawer
	{
		public const float TOP_PAD = 4f;
		public const float BTM_PAD = 6f;
		private const float TITL_HGHT = 18f;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(r, label, property);

			base.OnGUI(r, property, label);

			// Hacky way to get the real object
			ElementCrusher target = (ElementCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("xcrusher"));

			SerializedProperty uniformAxes = property.FindPropertyRelative("uniformAxes");
			SerializedProperty x = property.FindPropertyRelative("xcrusher");
			SerializedProperty y = property.FindPropertyRelative("ycrusher");
			SerializedProperty z = property.FindPropertyRelative("zcrusher");
			SerializedProperty u = property.FindPropertyRelative("ucrusher");
			SerializedProperty q = property.FindPropertyRelative("qcrusher");


			float xh = EditorGUI.GetPropertyHeight(x);
			float yh = EditorGUI.GetPropertyHeight(y);
			float zh = EditorGUI.GetPropertyHeight(z);
			float wh = EditorGUI.GetPropertyHeight(u);
			float qh = EditorGUI.GetPropertyHeight(q);

			bool isQuatCrush = target.TRSType == TRSType.Quaternion;
			bool isUniformScale = target.TRSType == TRSType.Scale && target.uniformAxes != 0;
			float boxesheight =
				isQuatCrush ? qh :
				isUniformScale ? wh :
				xh + yh + zh;


			float currentline = r.yMin + TOP_PAD;

			GUI.Box(new Rect(r.xMin - 1, currentline - 1, r.width + 2, TITL_HGHT + boxesheight + 2), GUIContent.none, (GUIStyle)"flow shader node 0 on");
			SolidTextures.DrawTexture(new Rect(r.xMin - 1, currentline - 1, r.width + 2, 16 + 2), SolidTextures.lowcontrast2D);
			SolidTextures.DrawTexture(new Rect(r.xMin, currentline, r.width, 16 + SPACING), SolidTextures.contrastgray2D);

			if (target.enableTRSTypeSelector)
				target.TRSType = (TRSType)EditorGUI.EnumPopup(new Rect(r.xMin, currentline + 1, 78, LINEHEIGHT), target.TRSType);
			else if (target.TRSType == TRSType.Quaternion || target.TRSType == TRSType.Euler)
			{
				target.TRSType = (TRSType)EditorGUI.EnumPopup(new Rect(r.xMin, currentline + 1, 78, LINEHEIGHT), (RotationType)target.TRSType);
			}
			else
			{
				GUIContent title = new GUIContent(System.Enum.GetName(typeof(TRSType), target.TRSType)); // + " Crshr");
				EditorGUI.LabelField(new Rect(paddedleft, currentline, r.width, LINEHEIGHT), title, (GUIStyle)"MiniBoldLabel");
			}

			int localtoggleleft = 80;
			target.local = GUI.Toggle(new Rect(paddedright - localtoggleleft, currentline + 1, 20, LINEHEIGHT), target.local, GUIContent.none, (GUIStyle)"OL Toggle");
			EditorGUI.LabelField(new Rect(paddedright - localtoggleleft + 14, currentline, 80, LINEHEIGHT), new GUIContent("Lcl"), (GUIStyle)"MiniLabel");

			EditorGUI.LabelField(new Rect(paddedleft, currentline, paddedwidth, 16), target.TallyBits() + " Bits", FloatCrusherDrawer.miniLabelRight);

			// Scale Uniform Enum
			if (target.TRSType == TRSType.Scale)
			{
				target.uniformAxes =
					(ElementCrusher.UniformAxes)EditorGUI.EnumPopup(new Rect(paddedright - 78 - 84, currentline + 1, 78, 16), GUIContent.none, target.uniformAxes);
			}

			currentline += TITL_HGHT;

			if (target.TRSType == TRSType.Scale && uniformAxes.enumValueIndex != 0)
			{
				//SolidTextures.DrawTexture(new Rect(r.xMin - 3, currentline - 1, r.width + 6, wh + 2), SolidTextures.black2D);
				EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, wh), u);
			}
			else if (target.TRSType == TRSType.Quaternion)
			{
				EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, wh), q);
			}
			else
			{
				//SolidTextures.DrawTexture(new Rect(r.xMin - 3, currentline - 1, r.width + 6, xh + yh +zh + 2), SolidTextures.black2D);
				EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, xh), x);
				currentline += xh;
				EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, yh), y);
				currentline += yh;
				EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, zh), z);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty trsType = property.FindPropertyRelative("_trsType");
			SerializedProperty uniformAxes = property.FindPropertyRelative("uniformAxes");
			SerializedProperty x = property.FindPropertyRelative("xcrusher");
			SerializedProperty y = property.FindPropertyRelative("ycrusher");
			SerializedProperty z = property.FindPropertyRelative("zcrusher");
			SerializedProperty u = property.FindPropertyRelative("ucrusher");
			SerializedProperty q = property.FindPropertyRelative("qcrusher");

			if (trsType.enumValueIndex == (int)TRSType.Scale && uniformAxes.enumValueIndex != 0)
			{
				float wh = EditorGUI.GetPropertyHeight(u);
				return TOP_PAD + TITL_HGHT + wh + BTM_PAD;
			}
			else if (trsType.enumValueIndex == (int)TRSType.Quaternion)
			{
				float qh = EditorGUI.GetPropertyHeight(q);
				return TOP_PAD + TITL_HGHT + qh + BTM_PAD;
			}
			else
			{
				float xh = EditorGUI.GetPropertyHeight(x);
				float yh = EditorGUI.GetPropertyHeight(y);
				float zh = EditorGUI.GetPropertyHeight(z);

				return TOP_PAD + TITL_HGHT + xh + yh + zh + BTM_PAD;
			}
		}
	}
#endif
}
