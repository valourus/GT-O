//Copyright 2018, Davin Carten, All rights reserved

using System;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{

#if UNITY_EDITOR


	public abstract class CrusherDrawer : PropertyDrawer
	{
		public static bool FC_ISPRO = FloatCrusher.ISPRO;
		public static bool QC_ISPRO = QuatCompress.ISPRO;

		public const int PADDING = 4;
		public const int LINEHEIGHT = 16;
		public const float HHEIGHT = 18f;
		public const int FOOTHGHT = LINEHEIGHT + 2;
		public const float ENBLD_HGHT = 62f;
		public const float DISBL_HGHT = 20f;
		public const float BTTM_MARGIN = 4;
		public const int SPACING = 2;

		protected float line, paddedleft, paddedright, paddedwidth, fieldleft, fieldwidth, labelwidth, stdfieldwidth, rightinputsleft;

		public static GUIStyle miniLabelRight = new GUIStyle((GUIStyle)"MiniLabelRight") { normal = ((GUIStyle)"MiniLabel").normal };
		public static GUIStyle miniFadedLabelRight = new GUIStyle((GUIStyle)"MiniLabelRight") { normal = ((GUIStyle)"PR DisabledLabel").normal };
		public static GUIStyle miniFadedLabel = new GUIStyle((GUIStyle)"MiniLabel") { normal = ((GUIStyle)"PR DisabledLabel").normal };

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			paddedleft = r.xMin + PADDING;
			paddedright = r.xMax - PADDING;
			paddedwidth = r.width - PADDING * 2;
			labelwidth = EditorGUIUtility.labelWidth - 32;
			fieldleft = paddedleft + labelwidth;
			fieldwidth = paddedwidth - labelwidth;
			stdfieldwidth = 50f;
			rightinputsleft = paddedright - stdfieldwidth;
		}

		public void ProFeatureDialog(string extratext)
		{
			if (!EditorUtility.DisplayDialog("Pro Version Feature", "Adjustable bits are only available in \nTransform Crusher Pro.", "OK", "Open in Asset Store"))
				Application.OpenURL("https://assetstore.unity.com/packages/tools/network/transform-crusher-116587");
		}
		
	}

	[CustomPropertyDrawer(typeof(FloatCrusher))]
	[CanEditMultipleObjects]

	public class FloatCrusherDrawer : CrusherDrawer
	{

		FloatCrusher fc;
		private const string HALFX = "180°";
		private const string FULLX = "360°";

		private Texture2D colortex;

		protected float colwidths;
		protected float height;
		protected int savedIndentLevel;

		Rect r;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(r, label, property);

			base.OnGUI(r, property, label);

			// Hackjob way to get the target
			fc = (FloatCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("_min"));
			height = CalculateHeight(fc);

			this.r = r;
			savedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			line = r.yMin;

			colortex =
				(fc.axis == Axis.X) ? SolidTextures.red2D :
				(fc.axis == Axis.Y) ? SolidTextures.green2D :
				(fc.axis == Axis.Z) ? SolidTextures.blue2D :
				SolidTextures.gray2D;

			SolidTextures.DrawTexture(new Rect(r.xMin - 1, line - 1, r.width + 2, height + 2), SolidTextures.lowcontrast2D);
			SolidTextures.DrawTexture(new Rect(r.xMin, line, r.width, height), colortex);

			line += SPACING;
			DrawHeader(r, label);

			if (fc.enabled)
			{
				line += HHEIGHT + SPACING;
				DrawResolution();

				DrawCodecSettings(property);

				line += LINEHEIGHT + 3;
				DrawFooter();
			}

			EditorGUI.indentLevel = savedIndentLevel;

			EditorGUI.EndProperty();
		}
		
		private void DrawCodecSettings(SerializedProperty p)
		{
			if (fc.BitsDeterminedBy == BitsDeterminedBy.HalfFloat)
				return;


			line += LINEHEIGHT + SPACING;

			/// Line for the optional ranges

			if (fc.TRSType == TRSType.Euler)
			{
				bool useRange = EditorGUI.Toggle(new Rect(paddedleft, line, 16, LINEHEIGHT), GUIContent.none, fc.LimitRange, (GUIStyle)"OL Toggle");
				// if range just got turned off, reset the min/max to full rotation ranges
				fc.LimitRange = useRange;

				if (useRange)
					DrawRotationRanges();
				else
					EditorGUI.LabelField(new Rect(paddedleft + 18, line, 120, LINEHEIGHT), new GUIContent("Limit Ranges"));

			}
			else if (fc.BitsDeterminedBy == BitsDeterminedBy.HalfFloat)
			{
				// Nothinig is drawn currently when we are in half. TODO: Add a note about halffloat.
			}
			else
				DrawBasicRanges();



			/// Line for the Accuracte Center toggle

			if (fc.BitsDeterminedBy != BitsDeterminedBy.HalfFloat)  //(fc.BitsDeterminedBy >= 0)
			{
				line += LINEHEIGHT;

				string centervalue = p.FindPropertyRelative("centerValue").floatValue.ToString();

				GUIContent zeroCenterContent = new GUIContent("Use Accurate Center (" + centervalue + ")", "Scales the range to not use the highest compressed value, " +
					"resulting in an odd number of increments - which allows for an absolute center value. " +
					"Use this setting if you need to have an absolute and exact center value (such as zero). " +
					"Accurate Center with current range settings is " + centervalue);

				EditorGUI.LabelField(new Rect(paddedleft + 18, line, paddedwidth, LINEHEIGHT), zeroCenterContent);
				bool zeroToggle = EditorGUI.Toggle(new Rect(paddedleft, line, 16, LINEHEIGHT), GUIContent.none, fc.AccurateCenter, (GUIStyle)"OL Toggle");
				if (fc.AccurateCenter != zeroToggle)
				{
					fc.AccurateCenter = zeroToggle;
				}

			}



		}

		private void DrawHeader(Rect r, GUIContent label)
		{
			String headertext =
				(fc.axis == Axis.X) ? (fc.TRSType == TRSType.Euler) ? "X (Pitch)" : "X" :
				(fc.axis == Axis.Y) ? (fc.TRSType == TRSType.Euler) ? "Y (Yaw)" : "Y" :
				(fc.axis == Axis.Z) ? (fc.TRSType == TRSType.Euler) ? "Z (Roll)" : "Z" :
				(fc.axis == Axis.Uniform) ? "Uniform" :
				label.text; // "Float Crusher";

			if (fc.showEnableToggle) //  target.axis != Axis.AlwaysOn)
			{
				fc.enabled = EditorGUI.Toggle(new Rect(paddedleft, line, 32, LINEHEIGHT), GUIContent.none, fc.enabled);
				EditorGUI.LabelField(new Rect(paddedleft + 16, line, 128, LINEHEIGHT), new GUIContent(headertext));
			}
			else
			{
				EditorGUI.LabelField(new Rect(paddedleft, line, 128, LINEHEIGHT), new GUIContent(headertext));
			}

			if (!fc.enabled)
				return;

			if (fc.TRSType == TRSType.Euler && fc.axis == Axis.X)
				if (GUI.Button(new Rect(paddedleft + 100, line, 60, LINEHEIGHT), fc.UseHalfRangeX ? HALFX : FULLX, (GUIStyle)"minibutton"))
					fc.UseHalfRangeX = !fc.UseHalfRangeX;

			String bitstr = fc.Bits + " Bits";
			EditorGUI.LabelField(new Rect(paddedleft, line, paddedwidth, LINEHEIGHT), bitstr, miniLabelRight);
		}

		private void DrawBasicRanges()
		{
			float labelW = 48f;
			float inputW = 50f;

			float input1Left = fieldleft;
			float input2Left = paddedright - inputW;
			float label1Left = input1Left - labelW;
			float label2Left = input2Left - labelW;

			EditorGUI.LabelField(new Rect(paddedleft, line, labelwidth, LINEHEIGHT), new GUIContent("Range:"));

			EditorGUI.LabelField(new Rect(label1Left, line, labelW, LINEHEIGHT), new GUIContent("min: "), "RightLabel");
			float min = EditorGUI.FloatField(new Rect(input1Left, line, inputW, LINEHEIGHT), GUIContent.none, fc.Min);

			if (fc.Min != min)
				fc.Min = min;

			EditorGUI.LabelField(new Rect(label2Left, line, labelW, LINEHEIGHT), new GUIContent("max: "), "RightLabel");
			float max = EditorGUI.FloatField(new Rect(input2Left, line, inputW, LINEHEIGHT), GUIContent.none, fc.Max);

			if (fc.Max != max)
				fc.Max = max;
		}

		private void DrawRotationRanges()
		{
			float usedRange = 360f;

			float bleft = fieldleft; // paddedleft + 18;
									 ///float bwidth = paddedwidth - 18;
			float input1offset = 10;
			float inputWidth = 40f;
			float degreeSpace = 10f;
			float sliderleft = bleft - input1offset + PADDING + degreeSpace; // + fieldwidth + padding;
			float sliderwidth = fieldwidth - inputWidth + input1offset - PADDING * 2 - degreeSpace * 2;
			float left = r.xMin;

			float input1left = fieldleft - inputWidth - input1offset;
			float input2left = rightinputsleft;

			float sliderMax = (fc.axis == Axis.X && fc.UseHalfRangeX) ? 90 : 360;
			float sliderMin = (fc.axis == Axis.X && fc.UseHalfRangeX) ? -90 : -360;

			EditorGUI.LabelField(new Rect(input1left, line, inputWidth + degreeSpace, LINEHEIGHT), new GUIContent("°"), (GUIStyle)"RightLabel");
			EditorGUI.LabelField(new Rect(input2left, line, inputWidth + degreeSpace, LINEHEIGHT), new GUIContent("°"), (GUIStyle)"RightLabel");
			float min = EditorGUI.FloatField(new Rect(input1left, line, inputWidth, LINEHEIGHT), GUIContent.none, fc.Min);
			float max = EditorGUI.FloatField(new Rect(input2left, line, inputWidth, LINEHEIGHT), GUIContent.none, fc.Max);

			EditorGUI.MinMaxSlider(new Rect(sliderleft, line, sliderwidth, LINEHEIGHT), ref min, ref max, sliderMin, sliderMax);

			usedRange = Math.Min(max, sliderMax) - Math.Max(min, sliderMin);

			if (usedRange > 360)
				max = Mathf.Min(min + 360, 360);

			if (fc.Min != min || fc.Min < sliderMin)
			{
				fc.Min = Mathf.Max((int)min, sliderMin);
			}

			if (fc.Max != max || fc.Max > sliderMax)
			{
				fc.Max = Mathf.Min((int)max, sliderMax);
			}
		}

		private void DrawResolution()
		{
			BitsDeterminedBy btb = (BitsDeterminedBy)
				EditorGUI.EnumPopup(new Rect(paddedleft, line, labelwidth - 8, LINEHEIGHT), GUIContent.none, fc.BitsDeterminedBy);

			// IF we switched to pro - the btb value is actually the bits value, force a change to SetBits
			if (FC_ISPRO && btb >= 0)
			{
				fc.Bits = (int)btb;
			}

			else if (!FC_ISPRO && btb == BitsDeterminedBy.SetBits)//.CustomBits)
			{
				// In case we went from pro to free... quietly set this back to non-custom.
				if (fc.BitsDeterminedBy == BitsDeterminedBy.SetBits)
					fc.Bits = ((int)BitsDeterminedBy.SetBits > -1) ? (int)BitsDeterminedBy.SetBits : fc.Bits;
				else
					ProFeatureDialog("");
				
			}
			else if (fc.BitsDeterminedBy != btb)
			{
				fc.BitsDeterminedBy = btb;
			}

			float fieldleft = paddedleft + labelwidth;
			float fieldwidth = paddedwidth - labelwidth;

			switch (fc.BitsDeterminedBy)
			{
				case BitsDeterminedBy.HalfFloat:
					break;

				case BitsDeterminedBy.Resolution:

					EditorGUI.LabelField(new Rect(rightinputsleft - 128, line, 128, LINEHEIGHT), new GUIContent("Min resolution: 1/"), miniLabelRight);
					uint res = (uint)EditorGUI.IntField(new Rect(rightinputsleft, line, stdfieldwidth, LINEHEIGHT), GUIContent.none, (int)fc.Resolution);

					//if (fc.Resolution != res)
					fc.Resolution = res;

					break;

				case BitsDeterminedBy.Precision:

					EditorGUI.LabelField(new Rect(rightinputsleft - 128, line, 128, LINEHEIGHT), new GUIContent("Min precicion: "), miniLabelRight);
					float precision = EditorGUI.FloatField(new Rect(rightinputsleft, line, stdfieldwidth, LINEHEIGHT), GUIContent.none, fc.Precision);

					//if (fc.Precision != precision)
					fc.Precision = (float)Math.Round(precision * 100000) / 100000;

					break;

				default:

					if (FC_ISPRO && fc.BitsDeterminedBy == (BitsDeterminedBy.SetBits))
					{
						int bits = EditorGUI.IntSlider(new Rect(fieldleft, line, fieldwidth, LINEHEIGHT), GUIContent.none, fc.Bits, 0, 32);

						if (fc.Bits != bits)
							fc.Bits = bits;
						break;
					}

					GUI.enabled = false;
					EditorGUI.IntSlider(new Rect(fieldleft, line, fieldwidth, LINEHEIGHT), GUIContent.none, (int)fc.BitsDeterminedBy, 0, 32);
					GUI.enabled = true;

					break;
			}
		}

		private void DrawFooter()
		{
			//EditorGUI.DrawRect(new Rect(r.xMin, r.yMin + ENBLD_HGHT - FOOTHGHT, r.width, FOOTHGHT), usedcolor * .9f);
			EditorGUI.LabelField(new Rect(paddedleft, line, paddedwidth, FOOTHGHT), new GUIContent("Actual:"), miniFadedLabel);

			float prec = fc.GetPrecAtBits();// fc.Precision;
			float res = fc.GetResAtBits();
			// restrict prec to 7 characters
			String precstr = prec.ToString("F" + Math.Min(6, Math.Max(0, (6 - (int)Math.Log10(prec)))).ToString());
			String str = "res: 1/" + res + (fc.TRSType == TRSType.Euler ? "°" : "") + "   prec: " + precstr + (fc.TRSType == TRSType.Euler ? "°" : "");
			EditorGUI.LabelField(new Rect(paddedleft, line, paddedwidth, FOOTHGHT), str, miniFadedLabelRight);
		}

		private float CalculateHeight(FloatCrusher fc)
		{
			float rangeLine = (fc.BitsDeterminedBy == BitsDeterminedBy.HalfFloat) ? 0 :(LINEHEIGHT + SPACING);
			float accCenterLine = (fc.BitsDeterminedBy == BitsDeterminedBy.HalfFloat) ? 0 :LINEHEIGHT;// LINEHEIGHT; // (fc.BitsDeterminedBy >= 0 ? LINEHEIGHT : 0);
			return (fc.enabled) ?
				ENBLD_HGHT + rangeLine + accCenterLine:
				DISBL_HGHT;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Hackjob way to get the target - needs to reference a serialized field in order to work.
			fc = (FloatCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("_min"));

			return CalculateHeight(fc);
		}
	}

#endif

}
