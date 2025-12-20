using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AIController : MonoBehaviour
{
    private float playerSpeed = 25.0f;
    private float turnSpeed = 120.0f;
    private float vehicleSpeed = 20.0f;

    private Rigidbody rb;

    public GameObject newVehicle;

    public bool isAuto = false;
    public bool isStart = false;
    public bool isBreak = false;
    public bool isStop = false;

    [System.Serializable]
    public struct CalculationResult
    {
        public Vector2 fPP; // futurePlayerPos
        public Vector2[] fPAx; // futurePlayerAxes
        public float fPAn; // futurePlayerAngles

        public Vector2 fVP; // futureVehiclePos
        public Vector2[] fVAx; // futureVehicleAxes

        public bool isValid;
        public bool isEnd;

        public CalculationResult
        (Vector2 playerPos, Vector2[] playerAxes, float playerAngle, Vector2 vehiclePos, Vector2[] vehicleAxes)
        {
            if (playerPos.x >= -8.0f && playerPos.x <= 8.0f && (playerAngle < 85.0f || playerAngle > 275.0f) && 
               (playerPos.y <= vehiclePos.y || Mathf.Abs(playerPos.y - vehiclePos.y) <= 10.0f))
            {
                fPP = playerPos;
                fPAx = playerAxes;
                fPAn = playerAngle;

                fVP = vehiclePos;
                fVAx = vehicleAxes;

                isValid = true;
                isEnd = (playerPos.y >= vehiclePos.y);
            }
            else
            {
                fPP = Vector2.zero;
                fPAx = new Vector2[2];
                fPAn = 0.0f;

                fVP = Vector2.zero;
                fVAx = new Vector2[2];

                isValid = false;
                isEnd = false;
            }
        }
    }
    [System.Serializable]
    public struct FutureCourseInfo
    {
        public int processSelected;
        public int keyInput;
        public float inputTime;

        public FutureCourseInfo(int process, int key, float time)
        {
            processSelected = process;
            keyInput = key;
            inputTime = time;
        }
    }
    [System.Serializable]
    public struct PairList
    {
        public CalculationResult nextCR;
        public FutureCourseInfo nextFCI;

        public PairList(CalculationResult cr, FutureCourseInfo fci)
        {
            nextCR = cr;
            nextFCI = fci;
        }
    }
    public class SelectedFilter
    {
        public List<PairList> easy = new List<PairList>();
        public List<PairList> normal = new List<PairList>();
        public List<PairList> hard = new List<PairList>();
    }

    public CalculationResult CR;
    public FutureCourseInfo FCI;
    public List<FutureCourseInfo> FCIInfo = new List<FutureCourseInfo>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isAuto && isStart && !isBreak)
        {
            Vector2 vehiclePos = new Vector2(newVehicle.transform.position.x, newVehicle.transform.position.z);
            Vector2[] vehicleAxes = new Vector2[2];
            vehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
            vehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);

            CR = new CalculationResult
            (CR.fPP, CR.fPAx, CR.fPAn, vehiclePos, vehicleAxes);

            CalculationCourse();
        }

        if (isAuto && isStop) transform.Translate(Vector3.forward * (playerSpeed * Time.deltaTime));
    }

    public void InitAuto()
    {
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.z);
        Vector2[] playerAxes = new Vector2[2];
        playerAxes[0] = new Vector2(transform.right.x, transform.right.z);
        playerAxes[1] = new Vector2(transform.forward.x, transform.forward.z);
        float playerAngle = transform.eulerAngles.y;

        Vector2 vehiclePos = new Vector2(newVehicle.transform.position.x, newVehicle.transform.position.z);
        Vector2[] vehicleAxes = new Vector2[2];
        vehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
        vehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);

        CR = new CalculationResult
        (playerPos, playerAxes, playerAngle, vehiclePos, vehicleAxes);

        FCIInfo.Clear();

        isBreak = false;
        isStop = false;
        rb.isKinematic = true;

        CalculationCourse();

        StartCoroutine(PlayerMovement());
    }

    IEnumerator PlayerMovement()
    {
        for (int i = 0; i < FCIInfo.Count; i++)
        {
            var f = FCIInfo[i];
            float elapsed = 0.0f;
            while (elapsed < f.inputTime)
            {
                if (!isAuto) yield break;

                float dt = Mathf.Min(Time.fixedDeltaTime, f.inputTime - elapsed);

                transform.Translate(Vector3.forward * (playerSpeed * dt));

                if (f.processSelected == 2 || f.processSelected == 3)
                {
                    transform.Rotate(Vector3.up * ((turnSpeed * f.keyInput) * dt));
                }

                elapsed += dt;
                yield return new WaitForFixedUpdate();
            }
        }
        rb.isKinematic = false;
        isStop = true;
    }

    private void CalculationCourse()
    {
        int futureProcess = 0;
        int futureKey = 0;

        while (!CR.isEnd && !isBreak)
        {
            List<PairList> PL = new List<PairList>();
            List<int> nextFutureProcess = new List<int> { 1, 2, 3 };

            SelectedFilter filter = new SelectedFilter();
            var rules = new[]
            {

                new{MinX = 0.0f, MaxX = 2.5f, MinAngle = 330.0f, MaxAngle = 20.0f, Target = filter.easy},
                new{MinX = 0.0f, MaxX = 2.5f, MinAngle = 300.0f, MaxAngle = 40.0f, Target = filter.normal},
                new{MinX = 0.0f, MaxX = 2.5f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = 2.5f, MaxX = 5.0f, MinAngle = 315.0f, MaxAngle = 20.0f, Target = filter.easy},
                new{MinX = 2.5f, MaxX = 5.0f, MinAngle = 300.0f, MaxAngle = 360.0f, Target = filter.normal},
                new{MinX = 2.5f, MaxX = 5.0f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = 5.0f, MaxX = 6.5f, MinAngle = 320.0f, MaxAngle = 360.0f, Target = filter.easy},
                new{MinX = 5.0f, MaxX = 6.5f, MinAngle = 0.0f, MaxAngle = 25.0f, Target = filter.normal},
                new{MinX = 5.0f, MaxX = 6.5f, MinAngle = 275f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = 6.5f, MaxX = 8.0f, MinAngle = 300.0f, MaxAngle = 360.0f, Target = filter.easy},
                new{MinX = 6.5f, MaxX = 8.0f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 340.0f, MaxAngle = 30.0f, Target = filter.easy},
                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 320.0f, MaxAngle = 85.0f, Target = filter.normal},
                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard},

                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 340.0f, MaxAngle = 40.0f, Target = filter.easy},
                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = filter.normal},
                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard},

                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 0.0f, MaxAngle = 45.0f, Target = filter.easy},
                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 335.0f, MaxAngle = 360.0f, Target = filter.normal},
                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 275.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = -8.0f, MaxX = -6.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = filter.easy},
                new{MinX = -8.0f, MaxX = -6.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard}
            };
            bool angleRange(float angle, float min, float max)
            {
                if (min < max) return angle >= min && angle <= max;
                else return angle >= min || angle <= max;
            }

            futureProcess = 1;
            futureKey = 1;
            for (int i = 1; i < 10; i++)
            {
                float time = i * 0.1f;

                Vector2 playerPos = CR.fPP + CR.fPAx[1] * (playerSpeed * time);
                Vector2 vehiclePos = CR.fVP + CR.fVAx[1] * (vehicleSpeed * time);

                CalculationResult nextCR = new CalculationResult
                (playerPos, CR.fPAx, CR.fPAn, vehiclePos, CR.fVAx);

                if (nextCR.isValid)
                {
                    FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);
                    PL.Add(new PairList(nextCR, nextFCI));
                }
                else break;

            }
            for (int rotation = 0; rotation < 2; rotation++)
            {
                if (rotation == 0)
                {
                    futureProcess = 2;
                    futureKey = 1;
                }
                else if (rotation == 1)
                {
                    futureProcess = 3;
                    futureKey = -1;
                }

                for (int i = 1; i < 6; i++)
                {
                    float time = i * 0.1f;

                    float angleDeg = Mathf.Repeat(CR.fPAn + (futureKey * (turnSpeed * time)), 360.0f);

                    float angularVelocity = turnSpeed * Mathf.Deg2Rad;
                    float radius = playerSpeed / angularVelocity;
                    Vector2 center = CR.fPP + CR.fPAx[0] * (radius * futureKey);

                    float[] result = new float[2];
                    result[0] = radius * -Mathf.Cos(angularVelocity * time);
                    result[1] = radius * Mathf.Sin(angularVelocity * time);
                    Vector2 WorldOffset = CR.fPAx[0] * ((radius + result[0]) * futureKey) + CR.fPAx[1] * result[1];
                    Vector2 playerPos = CR.fPP + WorldOffset;

                    Vector2 vehiclePos = CR.fVP + (CR.fVAx[1] * (vehicleSpeed * time));

                    float angleRad = angleDeg * Mathf.Deg2Rad;

                    Vector2[] playerAxes = new Vector2[2];
                    playerAxes[0] = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad));
                    playerAxes[1] = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

                    CalculationResult nextCR = new CalculationResult
                    (playerPos, playerAxes, angleDeg, vehiclePos, CR.fVAx);

                    if (nextCR.isValid)
                    {
                        FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);
                        PL.Add(new PairList(nextCR, nextFCI));
                    }
                    else break;
                }
            }

            if (PL.Count != 0)
            {
                foreach (var x in PL.Select((p, i) => (Item: p, Index: i)))
                {
                    foreach (var rule in rules)
                    {
                        if (x.Item.nextCR.fPP.x >= rule.MinX && x.Item.nextCR.fPP.x <= rule.MaxX
                            && angleRange(x.Item.nextCR.fPAn, rule.MinAngle, rule.MaxAngle))
                        {
                            rule.Target.Add(x.Item);
                            break;
                        }
                    }
                }

                int easyPro = 0; // easyProbability
                int normalPro = 0; // normalProbability
                int hardPro = 0; // hardProbability
                int rePro = Random.Range(0, 101); // resultProbability
                switch (filter.easy.Count, filter.normal.Count, filter.hard.Count)
                {
                    case ( > 0, 0, 0):
                        easyPro = 100;
                        normalPro = -1;
                        hardPro = -1;
                        break;

                    case (0, > 0, 0):
                        easyPro = -1;
                        normalPro = 100;
                        hardPro = -1;
                        break;

                    case (0, 0, > 0):
                        easyPro = -1;
                        normalPro = -1;
                        hardPro = 100;
                        break;

                    case ( > 0, > 0, 0):
                        easyPro = 97;
                        normalPro = 100;
                        hardPro = -1;
                        break;

                    case (0, > 0, > 0):
                        easyPro = -1;
                        normalPro = 98;
                        hardPro = 100;
                        break;

                    case ( > 0, 0, > 0):
                        easyPro = 98;
                        normalPro = -1;
                        hardPro = 100;
                        break;

                    case ( > 0, > 0, > 0):
                        easyPro = 90;
                        normalPro = 99;
                        hardPro = 100;
                        break;
                    default:
                        break;
                }

                if (rePro <= easyPro && filter.easy.Count != 0)
                {
                    int randomIndex = Random.Range(0, filter.easy.Count);
                    CR = filter.easy[randomIndex].nextCR;
                    FCI = filter.easy[randomIndex].nextFCI;
                    FCIInfo.Add(filter.easy[randomIndex].nextFCI);
                }
                else if (rePro <= normalPro && filter.normal.Count != 0)
                {
                    int randomIndex = Random.Range(0, filter.normal.Count);
                    CR = filter.normal[randomIndex].nextCR;
                    FCI = filter.normal[randomIndex].nextFCI;
                    FCIInfo.Add(filter.normal[randomIndex].nextFCI);
                }
                else if (rePro <= hardPro && filter.hard.Count != 0)
                {
                    int randomIndex = Random.Range(0, filter.hard.Count);
                    CR = filter.hard[randomIndex].nextCR;
                    FCI = filter.hard[randomIndex].nextFCI;
                    FCIInfo.Add(filter.hard[randomIndex].nextFCI);
                }
            }
            if (PL.Count == 0) isBreak = true;
        }
        isStart = false;
    }
}
