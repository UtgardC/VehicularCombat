using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class UIMenuController : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform menuPanel;

    [Header("Input (Nuevo Sistema)")]
    public InputAction toggleMenuAction = new InputAction("ToggleMenu", binding: "<Keyboard>/escape");

    [Header("Configuración de Posición")]
    public float posicionOcultaY = -1000f;
    public float posicionReveladaY = 0f;

    [Header("Configuración de Animación")]
    public float duracionTransicion = 0.5f;
    public AnimationCurve curvaTransicion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool menuAbierto = false;
    private Coroutine animacionActual;

    // Variables para almacenar el estado previo del cursor
    private CursorLockMode estadoAnteriorCursor;
    private bool visibilidadAnteriorCursor;

    private void OnEnable()
    {
        toggleMenuAction.Enable();
        toggleMenuAction.performed += OnToggleMenu;
    }

    private void OnDisable()
    {
        toggleMenuAction.Disable();
        toggleMenuAction.performed -= OnToggleMenu;
    }

    private void Start()
    {
        menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, posicionOcultaY);
        menuPanel.gameObject.SetActive(false);
        
        // Asegurar que el juego inicie con el tiempo normal
        Time.timeScale = 1f; 
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        AlternarMenu();
    }

    private void AlternarMenu()
    {
        menuAbierto = !menuAbierto;

        if (menuAbierto)
        {
            // 1. Guardar el estado del cursor antes de modificarlo
            estadoAnteriorCursor = Cursor.lockState;
            visibilidadAnteriorCursor = Cursor.visible;

            // 2. Activar UI y mostrar cursor
            menuPanel.gameObject.SetActive(true);
            ActualizarEstadoCursor(true);

            // 3. Pausar el juego
            Time.timeScale = 0f;
        }
        else
        {
            // 1. Restaurar el estado del cursor
            ActualizarEstadoCursor(false);

            // 2. Reanudar el juego
            Time.timeScale = 1f;
        }

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        float posicionObjetivoY = menuAbierto ? posicionReveladaY : posicionOcultaY;
        animacionActual = StartCoroutine(AnimarMenu(posicionObjetivoY));
    }

private void ActualizarEstadoCursor(bool menuActivo)
{
    if (menuActivo)
    {
        // Al abrir el menú: Cursor visible y libre para hacer clic en los botones
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    else
    {
        // Al cerrar el menú: Cursor invisible y confinado a la ventana
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
}

    private IEnumerator AnimarMenu(float posicionObjetivoY)
    {
        float tiempoTranscurrido = 0f;
        float posicionInicialY = menuPanel.anchoredPosition.y;

        while (tiempoTranscurrido < duracionTransicion)
        {
            // Time.unscaledDeltaTime permite que la animación progrese aunque Time.timeScale sea 0
            tiempoTranscurrido += Time.unscaledDeltaTime; 
            float porcentaje = tiempoTranscurrido / duracionTransicion;
            
            float valorCurva = curvaTransicion.Evaluate(porcentaje);
            float nuevaPosicionY = Mathf.LerpUnclamped(posicionInicialY, posicionObjetivoY, valorCurva);
            
            menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, nuevaPosicionY);

            yield return null;
        }

        menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, posicionObjetivoY);

        if (!menuAbierto)
        {
            menuPanel.gameObject.SetActive(false);
        }
    }
}