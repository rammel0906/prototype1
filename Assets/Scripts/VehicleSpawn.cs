using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class VehicleSpawn : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;
    public GameObject player3;

    public GameObject car;
    public GameObject ute;
    public GameObject van;
    public GameObject bus;
    public GameObject armorCar;

    public AIController scriptA;

    private List<GameObject> players = new List<GameObject>();

    public GameObject headPlayer;
    public GameObject headPlayerLog;

    public float spawnBaseZ;
    public float spawnBaseZLog;
    public float distance = 100.0f;

    public float newVehicleZ;
    private float[] intervalRange = { 30.0f, 60.0f, 85.0f };

    private GameObject vehicle;
    private Vector3 lanePos;
    private Quaternion laneRot = Quaternion.Euler(0, 180, 0);
    private GameObject newVehicle;

    private bool isWaiting = false;
    private bool isFixedSpawnPos = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        players.AddRange(new List<GameObject> { player1, player2, player3 }.Where(p => p != null));
        headPlayer = players.Where(p => p != null).OrderByDescending(p => p.transform.position.z).FirstOrDefault();

        if (headPlayer != null)
        {
            headPlayerLog = headPlayer;
            spawnBaseZ = headPlayer.transform.position.z + distance;
            SpawnVehicle();
        }
    }

    // Update is called once per frame
    void Update()
    {
        headPlayer = players.Where(p => p != null).OrderByDescending(p => Mathf.Round(p.transform.position.z * 10f) / 10f).FirstOrDefault();

        if (!isFixedSpawnPos)
        {
            if (headPlayer != null)
            {
                spawnBaseZ = headPlayer.transform.position.z + distance;
            }
        }

        if (headPlayer == headPlayerLog)
        {
            spawnBaseZLog = spawnBaseZ;
        }
        else if (!isWaiting)
        {
            Debug.Log("生成Pos更新中...");
            isWaiting = true;
        }

        if (isWaiting && spawnBaseZ >= spawnBaseZLog)
        {
            headPlayerLog = headPlayer;
            Debug.Log("生成Pos更新完了");
            isWaiting = false;
        }

        if (newVehicle != null)
        {
            newVehicleZ = spawnBaseZ - newVehicle.transform.position.z;

            if (newVehicleZ >= intervalRange[Random.Range(0, intervalRange.Length)])
            {
                SpawnVehicle();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.gameObject == headPlayer)
            {
                players.Remove(other.gameObject);
                headPlayer = players.Where(p => p != null).OrderByDescending(p => Mathf.Round(p.transform.position.z * 10f) / 10f).FirstOrDefault();
                headPlayerLog = headPlayer;
                isFixedSpawnPos = true;
            }
        }
    }

    private void SpawnVehicle()
    {
        int vehicleNumber = Random.Range(1, 101);
        int laneNumber = Random.Range(1, 101);

        if (vehicleNumber <= 30) // 1～30 30%
        {
            vehicle = car;
        }
        else if (vehicleNumber <= 55) // 31～55 25%
        {
            vehicle = ute;
        }
        else if (vehicleNumber <= 72) // 56～72 17%
        {
            vehicle = van;
        }
        else if (vehicleNumber <= 90) // 73～90 18%
        {
            vehicle = bus;
        }
        else // 89～100 10%
        {
            vehicle = armorCar;
        }

        if (laneNumber <= 20) // 1～20 20%
        {
            lanePos = new Vector3(-7.5f, 0f, spawnBaseZ);
        }
        else if (laneNumber <= 40) // 21～40 20%
        {
            lanePos = new Vector3(-3.75f, 0f, spawnBaseZ);
        }
        else if (laneNumber <= 60) // 41～60 20%
        {
            lanePos = new Vector3(0f, 0f, spawnBaseZ);
        }
        else if (laneNumber <= 80) // 61～80 20%
        {
            lanePos = new Vector3(3.75f, 0f, spawnBaseZ);
        }
        else // 81～100 20%
        {
            lanePos = new Vector3(7.5f, 0f, spawnBaseZ);
        }
        newVehicle = Instantiate(vehicle, lanePos, laneRot);
        newVehicle.AddComponent<VehicleController>().scriptA = scriptA;

        Vector2 newVehiclePos = new Vector2(newVehicle.transform.position.x, newVehicle.transform.position.z);
        if (scriptA.newVehicle != null && !isRestarting)
        {
            newVehiclePos.y = scriptA.newVehicle.transform.position.z + Mathf.Abs(scriptA.newVehicle.transform.position.z - newVehiclePos.y);
            scriptA.newVehiclePos = newVehiclePos;
        }
        else if (isRestarting)
        {
            int index = scriptA.RL.Count - 1;
            newVehiclePos.y = scriptA.RL.vehiclesPos[index].y + Mathf.Abs(scriptA.RL.vehiclesPos[index].y - newVehiclePos.y);
            Vector2[] newVehicleAxes = new Vector2[2];
            newVehicleAxes[0] = new Vector2(newVehicle.transform.right.x, newVehicle.transform.right.z);
            newVehicleAxes[1] = new Vector2(newVehicle.transform.forward.x, newVehicle.transform.forward.z);
            
            scriptA.RL.Add(newVehiclePos, newVehicleAxes);
        }

        scriptA.newVehicle = newVehicle;
        scriptA.vehicles.RemoveAll(objects => objects == null);
        scriptA.vehicles.Add(newVehicle);
        scriptA.isStart = true;
    }
}
