using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetGame : MonoBehaviour
{
    public GameObject goalText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            goalText.SetActive(true);
            Invoke("ResetSCcene", 1.0f);
        }
    }

    private void ResetSCcene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
