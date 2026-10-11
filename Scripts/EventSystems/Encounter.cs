using System;

// Remembers which enemy the next battle is against, across scene changes
public static class Encounter
{
    private static Func<Enemy> nextEnemy; // set when a fight is started through the run

    public static void Begin(Func<Enemy> createEnemy, string transitionScenePath = null)
    {
        nextEnemy = createEnemy;

        if (transitionScenePath != null)
            SceneTransition.Instance.GoToWith(transitionScenePath, Run.CombatScenePath);
        else
            SceneTransition.Instance.GoTo(Run.CombatScenePath);
    }

    // Opened directly (F6): use the run's current encounter instead of a fixed enemy
    public static Enemy CreateEnemy() => nextEnemy != null ? nextEnemy() : Run.CreateCurrentEnemy();
}