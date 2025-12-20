using UnityEngine;

public class Player2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Vehicle"))
        {
            Debug.Log("Destroy:" + gameObject.name);
            gameObject.SetActive(false);
            Debug.Log("aaa");
        }
    }
}
