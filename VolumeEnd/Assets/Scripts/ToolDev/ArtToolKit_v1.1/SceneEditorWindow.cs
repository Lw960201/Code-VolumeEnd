#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
	/// <summary>
	/// 场景工具箱
	/// </summary>
	public class SceneEditorWindow
	{
		enum TabType
		{
			MissPrefabCheck,
			NoSameScaleCheck,
			PrefabPeplace,
			FindRefNoInScene,
			FoliageDataCheck,
			ProbeSet,
		}

		public class Type
		{
			public static GUIContent[] m_tabs;
			public static GUIContent findPath;

			public static GUIContent minScale;
			public static GUIContent maxScale;
			public static GUIContent uniformScale;
			public static GUIContent prefabPreset;
			public static GUIContent isCheckArtScene;
			public static GUIContent isCheckDesignScene;
			public static GUIContent isCheckCombineScene;
			public static GUIContent isCheckAbScene;
			public static string[] checkSceneTypeStr = {"当前场景", "自定义场景"};
		}

		public class Parm
		{
			public static string m_FindPathStr = String.Empty;
			public static float minScale = 0f;
			public static float maxScale = 100f;
			public static float uniformScale = 1f;
			public static GameObject prefabPreset = null;
			public static bool isCheckArtScene = true;
			public static bool isCheckDesignScene = true;
			public static bool isCheckCombineScene = true;
			public static bool isCheckAbScene = true;
			public static int listCount = 0;
			public static List<Object> objList = new List<Object>();
			public static bool folder = true;
			public static int checkSceneTypeIndex = 0;
			public static CheckSceneType checkSceneType = CheckSceneType.CurScene;
		}

		protected Color oldColor;
		private TabType tabType = TabType.MissPrefabCheck;
		Vector2 scrollPos = Vector2.one;

		private List<Object> noReferencedByObjs = new List<Object>();
		private List<string> missPrefabScenes = new List<string>();
		private List<string> failFoliageDataCheckScenes = new List<string>();
		private List<string> probeSetScenes = new List<string>();

		private List<Object> selectionOBjs = new List<Object>();
		List<GameObject> noSameScaleGos = new List<GameObject>();

		public void Clear()
		{
			noReferencedByObjs.Clear();
			missPrefabScenes.Clear();
			failFoliageDataCheckScenes.Clear();
			probeSetScenes.Clear();
			selectionOBjs.Clear();
			FindReferencesNoSceneTool.Clear();
		}

		public void Init()
		{
			oldColor = GUI.color;
			Type.m_tabs = new GUIContent[]
			{
				new GUIContent("Missing Prefab检查"),
				new GUIContent("非等比缩放检查"),
				new GUIContent("替换Prefab"),
				new GUIContent("寻找当前场景未引用对象"),
				new GUIContent("FoliageData检查"),
				new GUIContent("Probe设置"),
			};

			Type.findPath = new GUIContent("查找路径:");
			Type.minScale = new GUIContent("最小缩放值:");
			Type.maxScale = new GUIContent("最大缩放值:");
			Type.uniformScale = new GUIContent("统一缩放值:");
			Type.prefabPreset = new GUIContent("Prefab模板:");
			Type.isCheckArtScene = new GUIContent("检查Art场景");
			Type.isCheckDesignScene = new GUIContent("检查Design场景");
			Type.isCheckCombineScene = new GUIContent("检查合并场景");
			Type.isCheckAbScene = new GUIContent("检查打包场景");
		}

		public void DrawSceneEditorWindow()
		{
			if (tabType == null || Type.m_tabs == null)
			{
				return;
			}

			tabType = (TabType) GUILayout.Toolbar((int) tabType, Type.m_tabs);

			switch (tabType)
			{
				case TabType.MissPrefabCheck:
					DrawMissPrefabCheckGUI();
					break;
				case TabType.NoSameScaleCheck:
					DrawNoSameScaleCheckGUI();
					break;
				case TabType.PrefabPeplace:
					DrawPrefabReplaceGUI();
					break;
				case TabType.FindRefNoInScene:
					DrawFindRefNoInSceneGUI();
					break;
				// case TabType.FoliageDataCheck:
				// 	DrawFoliageDataCheckGUI();
					// break;
				case TabType.ProbeSet:
					DrawProbeSetGUI();
					break;
			}
		}

		private void DrawProbeSetGUI()
		{
			ArtEditorUtil.DocButton(DocURL.ProbeSet);
			ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
			if (GUILayout.Button("Probe未设置场景"))
			{
				probeSetScenes = SetProbe(Parm.m_FindPathStr);
			}
			if (GUILayout.Button("当前场景未设置对象"))
			{
				selectionOBjs.Clear();
				selectionOBjs = SceneProbeCheck();
				Selection.objects = selectionOBjs.ToArray();
			}
			
			if (GUILayout.Button("修正当前场景未设置对象"))
			{
				selectionOBjs.Clear();
				selectionOBjs = SceneProbeCheck(true);
				Selection.objects = selectionOBjs.ToArray();
			}
			
			SceneList(probeSetScenes,"存在有问题的场景","没存在有问题的场景");
		}

		private string[] Vegetation = { "Flower","Grass","Creeper","TreeLeaf"};
		private List<string> SetProbe(string findPathStr)
		{
			string[] scenePaths = ArtEditorUtil.GetAllScenes(findPathStr);

			for (int i = 0; i < scenePaths.Length; i++)
			{
				var scenePath = scenePaths[i];
				string fileNameWithoutEx = Path.GetFileNameWithoutExtension(scenePath);
				//Art场景
				if (!IsArtScene(scenePath, fileNameWithoutEx))
				{
					continue;
				}
				var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				var sceneRootGos = scene.GetRootGameObjects();
				for (int j = 0; j < sceneRootGos.Length; j++)
				{
					var sceneRootGo = sceneRootGos[j];
					Transform[] trans = sceneRootGo.transform.GetComponentsInChildren<Transform>(true);
					if (trans.Length == 0)
					{
						break;
					}

					for (int k = 0; k < trans.Length; k++)
					{
						var tran = trans[k];
						//植被-反射probe
						for (int l = 0; l < Vegetation.Length; l++)
						{
							var v = Vegetation[l];
							if (tran.name.Contains(v))
							{
								MeshRenderer vegetationMeshRender = tran.GetComponent<MeshRenderer>();
								if (vegetationMeshRender.reflectionProbeUsage != ReflectionProbeUsage.Off)
								{
									if (!probeSetScenes.Contains(scenePath))
									{
										probeSetScenes.Add(scenePath);
										break;
									}
								}
							}
						}
						//所有light probe
						if (tran.GetComponent<MeshRenderer>())
						{
							var meshRender = tran.GetComponent<MeshRenderer>();
							
							if (meshRender.lightProbeUsage != LightProbeUsage.Off)
							{
								if (!probeSetScenes.Contains(scenePath))
								{
									probeSetScenes.Add(scenePath);
									break;
								}
							}
						}
						
					}
				}
			}

			return probeSetScenes;
		}

		private List<Object> SceneProbeCheck(bool isFix = false)
		{
			var curScene = EditorSceneManager.GetActiveScene();
			var sceneRootGos = curScene.GetRootGameObjects();
			for (int j = 0; j < sceneRootGos.Length; j++)
			{
				var sceneRootGo = sceneRootGos[j];
				Transform[] trans = sceneRootGo.transform.GetComponentsInChildren<Transform>(true);
				if (trans.Length == 0)
				{
					break;
				}

				for (int k = 0; k < trans.Length; k++)
				{
					var tran = trans[k];
					//植被-反射probe
					for (int l = 0; l < Vegetation.Length; l++)
					{
						var v = Vegetation[l];
						if (tran.name.Contains(v))
						{
							if (!tran.TryGetComponent<MeshRenderer>(out MeshRenderer vegetationMeshRender))
							{
								continue;
							}
							if (vegetationMeshRender.reflectionProbeUsage != ReflectionProbeUsage.Off)
							{
								selectionOBjs.Add(vegetationMeshRender.gameObject);
								if (isFix)
								{
									vegetationMeshRender.reflectionProbeUsage = ReflectionProbeUsage.Off;
								}
							}
						}
					}
					//所有light probe
					if (tran.GetComponent<MeshRenderer>())
					{
						var meshRender = tran.GetComponent<MeshRenderer>();
							
						if (meshRender.lightProbeUsage != LightProbeUsage.Off)
						{
							selectionOBjs.Add(meshRender.gameObject);
							if (isFix)
							{
								meshRender.lightProbeUsage = LightProbeUsage.Off;
							}
						}
						// meshRender.lightProbeUsage = LightProbeUsage.Off;
						// meshRender.reflectionProbeUsage = ReflectionProbeUsage.Off;
					}
						
				}
			}
			
			return selectionOBjs;
		}

		private static bool IsArtScene(string scenePath, string fileNameWithoutEx)
		{
			return scenePath.Contains("ScenePreview") & fileNameWithoutEx.EndsWith("_Art");
		}

		// private void DrawFoliageDataCheckGUI()
		// {
		// 	ArtEditorUtil.DocButton(DocURL.FoliageDataCheck);
		// 	ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);
		// 	if (GUILayout.Button("FoliageData检查"))
		// 	{
		// 		failFoliageDataCheckScenes = FoliageDataCheck(Parm.m_FindPathStr);
		// 	}
		//
		// 	SceneList(failFoliageDataCheckScenes,"存在有问题的场景","没存在有问题的场景");
		// }

		// private List<string> FoliageDataCheck(string findPathStr)
		// {
		// 	string[] scenePaths = ArtEditorUtil.GetAllScenes(findPathStr);
		//
		// 	for (int i = 0; i < scenePaths.Length; i++)
		// 	{
		// 		var scenePath = scenePaths[i];
		// 		string fileName = Path.GetFileName(scenePath);
		// 		string fileNameWithoutEx = Path.GetFileNameWithoutExtension(scenePath);
		// 		//Art场景
		// 		bool isArtScene = scenePath.Contains("ScenePreview") & fileNameWithoutEx.EndsWith("_Art");
		// 		
		// 		if (!isArtScene)
		// 		{
		// 			continue;
		// 		}
		// 		var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		// 		var sceneRootGos = scene.GetRootGameObjects();
		// 		for (int j = 0; j < sceneRootGos.Length; j++)
		// 		{
		// 			var sceneRootGo = sceneRootGos[j];
		// 			FoliageData[] foliageDatas = sceneRootGo.transform.GetComponentsInChildren<FoliageData>(true);
		// 			if (foliageDatas.Length == 0)
		// 			{
		// 				break;
		// 			}
		//
		// 			for (int k = 0; k < foliageDatas.Length; k++)
		// 			{
		// 				var foliageData = foliageDatas[k];
		// 				if (foliageData.m_transformListOfFoliage != null)
		// 				{
		// 					for (int l = 0; l < foliageData.m_transformListOfFoliage.Count; l++)
		// 					{
		// 						var tranList = foliageData.m_transformListOfFoliage[l];
		// 						if (tranList.m_obj == null)
		// 						{
		// 							if (!failFoliageDataCheckScenes.Contains(scenePath))
		// 							{
		// 								failFoliageDataCheckScenes.Add(scenePath);
		// 								break;
		// 							}
		// 						}
		// 						else
		// 						{
		// 							var meshRenders = tranList.m_obj.GetComponentsInChildren<MeshRenderer>(true);
		// 							for (int m = 0; m < meshRenders.Length; m++)
		// 							{
		// 								var meshRender = meshRenders[m];
		// 								if (meshRender.sharedMaterial == null)
		// 								{
		// 									if (!failFoliageDataCheckScenes.Contains(scenePath))
		// 									{
		// 										failFoliageDataCheckScenes.Add(scenePath);
		// 										break;
		// 									}
		// 								}
		// 								
		// 							}
		// 						}
		// 					}
		// 				}
		// 			}
		// 		}
		// 	}
		//
		// 	return failFoliageDataCheckScenes;
		// }

		private void DrawFindRefNoInSceneGUI()
		{
			ArtEditorUtil.DocButton(DocURL.FindRefNoInScene);
			Parm.checkSceneTypeIndex = EditorGUILayout.Popup("检查方式：", Parm.checkSceneTypeIndex, Type.checkSceneTypeStr);

			Parm.checkSceneType = (CheckSceneType) Parm.checkSceneTypeIndex;

			if (Parm.checkSceneType == CheckSceneType.CustomScene)
			{
				Parm.listCount = EditorGUILayout.DelayedIntField("检查场景数量：", Parm.listCount);
				for (int i = 0; i < Parm.listCount; i++)
				{
					if (Parm.objList.Count < Parm.listCount)
					{
						Parm.objList.Add(null);
					}
					else if (Parm.objList.Count > Parm.listCount)
					{
						Parm.objList.RemoveAt(Parm.objList.Count - 1);
					}
				}

				Parm.folder = EditorGUILayout.Foldout(Parm.folder, "检查场景");
				if (Parm.folder)
				{
					for (int i = 0; i < Parm.listCount; i++)
					{
						Parm.objList[i] =
							EditorGUILayout.ObjectField(new GUIContent($"场景{i + 1}"), Parm.objList[i], typeof(Object));
					}
				}
			}


			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				if (GUILayout.Button("加载选中的对象"))
				{
					selectionOBjs.Clear();
					for (int i = 0; i < Selection.objects.Length; i++)
					{
						var obj = Selection.objects[i];
						if (obj != null)
						{
							if (!selectionOBjs.Contains(obj))
							{
								selectionOBjs.Add(obj);
							}
						}
					}
				}

				if (Parm.checkSceneType == CheckSceneType.CurScene)
				{
					if (GUILayout.Button("寻找当前场景未引用对象"))
					{
						noReferencedByObjs.Clear();
						noReferencedByObjs =
							FindReferencesNoSceneTool.FindNoReferencesInSceneAsset(selectionOBjs.ToArray());
					}
				}
				else if (Parm.checkSceneType == CheckSceneType.CustomScene)
				{
					if (GUILayout.Button("寻找场景列表未引用对象"))
					{
						noReferencedByObjs.Clear();
						for (int i = 0; i < Parm.listCount; i++)
						{
							var scene = Parm.objList[i];
							scene = EditorGUILayout.ObjectField(new GUIContent($"检查场景{i + 1}"), scene, typeof(Object));
							var scenePath = AssetDatabase.GetAssetPath(scene);
							noReferencedByObjs.AddRange(
								FindReferencesNoSceneTool.FindNoReferencesInSceneAsset(selectionOBjs.ToArray(),
									scenePath));
						}
					}
				}

				if (GUILayout.Button("选中未引用对象"))
				{
					Selection.objects = noReferencedByObjs.ToArray();
				}
			}


			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(600));

				for (int i = 0; i < selectionOBjs.Count; i++)
				{
					var obj = selectionOBjs[i];
					if (obj != null)
					{
						if (noReferencedByObjs != null && noReferencedByObjs.Count > 0 &&
							noReferencedByObjs.Contains(obj))
						{
							GUI.color = Color.red;
						}

						if (GUILayout.Button(obj.name, GUILayout.Height(32)))
						{
							EditorGUIUtility.PingObject(obj);
						}

						GUI.color = oldColor;
					}
				}

				EditorGUILayout.EndScrollView();
			}
		}


		private void DrawPrefabReplaceGUI()
		{
			ArtEditorUtil.DocButton(DocURL.PrefabPeplace);
			Parm.prefabPreset =
				EditorGUILayout.ObjectField(Type.prefabPreset, Parm.prefabPreset, typeof(GameObject)) as
					GameObject;
			if (Parm.prefabPreset == null)
			{
				return;
			}

			if (GUILayout.Button("批量替换Prefab"))
			{
				BatchReplacePrefabWindow.BatchReplacePrefab(Parm.prefabPreset);
			}
		}


		private void DrawNoSameScaleCheckGUI()
		{
			ArtEditorUtil.DocButton(DocURL.NoSameScaleCheck);
			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				Parm.listCount = EditorGUILayout.DelayedIntField("排除目录数量：", Parm.listCount);
				for (int i = 0; i < Parm.listCount; i++)
				{
					if (Parm.objList.Count < Parm.listCount)
					{
						Parm.objList.Add(null);
					}
					else if (Parm.objList.Count > Parm.listCount)
					{
						Parm.objList.RemoveAt(Parm.objList.Count - 1);
					}
				}

				Parm.folder = EditorGUILayout.Foldout(Parm.folder, "排除目录");
				if (Parm.folder)
				{
					for (int i = 0; i < Parm.listCount; i++)
					{
						Parm.objList[i] =
							EditorGUILayout.ObjectField(new GUIContent($"目录{i + 1}"), Parm.objList[i],
								typeof(Object));
					}
				}
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				Parm.minScale = EditorGUILayout.FloatField(Type.minScale, Parm.minScale);
				Parm.maxScale = EditorGUILayout.FloatField(Type.maxScale, Parm.maxScale);

				if (GUILayout.Button("缩放范围检查(超出范围的列出)"))
				{
					noSameScaleGos.Clear();

					noSameScaleGos = SameScaleCheckTool.NoRangeScaleCheck(Parm.minScale, Parm.maxScale);

					for (int i = 0; i < Parm.objList.Count; i++)
					{
						var excludeObj = Parm.objList[i] as GameObject;
						var excludeTrans = excludeObj.GetComponentsInChildren<Transform>();
						for (int j = 0; j < excludeTrans.Length; j++)
						{
							var excludeTran = excludeTrans[j];
							if (noSameScaleGos.Contains(excludeTran.gameObject))
							{
								noSameScaleGos.Remove(excludeTran.gameObject);
							}
						}
					}

					Selection.objects = null;
					Selection.objects = noSameScaleGos.ToArray();
				}
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				// 输入框
				Parm.uniformScale = EditorGUILayout.FloatField(Type.uniformScale, Parm.uniformScale);
				if (GUILayout.Button("设置选中对象缩放"))
				{
					SetObjsUniformScale(Parm.uniformScale);
				}
			}


			if (GUILayout.Button("非等比缩放检查"))
			{
				noSameScaleGos.Clear();

				noSameScaleGos = SameScaleCheckTool.NoSameScaleCheck();

				for (int i = 0; i < Parm.objList.Count; i++)
				{
					var excludeObj = Parm.objList[i] as GameObject;
					var excludeTrans = excludeObj.GetComponentsInChildren<Transform>();
					for (int j = 0; j < excludeTrans.Length; j++)
					{
						var excludeTran = excludeTrans[j];
						if (noSameScaleGos.Contains(excludeTran.gameObject))
						{
							noSameScaleGos.Remove(excludeTran.gameObject);
						}
					}
				}

				Selection.objects = null;
				Selection.objects = noSameScaleGos.ToArray();
			}

			if (noSameScaleGos.Count <= 0)
			{
				return;
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(600));

				for (int i = 0; i < noSameScaleGos.Count; i++)
				{
					var go = noSameScaleGos[i];

					DrawLog(go);
				}

				EditorGUILayout.EndScrollView();
			}
		}

		private void SetObjsUniformScale(float scaleValue)
		{
			var gos = Selection.gameObjects;
			for (int i = 0; i < gos.Length; i++)
			{
				var go = gos[i];
				go.transform.localScale = Vector3.one * scaleValue;
			}
		}

		private void DrawLog(GameObject go)
		{
			GUI.color = Color.red;
			if (GUILayout.Button(go.name))
			{
				EditorGUIUtility.PingObject(go);
			}

			GUI.color = oldColor;
		}

		private void DrawMissPrefabCheckGUI()
		{
			ArtEditorUtil.DocButton(DocURL.MissPrefabCheck);
			ArtEditorUtil.DrawFindPath(Type.findPath, ref Parm.m_FindPathStr);

			// using (var z = new EditorGUILayout.HorizontalScope("Button"))
			// {
			// 	//Art场景
			// 	Parm.isCheckArtScene = EditorGUILayout.ToggleLeft(Type.isCheckArtScene, Parm.isCheckArtScene);
			// 	//Design场景
			// 	Parm.isCheckDesignScene = EditorGUILayout.ToggleLeft(Type.isCheckDesignScene, Parm.isCheckDesignScene);
			// 	//合并场景
			// 	Parm.isCheckCombineScene =
			// 		EditorGUILayout.ToggleLeft(Type.isCheckCombineScene, Parm.isCheckCombineScene);
			// 	//打包场景
			// 	Parm.isCheckAbScene = EditorGUILayout.ToggleLeft(Type.isCheckAbScene, Parm.isCheckAbScene);
			// }

			if (GUILayout.Button("查找Missing Prefab的场景"))
			{
				missPrefabScenes.Clear();
				missPrefabScenes = MissingPrefabDetector.CheckMissingPrefab(
					Parm.m_FindPathStr,
					Parm.isCheckArtScene,
					Parm.isCheckDesignScene,
					Parm.isCheckCombineScene,
					Parm.isCheckAbScene);
			}

			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(600));

				if (missPrefabScenes.Count > 0)
				{
					GUI.color = Color.red;

					GUILayout.Label("存在Missing Prefab的场景", new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});

					GUI.color = oldColor;

					for (int i = 0; i < missPrefabScenes.Count; i++)
					{
						var missPrefabScene = missPrefabScenes[i];
						Object scene = AssetDatabase.LoadAssetAtPath<Object>(missPrefabScene);
						if (scene != null)
						{
							GUI.color = Color.red;

							if (GUILayout.Button(scene.name, GUILayout.Height(32)))
							{
								EditorGUIUtility.PingObject(scene);
							}

							GUI.color = oldColor;
						}
					}
				}
				else
				{
					GUI.color = Color.green;

					GUILayout.Label("不存在Missing Prefab的场景", new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});

					GUI.color = oldColor;
				}


				EditorGUILayout.EndScrollView();
			}
		}


		public void SceneList(List<string> scenePaths, string failText, string successText)
		{
			using (var z = new EditorGUILayout.VerticalScope("Button"))
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false,
					GUILayout.Height(600));

				if (scenePaths.Count > 0)
				{
					GUI.color = Color.red;

					GUILayout.Label(failText, new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});

					GUI.color = oldColor;

					for (int i = 0; i < scenePaths.Count; i++)
					{
						var scenePath = scenePaths[i];
						Object scene = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
						if (scene != null)
						{
							GUI.color = Color.red;

							if (GUILayout.Button(scene.name, GUILayout.Height(32)))
							{
								EditorGUIUtility.PingObject(scene);
							}

							GUI.color = oldColor;
						}
					}
				}
				else
				{
					GUI.color = Color.green;

					GUILayout.Label(successText, new GUIStyle() {fontStyle = FontStyle.Bold, fontSize = 20});

					GUI.color = oldColor;
				}


				EditorGUILayout.EndScrollView();
			}
		}
	}
}
#endif