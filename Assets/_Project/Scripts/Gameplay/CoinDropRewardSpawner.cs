using UnityEngine;
using UnityEngine.Pool;

public sealed class CoinDropRewardSpawner : MonoBehaviour
{
    [Header("Coin Drop")]
    [SerializeField] private GameObject coinDropPrefab;
    [SerializeField] private RectTransform coinUITarget;
    [SerializeField] private float fallbackReleaseDelay = 0.8f;

    [Header("Boss Drop")]
    [SerializeField] private int bossCoinDropCount = 10;
    [SerializeField] private float bossCoinSpreadX = 1.8f;
    [SerializeField] private float bossCoinSpreadY = 0.35f;

    private IObjectPool<GameObject> coinDropPool;

    private void Awake()
    {
        BuildPool();
    }

    public void PlayReward(Vector3 position, float groundY, int totalGoldAmount, bool isBoss)
    {
        if (totalGoldAmount <= 0)
        {
            return;
        }

        if (!isBoss)
        {
            PlayCoinDrop(position, groundY, totalGoldAmount, null);
            return;
        }

        int coinCount = Mathf.Max(1, bossCoinDropCount);
        int remainingGold = totalGoldAmount;
        for (int i = 0; i < coinCount; i++)
        {
            int coinsLeft = coinCount - i;
            int goldAmount = Mathf.CeilToInt((float)remainingGold / coinsLeft);
            remainingGold -= goldAmount;

            float t = coinCount == 1 ? 0.5f : (float)i / (coinCount - 1);
            float x = Mathf.Lerp(-bossCoinSpreadX, bossCoinSpreadX, t);
            float y = Random.Range(-bossCoinSpreadY, bossCoinSpreadY);
            Vector3 throwOffset = new Vector3(x, y, Random.Range(-0.1f, 0.1f));
            Vector3 spawnJitter = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.05f, 0.2f), 0f);
            PlayCoinDrop(position + spawnJitter, groundY, goldAmount, throwOffset);
        }
    }

    private void BuildPool()
    {
        if (coinDropPrefab == null)
        {
            return;
        }

        coinDropPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(coinDropPrefab, transform),
            actionOnGet: (coin) => coin.SetActive(true),
            actionOnRelease: (coin) => coin.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 20
        );
    }

    private void PlayCoinDrop(Vector3 position, float groundY, int goldAmount, Vector3? throwOffsetOverride)
    {
        if (coinDropPrefab == null)
        {
            return;
        }

        if (coinDropPool == null)
        {
            BuildPool();
        }

        if (coinDropPool == null)
        {
            return;
        }

        var coin = coinDropPool.Get();
        coin.transform.rotation = Quaternion.identity;

        var coinDrop = coin.GetComponent<CoinDrop>();
        if (coinDrop == null)
        {
            coin.transform.position = position;
            StartCoroutine(ReleaseCoinDropAfterDelay(coin));
            return;
        }

        coinDrop.CoinUITarget = coinUITarget;
        coinDrop.GoldAmount = goldAmount;
        coinDrop.Pool = coinDropPool;

        if (throwOffsetOverride.HasValue)
        {
            coinDrop.Play(position, groundY, throwOffsetOverride.Value);
        }
        else
        {
            coinDrop.Play(position, groundY);
        }
    }

    private System.Collections.IEnumerator ReleaseCoinDropAfterDelay(GameObject coin)
    {
        yield return new WaitForSeconds(fallbackReleaseDelay);

        if (coin != null)
        {
            coinDropPool?.Release(coin);
        }
    }
}
