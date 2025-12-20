using UnityEngine;

public class PlayerConteoller_1 : MonoBehaviour
{
    private float speed = 25.0f;
    private float trunSpeed = 120.0f;

    private Rigidbody rb;
    private float horizontalInput;
    private float forwardInput;
    public KeyCode switchKey;

    public Camera mainCamera;
    public Camera hoodCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal1");
        forwardInput = Input.GetAxis("Vertical");
        // Moves the car forward based on vertical input
        transform.Translate(Vector3.forward * Time.deltaTime * speed * forwardInput);
        // Rotates the car based on horizotal input
        transform.Rotate(Vector3.up * Time.deltaTime * trunSpeed * horizontalInput);

        if (Input.GetKeyDown(switchKey))
        {
            bool isMainActive = mainCamera.enabled;
            mainCamera.enabled = !isMainActive;
            hoodCamera.enabled = isMainActive;
        }

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Vehicle"))
        {
            Destroy(gameObject);
        }
    }
}
