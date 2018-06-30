//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace emotitron.Compression
{
	/// <summary>
	/// Create solid textures. Used to avoid buggy flickering of DrawRect. Thanks Unity.
	/// </summary>
	public static class SolidTextures
	{
		public static Texture2D black2D;
		public static Texture2D white2D;
		public static Texture2D lowcontrast2D;
		public static Texture2D highcontrast2D;
		public static Texture2D gray2D;
		public static Texture2D contrastgray2D;
		public static Texture2D red2D;
		public static Texture2D green2D;
		public static Texture2D blue2D;

		public static float lite = EditorGUIUtility.isProSkin ? .5f : .8f; // .78f;
		public static float medi = EditorGUIUtility.isProSkin ? .4f : .75f; // .74f;
		public static float dark = EditorGUIUtility.isProSkin ? .3f : .7f; //  .7f;

		public static float lowcontrast = EditorGUIUtility.isProSkin ? .1f : .5f;
		public static float highcontrast = EditorGUIUtility.isProSkin ? .8f : .4f;

		public static Color lowcontrastgray = new Color(lowcontrast, lowcontrast, lowcontrast);
		public static Color highcontrastgray = new Color(highcontrast, highcontrast, highcontrast);
		public static Color gray = EditorGUIUtility.isProSkin ? new Color(dark, dark, dark) : new Color(medi, medi, medi);
		public static Color contrastgray = EditorGUIUtility.isProSkin ? new Color(dark, dark, dark) : new Color(lite, lite, lite);
		public static Color red = EditorGUIUtility.isProSkin ? new Color(lite, dark, medi) : new Color(lite, dark, medi);
		public static Color green = EditorGUIUtility.isProSkin ? new Color(dark, lite, dark) : new Color(dark, lite, dark);
		public static Color blue = EditorGUIUtility.isProSkin ? new Color(dark, dark, lite) : new Color(dark, dark, lite);

		static SolidTextures()
		{
			CreateDefaultSolids();
		}

		private static void CreateDefaultSolids()
		{
			white2D = CreateSolid(Color.white);
			black2D = CreateSolid(Color.black);
			lowcontrast2D = CreateSolid(lowcontrastgray);
			highcontrast2D = CreateSolid(highcontrastgray);
			gray2D = CreateSolid(gray);
			contrastgray2D = CreateSolid(contrastgray);
			red2D = CreateSolid(red);
			green2D = CreateSolid(green);
			blue2D = CreateSolid(blue);
		}

		private static Texture2D CreateSolid(Color color)
		{
			Texture2D tex = new Texture2D(1, 1);
			tex.wrapMode = TextureWrapMode.Repeat;
			tex.SetPixel(0, 0, color);
			tex.Apply();
			return tex;
		}

		private static GUIStyle s_TempStyle = new GUIStyle();

		/// <summary>
		/// Replacement method for EditorGUI.DrawTexture and DrawRect... since they are buggy in drawers and will flicker.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="texture"></param>
		public static void DrawTexture(Rect position, Texture2D texture)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			// Need to constantly check for null, since Unity looses these textures after the editor exist play.
			if (texture == null)
				CreateDefaultSolids();

			s_TempStyle.normal.background = texture;

			s_TempStyle.Draw(position, GUIContent.none, false, false, false, false);
		}

	}
}

#endif
