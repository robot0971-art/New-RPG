using UnityEngine;
using System;

public sealed class SkillLoadoutUI : MonoBehaviour, ISaveable
{
    [SerializeField] private SkillSlotUI[] slots;
    [SerializeField] private SkillWindowUI skillWindow;
    [SerializeField] private bool closeSkillWindowAfterAssign = true;

    private SkillManager skillManager;
    private SkillSlotUI pendingSlot;

    public static SkillLoadoutUI ActiveLoadout { get; private set; }
    public static SkillLoadoutUI CurrentAssigningLoadout { get; private set; }
    public bool IsAssigning => pendingSlot != null;
    public event Action LoadoutChanged;

    private void Awake()
    {
        CacheSlots();
        if (ActiveLoadout == null)
        {
            DIContainer.Global.Register(this);
            ActiveLoadout = this;
        }
    }

    private void OnDestroy()
    {
        if (ActiveLoadout == this)
        {
            ActiveLoadout = null;
        }

        if (CurrentAssigningLoadout == this)
        {
            CurrentAssigningLoadout = null;
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureSlots();
    }

    public void BeginAssign(SkillSlotUI slot)
    {
        if (slot == null)
        {
            return;
        }

        ResolveReferences();
        pendingSlot = slot;
        CurrentAssigningLoadout = this;
        LoadoutChanged?.Invoke();
        skillWindow?.Open();
    }

    public bool TryAssignSkill(SkillType skillType)
    {
        ResolveReferences();
        if (pendingSlot == null || skillManager == null || !skillManager.IsSkillUnlocked(skillType))
        {
            return false;
        }

        pendingSlot.AssignSkill(skillType);
        pendingSlot = null;
        if (CurrentAssigningLoadout == this)
        {
            CurrentAssigningLoadout = null;
        }

        LoadoutChanged?.Invoke();

        if (closeSkillWindowAfterAssign)
        {
            skillWindow?.Close();
        }

        SaveEvents.RequestSave();
        return true;
    }

    public void CancelAssign()
    {
        pendingSlot = null;
        if (CurrentAssigningLoadout == this)
        {
            CurrentAssigningLoadout = null;
        }

        LoadoutChanged?.Invoke();
    }

    public bool IsSkillAssigned(SkillType skillType)
    {
        CacheSlots();
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            SkillSlotUI slot = slots[i];
            if (slot != null && slot.HasAssignedSkill && slot.AssignedSkillType == skillType)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsSkillAssignedToOtherSlot(SkillType skillType)
    {
        CacheSlots();
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            SkillSlotUI slot = slots[i];
            if (slot != null
                && slot != pendingSlot
                && slot.HasAssignedSkill
                && slot.AssignedSkillType == skillType)
            {
                return true;
            }
        }

        return false;
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (ActiveLoadout != this)
        {
            return;
        }

        if (saveData == null)
        {
            return;
        }

        CacheSlots();
        saveData.skillLoadout = new int[slots != null ? slots.Length : 0];
        for (int i = 0; i < saveData.skillLoadout.Length; i++)
        {
            SkillSlotUI slot = slots[i];
            saveData.skillLoadout[i] = slot != null && slot.HasAssignedSkill ? (int)slot.AssignedSkillType : -1;
        }
    }

    public void RestoreSaveData(SaveData saveData)
    {
        if (ActiveLoadout != this)
        {
            return;
        }

        if (saveData == null || saveData.skillLoadout == null)
        {
            return;
        }

        CacheSlots();
        int count = Mathf.Min(slots.Length, saveData.skillLoadout.Length);
        for (int i = 0; i < count; i++)
        {
            SkillSlotUI slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            int savedSkill = saveData.skillLoadout[i];
            if (savedSkill < 0)
            {
                slot.ClearSkill();
            }
            else
            {
                slot.AssignSkill((SkillType)savedSkill);
            }
        }

        LoadoutChanged?.Invoke();
    }

    private void ResolveReferences()
    {
        if (skillManager == null)
        {
            skillManager = DIContainer.Global.Resolve<SkillManager>();
        }

        if (skillWindow == null)
        {
            skillWindow = UnityEngine.Object.FindFirstObjectByType<SkillWindowUI>();
        }
    }

    private void CacheSlots()
    {
        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<SkillSlotUI>(true);
        }
    }

    private void ConfigureSlots()
    {
        CacheSlots();
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Construct(this, skillManager);
            }
        }
    }
}
