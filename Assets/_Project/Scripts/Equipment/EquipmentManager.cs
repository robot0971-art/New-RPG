using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentManager : MonoBehaviour, ISaveable, IGameplayWindow
{
    [Serializable]
    private sealed class EquippedSlot
    {
        public EquipmentSlotType slotType;
        public EquipmentData item;
    }

    [SerializeField] private EquipmentData[] availableEquipment;
    [SerializeField] private Button equipmentToggleButton;
    [SerializeField] private GameObject equipmentRoot;
    [SerializeField] private GameObject equipmentScrollView;
    [SerializeField] private bool closeEquipmentScrollViewOnStart = true;
    [SerializeField] private EquippedSlot[] equippedSlots =
    {
        new() { slotType = EquipmentSlotType.Weapon },
        new() { slotType = EquipmentSlotType.Armor },
        new() { slotType = EquipmentSlotType.Ring },
        new() { slotType = EquipmentSlotType.Necklace }
    };

    public event Action EquipmentChanged;
    public bool IsOpen => equipmentScrollView != null && equipmentScrollView.activeInHierarchy;

    private void Awake()
    {
        DIContainer.Global.Register(this);
        ResolveEquipmentUiReferences();
        GameplayWindowManager.Register(this);

        if (closeEquipmentScrollViewOnStart && equipmentScrollView != null)
        {
            equipmentScrollView.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        GameplayWindowManager.Unregister(this);
    }

    private void OnEnable()
    {
        ResolveEquipmentUiReferences();

        if (equipmentToggleButton != null)
        {
            equipmentToggleButton.onClick.AddListener(ToggleEquipmentScrollView);
        }
    }

    private void OnDisable()
    {
        if (equipmentToggleButton != null)
        {
            equipmentToggleButton.onClick.RemoveListener(ToggleEquipmentScrollView);
        }
    }

    public EquipmentData GetEquipped(EquipmentSlotType slotType)
    {
        EquippedSlot slot = GetSlot(slotType);
        return slot != null && IsValidEquipment(slot.item) ? slot.item : null;
    }

    public EquipmentData FindByIcon(EquipmentSlotType slotType, Sprite icon)
    {
        if (availableEquipment == null || icon == null)
        {
            return null;
        }

        for (int i = 0; i < availableEquipment.Length; i++)
        {
            EquipmentData item = availableEquipment[i];
            if (IsValidEquipment(item) && item.slotType == slotType && item.icon == icon)
            {
                return item;
            }
        }

        return null;
    }

    public float GetAttackBonus() => SumBonus(item => item.attackBonus);
    public float GetHealthBonus() => SumBonus(item => item.healthBonus);
    public float GetAttackSpeedBonus() => SumBonus(item => item.attackSpeedBonus);
    public float GetCritChanceBonus() => SumBonus(item => item.critChanceBonus);
    public float GetCritDamageBonus() => SumBonus(item => item.critDamageBonus);
    public float GetGoldBonus() => SumBonus(item => item.goldBonus);
    public float GetExpBonus() => SumBonus(item => item.expBonus);

    public bool TryEquip(EquipmentData item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id))
        {
            return false;
        }

        EquippedSlot slot = GetOrCreateSlot(item.slotType);
        if (slot == null)
        {
            return false;
        }

        slot.item = item;
        EquipmentChanged?.Invoke();
        SaveEvents.RequestSave();
        return true;
    }

    public bool TryEquipFirstAvailable(EquipmentSlotType slotType)
    {
        EquipmentData item = FindFirstAvailable(slotType);
        return item != null && TryEquip(item);
    }

    public void Unequip(EquipmentSlotType slotType)
    {
        EquippedSlot slot = GetSlot(slotType);
        if (slot == null || slot.item == null)
        {
            return;
        }

        slot.item = null;
        EquipmentChanged?.Invoke();
        SaveEvents.RequestSave();
    }

    public void ToggleEquipmentScrollView()
    {
        ResolveEquipmentUiReferences();
        if (equipmentScrollView == null)
        {
            Debug.LogWarning("[EquipmentManager] Equipment Scroll View is missing.");
            return;
        }

        bool shouldOpen = !equipmentScrollView.activeSelf;
        if (shouldOpen)
        {
            Open();
            return;
        }

        Close();
    }

    public void Open()
    {
        ResolveEquipmentUiReferences();
        if (equipmentScrollView == null)
        {
            return;
        }

        GameplayWindowManager.OpenExclusive(this);

        if (equipmentRoot != null)
        {
            equipmentRoot.transform.SetAsLastSibling();
        }

        equipmentScrollView.transform.SetAsLastSibling();
        equipmentScrollView.SetActive(true);
    }

    public void Close()
    {
        ResolveEquipmentUiReferences();
        if (equipmentScrollView != null)
        {
            equipmentScrollView.SetActive(false);
        }
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (saveData == null || equippedSlots == null)
        {
            return;
        }

        saveData.equipmentSlots = new EquipmentSlotSaveData[equippedSlots.Length];
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            EquippedSlot slot = equippedSlots[i];
            saveData.equipmentSlots[i] = new EquipmentSlotSaveData
            {
                slotType = slot.slotType,
                equipmentId = IsValidEquipment(slot.item) ? slot.item.id : string.Empty
            };
        }
    }

    public void RestoreSaveData(SaveData saveData)
    {
        ClearEquippedSlots();

        if (saveData == null || saveData.equipmentSlots == null)
        {
            EquipmentChanged?.Invoke();
            return;
        }

        for (int i = 0; i < saveData.equipmentSlots.Length; i++)
        {
            EquipmentSlotSaveData savedSlot = saveData.equipmentSlots[i];
            EquipmentData item = FindById(savedSlot.equipmentId);
            if (item != null && item.slotType == savedSlot.slotType)
            {
                EquippedSlot slot = GetOrCreateSlot(savedSlot.slotType);
                if (slot != null)
                {
                    slot.item = item;
                }
            }
        }

        EquipmentChanged?.Invoke();
    }

    private EquipmentData FindFirstAvailable(EquipmentSlotType slotType)
    {
        if (availableEquipment == null)
        {
            return null;
        }

        for (int i = 0; i < availableEquipment.Length; i++)
        {
            EquipmentData item = availableEquipment[i];
            if (item != null && item.slotType == slotType)
            {
                return item;
            }
        }

        return null;
    }

    private void ResolveEquipmentUiReferences()
    {
        if (equipmentRoot != null && equipmentToggleButton != null && equipmentScrollView != null)
        {
            return;
        }

        if (equipmentRoot == null)
        {
            equipmentRoot = GameObject.Find("Equipment UI");
        }

        if (equipmentRoot == null)
        {
            return;
        }

        if (equipmentToggleButton == null)
        {
            Transform buttonTransform = equipmentRoot.transform.Find("UI Button");
            if (buttonTransform != null)
            {
                equipmentToggleButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (equipmentScrollView == null)
        {
            Transform scrollViewTransform = equipmentRoot.transform.Find("Scroll View");
            if (scrollViewTransform != null)
            {
                equipmentScrollView = scrollViewTransform.gameObject;
            }
        }
    }

    private float SumBonus(Func<EquipmentData, float> selector)
    {
        if (equippedSlots == null || selector == null)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            EquipmentData item = equippedSlots[i] != null ? equippedSlots[i].item : null;
            if (IsValidEquipment(item))
            {
                total += selector(item);
            }
        }

        return total;
    }

    private EquipmentData FindById(string id)
    {
        if (availableEquipment == null || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        for (int i = 0; i < availableEquipment.Length; i++)
        {
            EquipmentData item = availableEquipment[i];
            if (item != null && item.id == id)
            {
                return item;
            }
        }

        return null;
    }

    private static bool IsValidEquipment(EquipmentData item)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.id);
    }

    private EquippedSlot GetSlot(EquipmentSlotType slotType)
    {
        if (equippedSlots == null)
        {
            return null;
        }

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null && equippedSlots[i].slotType == slotType)
            {
                return equippedSlots[i];
            }
        }

        return null;
    }

    private EquippedSlot GetOrCreateSlot(EquipmentSlotType slotType)
    {
        EquippedSlot slot = GetSlot(slotType);
        if (slot != null)
        {
            return slot;
        }

        int oldLength = equippedSlots != null ? equippedSlots.Length : 0;
        Array.Resize(ref equippedSlots, oldLength + 1);
        equippedSlots[oldLength] = new EquippedSlot { slotType = slotType };
        return equippedSlots[oldLength];
    }

    private void ClearEquippedSlots()
    {
        if (equippedSlots == null)
        {
            return;
        }

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null)
            {
                equippedSlots[i].item = null;
            }
        }
    }
}
