using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    private RetoreObstacle scriptA;

    private bool isCollision = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scriptA = GetComponentInParent<RetoreObstacle>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player") && !isCollision)
        {
            if (!scriptA.isRetore)
            {
                scriptA.StartCoroutine(scriptA.RetoreObstacleTimer());
                scriptA.isRetore = true;
            }

            isCollision = true;
        }
    }
}
