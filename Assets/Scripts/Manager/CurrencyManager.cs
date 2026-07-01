using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Monedas")]
    public int coins = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        coins = PlayerPrefs.GetInt("Coins", 0);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        PlayerPrefs.SetInt("Coins", coins);
        HUDManager.Instance?.UpdateCoins(coins);
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        PlayerPrefs.SetInt("Coins", coins);
        HUDManager.Instance?.UpdateCoins(coins);
        return true;
    }

    public int GetCoins() => coins;
}