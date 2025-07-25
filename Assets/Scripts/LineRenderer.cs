using UnityEngine;
using Sirenix.OdinInspector; // Odin Inspector

[ExecuteAlways]
public class EditableLineOnPlane : MonoBehaviour
{
    [Title("Line Points")]
    [LabelText("Start Point")]
    public Vector3 point1 = new Vector3(-2, 0.01f, -2);

    [LabelText("End Point")]
    public Vector3 point2 = new Vector3(2, 0.01f, 2);

    [Title("Line Renderer")]
    public LineRenderer lineRenderer;

    [Button("Update Line")]
    private void UpdateLine()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, point1);
        lineRenderer.SetPosition(1, point2);

        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    private void OnValidate()
    {
        UpdateLine(); // Auto-update when values change in Inspector
    }
}