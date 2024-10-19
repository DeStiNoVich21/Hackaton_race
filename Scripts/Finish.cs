using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
    public bool firstPoint = true;

    void OnTriggerEnter(Collider other)
    {
        // �������� �������� ��������� AI � ������� ��� ��� ������������ �������� (������� �������� ���������)
        var car = other.GetComponentInParent<AI>();

        // �������� �������� ������� �������� ������
        var learningSystem = other.GetComponentInParent<Learning>();

        if (car != null && learningSystem != null)
        {
            // ���� ��� ������ ��������, ������������� �� ���������
            if (firstPoint)
            {
                car.first = true;
            }
            else
            {
                // ������������� ������� �������� ��� ���� ������
                learningSystem.EndRace();

                // ��������� ������ ����� � ������������� �������� ��� ���������� �����
                car.first = false;
                learningSystem.StartRace();  // ������������� ��������
            }
        }
    }
}
