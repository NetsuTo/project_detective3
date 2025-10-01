using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QTEManagerTest : MonoBehaviour
{
    public RectTransform qteParent;      // Panel ใน Canvas
    public GameObject qteSlotPrefab;     // Prefab มี Text + Image
    public float slotSpacing = 60f;      // ระยะห่าง slot

    private List<GameObject> slotUIs = new List<GameObject>();

    // สำหรับทดสอบ
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            List<KeyCode> seq = new List<KeyCode> { KeyCode.A, KeyCode.S, KeyCode.D };
            SpawnQTESlots(seq);
        }
    }

    public void SpawnQTESlots(List<KeyCode> sequence)
    {
        // ลบ slot เก่า
        foreach (var slot in slotUIs) Destroy(slot);
        slotUIs.Clear();

        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("Sequence ว่าง");
            return;
        }

        // ตำแหน่งเริ่ม: ให้ slot อยู่ตรงกลาง panel
        Vector2 startPos = new Vector2(-(sequence.Count - 1) * slotSpacing / 2f, 0f);

        for (int i = 0; i < sequence.Count; i++)
        {
            GameObject slot = Instantiate(qteSlotPrefab);
            slot.transform.SetParent(qteParent, false); // สำคัญ! เพื่อให้ anchoredPosition ถูกต้อง
            slot.transform.localScale = Vector3.one;

            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchoredPosition = startPos + new Vector2(i * slotSpacing, 0f);

            // ตั้งตัวอักษร
            Text t = slot.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = sequence[i].ToString();
                t.color = Color.black;
            }
            else
            {
                Debug.LogError("Prefab ไม่มี Text component");
            }

            // ตั้งสี slot
            Image img = slot.GetComponent<Image>();
            if (img != null) img.color = Color.white;

            slotUIs.Add(slot);
        }

        Debug.Log("Spawned " + sequence.Count + " QTE slots!");
    }
}
