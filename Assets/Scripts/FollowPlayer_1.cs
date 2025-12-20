using UnityEngine;

public class FollowPlayer_1 : MonoBehaviour
{
    public GameObject player;

    private Vector3 offset = new Vector3(0, 6, -7);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.transform.position + offset;
        }
    }
}
