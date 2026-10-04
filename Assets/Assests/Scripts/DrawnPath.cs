using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class DrawnPath : MonoBehaviour
{
    private class PathNode
    {
        public Vector2 position;
        public float spawnTime;
        public bool hasCollider;

        public PathNode(Vector2 pos, float time)
        {
            position = pos;
            spawnTime = time;
            hasCollider = false;
        }
    }

    [Header("Line Settings")]
    [SerializeField] private float minDistanceBetweenPoints = 0.08f;
    [SerializeField] private float maxDistanceBreak = 1.5f; // Prevents connecting if points jump too far apart
    [SerializeField] private float lineWidth = 0.3f;
    public Material lineMaterial;

    [Header("Timing")]
    [SerializeField] private float colliderActivationDelay = 0.5f;
    [SerializeField] private float totalLifetime = 3.0f;

    public PhysicsMaterial2D friction;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

    private readonly List<PathNode> nodes = new List<PathNode>();
    private readonly List<Vector3> visualPointsCache = new List<Vector3>();
    private readonly List<Vector2> colliderPointsCache = new List<Vector2>();

    private bool isFinalized = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        Color startColor = Color.red;
        Color endColor = Color.green;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        lineRenderer.material = lineMaterial;

    GradientColorKey[] colorKeys = new GradientColorKey[2];
    colorKeys[0] = new GradientColorKey(startColor, 0.0f); // Red at start
    colorKeys[1] = new GradientColorKey(endColor, 1.0f);   // Green at end

    // 3. Define Alpha / Transparency (REQUIRED - otherwise line defaults to 0% alpha / invisible)
    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
    alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f); // 100% visible at start
    alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f); // 100% visible at end

    // 4. Construct and assign the Gradient
    Gradient lineGradient = new Gradient();
    lineGradient.SetKeys(colorKeys, alphaKeys);
    lineRenderer.colorGradient = lineGradient;

        edgeCollider.edgeRadius = lineWidth * 0.5f;
        edgeCollider.sharedMaterial = friction;
        edgeCollider.points = new Vector2[0];
    }

    private void Update()
    {
        ProcessPointTimers();
    }

    /// <summary>
    /// Locks this path so no future points from later jumps can attach to it.
    /// </summary>
    public void FinalizePath()
    {
        isFinalized = true;
    }

    public void AddPoint(Vector2 newPoint)
    {
        // Don't accept points if this line stroke has finished
        if (isFinalized) return;

        if (nodes.Count > 0)
        {
            float dist = Vector2.Distance(nodes[nodes.Count - 1].position, newPoint);
            
            // Too close -> ignore
            if (dist < minDistanceBetweenPoints) return;

            // Too far (player moved/teleported/restarted jump) -> auto-finalize to prevent joining
            if (dist > maxDistanceBreak)
            {
                FinalizePath();
                return;
            }
        }

        nodes.Add(new PathNode(newPoint, Time.time));
        RefreshGeometry();
    }

    private void ProcessPointTimers()
    {
        if (nodes.Count == 0) return;

        float currentTime = Time.time;
        bool needsGeometryUpdate = false;

        // 1. Remove expired nodes
        while (nodes.Count > 0 && (currentTime - nodes[0].spawnTime) >= totalLifetime)
        {
            nodes.RemoveAt(0);
            needsGeometryUpdate = true;
        }

        // Destroy path object once all points expire
        if (nodes.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Activate colliders after delay
        for (int i = 0; i < nodes.Count; i++)
        {
            if (!nodes[i].hasCollider && (currentTime - nodes[i].spawnTime) >= colliderActivationDelay)
            {
                nodes[i].hasCollider = true;
                needsGeometryUpdate = true;
            }
        }

        if (needsGeometryUpdate)
        {
            RefreshGeometry();
        }
    }

    private void RefreshGeometry()
    {
        visualPointsCache.Clear();
        colliderPointsCache.Clear();

        for (int i = 0; i < nodes.Count; i++)
        {
            visualPointsCache.Add(nodes[i].position);

            if (nodes[i].hasCollider)
            {
                colliderPointsCache.Add(transform.InverseTransformPoint(nodes[i].position));
            }
        }

        lineRenderer.positionCount = visualPointsCache.Count;
        lineRenderer.SetPositions(visualPointsCache.ToArray());

        if (colliderPointsCache.Count >= 2)
        {
            edgeCollider.points = colliderPointsCache.ToArray();
        }
        else
        {
            edgeCollider.points = new Vector2[0];
        }
    }
}