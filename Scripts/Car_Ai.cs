using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Car_Ai : MonoBehaviour
{
    public AI carNavigation; // Ссылка на скрипт, который использует NavMeshAgent
    public CarController carController; // Ссылка на скрипт управления машиной
    public Learning learningSystem; // Ссылка на систему обучения

    // Параметры окружения
    public float sensorLength = 5f; // Длина сенсоров для обнаружения препятствий
    public LayerMask obstacleLayer; // Слой для определения препятствий (ограждения)
    public float rivalAvoidanceRadius = 10f; // Радиус для определения ближайших машин с тэгом "rival"
    public float collisionAvoidanceStrength = 1.5f; // Сила избегания столкновений

    // Параметры машины
    public float checkPositionInterval = 0.5f; // Уменьшили интервал обновления анализа позиции для более быстрой реакции
    private float lastPositionCheckTime;

    public enum CarState { Leading, Middle, Last }
    public CarState currentState;

    void Start()
    {
        // Начинаем с загрузки данных обучения
        learningSystem.StartRace();

        // Начальный анализ позиции
        CheckPosition();
    }

    void Update()
    {
        float currentSpeed = carController.GetSpeed();
        bool isOvertake = CheckForOvertake();
        bool isCollision = CheckForCollision();

        // Обновляем данные обучения во время гонки
        learningSystem.UpdateRaceData(currentSpeed, isOvertake, isCollision);

        // Выполняем анализ каждые checkPositionInterval секунд
        if (Time.time - lastPositionCheckTime > checkPositionInterval)
        {
            CheckPosition();
            lastPositionCheckTime = Time.time;
        }

        // В зависимости от текущего состояния автомобиля выбираем режим поведения
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
        // Завершаем гонку и сохраняем данные
        learningSystem.EndRace();
    }

    // Метод для проверки текущей позиции автомобиля относительно соперников
    void CheckPosition()
    {
        // Находим всех соперников с тэгом "rival"
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        int numRivalsAhead = 0;
        int numRivalsBehind = 0;

        foreach (GameObject rival in rivals)
        {
            Vector3 rivalPosition = rival.transform.position;
            Vector3 directionToRival = rivalPosition - transform.position;

            // Используем направление движения для определения, где находится соперник
            if (Vector3.Dot(transform.forward, directionToRival) > 0)
            {
                numRivalsAhead++; // Соперник впереди
            }
            else
            {
                numRivalsBehind++; // Соперник позади
            }
        }

        // Определение текущей позиции (лидирует, на среднем месте или последний)
        if (numRivalsAhead == 0)
        {
            currentState = CarState.Leading; // Лидирующая позиция
        }
        else if (numRivalsBehind == 0)
        {
            currentState = CarState.Last; // Последняя позиция
        }
        else
        {
            currentState = CarState.Middle; // Средняя позиция
        }
    }

    // Поведение в лидирующей позиции
    void LeadingBehavior()
    {
        // Сохраняем максимальную скорость
        carController.GoForward(1.2f); // Увеличили ускорение для лидирующей позиции

        // Следим за соперниками
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;
            float distanceToRival = rivalDirection.magnitude;

            // Если соперник близко, агрессивно пытаемся удержать позицию
            if (distanceToRival < 3f)
            {
                carController.GoForward(1.5f); // Максимальное ускорение
            }
        }
    }

    // Поведение на средней позиции
    void MiddleBehavior()
    {
        // Методичная езда, но с более агрессивным обгоном
        carController.GoForward(1f); // Умеренное ускорение

        // Проверяем на наличие соперников впереди для обгона
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;

            // Если соперник близко и впереди, пытаемся агрессивно обогнать
            if (Vector3.Dot(transform.forward, rivalDirection) > 0 && rivalDirection.magnitude < 5f)
            {
                carController.GoForward(1.3f); // Увеличиваем скорость для обгона
            }
        }
    }

    // Агрессивное поведение в последней позиции
    void LastBehavior()
    {
        // Ускорение до максимума, еще более агрессивное вождение
        carController.GoForward(1.5f); // Максимальное ускорение

        // Агрессивное поведение: обгон, таран
        GameObject[] rivals = GameObject.FindGameObjectsWithTag("rival");
        foreach (GameObject rival in rivals)
        {
            Vector3 rivalDirection = rival.transform.position - transform.position;

            // Если соперник близко, пытаемся его таранить
            if (rivalDirection.magnitude < 3f)
            {
                carController.Handbrake(); // Пример агрессивного действия: резко тормозим для тарана
            }
        }
    }

    // Проверка на обгон
    bool CheckForOvertake()
    {
        // Логика проверки обгона соперников
        return false; // Возвращает true, если был обгон
    }

    // Проверка на столкновение
    bool CheckForCollision()
    {
        // Логика проверки столкновений
        return false; // Возвращает true, если было столкновение
    }
}

// Класс для обучения и записи данных в файл
public class LearningSystem : MonoBehaviour
{
    public string carName; // Имя машины или уникальный идентификатор
    private string filePath; // Путь к файлу с данными
    private float totalRaceTime;
    private int overtakes;
    private int collisions;
    private float averageSpeed;

    private float speedSum = 0f;
    private int speedSamples = 0;

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
}
