using UnityEngine;

public class standingObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            Invoke("addCollider", 0.3f); // Calls MyMethod after 2 seconds
    }

    public void addCollider()
    {
        CircleCollider2D collider = this.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        Destroy(this.gameObject , 5f);
    }
}
