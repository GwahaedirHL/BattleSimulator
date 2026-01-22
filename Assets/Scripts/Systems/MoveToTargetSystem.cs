using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TargetAcquireSystem))]
public partial struct MoveToTargetSystem : ISystem
{
    const float StopDistance = 3f;
    const float AvoidRadius = 2f;
    const float AvoidWeight = 2f;
    EntityQuery allQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalTransform>();
        state.RequireForUpdate<TargetLockedData>();
        state.RequireForUpdate<SpeedComponent>();

        var allQuery = SystemAPI.QueryBuilder()
                                         .WithAll<LocalTransform>()
                                         .WithAll<TargetLockedData>()
                                         .Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var allQuery = SystemAPI.QueryBuilder()
                                         .WithAll<LocalTransform>()
                                         .WithAll<TargetLockedData>()
                                         .Build();

        var allEntities = allQuery.ToEntityArray(Allocator.Temp);
        var allTransforms = allQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        float avoidRadiusSq = AvoidRadius * AvoidRadius;

        foreach (var (selfTransform, speed, targetData, selfEntity) in
                 SystemAPI.Query<RefRW<LocalTransform>, SpeedComponent, TargetLockedData>().WithEntityAccess())
        {
            Entity target = targetData.Target;
            if (target == Entity.Null)
                continue;

            if (!targetTransformLookup.HasComponent(target))
                continue;

            float3 selfPos = selfTransform.ValueRW.Position;
            float3 targetPos = targetTransformLookup[target].Position;

            float3 to = targetPos - selfPos;
            to.y = 0f;

            float dist = math.length(to);
            if (dist <= StopDistance || dist < 0.0001f)
                continue;

            float3 seekDir = to / dist;

            float3 separation = float3.zero;

            for (int i = 0; i < allEntities.Length; i++)
            {
                var other = allEntities[i];
                if (other == selfEntity)
                    continue;

                float3 otherPos = allTransforms[i].Position;

                float3 away = selfPos - otherPos;
                away.y = 0f;

                float dSq = math.lengthsq(away);
                if (dSq <= 0.000001f || dSq > avoidRadiusSq)
                    continue;

                separation += away / dSq;
            }

            float3 dir = seekDir;

            if (!separation.Equals(float3.zero))
            {
                separation = math.normalizesafe(separation);
                dir = math.normalizesafe(seekDir + separation * AvoidWeight);
            }

            float step = speed.Value * deltaTime;
            float move = math.min(step, dist - StopDistance);

            float3 newPos = selfPos + dir * move;
            newPos.y = selfPos.y;

            selfTransform.ValueRW.Position = newPos;
        }

        allEntities.Dispose();
        allTransforms.Dispose();
    }
}
