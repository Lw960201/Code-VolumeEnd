#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ArtToolKit
{
	public class FileSizeDebug
	{
		private static long sum;
		private static double result;
		private static string text;
		static string[] ns = {"Byte", "KB", "MB", "GB", "TB"};

		public static string FileSizeLog()
		{
			string resultStr = String.Empty;
			
			for (int i = 0; i < Selection.objects.Length; i++)
			{
				object obj = Selection.objects[i];
				if (obj is Texture)
				{
					Texture target = obj as Texture;
					var type = System.Reflection.Assembly.Load("UnityEditor.dll").GetType("UnityEditor.TextureUtil");
					MethodInfo methodInfo = type.GetMethod("GetStorageMemorySize",
						BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
					text = EditorUtility.FormatBytes((int) methodInfo.Invoke(null, new object[] {target}));
					GetAllSize(text);
				}
			}

			resultStr ="所选贴图的总硬盘大小：" + ConvertFileSize(sum); //转为单位制

			sum = 0;
			result = 0;
			text = String.Empty;

			return resultStr;
		}

		/// <summary>
		/// 转为Byte
		/// </summary>
		/// <param name="sizeStr"></param>
		/// <returns></returns>
		private static long GetAllSize(string sizeStr)
		{
			string numText = Regex.Replace(sizeStr, @"[^\d.\d]", "");
			string charText = Regex.Replace(sizeStr, @"\d", "");
			charText = System.Text.RegularExpressions.Regex.Replace(charText, "[. ]", "");
			if (charText.Equals(ns[0]))
			{
				result = double.Parse(numText);
			}
			else if (charText.Equals(ns[1]))
			{
				result = double.Parse(numText);
				result *= 1024;
			}
			else if (charText.Equals(ns[2]))
			{
				result = double.Parse(numText);
				result *= 1024 * 1024;
			}

			sum += (long) result;
			return sum;
		}

		/// <summary>
		/// 将文件大小(字节)转换为最适合的显示方式
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static string ConvertFileSize(long size)
		{
			string result = "0KB";
			int filelength = size.ToString().Length;
			if (filelength < 4)
				result = size + ns[0];
			else if (filelength < 7)
				result = Math.Round(Convert.ToDouble(size / 1024d), 2) + ns[1];
			else if (filelength < 10)
				result = Math.Round(Convert.ToDouble(size / 1024d / 1024), 2) + ns[2];
			else if (filelength < 13)
				result = Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024), 2) + ns[3];
			else
				result = Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024), 2) + ns[4];
			return result;
		}
	}
}
#endif