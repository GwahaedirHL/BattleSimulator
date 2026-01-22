using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveToTargetSystem))]
public partial struct RetargetStuckSystem : ISystem
{
    private const float CheckInterval = 2f;
    private const float MinProgress = 2f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalTransform>();
        state.RequireForUpdate<TargetLockedData>();
        state.RequireForUpdate<RetargetData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var army1Query = SystemAPI.QueryBuilder().WithAll<ArmyOneTag, LocalTransform>().Build();
        var army2Query = SystemAPI.QueryBuilder().WithAll<ArmyTwoTag, LocalTransform>().Build();

        var army1Entities = army1Query.ToEntityArray(Allocator.Temp);
        var army1Transforms = army1Query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        var army2Entities = army2Query.ToEntityArray(Allocator.Temp);
        var army2Transforms = army2Query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        var localTransformsLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        foreach (var (selfLt, targetRw, retargetRw, selfEntity) in
                 SystemAPI.Query<LocalTransform, RefRW<TargetLockedData>, RefRW<RetargetData>>().WithEntityAccess())
        {
            Entity target = targetRw.ValueRO.Target;
            if (target == Entity.Null)
                continue;

            if (!localTransformsLookup.HasComponent(target))
            {
                targetRw.ValueRW.Target = Entity.Null;
                retargetRw.ValueRW.LastDistSq = -1f;
                retargetRw.ValueRW.Timer = 0f;
                retargetRw.ValueRW.StuckTicks = 0;
                continue;
            }

            retargetRw.ValueRW.Timer += deltaTime;
            if (retargetRw.ValueRO.Timer < CheckInterval)
                continue;

            retargetRw.ValueRW.Timer = 0f;

            float3 selfPos = selfLt.Position;
            float3 targetPos = localTransformsLookup[target].Position;

            float3 to = targetPos - selfPos;
            to.y = 0f;

            float distSq = math.lengthsq(to);

            if (retargetRw.ValueRO.LastDistSq < 0f)
            {
                retargetRw.ValueRW.LastDistSq = distSq;
                retargetRw.ValueRW.StuckTicks = 0;
                continue;
            }

            float prev = retargetRw.ValueRO.LastDistSq;
            float progress = prev - distSq;
           
            if (progress < MinProgress)
            {
                retargetRw.ValueRW.StuckTicks += 1;
                
                if (retargetRw.ValueRO.StuckTicks >= 1)
                {
                    Entity newTarget;
                    if (SystemAPI.HasComponent<ArmyOneTag>(selfEntity))
                        newTarget = FindNearestEnemyExcluding(selfPos, army2Entities, army2Transforms, target);
                    else
                        newTarget = FindNearestEnemyExcluding(selfPos, army1Entities, army1Transforms, target);

                    targetRw.ValueRW.Target = newTarget;

                    
                    retargetRw.ValueRW.LastDistSq = -1f;
                    retargetRw.ValueRW.StuckTicks = 0;
                }
            }
            else
            {
                retargetRw.ValueRW.LastDistSq = distSq;
                retargetRw.ValueRW.StuckTicks = 0;
            }
        }

        army1Entities.Dispose();
        army1Transforms.Dispose();
        army2Entities.Dispose();
        army2Transforms.Dispose();
    }

    Entity FindNearestEnemyExcluding(float3 selfPos, NativeArray<Entity> enemyEntities, NativeArray<LocalTransform> enemyTransforms, Entity exclude)
    {
        float bestDistSq = float.MaxValue;
        Entity best = Entity.Null;

        for (int i = 0; i < enemyEntities.Length; i++)
        {
            Entity e = enemyEntities[i];
            if (e == exclude)
                continue;

            float3 enemyPos = enemyTransforms[i].Position;
            float3 d = enemyPos - selfPos;
            d.y = 0f;

            float distSq = math.lengthsq(d);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = e;
            }
        }

        return best;
    }
}
