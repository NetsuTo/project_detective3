using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DG.Tweening;

public class SkillInventory : MonoBehaviour
{
    public Transform bottleParent;   // จุดวาง UI ใน Canvas
    public GameObject bottlePrefab;  // Prefab icon/slot สำหรับสกิลผสมแล้ว
    public ElementMiniGameManager miniGameManager;

    [Header("Recipe Database")]
    [Tooltip("ฐานข้อมูลสูตรทั้งหมด (ลาก ScriptableObject มาใส่ 1 ตัว)")]
    public ElementRecipeDatabase recipeDatabase;

    [Header("Fallback Settings")]
    [Tooltip("ถ้าไม่ตรงสูตรไหนเลย ให้ใช้สีนี้")]
    public Color defaultColor = Color.white;
    [Tooltip("ถ้าไม่ตรงสูตรไหนเลย ให้ใช้ sprite นี้")]
    public Sprite defaultSprite;

    [Header("Bottle Display Settings")]
    [Tooltip("ขนาดของขวด (0.1 = 10%, 1.0 = 100%)")]
    public float bottleScale = 0.2f; // ✅ ปรับได้ใน Inspector

    private List<List<string>> storedSkills = new List<List<string>>();

    // ===== Input System Actions =====
    private InputAction useSkillAction;

    void Awake()
    {
        SetupInputActions();
        useSkillAction?.Enable();

        int recipeCount = recipeDatabase != null ? recipeDatabase.allRecipes.Count : 0;
        Debug.Log($"✅ SkillInventory Started - โหลดสูตร {recipeCount} แบบ");
    }

    private void SetupInputActions()
    {
        useSkillAction = new InputAction("UseSkill", type: InputActionType.Button);
        useSkillAction.AddBinding("<Keyboard>/r");
        useSkillAction.AddBinding("<Gamepad>/rightTrigger");
        useSkillAction.AddBinding("<Gamepad>/rightShoulder");
        useSkillAction.performed += OnUseSkillPerformed;
    }

    private void Update()
    {
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnUseSkillPerformed(default);
            }
        }
    }

    private void OnEnable() => useSkillAction?.Enable();
    private void OnDisable() => useSkillAction?.Disable();

    // ✅ แก้ไขใหม่ - ไม่ต้องทำอะไร เพราะการใช้สกิลจะเกิดที่ TargetZone
    private void OnUseSkillPerformed(InputAction.CallbackContext ctx)
    {
        if (storedSkills.Count > 0)
        {
            Debug.Log($"📦 มีขวดอยู่ {storedSkills.Count} ขวด - พร้อมใช้ใน TargetZone");
        }
        else
        {
            Debug.Log("⚠️ ไม่มีขวดในคลัง");
        }
    }

    // ✅ เพิ่มสกิลใหม่เข้าขวด - รับ string sequence (เช่น "N", "N", "O", "O")
    public void AddMixedSkill(List<string> sequence)
    {
        storedSkills.Add(sequence);

        // ✅ ตรวจสอบ bottleParent ก่อน
        if (bottleParent == null)
        {
            Debug.LogError("❌ BottleParent is NULL! ไปตั้งค่าใน Inspector");
            return;
        }

        if (bottlePrefab == null)
        {
            Debug.LogError("❌ BottlePrefab is NULL! ไปตั้งค่าใน Inspector");
            return;
        }

        GameObject go = Instantiate(bottlePrefab, bottleParent);
        go.name = "Bottle_" + string.Join("", sequence);

        // ✅ ตั้งขนาดขวดทันที (ก่อนทำอย่างอื่น)
        go.transform.localScale = Vector3.one * bottleScale;

        // ✅ Debug ตำแหน่ง
        Debug.Log($"📍 Bottle spawned - Scale: {bottleScale}, Parent: {bottleParent.name}, Child count: {bottleParent.childCount}");

        // อัปเดตข้อความ
        Text t = go.GetComponentInChildren<Text>();
        if (t != null)
            t.text = string.Join("", sequence);

        // ✅ หาสูตรที่ตรง
        ElementRecipe matchedRecipe = null;
        if (recipeDatabase != null)
        {
            matchedRecipe = recipeDatabase.FindMatchingRecipe(sequence);
        }

        Image bottleImage = go.GetComponent<Image>();

        if (bottleImage != null)
        {
            Sprite spriteToUse = null;
            Color colorToUse = Color.white;

            if (matchedRecipe != null)
            {
                // ใช้ sprite จาก recipe หรือ fallback เป็น default
                spriteToUse = matchedRecipe.bottleSprite != null
                    ? matchedRecipe.bottleSprite
                    : defaultSprite;

                colorToUse = matchedRecipe.bottleColor;

                if (matchedRecipe.bottleSprite != null)
                    Debug.Log($"✅ ใช้ Sprite: {matchedRecipe.bottleSprite.name}");
                else
                    Debug.LogWarning($"⚠️ สูตร '{matchedRecipe.elementName}' ไม่มี Bottle Sprite - ใช้ Default");
            }
            else
            {
                // ไม่เจอสูตร ใช้ default
                spriteToUse = defaultSprite;
                colorToUse = defaultColor;
                Debug.LogWarning($"⚠️ ไม่เจอสูตร: {string.Join("-", sequence)} - ใช้ Default Sprite");
            }

            // ✅ ตรวจสอบว่ามี sprite ก่อนใช้
            if (spriteToUse != null)
            {
                bottleImage.sprite = spriteToUse;
                Debug.Log($"✅ Sprite set: {spriteToUse.name}");
            }
            else
            {
                Debug.LogError("❌ ไม่มี Sprite ให้ใช้เลย! ตรวจสอบ Default Sprite ใน SkillInventory");
            }

            // ✅ บังคับให้ alpha = 1 เสมอ (ไม่ให้โปร่งใส)
            colorToUse.a = 1f;
            bottleImage.color = colorToUse;
            bottleImage.enabled = true;

            Debug.Log($"🖼️ Final: sprite={bottleImage.sprite?.name}, color={bottleImage.color}, scale={go.transform.localScale}");
        }
        else
        {
            Debug.LogError("❌ BottlePrefab ไม่มี Image component!");
        }

        // ✅ แอนิเมชัน
        Vector3 startPos = go.transform.localPosition;
        go.transform.localPosition = startPos - new Vector3(0, 25f, 0);

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        Sequence seq = DOTween.Sequence();
        seq.Append(go.transform.DOLocalMoveY(startPos.y, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(1, 0.4f));

        string elementName = matchedRecipe != null ? matchedRecipe.elementName : "Unknown";
        Debug.Log($"✨ เพิ่มสกิล: {string.Join("-", sequence)} - ธาตุ: {elementName} (รวม {storedSkills.Count} ขวด)");
    }

    // ✅ รองรับ KeyCode แบบเดิม (backward compatibility)
    public void AddMixedSkill(List<KeyCode> sequence)
    {
        List<string> stringSeq = new List<string>();
        foreach (KeyCode key in sequence)
        {
            stringSeq.Add(key.ToString());
        }
        AddMixedSkill(stringSeq);
    }

    // ✅ ดึงข้อมูลขวดแรกโดยไม่ลบ (สำหรับ TargetZone)
    public List<string> GetFirstBottleSequence()
    {
        if (storedSkills.Count > 0)
        {
            return new List<string>(storedSkills[0]);
        }
        return null;
    }

    // ตรวจว่ามี skill ตรงกับ seq หรือไม่
    public bool HasSkill(List<string> seq)
    {
        foreach (var s in storedSkills)
        {
            if (SequencesMatch(s, seq)) return true;
        }
        return false;
    }

    public List<string> GetSkillSequence(List<string> seq)
    {
        foreach (var s in storedSkills)
        {
            if (SequencesMatch(s, seq)) return new List<string>(s);
        }
        return null;
    }

    public void ConsumeSkill(List<string> seq)
    {
        for (int i = 0; i < storedSkills.Count; i++)
        {
            if (SequencesMatch(storedSkills[i], seq))
            {
                storedSkills.RemoveAt(i);
                if (i < bottleParent.childCount)
                {
                    GameObject bottleObj = bottleParent.GetChild(i).gameObject;

                    CanvasGroup cg = bottleObj.GetComponent<CanvasGroup>();
                    if (cg == null)
                        cg = bottleObj.AddComponent<CanvasGroup>();

                    Sequence seq2 = DOTween.Sequence();
                    seq2.Append(bottleObj.transform.DOScale(0.8f * bottleScale, 0.2f).SetEase(Ease.InBack));
                    seq2.Join(cg.DOFade(0f, 0.2f));
                    seq2.OnComplete(() => Destroy(bottleObj));
                }

                Debug.Log($"💊 ใช้สกิล: {string.Join("-", seq)} (เหลือ {storedSkills.Count} ขวด)");
                return;
            }
        }
    }

    public void ConsumeFirstSkill()
    {
        if (storedSkills.Count > 0)
        {
            Debug.Log($"❌ เสียขวด: {string.Join("-", storedSkills[0])}");

            storedSkills.RemoveAt(0);
            if (bottleParent.childCount > 0)
            {
                GameObject bottleObj = bottleParent.GetChild(0).gameObject;

                CanvasGroup cg = bottleObj.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = bottleObj.AddComponent<CanvasGroup>();

                Sequence seq = DOTween.Sequence();
                seq.Append(bottleObj.transform.DOScale(0.8f * bottleScale, 0.2f).SetEase(Ease.InBack));
                seq.Join(cg.DOFade(0f, 0.2f));
                seq.OnComplete(() => Destroy(bottleObj));
            }
        }
    }

    public bool IsEmpty() => storedSkills.Count == 0;
    public bool HasAnyBottle() => storedSkills.Count > 0;
    public int GetBottleCount() => storedSkills.Count;

    private bool SequencesMatch(List<string> a, List<string> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    // ✅ เพิ่ม method นี้เพื่อรองรับโค้ดเก่าที่เรียกใช้
    public void AddSkill(List<string> sequence)
    {
        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("⚠️ พยายามเพิ่มสกิลที่เป็น null หรือว่างเปล่า");
            return;
        }

        // ✅ เรียก AddMixedSkill แทน (เพื่อให้ทำงานเหมือนเดิม)
        AddMixedSkill(sequence);
        Debug.Log($"✅ เพิ่มสกิลเข้าขวด: {string.Join("-", sequence)}");
    }

    void OnDestroy()
    {
        if (useSkillAction != null)
        {
            useSkillAction.performed -= OnUseSkillPerformed;
            useSkillAction.Dispose();
        }
    }
}