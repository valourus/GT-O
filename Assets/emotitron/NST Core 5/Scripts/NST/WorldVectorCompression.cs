//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Compression;

namespace emotitron.NST
{
	/// <summary>
	/// Compress vector3 to the scale of the map.
	/// </summary>
	public static class WorldVectorCompression
	{
		// constructor (not entirely sure if this is needed)
		static WorldVectorCompression()
		{
			Bounds bounds = NSTMapBounds.CombinedWorldBounds;

			axisRanges = new FloatCrusher[3];

			for (int axis = 0; axis < 3; axis++)
				axisRanges[axis] = new FloatCrusher(bounds.min[axis], bounds.max[axis], WorldCompressionSettings.Single.minPosResolution, (Axis)axis, TRSType.Position);

			SetWorldRanges(bounds, true);
		}

		public static FloatCrusher[] axisRanges = new FloatCrusher[3];

		/// <summary>
		/// Change the axisranges for the world bounds to a new bounds.
		/// </summary>
		public static void SetWorldRanges(Bounds bounds, bool silent = false)
		{
			//NSTSettings nstSettings = NSTSettings.EnsureExistsInScene(NSTSettings.DEFAULT_GO_NAME);
			DebugX.LogWarning(!DebugX.logWarnings ? null : 
				("<b>Scene is missing map bounds</b>, defaulting to a map size of Center:" + NSTMapBounds.CombinedWorldBounds.center + " Size:" + NSTMapBounds.CombinedWorldBounds.size +
				". Be sure to add NSTMapBounds components to your scene to define its bounds, or be sure the default bounds in NSTSettings are what you want."),
				(!silent && Application.isPlaying && NSTMapBounds.ActiveBoundsObjCount == 0 && Time.time > 1)
				);

			DebugX.LogWarning(!DebugX.logWarnings ? null :
				("<b>Scene map bounds are very small</b>. Current world bounds are " + bounds.center + " Size:" + bounds.size + ", is this intentional?" +
				"If not check that your NSTMapBounds fully encompass your world as intended, or if using the Default bounds set in NSTSettings, that it is correct."),
				(!silent && Application.isPlaying && NSTMapBounds.ActiveBoundsObjCount > 0 && (bounds.size.x <= 1 || bounds.size.y <= 1 || bounds.size.z <= 1))
				);

			for (int axis = 0; axis < 3; axis++)
			{
				axisRanges[axis].SetRange((float)bounds.min[axis], (float)bounds.max[axis], (uint)WorldCompressionSettings.Single.minPosResolution);
			}

			DebugX.Log( 
				("Notice: Change in Map Bounds (Due to an NSTBounds being added or removed from the scene) to \n" +
				"Center:"+ bounds.center +" Size:" + bounds.size + ". Be sure this map change is happening to all networked clients or things will break badly. \n" +
				"Position keyframes will use x:" + axisRanges[0].Bits + " bits, y:" + axisRanges[1].Bits + "bits, and z:" + axisRanges[2].Bits + 
				" bits at the current minimum resolutons settings (in NST Settings)."), !silent && Application.isPlaying, true);
		}

		public static void WriteWorldCompPosToBitstream(this CompressedElement compressedpos, ref UdpBitStream bitstream, IncludedAxes ia, BitCullingLevel bcl)
		{
			for (int axis = 0; axis < 3; axis++)
			{
				if (ia.IsXYZ(axis)) bitstream.WriteUInt(compressedpos[axis], axisRanges[axis].GetBitsAtCullLevel(bcl));
			}
		}

		public static void WriteCompressedAxisToBitstream(this uint val, int axis, ref UdpBitStream bitstream, BitCullingLevel bcl)
		{
			bitstream.WriteUInt(val, axisRanges[axis].GetBitsAtCullLevel(bcl));
		}

		public static uint WriteAxisToBitstream(this float val, int axis, ref UdpBitStream bitstream, BitCullingLevel bcl)
		{
			uint compressedAxis = val.CompressAxis(axis);
			bitstream.WriteUInt(val.CompressAxis(axis), axisRanges[axis].GetBitsAtCullLevel(bcl));
			return compressedAxis;
		}

		public static uint CompressAxis(this float val, int axis)
		{
			return axisRanges[axis].Compress(val);
		}

		public static CompressedElement CompressToWorld(this Vector3 pos)
		{
			CompressedValue cx = axisRanges[0].Compress(pos.x);
			CompressedValue cy = axisRanges[1].Compress(pos.y);
			CompressedValue cz = axisRanges[2].Compress(pos.z);
			CompressedElement ce = new CompressedElement(null, cx, cy, cz);

			return ce;
		}

		public static int ReadCompressedAxisFromBitstream(ref UdpBitStream bitstream, int axis, BitCullingLevel bcl)
		{
			return (bitstream.ReadInt(axisRanges[axis].GetBitsAtCullLevel(bcl)));
		}

		public static float ReadAxisFromBitstream(ref UdpBitStream bitstream, int axis, BitCullingLevel bcl)
		{
			uint compressedAxis = bitstream.ReadUInt(axisRanges[axis].GetBitsAtCullLevel(bcl));

			return compressedAxis.DecompressAxis(axis);
		}

		private static float DecompressAxis(this uint val, int axis)
		{
			return axisRanges[axis].Decompress(val);
		}

		public static CompressedElement ReadCompressedPosFromBitstream(ref UdpBitStream bitstream, IncludedAxes ia, BitCullingLevel bcl)
		{
			return new CompressedElement(
				null,
				(ia.IsXYZ(0)) ? (bitstream.ReadUInt(axisRanges[0].GetBitsAtCullLevel(bcl))) : 0,
				(ia.IsXYZ(1)) ? (bitstream.ReadUInt(axisRanges[1].GetBitsAtCullLevel(bcl))) : 0,
				(ia.IsXYZ(2)) ? (bitstream.ReadUInt(axisRanges[2].GetBitsAtCullLevel(bcl))) : 0);
		}

		private static Vector3 Decompress(uint x, uint y, uint z)
		{
			return new Vector3
				(
					axisRanges[0].Decompress(x),
					axisRanges[1].Decompress(y),
					axisRanges[2].Decompress(z)
				);
		}
		public static Vector3 DecompressFromWorld(this CompressedElement compos)
		{
			return new Vector3
				(
					axisRanges[0].Decompress(compos.cx),
					axisRanges[1].Decompress(compos.cy),
					axisRanges[2].Decompress(compos.cz)
				);
		}

		public static Vector3 ClampAxes(Vector3 value)
		{
			return new Vector3(
				axisRanges[0].Clamp(value[0]),
				axisRanges[1].Clamp(value[1]),
				axisRanges[2].Clamp(value[2])
				);
		}


		//private static bool TestMatchingUpper(uint a, uint b, int lowerbits)
		//{
		//	return (((a >> lowerbits) << lowerbits) == ((b >> lowerbits) << lowerbits));
		//}

		//public static bool TestMatchingUpper(CompressedElement prevPos, CompressedElement b, FloatRange[] ar, BitCullingLevel bcl)
		//{
		//	return
		//		(
		//		TestMatchingUpper(prevPos.x, b.x, ar[0].BitsAtCullLevel(bcl)) &&
		//		TestMatchingUpper(prevPos.y, b.y, ar[1].BitsAtCullLevel(bcl)) &&
		//		TestMatchingUpper(prevPos.z, b.z, ar[2].BitsAtCullLevel(bcl))
		//		);
		//}


		///// <summary>
		///// Attempts to guess the most likely upperbits state by seeing if each axis of the new position would be
		///// closer to the old one if the upper bit is incremented by one, two, three etc. Stops trying when it fails to get a better result than the last increment.
		///// </summary>
		///// <param name="oldcpos">Last best position test against.</param>
		///// <returns>Returns a corrected CompressPos</returns>
		//public static CompressedElement GuessUpperBitsWorld(this CompressedElement newcpos, CompressedElement oldcpos, BitCullingLevel bcl)
		//{
		//	return newcpos.GuessUpperBits(oldcpos, axisRanges, bcl);
		//}

		//public static CompressedElement ZeroLowerBits(this CompressedElement fullpos, BitCullingLevel bcl)
		//{
		//	return new CompressedElement(
		//		null,
		//		axisRanges[0].ZeroLowerBits(fullpos.cx, bcl),
		//		axisRanges[1].ZeroLowerBits(fullpos.cy, bcl),
		//		axisRanges[2].ZeroLowerBits(fullpos.cz, bcl)
		//		);
		//}

		//public static CompressedElement ZeroUpperBits(this CompressedElement fullpos, BitCullingLevel bcl)
		//{
		//	return new CompressedElement(
		//		null,
		//		axisRanges[0].ZeroUpperBits(fullpos.cx, bcl),
		//		axisRanges[1].ZeroUpperBits(fullpos.cy, bcl),
		//		axisRanges[2].ZeroUpperBits(fullpos.cz, bcl)
		//		);
		//}

		public static CompressedElement OverwriteLowerBits(CompressedElement upperbits, CompressedElement lowerbits, BitCullingLevel bcl)
		{
			return new CompressedElement
			(
				null,
				axisRanges[0].ZeroLowerBits(upperbits[0], bcl) | lowerbits[0],
				axisRanges[1].ZeroLowerBits(upperbits[1], bcl) | lowerbits[1],
				axisRanges[2].ZeroLowerBits(upperbits[2], bcl) | lowerbits[2]
			);
		}

	}
}



