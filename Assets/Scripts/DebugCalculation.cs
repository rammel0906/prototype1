using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class DebugCalculation : MonoBehaviour
{
    private float playerSpeed = 25.0f;
    private float turnSpeed = 120.0f;
    private float radius;
    private float time = 0.0f;

    public GameObject cube;

    private bool isSwitch = false;
    private bool isLoop = false;
    private bool isHybrid = false;

    private int key = 1;

    private int i = 0;
    private int ii = 0;
    private int iii = 0;

    [System.Serializable]
    public struct CalculationRresult
    {
        public Vector2 futurePlayerPos;
        public Vector2[] futurePlayerAxes;
        public float futurePlayerAngles;


        public CalculationRresult
            (Vector2 playerPos, Vector2[] playerAxes, float playerAngles)
        {
            futurePlayerPos = playerPos;
            futurePlayerAxes = playerAxes;
            futurePlayerAngles = playerAngles;
        }
    }
    public CalculationRresult CR;
    public CalculationRresult posCR;
    public CalculationRresult axesCR;
    public CalculationRresult hybridCR;
    public List<CalculationRresult> posInfo;
    public List<CalculationRresult> axesInfo;
    public List<CalculationRresult> hybridInfo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.z);
        Vector2[] playerAxes = new Vector2[2];
        playerAxes[0] = new Vector2(transform.right.x, transform.right.z);
        playerAxes[1] = new Vector2(transform.forward.x, transform.forward.z);
        float playerAngles = transform.eulerAngles.y;

        CR = new CalculationRresult
        (playerPos, playerAxes, playerAngles);
        Debug.Log("player現在座標:" + CR.futurePlayerPos);
        Debug.Log("player現在角度:" + CR.futurePlayerAngles);
        axesCR = CR;
        posCR = CR;
        hybridCR = CR;
        posInfo = new List<CalculationRresult>();
        axesInfo = new List<CalculationRresult>();

        Preparation();

        if (!isSwitch) Debug.Log("初期設定: 座標変更");
        if (key == 1) Debug.Log("初期設定: 右回転");
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Preparation()
    {
        for (int i2 = 0; i2 < 360; i2++)
        {
            time += 0.1f;
            float angularVelocity = turnSpeed * Mathf.Deg2Rad;
            radius = playerSpeed / angularVelocity;

            float[] result = new float[2];
            result[0] = radius * -Mathf.Cos(angularVelocity * time);
            result[1] = radius * Mathf.Sin(angularVelocity * time);
            Vector2 WorldOffset = CR.futurePlayerAxes[0] * ((result[0] + radius) * key) + CR.futurePlayerAxes[1] * result[1];
            Vector2 playerPos = CR.futurePlayerPos + WorldOffset;

            posCR = new CalculationRresult
            (playerPos, CR.futurePlayerAxes, CR.futurePlayerAngles);
            posInfo.Add(posCR);

            float anglesDeg = Mathf.Repeat(CR.futurePlayerAngles + (key * (turnSpeed * time)), 360.0f);

            float anglesRad = anglesDeg * Mathf.Deg2Rad;

            Vector2[] playerAxes = new Vector2[2];
            playerAxes[0] = new Vector2(Mathf.Cos(anglesRad), Mathf.Sin(anglesRad));
            playerAxes[1] = new Vector2(Mathf.Sin(anglesRad), Mathf.Cos(anglesRad));

            axesCR = new CalculationRresult
            (axesCR.futurePlayerPos, playerAxes, anglesDeg);
            axesInfo.Add(axesCR);

            hybridCR = new CalculationRresult
            (playerPos, playerAxes, anglesDeg);
            hybridInfo.Add(hybridCR);
        }


    }

    private void OnSwitch(InputValue value)
    {
        if (value.isPressed && !isLoop && !isHybrid)
        {
            isSwitch = !isSwitch;
            if (!isSwitch) Debug.Log("座標変更に設定");
            if (isSwitch) Debug.Log("角度変更に設定");
        }
    }

    private void OnHybrid(InputValue value)
    {
        if (value.isPressed && !isLoop)
        {
            isHybrid = !isHybrid;
            if (isHybrid) Debug.Log("ハイブリッドに設定");
            if (!isHybrid) Debug.Log("ハイブリッドの設定解除");
        }
    }

    private void OnKey(InputValue value)
    {
        if (value.isPressed && !isLoop)
        {
            if (key == 1)
            {
                key = -1;
                Debug.Log("左回転");
            }
            else
            {
                key = 1;
                Debug.Log("右回転");
            }
            posInfo.Clear();
            axesInfo.Clear();
            hybridInfo.Clear();
            Preparation();
        }
    }

    private void OnNext(InputValue value)
    {
        if (value.isPressed)
        {
            isLoop = !isLoop;
            if (isLoop)
            {
                Debug.Log("開始");
                StartCoroutine(Calculation());
            }
            if (!isLoop)
            {
                Debug.Log("終了");
                foreach (var c in GameObject.FindGameObjectsWithTag("AAA")) Destroy(c);
                i = 0;
                ii = 0;
                iii = 0;
            }
        }
    }

    IEnumerator Calculation()
    {
        Debug.Log("デバッグ開始");
        if (!isSwitch) Debug.Log("座標");
        else Debug.Log("軸");
        if (!isHybrid) Debug.Log("座標+軸");
        while (isLoop)
        {
            if (!isSwitch && !isHybrid)
            {
                for (; i < posInfo.Count; i++)
                {
                    var pos = posInfo[i];
                    transform.position = new Vector3(pos.futurePlayerPos.x, transform.position.y, pos.futurePlayerPos.y);
                    Instantiate(cube, transform.position, transform.rotation);
                    yield return new WaitForSeconds(0.1f);
                    if (!isLoop) break;
                }
            }
            else if (!isHybrid)
            {
                for (; ii < axesInfo.Count; ii++)
                {
                    var axes = axesInfo[ii];
                    Vector3 forward = new Vector3(axes.futurePlayerAxes[1].x, 0, axes.futurePlayerAxes[1].y);
                    transform.forward = forward;
                    Instantiate(cube, transform.position + transform.forward * radius, transform.rotation);
                    yield return new WaitForSeconds(0.1f);
                    if (!isLoop) break;
                }
            }
            if (isHybrid)
            {
                for (; iii < hybridInfo.Count; iii++)
                {
                    var hybrid = hybridInfo[iii];
                    transform.position = new Vector3(hybrid.futurePlayerPos.x, transform.position.y, hybrid.futurePlayerPos.y);

                    Vector3 forward = new Vector3(hybrid.futurePlayerAxes[1].x, 0, hybrid.futurePlayerAxes[1].y);
                    transform.forward = forward;
                    Instantiate(cube, transform.position, transform.rotation);
                    yield return new WaitForSeconds(0.1f);
                    if (!isLoop) break;
                    
                }
            }

            if (isLoop)
            {
                i = 0;
                ii = 0;
                iii = 0;
            }
        }
    }
}