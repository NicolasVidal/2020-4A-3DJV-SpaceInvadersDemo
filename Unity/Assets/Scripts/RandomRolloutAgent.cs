using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Rules = SpaceInvadersGameStateRules;

public class RandomRolloutAgent : IAgent
{
    [BurstCompile]
    struct RandomRolloutJob : IJob
    {
        public SpaceInvadersGameState gs;
        
        [ReadOnly]
        public NativeArray<int> availableActions;
        public RandomAgent rdmAgent;
        
        [WriteOnly]
        public NativeArray<int> chosenAction;
        
        public void Execute()
        {
            var epochs = 100;
            var agent = rdmAgent;

            var summedScores = new NativeArray<long>(availableActions.Length, Allocator.Temp);

            var gsCopy = Rules.Clone(ref gs);
            
            for (var i = 0; i < availableActions.Length; i++)
            {
                for (var n = 0; n < epochs; n++)
                {
                    Rules.CopyTo(ref gs, ref gsCopy);
                    Rules.Step(ref gsCopy, availableActions[i]);

                    var currentDepth = 0;
                    while (!gsCopy.isGameOver)
                    {
                        Rules.Step(ref gsCopy, agent.Act(ref gsCopy, availableActions));
                        currentDepth++;
                        if (currentDepth > 500)
                        {
                            break;
                        }
                    }

                    summedScores[i] += gsCopy.playerScore;
//                    gsCopy.enemies.Dispose();
//                    gsCopy.projectiles.Dispose();
                }
            }

            var bestActionIndex = -1;
            var bestScore = long.MinValue;
            for (var i = 0; i < summedScores.Length; i++)
            {
                if (bestScore > summedScores[i])
                {
                    continue;
                }

                bestScore = summedScores[i];
                bestActionIndex = i;
            }

//            summedScores.Dispose();

            chosenAction[0] = availableActions[bestActionIndex];
        }
    }

    public int Act(ref SpaceInvadersGameState gs, NativeArray<int> availableActions)
    {
        var job = new RandomRolloutJob
        {
            availableActions = availableActions,
            gs = gs,
            chosenAction = new NativeArray<int>(1, Allocator.TempJob),
            rdmAgent = new RandomAgent{rdm = new Random((uint) Time.frameCount)}
        };

        var handle = job.Schedule();
        handle.Complete();

        var chosenAction = job.chosenAction[0];

        job.chosenAction.Dispose();
        return chosenAction;
    }
}