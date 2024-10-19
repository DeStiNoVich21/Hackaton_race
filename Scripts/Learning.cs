using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Learning : MonoBehaviour
{
    public string carName; // Имя машины или уникальный идентификатор
    private string filePath; // Путь к файлу с данными
    private float totalRaceTime;
    private int overtakes;
    private int collisions;
    private float averageSpeed;

    private float speedSum = 0f;
    private int speedSamples = 0;

    private const float targetSpeed = 120f;  // Целевая скорость для гонки
    private const int collisionLimit = 3;   // Лимит столкновений для агрессивного стиля

    void Start()
    {
        filePath = Application.persistentDataPath + "/" + carName + "_data.txt"; // Файл для каждой машины
        LoadData();
    }

    // Метод для загрузки данных из файла
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

    // Метод для начала новой гонки
    public void StartRace()
    {
        // Сбрасываем данные гонки для новой сессии
        totalRaceTime = 0f;
        overtakes = 0;
        collisions = 0;
        speedSum = 0f;
        speedSamples = 0;
        Debug.Log("Race started, data reset.");
    }

    // Метод для сохранения данных в файл
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

    // Метод для обновления данных во время гонки
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

        // Постоянное сохранение данных во время гонки
        SaveData();
    }

    // Метод для завершения гонки
    public void EndRace()
    {
        if (speedSamples > 0)
        {
            averageSpeed = speedSum / speedSamples;
        }
        SaveData();
    }

    // Метод для адаптации стиля вождения в зависимости от данных
    public void AdaptDrivingStyle(CarController carController)
    {
        // Если средняя скорость ниже целевой — увеличиваем агрессивность
        if (averageSpeed < targetSpeed)
        {
            carController.maxSpeed += 10;  // Увеличиваем максимальную скорость
            carController.accelerationMultiplier += 1; // Увеличиваем ускорение
            Debug.Log("Увеличиваем агрессивность для достижения максимальной скорости.");
        }

        // Если мало обгонов — машина будет более активно пытаться обгонять
        if (overtakes < 3)
        {
            carController.maxSpeed += 5;  // Дополнительное увеличение скорости для обгонов
            Debug.Log("Увеличиваем скорость для активных обгонов.");
        }

        // Если было много столкновений — снижаем агрессивность
        if (collisions > collisionLimit)
        {
            carController.maxSpeed -= 5; // Уменьшаем максимальную скорость
            carController.accelerationMultiplier -= 1; // Снижаем ускорение
            Debug.Log("Снижаем агрессивность из-за частых столкновений.");
        }
    }
}
