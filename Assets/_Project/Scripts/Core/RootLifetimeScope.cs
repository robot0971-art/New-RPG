using UnityEngine;

public sealed class RootLifetimeScope : MonoBehaviour
{
    private void Awake()
    {
        // GameManager가 이미 존재하는지 확인 (GameBootstrap 등에 의해 생성됨)
        var gameManager = Object.FindFirstObjectByType<GameManager>();
        
        if (gameManager != null)
        {
            DIContainer.Global.Register(gameManager);
            Debug.Log("[RootDI] GameManager registered.");
        }
        else
        {
            Debug.LogWarning("[RootDI] GameManager not found in hierarchy. Make sure GameBootstrap is executed.");
        }
    }
}
