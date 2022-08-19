#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplaceMatShaderEditor : EditorWindow
{
   private static string[] findPath = {""};
   private static Shader newShader;

   [MenuItem("BlackJack/Materials/批量替换材质Shader工具")]
   public static void OpenWindow()
   {
      ReplaceMatShaderEditor window = EditorWindow.GetWindow<ReplaceMatShaderEditor>(false,"批量替换材质Shader工具");
      window.minSize = new Vector2(500, 150);
      window.Show();
   }
   
   private void OnGUI()
   {
      findPath[0] = EditorGUILayout.TextField(new GUIContent("检查路径："), findPath[0]);
      newShader = EditorGUILayout.ObjectField(new GUIContent("模板Shader:"), newShader, typeof(Shader)) as Shader;
      if (GUILayout.Button("批量替换材质Shader"))
      {
         ReplaceMatShaderTool();
      }
   }

   public static void ReplaceMatShaderTool()
   {
      List<Material> oldShaderMatList = new List<Material>();

      oldShaderMatList = FindOldShaderMat();
      ReplaceNewShader(oldShaderMatList);
   }
   

   private static List<Material> FindOldShaderMat()
   {
      List<Material> matList = FindMatPaths();
      List<Material> oldShaderMatPathList = new List<Material>();

      for (int i = 0; i < matList.Count; i++)
      {
         Material mat = matList[i];
         
         oldShaderMatPathList.Add(mat);
      }

      return oldShaderMatPathList;
   }

   private static List<string> FindMatGuids()
   {
      List<string> matGuidList = new List<string>();
      
      string[] matGuids = AssetDatabase.FindAssets("t:Material", findPath);
         
      matGuidList.AddRange(matGuids);

      return matGuidList;
   }

   private static List<Material> FindMatPaths()
   {
      List<string> guidList = FindMatGuids();
      List<Material> matList = new List<Material>();

      for (int i = 0; i < guidList.Count; i++)
      {
         string guidStr = guidList[i];
         string path = AssetDatabase.GUIDToAssetPath(guidStr);
         matList.Add(AssetDatabase.LoadAssetAtPath<Material>(path));
      }

      return matList;
   }

   private static void ReplaceNewShader(List<Material> oldShaderMatList)
   {
      for (int i = 0; i < oldShaderMatList.Count; i++)
      {
         Material oldShaderMat =  oldShaderMatList[i];
         oldShaderMat.shader = newShader;
         // Debug.Log(" oldShaderMat.shader >> " + oldShaderMat.shader);
      }
   }
}
#endif
