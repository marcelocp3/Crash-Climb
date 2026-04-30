using UnityEngine;

namespace CrashClimb
{
    public static class CrashClimbBootstrap2D
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameExists()
        {
            EnsureHealthHudExists();

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
    }
}
