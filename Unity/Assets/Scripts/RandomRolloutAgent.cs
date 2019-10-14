using Unity.Collections;
using Rules = SpaceInvadersGameStateRules;

public class RandomRolloutAgent : IAgent
{
    public int Act(ref SpaceInvadersGameState gs, int[] availableActions)
    {
        var epochs = 10;
        var agent = new RandomAgent();

        var summedScores = new NativeArray<long>(availableActions.Length, Allocator.Temp);

        for (var i = 0; i < availableActions.Length; i++)
        {
            for (var n = 0; n < epochs; n++)
            {
                var gsCopy = Rules.Clone(ref gs);

                Rules.Step(ref gsCopy, availableActions[i]);

                var currentDepth = 0;
                while (!gsCopy.isGameOver)
                {
                    Rules.Step(ref gsCopy, agent.Act(ref gsCopy, Rules.GetAvailableActions(ref gsCopy)));
                    currentDepth++;
                    if (currentDepth > 500)
                    {
                        break;
                    }
                }

                summedScores[i] += gsCopy.playerScore;
                gsCopy.enemies.Dispose();
                gsCopy.projectiles.Dispose();
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
        
        summedScores.Dispose();

        return availableActions[bestActionIndex];
    }
}