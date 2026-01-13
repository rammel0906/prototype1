using UnityEngine;

public class VehicleController : MonoBehaviour
{
    private float speed = 20.0f;

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Translate(Vector3.forward * (speed * Time.fixedDeltaTime));
    }
}
