using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private bool startInPlayMode = true;

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            var gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();
        }
    }

    private void Start()
    {
        if (startInPlayMode)
        {
            GameManager.Instance.StartNewGame();
            return;
        }

        GameManager.Instance.ShowTitle();
    }
}
