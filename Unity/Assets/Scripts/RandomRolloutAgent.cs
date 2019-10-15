using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Rules = SpaceInvadersGameStateRules;

public class RandomRolloutAgent : IAgent
{
    [BurstCompile]
    struct RandomRolloutJob : IJobParallelFor
    {
        public SpaceInvadersGameState gs;

        [ReadOnly]
        public NativeArray<int> availableActions;

        public RandomAgent rdmAgent;

        [WriteOnly]
        public NativeArray<long> summedScores;

        public void Execute(int index)
        {
            var epochs = 100;
            var agent = rdmAgent;

            var gsCopy = Rules.Clone(ref gs);

            for (var n = 0; n < epochs; n++)
            {
                Rules.CopyTo(ref gs, ref gsCopy);
                Rules.Step(ref gsCopy, availableActions[index]);

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

                summedScores[index] += gsCopy.playerScore;
            }
        }
    }

    public int Act(ref SpaceInvadersGameState gs, NativeArray<int> availableActions)
    {
        var job = new RandomRolloutJob
        {
            availableActions = availableActions,
            gs = gs,
            summedScores = new NativeArray<long>(availableActions.Length, Allocator.TempJob),
            rdmAgent = new RandomAgent {rdm = new Random((uint) Time.frameCount)}
        };

        var handle = job.Schedule(availableActions.Length, 1);
        handle.Complete();

        var bestActionIndex = -1;
        var bestScore = long.MinValue;
        for (var i = 0; i < job.summedScores.Length; i++)
        {
            if (bestScore > job.summedScores[i])
            {
                continue;
            }

            bestScore = job.summedScores[i];
            bestActionIndex = i;
        }

        var chosenAction = availableActions[bestActionIndex];

        job.summedScores.Dispose();
        return chosenAction;
    }
}