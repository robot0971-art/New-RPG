using System;
using UnityEngine;

public sealed class PlayerResources : MonoBehaviour, ISaveable
{
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float startingMana = 100f;
    [SerializeField] private float manaRegenPerSecond = 5f;

    public float MaxMana => maxMana;
    public float CurrentMana { get; private set; }
    public float ManaNormalized => maxMana > 0f ? Mathf.Clamp01(CurrentMana / maxMana) : 0f;

    public event Action<PlayerResources> ManaChanged;

    private void Awake()
    {
        maxMana = Mathf.Max(1f, maxMana);
        CurrentMana = Mathf.Clamp(startingMana, 0f, maxMana);
        DIContainer.Global.Register(this);
    }

    private void Update()
    {
        RestoreMana(manaRegenPerSecond * Time.deltaTime);
    }

    public bool CanSpendMana(float amount)
    {
        return amount <= 0f || CurrentMana >= amount;
    }

    public bool TrySpendMana(float amount)
    {
        if (!CanSpendMana(amount))
        {
            return false;
        }

        SetMana(CurrentMana - Mathf.Max(0f, amount));
        return true;
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0f || CurrentMana >= maxMana)
        {
            return;
        }

        SetMana(CurrentMana + amount);
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (saveData != null)
        {
            saveData.playerMana = CurrentMana;
        }
    }

    public void RestoreSaveData(SaveData saveData)
    {
        if (saveData != null)
        {
            SetMana(saveData.playerMana > 0f ? saveData.playerMana : startingMana);
        }
    }

    private void SetMana(float value)
    {
        float clampedMana = Mathf.Clamp(value, 0f, maxMana);
        if (Mathf.Approximately(CurrentMana, clampedMana))
        {
            return;
        }

        CurrentMana = clampedMana;
        ManaChanged?.Invoke(this);
    }
}
