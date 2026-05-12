using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrashClimb
{
    public static class CrashClimbBootstrap2D
    {
        private const string GameplaySceneName = "Main";
        private static bool sceneEventsRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneEvents()
        {
            if (sceneEventsRegistered)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneEventsRegistered = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameExists()
        {
            EnsureRuntimeObjects();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimeObjects();
        }

        public static void EnsureRuntimeObjects()
        {
            EnsureMainMenuExists();

            if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            EnsureHealthHudExists();
            EnsureMapExists();
        }

        private static bool IsGameplayScene(string sceneName)
        {
            return sceneName == GameplaySceneName;
        }

        private static void EnsureMapExists()
        {
            if (Object.FindFirstObjectByType<CrashClimbProceduralMap2D>() != null)
            {
                return;
            }

            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<CrashClimbProceduralMap2D>();
        }

        private static void EnsureHealthHudExists()
        {
            if (Object.FindFirstObjectByType<CrashClimbHealthHud2D>() != null)
            {
                return;
            }

            GameObject hud = new GameObject("Crash Climb HUD");
            hud.AddComponent<CrashClimbHealthHud2D>();
        }

        private static void EnsureMainMenuExists()
        {
            if (Object.FindFirstObjectByType<CrashClimbMainMenu2D>() != null)
            {
                return;
            }

            GameObject menu = new GameObject("Crash Climb Main Menu");
            menu.AddComponent<CrashClimbMainMenu2D>();
        }
    }
}
