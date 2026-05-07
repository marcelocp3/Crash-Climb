using UnityEngine;

namespace CrashClimb
{
    public static class CrashClimbBootstrap2D
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameExists()
        {
            EnsureRuntimeObjects();
        }

        public static void EnsureRuntimeObjects()
        {
            EnsureHealthHudExists();
            EnsureMainMenuExists();

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
