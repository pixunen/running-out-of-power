using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PowerUpData powerUpData;
    
    [Header("Runtime Data")]
    private Vector2Int gridPosition;
    private int spawnTurn;
    private int powerAmount;
    private SpriteRenderer spriteRenderer;
    
    /// <summary>
    /// Initialize the power-up with data and position
    /// </summary>
    public void Initialize(PowerUpData data, Vector2Int position, int currentTurn)
    {
        powerUpData = data;
        gridPosition = position;
        spawnTurn = currentTurn;
        powerAmount = data.GetRandomPowerAmount();
        
        // Setup visual representation
        SetupVisuals();
    }
    
    /// <summary>
    /// Setup the sprite and visual appearance
    /// </summary>
    private void SetupVisuals()
    {
        // Get or add SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Apply sprite and color from data
        if (powerUpData != null)
        {
            spriteRenderer.sprite = powerUpData.powerUpSprite;
            spriteRenderer.color = powerUpData.powerUpColor;
            spriteRenderer.sortingOrder = 5; // Render above grid cells
        }
        
        // Add a subtle pulsing effect
        StartCoroutine(PulseEffect());
    }
    
    /// <summary>
    /// Pulsing animation to make power-up more visible
    /// </summary>
    private System.Collections.IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        float pulseSpeed = 2f;
        float pulseAmount = 0.15f;
        
        while (true)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
            yield return null;
        }
    }
    
    /// <summary>
    /// Check if this power-up should despawn based on age
    /// </summary>
    public bool ShouldDespawn(int currentTurn)
    {
        if (powerUpData == null) return false;

        // Health orbs don't despawn (or have very long lifetime)
        if (!powerUpData.shouldDespawn)
        {
            return false;
        }

        int age = currentTurn - spawnTurn;
        return age >= powerUpData.despawnTurns;
    }
    
    /// <summary>
    /// Collect this power-up and return the power amount
    /// </summary>
    public int Collect()
    {
        return powerAmount;
    }

    /// <summary>
    /// Get the health amount for health orbs
    /// </summary>
    public int GetHealthAmount()
    {
        if (powerUpData == null) return 0;
        return powerUpData.healthAmount;
    }

    /// <summary>
    /// Check if this is a health orb
    /// </summary>
    public bool IsHealthOrb()
    {
        if (powerUpData == null) return false;
        return powerUpData.pickupType == PickupType.Health;
    }

    // Getters
    public Vector2Int GetGridPosition() => gridPosition;
    public int GetPowerAmount() => powerAmount;
    public int GetSpawnTurn() => spawnTurn;
    public PickupType GetPickupType() => powerUpData != null ? powerUpData.pickupType : PickupType.Power;
}