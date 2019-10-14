using System;
using System.Collections.Generic;
using UnityEngine;
using Rules = SpaceInvadersGameStateRules;

public class GameSystemScript : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public GameObject ProjectilePrefab;
    public GameObject PlayerPrefab;

    private SpaceInvadersGameState gs;

    private readonly List<Transform> enemiesView = new List<Transform>();
    private readonly List<Transform> projectilesView = new List<Transform>();
    private Transform playerView;
    private IAgent agent;

    void Start()
    {
        Rules.Init(ref gs);

        playerView = Instantiate(PlayerPrefab).GetComponent<Transform>();
//        agent = new RandomAgent();
//        agent = new HumanAgent();
        agent = new RandomRolloutAgent();
    }

    void Update()
    {
        if (gs.isGameOver)
        {
            return;
        }
        
        SyncEnemyViews();
        SyncProjectileViews();
        playerView.position = gs.playerPosition;

        Rules.Step(ref gs, agent.Act(ref gs, Rules.GetAvailableActions(ref gs)));
    }

    private void SyncEnemyViews()
    {
        var enemiesToSpawn = gs.enemies.Length - enemiesView.Count;

        for (var i = 0; i < enemiesToSpawn; i++)
        {
            var enemyView = Instantiate(EnemyPrefab).GetComponent<Transform>();
            enemiesView.Add(enemyView);
        }

        for (var i = 0; i < -enemiesToSpawn; i++)
        {
            Destroy(enemiesView[enemiesView.Count - 1].gameObject);
            enemiesView.RemoveAt(enemiesView.Count - 1);
        }

        for (var i = 0; i < enemiesView.Count; i++)
        {
            enemiesView[i].position = gs.enemies[i].position;
        }
    }

    private void SyncProjectileViews()
    {
        var projectilesToSpawn = gs.projectiles.Length - projectilesView.Count;

        for (var i = 0; i < projectilesToSpawn; i++)
        {
            var projectileView = Instantiate(ProjectilePrefab).GetComponent<Transform>();
            projectilesView.Add(projectileView);
        }

        for (var i = 0; i < -projectilesToSpawn; i++)
        {
            Destroy(projectilesView[projectilesView.Count - 1].gameObject);
            projectilesView.RemoveAt(projectilesView.Count - 1);
        }

        for (var i = 0; i < projectilesView.Count; i++)
        {
            projectilesView[i].position = gs.projectiles[i].position;
        }
    }

    private void OnDestroy()
    {
        gs.enemies.Dispose();
        gs.projectiles.Dispose();
    }
}