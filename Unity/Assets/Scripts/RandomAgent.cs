using Unity.Collections;
using UnityEngine;

public interface IAgent
{
    int Act(ref SpaceInvadersGameState gs, NativeArray<int> availableActions);
}

public struct RandomAgent : IAgent
{
    public Unity.Mathematics.Random rdm;
    
    public int Act(ref SpaceInvadersGameState gs, NativeArray<int> availableActions)
    {
        return availableActions[rdm.NextInt(0, availableActions.Length)];
    }}
