using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    GameOver
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    public TurnState currentTurnState = TurnState.PlayerTurn;
    public int turnNumber = 1;

    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;
    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;
    public event Action<TurnState> OnTurnStateChanged;

    private List<EnemyController> activeEnemies = new List<EnemyController>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDisable()
    {
        if (PowerManager.Instance != null)
        {
            PowerManager.Instance.OnPowerDepleted -= EndPlayerTurn;
        }
    }

    void Start()
    {
        StartPlayerTurn();
        
        if (PowerManager.Instance != null)
        {
            PowerManager.Instance.OnPowerDepleted += EndPlayerTurn;
        }
    }

    public void StartPlayerTurn()
    {
        currentTurnState = TurnState.PlayerTurn;
        Debug.Log($"--- Player Turn {turnNumber} Started ---");

        // Reset player power
        if (PowerManager.Instance != null)
        {
            PowerManager.Instance.ResetPower();
        }

        OnPlayerTurnStart?.Invoke();
        OnTurnStateChanged?.Invoke(currentTurnState);
    }

    public void EndPlayerTurn()
    {
        if (currentTurnState != TurnState.PlayerTurn) return;
        if (currentTurnState == TurnState.GameOver) return; // Don't proceed if game is over

        Debug.Log("--- Player Turn Ended ---");
        OnPlayerTurnEnd?.Invoke();

        // Clear any highlights
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ClearHighlights();
        }

        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        if (currentTurnState == TurnState.GameOver) return; // Don't start enemy turn if game is over

        currentTurnState = TurnState.EnemyTurn;
        Debug.Log("--- Enemy Turn Started ---");

        OnEnemyTurnStart?.Invoke();
        OnTurnStateChanged?.Invoke(currentTurnState);

        // Start enemy AI coroutine
        StartCoroutine(ExecuteEnemyTurns());
    }

    IEnumerator ExecuteEnemyTurns()
    {
        // Wait a moment before enemies act
        yield return new WaitForSeconds(0.5f);

        // Get all active enemies
        RefreshEnemyList();

        // Each enemy takes their action
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeTurn();
                yield return new WaitForSeconds(0.5f);
                yield return new WaitUntil(() => !enemy.isAttacking);

                if (currentTurnState == TurnState.GameOver)
                {
                    yield break; // Stop enemy turns immediately if game is over
                }
            }
        }

        // Wait a moment after all enemies acted
        yield return new WaitForSeconds(0.5f);

        EndEnemyTurn();
    }

    void EndEnemyTurn()
    {
        if (currentTurnState == TurnState.GameOver) return; // Don't start player turn if game is over

        Debug.Log("--- Enemy Turn Ended ---");
        OnEnemyTurnEnd?.Invoke();

        turnNumber++;
        StartPlayerTurn();
    }

    public void RegisterEnemy(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    void RefreshEnemyList()
    {
        // Remove null or inactive enemies
        activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
    }

    public bool IsPlayerTurn()
    {
        return currentTurnState == TurnState.PlayerTurn;
    }

    public bool IsEnemyTurn()
    {
        return currentTurnState == TurnState.EnemyTurn;
    }

    public void GameOver()
    {
        currentTurnState = TurnState.GameOver;
        OnTurnStateChanged?.Invoke(currentTurnState);
        Debug.Log("--- GAME OVER ---");
    }
}