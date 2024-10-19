using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Learning : MonoBehaviour
{
    public string carName; // ��� ������ ��� ���������� �������������
    private string filePath; // ���� � ����� � �������
    private float totalRaceTime;
    private int overtakes;
    private int collisions;
    private float averageSpeed;

    private float speedSum = 0f;
    private int speedSamples = 0;

    private const float targetSpeed = 120f;  // ������� �������� ��� �����
    private const int collisionLimit = 3;   // ����� ������������ ��� ������������ �����

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

    // ����� ��� ������ ����� �����
    public void StartRace()
    {
        // ���������� ������ ����� ��� ����� ������
        totalRaceTime = 0f;
        overtakes = 0;
        collisions = 0;
        speedSum = 0f;
        speedSamples = 0;
        Debug.Log("Race started, data reset.");
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

    // ����� ��� ��������� ����� �������� � ����������� �� ������
    public void AdaptDrivingStyle(CarController carController)
    {
        // ���� ������� �������� ���� ������� � ����������� �������������
        if (averageSpeed < targetSpeed)
        {
            carController.maxSpeed += 10;  // ����������� ������������ ��������
            carController.accelerationMultiplier += 1; // ����������� ���������
            Debug.Log("����������� ������������� ��� ���������� ������������ ��������.");
        }

        // ���� ���� ������� � ������ ����� ����� ������� �������� ��������
        if (overtakes < 3)
        {
            carController.maxSpeed += 5;  // �������������� ���������� �������� ��� �������
            Debug.Log("����������� �������� ��� �������� �������.");
        }

        // ���� ���� ����� ������������ � ������� �������������
        if (collisions > collisionLimit)
        {
            carController.maxSpeed -= 5; // ��������� ������������ ��������
            carController.accelerationMultiplier -= 1; // ������� ���������
            Debug.Log("������� ������������� ��-�� ������ ������������.");
        }
    }
}
