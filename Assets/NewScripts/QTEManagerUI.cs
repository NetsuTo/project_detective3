using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QTEManagerUI : MonoBehaviour
{
    public Transform qteSlotParent; // Panel หรือ Canvas
    public GameObject qteSlotPrefab; // Prefab มี Text + Image
    public float slotSpacing = 60f;

    private List<GameObject> slotUIs = new List<GameObject>();
    private List<KeyCode> sequence = new List<KeyCode>();
    private int currentIndex = 0;
    private float timer = 0f;
    public float timePerSlot = 1f;
    private bool isActive = false;

    public void StartQTE(List<KeyCode> keySequence)
    {
        ClearQTE();
        if (keySequence == null || keySequence.Count == 0) return;

        sequence = keySequence;
        currentIndex = 0;
        timer = 0f;
        isActive = true;

        // spawn QTE slot
        Vector2 startPos = new Vector2(-(sequence.Count - 1) * slotSpacing / 2f, 0f);
        for (int i = 0; i < sequence.Count; i++)
        {
            GameObject slot = Instantiate(qteSlotPrefab, qteSlotParent);
            slot.transform.localScale = Vector3.one;

            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchoredPosition = startPos + new Vector2(i * slotSpacing, 0);

            Text txt = slot.GetComponentInChildren<Text>();
            if (txt != null) txt.text = sequence[i].ToString();

            Image img = slot.GetComponent<Image>();
            if (img != null) img.color = Color.white;

            slotUIs.Add(slot);
        }
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer > timePerSlot)
        {
            FailQTE();
            return;
        }

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(sequence[currentIndex]))
            {
                // กดถูก
                slotUIs[currentIndex].GetComponent<Image>().color = Color.green;
                currentIndex++;
                timer = 0f;

                if (currentIndex >= sequence.Count)
                    SuccessQTE();
            }
            else
            {
                FailQTE();
            }
        }
    }

    void SuccessQTE()
    {
        Debug.Log("QTE Success!");
        isActive = false;
        ClearQTE();
    }

    void FailQTE()
    {
        Debug.Log("QTE Failed!");
        isActive = false;
        ClearQTE();
    }

    void ClearQTE()
    {
        foreach (var slot in slotUIs) Destroy(slot);
        slotUIs.Clear();
    }
}
