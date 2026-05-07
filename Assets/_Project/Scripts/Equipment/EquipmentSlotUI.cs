using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private GameObject equippedStateRoot;
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private EquipmentData equipmentItem;

    private EquipmentManager equipmentManager;
    private Image buttonImage;
    private EquipmentData equippedItem;

    public EquipmentSlotType SlotType => slotType;
    public EquipmentData EquippedItem => equippedItem;
    public bool HasEquippedItem => IsCurrentItemEquipped();

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        buttonImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }

        if (equipmentManager != null)
        {
            equipmentManager.EquipmentChanged += RefreshFromManager;
        }

        RefreshFromManager();
    }

    private void Start()
    {
        RefreshFromManager();
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.EquipmentChanged -= RefreshFromManager;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }

    public void Construct(EquipmentManager equipmentManager)
    {
        this.equipmentManager = equipmentManager;
        RefreshFromManager();
    }

    public void Equip(EquipmentData item)
    {
        if (item == null || item.slotType != slotType)
        {
            return;
        }

        equippedItem = item;
        Refresh();
    }

    public void Clear()
    {
        equippedItem = null;
        Refresh();
    }

    private void ResolveReferences()
    {
        if (equipmentManager == null)
        {
            equipmentManager = DIContainer.Global.Resolve<EquipmentManager>();
        }

        ResolveEquipmentItemFromButtonImage();
    }

    private void ResolveEquipmentItemFromButtonImage()
    {
        if (HasButtonEquipmentItem() || equipmentManager == null || buttonImage == null)
        {
            return;
        }

        equipmentItem = equipmentManager.FindByIcon(slotType, buttonImage.sprite);
    }

    private void RefreshFromManager()
    {
        ResolveReferences();
        equippedItem = equipmentManager != null ? equipmentManager.GetEquipped(slotType) : equippedItem;
        Refresh();
    }

    private void OnClicked()
    {
        ResolveReferences();
        if (equipmentManager == null)
        {
            Debug.LogWarning("[EquipmentSlotUI] EquipmentManager is missing in the scene.");
            return;
        }

        if (HasButtonEquipmentItem())
        {
            if (IsCurrentItemEquipped())
            {
                equipmentManager.Unequip(equipmentItem.slotType);
                RefreshFromManager();
                return;
            }

            equipmentManager.TryEquip(equipmentItem);
            RefreshFromManager();
            return;
        }

        if (equippedItem == null)
        {
            if (!equipmentManager.TryEquipFirstAvailable(slotType))
            {
                Debug.Log($"No available equipment for {slotType}.");
            }

            return;
        }

        equipmentManager.Unequip(slotType);
        RefreshFromManager();
    }

    private void Refresh()
    {
        bool isEquipped = IsCurrentItemEquipped();
        Sprite icon = HasButtonEquipmentItem() ? equipmentItem.icon : equippedItem != null ? equippedItem.icon : null;

        if (equipmentIconImage != null)
        {
            equipmentIconImage.sprite = icon;
            equipmentIconImage.enabled = icon != null;
            equipmentIconImage.raycastTarget = false;
            equipmentIconImage.preserveAspect = true;
        }

        if (emptyStateRoot != null && emptyStateRoot != gameObject)
        {
            emptyStateRoot.SetActive(!isEquipped);
        }

        if (equippedStateRoot != null && equippedStateRoot != gameObject)
        {
            equippedStateRoot.SetActive(isEquipped);
        }
    }

    private bool IsCurrentItemEquipped()
    {
        if (HasButtonEquipmentItem())
        {
            return equippedItem != null
                && !string.IsNullOrWhiteSpace(equipmentItem.id)
                && equippedItem.id == equipmentItem.id;
        }

        return equippedItem != null && !string.IsNullOrWhiteSpace(equippedItem.id);
    }

    private bool HasButtonEquipmentItem()
    {
        return equipmentItem != null && !string.IsNullOrWhiteSpace(equipmentItem.id);
    }
}
