using System;

using Unity.Collections;
using Unity.Entities;

public partial class CheckEndBattleSystem : SystemBase
{
    public event Action<int> Win;

    EntityQuery army1Query;
    EntityQuery army2Query;

    protected override void OnCreate()
    {
        army1Query = GetEntityQuery(ComponentType.ReadOnly<ArmyOneTag>());
        army2Query = GetEntityQuery(ComponentType.ReadOnly<ArmyTwoTag>());

        RequireForUpdate<GameStartedTag>();
    }

    protected override void OnUpdate()
    {
        int army1Count = army1Query.CalculateEntityCount();
        int army2Count = army2Query.CalculateEntityCount();

        if (army1Count != 0 && army2Count != 0)
            return;

        int winner = (army1Count == 0) ? 2 : 1;
        UnityEngine.Debug.Log($"{winner} wins");
        Win?.Invoke(winner);
        Win = null;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        if(SystemAPI.TryGetSingletonEntity<GameStartedTag>(out var e))
        {
            ecb.DestroyEntity(e);
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
