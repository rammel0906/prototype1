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

    public int returnCount = 0;

    public int FCIIndex = 0;
    public float elapsed = 0.0f;
    public float dt;

    public GameObject cube;
    public GameObject cube1;
    public GameObject cube2;
    public GameObject cube3;
    public GameObject cube4;
    public GameObject cube5;
    public GameObject cube42;
    public GameObject cube52;

    public bool isStart = false;
    public bool isBreak = false;
    public bool isStop = false;
    public bool isReset = false;
    public bool isRestarting = false;

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

        public float MVT;
        public float diffMVT; // differenceMVT

        public bool isValid;
        public bool isEnd;

        public CalculationResult
        (Vector2 playerPos, Vector2[] playerAxes, float playerAngle, Vector2 vehiclePos, Vector2[] vehicleAxes, 
         float mvt, float diffmvt)
        {
            if (playerPos.x >= -8.5f && playerPos.x <= 8.5f && (playerAngle < 85.0f || playerAngle > 275.0f) && 
               (playerPos.y <= vehiclePos.y || Mathf.Abs(playerPos.y - vehiclePos.y) <= 7.0f))
            {
                fPP = playerPos;
                fPAx = playerAxes;
                fPAn = playerAngle;

                fVP = vehiclePos;
                fVAx = vehicleAxes;

                MVT = mvt;
                diffMVT = diffmvt;

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

                MVT  = 0.0f;
                diffMVT = 0.0f;

                isValid = false;
                isEnd = false;
            }
        }
    }
    public struct OBB2D_XZ
    {
        public Vector2 center;
        public Vector2[] axes;
        public Vector2 extents;

        public OBB2D_XZ(Vector2 CRCenter,Vector2[] CRAxes, BoxCollider box)
        {
            center = CRCenter;
            axes = new Vector2[2];
            axes[0] = CRAxes[0];
            axes[1] = CRAxes[1];
            Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
            extents = new Vector2((size.x * 1.15f) * 0.5f, (size.z * 1.15f) * 0.5f);
        }
    }
    public static class SAT2D_XZ
    {
        public static float CheckOBBvsOBB(OBB2D_XZ a, OBB2D_XZ b)
        {
            float MVT = float.MaxValue;

            Vector2[] axesToTest = new Vector2[4];
            axesToTest[0] = a.axes[0];
            axesToTest[1] = a.axes[1];
            axesToTest[2] = b.axes[0];
            axesToTest[3] = b.axes[1];

            foreach (var rawAxis in axesToTest)
            {
                Vector2 axis = rawAxis.normalized;

                float minA, maxA;
                ProjectOBBOnAxis(a, axis, out minA, out maxA);

                float minB, maxB;
                ProjectOBBOnAxis(b, axis, out minB, out maxB);

                float overlap = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);

                if (overlap < 0.0f) return overlap;

                if (overlap < MVT) MVT = overlap;
            }
            return MVT;
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
        public CalculationResult CR;
        public FutureCourseInfo FCI;

        public PairList(CalculationResult cr, FutureCourseInfo fci)
        {
            CR = cr;
            FCI = fci;
        }
    }
    [System.Serializable]
    public struct ReturnList
    {
        public PairList PL;
        public List<int> process;

        public ReturnList(PairList pl, List<int> selectProcess)
        {
            PL = pl;
            process = new List<int>(selectProcess);
        }
    }
    public class avoidanceFilter
    {
        public List<PairList> fast = new List<PairList>();
        public List<PairList> medium = new List<PairList>();
        public List<PairList> slow = new List<PairList>();
    }
    public class difficultyFilter
    {
        public avoidanceFilter easy = new avoidanceFilter();
        public avoidanceFilter normal = new avoidanceFilter();
        public avoidanceFilter hard = new avoidanceFilter();
    }
    public struct RestartList
    {
        public Vector2 vehiclesPos;
        public Vector2[] vehiclesAxes;

        public RestartList(Vector2 pos, Vector2[] axes)
        {
            vehiclesPos = pos;
            vehiclesAxes = axes;
        }
    }

    public CalculationResult CR;
    public List<FutureCourseInfo> FCI = new List<FutureCourseInfo>();
    public List<ReturnList> RTL = new List<ReturnList>();

    public List<RestartList> RSL = new List<RestartList>();

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
        if (isStart && !isBreak && !isRestarting)
        {
            BoxCollider vehicleBox = newVehicle.GetComponent<BoxCollider>();

            Vector2[] vehicleAxes = new Vector2[2];
            vehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
            vehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);

            Vector2 correctedVP = new Vector2(newVehiclePos.x, CR.fPP.y);

            OBB2D_XZ playerOBB = new OBB2D_XZ(CR.fPP, CR.fPAx, playerBox);
            OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, vehicleAxes, vehicleBox);

            float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

            CR = new CalculationResult
            (CR.fPP, CR.fPAx, CR.fPAn, newVehiclePos, vehicleAxes, MVT, MVT);

            CalculationCourse();
        }

        if (isStop) transform.Translate(Vector3.forward * (playerSpeed * Time.deltaTime));

        if (transform.position.y <= -7.0f) ResetAI();
    }

    void FixedUpdate()
    {
        if (!isStop)PlayerMovement(Time.fixedDeltaTime);
    }

    void OnDisable()
    {
        isReset = true;
        Invoke("ResetAI", 2.0f);
    }

    public void InitStart()
    {
        BoxCollider vehicleBox = newVehicle.GetComponent<BoxCollider>();

        Vector2 playerPos = new Vector2(transform.position.x, transform.position.z);
        Vector2[] playerAxes = new Vector2[2];
        playerAxes[0] = new Vector2(transform.right.x, transform.right.z);
        playerAxes[1] = new Vector2(transform.forward.x, transform.forward.z);
        float playerAngle = transform.eulerAngles.y;

        Vector2 vehiclePos = new Vector2(newVehicle.transform.position.x, newVehicle.transform.position.z);
        Vector2[] vehicleAxes = new Vector2[2];
        vehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
        vehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);

        Vector2 correctedVP = new Vector2(vehiclePos.x, playerPos.y);

        Debug.Log("player:" + playerPos + "   vehicle:" + correctedVP);

        OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, playerAxes, playerBox);
        OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, vehicleAxes, vehicleBox);

        float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

        CR = new CalculationResult
        (playerPos, playerAxes, playerAngle, vehiclePos, vehicleAxes, MVT, MVT);

        FCI.Clear();

        rb.isKinematic = true;
        isBreak = false;
        isStop = false;

        CalculationCourse();
    }

    private void PlayerMovement(float dt)
    {
        float remaining = dt;

        while (remaining > 0.0f)
        {
            if (FCIIndex >= FCI.Count)
            {
                rb.isKinematic = false;
                isStop = true;
                return;
            }
            var f = FCI[FCIIndex];

            float toInputEnd = f.inputTime - elapsed;

            float stepTime = Mathf.Min(remaining, toInputEnd);

            transform.Translate(Vector3.forward * (playerSpeed * stepTime));

            if (f.processSelected == 2 || f.processSelected == 3)
            {
                transform.Rotate(Vector3.up * ((turnSpeed * f.keyInput) * stepTime));
            }

            elapsed += stepTime;
            remaining -= stepTime;

            if (elapsed >= f.inputTime)
            {
                if (FCIIndex < FCI.Count - 1)
                {
                    FCIIndex++;
                    elapsed = 0.0f;
                }
                else
                {
                    rb.isKinematic = false;
                    isStop = true;
                    return;
                }
            }
        }
    }

    private void ResetAI()
    {
        transform.position = new Vector3(5.0f, 0.0f, -60.0f);
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (isReset)
        {
            gameObject.SetActive(true);
            isReset = false;
        }
        foreach (var p in GameObject.FindGameObjectsWithTag("AAAA")) Destroy(p);
        StartCoroutine(RestartAI());
    }

    IEnumerator RestartAI()
    {
        isRestarting = true;
        bool isRunning = false;
        
        RSL.Clear();
        FCI.Clear();
        RTL.Clear();
        vehicles.RemoveAll(objects => objects == null);

        rb.isKinematic = true;
        isBreak = false;
        isStop = false;

        foreach (var p in vehicles)
        {
            Vector2 pos = new Vector2(p.transform.position.x, p.transform.position.z);
            Vector2[] axes = new Vector2[2];
            axes[0] = new Vector2(p.transform.right.x, p.transform.right.z);
            axes[1] = new Vector2(p.transform.forward.x, p.transform.forward.z);

            RSL.Add(new RestartList(pos, axes));
        }
        
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.z);
        Vector2[] playerAxes = new Vector2[2];
        playerAxes[0] = new Vector2(transform.right.x, transform.right.z);
        playerAxes[1] = new Vector2(transform.forward.x, transform.forward.z);
        float playerAngle = transform.eulerAngles.y;

        for(int i = 0; i < vehicles.Count; i++)
        {
            BoxCollider vehicleBox = vehicles[i].GetComponent<BoxCollider>();

            Vector2 vehiclePos = RSL[i].vehiclesPos;
            if (i > 0) // ñ¢óàåvéZÇ≈Ç‡é‘óÒÇÃé‘ä‘ãóó£Çà€éùÇ∑ÇÈÇΩÇﬂÇÃyï‚ê≥
            {
                Vector2 critenionAbs = RSL[i - 1].vehiclesPos;
                vehiclePos.y = CR.fVP.y + Mathf.Abs(critenionAbs.y - vehiclePos.y);
            }
            Vector2[] vehicleAxes = RSL[i].vehiclesAxes;

            if (i == 0)
            {
                Vector2 correctedVP = new Vector2(vehiclePos.x, playerPos.y);

                OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, playerAxes, playerBox);
                OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, vehicleAxes, vehicleBox);

                float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

                CR = new CalculationResult
                (playerPos, playerAxes, playerAngle, vehiclePos, vehicleAxes, MVT, MVT);
            }
            else
            {
                Vector2 correctedVP = new Vector2(vehiclePos.x, CR.fPP.y);

                OBB2D_XZ playerOBB = new OBB2D_XZ(CR.fPP, CR.fPAx, playerBox);
                OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, vehicleAxes, vehicleBox);

                float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

                CR = new CalculationResult
                (CR.fPP, CR.fPAx, CR.fPAn, vehiclePos, vehicleAxes, MVT, MVT);
            }

            CalculationCourse();
            if (!isRunning)
            {
                FCIIndex = 0;
                elapsed = 0.0f;
                dt = 0.0f;

                isRunning = true;
            }
            yield return null;
        }

        isRestarting = false;
    }

    private void CalculationCourse()
    {
        BoxCollider vehicleBox = newVehicle.GetComponent<BoxCollider>();

        CalculationResult tempCR = CR;

        Vector3 aaagh = new Vector3(CR.fPP.x, transform.position.y + 1, CR.fPP.y);
        Quaternion bbbgh = Quaternion.Euler(0, CR.fPAn, 0);
        Instantiate(cube4, aaagh, bbbgh);

        int futureProcess = 0;
        int futureKey = 0;

        int returnCount = 0;

        List<int> nextFutureProcess = new List<int> { 1, 2, 3 };

        while (!CR.isEnd && !isBreak)
        {
            List<PairList> nextPL = new List<PairList>();
            List<PairList> PL = new List<PairList>();

            //Debug.Log("<color=blue>äÓèÄMVT:</color>" + CR.MVT);

            bool overlapped = CR.MVT >= 0.0f;

            //Debug.Log("<color=blue>overlapped:</color>" + overlapped);

            if (nextFutureProcess.Contains(1))
            {
                futureProcess = 1;
                futureKey = 1;

                for (int i = 1; i < 51; i++)
                {
                    float time = i * 0.02f;

                    Vector2 playerPos = CR.fPP + CR.fPAx[1] * (playerSpeed * time);
                    Vector2 vehiclePos = CR.fVP + CR.fVAx[1] * (vehicleSpeed * time);

                    Vector2 correctedVP = new Vector2(vehiclePos.x, playerPos.y);

                    OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, CR.fPAx, playerBox);
                    OBB2D_XZ vehicleOBB = new OBB2D_XZ(vehiclePos, CR.fVAx, vehicleBox);
                    OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, CR.fVAx, vehicleBox);

                    bool isCollision = SAT2D_XZ.CheckOBBvsOBB(playerOBB, vehicleOBB) >= 0.0f;

                    float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

                    float diffMVT = CR.MVT - MVT;

                    bool isOverlap = MVT >= 0.0f;

                    bool ProcessBreak = !overlapped && isOverlap || isCollision;

                    //Debug.Log("1");
                    //Debug.Log("MVT:" + MVT + "  diffMVT" + diffMVT);

                    if (!overlapped && isOverlap) Debug.LogWarning("êiòHè„Ç…êNì¸");

                    CalculationResult nextCR = new CalculationResult
                    (playerPos, CR.fPAx, CR.fPAn, vehiclePos, CR.fVAx, MVT, diffMVT);
                    FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);

                    foreach (var p in useLoop)
                    {
                        if (i == p)
                        {
                            Vector3 aaa = new Vector3(nextCR.fPP.x, transform.position.y, nextCR.fPP.y);
                            Quaternion bbb = Quaternion.Euler(0, nextCR.fPAn, 0);
                            Instantiate(cube, aaa, bbb);
                            Vector3 ccc = new Vector3(nextCR.fVP.x, newVehicle.transform.position.y, nextCR.fVP.y);
                            Instantiate(cube1, ccc, newVehicle.transform.rotation);
                        }
                    }

                    if (nextCR.isValid && !ProcessBreak)
                    {
                        foreach (var p in useLoop)
                        {
                            if (i == p)
                            {
                                nextPL.Add(new PairList(nextCR, nextFCI));

                                break;
                            }
                        }
                    }
                    else
                    {
                        if (ProcessBreak || nextPL.Count == 0)
                        {
                            nextPL.Clear();

                            nextFutureProcess.Remove(1);
                            if (isCollision)
                            {
                                Vector3 sss = new Vector3(playerPos.x, transform.position.y, playerPos.y);
                                Quaternion ttt = Quaternion.Euler(0, nextCR.fPAn, 0);
                                Instantiate(cube3, sss, ttt);
                                Vector3 fff = new Vector3(vehiclePos.x, newVehicle.transform.position.y + 1f, vehiclePos.y);
                                Instantiate(cube5, fff, newVehicle.transform.rotation);
                            }
                        }

                        break;
                    }
                }
            }
            foreach (var p in nextPL) PL.Add(p);
            nextPL.Clear();

            for (int dir = 0; dir < 2; dir++)
            {
                if (dir == 0)
                {
                    if (nextFutureProcess.Contains(2))
                    {
                        futureProcess = 2;
                        futureKey = 1;
                    }
                    else continue;
                }
                else if (dir == 1)
                {
                    if (nextFutureProcess.Contains(3))
                    {
                        futureProcess = 3;
                        futureKey = -1;
                    }
                    else break;
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

                    Vector2 correctedVP = new Vector2(vehiclePos.x, playerPos.y);

                    OBB2D_XZ playerOBB = new OBB2D_XZ(playerPos, playerAxes, playerBox);
                    OBB2D_XZ vehicleOBB = new OBB2D_XZ(vehiclePos, CR.fVAx, vehicleBox);
                    OBB2D_XZ correctedVOBB = new OBB2D_XZ(correctedVP, CR.fVAx, vehicleBox);

                    bool isCollision = SAT2D_XZ.CheckOBBvsOBB(playerOBB, vehicleOBB) >= 0.0f;

                    float MVT = SAT2D_XZ.CheckOBBvsOBB(playerOBB, correctedVOBB);

                    float diffMVT = CR.MVT - MVT;

                    bool isOverlap = MVT >= 0.0f;

                    bool ProcessBreak = !overlapped && isOverlap || isCollision;

                    //if (dir == 0) Debug.Log("2");
                    //else Debug.Log("3");
                        //Debug.Log("MVT:" + MVT + "  diffMVT" + diffMVT);

                    if (!overlapped && isOverlap) Debug.LogWarning("êiòHè„Ç…êNì¸");

                    CalculationResult nextCR = new CalculationResult
                    (playerPos, playerAxes, angleDeg, vehiclePos, CR.fVAx, MVT, diffMVT);
                    FutureCourseInfo nextFCI = new FutureCourseInfo(futureProcess, futureKey, time);

                    foreach (var p in useLoop)
                    {
                        if (i == p)
                        {
                            Vector3 aaa = new Vector3(nextCR.fPP.x, transform.position.y, nextCR.fPP.y);
                            Quaternion bbb = Quaternion.Euler(0, nextCR.fPAn, 0);
                            Instantiate(cube, aaa, bbb);
                            Vector3 ccc = new Vector3(nextCR.fVP.x, newVehicle.transform.position.y, nextCR.fVP.y);
                            Instantiate(cube1, ccc, newVehicle.transform.rotation);
                        }
                    }

                    if (nextCR.isValid && !ProcessBreak)
                    {
                        foreach (var p in useLoop)
                        {
                            if (i == p)
                            {
                                nextPL.Add(new PairList(nextCR, nextFCI));

                                break;
                            }
                        }
                    }
                    else
                    {
                        if (ProcessBreak || nextPL.Count == 0)
                        {
                            nextPL.Clear();

                            if (dir == 0) nextFutureProcess.Remove(2);
                            else if (dir == 1) nextFutureProcess.Remove(3);

                            if (isCollision)
                            {
                                Vector3 sss = new Vector3(playerPos.x, transform.position.y, playerPos.y);
                                Quaternion ttt = Quaternion.Euler(0, nextCR.fPAn, 0);
                                Instantiate(cube3, sss, ttt);
                                Vector3 fff = new Vector3(vehiclePos.x, newVehicle.transform.position.y + 1f, vehiclePos.y);
                                Instantiate(cube5, fff, newVehicle.transform.rotation);
                            }
                        }

                        break;
                    }
                }
                foreach (var p in nextPL) PL.Add(p);
                nextPL.Clear();
            }

            if (PL.Count != 0)
            {
                difficultyFilter diffFilter = new difficultyFilter();

                var roadRules = new[]
                {
                new { MinX = 0.0f, MaxX = 2.5f, MinAngle = 330.0f, MaxAngle = 20.0f, Target = diffFilter.easy },
                new { MinX = 0.0f, MaxX = 2.5f, MinAngle = 300.0f, MaxAngle = 40.0f, Target = diffFilter.normal },
                new { MinX = 0.0f, MaxX = 2.5f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = diffFilter.hard },

                new { MinX = 2.5f, MaxX = 5.0f, MinAngle = 315.0f, MaxAngle = 20.0f, Target = diffFilter.easy },
                new { MinX = 2.5f, MaxX = 5.0f, MinAngle = 300.0f, MaxAngle = 360.0f, Target = diffFilter.normal },
                new { MinX = 2.5f, MaxX = 5.0f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = diffFilter.hard },

                new { MinX = 5.0f, MaxX = 6.5f, MinAngle = 320.0f, MaxAngle = 360.0f, Target = diffFilter.easy },
                new { MinX = 5.0f, MaxX = 6.5f, MinAngle = 0.0f, MaxAngle = 25.0f, Target = diffFilter.normal },
                new { MinX = 5.0f, MaxX = 6.5f, MinAngle = 275f, MaxAngle = 85.0f, Target = diffFilter.hard },

                new { MinX = 6.5f, MaxX = 8.5f, MinAngle = 300.0f, MaxAngle = 360.0f, Target = diffFilter.easy },
                new { MinX = 6.5f, MaxX = 8.5f, MinAngle = 0.0f, MaxAngle = 85.0f, Target = diffFilter.hard },

                new { MinX = -2.5f, MaxX = 0.0f, MinAngle = 340.0f, MaxAngle = 30.0f, Target = diffFilter.easy },
                new { MinX = -2.5f, MaxX = 0.0f, MinAngle = 320.0f, MaxAngle = 85.0f, Target = diffFilter.normal },
                new { MinX = -2.5f, MaxX = 0.0f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = diffFilter.hard },

                new { MinX = -5.0f, MaxX = -2.5f, MinAngle = 340.0f, MaxAngle = 40.0f, Target = diffFilter.easy },
                new { MinX = -5.0f, MaxX = -2.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = diffFilter.normal },
                new { MinX = -5.0f, MaxX = -2.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = diffFilter.hard },

                new { MinX = -6.5f, MaxX = -5.0f, MinAngle = 0.0f, MaxAngle = 45.0f, Target = diffFilter.easy },
                new { MinX = -6.5f, MaxX = -5.0f, MinAngle = 335.0f, MaxAngle = 360.0f, Target = diffFilter.normal },
                new { MinX = -6.5f, MaxX = -5.0f, MinAngle = 275.0f, MaxAngle = 85.0f, Target = diffFilter.hard },

                new { MinX = -8.5f, MaxX = -6.5f, MinAngle = 0.0f, MaxAngle = 60.0f, Target = diffFilter.easy },
                new { MinX = -8.5f, MaxX = -6.5f, MinAngle = 275.0f, MaxAngle = 360.0f, Target = diffFilter.hard }
                };
                var overlapRules = new[]
                {
                new { MinDiffMVT = 1.0f, MaxDiffMVT = float.PositiveInfinity, Target = 1 },
                new { MinDiffMVT = 0.4f, MaxDiffMVT = 1.0f, Target = 2 },
                new { MinDiffMVT = float.NegativeInfinity, MaxDiffMVT = 0.4f, Target = 3 },
                };
                bool angleRange(float angle, float min, float max)
                {
                    if (min < max) return angle >= min && angle <= max;
                    else return angle >= min || angle <= max;
                }

                var patterns = new[]
{
                    new { First = true, Second = false, Third = false },
                    new { First = false, Second = true, Third = false },
                    new { First = false, Second = false, Third = true },
                    new { First = true, Second = true, Third = false },
                    new { First = false, Second = true, Third = true },
                    new { First = true, Second = false, Third = true },
                    new { First = true,Second = true, Third = true }
                };
                var difficultyPro = new (int easyPro, int normalPro, int hardPro)[]
                {
                    (100, -1, -1),
                    (-1, 100, -1),
                    (-1, -1, 100),
                    (97, 100, -1),
                    (-1, 98, 100),
                    (98, -1, 100),
                    (90, 99, 100)
                };
                var easyAP = new (int FastPro, int MediumPro, int SlowPro)[] // easyAvoidanceProbability
                {
                    (100, -1, -1),
                    (-1, 100, -1),
                    (-1, -1, 100),
                    (80, 100, -1),
                    (-1, 90, 100),
                    (95, -1, 100),
                    (70, 90, 100)
                };
                var normalAP = new (int FastPro, int MediumPro, int SlowPro)[] // normalAvoidanceProbability
                {
                    (100, -1, -1),
                    (-1, 100, -1),
                    (-1, -1, 100),
                    (60, 100, -1),
                    (-1, 75, 100),
                    (80, -1, 100),
                    (65, 90, 100)
                };
                var hardAP = new (int FastPro, int MediumPro, int SlowPro)[] // hardAvoidanceProbability
                {
                    (100, -1, -1),
                    (-1, 100, -1),
                    (-1, -1, 100),
                    (20, 100, -1),
                    (-1, 30, 100),
                    (15, -1, 100),
                    (15, 35, 100)
                };

                foreach (var p in PL)
                {
                    foreach (var road in roadRules)
                    {
                        if (p.CR.fPP.x >= road.MinX && p.CR.fPP.x <= road.MaxX && angleRange(p.CR.fPAn, road.MinAngle, road.MaxAngle))
                        {
                            foreach (var overlap in overlapRules)
                            {
                                if (p.CR.diffMVT >= overlap.MinDiffMVT && p.CR.diffMVT <= overlap.MaxDiffMVT)
                                {
                                    switch (overlap.Target)
                                    {
                                        case 1:
                                            road.Target.fast.Add(p);
                                            break;
                                        case 2:
                                            road.Target.medium.Add(p);
                                            break;
                                        case 3:
                                            road.Target.slow.Add(p);
                                            break;
                                    }

                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                int diffPro = Random.Range(0, 101); // difficultyProbability
                int easyPro = 0; // easyProbability
                int normalPro = 0; // normalProbability
                int hardPro = 0; // hardProbability

                int avoidPro = Random.Range(0, 101); // avoidanceProbability
                int fastPro = 0; // fastProbability
                int mediumPro = 0; // mediumProbability
                int slowPro = 0; // slowProbability

                bool[] difficultyExists = new bool[3];
                difficultyExists[0] = diffFilter.easy.fast.Any() || diffFilter.easy.medium.Any() || diffFilter.easy.slow.Any();
                difficultyExists[1] = diffFilter.normal.fast.Any() || diffFilter.normal.medium.Any() || diffFilter.normal.slow.Any();
                difficultyExists[2] = diffFilter.hard.fast.Any() || diffFilter.hard.medium.Any() || diffFilter.hard.slow.Any();

                for (var i = 0; i < patterns.Length; i++)
                {
                    if (difficultyExists[0] == patterns[i].First && difficultyExists[1] == patterns[i].Second && difficultyExists[2] == patterns[i].Third)
                    {
                        (easyPro, normalPro, hardPro) = difficultyPro[i];

                        break;
                    }
                }

                var subject = diffFilter.easy;
                var proTable = easyAP;

                var TargetList = diffFilter.easy.fast;

                if (diffPro <= easyPro)
                {
                    subject = diffFilter.easy;
                    proTable = easyAP;
                }
                else if (diffPro <= normalPro)
                {
                    subject = diffFilter.normal;
                    proTable = normalAP;
                }
                else if (diffPro <= hardPro)
                {
                    subject = diffFilter.hard;
                    proTable = hardAP;
                }

                bool fast = subject.fast.Any();
                bool medium = subject.medium.Any();
                bool slow = subject.slow.Any();
                
                for (var i = 0; i < patterns.Length; i++)
                {
                    if (fast == patterns[i].First && medium == patterns[i].Second && slow == patterns[i].Third)
                    {
                        (fastPro, mediumPro, slowPro) = proTable[i];

                        break;
                    }
                }

                if (avoidPro <= fastPro) TargetList = subject.fast;
                else if (avoidPro <= mediumPro) TargetList = subject.medium;
                else if (avoidPro <= slowPro) TargetList = subject.slow;
                
                int randomindex = Random.Range(0, TargetList.Count);
                int index = TargetList.Select((x, i) => (x.CR.fPP.y, i)).OrderByDescending(p => p.y).Select(p => p.i).FirstOrDefault();
                CR = TargetList[index].CR;
                FCI.Add(TargetList[index].FCI);
                RTL.Add(new ReturnList(TargetList[index], nextFutureProcess));

                if (CR.isEnd)
                {
                    Vector3 aaafd = new Vector3(CR.fPP.x, transform.position.y, CR.fPP.y);
                    Quaternion bbbfd = Quaternion.Euler(0, CR.fPAn, 0);
                    //Instantiate(cube52, aaafd, bbbfd);
                    Vector3 fff = new Vector3(CR.fVP.x, newVehicle.transform.position.y, CR.fVP.y);
                    //Instantiate(cube52, fff, newVehicle.transform.rotation);
                }
                Vector3 aaa = new Vector3(CR.fPP.x, transform.position.y, CR.fPP.y);
                Quaternion bbb = Quaternion.Euler(0, CR.fPAn, 0);
                Instantiate(cube42, aaa, bbb);

                //Debug.Log("nextäÓèÄMVT:" + CR.MVT);
                //Debug.Log("ç∑ï™MVT:" + CR.diffMVT);

                nextFutureProcess = new List<int> { 1, 2, 3 };
            }
            else
            {
                if (returnCount < 6 && RTL.Count >= 2)
                {
                    returnCount++;
                    //Debug.Log("<color=green>ReturnCount:</color>" + returnCount);
                    while (nextFutureProcess.Count == 0)
                    {
                        int newRTLIndex = RTL.Count - 1;
                        int oldRTLIndex = RTL.Count - 2;

                        if (oldRTLIndex >= FCIIndex && !RTL[newRTLIndex].PL.CR.isEnd)
                        {
                            if (RTL[oldRTLIndex].PL.CR.isEnd) CR = tempCR;
                            else CR = RTL[oldRTLIndex].PL.CR;

                            nextFutureProcess = new List<int>(RTL[newRTLIndex].process);
                            //Debug.Log("newRTLIndex.process:" + string.Join(", ", nextFutureProcess) + "  Remove:" + RTL[newRTLIndex].PL.FCI.processSelected);
                            nextFutureProcess.Remove(RTL[newRTLIndex].PL.FCI.processSelected);
                            //if (nextFutureProcess.Count == 0) Debug.Log("<color=yellow>new nextFutureProcess:</color>" + string.Join(", ", nextFutureProcess));
                            //else Debug.Log("<color=blue>new nextFutureProcess:</color>" + string.Join(", ", nextFutureProcess));

                            RTL.RemoveAt(newRTLIndex);
                            FCI.RemoveAt(newRTLIndex);
                        }
                        else
                        {
                            //if (oldRTLIndex < FCIIndex) Debug.LogWarning("FCIIndexÇ™oldRTLIndexÇí¥Ç¶ÇΩÇΩÇﬂñﬂÇÍÇ‹ÇπÇÒ");
                            //if (RTL[newRTLIndex].PL.CR.isEnd) Debug.LogWarning("åªç›ÇÃåvéZÇ™ç≈èâÇÃCRÇÃÇΩÇﬂñﬂÇÍÇ‹ÇπÇÒ");
                            isBreak = true;
                            break;
                        }

                        Vector3 aaa = new Vector3(CR.fPP.x, transform.position.y + 1f, CR.fPP.y);
                        Quaternion bbb = Quaternion.Euler(0, CR.fPAn, 0);
                        //Instantiate(cube2, aaa, bbb);
                    }
                }
                else
                {
                    isBreak = true;

                    Vector3 aaa = new Vector3(CR.fPP.x, transform.position.y + 1f, CR.fPP.y);
                    Quaternion bbb = Quaternion.Euler(0, CR.fPAn, 0);
                    Instantiate(cube2, aaa, bbb);
                }
            }
        }
        isStart = false;
    }
}
