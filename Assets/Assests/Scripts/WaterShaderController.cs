using UnityEngine;

public class WaterController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    private static readonly int WaterProgressID = Shader.PropertyToID("_WaterProgress");

    // Modify directly on the material instance
    public void Update()
    {
        targetRenderer.material.SetFloat(WaterProgressID, 2);
    }

    // Read the current value
    public float GetProgress()
    {
        return targetRenderer.material.GetFloat(WaterProgressID);
    }
}