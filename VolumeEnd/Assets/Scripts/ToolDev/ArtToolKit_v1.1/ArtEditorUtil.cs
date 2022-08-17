#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
	public enum SelectionType
	{
		Folder,
		Obj,
	}

	public enum CheckSceneType
	{
		CurScene, //当前场景
		CustomScene, //自定义场景，可以多选想检查的场景
	}

	public enum AssetType
	{
		All,
		Texture,
		Model,
		Prefab,
		Scene,
	}


	public static class Type
	{
		public static GUIContent docUrl = new GUIContent("使用文档");
	}

	/// <summary>
	/// 美术工具箱常用方法
	/// </summary>
	public class ArtEditorUtil
	{
		/// <summary>
		/// 清除某一文路径下的材质的所有贴图引用
		/// </summary>
		/// <param name="findPath"></param>
		public static void ClearMatAllTextureRef(string findPath, SelectionType selectionType)
		{
			List<string> paths = GetPaths(findPath, selectionType, "Material");

			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
				var props = mat.GetTexturePropertyNames();
				for (int j = 0; j < props.Length; j++)
				{
					var prop = props[j];
					if (mat.GetTexture(prop))
					{
						mat.SetTexture(prop, null);
					}
				}
			}
		}


		#region 其他

		public static void ClearMatGomRef(string findPath, SelectionType selectionType, bool isCheck = false)
		{
			List<Material> hasRefMats = new List<Material>();

			List<string> paths = GetPaths(findPath, selectionType, "Material");

			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (mat.shader.name.Contains("Lit"))
				{
					var props = mat.GetTexturePropertyNames();
					for (int j = 0; j < props.Length; j++)
					{
						var prop = props[j];
						if (prop == "_MetallicGlossMap")
						{
							if (mat.GetTexture(prop))
							{
								if (isCheck)
								{
									hasRefMats.Add(mat);
								}
								else
								{
									mat.SetTexture(prop, null);
								}
							}
						}
					}
				}
			}

			if (isCheck)
			{
				Selection.objects = null;
				Selection.objects = hasRefMats.ToArray();
			}
		}

		private static List<string> GetPaths(string findPath, SelectionType selectionType, string filter)
		{
			List<string> paths = new List<string>();
			if (selectionType == SelectionType.Folder)
			{
				paths.AddRange(GetFoldObjPaths(findPath, filter));
			}
			else if (selectionType == SelectionType.Obj)
			{
				paths.AddRange(GetSelectObjPaths());
			}

			return paths;
		}

		#endregion

		/// <summary>
		/// 显示弹出窗口信息
		/// </summary>
		/// <param name="message"></param>
		public static void ShowTips(string message)
		{
			EditorUtility.DisplayDialog("提示", message, "OK");
		}

		/// <summary>
		/// 文档按钮
		/// </summary>
		/// <param name="docUrl"></param>
		public static void DocButton(string docUrl)
		{
			if (GUILayout.Button(Type.docUrl))
			{
				Help.BrowseURL(docUrl);
			}
		}

		#region BatchRenameSuffix

		/// <summary>
		/// 批量修改资源后缀
		/// </summary>
		/// <param name="findPath">查找路径</param>
		/// <param name="srcSuffix">源后缀</param>
		/// <param name="destSuffix">目标后缀</param>
		/// <param name="assetType">资源类型</param>
		public static void ReplaceSuffixs(string findPath, string srcSuffix, string destSuffix, AssetType assetType)
		{
			List<string> paths = GetFoldObjPaths(findPath);
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				ReplaceSuffix(srcSuffix, destSuffix, exs[i], paths);
			}
		}

		/// <summary>
		/// 添加后缀
		/// </summary>
		/// <param name="findPath">查找路径</param>
		/// <param name="destSuffix">目标后缀</param>
		/// <param name="assetType">资源类型</param>
		public static void AddSuffixs(string findPath, string destSuffix, AssetType assetType)
		{
			List<string> paths = GetFoldObjPaths(findPath);
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				AddSuffix(destSuffix, exs[i], paths);
			}
		}

		/// <summary>
		/// 选中文件夹，添加特效后缀
		/// </summary>
		/// <param name="findPath"></param>
		/// <param name="destSuffix"></param>
		/// <param name="assetType"></param>
		public static void AddFxSuffixs(string findPath, string destSuffix, AssetType assetType)
		{
			List<string> paths = GetFoldObjPaths(findPath);
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				AddFxSuffix(destSuffix, exs[i], paths);
			}
		}

		/// <summary>
		/// 选中对象，添加特效后缀
		/// </summary>
		/// <param name="destSuffix"></param>
		/// <param name="assetType"></param>
		public static void AddFxSuffixs(string destSuffix, AssetType assetType)
		{
			var paths = GetSelectObjPaths();
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				AddFxSuffix(destSuffix, exs[i], paths);
			}
		}


		/// <summary>
		/// 根据选择的对象，进行后缀替换
		/// </summary>
		/// <param name="srcSuffix"></param>
		/// <param name="destSuffix"></param>
		/// <param name="assetEx"></param>
		public static void ReplaceSuffixs(string srcSuffix, string destSuffix, AssetType assetType)
		{
			var paths = GetSelectObjPaths();
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				ReplaceSuffix(srcSuffix, destSuffix, exs[i], paths);
			}
		}

		/// <summary>
		/// 添加后缀
		/// </summary>
		/// <param name="findPath">查找路径</param>
		/// <param name="destSuffix">目标后缀</param>
		/// <param name="assetType">资源类型</param>
		public static void AddSuffixs(string destSuffix, AssetType assetType)
		{
			var paths = GetSelectObjPaths();
			var exs = GetEx(assetType);
			for (int i = 0; i < exs.Count; i++)
			{
				AddSuffix(destSuffix, exs[i], paths);
			}
		}

		/// <summary>
		/// 获取资源扩展名
		/// </summary>
		/// <param name="assetType">资源类型</param>
		/// <returns></returns>
		private static List<string> GetEx(AssetType assetType)
		{
			List<string> result = new List<string>();

			if (assetType == AssetType.Model)
			{
				result.Add("FBX");
				result.Add("fbx");
			}
			else if (assetType == AssetType.Texture)
			{
				result.Add("png");
				result.Add("PNG");

				result.Add("tga");
				result.Add("TGA");

				result.Add("jpg");
				result.Add("JPG");
			}
			else if (assetType == AssetType.Prefab)
			{
				result.Add("prefab");
			}
			else if (assetType == AssetType.Scene)
			{
				result.Add("unity");
			}
			else
			{
				result.Add("");
			}

			return result;
		}

		private static void ReplaceSuffix(string srcSuffix, string destSuffix, string assetEx, List<string> paths)
		{
			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				var fileName = Path.GetFileNameWithoutExtension(path);
				var fileNameEx = Path.GetExtension(path);

				if (!fileNameEx.Equals("." + assetEx) && !String.IsNullOrEmpty(assetEx))
				{
					continue;
				}

				if (fileName.EndsWith(srcSuffix))
				{
					var strs = path.Split('/');
					strs[strs.Length - 1] = strs[strs.Length - 1].Replace(srcSuffix, destSuffix);
					var newPath = String.Join("/", strs);
					string newName = Path.GetFileNameWithoutExtension(newPath);
					AssetDatabase.RenameAsset(path, newName);
				}
			}
		}

		private static void AddSuffix(string destSuffix, string assetEx, List<string> paths)
		{
			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				var fileName = Path.GetFileNameWithoutExtension(path);
				var fileNameEx = Path.GetExtension(path);

				if (!fileNameEx.Equals("." + assetEx) && !String.IsNullOrEmpty(assetEx))
				{
					continue;
				}

				if (fileName.EndsWith(destSuffix))
				{
					continue;
				}

				var strs = path.Split('/');
				strs[strs.Length - 1] = Path.GetFileNameWithoutExtension(strs[strs.Length - 1]) + destSuffix;
				var newPath = String.Join("/", strs);
				string newName = Path.GetFileNameWithoutExtension(newPath);
				AssetDatabase.RenameAsset(path, newName);
			}
		}

		private static void AddFxSuffix(string destSuffix, string assetEx, List<string> paths)
		{
			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				var fileName = Path.GetFileNameWithoutExtension(path);
				var fileNameEx = Path.GetExtension(path);

				if (!fileNameEx.Equals("." + assetEx) && !String.IsNullOrEmpty(assetEx))
				{
					continue;
				}

				if (fileName.EndsWith(destSuffix))
				{
					continue;
				}

				var strs = path.Split('/');
				strs[strs.Length - 1] = Path.GetFileNameWithoutExtension(strs[strs.Length - 1]) + destSuffix;
				var newPath = String.Join("/", strs);
				string newName = Path.GetFileNameWithoutExtension(newPath);
				AssetDatabase.RenameAsset(path, newName);
			}
		}

		/// <summary>
		/// 获取所有文件夹中的对象路径
		/// </summary>
		/// <param name="findPath"></param>
		/// <returns></returns>
		public static List<string> GetFoldObjPaths(string findPath)
		{
			List<string> paths = new List<string>();

			var guids = AssetDatabase.FindAssets("t:Object", new[] {findPath});
			for (int i = 0; i < guids.Length; i++)
			{
				var guid = guids[i];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				paths.Add(path);
			}

			return paths;
		}

		/// <summary>
		/// 获取所有文件夹中的对象路径
		/// </summary>
		/// <param name="findPath"></param>
		/// <returns></returns>
		public static List<string> GetFoldObjPaths(string findPath, string filter)
		{
			List<string> paths = new List<string>();

			var guids = AssetDatabase.FindAssets($"t:{filter}", new[] {findPath});
			for (int i = 0; i < guids.Length; i++)
			{
				var guid = guids[i];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				paths.Add(path);
			}

			return paths;
		}

		/// <summary>
		/// 获取所有选择对象的路径
		/// </summary>
		/// <returns></returns>
		public static List<string> GetSelectObjPaths()
		{
			List<string> paths = new List<string>();

			var objs = Selection.objects;
			for (int i = 0; i < objs.Length; i++)
			{
				var obj = objs[i];
				var path = AssetDatabase.GetAssetPath(obj);
				paths.Add(path);
			}

			return paths;
		}

		#endregion

		public static string RefreshFindPath(string path)
		{
			return AssetDatabase.GUIDToAssetPath(path);
		}

		/// <summary>
		/// 绘制查找路径
		/// </summary>
		public static void DrawFindPath(GUIContent findPathStr, ref string findPath)
		{
			using (var z = new EditorGUILayout.HorizontalScope("Button"))
			{
				findPath = EditorGUILayout.TextField(findPathStr, findPath);
				if (GUILayout.Button("更新路径"))
				{
					findPath = RefreshFindPath(Selection.assetGUIDs[0]);
				}
			}
		}


		#region 添加特效后缀
		

		public static string[] fxSuffixType =
		{
			"C",
			"AC",
			"R",
			"AR"
		};
		
		public static int[] sizeTyp = {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384};


		public static List<string> FxSuffixsCheck(string findPath)
		{
			List<string> paths = new List<string>();

			paths = GetFoldObjPaths(findPath);

			return FxSuffixCheck(paths);
		}

		private static List<string> FxSuffixCheck(List<string> paths)
		{
			List<string> resultPaths = new List<string>();
			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];

				var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (obj is Texture)
				{
					Texture texture = obj as Texture;

					for (int j = 0; j < fxSuffixType.Length; j++)
					{
						if (texture.name.Contains("_" + fxSuffixType[j] + "_")) //包含特效后缀
						{
							if (FxImporterCheck(path, fxSuffixType[j]) != null)
							{
								resultPaths.Add(FxImporterCheck(path, fxSuffixType[j]));
							}
						}
					}
				}
			}

			return resultPaths;
		}
		
		public static List<string> ResolutionSuffixCheck(string findPath)
		{
			List<string> paths = new List<string>();

			paths = GetFoldObjPaths(findPath);
			
			bool isContainsResolution = true;
			List<string> resultPaths = new List<string>();
			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];

				var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (obj is Texture)
				{
					Texture texture = obj as Texture;

					for (int k = 0; k < sizeTyp.Length; k++)
					{
						var type = sizeTyp[k];
						if (texture.name.EndsWith("_"+type)) //包含分辨率后缀
						{
							isContainsResolution = true;
							break;
						}
						else
						{
							isContainsResolution = false;
						}
					}
					
					if (!isContainsResolution)
					{
						if (!resultPaths.Contains(path))
						{
							resultPaths.Add(path);
						}
					}
				}
			}

			return resultPaths;
		}


		private static string FxImporterCheck(string path, string fxSuffix)
		{
			AssetImporter importer = AssetImporter.GetAtPath(path);
			TextureImporter textureImporter = importer as TextureImporter;
			if (!IsFxSuffixImporterParmSame(fxSuffix, textureImporter,textureImporter.alphaSource,textureImporter.wrapMode))
			{
				return path;
			}
			
			return null;
		}

		// [MenuItem("Assets/所选中的文件夹加特效专有后缀")]
		public static void AddFxSuffixs(string findPath)
		{
			List<string> paths = new List<string>();

			paths = GetFoldObjPaths(findPath);

			AddFxSuffix(paths);
		}

		public static void AddFxSuffixs()
		{
			List<string> paths = new List<string>();

			paths = GetSelectObjPaths();

			AddFxSuffix(paths);
		}

		private static void AddFxSuffix(List<string> paths)
		{
			bool IsJumpOut = false;

			//根据导入设置规则加后缀
			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];
				var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (obj is Texture)
				{
					Texture texture = obj as Texture;


					for (int j = 0; j < fxSuffixType.Length; j++)
					{
						if (texture.name.EndsWith("_" + fxSuffixType[j])) //相同后缀
						{
							// Debug.Log(" continue ");
							IsJumpOut = true;
							break;
						}
					}

					if (IsJumpOut)
					{
						IsJumpOut = false;
						continue;
					}

					string fileSuffix = Path.GetExtension(path);

					AssetImporter importer = AssetImporter.GetAtPath(path);
					TextureImporter textureImporter = importer as TextureImporter;
					string suffix = SetFxSuffix(textureImporter, TextureImporterAlphaSource.FromInput,
						textureImporter.wrapMode);
					string name = texture.name + "_" + suffix;
					texture.name = name;

					AssetDatabase.RenameAsset(path, name + fileSuffix);
				}
			}
		}

		private static string SetFxSuffix(TextureImporter textureImporter, TextureImporterAlphaSource alphaSource,
			TextureWrapMode wrapMode)
		{
			string result = String.Empty;

			if (wrapMode == TextureWrapMode.Clamp)
			{
				result = fxSuffixType[0];
			}

			if (wrapMode == TextureWrapMode.Clamp && textureImporter.alphaSource == alphaSource)
			{
				result = fxSuffixType[1];
			}

			if (wrapMode == TextureWrapMode.Repeat)
			{
				result = fxSuffixType[2];
			}

			if (wrapMode == TextureWrapMode.Repeat && textureImporter.alphaSource == alphaSource)
			{
				result = fxSuffixType[3];
			}

			return result;
		}

		private static bool IsFxSuffixImporterParmSame(string fxSuffix, TextureImporter textureImporter,
			TextureImporterAlphaSource alphaSource, TextureWrapMode wrapMode)
		{
			if (fxSuffix == fxSuffixType[0])
			{
				return wrapMode == TextureWrapMode.Clamp;
			}

			if (fxSuffix == fxSuffixType[1])
			{
				return wrapMode == TextureWrapMode.Clamp && alphaSource == textureImporter.alphaSource;
			}

			if (fxSuffix == fxSuffixType[2])
			{
				return wrapMode == TextureWrapMode.Repeat;
			}

			if (fxSuffix == fxSuffixType[3])
			{
				return wrapMode == TextureWrapMode.Repeat && alphaSource == textureImporter.alphaSource;
			}

			return false;
		}
		
		#endregion
		
		public static string[] GetAllScenes(string checkPath)
		{
			string[] guids = AssetDatabase.FindAssets("t:Scene", new[] {checkPath});
			List<string> result = new List<string>();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				result.Add(path);
			}

			return result.ToArray();
		}
	}
}
#endif