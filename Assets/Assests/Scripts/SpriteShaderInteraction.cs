using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicShaderController : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private string sizePropertyName = "WaterProgress";
    [SerializeField] private float growthSpeed = 1.5f;
    [SerializeField] private float maxSize = 5f;

    public SpriteRenderer spriteRenderer;
    public MaterialPropertyBlock propertyBlock;
    public int sizePropertyID;
    
    private float currentSize = 0f;
    private bool isGrowing = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        
        // Cache shader property name to an integer ID for fast updates
        sizePropertyID = Shader.PropertyToID(sizePropertyName);

        if (spriteRenderer.sharedMaterial.HasProperty(sizePropertyName))
        {
            Debug.Log($"<color=green>SUCCESS:</color> Property '{sizePropertyName}' found on material '{spriteRenderer.sharedMaterial.name}'!");
        }
        else
        {
            Debug.LogError($"<color=red>ERROR:</color> Property '{sizePropertyName}' does NOT exist on material '{spriteRenderer.sharedMaterial.name}'! Check your Reference name in Shader Graph.");
        }
    }

    private void Update()
    {
        Debug.Log("one");
        // if (!isGrowing) return;
        Debug.Log("two");
        if (currentSize < maxSize)
        {
            Debug.Log("four");
            currentSize += growthSpeed * Time.deltaTime;
            currentSize = Mathf.Min(currentSize, maxSize);

            // Read existing properties, modify the value, and push back
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(sizePropertyID, currentSize);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
        else
        {
            Debug.Log("three");
            isGrowing = false;
        }
    }

    /// <summary>
    /// Call this when the player interacts with the object.
    /// </summary>
    public void TriggerEffect()
    {
        isGrowing = true;
    }

    /// <summary>
    /// Set a precise size directly (e.g., leveling up or quest progression).
    /// </summary>
    public void SetSize(float newSize)
    {
        currentSize = Mathf.Clamp(newSize, 0f, maxSize);
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(sizePropertyID, currentSize);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}