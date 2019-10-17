using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Rules = SpaceInvadersGameStateRules;

public struct Node
{
    public int action;
    public int nc;
    public long rc;
    public int npc;
}

public struct SelectedNodeInfo
{
    public long hash;
    public int nodeIndex;
}

public class MCTSAgent : IAgent
{
    [BurstCompile]
    struct MCTSAgentJob : IJob
    {
        public SpaceInvadersGameState gs;

        [ReadOnly]
        public NativeArray<int> availableActions;

        public RandomAgent rdmAgent;

        [WriteOnly]
        public NativeArray<long> summedScores;

        public void Execute()
        {
            var epochs = 200;
            var agent = rdmAgent;

            var gsCopy = Rules.Clone(ref gs);

            var rootHash = Rules.GetHashCode(ref gsCopy);

            // CREATION DE LA MEMOIRE (Arbre)
            var memory = new NativeHashMap<long, NativeList<Node>>(2048, Allocator.Temp);
            memory.TryAdd(rootHash, new NativeList<Node>(availableActions.Length, Allocator.Temp));

            for (var i = 0; i < availableActions.Length; i++)
            {
                memory[rootHash]
                    .Add(new Node
                    {
                        action = availableActions[i],
                        nc = 0,
                        npc = 0,
                        rc = 0
                    });
            }

            for (var n = 0; n < epochs; n++)
            {
                Rules.CopyTo(ref gs, ref gsCopy);
                var currentHash = rootHash;

                var selectedNodes = new NativeList<SelectedNodeInfo>(Allocator.Temp);

                //SELECT
                while (!gsCopy.isGameOver)
                {
                    var hasUnexploredNodes = false;

                    for (var i = 0; i < memory[currentHash].Length; i++)
                    {
                        if (memory[currentHash][i].nc == 0)
                        {
                            hasUnexploredNodes = true;
                            break;
                        }
                    }

                    if (hasUnexploredNodes)
                    {
                        break;
                    }

                    var bestNodeIndex = -1;
                    var bestNodeScore = float.MinValue;

                    for (var i = 0; i < memory[currentHash].Length; i++)
                    {
                        var list = memory[currentHash];
                        var node = list[i];
                        node.npc += 1;
                        list[i] = node;
                        memory[currentHash] = list;

                        var score = (float) memory[currentHash][i].rc / memory[currentHash][i].nc
                                    + math.sqrt(2 * math.log(memory[currentHash][i].npc) / memory[currentHash][i].nc);

                        if (score >= bestNodeScore)
                        {
                            bestNodeIndex = i;
                            bestNodeScore = score;
                        }
                    }

                    selectedNodes.Add(new SelectedNodeInfo
                    {
                        hash = currentHash,
                        nodeIndex = bestNodeIndex
                    });
                    Rules.Step(ref gsCopy, memory[currentHash][bestNodeIndex].action);
                    currentHash = Rules.GetHashCode(ref gsCopy);

                    if (!memory.ContainsKey(currentHash))
                    {
                        memory.TryAdd(currentHash, new NativeList<Node>(availableActions.Length, Allocator.Temp));

                        for (var i = 0; i < availableActions.Length; i++)
                        {
                            memory[currentHash]
                                .Add(new Node
                                {
                                    action = availableActions[i],
                                    nc = 0,
                                    npc = 0,
                                    rc = 0
                                });
                        }
                    }
                }

                //EXPAND
                if (!gsCopy.isGameOver)
                {
                    var unexploredActions = new NativeList<int>(Allocator.Temp);

                    for (var i = 0; i < memory[currentHash].Length; i++)
                    {
                        if (memory[currentHash][i].nc == 0)
                        {
                            unexploredActions.Add(i);
                        }
                    }

                    var chosenNodeIndex = agent.rdm.NextInt(0, unexploredActions.Length);

                    selectedNodes.Add(new SelectedNodeInfo
                    {
                        hash = currentHash,
                        nodeIndex = unexploredActions[chosenNodeIndex]
                    });
                    Rules.Step(ref gsCopy, memory[currentHash][unexploredActions[chosenNodeIndex]].action);
                    currentHash = Rules.GetHashCode(ref gsCopy);

                    if (!memory.ContainsKey(currentHash))
                    {
                        memory.TryAdd(currentHash, new NativeList<Node>(availableActions.Length, Allocator.Temp));

                        for (var i = 0; i < availableActions.Length; i++)
                        {
                            memory[currentHash]
                                .Add(new Node
                                {
                                    action = availableActions[i],
                                    nc = 0,
                                    npc = 0,
                                    rc = 0
                                });
                        }
                    }
                }

                //SIMULATE
                while (!gsCopy.isGameOver)
                {
                    var chosenActionIndex = agent.rdm.NextInt(0, availableActions.Length);
                    Rules.Step(ref gsCopy, chosenActionIndex);
                }


                //BACKPROPAGATE
                for (var i = 0; i < selectedNodes.Length; i++)
                {
                    var list = memory[selectedNodes[i].hash];
                    var node = list[selectedNodes[i].nodeIndex];

                    node.rc += gsCopy.playerScore;
                    node.nc += 1;

                    list[selectedNodes[i].nodeIndex] = node;

                    memory[selectedNodes[i].hash] = list;
                }
            }

            for (var i = 0; i < memory[rootHash].Length; i++)
            {
                summedScores[i] = memory[rootHash][i].nc;
            }
        }
    }

    public int Act(ref SpaceInvadersGameState gs, NativeArray<int> availableActions)
    {
        var job = new MCTSAgentJob
        {
            availableActions = availableActions,
            gs = gs,
            summedScores = new NativeArray<long>(availableActions.Length, Allocator.TempJob),
            rdmAgent = new RandomAgent {rdm = new Random((uint) Time.frameCount)}
        };

        var handle = job.Schedule();
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