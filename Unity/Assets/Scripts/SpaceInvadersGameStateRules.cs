using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class SpaceInvadersGameStateRules
{
    public static void Init(ref SpaceInvadersGameState gs)
    {
        gs.enemies = new NativeList<Enemy>(10, Allocator.Persistent);
        for (var i = 0; i < 10; i++)
        {
            var enemy = new Enemy
            {
                position = new Vector2(i - 4.5f, 9f),
                speed = new Vector2(0f, -SpaceInvadersGameState.enemySpeed)
            };
            gs.enemies.Add(enemy);
        }

        gs.projectiles = new NativeList<Projectile>(100, Allocator.Persistent);
        gs.playerPosition = new Vector2(0f, 0f);
        gs.isGameOver = false;
        gs.lastShootStep = -SpaceInvadersGameState.shootDelay;
        gs.playerScore = 0;
    }


    public static void Step(ref SpaceInvadersGameState gs, int chosenPlayerAction)
    {
        if (gs.isGameOver)
        {
            throw new Exception("YOU SHOULD NOT TRY TO UPDATE GAME STATE WHEN GAME IS OVER !!!");
        }

        UpdateEnemyPositions(ref gs);
        UpdateProjectiles(ref gs);
        HandleAgentInputs(ref gs, chosenPlayerAction);
        HandleCollisions(ref gs);
        HandleEnemyAtBottom(ref gs);
        gs.currentGameStep += 1;
    }

    static void UpdateEnemyPositions(ref SpaceInvadersGameState gs)
    {
        for (var i = 0; i < gs.enemies.Length; i++)
        {
            var enemy = gs.enemies[i];
            enemy.position += gs.enemies[i].speed;
            gs.enemies[i] = enemy;
        }
    }

    static void UpdateProjectiles(ref SpaceInvadersGameState gs)
    {
        for (var i = 0; i < gs.projectiles.Length; i++)
        {
            var projectile = gs.projectiles[i];
            projectile.position += gs.projectiles[i].speed;
            gs.projectiles[i] = projectile;
        }
    }

    static void HandleCollisions(ref SpaceInvadersGameState gs)
    {
        for (var i = 0; i < gs.projectiles.Length; i++)
        {
            var sqrDistance = (gs.projectiles[i].position - gs.playerPosition).sqrMagnitude;

            if (!(sqrDistance
                  <= Mathf.Pow(SpaceInvadersGameState.projectileRadius + SpaceInvadersGameState.playerRadius,
                      2)))
            {
                continue;
            }

            gs.isGameOver = true;
            return;
        }

        for (var i = 0; i < gs.projectiles.Length; i++)
        {
            if (gs.projectiles[i].position.y > 10)
            {
                gs.projectiles.RemoveAtSwapBack(i);
                i--;
                continue;
            }

            for (var j = 0; j < gs.enemies.Length; j++)
            {
                var sqrDistance = (gs.projectiles[i].position - gs.enemies[j].position).sqrMagnitude;

                if (!(sqrDistance
                      <= Mathf.Pow(SpaceInvadersGameState.projectileRadius + SpaceInvadersGameState.enemyRadius,
                          2)))
                {
                    continue;
                }

                gs.projectiles.RemoveAtSwapBack(i);
                i--;
                gs.enemies.RemoveAtSwapBack(j);
                j--;
                gs.playerScore += 100;
                break;
            }
        }

        if (gs.enemies.Length == 0)
        {
            gs.isGameOver = true;
        }
    }

    static void HandleAgentInputs(ref SpaceInvadersGameState gs, int chosenPlayerAction)
    {
        switch (chosenPlayerAction)
        {
            case 0: // DO NOTHING
                return;
            case 1: // LEFT
            {
                gs.playerPosition += Vector2.left * SpaceInvadersGameState.playerSpeed;
                break;
            }
            case 2: // RIGHT
            {
                gs.playerPosition += Vector2.right * SpaceInvadersGameState.playerSpeed;
                break;
            }
            case 3: // SHOOT
            {
                if (gs.currentGameStep - gs.lastShootStep < SpaceInvadersGameState.shootDelay)
                {
                    break;
                }

                gs.lastShootStep = gs.currentGameStep;
                gs.projectiles.Add(new Projectile
                {
                    position = gs.playerPosition + Vector2.up * 1.3f,
                    speed = Vector2.up * SpaceInvadersGameState.projectileSpeed
                });
                break;
            }
        }
    }

    static void HandleEnemyAtBottom(ref SpaceInvadersGameState gs)
    {
        for (var i = 0; i < gs.enemies.Length; i++)
        {
            if (gs.enemies[i].position.y >= 0)
            {
                continue;
            }

            gs.isGameOver = true;
            return;
        }
    }

    private static readonly int[] AvailableActions = new[]
    {
        0, 1, 2, 3
    };

    public static int[] GetAvailableActions(ref SpaceInvadersGameState gs)
    {
        return AvailableActions;
    }

    public static SpaceInvadersGameState Clone(ref SpaceInvadersGameState gs)
    {
        var gsCopy = new SpaceInvadersGameState();
        gsCopy.enemies = new NativeList<Enemy>(gs.enemies.Length, Allocator.Temp);
        gsCopy.enemies.AddRange(gs.enemies);
        gsCopy.projectiles = new NativeList<Projectile>(gs.projectiles.Length, Allocator.Temp);
        gsCopy.projectiles.AddRange(gs.projectiles);
        gsCopy.playerPosition = gs.playerPosition;
        gsCopy.currentGameStep = gs.currentGameStep;
        gsCopy.isGameOver = gs.isGameOver;
        gsCopy.lastShootStep = gsCopy.lastShootStep;

        return gsCopy;
    }
}