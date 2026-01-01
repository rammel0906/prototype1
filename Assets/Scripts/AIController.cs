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
    private BoxCollider playerBox;
    private Vector2 playerExtents;

    public GameObject newVehicle;
    public Vector2 newVehiclePos;

    public float dt;

    public GameObject cube;
    public GameObject cube1;
    public GameObject cube2;
    public GameObject cube3;
    public GameObject cube4;
    public GameObject cube5;
    public GameObject cube6;

    public bool isStart = false;
    public bool isBreak = false;
    public bool isStop = false;
    public bool isReset = false;

    private int[] useLoop = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    public List<GameObject> vehicles = new List<GameObject>();

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
            if (playerPos.x >= -8.5f && playerPos.x <= 8.5f && (playerAngle < 85.0f || playerAngle > 275.0f) && 
               (playerPos.y <= vehiclePos.y || Mathf.Abs(playerPos.y - vehiclePos.y) <= 7.0f))
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
    public struct OBB2D_XZ
    {
        public Vector2 center;
        public Vector2[] axes;
        public Vector2 extents;

        public OBB2D_XZ(Vector2 nextCRCenter,Vector2[] nextCRAxes, BoxCollider box)
        {
            center = nextCRCenter;
            axes = new Vector2[2];
            axes[0] = nextCRAxes[0].normalized;
            axes[1] = nextCRAxes[1].normalized;
            Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
            extents = new Vector2((size.x * 1.01f) * 0.5f, (size.z * 1.01f) * 0.5f);
        }
    }
    public static class SAT2D_XZ
    {
        public static bool CheckOBBvsOBB(OBB2D_XZ a, OBB2D_XZ b)
        {
            Vector2[] axesToTest = new Vector2[4];
            axesToTest[0] = a.axes[0];
            axesToTest[1] = a.axes[1];
            axesToTest[2] = b.axes[0];
            axesToTest[3] = b.axes[1];

            foreach (var axis in axesToTest)
            {
                float minA, maxA;
                ProjectOBBOnAxis(a, axis, out minA, out maxA);

                float minB, maxB;
                ProjectOBBOnAxis(b, axis, out minB, out maxB);

                if (minA > maxB || minB > maxA) return false;
            }
            return true;
        }
        private static void ProjectOBBOnAxis(OBB2D_XZ obb, Vector2 axis, out float min, out float max)
        {
            Vector2[] vertices = new Vector2[4];
            vertices[0] = obb.center + obb.axes[0] * obb.extents.x + obb.axes[1] * obb.extents.y;
            vertices[1] = obb.center - obb.axes[0] * obb.extents.x + obb.axes[1] * obb.extents.y;
            vertices[2] = obb.center + obb.axes[0] * obb.extents.x - obb.axes[1] * obb.extents.y;
            vertices[3] = obb.center - obb.axes[0] * obb.extents.x - obb.axes[1] * obb.extents.y;

            min = max = Vector2.Dot(vertices[0], axis);
            for (int i = 1; i < 4; i++)
            {
                float projection = Vector2.Dot(vertices[i], axis);
                if (projection > max) max = projection;
                if (projection < min) min = projection;
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
        playerBox = GetComponent<BoxCollider>();

        InitStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (isStart && !isBreak)
        {
            Vector2 vehiclePos = newVehiclePos;
            Vector2[] vehicleAxes = new Vector2[2];
            vehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
            vehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);

            CR = new CalculationResult
            (CR.fPP, CR.fPAx, CR.fPAn, vehiclePos, vehicleAxes);

            CalculationCourse();
        }

        if (isStop) transform.Translate(Vector3.forward * (playerSpeed * Time.deltaTime));

        if (transform.position.y <= -7.0f) ResetAI();
    }
    
    void OnDisable()
    {
        isReset = true;
        Invoke("ResetAI", 2.0f);
    }

    public void InitStart()
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

        rb.isKinematic = true;
        isBreak = false;
        isStop = false;

        CalculationCourse();

        StartCoroutine(PlayerMovement());
    }

    IEnumerator PlayerMovement()
    {
        yield return new WaitForFixedUpdate();
        for (int i = 0; i <= FCIInfo.Count; i++)
        {
            var f = FCIInfo[i];
            float elapsed = 0.0f;
            while (elapsed < f.inputTime)
            {
                dt = Mathf.Min(Time.fixedDeltaTime, f.inputTime - elapsed);

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

    IEnumerator restartAI()
    {
        FCIInfo.Clear();

        rb.isKinematic = true;
        isBreak = false;
        isStop = false;

        bool isRunning = false;
        
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.z);
        Vector2[] playerAxes = new Vector2[2];
        playerAxes[0] = new Vector2(transform.right.x, transform.right.z);
        playerAxes[1] = new Vector2(transform.forward.x, transform.forward.z);
        float playerAngle = transform.eulerAngles.y;

        for(int i = 0; i <= vehicles.Count; i++)
        {    
            Vector2 vehiclePos = new Vector2(vehicles[i].transform.position.x, vehicles[i].transform.position.z);
            if (i > 0)
            {
                GameObject absCenter = vehicles[i - 1];
                GameObject absTo = vehicles[i];
                vehiclePos.y = CR.fVP.y + Mathf.abs(absCenter.transform.position.z - absTo.transform.position.z);
            }
            Vector2[] vehicleAxes = new Vector2[2];
            vehicleAxes[0] = new Vector2(vehicles[i].transform.right.x, vehicles[i].transform.right.z);
            vehicleAxes[1] = new Vector2(vehicles[i].transform.forward.x, vehicles[i].transform.forward.z);

            if (i == 0) 
            CR = new CalculationResult
            (playerPos, playerAxes, playerAngle, vehiclePos, vehicleAxes);
            else
            CR = new CalculationResult
            (CR.fPP, CR.fPAx, CR.fPA, vehiclePos, vehicleAxes);

            CalculationCourse();
            if (!isRunning)
            {
                StartCoroutine(PlayerMovement());
                isRunning = true;
            }
            yield return null;
        }
    }

    private void ResetAI()
    {
        transform.position = new Vector3(5.0f, 0.0f, -53.0f);
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (isReset)
        {
            gameObject.SetActive(true);
            isReset = false;
            Startcoroutine(restartAI);
        }
        else InitStart();
    }

    private void CalculationCourse()
    {
        int futureProcess = 0;
        int futureKey = 0;

        BoxCollider vehicleBox = newVehicle.GetComponent<BoxCollider>();

        while (!CR.isEnd && !isBreak)
        {
            List<PairList> nextPL = new List<PairList>();
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

                new{MinX = 6.5f, MaxX = 8.5f, MinAngle = 300.0f, MaxAngle = 360.0f, Target = filter.easy},
                new{MinX = 6.5f, MaxX = 8.5f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 340.0f, MaxAngle = 30.0f, Target = filter.easy},
                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 320.0f, MaxAngle = 85.0f, Target = filter.normal},
                new{MinX = -2.5f, MaxX = 0.0f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard},

                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 340.0f, MaxAngle = 40.0f, Target = filter.easy},
                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = filter.normal},
                new{MinX = -5.0f, MaxX = -2.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard},

                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 0.0f, MaxAngle = 45.0f, Target = filter.easy},
                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 335.0f, MaxAngle = 360.0f, Target = filter.normal},
                new{MinX = -6.5f, MaxX = -5.0f, MinAngle = 275.0f, MaxAngle = 85.0f, Target = filter.hard},

                new{MinX = -8.5f, MaxX = -6.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = filter.easy},
                new{MinX = -8.5f, MaxX = -6.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = filter.hard}
            };
            bool angleRange(float angle, float min, float max)
            {
                if (min < max) return angle >= min && angle <= max;
                else return angle >= min || angle <= max;
            }

            futureProcess = 1;
            futureKey = 1;
            for (int i = 1; i < 51; i++)
            {
                float time = i * 0.02f;

                Vector2 playerPos = CR.fPP + CR.fPAx[1] * (playerSpeed * time);
                Vector2 vehiclePos = CR.fVP + CR.fVAx[1] * (vehicleSpeed * time);

                CalculationResult nextCR = new CalculationResult
                (playerPos, CR.fPAx, CR.fPAn, vehiclePos, CR.fVAx);

                OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, CR.fPAx, playerBox);
                OBB2D_XZ vehicleOBB = new OBB2D_XZ(vehiclePos, CR.fVAx, vehicleBox);

                bool isCollision = SAT2D_XZ.CheckOBBvsOBB(playerOBB, vehicleOBB);

                if (nextCR.isValid && !isCollision)
                {
                    foreach (var p in useLoop)
                    {
                        if (i == p)
                        {
                            FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);
                            nextPL.Add(new PairList(nextCR, nextFCI));

                            Vector3 aaa = new Vector3(nextCR.fPP.x, transform.position.y, nextCR.fPP.y);
                            Quaternion bbb = Quaternion.Euler(0, nextCR.fPAn, 0);
                            //Instantiate(cube, aaa, bbb);
                            Vector3 ccc = new Vector3(nextCR.fVP.x, newVehicle.transform.position.y, nextCR.fVP.y);
                            //Instantiate(cube1, ccc, newVehicle.transform.rotation);

                            break;
                        }
                    }
                }
                else
                {
                    if (isCollision)
                    {
                        nextPL.Clear();
                        Vector3 sss = new Vector3(playerPos.x, transform.position.y, playerPos.y);
                        Quaternion ttt = Quaternion.Euler(0, nextCR.fPAn, 0);
                        //Instantiate(cube3, sss, ttt);
                        Vector3 fff = new Vector3(vehiclePos.x, newVehicle.transform.position.y + 1f, vehiclePos.y);
                        //Instantiate(cube5, fff, newVehicle.transform.rotation);
                    }
                    break;
                }
            }
            foreach (var p in nextPL) PL.Add(p);
            nextPL.Clear();
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

                for (int i = 1; i < 26; i++)
                {
                    float time = i * 0.02f;

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

                    OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, playerAxes, playerBox);
                    OBB2D_XZ vehicleOBB = new OBB2D_XZ(vehiclePos, CR.fVAx, vehicleBox);

                    bool isCollision = SAT2D_XZ.CheckOBBvsOBB(playerOBB, vehicleOBB);

                    if (nextCR.isValid && !isCollision)
                    {
                        foreach (var p in useLoop)
                        {
                            if (i == p)
                            {
                                FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);
                                nextPL.Add(new PairList(nextCR, nextFCI));

                                Vector3 aaa = new Vector3(nextCR.fPP.x, transform.position.y, nextCR.fPP.y);
                                Quaternion bbb = Quaternion.Euler(0, nextCR.fPAn, 0);
                                //Instantiate(cube, aaa, bbb);
                                Vector3 ccc = new Vector3(nextCR.fVP.x, newVehicle.transform.position.y, nextCR.fVP.y);
                                //Instantiate(cube1, ccc, newVehicle.transform.rotation);

                                break;
                            }
                        }
                    }
                    else
                    {
                        if (isCollision)
                        {
                            nextPL.Clear();
                            Vector3 sss = new Vector3(playerPos.x, transform.position.y, playerPos.y);
                            Quaternion ttt = Quaternion.Euler(0, angleDeg, 0);
                            //Instantiate(cube3, sss, ttt);
                            Vector3 fff = new Vector3(vehiclePos.x, newVehicle.transform.position.y + 1f, vehiclePos.y);
                            //Instantiate(cube5, fff, newVehicle.transform.rotation);
                        }
                        break;
                    }
                }
                foreach (var p in nextPL) PL.Add(p);
                nextPL.Clear();
            }

            if (PL.Count != 0)
            {
                Vector3 aaa = new Vector3(CR.fPP.x, transform.position.y, CR.fPP.y);
                Quaternion bbb = Quaternion.Euler(0, CR.fPAn, 0);
                //Instantiate(cube4, aaa, bbb);
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
                if (CR.isEnd)
                {
                    Vector3 zzz = new Vector3(CR.fPP.x, transform.position.y, CR.fPP.y);
                    Quaternion vvv = Quaternion.Euler(0, CR.fPAn, 0);
                    //Instantiate(cube6, zzz, vvv);
                    Vector3 ccc = new Vector3(CR.fVP.x, newVehicle.transform.position.y, CR.fVP.y);
                    //Instantiate(cube6, ccc, newVehicle.transform.rotation);
                }
            }
            if (PL.Count == 0)
            {
                isBreak = true;

                Vector3 aaa = new Vector3(CR.fPP.x, transform.position.y + 1f, CR.fPP.y);
                Quaternion bbb = Quaternion.Euler(0, CR.fPAn, 0);
                //Instantiate(cube2, aaa, bbb);
            }
        }
        isStart = false;
    }
}
