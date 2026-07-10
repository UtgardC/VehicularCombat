using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración de Run")]
    public float runDuration = 300f;
    public int objectivesRequired = 3;

    [Header("Estado Actual (solo lectura)")]
    public float currentTime;
    public int objectivesDestroyed;
    public bool finalZoneUnlocked;
    public bool isFinalMotorDestroyed;

    [Header("Configuración de Objetivos")]
    public int totalObjectives = 3;

    public enum GameState { Hub, WaitingToStart, Playing, Paused, Victory, Defeat }
    public GameState State { get; private set; }

    public delegate void OnGameStateChanged(GameState newState);
    public event OnGameStateChanged GameStateChanged;

    public event System.Action OnFinalZoneUnlocked;

    void Start()
    {
        StartRun();
    }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetState(GameState.WaitingToStart);
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        currentTime -= Time.deltaTime;
        HUDManager.Instance?.UpdateTimer(currentTime);

        if (currentTime <= 0f)
            TriggerDefeat("Tiempo agotado");
    }

    // ── Run ───────────────────────────────────────────────────────────────────
    public void StartRun()
    {
        currentTime = runDuration;
        objectivesDestroyed = 0;
        finalZoneUnlocked = false;
        isFinalMotorDestroyed = false;
        SetState(GameState.Playing);
        EntryPointManager.Instance?.SelectRandomEntry();
        HUDManager.Instance?.UpdateCoins(CurrencyManager.Instance?.GetCoins() ?? 0);
    }

    // ── Objetivos ─────────────────────────────────────────────────────────────
    public void RegisterObjectiveDestroyed()
    {
        if (objectivesDestroyed >= objectivesRequired) return; // ya completo

        objectivesDestroyed++;
        CurrencyManager.Instance?.AddCoins(30);

        global::HUDManager.Instance?.UpdateObjectiveText(objectivesDestroyed, objectivesRequired);

        if (objectivesDestroyed >= objectivesRequired)
        {
            UnlockFinalZone();
        }
    }

    void UnlockFinalZone()
    {
        finalZoneUnlocked = true;
        MapManager.Instance?.EnableFinalZone();
        OnFinalZoneUnlocked?.Invoke();
        Debug.Log("Zona final desbloqueada.");
    }

    // ── Motor final ───────────────────────────────────────────────────────────
    public void RegisterMotorDestroyed()
    {
        if (State != GameState.Playing) return;
        isFinalMotorDestroyed = true;
        TriggerVictory();
    }

    // ── Condiciones ───────────────────────────────────────────────────────────
    public void TriggerDefeat(string reason = "Nave destruida")
    {
        if (State == GameState.Defeat || State == GameState.Victory) return;
        SetState(GameState.Defeat);
        UIManager.Instance?.ShowDefeatScreen(reason);
    }

    public void TriggerVictory()
    {
        if (State == GameState.Victory || State == GameState.Defeat) return;
        SetState(GameState.Victory);
        UIManager.Instance?.ShowVictoryScreen();
    }

    public void SetStateWaitingToStart() => SetState(GameState.WaitingToStart);

    // ── Pausa ─────────────────────────────────────────────────────────────────
    public void SetStatePaused() => SetState(GameState.Paused);
    public void ResumeFromPause() => SetState(GameState.Playing);

    // ── Navegación ────────────────────────────────────────────────────────────
    public void ReturnToHub()
    {
        SetState(GameState.Hub);
        Time.timeScale = 1f;
        SceneManager.LoadScene("HUBScene");
    }

    public void SetState(GameState newState)
    {
        State = newState;
        GameStateChanged?.Invoke(newState);
    }
}