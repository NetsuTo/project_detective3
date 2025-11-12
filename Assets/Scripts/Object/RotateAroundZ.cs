using UnityEngine;

public class RotateAroundZ : MonoBehaviour
{
    [Header("การตั้งค่าการหมุน")]
    [Tooltip("ความเร็วในการหมุน (องศาต่อวินาที)")]
    public float rotationSpeed = 50f;

    [Tooltip("ทิศทางการหมุน (true = ทวนเข็มนาฬิกา, false = ตามเข็มนาฬิกา)")]
    public bool counterClockwise = true;

    void Update()
    {
        // คำนวณมุมการหมุนในเฟรมนี้
        float rotation = rotationSpeed * Time.deltaTime;

        // กำหนดทิศทางการหมุน
        if (!counterClockwise)
        {
            rotation = -rotation;
        }

        // หมุน Object รอบแกน Z
        transform.Rotate(0, 0, rotation);
    }
}