using Unity.Collections;
using UnityEngine;

public struct SpaceInvadersGameState
{
    public const long shootDelay = 30;
    public const float enemyRadius = 0.5f;
    public const float playerRadius = 0.5f;
    public const float projectileRadius = 0.1f / 60f * 10f;
    public const float projectileSpeed = 0.5f / 60f * 10f;
    public const float enemySpeed = 0.1f / 60f * 10f;
    public const float playerSpeed = 0.3f / 60f * 10f;

    [NativeDisableParallelForRestriction]
    public NativeList<Enemy> enemies;

    [NativeDisableParallelForRestriction]
    public NativeList<Projectile> projectiles;

    public Vector2 playerPosition;
    public long lastShootStep;
    public long currentGameStep;
    public bool isGameOver;
    public long playerScore;
}