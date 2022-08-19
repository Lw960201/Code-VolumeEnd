using UnityEngine;
using System.Collections.Generic;

using UnityEditor;

[CustomEditor(typeof(AirWall))]
public class AirWallGeneratorInspector : Editor
{
    AirWall airWall;
    public bool isEditAirWall = false;
    GameObject parentGo;

    void OnEnable()
    {
        airWall = (AirWall)target;
    }

    public override void OnInspectorGUI()
    {
        isEditAirWall = GUILayout.Toggle(isEditAirWall, "编辑(alt+鼠标左键)", new GUIStyle("button"));

        if (GUILayout.Button("生成碰撞体"))
        {
            isEditAirWall = false;
            GeneratCollider();
        }
        DrawDefaultInspector();
    }

    private void GeneratCollider()
    {
        Debug.LogError("生成碰撞体");

        var airWalls = GameObject.Find("AirWall");
        if (airWalls)
        {
            parentGo = airWalls;
            RemoveCollider(parentGo);
        }
        else
        { 
            parentGo = new GameObject("AirWall");
        }
        
        for (int i = 0; i < airWall.points.Count - 1; i++)
        {
            GameObject go = new GameObject("Physx");
            var offset = Vector3.Distance(airWall.points[i], airWall.points[i + 1]) / 2;
            var dir = Vector3.Normalize(airWall.points[i + 1] - airWall.points[i]);
            var sideDir = Vector3.Cross(dir, Vector3.up);
            go.transform.localPosition = airWall.points[i] + dir * offset + sideDir * airWall.widht/2 + Vector3.up * airWall.height/2;
            go.transform.forward =  dir;
            go.transform.SetParent(parentGo.transform);
            if (!go.transform.TryGetComponent(out BoxCollider boxCollider))
            {
                boxCollider = go.AddComponent<BoxCollider>();
            }

            // Debug.LogError("dir >>>>>>>>>>>>>>>>> " + dir);
            // Debug.LogError("offset >> " + offset);
            boxCollider.size = new Vector3(airWall.widht,airWall.height,offset * 2);
        }
    }

    private void RemoveCollider(GameObject rootGo)
    {
        var trans = rootGo.transform.GetComponentsInChildren<Transform>();
        for (int i = 1; i < trans.Length; i++)
        {
            var tran = trans[i];
            GameObject.DestroyImmediate(tran.gameObject);
        }
    }

    public void OnSceneGUI()
    {
        if (!isEditAirWall)
        {
            return;
        }
        
        Event curEvent = Event.current;
        if (curEvent.alt && curEvent.type == EventType.MouseDown && curEvent.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(curEvent.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Debug.LogError(hit.normal);

                if (!airWall.points.Contains(hit.point))
                {
                    airWall.points.Add(hit.point);
                }
                else
                {
                    return;
                }
                
                
            }
        }

        if (airWall.points.Count <= 0)
        {
            return;
        }
        for (int i = 0; i < airWall.points.Count; i++)
        {
            airWall.points[i] = Handles.PositionHandle(airWall.points[i], Quaternion.identity);
        }

        Handles.color = Color.yellow;
        if (airWall.points.Count < 2)
        {
            return;
        }
        for (int i = 0; i < airWall.points.Count - 1; i++)
        {
            Handles.DrawDottedLine(airWall.points[i], airWall.points[i + 1], 7);
        }
    }
}