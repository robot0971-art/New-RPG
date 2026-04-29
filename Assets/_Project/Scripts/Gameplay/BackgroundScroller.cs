using UnityEngine;

public sealed class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 1.5f;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Vector2 scrollDirection = Vector2.left;

    public float ScrollSpeed => scrollSpeed;

    private bool isScrolling = true;
    private Vector2 currentOffset;
    private Material runtimeMaterial;

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
        }
    }

    private void Update()
    {
        if (!isScrolling || runtimeMaterial == null)
        {
            return;
        }

        currentOffset += scrollDirection.normalized * (scrollSpeed * Time.deltaTime);
        runtimeMaterial.mainTextureOffset = currentOffset;
    }

    private void OnDestroy()
    {
        // 생성된 머티리얼 인스턴스를 명시적으로 해제하여 메모리 누수 방지
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    public void SetScrolling(bool value)
    {
        isScrolling = value;
    }
}
