using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AttackSystem))]
public partial struct DeathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HealthComponent>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                                          .CreateCommandBuffer(state.WorldUnmanaged);

        var dead = new NativeList<Entity>(Allocator.Temp);

        foreach (var (hp, e) in SystemAPI.Query<RefRO<HealthComponent>>().WithEntityAccess())
        {
            if (hp.ValueRO.Value <= 0)
                dead.Add(e);
        }

        if (dead.Length == 0)
        {
            dead.Dispose();
            return;
        }

        for (int i = 0; i < dead.Length; i++)
        {
            Entity deadEntity = dead[i];

            foreach (var target in SystemAPI.Query<RefRW<TargetLockedData>>())
            {
                if (target.ValueRO.Target == deadEntity)
                    target.ValueRW.Target = Entity.Null;
            }

            ecb.DestroyEntity(deadEntity);
        }

        dead.Dispose();
    }
}
