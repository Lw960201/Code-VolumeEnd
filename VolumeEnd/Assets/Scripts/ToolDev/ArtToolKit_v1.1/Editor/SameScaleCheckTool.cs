#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace ArtToolKit
{
	public class SameScaleCheckTool
	{
		public static List<GameObject> NoSameScaleCheck()
		{
			Scene curScene = SceneManager.GetActiveScene();
			List<Transform> trans = FindSceneObject<Transform>(curScene.name);
			List<GameObject> noSameScaleGo = new List<GameObject>();

			for (int i = 0; i < trans.Count; i++)
			{
				Transform tran = trans[i];
				int intensity = 1000;
				Vector3 localScale = new Vector3((int) (tran.localScale.x * intensity),
					(int) (tran.localScale.y * intensity), (int) (tran.localScale.z * intensity));
				bool isSameScale = (localScale.x == localScale.y) && (localScale.x == localScale.z);
				if (!isSameScale)
				{
					noSameScaleGo.Add(tran.gameObject);
				}
			}

			return noSameScaleGo;
		}
		
		public static List<GameObject> NoRangeScaleCheck(float minScale, float maxScale)
		{
			Scene curScene = SceneManager.GetActiveScene();
			List<Transform> trans = FindSceneObject<Transform>(curScene.name);
			List<GameObject> noRangeScaleGo = new List<GameObject>();

			for (int i = 0; i < trans.Count; i++)
			{
				Transform tran = trans[i];
				float tranMinScale = Mathf.Min(Mathf.Min(tran.localScale.x, tran.localScale.y), tran.localScale.z);
				float tranMaxScale = Mathf.Max(Mathf.Max(tran.localScale.x, tran.localScale.y), tran.localScale.z);
				bool isRangeScale = tranMinScale >= minScale && tranMaxScale <= maxScale;
				if (!isRangeScale)
				{
					noRangeScaleGo.Add(tran.gameObject);
				}
			}

			return noRangeScaleGo;
		}

		// 获取场景中所有目标对象（包括不激活的对象）不包括Prefabs:
		static List<T> FindSceneObject<T>(string _SceneName) where T : UnityEngine.Component
		{
			List<T> objectsInScene = new List<T>();
			foreach (var go in Resources.FindObjectsOfTypeAll<T>())
			{
				if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
					continue;
				if (EditorUtility.IsPersistent(go.transform.root.gameObject)) // 如果对象位于Scene中，则返回false
					continue;
				if (_SceneName != go.gameObject.scene.name)
					continue;
				//Debug.LogFormat("gameObject:{0},scene:{1}", go.gameObject.name, go.gameObject.scene.name);
				objectsInScene.Add(go);
			}

			return objectsInScene;
		}
	}
}
#endif