//Copyright 2018, Davin Carten, All rights reserved

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace emotitron.Compression
{

	[CustomPropertyDrawer(typeof(TransformCrusher))]
	[CanEditMultipleObjects]

	public class TransformCrusherDrawer : CrusherDrawer
	{
		private const float TITL_HGHT = 18f;
		private const float SET_PAD = 2;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);

			// Hacky way to get the real object
			TransformCrusher target = (TransformCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("posCrusher"));

			float currentline = r.yMin;

			SerializedProperty pos = property.FindPropertyRelative("posCrusher");
			SerializedProperty rot = property.FindPropertyRelative("rotCrusher");
			SerializedProperty scl = property.FindPropertyRelative("sclCrusher");

			float ph = EditorGUI.GetPropertyHeight(pos);
			float rh = EditorGUI.GetPropertyHeight(rot);
			float sh = EditorGUI.GetPropertyHeight(scl);

			/// Header
			EditorGUI.LabelField(new Rect(r.xMin, currentline, r.width, TITL_HGHT), new GUIContent("Transform Crusher"), (GUIStyle)"BoldLabel");

			int totalbits = target.TallyBits();
			int frag0bits = Mathf.Clamp(totalbits, 0, 64);
			int frag1bits = Mathf.Clamp(totalbits - 64, 0, 64);
			int frag2bits = Mathf.Clamp(totalbits - 128, 0, 64);
			int frag3bits = Mathf.Clamp(totalbits - 192, 0, 64);

			string bitstr = frag0bits.ToString();
			if (frag1bits > 0)
				bitstr += " | " + frag1bits;
			if (frag2bits > 0)
				bitstr += " | " + frag2bits;
			if (frag3bits > 0)
				bitstr += " | " + frag3bits;

			bitstr = bitstr + " Bits";
			EditorGUI.LabelField(new Rect(paddedleft, currentline, paddedwidth, 16), bitstr, miniLabelRight);

			///GameObject target
			currentline += LINEHEIGHT;
			target.defaultTransform = (Transform)EditorGUI.ObjectField(new Rect(r.xMin, currentline, r.width, 16), new GUIContent("Target"), target.defaultTransform, typeof(Transform), true);

			if (target.defaultTransform == null)
			{
				target.defaultTransform = (property.serializedObject.targetObject as Component).transform;
			}

			/// TRS Element Boxes
			currentline += TITL_HGHT;

			DrawSet(r, currentline, ph, pos);
			currentline += ph + SET_PAD;

			DrawSet(r, currentline, rh, rot);
			currentline += rh + SET_PAD;

			DrawSet(r, currentline, sh, scl);
		}

		private void DrawSet(Rect r, float currentline, float h, SerializedProperty prop)
		{
			EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, h), prop);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float ph = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("posCrusher"));
			float rh = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rotCrusher"));
			float sh = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sclCrusher"));

			return TITL_HGHT + LINEHEIGHT + ph + rh + sh + SET_PAD * 2;
		}
	}

}
#endif
