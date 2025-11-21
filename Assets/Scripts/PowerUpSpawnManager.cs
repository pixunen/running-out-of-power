using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawnManager : MonoBehaviour
{
    public static PowerUpSpawnManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private PowerUpData powerUpData;
    [SerializeField] private GameObject powerUpPrefab;

    [Header("Health Orb Configuration")]
    [SerializeField] private PowerUpData healthOrbData;
    [SerializeField] private GameObject healthOrbPrefab;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.3f; // 30% chance per turn
    [SerializeField] private int maxPowerUpsOnMap = 3;

    [Header("Health Orb Spawn Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float healthOrbSpawnChance = 0.15f; // 15% chance per turn (rarer)
    [SerializeField] private int maxHealthOrbsOnMap = 2;
    
    [Header("Runtime Data")]
    private List<PowerUpController> activePowerUps = new List<PowerUpController>();
    private List<PowerUpController> activeHealthOrbs = new List<PowerUpController>();
    private int currentTurn = 0;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Subscribe to turn events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
        }
        else
        {
            Debug.LogError("PowerUpSpawnManager: TurnManager not found!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
        }
    }
    
    /// <summary>
    /// Called at the start of each player turn
    /// </summary>
    private void OnPlayerTurnStart()
    {
        currentTurn++;

        // Remove expired power-ups
        DespawnOldPowerUps();

        // Try to spawn new power-up
        TrySpawnPowerUp();

        // Try to spawn new health orb (separate chance)
        TrySpawnHealthOrb();
    }
    
    /// <summary>
    /// Attempt to spawn a power-up based on spawn chance
    /// </summary>
    private void TrySpawnPowerUp()
    {
        // Check if we've reached max power-ups on map
        if (activePowerUps.Count >= maxPowerUpsOnMap)
        {
            return;
        }
        
        // Roll for spawn chance
        float roll = Random.value;
        if (roll > spawnChance)
        {
            return; // No spawn this turn
        }
        
        // Find a valid spawn position
        Vector2Int spawnPosition = FindRandomSpawnPosition();
        if (spawnPosition == Vector2Int.one * -1)
        {
            Debug.LogWarning("PowerUpSpawnManager: Could not find valid spawn position");
            return;
        }
        
        // Spawn the power-up
        SpawnPowerUp(spawnPosition);
    }
    
    /// <summary>
    /// Spawn a power-up at the specified grid position
    /// </summary>
    private void SpawnPowerUp(Vector2Int gridPosition)
    {
        if (powerUpPrefab == null || powerUpData == null)
        {
            Debug.LogError("PowerUpSpawnManager: PowerUp prefab or data not assigned!");
            return;
        }
        
        // Get world position from grid
        Vector3 worldPosition = GridManager.Instance.GetWorldPosition(gridPosition);
        
        // Instantiate power-up
        GameObject powerUpObj = Instantiate(powerUpPrefab, worldPosition, Quaternion.identity);
        powerUpObj.name = $"PowerUp_{gridPosition.x}_{gridPosition.y}";
        
        // Initialize controller
        PowerUpController controller = powerUpObj.GetComponent<PowerUpController>();
        if (controller != null)
        {
            controller.Initialize(powerUpData, gridPosition, currentTurn);
            activePowerUps.Add(controller);
            
            // Register with grid cell
            GridCell cell = GridManager.Instance.GetCell(gridPosition);
            if (cell != null)
            {
                cell.SetPowerUp(powerUpObj);
            }
            
            Debug.Log($"PowerUp spawned at {gridPosition} with {controller.GetPowerAmount()} power");
        }
        else
        {
            Debug.LogError("PowerUpSpawnManager: PowerUpController not found on prefab!");
            Destroy(powerUpObj);
        }
    }

    /// <summary>
    /// Attempt to spawn a health orb based on spawn chance
    /// </summary>
    private void TrySpawnHealthOrb()
    {
        // Check if we've reached max health orbs on map
        if (activeHealthOrbs.Count >= maxHealthOrbsOnMap)
        {
            return;
        }

        // Roll for spawn chance
        float roll = Random.value;
        if (roll > healthOrbSpawnChance)
        {
            return; // No spawn this turn
        }

        // Find a valid spawn position
        Vector2Int spawnPosition = FindRandomSpawnPosition();
        if (spawnPosition == Vector2Int.one * -1)
        {
            Debug.LogWarning("PowerUpSpawnManager: Could not find valid spawn position for health orb");
            return;
        }

        // Spawn the health orb
        SpawnHealthOrb(spawnPosition);
    }

    /// <summary>
    /// Spawn a health orb at the specified grid position
    /// </summary>
    private void SpawnHealthOrb(Vector2Int gridPosition)
    {
        if (healthOrbPrefab == null || healthOrbData == null)
        {
            Debug.LogWarning("PowerUpSpawnManager: Health orb prefab or data not assigned!");
            return;
        }

        // Get world position from grid
        Vector3 worldPosition = GridManager.Instance.GetWorldPosition(gridPosition);

        // Instantiate health orb
        GameObject healthOrbObj = Instantiate(healthOrbPrefab, worldPosition, Quaternion.identity);
        healthOrbObj.name = $"HealthOrb_{gridPosition.x}_{gridPosition.y}";

        // Initialize controller
        PowerUpController controller = healthOrbObj.GetComponent<PowerUpController>();
        if (controller != null)
        {
            controller.Initialize(healthOrbData, gridPosition, currentTurn);
            activeHealthOrbs.Add(controller);

            // Register with grid cell
            GridCell cell = GridManager.Instance.GetCell(gridPosition);
            if (cell != null)
            {
                cell.SetPowerUp(healthOrbObj);
            }

            Debug.Log($"Health orb spawned at {gridPosition} with {controller.GetHealthAmount()} health");
        }
        else
        {
            Debug.LogError("PowerUpSpawnManager: PowerUpController not found on health orb prefab!");
            Destroy(healthOrbObj);
        }
    }

    /// <summary>
    /// Find a random valid spawn position on the grid
    /// </summary>
    private Vector2Int FindRandomSpawnPosition()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("PowerUpSpawnManager: GridManager not found!");
            return Vector2Int.one * -1;
        }
        
        int gridWidth = GridManager.Instance.gridWidth;
        int gridHeight = GridManager.Instance.gridHeight;
        int maxAttempts = 100;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Random position
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            Vector2Int position = new Vector2Int(x, y);
            
            // Check if valid
            GridCell cell = GridManager.Instance.GetCell(position);
            if (cell != null && cell.isWalkable && !cell.isOccupied && !cell.HasPowerUp())
            {
                return position;
            }
        }
        
        // No valid position found
        return Vector2Int.one * -1;
    }
    
    /// <summary>
    /// Remove power-ups that have exceeded their lifespan
    /// </summary>
    private void DespawnOldPowerUps()
    {
        List<PowerUpController> toRemove = new List<PowerUpController>();
        
        foreach (PowerUpController powerUp in activePowerUps)
        {
            if (powerUp == null) continue;
            
            if (powerUp.ShouldDespawn(currentTurn))
            {
                toRemove.Add(powerUp);
            }
        }
        
        // Remove expired power-ups
        foreach (PowerUpController powerUp in toRemove)
        {
            RemovePowerUp(powerUp);
        }
    }
    
    /// <summary>
    /// Remove a power-up from the game
    /// </summary>
    public void RemovePowerUp(PowerUpController powerUp)
    {
        if (powerUp == null) return;

        // Clear from grid cell
        GridCell cell = GridManager.Instance.GetCell(powerUp.GetGridPosition());
        if (cell != null)
        {
            cell.ClearPowerUp();
        }

        // Remove from appropriate list based on type
        if (powerUp.IsHealthOrb())
        {
            activeHealthOrbs.Remove(powerUp);
        }
        else
        {
            activePowerUps.Remove(powerUp);
        }

        // Destroy GameObject
        Destroy(powerUp.gameObject);
    }
    
    // Public getters for debugging
    public int GetActivePowerUpCount() => activePowerUps.Count;
    public int GetCurrentTurn() => currentTurn;
}