#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
	/// <summary>
	/// 特效工具箱
	/// </summary>
	public class VfxEditorWindow
	{
		enum TabType
		{
			AddFxTextureSuffix,
			FxTextureSuffixCheck,
			ParticleSystemOptimize,
		}

		public static class Type
		{
			public static GUIContent[] m_tabs;
			public static GUIContent findPath;
			public readonly static string[] selectionTypeStr = {"选中文件夹操作", "选中对象操作"};
		}

		public class Parm
		{
			public static string m_FindPathStr = String.Empty;
			public static SelectionType selectionType;
			public static int slectionTypeIndex;
		}

		private TabType tabType = TabType.AddFxTextureSuffix;

		private bool toggleAutoMaxParticles;
		private bool togglePlayOnAwake;

		private Vector2 scrollPos = Vector2.zero;
		protected Color oldColor;
		private List<string> list = new List<string>();
		private List<Object> objs = new List<Object>();

		public void Clear()
		{
			list.Clear();
			objs.Clear();
		}

		public void Init()
		{
			oldColor = GUI.color;
			
			Type.m_tabs = new GUIContent[]
			{
				new GUIContent("添加特效贴图后缀"),
				new GUIContent("特效贴图后缀检查"),
				new GUIContent("粒子设置优化"),
			};

			Type.findPath = new GUIContent("查找路径:");
		}


		public void DrawVfxEditorWindow()
		{
			if (tabType == null || Type.m_tabs == null)
			{
				return;
			}

			tabType = (TabType) GUILayout.Toolbar((int) tabType, Type.m_tabs);

			switch (tabType)
			{
				case TabType.AddFxTextureSuffix:
					DrawAddFxTextureSuffixGUI();
					break;
				case TabType.FxTextureSuffixCheck:
					DrawFxTextureSuffixCheckGUI();
					break;
				case TabType.ParticleSystemOptimize:
					DrawParticleSystemOptimizeGUI();
					break;
			}
		}

		private void DrawFxTextureSuffixCheckGUI()
		{
			ArtEditorUtil.DocButton(DocURL.FxTextureSuffixCheck);
			ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
			if (GUILayout.Button("特效贴图后缀检查"))
			{
				list.Clear();
				objs.Clear();
				list = ArtEditorUtil.FxSuffixsCheck(Parm.m_FindPathStr);
				list.AddRange(ArtEditorUtil.ResolutionSuffixCheck(Parm.m_FindPathStr));
				
				Selection.objects = null;
				for (int i = 0; i < list.Count; i++)
				{
					var ls = list[i];
					objs.Add(AssetDatabase.LoadAssetAtPath<Object>(ls)); 
				}
				Selection.objects = objs.ToArray();
			}
			

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(600));

				if (list.Count <= 0)
				{
					GUILayout.Label("不存在未修正的特效贴图", new GUIStyle() {fontStyle = FontStyle.Bold,fontSize = 20});
				}
				else
				{
					GUILayout.Label("存在未修正的特效贴图", new GUIStyle() {fontStyle = FontStyle.Bold,fontSize = 20});
				}
				
				for (int i = 0; i < list.Count; i++)
				{
					var path = list[i];
					var go = AssetDatabase.LoadAssetAtPath<Object>(path);
					GUI.color = Color.red;
					if (GUILayout.Button(go.name))
					{
						EditorGUIUtility.PingObject(go);
					}

					GUI.color = oldColor;
				}

				EditorGUILayout.EndScrollView();
			}
		}

		private void DrawAddFxTextureSuffixGUI()
		{
			ArtEditorUtil.DocButton(DocURL.AddFxTextureSuffix);
			Parm.selectionType = DrawSelectionType(ref Parm.slectionTypeIndex, Type.selectionTypeStr);

			if (Parm.selectionType == SelectionType.Folder)
			{
				ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
			}

			if (GUILayout.Button("添加特效贴图后缀"))
			{
				if (Parm.selectionType == SelectionType.Folder)
				{
					ArtEditorUtil.AddFxSuffixs(Parm.m_FindPathStr);
				}
				else if (Parm.selectionType == SelectionType.Obj)
				{
					ArtEditorUtil.AddFxSuffixs();
				}
			}
		}

		private static SelectionType DrawSelectionType(ref int slectionTypeIndex, string[] selectionTypeStr)
		{
			SelectionType selectionType;
			slectionTypeIndex = EditorGUILayout.Popup("操作方式：", slectionTypeIndex, selectionTypeStr);
			selectionType = (SelectionType) slectionTypeIndex;
			return selectionType;
		}

		private void DrawParticleSystemOptimizeGUI()
		{
			EditorGUILayout.LabelField("步骤1 选择工程目录的资源(可多选)");

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(200));

				for (int i = 0; i < Selection.objects.Length; i++)
				{
					var obj = Selection.objects[i];
					if (obj != null)
					{
						if (GUILayout.Button(obj.name, GUILayout.Height(32)))
						{
							EditorGUIUtility.PingObject(obj);
						}
					}
				}

				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.LabelField("步骤2 优化粒子系统中的参数");
			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				toggleAutoMaxParticles = EditorGUILayout.ToggleLeft("自动计算 MaxParticles", toggleAutoMaxParticles);
				togglePlayOnAwake = EditorGUILayout.ToggleLeft("开启 PlayOnAwake", togglePlayOnAwake);
			}

			if (GUILayout.Button("执行优化"))
			{
				ParticleSystemOptimizeToolWindow.Optimize(toggleAutoMaxParticles, togglePlayOnAwake);
			}
		}
	}
}
#endif