//Copyright 2018, Davin Carten, All rights reserved

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{
	public enum CompressLevel { SetBits = -1, Disabled = 0, uint16Low = 16, uint32Med = 32, uint64Hi = 64 }

	/// <summary>
	/// A struct type that can accept any unsigned type and implicitly convert it to any other unsigned type. Removes the need for casting.
	/// (note: This will truncate any higher order bits when casting down)
	/// </summary>
	[System.Serializable]
	public class QuatCrusher
	{
		public static bool QC_ISPRO = QuatCompress.ISPRO;

		[Range(16, 64)]
		[SerializeField]
		private int bits;
		public int Bits
		{
			get { return (enabled) ? QC_ISPRO ? bits : RoundBitsToBestPreset(bits) : 0; }
			set
			{
				if (QC_ISPRO)
				{
					bits = value;
					CompressLevel = CompressLevel.SetBits;
				}
				else
				{
					bits = RoundBitsToBestPreset(value);
					CompressLevel = (CompressLevel)bits;
				}
			}
		}

		[SerializeField] public CompressLevel _compressLevel;
		public CompressLevel CompressLevel
		{
			get { return _compressLevel; }
			set
			{
				if (QC_ISPRO)
				{
					_compressLevel = value;
					bits = (_compressLevel == CompressLevel.SetBits) ? bits : (int)_compressLevel;
				}
				else
				{
					// If we were using custom bits (moved from Pro to free?), we need to get those bits, and use the closest value we can find
					if (_compressLevel == CompressLevel.SetBits)
						_compressLevel = (CompressLevel)bits;

					_compressLevel = (CompressLevel)RoundBitsToBestPreset((int)value);
					bits = (int)_compressLevel;
				}
			}
		}

		[SerializeField] public Transform transform;
		[SerializeField] public bool local;

		[HideInInspector] public bool isStandalone;
		[SerializeField] public bool showEnableToggle;
		[SerializeField] public bool enabled = true;


		private QuatCompress.Cache cache;

		private bool initialized;

		// Constructor
		public QuatCrusher(int bits, bool showEnableToggle = false, bool isStandalone = true)
		{
			this.bits = (QC_ISPRO) ? bits : RoundBitsToBestPreset(bits);
			this._compressLevel = CompressLevel.SetBits;

			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		// Constructor
		public QuatCrusher(bool showEnableToggle = false, bool isStandalone = true)
		{
			this.bits = 32;
			this._compressLevel = (QC_ISPRO) ? CompressLevel.SetBits : CompressLevel.uint32Med;
			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		// Constructor
		public QuatCrusher(CompressLevel compressLevel, bool showEnableToggle = false, bool isStandalone = true)
		{
			this._compressLevel = compressLevel;
			this.bits = (int)compressLevel;
			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		public void Initialize()
		{
			cache = QuatCompress.caches[/*_compressLevel != 0 ? (int)_compressLevel : */bits];
			initialized = true;
		}

		public static int RoundBitsToBestPreset(int bits)
		{
			if (bits > 32)
				return 64;
			if (bits > 16)
				return 32;
			if (bits > 8)
				return 16;
			return 0;
		}

		public ulong Compress()
		{
			if (!initialized)
				Initialize();

			if (local)
				return transform.localRotation.Compress(cache);
			else
				return transform.rotation.Compress(cache);
		}

		public ulong Compress(Quaternion quat)
		{
			if (!initialized)
				Initialize();

			return quat.Compress(cache);
		}

		public Quaternion Decompress(ulong compressed)
		{
			if (!initialized)
				Initialize();

			return compressed.Decompress(cache);
		}

		#region Array Buffer Writers

		public ulong Write(Quaternion quat, byte[] buffer, ref int bitposition)
		{
			ulong compressed = Compress(quat);
			buffer.Write(compressed, bits, ref bitposition);
			return compressed;
		}

		public ulong Write(Quaternion quat, uint[] buffer, ref int bitposition)
		{
			ulong compressed = Compress(quat);
			buffer.Write(compressed, bits, ref bitposition);
			return compressed;
		}

		public ulong Write(Quaternion quat, ulong[] buffer, ref int bitposition)
		{
			ulong compressed = Compress(quat);
			buffer.Write(compressed, bits, ref bitposition);
			return compressed;
		}

		public ulong Write(ulong c, byte[] buffer, ref int bitposition)
		{
			buffer.Write(c, bits, ref bitposition);
			return c;
		}

		public ulong Write(ulong c, uint[] buffer, ref int bitposition)
		{
			buffer.Write(c, bits, ref bitposition);
			return c;
		}

		public ulong Write(ulong c, ulong[] buffer, ref int bitposition)
		{
			buffer.Write(c, bits, ref bitposition);
			return c;
		}

		#endregion

		#region Array Buffer Readers

		public Quaternion Read(byte[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.ReadUInt64(bits, ref bitposition);
			return Decompress(compressed);
		}

		public Quaternion Read(uint[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.ReadUInt64(bits, ref bitposition);
			return Decompress(compressed);
		}

		public Quaternion Read(ulong[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.ReadUInt64(bits, ref bitposition);
			return Decompress(compressed);
		}

		#endregion

		public ulong Write(Quaternion quat, ref ulong buffer, ref int bitposition)
		{
			ulong compressed = Compress(quat);
			compressed.Inject(ref buffer, bits, ref bitposition);
			return compressed;
		}

		public Quaternion Read(ref ulong buffer, ref int bitposition)
		{
			ulong compressed = buffer.Extract(bits, ref bitposition);
			return Decompress(compressed);
		}
	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(QuatCrusher))]
	[CanEditMultipleObjects]

	public class QuatCrusherDrawer : CrusherDrawer
	{
		public const float TOP_PAD = 4f;
		public const float BTM_PAD = 6f;
		private const float TITL_HGHT = 18f;
		QuatCrusher target;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{

			EditorGUI.BeginProperty(r, label, property);

			base.OnGUI(r, property, label);

			property.serializedObject.Update();

			target = (QuatCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("bits"));
			MonoBehaviour component = (MonoBehaviour)property.serializedObject.targetObject;

			if (target.transform == null)
				target.transform = component.transform;

			line = r.yMin;

			float standalonesheight = target.isStandalone ? (SPACING + LINEHEIGHT) * 2 : 0;
			float boxheight = SPACING + HHEIGHT + SPACING + LINEHEIGHT + standalonesheight + SPACING;

			SolidTextures.DrawTexture(new Rect(r.xMin - 1, line - 1, r.width + 2, boxheight + 2), SolidTextures.lowcontrast2D);
			SolidTextures.DrawTexture(new Rect(r.xMin, line, r.width, boxheight), SolidTextures.gray2D);

			line += SPACING;
			DrawHeader(new Rect(r));
			line += HHEIGHT + SPACING;

			CompressLevel clvl = (CompressLevel)EditorGUI.EnumPopup(new Rect(paddedleft, line, labelwidth - PADDING, LINEHEIGHT), GUIContent.none, target.CompressLevel);

			if (!QC_ISPRO)
			{
				// In case we went from pro to free... quietly set this back to non-custom.
				if (target.CompressLevel == CompressLevel.SetBits)
					target.Bits = (int)target.CompressLevel; // CompressLevel =  CompressLevel.uint32Med;

				else if (clvl == CompressLevel.SetBits)
				{
					ProFeatureDialog("");
					target.CompressLevel = (CompressLevel)target.Bits;
				}

				else
					target.CompressLevel = clvl;
			}

			else if (clvl != target.CompressLevel)
				target.CompressLevel = clvl;


			var bitssp = property.FindPropertyRelative("bits");

			GUI.enabled = (QC_ISPRO);
			EditorGUI.PropertyField(new Rect(fieldleft, line, fieldwidth, LINEHEIGHT), bitssp, GUIContent.none);
			GUI.enabled = true;

			if (QC_ISPRO && bitssp.intValue != target.Bits)
			{
				target.CompressLevel = CompressLevel.SetBits;
			}



			if (target.isStandalone)
			{
				line += LINEHEIGHT + SPACING;
				EditorGUI.PropertyField(new Rect(paddedleft, line, paddedwidth, LINEHEIGHT), property.FindPropertyRelative("transform"));
				line += LINEHEIGHT + SPACING;
				EditorGUI.PropertyField(new Rect(paddedleft, line, paddedwidth, LINEHEIGHT), property.FindPropertyRelative("local"));
			}

			property.serializedObject.ApplyModifiedProperties();

			EditorGUI.EndProperty();
		}


		private void DrawHeader(Rect r)
		{
			String headertext = "Quat Compress";

			if (target.showEnableToggle) //  target.axis != Axis.AlwaysOn)
			{
				target.enabled = EditorGUI.Toggle(new Rect(paddedleft, line, 32, LINEHEIGHT), GUIContent.none, target.enabled);
				EditorGUI.LabelField(new Rect(paddedleft + 16, line, paddedwidth - 18, LINEHEIGHT), new GUIContent(headertext));
			}
			else
			{
				EditorGUI.LabelField(new Rect(paddedleft, line, paddedwidth, LINEHEIGHT), new GUIContent(headertext));
			}

			EditorGUI.LabelField(new Rect(r.xMin, line, r.width, 16), target.Bits + " Bits", FloatCrusherDrawer.miniLabelRight);

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float standalones = property.FindPropertyRelative("isStandalone").boolValue ? (SPACING + LINEHEIGHT) * 2 : 0;
			return SPACING + HHEIGHT + (SPACING + LINEHEIGHT) + standalones + SPACING; // + BTTM_MARGIN;
		}
	}
#endif
}