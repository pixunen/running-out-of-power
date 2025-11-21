using UnityEngine;

public enum PickupType
{
    Power,      // Power-ups that give bonus power
    Health      // Health orbs that restore health
}

[CreateAssetMenu(fileName = "PowerUp", menuName = "ScriptableObjects/PowerUpData", order = 2)]
public class PowerUpData : ScriptableObject
{
    [Header("Pickup Type")]
    [Tooltip("Type of pickup - Power or Health")]
    public PickupType pickupType = PickupType.Power;

    [Header("Power Settings")]
    [Tooltip("Minimum power amount this power-up can give")]
    public int minPowerAmount = 2;

    [Tooltip("Maximum power amount this power-up can give")]
    public int maxPowerAmount = 5;

    [Header("Health Settings")]
    [Tooltip("Amount of health to restore (for health orbs)")]
    public int healthAmount = 1;
    
    [Header("Spawn Settings")]
    [Tooltip("Should this pickup despawn after a certain number of turns?")]
    public bool shouldDespawn = true;

    [Tooltip("Number of turns before this power-up despawns if not collected")]
    public int despawnTurns = 3;
    
    [Header("Visual Settings")]
    [Tooltip("Sprite to display for this power-up")]
    public Sprite powerUpSprite;
    
    [Tooltip("Color tint for the power-up")]
    public Color powerUpColor = Color.yellow;
    
    /// <summary>
    /// Get a random power amount within the configured range
    /// </summary>
    public int GetRandomPowerAmount()
    {
        return Random.Range(minPowerAmount, maxPowerAmount + 1);
    }
}