using UnityEngine;

public interface IAgent
{
    int Act(ref SpaceInvadersGameState gs, int[] availableActions);
}

public class RandomAgent : IAgent
{
    public int Act(ref SpaceInvadersGameState gs, int[] availableActions)
    {
        return availableActions[Random.Range(0, availableActions.Length)];
    }
}