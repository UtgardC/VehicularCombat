using UnityEngine;
using UnityEngine.SceneManagement; // 🔥 Esta librería es obligatoria para cargar niveles

namespace VehicularCombat
{
    public class MenuController : MonoBehaviour
    {
        // ── Botón de Reiniciar ──────────────────────────────────────────
        public void RestartRun()
        {
            // 1. Es VITAL descongelar el tiempo antes de reiniciar, 
            // por si tu pantalla de final pausó el juego (Time.timeScale = 0)
            Time.timeScale = 1f;

            // 2. Recarga la escena actual, sin importar cómo se llame
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── Botón de Salir ──────────────────────────────────────────────
        public void QuitGame()
        {
            Debug.Log("🚪 Saliendo del juego...");

            // Esta línea cierra el juego en la versión final (el .exe compilado)
            Application.Quit();

            // Esta línea hace que el botón también funcione dentro del editor de Unity
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}