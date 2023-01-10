#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtToolKit
{
    public class AirWallGenerator
    {
        static GameObject parentGo;
        public static AirWall airWall = new AirWall();
        public static void DrawAirWall(SceneView sceneView)
        {
                Event curEvent = Event.current;
                if (curEvent.type == EventType.MouseDown && curEvent.button == 0)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(curEvent.mousePosition);

                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        Debug.LogError(hit.normal);
                        
                        // GameObject go = new GameObject("Physx");
                        //
                        // if (GameObject.Find("AirWalls"))
                        // {
                        //     parentGo = GameObject.Find("AirWalls");
                        // }
                        // else
                        // { 
                        //     parentGo = new GameObject("AirWalls");
                        // }
                        //
                        // go.transform.SetParent(parentGo.transform);
                        
                        if (!airWall.points.Contains(hit.point))
                        {
                            airWall.points.Add(hit.point);
                        }

                        // if (airWall.points.Count <= 0)
                        // {
                        //     return;
                        // }
                        
                        for (int i = 0; i < airWall.points.Count; i++)
                        {
                            Handles.lighting = true;
                            // airWall.points[i] = Handles.PositionHandle(airWall.points[i], Quaternion.Euler(hit.normal));
                            // airWall.points[i] = Handles.DoPositionHandle(airWall.points[i], Quaternion.Euler(hit.normal));
                            airWall.points[i] = Handles.FreeMoveHandle(airWall.points[i], Quaternion.Euler(hit.normal),7,Vector3.one, Handles.ArrowHandleCap);
                            
                            //go.transform.localPosition = airWall.points[i];
                            Handles.Label(airWall.points[i],"测试点");
                            HandleUtility.AddControl(HandleUtility.nearestControl,5);
                        }

                        // go.transform.forward = hit.normal;
                        // if (!go.TryGetComponent(out BoxCollider boxCollider))
                        // {
                        //     go.AddComponent<BoxCollider>();
                        // }

                        // curEvent.Use();
                    }
                }

                Handles.color = Color.yellow;
                // if (airWall.points.Count < 2)
                // {
                //     return;
                // }
                for (int i = 0; i < airWall.points.Count-1; i++)
                {
                    Handles.DrawDottedLine(airWall.points[i], airWall.points[i+1],7);
                }

        }
    }
}
#endif