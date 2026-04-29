using System;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Booting;

    public event Action<GameState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // DI 컨테이너에 자신을 등록
        DIContainer.Global.Register(this);
    }

    public void StartNewGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void ShowTitle()
    {
        Time.timeScale = 1f;
        SetState(GameState.Title);
    }

    public void PauseGame()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void EndGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.GameOver);
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(State);
        Debug.Log($"[GameManager] State changed to {State}");
    }
}
