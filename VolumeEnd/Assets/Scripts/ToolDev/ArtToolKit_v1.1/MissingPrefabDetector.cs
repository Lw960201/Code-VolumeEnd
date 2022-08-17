#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ArtToolKit
{
	public class MissingPrefabDetector
	{

		public static List<string> CheckMissingPrefab(string checkPath,
			bool isCheckArtScene=true,
			bool isCheckDesignScene=true,
			bool isCheckCombineScene=true,
			bool isCheckAbScene=true)
		{
			List<string> scenePaths = new List<string>();

			string[] allScenes = ArtEditorUtil.GetAllScenes(checkPath);
			bool hasMissing = false;

			bool isCheckArt;
			bool isCheckDesign;
			bool isCheckCombine;
			bool isCheckAb;

			for (int i = 0; i < allScenes.Length; i++)
			{
				var scenePath = allScenes[i];
				string fileName = Path.GetFileName(scenePath);
				string fileNameWithoutEx = Path.GetFileNameWithoutExtension(scenePath);

				//Art
				bool isArtScene = scenePath.Contains("ScenePreview") & fileNameWithoutEx.EndsWith("_Art");
				isCheckArt = isCheckArtScene & isArtScene;
				// if (!isCheckArtScene)
				// {
				// 	continue;
				// }
				
				//Desigin
				bool isDesignScene = scenePath.Contains("ScenePreview") & fileNameWithoutEx.Contains("DesignClip");
				isCheckDesign = isCheckDesignScene & isDesignScene;
				// if (!isCheckDesignScene)
				// {
				// 	continue;
				// }
				
				//合并场景
				bool isCombineScene = scenePath.Contains("Scene_ABS");
				isCheckCombine = isCheckCombineScene & isCombineScene;
				// if (isCombineScene)
				// {
				// 	continue;
				// }
				
				string budnleGeneretor = scenePath.Replace(fileName, "BundleDescriptionListGenerator.asset");
				//打包场景
				bool isAbScene = File.Exists(budnleGeneretor);
				isCheckAb = isCheckAbScene & isAbScene;
				if (!isAbScene)
				{
					continue;
				}
				

				
				var scene = EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
				var sceneRootGos = scene.GetRootGameObjects();
				for (int j = 0; j < sceneRootGos.Length; j++)
				{
					var sceneRootGo = sceneRootGos[j];
					scenePaths.AddRange(GetMissPrefabChildernTrans(sceneRootGo.transform, scenePath));
				}
			}
			
			return scenePaths;
		}
		

		public static List<string> GetMissPrefabChildernTrans(Transform tran,string scenePath)
		{
			List<string> scenePaths = new List<string>();

			
			Transform[] childTrans = tran.GetComponentsInChildren<Transform>(true);
			Transform childTran;

			for (int i = 0; i < childTrans.Length; i++)
			{
				childTran = childTrans[i];
				if (IsFindMissingPrefabInScene(childTran))
				{
					if (!scenePaths.Contains(scenePath))
					{
						scenePaths.Add(scenePath);
					}
				}
			}
			
			return scenePaths;
			
		}


		static bool IsFindMissingPrefabInScene(Transform g)
		{
			if (g.name.Contains("Missing Prefab"))
			{
				return true;
			}

			if (PrefabUtility.IsPrefabAssetMissing(g))
			{
				return true;
			}

			if (PrefabUtility.IsDisconnectedFromPrefabAsset(g))
			{
				return true;
			}

			return false;
		}

		
	}
}
#endif