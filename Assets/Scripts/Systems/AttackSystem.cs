using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveToTargetSystem))]
public partial struct AttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TargetLockedData>();
        state.RequireForUpdate<HealthComponent>();
        state.RequireForUpdate<AttackComponent>();
        state.RequireForUpdate<AttackSpeedComponent>();
        state.RequireForUpdate<AttackRangeComponent>();
        state.RequireForUpdate<AttackCooldownComponent>();
        state.RequireForUpdate<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var healthLookupRW = SystemAPI.GetComponentLookup<HealthComponent>(false);

        foreach (var (selfLocalTransform, attack, attackSpeed, range, cooldown, target) in
                 SystemAPI.Query<LocalTransform, AttackComponent, AttackSpeedComponent, AttackRangeComponent, RefRW<AttackCooldownComponent>, TargetLockedData>())
        {
            float timeLeft = cooldown.ValueRW.TimeLeft - deltaTime;
            cooldown.ValueRW.TimeLeft = math.max(0f, timeLeft);

            Entity enemy = target.Target;
            if (enemy == Entity.Null)
                continue;

            if (!localTransformLookup.HasComponent(enemy) || !healthLookupRW.HasComponent(enemy))
                continue;

            float3 selfPos = selfLocalTransform.Position;
            float3 targetPos = localTransformLookup[enemy].Position;

            float3 distance = targetPos - selfPos;
            distance.y = 0f;

            float distSq = math.lengthsq(distance);
            float rangeSq = range.Value * range.Value;

            if (distSq > rangeSq)
                continue;

            if (cooldown.ValueRO.TimeLeft > 0f)
                continue;

            var hp = healthLookupRW[enemy];
            hp.Value -= attack.Value;
            healthLookupRW[enemy] = hp;

            cooldown.ValueRW.TimeLeft = math.max(0.01f, attackSpeed.Value);
        }
    }
}
