using System.Collections.Generic;  // ��ͧ���
using UnityEngine;

public class QTE_Test : MonoBehaviour
{
    public QTEManager qteManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // ���ҧ sequence �ͧ QTE
            List<KeyCode> sequence = new List<KeyCode> { KeyCode.A, KeyCode.S, KeyCode.D };
            qteManager.StartQTE(sequence);
        }
    }
}
