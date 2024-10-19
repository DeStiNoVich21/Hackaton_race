using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    public Transform target; // ����, ���� ����� ��������� (��������, ����� ��� ���������� ����� �� �����)
    public Transform point1; // ����, ���� ����� ��������� (��������, ����� ��� ���������� ����� �� �����)

    public Transform point2; // ����, ���� ����� ��������� (��������, ����� ��� ���������� ����� �� �����)

    public NavMeshAgent agent; // NavMeshAgent, ������� ����� ������������ �������
    public bool first;
    void Start()
    {
   

        // ������������� ���� ��� ������
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
        // ��������� ��������� ����, ���� ��� ��������
        if (target != null && agent.destination != target.position)
        {
            SetDestination(target.position);
        }

        // ��������� �������� � ����� ���������� ����� � ������ ��������
        UpdateCenterCorrection();
    }

    // ����� ��������� ����� ����
    public void SetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    // ����� ��������� ��������� ����� �� ����
    public Vector3 GetNextWaypoint()
    {
        if (agent.path.corners.Length > 1)
        {
            // ���������� ��������� ���� ����
            return agent.path.corners[1];
        }
        else
        {
            // ���� ����� ������ ���, ���������� ������� �������
            return transform.position;
        }
    }

    // ����� ��������, �������� �� �� ����
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

    // ����� ��� ���������� �������� ������ � ��������� �� ����� ��������
    void UpdateCenterCorrection()
    {
        if (agent.path.corners.Length < 2) return; // ���� ��� ����� ����, ������ �� ������

        Vector3 currentPosition = transform.position;
        Vector3 nextCorner = agent.path.corners[1]; // ��������� ���� ��������

        // ���������� ������� ������� ���� (����� ����� ������)
        Vector3 previousCorner = agent.path.corners[0];

        // ���������� ������ ����������� ����� ����� ��������
        Vector3 centerLine = (nextCorner + previousCorner) / 2;

        // ���������� ����������� � ����������� �����
        Vector3 directionToCenter = centerLine - currentPosition;
        directionToCenter.y = 0; // ���������� ��������� �� ��� Y, ����� ��������� ������ �������������� ���������

        float distanceToCenter = directionToCenter.magnitude;

        // ������������ ����������� ������, ���� ��� ������ �� ������
        if (distanceToCenter > 1f) // ����� ����� ��������� ���������� ��� "������"
        {
            Vector3 correctionDirection = directionToCenter.normalized;
            agent.Move(correctionDirection * Time.deltaTime * agent.speed);
        }
    }
}
