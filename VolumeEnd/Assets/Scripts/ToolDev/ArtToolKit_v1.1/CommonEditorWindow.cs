#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
	/// <summary>
	/// 通用工具箱
	/// </summary>
	public class CommonEditorWindow
	{
		enum TabType
		{
			ABCheck,
			ReplaceSuffix,
			AddSuffix,
			AddTextureSizeSuffix,
			ClearMatTextureRef,
			FileSizeCalculation,
			SetSpeTextureFormat,
			SetIosTextureImport,
		}

		public class Type
		{
			public static GUIContent[] m_tabs;
			public static GUIContent findPath;
			public static GUIContent srcSuffix;
			public static GUIContent destSuffix;
			public readonly static string[] selectionTypeStr = {"选中文件夹操作", "选中对象操作"};
			public readonly static string[] assetTypeStr = {"全部", "贴图", "模型", "预制体", "场景"};
		}

		public class Parm
		{
			public static string m_FindPathStr = String.Empty;
			public static string srcSuffixStr = String.Empty;
			public static string destSuffixStr = String.Empty;

			public static SelectionType selectionType = SelectionType.Folder;
			public static int slectionTypeIndex = 0;

			public static AssetType assetType = AssetType.Texture;
			public static int AssetTypeIndex = 0;
			
			public static Object ObjA;
			public static Object ObjB;
			public static string isADependency = String.Empty;
			public static string isBDependency = String.Empty;
		}

		private TabType tabType = TabType.ReplaceSuffix;
		private Vector2 scrollPos = Vector2.zero;
		private string log;
		protected Color oldColor;
		
		public void Clear()
		{
		}

		public void Init()
		{
			oldColor = GUI.color;
			
			Type.m_tabs = new GUIContent[]
			{
				new GUIContent("AB循环依赖检查"),
				new GUIContent("替换资源后缀"),
				new GUIContent("添加资源后缀"),
				new GUIContent("添加贴图分辨率后缀"),
				new GUIContent("清除材质贴图引用"),
				new GUIContent("计算贴图大小"),
				new GUIContent("设置SPE贴图Format"),
				new GUIContent("设置IOS贴图导入设置"),
			};

			Type.findPath = new GUIContent("查找路径:");
			Type.srcSuffix = new GUIContent("源后缀(如：_Stc):");
			Type.destSuffix = new GUIContent("目标后缀(如：_Pfb):");
			
		}


		public void DrawCommonEditorWindow()
		{
			if (tabType == null || Type.m_tabs == null)
			{
				return;
			}

			tabType = (TabType) GUILayout.Toolbar((int) tabType, Type.m_tabs);

			switch (tabType)
			{
				case TabType.ABCheck:
					DrawABCheckGUI();
					break;
				case TabType.ReplaceSuffix:
					DrawReplaceSuffixGUI();
					break;
				case TabType.AddSuffix:
					DrawAddSuffixGUI();
					break;
				case TabType.AddTextureSizeSuffix:
					DrawAddTextureSizeSuffixGUI();
					break;
				case TabType.ClearMatTextureRef:
					DrawClearMatTextureRefGUI();
					break;
				case TabType.FileSizeCalculation:
					DrawFileSizeCalculationGUI();
					break;
				case TabType.SetSpeTextureFormat:
					DrawSetSpeTextureFormatGUI();
					break;
				case TabType.SetIosTextureImport:
					DrawSetIosTextureImportGUI();
					break;
			}
		}


		List<string> aDependencyPaths = new List<string>();
		List<string> bDependencyPaths = new List<string>();

		private void DrawABCheckGUI()
		{
			ArtEditorUtil.DocButton(DocURL.ABCheck);

			Parm.ObjA = EditorGUILayout.ObjectField(new GUIContent("检查对象A:"), Parm.ObjA, typeof(Object));
			Parm.ObjB = EditorGUILayout.ObjectField(new GUIContent("检查对象B:"), Parm.ObjB, typeof(Object));

			if (GUILayout.Button("检查是否存在循环依赖"))
			{
				ABCheck(Parm.ObjA, Parm.ObjB, out aDependencyPaths, out bDependencyPaths);
			}

			if (Parm.ObjA==null || Parm.ObjB==null)
			{
				return;
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{

				if (aDependencyPaths.Count > 0 && bDependencyPaths.Count > 0)
				{
					GUI.color = Color.red;
					GUILayout.Label("对象所在的ABS文件夹之间存在互相依赖!", new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});
					GUI.color = oldColor;
					
					Parm.isADependency = $"{Parm.ObjA.name}依赖的对象";
					Parm.isBDependency = $"{Parm.ObjB.name}依赖的对象";
					EditorGUILayout.LabelField(Parm.isADependency);
				
					DependencyLog(aDependencyPaths);
					EditorGUILayout.LabelField(Parm.isBDependency);
					DependencyLog(bDependencyPaths);
				}
				else
				{
					GUI.color = Color.green;
					GUILayout.Label("对象所在的ABS文件夹之间不存在互相依赖!", new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});
					GUI.color = oldColor;
				}


			}
		}

		private void DependencyLog(List<string> aDependencyPaths)
		{
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
			for (int i = 0; i < aDependencyPaths.Count; i++)
			{
				var aDependencyPaht = aDependencyPaths[i];
				var aName = Path.GetFileNameWithoutExtension(aDependencyPaht);
				var aDependencyObj = AssetDatabase.LoadAssetAtPath<Object>(aDependencyPaht);
				GUI.color = Color.red;
				if (GUILayout.Button(aName, GUILayout.Height(32)))
				{
					EditorGUIUtility.PingObject(aDependencyObj);
				}

				GUI.color = oldColor;
			}
			
			EditorGUILayout.EndScrollView();

		}

		private void ABCheck(Object ObjA, Object ObjB, out List<string> aDependencyPaths,
			out List<string> bDependencyPaths)
		{
			// bool isAdepenB = false;
			// bool isBdepenA = false;
			
			List<string> aPaths = new List<string>();
			List<string> bPaths = new List<string>();

			aDependencyPaths = new List<string>();
			bDependencyPaths = new List<string>();

			var aPath = AssetDatabase.GetAssetPath(ObjA);
			var bPath = AssetDatabase.GetAssetPath(ObjB);
			
			string aFolderPath = GetAbsFolder(aPath);
			string bFolderPath = GetAbsFolder(bPath);
			if (aFolderPath == null || bFolderPath == null)
			{
				ArtEditorUtil.ShowTips("检查对象不在ABS文件夹内！");
				return;
			}
			
			if (aFolderPath.Equals(bFolderPath))
			{
				ArtEditorUtil.ShowTips("检查对象属于同一个ABS文件夹内！");
				return;
			}


			string[] aPathsInFolder = GetPathsInAbsFolder(aPath);
			string[] bPathsInFolder = GetPathsInAbsFolder(bPath);


			for (int i = 0; i < aPathsInFolder.Length; i++)
			{
				var aPathInFolder = aPathsInFolder[i];
				aPaths.AddRange(GetAllDependency(aPathInFolder));
			}

			for (int i = 0; i < bPathsInFolder.Length; i++)
			{
				var bPathInFolder = bPathsInFolder[i];
				bPaths.AddRange(GetAllDependency(bPathInFolder));
			}

			aDependencyPaths = GetDependencyPaths(aPaths, bPathsInFolder /*, out isAdepenB*/);
			bDependencyPaths = GetDependencyPaths(bPaths, aPathsInFolder /*, out isBdepenA*/);
			//
			// if (isAdepenB && isBdepenA)
			// {
			//     return true; 
			// }
		}

		/// <summary>
		/// a依赖的对象是否有b中的对象
		/// </summary>
		/// <param name="aDependencyPaths"></param>
		/// <param name="bPathsInFolder"></param>
		/// <param name="isAdepenB"></param>
		private static void IsDependency(List<string> aDependencyPaths, string[] bPathsInFolder, out bool isAdepenB)
		{
			isAdepenB = false;
			for (int i = 0; i < aDependencyPaths.Count; i++)
			{
				var aPath = aDependencyPaths[i];
				for (int j = 0; j < bPathsInFolder.Length; j++)
				{
					var bPathInFolder = bPathsInFolder[j];
					if (aPath.Equals(bPathInFolder))
					{
						isAdepenB = true;
					}
				}
			}
		}

		/// <summary>
		/// a依赖的对象是否有b中的对象
		/// </summary>
		/// <param name="aDependencyPaths"></param>
		/// <param name="bPathsInFolder"></param>
		/// <param name="isAdepenB"></param>
		private static List<string> GetDependencyPaths(List<string> aDependencyPaths,
			string[] bPathsInFolder /*, out bool isAdepenB*/)
		{
			List<string> dependencyPahts = new List<string>();
			// isAdepenB = false;

			for (int i = 0; i < aDependencyPaths.Count; i++)
			{
				var aPath = aDependencyPaths[i];
				for (int j = 0; j < bPathsInFolder.Length; j++)
				{
					var bPathInFolder = bPathsInFolder[j];
					if (aPath.Equals(bPathInFolder))
					{
						// isAdepenB = true;
						if (!dependencyPahts.Contains(bPathInFolder))
						{
							dependencyPahts.Add(bPathInFolder);
						}
					}
				}
			}

			return dependencyPahts;
		}

		private string[] GetPathsInAbsFolder(string folderPath)
		{
			List<string> paths = new List<string>();
			
			var folderNames = folderPath.Split('/');
			for (int i = 0; i < folderNames.Length; i++)
			{
				var folderName = folderNames[i];
				if (folderName.EndsWith("_ABS"))
				{
					folderPath =String.Empty;
					for (int j = 0; j <= i; j++)
					{
						if (j==0)
						{
							folderPath += folderNames[j];
						}
						else
						{
							folderPath += "/" + folderNames[j];
						}
					}
				}
			}
			
			var guids = AssetDatabase.FindAssets("t:Object", new string[] {folderPath});
			for (int i = 0; i < guids.Length; i++)
			{
				var guid = guids[i];
				paths.Add(AssetDatabase.GUIDToAssetPath(guid));
			}

			return paths.ToArray();
		}

		private string GetAbsFolder(Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			string[] paths = path.Split('/');
			for (int i = paths.Length - 1; i >= 0; i--)
			{
				var p = paths[i];
				if (p.Contains("_ABS"))
				{
					return p;
				}
			}

			return null;
		}
		
		private string GetAbsFolder(string path)
		{
			string[] paths = path.Split('/');
			for (int i = paths.Length - 1; i >= 0; i--)
			{
				var p = paths[i];
				if (p.Contains("_ABS"))
				{
					return p;
				}
			}

			return null;
		}

		private string[] GetAllDependency(Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			var dependencyPaths = AssetDatabase.GetDependencies(path);
			return dependencyPaths;
		}

		private string[] GetAllDependency(string path)
		{
			var dependencyPaths = AssetDatabase.GetDependencies(path);
			return dependencyPaths;
		}

		private void DrawSetIosTextureImportGUI()
		{
			if (GUILayout.Button("选中对象设置IOS贴图导入设置"))
			{
				BatchTextureRename.BatchSetIosTextureImport();
			}
		}

		private void DrawAddTextureSizeSuffixGUI()
		{
			ArtEditorUtil.DocButton(DocURL.AddTextureSizeSuffix);
			if (GUILayout.Button("选中对象添加贴图尺寸后缀"))
			{
				BatchTextureRename.AddTextureSizeSuffix();
			}
		}

		private void DrawAddSuffixGUI()
		{
			ArtEditorUtil.DocButton(DocURL.AddSuffix);

			Parm.slectionTypeIndex = EditorGUILayout.Popup("操作方式：", Parm.slectionTypeIndex, Type.selectionTypeStr);

			Parm.selectionType = (SelectionType) Parm.slectionTypeIndex;

			if (Parm.selectionType == SelectionType.Folder)
			{
				using (var z = new EditorGUILayout.HorizontalScope("Button"))
				{
					Parm.m_FindPathStr = EditorGUILayout.TextField(Type.findPath, Parm.m_FindPathStr);
					if (GUILayout.Button("更新路径"))
					{
						Parm.m_FindPathStr = ArtEditorUtil.RefreshFindPath(Selection.assetGUIDs[0]);
					}
				}
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				Parm.destSuffixStr = EditorGUILayout.TextField(Type.destSuffix, Parm.destSuffixStr);
				Parm.AssetTypeIndex = EditorGUILayout.Popup("资源类型：", Parm.AssetTypeIndex, Type.assetTypeStr);

				Parm.assetType = (AssetType) Parm.AssetTypeIndex;
			}

			if (GUILayout.Button("添加资源名后缀"))
			{
				if (Parm.selectionType == SelectionType.Folder)
				{
					ArtEditorUtil.AddSuffixs(Parm.m_FindPathStr, Parm.destSuffixStr,
						Parm.assetType);
				}
				else if (Parm.selectionType == SelectionType.Obj)
				{
					ArtEditorUtil.AddSuffixs(Parm.destSuffixStr,
						Parm.assetType);
				}
			}
		}

		private void DrawReplaceSuffixGUI()
		{
			ArtEditorUtil.DocButton(DocURL.ReplaceSuffix);
			Parm.slectionTypeIndex = EditorGUILayout.Popup("操作方式：", Parm.slectionTypeIndex, Type.selectionTypeStr);

			Parm.selectionType = (SelectionType) Parm.slectionTypeIndex;

			if (Parm.selectionType == SelectionType.Folder)
			{
				ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				Parm.srcSuffixStr = EditorGUILayout.TextField(Type.srcSuffix, Parm.srcSuffixStr);
				Parm.destSuffixStr = EditorGUILayout.TextField(Type.destSuffix, Parm.destSuffixStr);
				Parm.AssetTypeIndex = EditorGUILayout.Popup("资源类型：", Parm.AssetTypeIndex, Type.assetTypeStr);

				Parm.assetType = (AssetType) Parm.AssetTypeIndex;
			}

			if (GUILayout.Button("替换资源后缀"))
			{
				if (Parm.selectionType == SelectionType.Folder)
				{
					ArtEditorUtil.ReplaceSuffixs(Parm.m_FindPathStr, Parm.srcSuffixStr, Parm.destSuffixStr,
						Parm.assetType);
				}
				else if (Parm.selectionType == SelectionType.Obj)
				{
					ArtEditorUtil.ReplaceSuffixs(Parm.srcSuffixStr, Parm.destSuffixStr,
						Parm.assetType);
				}
			}
		}

		private void DrawClearMatTextureRefGUI()
		{
			ArtEditorUtil.DocButton(DocURL.ClearMatTextureRef);

			Parm.slectionTypeIndex = EditorGUILayout.Popup("操作方式：", Parm.slectionTypeIndex, Type.selectionTypeStr);
			Parm.selectionType = (SelectionType) Parm.slectionTypeIndex;

			if (Parm.selectionType == SelectionType.Folder)
			{
				ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
			}

			if (GUILayout.Button("检查存在GOM贴图引用的材质"))
			{
				ArtEditorUtil.ClearMatGomRef(Parm.m_FindPathStr, Parm.selectionType, true);
			}

			if (GUILayout.Button("清除材质GOM贴图引用"))
			{
				ArtEditorUtil.ClearMatGomRef(Parm.m_FindPathStr, Parm.selectionType);
				ArtEditorUtil.ShowTips("清除成功！");
			}

			if (GUILayout.Button("清除材质所有贴图引用"))
			{
				ArtEditorUtil.ClearMatAllTextureRef(Parm.m_FindPathStr, Parm.selectionType);
				ArtEditorUtil.ShowTips("清除成功！");
			}
		}

		private void DrawSetSpeTextureFormatGUI()
		{
			ArtEditorUtil.DocButton(DocURL.SetSpeTextureFormat);
			if (GUILayout.Button("批量设置SPE贴图Format"))
			{
				BatchTextureRename.BatchSetSpeTextrueFormat();
			}
		}

		/// <summary>
		/// 绘制文件大小计算UI
		/// </summary>
		private void DrawFileSizeCalculationGUI()
		{
			ArtEditorUtil.DocButton(DocURL.FileSizeCalculation);
			if (GUILayout.Button("计算贴图大小"))
			{
				log = FileSizeDebug.FileSizeLog();
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(200));

				EditorGUILayout.LabelField(log);

				EditorGUILayout.EndScrollView();
			}
		}
	}
}
#endif