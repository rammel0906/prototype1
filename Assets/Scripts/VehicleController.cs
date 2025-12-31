using UnityEngine;

public class VehicleController : MonoBehaviour
{
    private float speed = 20.0f;
    public AIController scriptA;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (scriptA == null) return;

        float dt = !scriptA.isStop ? scriptA.dt : Time.fixedDeltaTime;
        transform.Translate(Vector3.forward * (speed * dt));
    }
}
