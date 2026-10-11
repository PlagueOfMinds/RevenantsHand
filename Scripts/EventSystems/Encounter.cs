using System;

// Remembers which enemy the next battle is against, across scene changes
public static class Encounter
{
	// Used when the Combat scene is run directly (F6)
	private static Func<Enemy> nextEnemy = () => new Revenant();

	public static void Begin(Func<Enemy> createEnemy)
	{
		nextEnemy = createEnemy;
		SceneTransition.Instance.GoTo(Run.CombatScenePath);
	}

	public static Enemy CreateEnemy() => nextEnemy();
}
