using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public sealed class CoinDrop : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private Vector3 throwOffset = new Vector3(0.8f, 0f, 0f);
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float jumpDuration = 0.3f;

    [Header("Bounce")]
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private int bounceCount = 3;
    [SerializeField] private float bounceDuration = 0.15f;
    [SerializeField] private float groundWaitTime = 0.2f;
    [SerializeField] private float groundYOffset = -0.6f;

    [Header("Fly to UI")]
    [SerializeField] private float flyDuration = 0.4f;
    [SerializeField] private float flyArcHeight = 1.5f;
    [SerializeField] private float spinSpeed = 1f;
    [SerializeField] private float flySpinSpeedMultiplier = 2f;

    public RectTransform CoinUITarget { get; set; }
    public int GoldAmount { get; set; }
    public IObjectPool<GameObject> Pool { get; set; }

    private float groundY;
    private GameManager gameManager;
    private Coroutine activeSequence;
    private Collider2D[] colliders;
    private Animator animator;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        animator = GetComponent<Animator>();
    }

    public void Play(Vector3 spawnPos, float groundY)
    {
        this.groundY = groundY + groundYOffset;
        transform.position = spawnPos;
        SetSpinMultiplier(1f);
        gameManager = DIContainer.Global.Resolve<GameManager>() ?? GameManager.Instance;
        gameObject.SetActive(true);
        activeSequence = StartCoroutine(CoinSequence(spawnPos));
    }

    private void OnDisable()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }
    }

    private IEnumerator CoinSequence(Vector3 spawnPos)
    {
        Vector3 currentPos = spawnPos;

        // Phase 1: Throw from the monster position to the landing point.
        Vector3 groundPos = new Vector3(
            spawnPos.x + throwOffset.x,
            groundY + throwOffset.y,
            spawnPos.z + throwOffset.z);
        yield return ArcMoveCoroutine(currentPos, groundPos, jumpHeight, jumpDuration);
        currentPos = groundPos;

        // Phase 3: Bounce on ground
        for (int i = 0; i < bounceCount; i++)
        {
            float decay = (float)(bounceCount - i) / bounceCount;
            float currentBounceY = groundY + bounceHeight * decay;
            Vector3 bouncePeak = new Vector3(groundPos.x, currentBounceY, groundPos.z);

            yield return MoveCoroutine(currentPos, bouncePeak, bounceDuration * 0.5f, EaseOutQuad);
            yield return MoveCoroutine(bouncePeak, groundPos, bounceDuration * 0.5f, EaseInQuad);
            currentPos = groundPos;
        }

        // Phase 4: Wait on ground
        yield return new WaitForSeconds(groundWaitTime);

        // Phase 5: Fly to UI (Bezier curve)
        Vector3 uiWorldPos = GetCoinUIWorldPosition();
        Vector3 midPoint = Vector3.Lerp(groundPos, uiWorldPos, 0.5f) + Vector3.up * flyArcHeight;

        float flyTime = 0f;
        while (flyTime < flyDuration)
        {
            flyTime += Time.deltaTime;
            float t = Mathf.Clamp01(flyTime / flyDuration);
            transform.position = QuadraticBezier(groundPos, midPoint, uiWorldPos, t);
            SetSpinMultiplier(flySpinSpeedMultiplier);
            yield return null;
        }
        transform.position = uiWorldPos;

        // Phase 6: Add gold and return to pool
        gameManager?.AddGold(GoldAmount);
        Pool?.Release(gameObject);
        activeSequence = null;
    }

    private IEnumerator MoveCoroutine(Vector3 from, Vector3 to, float duration, System.Func<float, float> easing)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            transform.position = Vector3.Lerp(from, to, easing(t));
            SetSpinMultiplier(1f);
            yield return null;
        }
        transform.position = to;
    }

    private IEnumerator ArcMoveCoroutine(Vector3 from, Vector3 to, float height, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            Vector3 position = Vector3.Lerp(from, to, t);
            position.y += Mathf.Sin(t * Mathf.PI) * height;
            transform.position = position;
            SetSpinMultiplier(1f);
            yield return null;
        }

        transform.position = to;
    }

    private void SetSpinMultiplier(float multiplier)
    {
        if (animator != null)
        {
            animator.speed = spinSpeed * multiplier;
        }
    }

    private Vector3 GetCoinUIWorldPosition()
    {
        if (CoinUITarget == null || Camera.main == null)
        {
            return transform.position + Vector3.up * 3f;
        }

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, CoinUITarget.position);
        float zDistance = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
    }

    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private float EaseInQuad(float t) => t * t;
}
