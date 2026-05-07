using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Equipment", fileName = "New Equipment")]
public sealed class EquipmentData : ScriptableObject
{
    public string id;
    public string displayName = "Equipment";
    public Sprite icon;
    public EquipmentSlotType slotType;
    public float attackBonus;
    public float healthBonus;
    public float attackSpeedBonus;
    public float critChanceBonus;
    public float critDamageBonus;
    public float goldBonus;
    public float expBonus;
}
