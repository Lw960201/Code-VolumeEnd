#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ArtToolKit
{
	public class BatchReplacePrefabWindow
	{
		public static void BatchReplacePrefab(GameObject presetGo)
		{
			var gos = Selection.gameObjects;

			for (int i = 0; i < gos.Length; i++)
			{
				bool isReplac = false;
				var go = gos[i];


				//克隆对象
				var newGo = PrefabUtility.InstantiatePrefab(presetGo, go.transform.parent) as GameObject;
				newGo.transform.localPosition = go.transform.localPosition;
				newGo.transform.localRotation = go.transform.localRotation;
				newGo.transform.localScale = go.transform.localScale;

				var childrenTrans = go.GetComponentsInChildren<Transform>();

				//对遮挡剔除碰撞对象特殊处理
				for (int j = 0; j < childrenTrans.Length; j++)
				{
					var childrenTran = childrenTrans[j];
					if (childrenTran.name.Equals("Collider_Far"))
					{
						childrenTran.SetParent(newGo.transform);
						isReplac = true;
					}

					if (childrenTran.name.Equals("Collider_Near"))
					{
						childrenTran.SetParent(newGo.transform);
						isReplac = true;
					}
				}

				if (isReplac)
				{
					var meshRenderers = newGo.GetComponentsInChildren<MeshRenderer>();
					for (int j = 0; j < meshRenderers.Length; j++)
					{
						var meshRender = meshRenderers[j];
						for (int k = 0; k < meshRender.sharedMaterials.Length; k++)
						{
							var mat = meshRender.sharedMaterials[k];
							if (!mat.shader.name.EndsWith("-DitherCommon"))
							{
								try
								{
									mat.shader = Shader.Find(mat.shader.name + "-DitherCommon");
								}
								catch (Exception e)
								{
									Console.WriteLine(e);
									throw;
								}
							}


						}
					}
				}

				GameObject.DestroyImmediate(go);
			}
		}
	}
}
#endif