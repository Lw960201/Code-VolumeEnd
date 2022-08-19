#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArtToolKit
{

	/// <summary>
	/// 美术工具箱
	/// </summary>
	public class ArtEditorWindow:EditorWindow
	{
		enum TabType
		{
			Common,
			Scene,
			Vfx,
		}
		
		public class Type
		{
			public static GUIContent[] m_tabs;
		}

		private SceneEditorWindow sceneWindow;
		private VfxEditorWindow vfxWindow;
		private CommonEditorWindow commonWindow;
		private TabType tabType = TabType.Common;
		
		
		
		[MenuItem("Tools/美术工具箱")]
        public static void OpenWindow()
        {
			ArtEditorWindow window = GetWindow<ArtEditorWindow>(false, "美术工具箱", false);
        	window.Show();
        }

		private void OnEnable()
		{
			Init();
		}

		protected void OnDisable()
		{
			sceneWindow.Clear();
			vfxWindow.Clear();
			commonWindow.Clear();
		}

		protected void Init()
		{
			sceneWindow = new SceneEditorWindow();
			sceneWindow.Init();

			vfxWindow = new VfxEditorWindow();
			vfxWindow.Init();
			
			commonWindow = new CommonEditorWindow();
			commonWindow.Init();
			
			Type.m_tabs = new GUIContent[]
			{
				new GUIContent("通用"),
				new GUIContent("场景"),
				new GUIContent("特效"),
			};
		}

		protected void OnGUI()
		{
			if (tabType == null || Type.m_tabs == null)
			{
				return;
			}

			tabType = (TabType) GUILayout.Toolbar((int) tabType, Type.m_tabs);
			EditorGUILayout.Separator();
			switch (tabType)
			{
				case TabType.Common:
					DrawCommonGUI();
					break;
				case TabType.Scene:
					DrawSceneGUI();
					break;
				case TabType.Vfx:
					DrawVfxGUI();
					break;
			}
		}

		private void DrawVfxGUI()
		{
			vfxWindow.DrawVfxEditorWindow();
		}

		private void DrawSceneGUI()
		{
			sceneWindow.DrawSceneEditorWindow();
		}

		private void DrawCommonGUI()
		{
			commonWindow.DrawCommonEditorWindow();
		}

	}
}
#endif