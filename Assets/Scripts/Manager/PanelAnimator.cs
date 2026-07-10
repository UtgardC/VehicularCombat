using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Necesario para el componente Image

public class PanelAnimator : MonoBehaviour
{
    [Header("Referencias Generales")]
    public GameObject panelObject;
    private CanvasGroup canvasGroup;

    [Header("Efecto Flicker (Solo Imagen)")]
    [Tooltip("Arrastra aquí la imagen del mapa/zona que va a parpadear")]
    public Image targetImage;
    public bool useFlickerEffect = true;

    [Header("Configuración de Efectos")]
    public float transitionDuration = 1.5f;

    private void Awake()
    {
        if (panelObject != null)
        {
            canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetupHidden()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panelObject != null) panelObject.SetActive(true);

        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = 1f;
            targetImage.color = c;
        }
    }

    public IEnumerator Animate(bool show)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        Color baseImageColor = targetImage != null ? targetImage.color : Color.white;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / transitionDuration;

            // Fade suave del fondo
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            // Flicker intencional de la foto
            if (show && useFlickerEffect && targetImage != null)
            {
                Color flickerColor = baseImageColor;
                if (Random.value > 0.8f)
                {
                    flickerColor.a = Random.Range(0.1f, 0.5f);
                }
                else
                {
                    flickerColor.a = 1f;
                }
                targetImage.color = flickerColor;
            }

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetImage != null)
        {
            Color finalColor = baseImageColor;
            finalColor.a = 1f;
            targetImage.color = finalColor;
        }

        if (!show && panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
}