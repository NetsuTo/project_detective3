using System.Collections.Generic;  // ต้องใส่
using UnityEngine;

public class QTE_Test : MonoBehaviour
{
    public QTEManager qteManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // สร้าง sequence ของ QTE
            List<KeyCode> sequence = new List<KeyCode> { KeyCode.A, KeyCode.S, KeyCode.D };
            qteManager.StartQTE(sequence);
        }
    }
}
