using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image manaFillImage;
    [SerializeField] private Image expFillImage;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Slider expSlider;

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureFillImages();
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (player == null || playerResources == null)
        {
            ResolveReferences();
            Subscribe();
            RefreshAll();
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (playerResources == null)
        {
            playerResources = DIContainer.Global.Resolve<PlayerResources>();
        }
    }

    private void ConfigureFillImages()
    {
        ConfigureFillImage(healthFillImage);
        ConfigureFillImage(manaFillImage);
        ConfigureFillImage(expFillImage);
    }

    private static void ConfigureFillImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillClockwise = true;
    }

    private void Subscribe()
    {
        if (player != null)
        {
            player.Damaged -= RefreshHealth;
            player.ExperienceChanged -= RefreshExperience;
            player.LevelChanged -= RefreshExperience;
            player.Damaged += RefreshHealth;
            player.ExperienceChanged += RefreshExperience;
            player.LevelChanged += RefreshExperience;
        }

        if (playerResources != null)
        {
            playerResources.ManaChanged -= RefreshMana;
            playerResources.ManaChanged += RefreshMana;
        }
    }

    private void Unsubscribe()
    {
        if (player != null)
        {
            player.Damaged -= RefreshHealth;
            player.ExperienceChanged -= RefreshExperience;
            player.LevelChanged -= RefreshExperience;
        }

        if (playerResources != null)
        {
            playerResources.ManaChanged -= RefreshMana;
        }
    }

    private void RefreshAll()
    {
        if (player != null)
        {
            RefreshHealth(player);
            RefreshExperience(player);
        }

        if (playerResources != null)
        {
            RefreshMana(playerResources);
        }
    }

    private void RefreshHealth(AutoBattleUnit unit)
    {
        if (healthFillImage != null && unit != null)
        {
            SetNormalizedValue(healthFillImage, healthSlider, unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f);
        }
        else if (healthSlider != null && unit != null)
        {
            SetNormalizedValue(null, healthSlider, unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f);
        }
    }

    private void RefreshMana(PlayerResources resources)
    {
        if (manaFillImage != null && resources != null)
        {
            SetNormalizedValue(manaFillImage, manaSlider, resources.ManaNormalized);
        }
        else if (manaSlider != null && resources != null)
        {
            SetNormalizedValue(null, manaSlider, resources.ManaNormalized);
        }
    }

    private void RefreshExperience(AutoBattleUnit unit)
    {
        if (expFillImage != null && unit != null)
        {
            float requiredExp = Mathf.Max(1f, unit.RequiredExp);
            SetNormalizedValue(expFillImage, expSlider, unit.CurrentExp / requiredExp);
        }
        else if (expSlider != null && unit != null)
        {
            float requiredExp = Mathf.Max(1f, unit.RequiredExp);
            SetNormalizedValue(null, expSlider, unit.CurrentExp / requiredExp);
        }
    }

    private void RefreshExperience(int level)
    {
        if (player != null)
        {
            RefreshExperience(player);
        }
    }

    private static void SetNormalizedValue(Image image, Slider slider, float value)
    {
        float normalizedValue = Mathf.Clamp01(value);

        if (image != null)
        {
            image.fillAmount = normalizedValue;
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = normalizedValue;
        }
    }
}
