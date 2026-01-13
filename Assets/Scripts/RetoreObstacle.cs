using UnityEngine;
using System.Collections;

public class RetoreObstacle : MonoBehaviour
{
    private float retoreTime = 0.0f;

    public GameObject thisPrefab;
    private Vector3 thisPosition;
    private Quaternion thisRotation;

    public bool isRetore = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisPosition = transform.position;
        thisRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator RetoreObstacleTimer()
    {
        retoreTime = 5.0f;

        while (retoreTime > 0)
        {
            retoreTime -= Time.deltaTime;

            yield return null;
        }

        GameObject retoreObj = Instantiate(thisPrefab, thisPosition, thisRotation);
        retoreObj.gameObject.GetComponent<RetoreObstacle>().thisPrefab = thisPrefab;

        Destroy(gameObject);
    }
}
