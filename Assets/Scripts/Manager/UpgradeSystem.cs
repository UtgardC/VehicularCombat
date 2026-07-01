using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UpgradeTier
{
    public int cost;
    public float statBonus;
    public GameObject visualPart; // pieza 3D que se activa en el modelo de la nave
}

public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    [Header("Velocidad")]
    public UpgradeTier[] speedTiers;
    private int speedLevel = 0;

    [Header("Armadura")]
    public UpgradeTier[] armorTiers;
    private int armorLevel = 0;

    [Header("Daño")]
    public UpgradeTier[] damageTiers;
    private int damageLevel = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadUpgrades();
    }

    // ── Velocidad ──────────────────────────────────────────
    public bool UpgradeSpeed()
    {
        return ApplyUpgrade(ref speedLevel, speedTiers, "SpeedLevel", tier =>
        {
            Debug.Log("Velocidad Mejorada");
            // PlayerController.Instance?.ApplySpeedUpgrade(tier.statBonus);
        });
    }

    // ── Armadura ───────────────────────────────────────────
    public bool UpgradeArmor()
    {
        return ApplyUpgrade(ref armorLevel, armorTiers, "ArmorLevel", tier =>
        {
            Debug.Log("Armadura Mejorada");
            // PlayerHealth.Instance?.ApplyArmorUpgrade(tier.statBonus);
        });
    }

    // ── Daño ───────────────────────────────────────────────
    public bool UpgradeDamage()
    {
        return ApplyUpgrade(ref damageLevel, damageTiers, "DamageLevel", tier =>
        {
            Debug.Log("Daño Mejorado");
            // WeaponSystem.Instance?.ApplyDamageUpgrade(tier.statBonus);
        });
    }

    // ── Lógica genérica ────────────────────────────────────
    bool ApplyUpgrade(ref int level, UpgradeTier[] tiers, string saveKey, System.Action<UpgradeTier> applyEffect)
    {
        if (level >= tiers.Length) return false;

        UpgradeTier tier = tiers[level];
        if (!CurrencyManager.Instance.SpendCoins(tier.cost)) return false;

        applyEffect(tier);

        // Activa la pieza visual del modelo 3D si existe
        if (tier.visualPart != null)
            tier.visualPart.SetActive(true);

        level++;
        PlayerPrefs.SetInt(saveKey, level);
        return true;
    }

    void LoadUpgrades()
    {
        speedLevel = PlayerPrefs.GetInt("SpeedLevel", 0);
        armorLevel = PlayerPrefs.GetInt("ArmorLevel", 0);
        damageLevel = PlayerPrefs.GetInt("DamageLevel", 0);
    }

    public int GetSpeedLevel() => speedLevel;
    public int GetArmorLevel() => armorLevel;
    public int GetDamageLevel() => damageLevel;

    public bool IsMaxSpeed() => speedLevel >= speedTiers.Length;
    public bool IsMaxArmor() => armorLevel >= armorTiers.Length;
    public bool IsMaxDamage() => damageLevel >= damageTiers.Length;
}