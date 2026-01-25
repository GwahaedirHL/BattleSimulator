using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct MeteorSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MeteorSpawnRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        var prefabData = SystemAPI.GetSingleton<MeteorPrefabData>();
        var impactData = SystemAPI.GetSingleton<MeteorImpactData>();

        var meteor = ecb.Instantiate(prefabData.Prefab);
        ecb.SetComponent(meteor, LocalTransform.FromPositionRotationScale(
            prefabData.SpawnPosition,
            quaternion.identity,
            30f
            ));

        ecb.AddComponent<Disabled>(meteor);
        ecb.AddComponent<MeteorMovementRequest>(meteor);

        ecb.AddComponent(meteor, impactData);

        ecb.RemoveComponent<Disabled>(meteor);

        if (SystemAPI.TryGetSingletonEntity<MeteorSpawnRequest>(out var e))
            ecb.DestroyEntity(e);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MeteorMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MeteorMovementRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var deltaTime = SystemAPI.Time.DeltaTime;
        int speed = 70;

        foreach (var (localTransform, meteorEntity) 
                    in SystemAPI.Query<RefRW<LocalTransform>>()
                    .WithAll<MeteorMovementRequest>()
                    .WithEntityAccess())
        {
            localTransform.ValueRW.Position.y -= deltaTime * speed;

            if(localTransform.ValueRW.Position.y <= 0.00001f)
            {
                ecb.RemoveComponent<MeteorMovementRequest>(meteorEntity);
                ecb.AddComponent<MeteorImpactRequest>(meteorEntity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DeathSystem))]
public partial struct MeteorImpactSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MeteorImpactRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        foreach (var (meteorTransform, impact, meteorEntity)
                 in SystemAPI.Query<RefRO<LocalTransform>, RefRO<MeteorImpactData>>()
                     .WithAll<MeteorImpactRequest>()
                     .WithEntityAccess())
        {
            float3 center = meteorTransform.ValueRO.Position;
            float radius = impact.ValueRO.Radius;
            float radiusSq = radius * radius;
            int damage = impact.ValueRO.Damage;

            foreach (var (hp, unitTransform)
                     in SystemAPI.Query<RefRW<HealthComponent>, RefRO<LocalTransform>>()
                         .WithAny<ArmyOneTag, ArmyTwoTag>())
            {
                float distSq = math.distancesq(unitTransform.ValueRO.Position, center);
                if (distSq <= radiusSq)
                {
                    hp.ValueRW.Value -= damage;
                }
            }

            ecb.RemoveComponent<MeteorImpactRequest>(meteorEntity);
            ecb.DestroyEntity(meteorEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}