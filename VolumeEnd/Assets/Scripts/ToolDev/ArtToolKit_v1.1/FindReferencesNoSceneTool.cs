#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
	public class FindReferencesNoSceneTool
	{
		static List<Object> noReferencedBy = new List<Object>();

		public static void Clear()
		{
			noReferencedBy.Clear();
		}

		//[MenuItem("Assets/寻找不在场景中有引用的对象")]
		public static List<Object> FindNoReferencesInSceneAsset(Object[] loadedObjs)
		{
			var selecteds = loadedObjs;

			for (int i = 0; i < selecteds.Length; i++)
			{
				var selected = selecteds[i];
				if (selected)
				{
					string selectedPath = AssetDatabase.GetAssetPath(selected);
					if (selectedPath.EndsWith(".FBX") || selectedPath.EndsWith(".fbx")) //如果是模型
					{
						Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(selectedPath);
						selected = mesh;
					}

					if (!FindReferencesTo(selected))
					{
						noReferencedBy.Add(selected);
					}
				}
			}

			return noReferencedBy;
		}

		public static List<Object> FindNoReferencesInSceneAsset(Object[] loadedObjs, string scenePath)
		{
			var selecteds = loadedObjs;

			for (int i = 0; i < selecteds.Length; i++)
			{
				var selected = selecteds[i];
				if (selected)
				{
					string selectedPath = AssetDatabase.GetAssetPath(selected);
					if (selectedPath.EndsWith(".FBX") || selectedPath.EndsWith(".fbx")) //如果是模型
					{
						Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(selectedPath);
						selected = mesh;
					}

					var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
					var sceneRootGos = scene.GetRootGameObjects();

					if (!FindReferencesTo(selected, sceneRootGos))
					{
						if (!noReferencedBy.Contains(selected))
						{
							noReferencedBy.Add(selected);
						}
					}
				}
			}

			return noReferencedBy;
		}

		private static bool FindReferencesTo(Object oldObj)
		{
			var allObjects = Object.FindObjectsOfType<GameObject>();
			for (int j = 0; j < allObjects.Length; j++)
			{
				var go = allObjects[j];
				//Debug.Log(" allObjects " + go.name);

				//Prefab
				if (PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Regular)
				{
					if (PrefabUtility.GetCorrespondingObjectFromSource(go) == oldObj)
					{
						//Debug.Log(string.Format("Prefab referenced by {0}, {1}", go.name, go.GetType()));
						return true;
					}
				}

				//组件的
				var components = go.GetComponents<Component>();
				for (int i = 0; i < components.Length; i++)
				{
					var c = components[i];
					if (!c) continue;

					//从SerializedObject中获取参数
					var so = new SerializedObject(c);
					var sp = so.GetIterator();

					while (sp.NextVisible(true))
					{
						if (sp.propertyType == SerializedPropertyType.ObjectReference)
						{
							// if (sp.objectReferenceValue)
							// {
							//     //Debug.Log($"Test >> {sp.objectReferenceValue.name}=={oldObj.name}为{sp.objectReferenceValue == oldObj}");
							// }

							if (sp.objectReferenceValue is Material mat && oldObj is Texture) //材质
							{
								var textureProps = mat.GetTexturePropertyNames();
								for (int k = 0; k < textureProps.Length; k++)
								{
									var textureProp = textureProps[k];
									if (mat.GetTexture(textureProp) == oldObj)
									{
										return true;
									}
								}
							}
							else
							{
								if (sp.objectReferenceValue == oldObj)
								{
									//Debug.Log(string.Format("component referenced by {0}, {1}", c.name, c.GetType()));
									return true;
								}
							}
						}
					}
				}
			}

			return false;
		}


		private static bool FindReferencesTo(Object oldObj, GameObject[] sceneRootGos)
		{
			List<Transform> allTrans = new List<Transform>();
			for (int i = 0; i < sceneRootGos.Length; i++)
			{
				var tran = sceneRootGos[i].transform;
				Transform[] childTrans = tran.GetComponentsInChildren<Transform>(true);
				allTrans.AddRange(childTrans);
			}
			for (int j = 0; j < allTrans.Count; j++)
			{
				var go = allTrans[j].gameObject;
				//Debug.Log(" allObjects " + go.name);

				//Prefab
				if (PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Regular)
				{
					if (PrefabUtility.GetCorrespondingObjectFromSource(go) == oldObj)
					{
						//Debug.Log(string.Format("Prefab referenced by {0}, {1}", go.name, go.GetType()));
						return true;
					}
				}

				//组件的
				var components = go.GetComponents<Component>();
				for (int i = 0; i < components.Length; i++)
				{
					var c = components[i];
					if (!c) continue;

					//从SerializedObject中获取参数
					var so = new SerializedObject(c);
					var sp = so.GetIterator();

					while (sp.NextVisible(true))
					{
						if (sp.propertyType == SerializedPropertyType.ObjectReference)
						{
							// if (sp.objectReferenceValue)
							// {
							//     //Debug.Log($"Test >> {sp.objectReferenceValue.name}=={oldObj.name}为{sp.objectReferenceValue == oldObj}");
							// }

							if (sp.objectReferenceValue is Material mat && oldObj is Texture) //材质
							{
								var textureProps = mat.GetTexturePropertyNames();
								for (int k = 0; k < textureProps.Length; k++)
								{
									var textureProp = textureProps[k];
									if (mat.GetTexture(textureProp) == oldObj)
									{
										return true;
									}
								}
							}
							else
							{
								if (sp.objectReferenceValue == oldObj)
								{
									//Debug.Log(string.Format("component referenced by {0}, {1}", c.name, c.GetType()));
									return true;
								}
							}
						}
					}
				}
			}

			return false;
		}
	}
}
#endif