using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CoinTextCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text coinTmpText;
    [SerializeField] private Text coinText;

    private GameManager gameManager;
    private bool isSubscribed;

    private void Awake()
    {
        if (coinTmpText == null)
        {
            coinTmpText = GetComponent<TMP_Text>();
        }

        if (coinText == null)
        {
            coinText = GetComponent<Text>();
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
        if (gameManager != null && isSubscribed)
        {
            gameManager.GoldChanged -= UpdateCoinText;
        }

        isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        gameManager = DIContainer.Global.Resolve<GameManager>();
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (gameManager == null)
        {
            return;
        }

        gameManager.GoldChanged += UpdateCoinText;
        isSubscribed = true;
        UpdateCoinText(gameManager.Gold);
    }

    private void UpdateCoinText(int gold)
    {
        if (coinTmpText != null)
        {
            coinTmpText.text = gold.ToString("D6");
            return;
        }

        if (coinText != null)
        {
            coinText.text = gold.ToString("D6");
        }
    }
}
