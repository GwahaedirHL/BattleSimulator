using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct TargetAcquireSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalTransform>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var army1Query = SystemAPI.QueryBuilder()
                                           .WithAll<ArmyOneTag, LocalTransform>()
                                           .Build();

        var army2Query = SystemAPI.QueryBuilder()
                                           .WithAll<ArmyTwoTag, LocalTransform>()
                                           .Build();

        var army1Entities = army1Query.ToEntityArray(Allocator.Temp);
        var army1Transforms = army1Query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        var army2Entities = army2Query.ToEntityArray(Allocator.Temp);
        var army2Transforms = army2Query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        var ltLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        
        foreach (var (selfLt, target, selfEntity) in
                 SystemAPI.Query<LocalTransform, RefRW<TargetLockedData>>().WithAll<ArmyOneTag>().WithEntityAccess())
        {
            if (target.ValueRO.Target != Entity.Null && !ltLookup.HasComponent(target.ValueRO.Target))
                target.ValueRW.Target = Entity.Null;

            if (target.ValueRO.Target != Entity.Null)
                continue;

            target.ValueRW.Target = FindNearestEnemy(selfLt.Position, army2Entities, army2Transforms);
        }

        foreach (var (selfLt, target, selfEntity) in
                 SystemAPI.Query<LocalTransform, RefRW<TargetLockedData>>()
                     .WithAll<ArmyTwoTag>()
                     .WithEntityAccess())
        {
            if (target.ValueRO.Target != Entity.Null && !ltLookup.HasComponent(target.ValueRO.Target))
                target.ValueRW.Target = Entity.Null;

            if (target.ValueRO.Target != Entity.Null)
                continue;

            target.ValueRW.Target = FindNearestEnemy(selfLt.Position, army1Entities, army1Transforms);
        }

        army1Entities.Dispose();
        army1Transforms.Dispose();
        army2Entities.Dispose();
        army2Transforms.Dispose();
    }

    Entity FindNearestEnemy(float3 selfPos, NativeArray<Entity> enemyEntities, NativeArray<LocalTransform> enemyTransforms)
    {
        if (enemyEntities.Length == 0)
            return Entity.Null;

        float bestDistSq = float.MaxValue;
        Entity best = Entity.Null;

        for (int i = 0; i < enemyEntities.Length; i++)
        {
            float3 enemyPos = enemyTransforms[i].Position;
            float d = math.distancesq(selfPos, enemyPos);
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = enemyEntities[i];
            }
        }

        return best;
    }
}
