using UnityEngine;
using System;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance { get; private set; }

    [Header("Power Settings")]
    public int maxPower = 10;
    public int currentPower;

    [Header("Bonus Power Settings")]
    [Tooltip("Bonus power from power-ups, consumed before regular power")]
    public int bonusPower = 0;

    [Tooltip("Maximum bonus power that can be accumulated")]
    public int maxBonusPower = 5;

    [Header("Action Costs")]
    [Tooltip("Minimum power cost of any available action")]
    public int minimumActionCost = 2;

    public event Action<int, int> OnPowerChanged; // current, max
    public event Action<int> OnBonusPowerChanged; // bonus power
    public event Action OnPowerDepleted;
    public event Action OnNoActionsAffordable;

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

    void Start()
    {
        ResetPower();
    }

    public void ResetPower()
    {
        currentPower = maxPower;
        OnPowerChanged?.Invoke(currentPower, maxPower);
        Debug.Log($"Power reset to {currentPower}/{maxPower}");
    }

    public bool HasEnoughPower(int cost)
    {
        return (bonusPower + currentPower) >= cost;
    }

    public bool ConsumePower(int cost)
    {
        if (HasEnoughPower(cost))
        {
            int remainingCost = cost;

            // First consume bonus power
            if (bonusPower > 0)
            {
                int bonusUsed = Mathf.Min(bonusPower, remainingCost);
                bonusPower -= bonusUsed;
                remainingCost -= bonusUsed;
                OnBonusPowerChanged?.Invoke(bonusPower);
                Debug.Log($"Bonus power consumed: {bonusUsed}. Remaining bonus: {bonusPower}");
            }

            // Then consume regular power if needed
            if (remainingCost > 0)
            {
                currentPower -= remainingCost;
                OnPowerChanged?.Invoke(currentPower, maxPower);
            }

            Debug.Log($"Total power consumed: {cost}. Remaining: {currentPower}/{maxPower} (+{bonusPower} bonus)");

            if (currentPower <= 0 && bonusPower <= 0)
            {
                Debug.Log("Power depleted! Auto-ending turn...");
                OnPowerDepleted?.Invoke();
            }
            else if (!CanAffordAnyAction())
            {
                Debug.Log($"Insufficient power for any action! (Total: {GetTotalPower()}, Min needed: {minimumActionCost}) Auto-ending turn...");
                OnNoActionsAffordable?.Invoke();
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"Not enough power! Need {cost}, have {currentPower} (+{bonusPower} bonus)");
            return false;
        }
    }

    public void AddPower(int amount)
    {
        currentPower = Mathf.Min(currentPower + amount, maxPower);
        OnPowerChanged?.Invoke(currentPower, maxPower);
        Debug.Log($"Power added: {amount}. Current: {currentPower}/{maxPower}");
    }

    public float GetPowerPercentage()
    {
        return (float)currentPower / maxPower;
    }

    public int GetCurrentPower()
    {
        return currentPower;
    }

    public int GetMaxPower()
    {
        return maxPower;
    }

    public void AddBonusPower(int amount)
    {
        int previousBonus = bonusPower;
        bonusPower = Mathf.Min(bonusPower + amount, maxBonusPower);
        int actualAdded = bonusPower - previousBonus;

        OnBonusPowerChanged?.Invoke(bonusPower);
        Debug.Log($"Bonus power added: {actualAdded}. Current bonus: {bonusPower}/{maxBonusPower}");

        if (actualAdded < amount)
        {
            Debug.LogWarning($"Bonus power capped! Could only add {actualAdded} of {amount}. At max: {bonusPower}/{maxBonusPower}");
        }
    }

    public int GetBonusPower()
    {
        return bonusPower;
    }

    public int GetMaxBonusPower()
    {
        return maxBonusPower;
    }

    public int GetTotalPower()
    {
        return currentPower + bonusPower;
    }

    public bool CanAffordAnyAction()
    {
        return GetTotalPower() >= minimumActionCost;
    }

    public void SetMinimumActionCost(int cost)
    {
        minimumActionCost = cost;
    }
}