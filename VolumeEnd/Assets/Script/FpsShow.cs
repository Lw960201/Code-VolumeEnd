using UnityEngine;

public class FpsShow : MonoBehaviour
{
    private float deltaTime;
    void OnGUI()
    {
        GUIStyle fontStyle = new GUIStyle();
        fontStyle.fontSize =60;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        string fps = (1f / deltaTime).ToString("f2");
        GUILayout.Label("FPS:" + fps,fontStyle);
    }
}
