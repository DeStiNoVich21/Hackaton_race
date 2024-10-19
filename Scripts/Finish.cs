using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
    public bool firstPoint = true;

    void OnTriggerEnter(Collider other)
    {
        // Пытаемся получить компонент AI у объекта или его родительских объектов (включая родителя родителей)
        var car = other.GetComponentInParent<AI>();

        // Пытаемся получить систему обучения машины
        var learningSystem = other.GetComponentInParent<Learning>();

        if (car != null && learningSystem != null)
        {
            // Если это первый чекпоинт, переключаемся на следующий
            if (firstPoint)
            {
                car.first = true;
            }
            else
            {
                // Останавливаем текущее обучение для этой машины
                learningSystem.EndRace();

                // Сохраняем данные гонки и перезапускаем обучение для следующего этапа
                car.first = false;
                learningSystem.StartRace();  // Перезапускаем обучение
            }
        }
    }
}
