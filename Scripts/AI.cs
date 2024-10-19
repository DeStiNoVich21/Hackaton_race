using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    public Transform target; // Цель, куда нужно добраться (например, игрок или конкретная точка на карте)
    public Transform point1; // Цель, куда нужно добраться (например, игрок или конкретная точка на карте)

    public Transform point2; // Цель, куда нужно добраться (например, игрок или конкретная точка на карте)

    public NavMeshAgent agent; // NavMeshAgent, который будет рассчитывать маршрут
    public bool first;
    void Start()
    {
   

        // Устанавливаем цель для агента
        if (target != null)
        {
            SetDestination(target.position);
        }
    }

    void Update()
    {
        if(first)
        {
            target = point1;
        }
        else
        {
            target = point2;
        }
        // Постоянно обновляем цель, если она меняется
        if (target != null && agent.destination != target.position)
        {
            SetDestination(target.position);
        }

        // Обновляем движение с целью оставаться ближе к центру маршрута
        UpdateCenterCorrection();
    }

    // Метод установки новой цели
    public void SetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    // Метод получения следующей точки на пути
    public Vector3 GetNextWaypoint()
    {
        if (agent.path.corners.Length > 1)
        {
            // Возвращаем следующий угол пути
            return agent.path.corners[1];
        }
        else
        {
            // Если точек больше нет, возвращаем текущую позицию
            return transform.position;
        }
    }

    // Метод проверки, достигли ли мы цели
    public bool HasReachedDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Метод для обновления движения машины с поправкой на центр маршрута
    void UpdateCenterCorrection()
    {
        if (agent.path.corners.Length < 2) return; // Если нет углов пути, ничего не делаем

        Vector3 currentPosition = transform.position;
        Vector3 nextCorner = agent.path.corners[1]; // Следующий угол маршрута

        // Определяем текущий сегмент пути (между двумя углами)
        Vector3 previousCorner = agent.path.corners[0];

        // Определяем вектор центральной линии этого сегмента
        Vector3 centerLine = (nextCorner + previousCorner) / 2;

        // Определяем направление к центральной линии
        Vector3 directionToCenter = centerLine - currentPosition;
        directionToCenter.y = 0; // Игнорируем изменение по оси Y, чтобы учитывать только горизонтальное положение

        float distanceToCenter = directionToCenter.magnitude;

        // Корректируем направление машины, если она далеко от центра
        if (distanceToCenter > 1f) // Здесь можно настроить расстояние для "центра"
        {
            Vector3 correctionDirection = directionToCenter.normalized;
            agent.Move(correctionDirection * Time.deltaTime * agent.speed);
        }
    }
}
