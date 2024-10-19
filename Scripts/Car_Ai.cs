using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Car_Ai : MonoBehaviour
{
    public AI carNavigation; // ������ �� ������, ������� ���������� NavMeshAgent
    public CarController carController; // ������ �� ������ ���������� �������
    public Learning learningSystem; // ������ �� ������� ��������

    // ��������� ���������
    public float sensorLength = 5f; // ����� �������� ��� ����������� �����������
    public LayerMask obstacleLayer; // ���� ��� ����������� ����������� (����������)
    public float rivalAvoidanceRadius = 10f; // ������ ��� ����������� ��������� ����� � ����� "rival"
    public float collisionAvoidanceStrength = 1.5f; // ���� ��������� ������������

    // ��������� ������
    public float checkPositionInterval = 0.5f; // ��������� �������� ���������� ������� ������� ��� ����� ������� �������
    private float lastPositionCheckTime;

    public enum CarState { Leading, Middle, Last }
    public CarState currentState;

    void Start()
    {
        // �������� � �������� ������ ��������
        learningSystem.StartRace();

        // ��������� ������ �������
        CheckPosition();
    }

    void Update()
    {
        float currentSpeed = carController.GetSpeed();
        bool isOvertake = CheckForOvertake();
        bool isCollision = CheckForCollision();

        // ��������� ������ �������� �� ����� �����
        learningSystem.UpdateRaceData(currentSpeed, isOvertake, isCollision);

        // ��������� ������ ������ checkPositionInterval ������
        if (Time.time - lastPositionCheckTime > checkPositionInterval)
        {
            CheckPosition();
            lastPositionCheckTime = Time.time;
        }

        // � ����������� �� �������� ��������� ���������� �������� ����� ���������
        switch (currentState)
        {
            case CarState.Leading:
                LeadingBehavior();
                break;
            case CarState.Middle:
                MiddleBehavior();
                break;
            case CarState.Last:
                LastBehavior();
                break;
        }
    }

    void EndRace()
    {
        // ��������� ����� � ��������� ������
        learningSystem.EndRace();
    }

    // ����� ��� �������� ������� ������� ���������� ������������ ����������
    void CheckPosition()
    {
        // ������� ���� ���������� � ����� "rival"
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        int numRivalsAhead = 0;
        int numRivalsBehind = 0;

        foreach (GameObject rival in rivals)
        {
            Vector3 rivalPosition = rival.transform.position;
            Vector3 directionToRival = rivalPosition - transform.position;

            // ���������� ����������� �������� ��� �����������, ��� ��������� ��������
            if (Vector3.Dot(transform.forward, directionToRival) > 0)
            {
                numRivalsAhead++; // �������� �������
            }
            else
            {
                numRivalsBehind++; // �������� ������
            }
        }

        // ����������� ������� ������� (��������, �� ������� ����� ��� ���������)
        if (numRivalsAhead == 0)
        {
            currentState = CarState.Leading; // ���������� �������
        }
        else if (numRivalsBehind == 0)
        {
            currentState = CarState.Last; // ��������� �������
        }
        else
        {
            currentState = CarState.Middle; // ������� �������
        }
    }

    // ��������� � ���������� �������
    void LeadingBehavior()
    {
        // ��������� ������������ ��������
        carController.GoForward(1.2f); // ��������� ��������� ��� ���������� �������

        // ������ �� �����������
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;
            float distanceToRival = rivalDirection.magnitude;

            // ���� �������� ������, ���������� �������� �������� �������
            if (distanceToRival < 3f)
            {
                carController.GoForward(1.5f); // ������������ ���������
            }
        }
    }

    // ��������� �� ������� �������
    void MiddleBehavior()
    {
        // ���������� ����, �� � ����� ����������� �������
        carController.GoForward(1f); // ��������� ���������

        // ��������� �� ������� ���������� ������� ��� ������
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;

            // ���� �������� ������ � �������, �������� ���������� ��������
            if (Vector3.Dot(transform.forward, rivalDirection) > 0 && rivalDirection.magnitude < 5f)
            {
                carController.GoForward(1.3f); // ����������� �������� ��� ������
            }
        }
    }

    // ����������� ��������� � ��������� �������
    void LastBehavior()
    {
        // ��������� �� ���������, ��� ����� ����������� ��������
        carController.GoForward(1.5f); // ������������ ���������

        // ����������� ���������: �����, �����
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;

            // ���� �������� ������, �������� ��� ��������
            if (rivalDirection.magnitude < 3f)
            {
                carController.Handbrake(); // ������ ������������ ��������: ����� �������� ��� ������
            }
        }
    }

    // �������� �� �����
    bool CheckForOvertake()
    {
        // ������ �������� ������ ����������
        return false; // ���������� true, ���� ��� �����
    }

    // �������� �� ������������
    bool CheckForCollision()
    {
        // ������ �������� ������������
        return false; // ���������� true, ���� ���� ������������
    }
}

// ����� ��� �������� � ������ ������ � ����
public class LearningSystem : MonoBehaviour
{
    public string carName; // ��� ������ ��� ���������� �������������
    private string filePath; // ���� � ����� � �������
    private float totalRaceTime;
    private int overtakes;
    private int collisions;
    private float averageSpeed;

    private float speedSum = 0f;
    private int speedSamples = 0;

    void Start()
    {
        filePath = Application.persistentDataPath + "/" + carName + "_data.txt"; // ���� ��� ������ ������
        LoadData();
    }

    // ����� ��� �������� ������ �� �����
    void LoadData()
    {
        if (File.Exists(filePath))
        {
            string[] data = File.ReadAllLines(filePath);
            totalRaceTime = float.Parse(data[0]);
            overtakes = int.Parse(data[1]);
            collisions = int.Parse(data[2]);
            averageSpeed = float.Parse(data[3]);

            Debug.Log($"Loaded data for {carName}: Time={totalRaceTime}, Overtakes={overtakes}, Collisions={collisions}, AvgSpeed={averageSpeed}");
        }
        else
        {
            Debug.Log($"No data found for {carName}, starting fresh.");
        }
    }

    // ����� ��� ���������� ������ � ����
    void SaveData()
    {
        string[] data = {
            totalRaceTime.ToString(),
            overtakes.ToString(),
            collisions.ToString(),
            averageSpeed.ToString()
        };

        File.WriteAllLines(filePath, data);
        Debug.Log($"Saved data for {carName}");
    }

    // ����� ��� ���������� ������ �� ����� �����
    public void UpdateRaceData(float speed, bool isOvertake, bool isCollision)
    {
        totalRaceTime += Time.deltaTime;

        speedSum += speed;
        speedSamples++;

        if (isOvertake)
        {
            overtakes++;
        }

        if (isCollision)
        {
            collisions++;
        }

        // ���������� ���������� ������ �� ����� �����
        SaveData();
    }

    // ����� ��� ���������� �����
    public void EndRace()
    {
        if (speedSamples > 0)
        {
            averageSpeed = speedSum / speedSamples;
        }
        SaveData();
    }
}
