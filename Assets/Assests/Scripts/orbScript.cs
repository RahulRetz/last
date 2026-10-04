using UnityEngine;

public class orbScript : MonoBehaviour
{
    public PlayerController playerScript;
    public Vector2 orbSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      this.transform.localScale = orbSize;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // Debug.Log("collected");
        playerScript = collision.transform.GetComponent<PlayerController>();
        if(playerScript != null)
        {
            playerScript.transform.localScale = orbSize;
        }
    }
}
