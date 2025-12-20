using UnityEngine;
using UnityEngine.InputSystem;

public class Player2Controller : MonoBehaviour
{
    public GameObject player2;

    public float speed = 25.0f;
    public float turnSpeed = 120.0f;

    private Rigidbody rb;
    public AIController scriptA;

    public float horizontalInput;
    public float forwardInput;

    private bool detRight;
    private bool detLeft;
    public bool rotaStopRight = false;
    public bool rotaStopLeft = false;

    public Camera mainCamera;
    public Camera hoodCamera;

    private bool isReset = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = player2.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isReset && player2 != null && (!player2.gameObject.activeSelf || player2.transform.position.y <= -7.0f))
        {
            isReset = !player2.gameObject.activeSelf;
            if (isReset) Invoke("ResetPlayer", 2.0f);
            else ResetPlayer();
        }
    }

    void FixedUpdate()
    {
        if (player2 != null && !scriptA.isAuto)
        {
            Vector3 rota = player2.transform.eulerAngles;

            detRight = (rota.y >= 85f && rota.y <= 170f);
            detLeft = (rota.y <= 275f && rota.y >= 190f);

            if (detRight && horizontalInput > 0 && !rotaStopLeft)
            {
                Debug.Log("rotaStopRight");
                horizontalInput = 0;
                rota.y = 85.0f;
                player2.transform.eulerAngles = rota;
                rotaStopRight = true;
            }
            else if (!detRight && rotaStopRight)
            {
                rotaStopRight = false;
            }

            if (detLeft && horizontalInput < 0 && !rotaStopRight)
            {
                Debug.Log("rotaStopLeft");
                horizontalInput = 0;
                rota.y = 275.0f;
                player2.transform.eulerAngles = rota;
                rotaStopLeft = true;
            }
            else if (!detLeft && rotaStopLeft)
            {
                rotaStopLeft = false;
            }

            Vector3 movement = player2.transform.forward * Time.fixedDeltaTime * speed * forwardInput;
            rb.MovePosition(rb.position + movement);
            float trun = Time.fixedDeltaTime * turnSpeed * horizontalInput;
            Quaternion trunRotation = Quaternion.Euler(0f, trun, 0f);
            rb.MoveRotation(rb.rotation * trunRotation);
        }
    }

    private void OnMove2(InputValue movementValue)
    {
        if (!scriptA.isAuto)
        {
            Vector2 movementVector = movementValue.Get<Vector2>();
            horizontalInput = movementVector.x;
            forwardInput = movementVector.y;

            if (forwardInput < 0)
            {
                forwardInput = 0;
            }

            if (rotaStopRight && horizontalInput > 0)
            {
                Debug.Log("‰E‚Ö‰ñ“]‚Å‚«‚Ü‚¹‚ñ");
                horizontalInput = 0;
            }
            if (rotaStopLeft && horizontalInput < 0)
            {
                Debug.Log("¶‚Ö‰ñ“]‚Å‚«‚Ü‚¹‚ñ");
                horizontalInput = 0;
            }
        }
    }

    private void OnSwitchCamera2(InputValue Value)
    {
        if (Value.isPressed)
        {
            bool isMainActive = mainCamera.enabled;
            mainCamera.enabled = !isMainActive;
            hoodCamera.enabled = isMainActive;
        }
    }

    private void OnAutoPilot(InputValue Value)
    {
        if (Value.isPressed)
        {
            scriptA.isAuto = !scriptA.isAuto;
            if (scriptA.isAuto) scriptA.InitAuto();
            else rb.isKinematic = false;
        }
    }

    private void ResetPlayer()
    {
        player2.transform.position = new Vector3(5.0f, 0.0f, -53.0f);
        player2.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (isReset)
        {
            player2.gameObject.SetActive(true);
            isReset = false;
        }
        if (scriptA.isAuto) scriptA.InitAuto();
    }
}