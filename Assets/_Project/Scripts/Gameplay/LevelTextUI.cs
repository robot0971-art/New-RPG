using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelTextUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelTmpText;
    [SerializeField] private Text levelText;
    [SerializeField] private string format = "LV {0:D2}";

    private AutoBattleUnit player;
    private bool isSubscribed;

    private void Awake()
    {
        if (levelTmpText == null)
        {
            levelTmpText = GetComponent<TMP_Text>();
        }

        if (levelText == null)
        {
            levelText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!isSubscribed)
        {
            TrySubscribe();
        }
    }

    private void OnDisable()
    {
        if (player != null && isSubscribed)
        {
            player.LevelChanged -= UpdateLevelText;
        }

        isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        player = DIContainer.Global.Resolve<AutoBattleUnit>();
        if (player == null)
        {
            return;
        }

        player.LevelChanged += UpdateLevelText;
        isSubscribed = true;
        UpdateLevelText(player.Level);
    }

    private void UpdateLevelText(int level)
    {
        string text = string.Format(format, level);

        if (levelTmpText != null)
        {
            levelTmpText.text = text;
            return;
        }

        if (levelText != null)
        {
            levelText.text = text;
        }
    }
}
