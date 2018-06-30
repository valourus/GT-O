//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

namespace emotitron.Compression
{

	public static class BitstreamExtensions
	{

		///// <summary>
		///// Write a compressed value to the bitstream. CompressedValue contains the bits used, so that no arugment is required for bits.
		///// </summary>
		//public static void Write(this Bitstream bs, CompressedValue cv)
		//{
		//	bs.Write(cv.cvalue, cv.bits);
		//}

		/// <summary>
		/// Write the used bytes (based on the writer position) to the NetworkWriter.
		/// </summary>
		public static void Write(this UnityEngine.Networking.NetworkWriter writer, ref Bitstream bitstream)
		{
			// Write the packed bytes from the bitstream into the UNET writer.
			int count = bitstream.BytesUsed;
			for (int i = 0; i < count; ++i)
			{
				writer.Write(bitstream.ReadByte());
			}
		}

		//public static void Read(UnityEngine.Networking.NetworkMessage msg) : this()
		//{
		//	UnityEngine.Networking.NetworkReader reader = msg.reader;

		//	int count = System.Math.Min(40, reader.Length);
		//	for (int i = (int)reader.Position; i < count; ++i)
		//	{
		//		byte by = reader.ReadByte();
		//		this.Write(by);
		//	}
		//}

		public static void Read(this UnityEngine.Networking.NetworkReader reader, ref Bitstream bitstream)
		{
			// Copy the reader into our buffer so we can extra the packed bits. UNET uses a byte reader so we can't directly read bit fragments out of it.
			int count = System.Math.Min(40, reader.Length);
			for (int i = (int)reader.Position; i < count; ++i)
			{
				byte b = reader.ReadByte();
				bitstream.WriteByte(b);
			}
		}

		//public void Write(this Bitstream bitstream, FloatCrusher crusher, float value)
		//{
		//	crusher.Write(value, ref bitstream);
		//}

		///// <summary>
		///// Write bitstream contents (rounded up to the nearest byte) UNET NetworkWriter.
		///// </summary>
		///// <param name="writer"></param>
		//public static void Write(this Bitstream bitstream, UnityEngine.Networking.NetworkWriter writer)
		//{
		//	// Write the packed bytes from the bitstream into the UNET writer.
		//	int count = bitstream.BytesUsed;
		//	for (int i = 0; i < count; ++i)
		//	{
		//		writer.Write(bitstream.ReadByte());
		//	}
		//}
	}
}

