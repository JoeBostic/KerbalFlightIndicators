﻿using System;
using System.Globalization;
using System.Text;
using DaMichelToolbarSuperWrapper;
using KSP.IO;
using UnityEngine;

namespace KerbalFlightIndicators
{
	#region Utilities

#if DEBUG
	public static class DMDebug
	{
		public static void PrintTransformHierarchy(Transform transf, int depth, StringBuilder sb)
		{
			PrintTransform(transf, depth, sb);
			for (var i = 0; i < transf.childCount; ++i) {
				PrintTransformHierarchy(transf.GetChild(i), depth + 1, sb);
			}
		}

		public static void PrintTransform(Transform transf, int depth, StringBuilder sb)
		{
			var vfmt = "F3";
			sb.AppendLine(new string(' ', depth) + transf);
			sb.AppendLine("P = " + transf.localPosition.ToString(vfmt));
			sb.AppendLine("R = " + transf.localEulerAngles.ToString(vfmt));
			sb.AppendLine("S = " + transf.localScale.ToString(vfmt));
			sb.AppendLine("MW = \n" + transf.localToWorldMatrix.ToString(vfmt));
			if (transf.parent != null) {
				var ml = transf.parent.localToWorldMatrix.inverse * transf.localToWorldMatrix;
				sb.AppendLine("ML = \n" + ml.ToString(vfmt));
			}
		}

		public static void PrintTransformHierarchyUp(Transform transf, int depth, StringBuilder sb)
		{
			PrintTransform(transf, depth, sb);
			if (transf.parent) {
				PrintTransformHierarchyUp(transf.parent, depth + 1, sb);
			}
		}

		// from http://stackoverflow.com/questions/1838963/easy-and-fast-way-to-convert-an-int-to-binary
		public static string ToBin(int value, int len)
		{
			return (len > 1 ? ToBin(value >> 1, len - 1) : null) + "01"[value & 1];
		}

		public static string DebugRepr(Quaternion q)
		{
			var x = q * Util.x; // apply rotation to vectors
			var y = q * Util.y;
			var z = q * Util.z;
			return string.Format("[{0}, {1}, {2}]", x.ToString("F3"), y.ToString("F3"), z.ToString("F3"));
		}
	}
#endif


	public static class GUIExt
	{
		public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height)
		{
			GUI.DrawTexture(new Rect(position.x - width * 0.5f,
					position.y - height * 0.5f,
					width, height),
				tex);
		}

		public static void DrawTextureCentered(Vector3 position, Texture2D tex)
		{
			DrawTextureCentered(position, tex, tex.width, tex.height);
		}

		public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height, Rect texCoords)
		{
			GUI.DrawTextureWithTexCoords(new Rect(position.x - width * 0.5f,
					position.y - height * 0.5f,
					width, height),
				tex, texCoords);
		}
	}


	public static class Util
	{
		public static Vector3 x = Vector3.right;
		public static Vector3 y = Vector3.up;
		public static Vector3 z = Vector3.forward;

		public static float RAD2DEGf = 180f / Mathf.PI;
		public static double RAD2DEG = 180.0 / Math.PI;

		// angle need to rotate the x-axis so that it is parallel with (x,y); in Radians
		public static float PolarAngle(Vector2 v)
		{
			var n = v.magnitude;
			var a = Mathf.Acos(v.x / n);
			if (v.y < 0f) a = Mathf.PI * 2f - a;
			return a;
		}

		public static Vector3 PerspectiveProjection(Camera cam, Vector3 p)
		{
			return cam.projectionMatrix.MultiplyPoint(p);
		}

		public static Vector3 CameraToViewport(Camera cam, Vector3 v)
		{
			var q = cam.projectionMatrix.MultiplyPoint(v);
			// After projection, coordinates go from -1 to 1.
			// We need to transform to proper viewport space which goes from 0 to 1.
			// The bottom left is viewport-(0,0), according to the Unity 3d reference.
			// For some odd reason the x axis in camera space seems reversed. 
			q.y = (q.y + 1.0f) * 0.5f;
			q.x = (1.0f - q.x) * 0.5f;
			return q;
		}

		public static Vector3 CameraToScreen(Camera cam, Vector3 v)
		{
			return cam.ViewportToScreenPoint(CameraToViewport(cam, v));
		}

		public static Texture2D LoadTexture(string filename)
		{
			var data = File.ReadAllBytes<KerbalFlightIndicators>(filename);
			// we can just create a new texture object with a blank 2x2 image. 
			var tex = new Texture2D(2, 2);
			tex.LoadImage(data); // Resizes the texture to hold the image data. Sets width and height attribute according to the loaded image.
			tex.anisoLevel = 0;
			tex.filterMode = FilterMode.Bilinear;
			return tex;
		}

		public static Color ColorFromString(string s)
		{
			var result = new Color();
			var strvalues = s.Split(',');
			for (var i = 0; i < strvalues.Length; ++i) {
				result[i] = float.Parse(strvalues[i].Trim(), CultureInfo.InvariantCulture);
			}

			return result;
		}

		public static string ColorToString(Color c)
		{
			var s = new string[4];
			for (var i = 0; i < 4; ++i) {
				s[i] = c[i].ToString(CultureInfo.InvariantCulture);
			}

			return string.Join(",", s);
		}


		public static Rect[] ComputeTexCoords(int tex_width, int tex_height, RectOffset[] atlas)
		{
			var result = new Rect[atlas.Length];
			var sx = 1f / tex_width;
			var sy = 1f / tex_height;
			for (var i = 0; i < atlas.Length; ++i) {
				result[i] = new Rect(
					atlas[i].left * sx,
					1f - atlas[i].bottom * sy,
					(atlas[i].right - atlas[i].left) * sx,
					(atlas[i].bottom - atlas[i].top) * sy
				);
			}

			return result;
		}

		public static RectOffset RectOffsetFromString(string s)
		{
			int left, right, bottom, top;
			var strvalues = s.Split(',');
			left = int.Parse(strvalues[0]);
			right = int.Parse(strvalues[1]);
			top = int.Parse(strvalues[2]);
			bottom = int.Parse(strvalues[3]);
			return new RectOffset(left, right, top, bottom);
		}
	}

	#endregion

	#region comments

/*
These are KSPs layers
 mask 0 = Default
 mask 1 = TransparentFX
 mask 2 = Ignore Raycast
 mask 3 = 
 mask 4 = Water
 mask 5 = UI
 mask 6 = 
 mask 7 = 
 mask 8 = PartsList_Icons
 mask 9 = Atmosphere
 mask 10 = Scaled Scenery
 mask 11 = UI_Culled
 mask 12 = UI_Main
 mask 13 = UI_Mask
 mask 14 = Screens
 mask 15 = Local Scenery
 mask 16 = kerbals
 mask 17 = Editor_UI
 mask 18 = SkySphere
 mask 19 = Disconnected Parts
 mask 20 = Internal Space
 mask 21 = Part Triggers
 mask 22 = KerbalInstructors
 mask 23 = ScaledSpaceSun
 mask 24 = MapFX
 mask 25 = EzGUI_UI
 mask 26 = WheelCollidersIgnore
 mask 27 = WheelColliders
 mask 28 = TerrainColliders
 mask 29 = 
 mask 30 = 
 */
/*
ToBin
*/
/*
AllLayers = 11111111111111111111111111111111, -1
DefaultRaycastLayers = 11111111111111111111111111111011, -5
IgnoreRaycastLayer = 00000000000000000000000000000100, 4
Collision Matrix: 
   0(Default) - 0(Default): True
   0(Default) - 1(TransparentFX): True
   0(Default) - 2(Ignore Raycast): True
   0(Default) - 3(): True
   0(Default) - 4(Water): True
   0(Default) - 5(UI): False
   0(Default) - 6(): True
   0(Default) - 7(): True
   0(Default) - 8(PartsList_Icons): False
   0(Default) - 9(Atmosphere): False
   0(Default) - 10(Scaled Scenery): False
   0(Default) - 11(UI_Culled): False
   0(Default) - 12(UI_Main): False
   0(Default) - 13(UI_Mask): False
   0(Default) - 14(Screens): False
   0(Default) - 15(Local Scenery): True
   0(Default) - 16(kerbals): False
   0(Default) - 17(Editor_UI): False
   0(Default) - 18(SkySphere): False
   0(Default) - 19(Disconnected Parts): True
   0(Default) - 20(Internal Space): False
   0(Default) - 21(Part Triggers): False
   0(Default) - 22(KerbalInstructors): False
   0(Default) - 23(ScaledSpaceSun): False
   0(Default) - 24(MapFX): False
   0(Default) - 25(EzGUI_UI): False
   0(Default) - 26(WheelCollidersIgnore): True
   0(Default) - 27(WheelColliders): True
   0(Default) - 28(TerrainColliders): False
   0(Default) - 29(): True
   0(Default) - 30(): True
   1(TransparentFX) - 0(Default): True
   1(TransparentFX) - 1(TransparentFX): True
   1(TransparentFX) - 2(Ignore Raycast): True
   1(TransparentFX) - 3(): True
   1(TransparentFX) - 4(Water): True
   1(TransparentFX) - 5(UI): False
   1(TransparentFX) - 6(): True
   1(TransparentFX) - 7(): True
   1(TransparentFX) - 8(PartsList_Icons): False
   1(TransparentFX) - 9(Atmosphere): False
   1(TransparentFX) - 10(Scaled Scenery): False
   1(TransparentFX) - 11(UI_Culled): False
   1(TransparentFX) - 12(UI_Main): False
   1(TransparentFX) - 13(UI_Mask): False
   1(TransparentFX) - 14(Screens): False
   1(TransparentFX) - 15(Local Scenery): True
   1(TransparentFX) - 16(kerbals): False
   1(TransparentFX) - 17(Editor_UI): False
   1(TransparentFX) - 18(SkySphere): False
   1(TransparentFX) - 19(Disconnected Parts): True
   1(TransparentFX) - 20(Internal Space): False
   1(TransparentFX) - 21(Part Triggers): False
   1(TransparentFX) - 22(KerbalInstructors): False
   1(TransparentFX) - 23(ScaledSpaceSun): False
   1(TransparentFX) - 24(MapFX): False
   1(TransparentFX) - 25(EzGUI_UI): False
   1(TransparentFX) - 26(WheelCollidersIgnore): True
   1(TransparentFX) - 27(WheelColliders): True
   1(TransparentFX) - 28(TerrainColliders): False
   1(TransparentFX) - 29(): True
   1(TransparentFX) - 30(): True
   2(Ignore Raycast) - 0(Default): True
   2(Ignore Raycast) - 1(TransparentFX): True
   2(Ignore Raycast) - 2(Ignore Raycast): True
   2(Ignore Raycast) - 3(): True
   2(Ignore Raycast) - 4(Water): True
   2(Ignore Raycast) - 5(UI): False
   2(Ignore Raycast) - 6(): True
   2(Ignore Raycast) - 7(): True
   2(Ignore Raycast) - 8(PartsList_Icons): False
   2(Ignore Raycast) - 9(Atmosphere): False
   2(Ignore Raycast) - 10(Scaled Scenery): False
   2(Ignore Raycast) - 11(UI_Culled): False
   2(Ignore Raycast) - 12(UI_Main): False
   2(Ignore Raycast) - 13(UI_Mask): False
   2(Ignore Raycast) - 14(Screens): False
   2(Ignore Raycast) - 15(Local Scenery): True
   2(Ignore Raycast) - 16(kerbals): False
   2(Ignore Raycast) - 17(Editor_UI): False
   2(Ignore Raycast) - 18(SkySphere): False
   2(Ignore Raycast) - 19(Disconnected Parts): True
   2(Ignore Raycast) - 20(Internal Space): False
   2(Ignore Raycast) - 21(Part Triggers): False
   2(Ignore Raycast) - 22(KerbalInstructors): False
   2(Ignore Raycast) - 23(ScaledSpaceSun): False
   2(Ignore Raycast) - 24(MapFX): False
   2(Ignore Raycast) - 25(EzGUI_UI): False
   2(Ignore Raycast) - 26(WheelCollidersIgnore): True
   2(Ignore Raycast) - 27(WheelColliders): True
   2(Ignore Raycast) - 28(TerrainColliders): False
   2(Ignore Raycast) - 29(): True
   2(Ignore Raycast) - 30(): True
   3() - 0(Default): True
   3() - 1(TransparentFX): True
   3() - 2(Ignore Raycast): True
   3() - 3(): True
   3() - 4(Water): True
   3() - 5(UI): True
   3() - 6(): True
   3() - 7(): True
   3() - 8(PartsList_Icons): True
   3() - 9(Atmosphere): True
   3() - 10(Scaled Scenery): True
   3() - 11(UI_Culled): True
   3() - 12(UI_Main): True
   3() - 13(UI_Mask): True
   3() - 14(Screens): True
   3() - 15(Local Scenery): True
   3() - 16(kerbals): True
   3() - 17(Editor_UI): True
   3() - 18(SkySphere): True
   3() - 19(Disconnected Parts): True
   3() - 20(Internal Space): True
   3() - 21(Part Triggers): True
   3() - 22(KerbalInstructors): True
   3() - 23(ScaledSpaceSun): True
   3() - 24(MapFX): True
   3() - 25(EzGUI_UI): True
   3() - 26(WheelCollidersIgnore): True
   3() - 27(WheelColliders): True
   3() - 28(TerrainColliders): True
   3() - 29(): True
   3() - 30(): True
   4(Water) - 0(Default): True
   4(Water) - 1(TransparentFX): True
   4(Water) - 2(Ignore Raycast): True
   4(Water) - 3(): True
   4(Water) - 4(Water): True
   4(Water) - 5(UI): False
   4(Water) - 6(): True
   4(Water) - 7(): True
   4(Water) - 8(PartsList_Icons): False
   4(Water) - 9(Atmosphere): False
   4(Water) - 10(Scaled Scenery): False
   4(Water) - 11(UI_Culled): False
   4(Water) - 12(UI_Main): False
   4(Water) - 13(UI_Mask): False
   4(Water) - 14(Screens): False
   4(Water) - 15(Local Scenery): True
   4(Water) - 16(kerbals): False
   4(Water) - 17(Editor_UI): False
   4(Water) - 18(SkySphere): False
   4(Water) - 19(Disconnected Parts): True
   4(Water) - 20(Internal Space): False
   4(Water) - 21(Part Triggers): False
   4(Water) - 22(KerbalInstructors): False
   4(Water) - 23(ScaledSpaceSun): False
   4(Water) - 24(MapFX): False
   4(Water) - 25(EzGUI_UI): False
   4(Water) - 26(WheelCollidersIgnore): True
   4(Water) - 27(WheelColliders): True
   4(Water) - 28(TerrainColliders): True
   4(Water) - 29(): True
   4(Water) - 30(): True
   5(UI) - 0(Default): False
   5(UI) - 1(TransparentFX): False
   5(UI) - 2(Ignore Raycast): False
   5(UI) - 3(): True
   5(UI) - 4(Water): False
   5(UI) - 5(UI): False
   5(UI) - 6(): True
   5(UI) - 7(): True
   5(UI) - 8(PartsList_Icons): False
   5(UI) - 9(Atmosphere): False
   5(UI) - 10(Scaled Scenery): False
   5(UI) - 11(UI_Culled): False
   5(UI) - 12(UI_Main): False
   5(UI) - 13(UI_Mask): False
   5(UI) - 14(Screens): False
   5(UI) - 15(Local Scenery): False
   5(UI) - 16(kerbals): False
   5(UI) - 17(Editor_UI): False
   5(UI) - 18(SkySphere): False
   5(UI) - 19(Disconnected Parts): False
   5(UI) - 20(Internal Space): False
   5(UI) - 21(Part Triggers): False
   5(UI) - 22(KerbalInstructors): False
   5(UI) - 23(ScaledSpaceSun): False
   5(UI) - 24(MapFX): False
   5(UI) - 25(EzGUI_UI): False
   5(UI) - 26(WheelCollidersIgnore): False
   5(UI) - 27(WheelColliders): False
   5(UI) - 28(TerrainColliders): False
   5(UI) - 29(): True
   5(UI) - 30(): True
   6() - 0(Default): True
   6() - 1(TransparentFX): True
   6() - 2(Ignore Raycast): True
   6() - 3(): True
   6() - 4(Water): True
   6() - 5(UI): True
   6() - 6(): True
   6() - 7(): True
   6() - 8(PartsList_Icons): True
   6() - 9(Atmosphere): True
   6() - 10(Scaled Scenery): True
   6() - 11(UI_Culled): True
   6() - 12(UI_Main): True
   6() - 13(UI_Mask): True
   6() - 14(Screens): True
   6() - 15(Local Scenery): True
   6() - 16(kerbals): True
   6() - 17(Editor_UI): True
   6() - 18(SkySphere): True
   6() - 19(Disconnected Parts): True
   6() - 20(Internal Space): True
   6() - 21(Part Triggers): True
   6() - 22(KerbalInstructors): True
   6() - 23(ScaledSpaceSun): True
   6() - 24(MapFX): True
   6() - 25(EzGUI_UI): True
   6() - 26(WheelCollidersIgnore): True
   6() - 27(WheelColliders): True
   6() - 28(TerrainColliders): True
   6() - 29(): True
   6() - 30(): True
   7() - 0(Default): True
   7() - 1(TransparentFX): True
   7() - 2(Ignore Raycast): True
   7() - 3(): True
   7() - 4(Water): True
   7() - 5(UI): True
   7() - 6(): True
   7() - 7(): True
   7() - 8(PartsList_Icons): True
   7() - 9(Atmosphere): True
   7() - 10(Scaled Scenery): True
   7() - 11(UI_Culled): True
   7() - 12(UI_Main): True
   7() - 13(UI_Mask): True
   7() - 14(Screens): True
   7() - 15(Local Scenery): True
   7() - 16(kerbals): True
   7() - 17(Editor_UI): True
   7() - 18(SkySphere): True
   7() - 19(Disconnected Parts): True
   7() - 20(Internal Space): True
   7() - 21(Part Triggers): True
   7() - 22(KerbalInstructors): True
   7() - 23(ScaledSpaceSun): True
   7() - 24(MapFX): True
   7() - 25(EzGUI_UI): True
   7() - 26(WheelCollidersIgnore): True
   7() - 27(WheelColliders): True
   7() - 28(TerrainColliders): True
   7() - 29(): True
   7() - 30(): True
   8(PartsList_Icons) - 0(Default): False
   8(PartsList_Icons) - 1(TransparentFX): False
   8(PartsList_Icons) - 2(Ignore Raycast): False
   8(PartsList_Icons) - 3(): True
   8(PartsList_Icons) - 4(Water): False
   8(PartsList_Icons) - 5(UI): False
   8(PartsList_Icons) - 6(): True
   8(PartsList_Icons) - 7(): True
   8(PartsList_Icons) - 8(PartsList_Icons): False
   8(PartsList_Icons) - 9(Atmosphere): False
   8(PartsList_Icons) - 10(Scaled Scenery): False
   8(PartsList_Icons) - 11(UI_Culled): False
   8(PartsList_Icons) - 12(UI_Main): False
   8(PartsList_Icons) - 13(UI_Mask): False
   8(PartsList_Icons) - 14(Screens): False
   8(PartsList_Icons) - 15(Local Scenery): False
   8(PartsList_Icons) - 16(kerbals): False
   8(PartsList_Icons) - 17(Editor_UI): False
   8(PartsList_Icons) - 18(SkySphere): False
   8(PartsList_Icons) - 19(Disconnected Parts): False
   8(PartsList_Icons) - 20(Internal Space): False
   8(PartsList_Icons) - 21(Part Triggers): False
   8(PartsList_Icons) - 22(KerbalInstructors): False
   8(PartsList_Icons) - 23(ScaledSpaceSun): False
   8(PartsList_Icons) - 24(MapFX): False
   8(PartsList_Icons) - 25(EzGUI_UI): False
   8(PartsList_Icons) - 26(WheelCollidersIgnore): False
   8(PartsList_Icons) - 27(WheelColliders): False
   8(PartsList_Icons) - 28(TerrainColliders): False
   8(PartsList_Icons) - 29(): True
   8(PartsList_Icons) - 30(): True
   9(Atmosphere) - 0(Default): False
   9(Atmosphere) - 1(TransparentFX): False
   9(Atmosphere) - 2(Ignore Raycast): False
   9(Atmosphere) - 3(): True
   9(Atmosphere) - 4(Water): False
   9(Atmosphere) - 5(UI): False
   9(Atmosphere) - 6(): True
   9(Atmosphere) - 7(): True
   9(Atmosphere) - 8(PartsList_Icons): False
   9(Atmosphere) - 9(Atmosphere): False
   9(Atmosphere) - 10(Scaled Scenery): False
   9(Atmosphere) - 11(UI_Culled): False
   9(Atmosphere) - 12(UI_Main): False
   9(Atmosphere) - 13(UI_Mask): False
   9(Atmosphere) - 14(Screens): False
   9(Atmosphere) - 15(Local Scenery): False
   9(Atmosphere) - 16(kerbals): False
   9(Atmosphere) - 17(Editor_UI): False
   9(Atmosphere) - 18(SkySphere): False
   9(Atmosphere) - 19(Disconnected Parts): False
   9(Atmosphere) - 20(Internal Space): False
   9(Atmosphere) - 21(Part Triggers): False
   9(Atmosphere) - 22(KerbalInstructors): False
   9(Atmosphere) - 23(ScaledSpaceSun): False
   9(Atmosphere) - 24(MapFX): False
   9(Atmosphere) - 25(EzGUI_UI): False
   9(Atmosphere) - 26(WheelCollidersIgnore): False
   9(Atmosphere) - 27(WheelColliders): False
   9(Atmosphere) - 28(TerrainColliders): False
   9(Atmosphere) - 29(): True
   9(Atmosphere) - 30(): True
   10(Scaled Scenery) - 0(Default): False
   10(Scaled Scenery) - 1(TransparentFX): False
   10(Scaled Scenery) - 2(Ignore Raycast): False
   10(Scaled Scenery) - 3(): True
   10(Scaled Scenery) - 4(Water): False
   10(Scaled Scenery) - 5(UI): False
   10(Scaled Scenery) - 6(): True
   10(Scaled Scenery) - 7(): True
   10(Scaled Scenery) - 8(PartsList_Icons): False
   10(Scaled Scenery) - 9(Atmosphere): False
   10(Scaled Scenery) - 10(Scaled Scenery): False
   10(Scaled Scenery) - 11(UI_Culled): False
   10(Scaled Scenery) - 12(UI_Main): False
   10(Scaled Scenery) - 13(UI_Mask): False
   10(Scaled Scenery) - 14(Screens): False
   10(Scaled Scenery) - 15(Local Scenery): False
   10(Scaled Scenery) - 16(kerbals): False
   10(Scaled Scenery) - 17(Editor_UI): False
   10(Scaled Scenery) - 18(SkySphere): False
   10(Scaled Scenery) - 19(Disconnected Parts): False
   10(Scaled Scenery) - 20(Internal Space): False
   10(Scaled Scenery) - 21(Part Triggers): False
   10(Scaled Scenery) - 22(KerbalInstructors): False
   10(Scaled Scenery) - 23(ScaledSpaceSun): False
   10(Scaled Scenery) - 24(MapFX): False
   10(Scaled Scenery) - 25(EzGUI_UI): False
   10(Scaled Scenery) - 26(WheelCollidersIgnore): False
   10(Scaled Scenery) - 27(WheelColliders): False
   10(Scaled Scenery) - 28(TerrainColliders): False
   10(Scaled Scenery) - 29(): True
   10(Scaled Scenery) - 30(): True
   11(UI_Culled) - 0(Default): False
   11(UI_Culled) - 1(TransparentFX): False
   11(UI_Culled) - 2(Ignore Raycast): False
   11(UI_Culled) - 3(): True
   11(UI_Culled) - 4(Water): False
   11(UI_Culled) - 5(UI): False
   11(UI_Culled) - 6(): True
   11(UI_Culled) - 7(): True
   11(UI_Culled) - 8(PartsList_Icons): False
   11(UI_Culled) - 9(Atmosphere): False
   11(UI_Culled) - 10(Scaled Scenery): False
   11(UI_Culled) - 11(UI_Culled): False
   11(UI_Culled) - 12(UI_Main): False
   11(UI_Culled) - 13(UI_Mask): False
   11(UI_Culled) - 14(Screens): False
   11(UI_Culled) - 15(Local Scenery): False
   11(UI_Culled) - 16(kerbals): False
   11(UI_Culled) - 17(Editor_UI): False
   11(UI_Culled) - 18(SkySphere): False
   11(UI_Culled) - 19(Disconnected Parts): False
   11(UI_Culled) - 20(Internal Space): False
   11(UI_Culled) - 21(Part Triggers): False
   11(UI_Culled) - 22(KerbalInstructors): False
   11(UI_Culled) - 23(ScaledSpaceSun): False
   11(UI_Culled) - 24(MapFX): False
   11(UI_Culled) - 25(EzGUI_UI): False
   11(UI_Culled) - 26(WheelCollidersIgnore): False
   11(UI_Culled) - 27(WheelColliders): False
   11(UI_Culled) - 28(TerrainColliders): False
   11(UI_Culled) - 29(): True
   11(UI_Culled) - 30(): True
   12(UI_Main) - 0(Default): False
   12(UI_Main) - 1(TransparentFX): False
   12(UI_Main) - 2(Ignore Raycast): False
   12(UI_Main) - 3(): True
   12(UI_Main) - 4(Water): False
   12(UI_Main) - 5(UI): False
   12(UI_Main) - 6(): True
   12(UI_Main) - 7(): True
   12(UI_Main) - 8(PartsList_Icons): False
   12(UI_Main) - 9(Atmosphere): False
   12(UI_Main) - 10(Scaled Scenery): False
   12(UI_Main) - 11(UI_Culled): False
   12(UI_Main) - 12(UI_Main): False
   12(UI_Main) - 13(UI_Mask): False
   12(UI_Main) - 14(Screens): False
   12(UI_Main) - 15(Local Scenery): False
   12(UI_Main) - 16(kerbals): False
   12(UI_Main) - 17(Editor_UI): False
   12(UI_Main) - 18(SkySphere): False
   12(UI_Main) - 19(Disconnected Parts): False
   12(UI_Main) - 20(Internal Space): False
   12(UI_Main) - 21(Part Triggers): False
   12(UI_Main) - 22(KerbalInstructors): False
   12(UI_Main) - 23(ScaledSpaceSun): False
   12(UI_Main) - 24(MapFX): False
   12(UI_Main) - 25(EzGUI_UI): False
   12(UI_Main) - 26(WheelCollidersIgnore): False
   12(UI_Main) - 27(WheelColliders): False
   12(UI_Main) - 28(TerrainColliders): False
   12(UI_Main) - 29(): True
   12(UI_Main) - 30(): True
   13(UI_Mask) - 0(Default): False
   13(UI_Mask) - 1(TransparentFX): False
   13(UI_Mask) - 2(Ignore Raycast): False
   13(UI_Mask) - 3(): True
   13(UI_Mask) - 4(Water): False
   13(UI_Mask) - 5(UI): False
   13(UI_Mask) - 6(): True
   13(UI_Mask) - 7(): True
   13(UI_Mask) - 8(PartsList_Icons): False
   13(UI_Mask) - 9(Atmosphere): False
   13(UI_Mask) - 10(Scaled Scenery): False
   13(UI_Mask) - 11(UI_Culled): False
   13(UI_Mask) - 12(UI_Main): False
   13(UI_Mask) - 13(UI_Mask): False
   13(UI_Mask) - 14(Screens): False
   13(UI_Mask) - 15(Local Scenery): False
   13(UI_Mask) - 16(kerbals): False
   13(UI_Mask) - 17(Editor_UI): False
   13(UI_Mask) - 18(SkySphere): False
   13(UI_Mask) - 19(Disconnected Parts): False
   13(UI_Mask) - 20(Internal Space): False
   13(UI_Mask) - 21(Part Triggers): False
   13(UI_Mask) - 22(KerbalInstructors): False
   13(UI_Mask) - 23(ScaledSpaceSun): False
   13(UI_Mask) - 24(MapFX): False
   13(UI_Mask) - 25(EzGUI_UI): False
   13(UI_Mask) - 26(WheelCollidersIgnore): False
   13(UI_Mask) - 27(WheelColliders): False
   13(UI_Mask) - 28(TerrainColliders): False
   13(UI_Mask) - 29(): True
   13(UI_Mask) - 30(): True
   14(Screens) - 0(Default): False
   14(Screens) - 1(TransparentFX): False
   14(Screens) - 2(Ignore Raycast): False
   14(Screens) - 3(): True
   14(Screens) - 4(Water): False
   14(Screens) - 5(UI): False
   14(Screens) - 6(): True
   14(Screens) - 7(): True
   14(Screens) - 8(PartsList_Icons): False
   14(Screens) - 9(Atmosphere): False
   14(Screens) - 10(Scaled Scenery): False
   14(Screens) - 11(UI_Culled): False
   14(Screens) - 12(UI_Main): False
   14(Screens) - 13(UI_Mask): False
   14(Screens) - 14(Screens): False
   14(Screens) - 15(Local Scenery): False
   14(Screens) - 16(kerbals): False
   14(Screens) - 17(Editor_UI): False
   14(Screens) - 18(SkySphere): False
   14(Screens) - 19(Disconnected Parts): False
   14(Screens) - 20(Internal Space): False
   14(Screens) - 21(Part Triggers): False
   14(Screens) - 22(KerbalInstructors): False
   14(Screens) - 23(ScaledSpaceSun): False
   14(Screens) - 24(MapFX): False
   14(Screens) - 25(EzGUI_UI): False
   14(Screens) - 26(WheelCollidersIgnore): False
   14(Screens) - 27(WheelColliders): False
   14(Screens) - 28(TerrainColliders): False
   14(Screens) - 29(): True
   14(Screens) - 30(): True
   15(Local Scenery) - 0(Default): True
   15(Local Scenery) - 1(TransparentFX): True
   15(Local Scenery) - 2(Ignore Raycast): True
   15(Local Scenery) - 3(): True
   15(Local Scenery) - 4(Water): True
   15(Local Scenery) - 5(UI): False
   15(Local Scenery) - 6(): True
   15(Local Scenery) - 7(): True
   15(Local Scenery) - 8(PartsList_Icons): False
   15(Local Scenery) - 9(Atmosphere): False
   15(Local Scenery) - 10(Scaled Scenery): False
   15(Local Scenery) - 11(UI_Culled): False
   15(Local Scenery) - 12(UI_Main): False
   15(Local Scenery) - 13(UI_Mask): False
   15(Local Scenery) - 14(Screens): False
   15(Local Scenery) - 15(Local Scenery): True
   15(Local Scenery) - 16(kerbals): False
   15(Local Scenery) - 17(Editor_UI): False
   15(Local Scenery) - 18(SkySphere): False
   15(Local Scenery) - 19(Disconnected Parts): True
   15(Local Scenery) - 20(Internal Space): False
   15(Local Scenery) - 21(Part Triggers): False
   15(Local Scenery) - 22(KerbalInstructors): False
   15(Local Scenery) - 23(ScaledSpaceSun): False
   15(Local Scenery) - 24(MapFX): False
   15(Local Scenery) - 25(EzGUI_UI): False
   15(Local Scenery) - 26(WheelCollidersIgnore): True
   15(Local Scenery) - 27(WheelColliders): True
   15(Local Scenery) - 28(TerrainColliders): True
   15(Local Scenery) - 29(): True
   15(Local Scenery) - 30(): True
   16(kerbals) - 0(Default): False
   16(kerbals) - 1(TransparentFX): False
   16(kerbals) - 2(Ignore Raycast): False
   16(kerbals) - 3(): True
   16(kerbals) - 4(Water): False
   16(kerbals) - 5(UI): False
   16(kerbals) - 6(): True
   16(kerbals) - 7(): True
   16(kerbals) - 8(PartsList_Icons): False
   16(kerbals) - 9(Atmosphere): False
   16(kerbals) - 10(Scaled Scenery): False
   16(kerbals) - 11(UI_Culled): False
   16(kerbals) - 12(UI_Main): False
   16(kerbals) - 13(UI_Mask): False
   16(kerbals) - 14(Screens): False
   16(kerbals) - 15(Local Scenery): False
   16(kerbals) - 16(kerbals): False
   16(kerbals) - 17(Editor_UI): False
   16(kerbals) - 18(SkySphere): False
   16(kerbals) - 19(Disconnected Parts): False
   16(kerbals) - 20(Internal Space): True
   16(kerbals) - 21(Part Triggers): False
   16(kerbals) - 22(KerbalInstructors): False
   16(kerbals) - 23(ScaledSpaceSun): False
   16(kerbals) - 24(MapFX): False
   16(kerbals) - 25(EzGUI_UI): False
   16(kerbals) - 26(WheelCollidersIgnore): False
   16(kerbals) - 27(WheelColliders): False
   16(kerbals) - 28(TerrainColliders): False
   16(kerbals) - 29(): True
   16(kerbals) - 30(): True
   17(Editor_UI) - 0(Default): False
   17(Editor_UI) - 1(TransparentFX): False
   17(Editor_UI) - 2(Ignore Raycast): False
   17(Editor_UI) - 3(): True
   17(Editor_UI) - 4(Water): False
   17(Editor_UI) - 5(UI): False
   17(Editor_UI) - 6(): True
   17(Editor_UI) - 7(): True
   17(Editor_UI) - 8(PartsList_Icons): False
   17(Editor_UI) - 9(Atmosphere): False
   17(Editor_UI) - 10(Scaled Scenery): False
   17(Editor_UI) - 11(UI_Culled): False
   17(Editor_UI) - 12(UI_Main): False
   17(Editor_UI) - 13(UI_Mask): False
   17(Editor_UI) - 14(Screens): False
   17(Editor_UI) - 15(Local Scenery): False
   17(Editor_UI) - 16(kerbals): False
   17(Editor_UI) - 17(Editor_UI): False
   17(Editor_UI) - 18(SkySphere): False
   17(Editor_UI) - 19(Disconnected Parts): False
   17(Editor_UI) - 20(Internal Space): False
   17(Editor_UI) - 21(Part Triggers): False
   17(Editor_UI) - 22(KerbalInstructors): False
   17(Editor_UI) - 23(ScaledSpaceSun): False
   17(Editor_UI) - 24(MapFX): False
   17(Editor_UI) - 25(EzGUI_UI): False
   17(Editor_UI) - 26(WheelCollidersIgnore): False
   17(Editor_UI) - 27(WheelColliders): False
   17(Editor_UI) - 28(TerrainColliders): False
   17(Editor_UI) - 29(): True
   17(Editor_UI) - 30(): True
   18(SkySphere) - 0(Default): False
   18(SkySphere) - 1(TransparentFX): False
   18(SkySphere) - 2(Ignore Raycast): False
   18(SkySphere) - 3(): True
   18(SkySphere) - 4(Water): False
   18(SkySphere) - 5(UI): False
   18(SkySphere) - 6(): True
   18(SkySphere) - 7(): True
   18(SkySphere) - 8(PartsList_Icons): False
   18(SkySphere) - 9(Atmosphere): False
   18(SkySphere) - 10(Scaled Scenery): False
   18(SkySphere) - 11(UI_Culled): False
   18(SkySphere) - 12(UI_Main): False
   18(SkySphere) - 13(UI_Mask): False
   18(SkySphere) - 14(Screens): False
   18(SkySphere) - 15(Local Scenery): False
   18(SkySphere) - 16(kerbals): False
   18(SkySphere) - 17(Editor_UI): False
   18(SkySphere) - 18(SkySphere): False
   18(SkySphere) - 19(Disconnected Parts): False
   18(SkySphere) - 20(Internal Space): False
   18(SkySphere) - 21(Part Triggers): False
   18(SkySphere) - 22(KerbalInstructors): False
   18(SkySphere) - 23(ScaledSpaceSun): False
   18(SkySphere) - 24(MapFX): False
   18(SkySphere) - 25(EzGUI_UI): False
   18(SkySphere) - 26(WheelCollidersIgnore): False
   18(SkySphere) - 27(WheelColliders): False
   18(SkySphere) - 28(TerrainColliders): False
   18(SkySphere) - 29(): True
   18(SkySphere) - 30(): True
   19(Disconnected Parts) - 0(Default): True
   19(Disconnected Parts) - 1(TransparentFX): True
   19(Disconnected Parts) - 2(Ignore Raycast): True
   19(Disconnected Parts) - 3(): True
   19(Disconnected Parts) - 4(Water): True
   19(Disconnected Parts) - 5(UI): False
   19(Disconnected Parts) - 6(): True
   19(Disconnected Parts) - 7(): True
   19(Disconnected Parts) - 8(PartsList_Icons): False
   19(Disconnected Parts) - 9(Atmosphere): False
   19(Disconnected Parts) - 10(Scaled Scenery): False
   19(Disconnected Parts) - 11(UI_Culled): False
   19(Disconnected Parts) - 12(UI_Main): False
   19(Disconnected Parts) - 13(UI_Mask): False
   19(Disconnected Parts) - 14(Screens): False
   19(Disconnected Parts) - 15(Local Scenery): True
   19(Disconnected Parts) - 16(kerbals): False
   19(Disconnected Parts) - 17(Editor_UI): False
   19(Disconnected Parts) - 18(SkySphere): False
   19(Disconnected Parts) - 19(Disconnected Parts): False
   19(Disconnected Parts) - 20(Internal Space): False
   19(Disconnected Parts) - 21(Part Triggers): False
   19(Disconnected Parts) - 22(KerbalInstructors): False
   19(Disconnected Parts) - 23(ScaledSpaceSun): False
   19(Disconnected Parts) - 24(MapFX): False
   19(Disconnected Parts) - 25(EzGUI_UI): False
   19(Disconnected Parts) - 26(WheelCollidersIgnore): True
   19(Disconnected Parts) - 27(WheelColliders): True
   19(Disconnected Parts) - 28(TerrainColliders): False
   19(Disconnected Parts) - 29(): True
   19(Disconnected Parts) - 30(): True
   20(Internal Space) - 0(Default): False
   20(Internal Space) - 1(TransparentFX): False
   20(Internal Space) - 2(Ignore Raycast): False
   20(Internal Space) - 3(): True
   20(Internal Space) - 4(Water): False
   20(Internal Space) - 5(UI): False
   20(Internal Space) - 6(): True
   20(Internal Space) - 7(): True
   20(Internal Space) - 8(PartsList_Icons): False
   20(Internal Space) - 9(Atmosphere): False
   20(Internal Space) - 10(Scaled Scenery): False
   20(Internal Space) - 11(UI_Culled): False
   20(Internal Space) - 12(UI_Main): False
   20(Internal Space) - 13(UI_Mask): False
   20(Internal Space) - 14(Screens): False
   20(Internal Space) - 15(Local Scenery): False
   20(Internal Space) - 16(kerbals): True
   20(Internal Space) - 17(Editor_UI): False
   20(Internal Space) - 18(SkySphere): False
   20(Internal Space) - 19(Disconnected Parts): False
   20(Internal Space) - 20(Internal Space): True
   20(Internal Space) - 21(Part Triggers): False
   20(Internal Space) - 22(KerbalInstructors): False
   20(Internal Space) - 23(ScaledSpaceSun): False
   20(Internal Space) - 24(MapFX): False
   20(Internal Space) - 25(EzGUI_UI): False
   20(Internal Space) - 26(WheelCollidersIgnore): False
   20(Internal Space) - 27(WheelColliders): False
   20(Internal Space) - 28(TerrainColliders): False
   20(Internal Space) - 29(): True
   20(Internal Space) - 30(): True
   21(Part Triggers) - 0(Default): False
   21(Part Triggers) - 1(TransparentFX): False
   21(Part Triggers) - 2(Ignore Raycast): False
   21(Part Triggers) - 3(): True
   21(Part Triggers) - 4(Water): False
   21(Part Triggers) - 5(UI): False
   21(Part Triggers) - 6(): True
   21(Part Triggers) - 7(): True
   21(Part Triggers) - 8(PartsList_Icons): False
   21(Part Triggers) - 9(Atmosphere): False
   21(Part Triggers) - 10(Scaled Scenery): False
   21(Part Triggers) - 11(UI_Culled): False
   21(Part Triggers) - 12(UI_Main): False
   21(Part Triggers) - 13(UI_Mask): False
   21(Part Triggers) - 14(Screens): False
   21(Part Triggers) - 15(Local Scenery): False
   21(Part Triggers) - 16(kerbals): False
   21(Part Triggers) - 17(Editor_UI): False
   21(Part Triggers) - 18(SkySphere): False
   21(Part Triggers) - 19(Disconnected Parts): False
   21(Part Triggers) - 20(Internal Space): False
   21(Part Triggers) - 21(Part Triggers): True
   21(Part Triggers) - 22(KerbalInstructors): False
   21(Part Triggers) - 23(ScaledSpaceSun): False
   21(Part Triggers) - 24(MapFX): False
   21(Part Triggers) - 25(EzGUI_UI): False
   21(Part Triggers) - 26(WheelCollidersIgnore): False
   21(Part Triggers) - 27(WheelColliders): False
   21(Part Triggers) - 28(TerrainColliders): False
   21(Part Triggers) - 29(): True
   21(Part Triggers) - 30(): True
   22(KerbalInstructors) - 0(Default): False
   22(KerbalInstructors) - 1(TransparentFX): False
   22(KerbalInstructors) - 2(Ignore Raycast): False
   22(KerbalInstructors) - 3(): True
   22(KerbalInstructors) - 4(Water): False
   22(KerbalInstructors) - 5(UI): False
   22(KerbalInstructors) - 6(): True
   22(KerbalInstructors) - 7(): True
   22(KerbalInstructors) - 8(PartsList_Icons): False
   22(KerbalInstructors) - 9(Atmosphere): False
   22(KerbalInstructors) - 10(Scaled Scenery): False
   22(KerbalInstructors) - 11(UI_Culled): False
   22(KerbalInstructors) - 12(UI_Main): False
   22(KerbalInstructors) - 13(UI_Mask): False
   22(KerbalInstructors) - 14(Screens): False
   22(KerbalInstructors) - 15(Local Scenery): False
   22(KerbalInstructors) - 16(kerbals): False
   22(KerbalInstructors) - 17(Editor_UI): False
   22(KerbalInstructors) - 18(SkySphere): False
   22(KerbalInstructors) - 19(Disconnected Parts): False
   22(KerbalInstructors) - 20(Internal Space): False
   22(KerbalInstructors) - 21(Part Triggers): False
   22(KerbalInstructors) - 22(KerbalInstructors): False
   22(KerbalInstructors) - 23(ScaledSpaceSun): False
   22(KerbalInstructors) - 24(MapFX): False
   22(KerbalInstructors) - 25(EzGUI_UI): False
   22(KerbalInstructors) - 26(WheelCollidersIgnore): False
   22(KerbalInstructors) - 27(WheelColliders): False
   22(KerbalInstructors) - 28(TerrainColliders): False
   22(KerbalInstructors) - 29(): True
   22(KerbalInstructors) - 30(): True
   23(ScaledSpaceSun) - 0(Default): False
   23(ScaledSpaceSun) - 1(TransparentFX): False
   23(ScaledSpaceSun) - 2(Ignore Raycast): False
   23(ScaledSpaceSun) - 3(): True
   23(ScaledSpaceSun) - 4(Water): False
   23(ScaledSpaceSun) - 5(UI): False
   23(ScaledSpaceSun) - 6(): True
   23(ScaledSpaceSun) - 7(): True
   23(ScaledSpaceSun) - 8(PartsList_Icons): False
   23(ScaledSpaceSun) - 9(Atmosphere): False
   23(ScaledSpaceSun) - 10(Scaled Scenery): False
   23(ScaledSpaceSun) - 11(UI_Culled): False
   23(ScaledSpaceSun) - 12(UI_Main): False
   23(ScaledSpaceSun) - 13(UI_Mask): False
   23(ScaledSpaceSun) - 14(Screens): False
   23(ScaledSpaceSun) - 15(Local Scenery): False
   23(ScaledSpaceSun) - 16(kerbals): False
   23(ScaledSpaceSun) - 17(Editor_UI): False
   23(ScaledSpaceSun) - 18(SkySphere): False
   23(ScaledSpaceSun) - 19(Disconnected Parts): False
   23(ScaledSpaceSun) - 20(Internal Space): False
   23(ScaledSpaceSun) - 21(Part Triggers): False
   23(ScaledSpaceSun) - 22(KerbalInstructors): False
   23(ScaledSpaceSun) - 23(ScaledSpaceSun): False
   23(ScaledSpaceSun) - 24(MapFX): False
   23(ScaledSpaceSun) - 25(EzGUI_UI): False
   23(ScaledSpaceSun) - 26(WheelCollidersIgnore): False
   23(ScaledSpaceSun) - 27(WheelColliders): False
   23(ScaledSpaceSun) - 28(TerrainColliders): False
   23(ScaledSpaceSun) - 29(): True
   23(ScaledSpaceSun) - 30(): True
   24(MapFX) - 0(Default): False
   24(MapFX) - 1(TransparentFX): False
   24(MapFX) - 2(Ignore Raycast): False
   24(MapFX) - 3(): True
   24(MapFX) - 4(Water): False
   24(MapFX) - 5(UI): False
   24(MapFX) - 6(): True
   24(MapFX) - 7(): True
   24(MapFX) - 8(PartsList_Icons): False
   24(MapFX) - 9(Atmosphere): False
   24(MapFX) - 10(Scaled Scenery): False
   24(MapFX) - 11(UI_Culled): False
   24(MapFX) - 12(UI_Main): False
   24(MapFX) - 13(UI_Mask): False
   24(MapFX) - 14(Screens): False
   24(MapFX) - 15(Local Scenery): False
   24(MapFX) - 16(kerbals): False
   24(MapFX) - 17(Editor_UI): False
   24(MapFX) - 18(SkySphere): False
   24(MapFX) - 19(Disconnected Parts): False
   24(MapFX) - 20(Internal Space): False
   24(MapFX) - 21(Part Triggers): False
   24(MapFX) - 22(KerbalInstructors): False
   24(MapFX) - 23(ScaledSpaceSun): False
   24(MapFX) - 24(MapFX): False
   24(MapFX) - 25(EzGUI_UI): False
   24(MapFX) - 26(WheelCollidersIgnore): False
   24(MapFX) - 27(WheelColliders): False
   24(MapFX) - 28(TerrainColliders): False
   24(MapFX) - 29(): True
   24(MapFX) - 30(): True
   25(EzGUI_UI) - 0(Default): False
   25(EzGUI_UI) - 1(TransparentFX): False
   25(EzGUI_UI) - 2(Ignore Raycast): False
   25(EzGUI_UI) - 3(): True
   25(EzGUI_UI) - 4(Water): False
   25(EzGUI_UI) - 5(UI): False
   25(EzGUI_UI) - 6(): True
   25(EzGUI_UI) - 7(): True
   25(EzGUI_UI) - 8(PartsList_Icons): False
   25(EzGUI_UI) - 9(Atmosphere): False
   25(EzGUI_UI) - 10(Scaled Scenery): False
   25(EzGUI_UI) - 11(UI_Culled): False
   25(EzGUI_UI) - 12(UI_Main): False
   25(EzGUI_UI) - 13(UI_Mask): False
   25(EzGUI_UI) - 14(Screens): False
   25(EzGUI_UI) - 15(Local Scenery): False
   25(EzGUI_UI) - 16(kerbals): False
   25(EzGUI_UI) - 17(Editor_UI): False
   25(EzGUI_UI) - 18(SkySphere): False
   25(EzGUI_UI) - 19(Disconnected Parts): False
   25(EzGUI_UI) - 20(Internal Space): False
   25(EzGUI_UI) - 21(Part Triggers): False
   25(EzGUI_UI) - 22(KerbalInstructors): False
   25(EzGUI_UI) - 23(ScaledSpaceSun): False
   25(EzGUI_UI) - 24(MapFX): False
   25(EzGUI_UI) - 25(EzGUI_UI): False
   25(EzGUI_UI) - 26(WheelCollidersIgnore): False
   25(EzGUI_UI) - 27(WheelColliders): False
   25(EzGUI_UI) - 28(TerrainColliders): False
   25(EzGUI_UI) - 29(): True
   25(EzGUI_UI) - 30(): True
   26(WheelCollidersIgnore) - 0(Default): True
   26(WheelCollidersIgnore) - 1(TransparentFX): True
   26(WheelCollidersIgnore) - 2(Ignore Raycast): True
   26(WheelCollidersIgnore) - 3(): True
   26(WheelCollidersIgnore) - 4(Water): True
   26(WheelCollidersIgnore) - 5(UI): False
   26(WheelCollidersIgnore) - 6(): True
   26(WheelCollidersIgnore) - 7(): True
   26(WheelCollidersIgnore) - 8(PartsList_Icons): False
   26(WheelCollidersIgnore) - 9(Atmosphere): False
   26(WheelCollidersIgnore) - 10(Scaled Scenery): False
   26(WheelCollidersIgnore) - 11(UI_Culled): False
   26(WheelCollidersIgnore) - 12(UI_Main): False
   26(WheelCollidersIgnore) - 13(UI_Mask): False
   26(WheelCollidersIgnore) - 14(Screens): False
   26(WheelCollidersIgnore) - 15(Local Scenery): True
   26(WheelCollidersIgnore) - 16(kerbals): False
   26(WheelCollidersIgnore) - 17(Editor_UI): False
   26(WheelCollidersIgnore) - 18(SkySphere): False
   26(WheelCollidersIgnore) - 19(Disconnected Parts): True
   26(WheelCollidersIgnore) - 20(Internal Space): False
   26(WheelCollidersIgnore) - 21(Part Triggers): False
   26(WheelCollidersIgnore) - 22(KerbalInstructors): False
   26(WheelCollidersIgnore) - 23(ScaledSpaceSun): False
   26(WheelCollidersIgnore) - 24(MapFX): False
   26(WheelCollidersIgnore) - 25(EzGUI_UI): False
   26(WheelCollidersIgnore) - 26(WheelCollidersIgnore): True
   26(WheelCollidersIgnore) - 27(WheelColliders): False
   26(WheelCollidersIgnore) - 28(TerrainColliders): False
   26(WheelCollidersIgnore) - 29(): True
   26(WheelCollidersIgnore) - 30(): True
   27(WheelColliders) - 0(Default): True
   27(WheelColliders) - 1(TransparentFX): True
   27(WheelColliders) - 2(Ignore Raycast): True
   27(WheelColliders) - 3(): True
   27(WheelColliders) - 4(Water): True
   27(WheelColliders) - 5(UI): False
   27(WheelColliders) - 6(): True
   27(WheelColliders) - 7(): True
   27(WheelColliders) - 8(PartsList_Icons): False
   27(WheelColliders) - 9(Atmosphere): False
   27(WheelColliders) - 10(Scaled Scenery): False
   27(WheelColliders) - 11(UI_Culled): False
   27(WheelColliders) - 12(UI_Main): False
   27(WheelColliders) - 13(UI_Mask): False
   27(WheelColliders) - 14(Screens): False
   27(WheelColliders) - 15(Local Scenery): True
   27(WheelColliders) - 16(kerbals): False
   27(WheelColliders) - 17(Editor_UI): False
   27(WheelColliders) - 18(SkySphere): False
   27(WheelColliders) - 19(Disconnected Parts): True
   27(WheelColliders) - 20(Internal Space): False
   27(WheelColliders) - 21(Part Triggers): False
   27(WheelColliders) - 22(KerbalInstructors): False
   27(WheelColliders) - 23(ScaledSpaceSun): False
   27(WheelColliders) - 24(MapFX): False
   27(WheelColliders) - 25(EzGUI_UI): False
   27(WheelColliders) - 26(WheelCollidersIgnore): False
   27(WheelColliders) - 27(WheelColliders): False
   27(WheelColliders) - 28(TerrainColliders): False
   27(WheelColliders) - 29(): True
   27(WheelColliders) - 30(): True
   28(TerrainColliders) - 0(Default): False
   28(TerrainColliders) - 1(TransparentFX): False
   28(TerrainColliders) - 2(Ignore Raycast): False
   28(TerrainColliders) - 3(): True
   28(TerrainColliders) - 4(Water): True
   28(TerrainColliders) - 5(UI): False
   28(TerrainColliders) - 6(): True
   28(TerrainColliders) - 7(): True
   28(TerrainColliders) - 8(PartsList_Icons): False
   28(TerrainColliders) - 9(Atmosphere): False
   28(TerrainColliders) - 10(Scaled Scenery): False
   28(TerrainColliders) - 11(UI_Culled): False
   28(TerrainColliders) - 12(UI_Main): False
   28(TerrainColliders) - 13(UI_Mask): False
   28(TerrainColliders) - 14(Screens): False
   28(TerrainColliders) - 15(Local Scenery): True
   28(TerrainColliders) - 16(kerbals): False
   28(TerrainColliders) - 17(Editor_UI): False
   28(TerrainColliders) - 18(SkySphere): False
   28(TerrainColliders) - 19(Disconnected Parts): False
   28(TerrainColliders) - 20(Internal Space): False
   28(TerrainColliders) - 21(Part Triggers): False
   28(TerrainColliders) - 22(KerbalInstructors): False
   28(TerrainColliders) - 23(ScaledSpaceSun): False
   28(TerrainColliders) - 24(MapFX): False
   28(TerrainColliders) - 25(EzGUI_UI): False
   28(TerrainColliders) - 26(WheelCollidersIgnore): False
   28(TerrainColliders) - 27(WheelColliders): False
   28(TerrainColliders) - 28(TerrainColliders): True
   28(TerrainColliders) - 29(): True
   28(TerrainColliders) - 30(): True
   29() - 0(Default): True
   29() - 1(TransparentFX): True
   29() - 2(Ignore Raycast): True
   29() - 3(): True
   29() - 4(Water): True
   29() - 5(UI): True
   29() - 6(): True
   29() - 7(): True
   29() - 8(PartsList_Icons): True
   29() - 9(Atmosphere): True
   29() - 10(Scaled Scenery): True
   29() - 11(UI_Culled): True
   29() - 12(UI_Main): True
   29() - 13(UI_Mask): True
   29() - 14(Screens): True
   29() - 15(Local Scenery): True
   29() - 16(kerbals): True
   29() - 17(Editor_UI): True
   29() - 18(SkySphere): True
   29() - 19(Disconnected Parts): True
   29() - 20(Internal Space): True
   29() - 21(Part Triggers): True
   29() - 22(KerbalInstructors): True
   29() - 23(ScaledSpaceSun): True
   29() - 24(MapFX): True
   29() - 25(EzGUI_UI): True
   29() - 26(WheelCollidersIgnore): True
   29() - 27(WheelColliders): True
   29() - 28(TerrainColliders): True
   29() - 29(): True
   29() - 30(): True
   30() - 0(Default): True
   30() - 1(TransparentFX): True
   30() - 2(Ignore Raycast): True
   30() - 3(): True
   30() - 4(Water): True
   30() - 5(UI): True
   30() - 6(): True
   30() - 7(): True
   30() - 8(PartsList_Icons): True
   30() - 9(Atmosphere): True
   30() - 10(Scaled Scenery): True
   30() - 11(UI_Culled): True
   30() - 12(UI_Main): True
   30() - 13(UI_Mask): True
   30() - 14(Screens): True
   30() - 15(Local Scenery): True
   30() - 16(kerbals): True
   30() - 17(Editor_UI): True
   30() - 18(SkySphere): True
   30() - 19(Disconnected Parts): True
   30() - 20(Internal Space): True
   30() - 21(Part Triggers): True
   30() - 22(KerbalInstructors): True
   30() - 23(ScaledSpaceSun): True
   30() - 24(MapFX): True
   30() - 25(EzGUI_UI): True
   30() - 26(WheelCollidersIgnore): True
   30() - 27(WheelColliders): True
   30() - 28(TerrainColliders): True
   30() - 29(): True
   30() - 30(): True

Cameras:
Camera ScaledSpace                      , cullmask = 00100000100001000000011000000000 = 545523200
Camera 01                               , cullmask = 00000000000010001000000000000011 = 557059
Camera 00                               , cullmask = 00000000000010001000000000000011 = 557059
KerbalFlightIndicators-CustomCamera     , cullmask = 01000000000000000000000000000000 = 1073741824
FXCamera                                , cullmask = 00000000000000000000000000000001 = 1
Camera                                  , cullmask = 00000000000000000000000000000010 = 2
UICamera                                , cullmask = 00000010000000000000000000000000 = 33554432
velocity camera                         , cullmask = 00000000000000000000000000000001 = 1
UI camera                               , cullmask = 00000000000000000001000000000000 = 4096
UI mask camera                          , cullmask = 00000000000000000
 
 created by:
		StringBuilder sb = new StringBuilder();
		sb.AppendFormat("AllLayers = {0}, {1}\n", DMDebug.ToBin(Physics.AllLayers, 32), Physics.AllLayers.ToString());
		sb.AppendFormat("DefaultRaycastLayers = {0}, {1}\n", DMDebug.ToBin(Physics.DefaultRaycastLayers, 32), Physics.DefaultRaycastLayers.ToString());
		sb.AppendFormat("IgnoreRaycastLayer = {0}, {1}\n", DMDebug.ToBin(Physics.IgnoreRaycastLayer, 32), Physics.IgnoreRaycastLayer.ToString());
		sb.AppendFormat("Collision Matrix: \n");
		for (int i=0; i<=30; ++i)
			for (int j=0; j<=30; ++j)
			{
				string namei = LayerMask.LayerToName(i);
				string namej = LayerMask.LayerToName(j);
				sb.AppendFormat("   {0}({1}) - {2}({3}): {4}\n", i.ToString(), namei, j.ToString(), namej, (!Physics.GetIgnoreLayerCollision(i, j)).ToString());
			}
		sb.Append("Cameras:\n");
		foreach (var cam in Camera.allCameras)
		{
			sb.AppendFormat("{0}, cullmask = {1} = {2}\n", cam.name,DMDebug.ToBin(cam.cullingMask, 32), cam.cullingMask.ToString());
		}
		Debug.Log(sb.ToString());
 
something else. Not needed any more.
	Quaternion BuildUpFrame(FlightCamera cam, Vector3 up)
	{
		Vector3 z;
		if (Mathf.Abs(Vector3.Dot(up, cam.transform.right)) < 0.9999)
		{
			z = Vector3.Cross(cam.transform.right, up); // z is in the cameras y-z plane
			z.Normalize();
		}
		else // up_in_cam_frame is parallel to x
		{
			z = cam.transform.forward;
		}
		return Quaternion.LookRotation(z, up);
	}
*/

	#endregion

	#region Marker Implementation

	public enum Markers
	{
		Heading = 0,
		Prograde,
		Retrograde,
		Reverse,
		LevelGuide,
		Vertical,
		Horizon,
		COUNT
	}


	public class MarkerScript : MonoBehaviour
	{
		public Color baseColor;
		private float blendC0; // fully visible
		private float blendC1; // invisible
		private float blendCM;
		public float blendValue;
		private bool doBlending;
		private Renderer renderer;

		public void SetBlendConstants(float blendC0_, float blendC1_)
		{
			blendC0 = blendC0_;
			blendC1 = blendC1_;
			blendCM = 1.0f / (blendC1 - blendC0);
			blendValue = blendC1;
			doBlending = true;
		}

		public void Start()
		{
			renderer = GetComponent<Renderer>();
			renderer.material.color = baseColor;
		}

		public void OnWillRenderObject()
		{
			if (doBlending) {
				var alpha = 1.0f - Mathf.Clamp01((blendValue - blendC0) * blendCM);
				var c = baseColor;
				c.a *= alpha;
				renderer.material.color = c;
			}
		}
	}


	public class CameraScript : MonoBehaviour
	{
		private const float speed_draw_threshold = 1.0e-1f;
		private Camera cam; // through which the player sees the world
		private Vector3 east = Vector3.zero;
		private Quaternion last_qvessel = Quaternion.identity; // for filtering
		private Vector3 last_speed = Vector3.zero; // for filtering

		public bool[] markerEnabling;
		public MarkerScript[] markerScripts;
		private Camera my_indicator_cam; // that is used to render our indicator objects.

		private Vector3 north = Vector3.zero;

		/* cached information about the vessel and the world.
		 * Most of teh calculations are done in the draw pass
		 * because i want to make sure the camera is up to date */
		private Quaternion qvessel = Quaternion.identity;
		private Vector3 ship_up = Util.z;
		private Vector3 speed = Vector3.zero;


		public void Start()
		{
			my_indicator_cam = GetComponent<Camera>();
		}

		private void OnPreCull() // this is only called if the MonoBehaviour component is attached to a GameObject which also has a Camera!
		{
#if false
		if (Input.GetKeyDown(KeyCode.O))
		{
			var g = FlightGlobals.fetch;
			StringBuilder sb = new StringBuilder(8);
			sb.AppendLine("ship_upAxis             = "+((Vector3)FlightGlobals.ship_upAxis).ToString("F3"));
			sb.AppendLine("upAxis                  = "+((Vector3)FlightGlobals.upAxis).ToString("F3"));
			sb.AppendLine("getUpAxis                  = "+((Vector3)FlightGlobals.getUpAxis()).ToString("F3"));
			sb.AppendLine("ship_srfVelocity        = "+((Vector3)FlightGlobals.ship_srfVelocity).ToString("F3"));
			sb.AppendLine("GetSpeedVector()        = "+GetSpeedVector().ToString("F3"));
			sb.AppendLine("ship_rotation           = "+DMDebug.DebugRepr(FlightGlobals.ship_rotation));
			sb.AppendLine("ship_orientation        = "+DMDebug.DebugRepr(FlightGlobals.ship_orientation));
			sb.AppendLine("ship_orientation_offset = "+DMDebug.DebugRepr(FlightGlobals.ship_orientation_offset));
			Debug.Log(sb.ToString());
		}
#endif

#if false
		if (Input.GetKeyDown(KeyCode.O))
		{
			StringBuilder sb = new StringBuilder(8);
			sb.AppendLine("DMHUD:");
			sb.AppendLine("       up_in_cam_frame = " + up_in_cam_frame.ToString("F3"));
			sb.AppendLine("       speed_in_cam_frame = " + speed_in_cam_frame.ToString("F3"));
			sb.AppendLine("       heading_in_cam_frame = " + heading_in_cam_frame.ToString("F3"));
			sb.AppendLine("       qvessel = " + (qhorizoninv*qvessel).ToString("F3"));
			sb.AppendLine("       qhorizon = " + qhorizon.ToString("F3"));
			sb.AppendLine("       euler    = " + vessel_euler.ToString("F3"));
			sb.AppendLine("       heading  = " + (qhorizoninv * heading).ToString("F2"));
			sb.AppendLine("       horizon_roll_z = " + (qcaminv * qhorizon * Util.z).ToString("F2"));
			Debug.Log(sb.ToString());
		}
#endif

			for (var i = 0; i < (int) Markers.COUNT; ++i) {
				markerEnabling[i] = false;
			}

			if (CheckAndPrepare()) {
				UpdateAllMarkers();
			}

			for (var i = 0; i < (int) Markers.COUNT; ++i) {
				markerScripts[i].gameObject.SetActive(markerEnabling[i]);
			}
		}


		private bool CheckAndPrepare()
		{
			if (!FlightGlobals.ready) return false;

			var vessel = FlightGlobals.ActiveVessel;
			if (vessel == null) return false;

			if (vessel.state == Vessel.State.DEAD) {
				return false;
			}

			cam = FlightGlobals.fetch.mainCameraRef;
			if (cam == null) return false;

			if (!IsInAdmissibleCameraMode()) {
				return false;
			}

			// simple running average filtering
			var current_speed = GetSpeedVector();
			var current_qvessel = FlightGlobals.ship_rotation;
			speed = 0.5f * (current_speed + last_speed);
			qvessel = Quaternion.Lerp(current_qvessel, last_qvessel, 0.5f);
			last_speed = current_speed;
			last_qvessel = current_qvessel;

			ship_up = FlightGlobals.ship_upAxis;
			east = vessel.east;
			north = vessel.north;
			return true;
		}


		private void UpdateMarker(Markers id, Vector3 position, Vector3 up_vector, float blend_value)
		{
			// alpha = 0 is transparent
			// position is in camera space
			var ms = markerScripts[(int) id];
			position.x *= -my_indicator_cam.aspect * my_indicator_cam.orthographicSize;
			position.y *= -my_indicator_cam.orthographicSize;
			position.z = ms.transform.localPosition.z;
			ms.transform.localPosition = position;
			ms.blendValue = blend_value;
			var nrm = Mathf.Sqrt(up_vector.x * up_vector.x + up_vector.y * up_vector.y);
			var cs = up_vector.y / nrm;
			var sn = -up_vector.x / nrm;
			Quaternion q;
			q.x = q.y = 0;
			if (cs < 0.9999999999) {
				q.z = Mathf.Sqrt((1 - cs) * 0.5f);
				q.w = sn / q.z * 0.5f;
			} else // identity
			{
				q.z = 0.0f;
				q.w = 1.0f;
			}

			ms.transform.localRotation = q;
			markerEnabling[(int) id] = true;
		}


		private void UpdateAllMarkers()
		{
			var normalized_speed = speed.normalized;
			var is_moving = speed.sqrMagnitude > speed_draw_threshold * speed_draw_threshold;

			var qcam = cam.transform.rotation;
			var qcaminv = Quaternion.Inverse(qcam);
			var vessel_to_cam = qcaminv * qvessel;

			var cf_vertical = qcaminv * ship_up;
			var cf_up = -(vessel_to_cam * Util.z);
			var cf_heading = vessel_to_cam * Util.y;
			var cf_velocity_direction = qcaminv * normalized_speed;
			var cf_hproj = cf_heading - Vector3.Project(cf_heading, cf_vertical); // the projection of the heading onto the trangent plane of upAxis
			var cf_north = qcaminv * north;

			var heading_dot_up = Vector3.Dot(cf_vertical, cf_heading);
			var speed_dot_up = Vector3.Dot(cf_vertical, cf_velocity_direction);

			var tmp = Vector3.Cross(cf_vertical, cf_heading);
			var cf_horizon_up_vector = Vector3.Cross(cf_heading, tmp);

			var blend_vertical = Mathf.Max(Mathf.Abs(heading_dot_up), is_moving ? Mathf.Abs(speed_dot_up) : 0f);
			var blend_horizon = Mathf.Min(Mathf.Abs(heading_dot_up), is_moving ? Mathf.Abs(speed_dot_up) : 0f);
			var blend_level_guide = Mathf.Abs(heading_dot_up);

			{
				var screen_position = Util.PerspectiveProjection(cam, cf_hproj);
				if (CheckPosition(cam, screen_position)) // possible optimization: i think this check can be made before screen space projection
				{
					UpdateMarker(Markers.Horizon, screen_position, cf_vertical, blend_horizon);
				}
			}

			{
				var screen_position = Util.PerspectiveProjection(cam, cf_vertical);
				if (CheckPosition(cam, screen_position)) {
					UpdateMarker(Markers.Vertical, screen_position, cf_north, blend_vertical);
				}
			}

			if (is_moving) {
				var screen_pos = Util.PerspectiveProjection(cam, cf_velocity_direction);
				if (CheckPosition(cam, screen_pos)) {
					if (cf_velocity_direction.z >= 0) {
						UpdateMarker(Markers.Prograde, screen_pos, Util.y, 1.0f);
					} else {
						UpdateMarker(Markers.Retrograde, screen_pos, Util.y, 1.0f);
					}
				}
			}

			{
				var screen_pos = Util.PerspectiveProjection(cam, cf_heading);
				if (CheckPosition(cam, screen_pos)) {
					if (cf_heading.z >= 0) {
						UpdateMarker(Markers.Heading, screen_pos, cf_up, 1.0f);
					} else {
						UpdateMarker(Markers.Reverse, screen_pos, cf_up, 1.0f);
					}

					UpdateMarker(Markers.LevelGuide, screen_pos, cf_horizon_up_vector, blend_level_guide);
				}
			}
		}

		/*  from Steam Gauges */
		public static Vector3 GetSpeedVector()
		{
			var vessel = FlightGlobals.ActiveVessel; // must not be null. Check before calling.
			// Target velocity correction
			var tar = FlightGlobals.fetch.VesselTarget;
			Vector3 tgt_velocity = FlightGlobals.ship_tgtVelocity;
			if (tar != null && tar.GetVessel() != null) {
				// Otherwise it seems to be equal to orbital velocity when the target
				// vessel isn't loaded (i.e. more than 2km away), which makes no sense.
				// I consider this as a bug in the stock nav-ball functionality.
				var target_vessel = tar.GetVessel();
				if (target_vessel.LandedOrSplashed) {
					tgt_velocity = vessel.GetSrfVelocity();
					if (target_vessel.loaded) {
						tgt_velocity -= tar.GetSrfVelocity();
					}
				}
			}

			var speed = Vector3.zero;
			switch (FlightGlobals.speedDisplayMode) {
				case FlightGlobals.SpeedDisplayModes.Orbit:
					speed = vessel.obt_velocity;
					break;

				case FlightGlobals.SpeedDisplayModes.Target:
					speed = tgt_velocity;
					break;

				default:
					speed = vessel.GetSrfVelocity();
					break;
			}

			return speed;
		}
		/* end of Steam Gauges code */

		private bool CheckPosition(Camera cam, Vector3 q)
		{
			if (float.IsInfinity(q.x) || float.IsNaN(q.x) || q.x < -1.5f || q.x > 1.5f) return false;
			if (float.IsInfinity(q.y) || float.IsNaN(q.y) || q.y < -1.5f || q.y > 1.5f) return false;
			return true;
		}

		private bool IsInAdmissibleCameraMode()
		{
			switch (CameraManager.Instance.currentCameraMode) {
				case CameraManager.CameraMode.Flight:
				case CameraManager.CameraMode.Internal:
				case CameraManager.CameraMode.IVA:
					return true;
			}

			return false;
		}
	}

	#endregion


	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KerbalFlightIndicators : PluginWithToolbarSupport
	{
		private const int KFI_LAYER = 30; // I claim this layer just for me!

		private readonly RectOffset[] atlas_px =
		{
			new RectOffset(0 * 2, 32 * 2, 32 * 2, 64 * 2), // heading
			new RectOffset(32 * 2, 64 * 2, 32 * 2, 64 * 2), // prograde
			new RectOffset(64 * 2, 96 * 2, 32 * 2, 64 * 2), // retrograde
			new RectOffset(96 * 2, 128 * 2, 32 * 2, 64 * 2), // reverse heading
			new RectOffset(128 * 2, 192 * 2, 32 * 2, 64 * 2), // wings level guide
			new RectOffset((208 - 2) * 2, (256 - 2) * 2, (16 - 2) * 2, (64 - 2) * 2), // vertical
			new RectOffset(0 * 2, 256 * 2, 6 * 2, 10 * 2) // horizon
		};

		private Rect[] atlas_uv;
		private string atlasTexture = "atlas.png";
		private Color attitudeColor = Color.green;
		private CameraScript cameraScript;
		private float displayScaleFactor = 1.0f;
		private bool drawInFrontOfCockpit = true;

		private Color horizonColor = Color.green;
		private Texture2D marker_atlas;
		private bool[] markerEnabling;
		private GameObject markerParentObject;

		private MarkerScript[] markerScripts;
		private Color progradeColor = Color.green;

		protected override ToolbarInfo GetToolbarInfo()
		{
			return new ToolbarInfo
			{
				name = "KerbalFlightIndicators",
				tooltip = "KerbalFlightIndicators On/Off Switch",
				toolbarTexture = "KerbalFlightIndicators/toolbarbutton",
				launcherTexture = "KerbalFlightIndicators/icon",
				visibleInScenes = new[] {GameScenes.FLIGHT}
			};
		}

		// see http://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
		// Called when the script instance is being loaded.
		private void Awake()
		{
			LoadSettings();
			InitializeToolbars();
		}


		// see http://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
		// Maybe called once, on the first frame where the script is enabled. If the script is never enabled, Start won't be called.
		private void Start()
		{
			marker_atlas = Util.LoadTexture(atlasTexture);
			atlas_uv = Util.ComputeTexCoords(marker_atlas.width, marker_atlas.height, atlas_px);
			AdjustPhysics();
			CreateGameObjects();
		}


		public void OnDestroy()
		{
			SaveSettings();
			// well we probably need this to not create a memory leak or so ...
			DestroyGameObjects();
			TearDownToolbars();
		}

		public void SaveSettings()
		{
			var settings = new ConfigNode();
			settings.name = "SETTINGS";
			SaveMutableToolbarSettings(settings);
			settings.Save(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + "/state.cfg");
		}


		public void LoadSettings()
		{
			var settings = new ConfigNode();
			// load mutable configuration
			settings = ConfigNode.Load(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + "/state.cfg");
			if (settings != null) {
				LoadMutableToolbarSettings(settings);
			}

			// load configuration that won't be changed in game
			settings = ConfigNode.Load(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + "/settings.cfg");
			if (settings != null) {
				string[] markerNames =
				{
					"Heading",
					"Prograde",
					"Retrograde",
					"Reverse",
					"LevelGuide",
					"Vertical",
					"Horizon"
				};
				for (var i = 0; i < (int) Markers.COUNT; ++i) {
					if (settings.HasValue("rect" + markerNames[i])) {
						atlas_px[i] = Util.RectOffsetFromString(settings.GetValue("rect" + markerNames[i]));
					}
				}

				LoadImmutableToolbarSettings(settings);
				settings.TryGetValue("displayScaleFactor", ref displayScaleFactor);
				settings.TryGetValue("atlasTexture", ref atlasTexture);
				if (settings.HasValue("horizonColor")) horizonColor = Util.ColorFromString(settings.GetValue("horizonColor"));
				if (settings.HasValue("progradeColor")) progradeColor = Util.ColorFromString(settings.GetValue("progradeColor"));
				if (settings.HasValue("attitudeColor")) attitudeColor = Util.ColorFromString(settings.GetValue("attitudeColor"));
				if (settings.HasValue("drawInFrontOfCockpit")) drawInFrontOfCockpit = bool.Parse(settings.GetValue("drawInFrontOfCockpit"));
			}
		}


		protected override void OnGuiVisibilityChange()
		{
			var enabled_ = isGuiVisible;
			enabled = enabled_;
			if (cameraScript) cameraScript.gameObject.SetActive(enabled_);
			if (markerParentObject) markerParentObject.SetActive(enabled_);
		}


		private void AdjustPhysics()
		{
			// DON'T WANT OUR GUI ELEMENTS COLLIDING WITH STUFF!
			for (var i = 0; i < 30; ++i) {
				Physics.IgnoreLayerCollision(KFI_LAYER, i, true); // ignore = true
			}

			// Colliders are removed, too.
			// Is there anything else???
		}


		private void CreateGameObjects()
		{
			const float BLEND_HORIZON_C0 = 0.259f;
			const float BLEND_HORIZON_C1 = 0.342f;
			const float BLEND_VERT_C0 = 0.966f;
			const float BLEND_VERT_C1 = 0.94f;
			const float BLEND_LEVEL_GUIDE_C0 = 0.984f;
			const float BLEND_LEVEL_GUIDE_C1 = 0.996f;

			var viewportSizeX = Screen.width * 0.5f;
			var viewportSizeY = Screen.height * 0.5f;

			{
				var o = new GameObject("KerbalFlightIndicators-CustomCamera");
				var cam = o.AddComponent<Camera>();
				cam.orthographic = true;
				cam.clearFlags = CameraClearFlags.Nothing;
				cam.orthographicSize = viewportSizeY;
				cam.aspect = viewportSizeX / viewportSizeY;
				cam.cullingMask = 1 << KFI_LAYER;
				cam.depth = drawInFrontOfCockpit ? 10 : 1;
				cam.farClipPlane = 2.0f;
				cam.nearClipPlane = 0.5f;
				o.transform.localPosition = new Vector3(0.5f, 0.5f, 0.0f);
				o.transform.localRotation = Quaternion.identity;
				var cs = cameraScript = o.AddComponent<CameraScript>();
			}

			{
				var shd = Shader.Find("KSP/Alpha/Unlit Transparent");

				var o = markerParentObject = new GameObject("KerbalFlightIndicators-MarkerParent");

				markerEnabling = cameraScript.markerEnabling = new bool[(int) Markers.COUNT];
				markerScripts = cameraScript.markerScripts = new MarkerScript[(int) Markers.COUNT];
				for (var i = 0; i < (int) Markers.COUNT; ++i) {
					var mat = new Material(shd);
					mat.mainTexture = marker_atlas;
					var uv = atlas_uv[i];
					var px = atlas_px[i];
					mat.mainTextureScale = new Vector2(uv.width, uv.height);
					mat.mainTextureOffset = new Vector2(uv.xMin, uv.yMin);

					o = GameObject.CreatePrimitive(PrimitiveType.Quad);
					o.name = "KerbalFlightIndicators-" + Enum.GetName(typeof(Markers), i);

					Component[] colliders = o.GetComponents<Collider>();
					foreach (var c in colliders) {
						DestroyImmediate(c); // DON'T WANT OUR GUI ELEMENTS COLLIDING WITH STUFF!
					}

					var mr = o.GetComponent<MeshRenderer>();
					mr.material = mat;
					o.layer = KFI_LAYER;

					var zlevel = 1.0f;
					var ms = markerScripts[i] = o.AddComponent<MarkerScript>();
					if (i == (int) Markers.Heading || i == (int) Markers.LevelGuide || i == (int) Markers.Reverse) {
						ms.baseColor = attitudeColor;
					} else if (i == (int) Markers.Prograde || i == (int) Markers.Retrograde) {
						ms.baseColor = progradeColor;
						zlevel = 1.1f;
					} else {
						ms.baseColor = horizonColor;
						zlevel = 1.2f;
					}

					if (i == (int) Markers.Horizon) {
						ms.SetBlendConstants(BLEND_HORIZON_C0, BLEND_HORIZON_C1);
					} else if (i == (int) Markers.Vertical) {
						ms.SetBlendConstants(BLEND_VERT_C0, BLEND_VERT_C1);
					} else if (i == (int) Markers.LevelGuide) {
						ms.SetBlendConstants(BLEND_LEVEL_GUIDE_C0, BLEND_LEVEL_GUIDE_C1);
					}

					o.transform.parent = markerParentObject.transform;
					o.transform.localPosition = new Vector3(0.0f, 0.0f, zlevel);
					o.transform.localRotation = Quaternion.identity;
					o.transform.localScale = new Vector3((px.right - px.left) * displayScaleFactor, (px.bottom - px.top) * displayScaleFactor, 1.0f);

					markerEnabling[i] = true;
				}
			}
			OnGuiVisibilityChange();
		}


		private void DestroyGameObjects()
		{
			if (cameraScript != null && cameraScript.gameObject != null) // could it be that they are deleted somewhere outside this plugin? I suppose because i got a NRE here ...
			{
				Destroy(cameraScript.gameObject);
			}

			if (markerParentObject != null) {
				Destroy(markerParentObject); // destroys children, too
			}

			cameraScript = null;
			markerScripts = null;
			markerParentObject = null;
		}
	}
}