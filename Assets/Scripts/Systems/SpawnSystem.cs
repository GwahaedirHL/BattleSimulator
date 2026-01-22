using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpawnRequest>();
        state.RequireForUpdate<SpawnerData>();
        state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                                          .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawner, spawnerEntity) in SystemAPI.Query<SpawnerData>().WithEntityAccess().WithAll<SpawnRequest>())
        {
            for (int army = 0; army < UnitsConfig.ARMIES_COUNT; army++)
            {
                var types = ArmiesConfig.armies[army];
                if (types == null || types.Count == 0)
                    continue;

                bool isFirstArmy = army == 0;
                float3 origin = isFirstArmy ? spawner.Army0Origin : spawner.Army1Origin;
                float xDir = (army == 0) ? 1f : -1f;

                int spawnedIndex = 0;

                for (int t = 0; t < types.Count; t++)
                {
                    var unitType = types[t];
                    if (unitType.count <= 0)
                        continue;

                    var color = new float4(unitType.color.r, unitType.color.g, unitType.color.b, unitType.color.a);

                    Entity prefab = (unitType.shape == UnitShape.Sphere)
                        ? spawner.SpherePrefab
                        : spawner.CubePrefab;

                    for (int i = 0; i < unitType.count; i++)
                    {
                        var unit = ecb.Instantiate(prefab);

                        float3 pos = origin + GridOffset(spawnedIndex, spawner.UnitsPerRow, spawner.Spacing, xDir);
                        spawnedIndex++;

                        ecb.SetComponent(unit, LocalTransform.FromPositionRotationScale(
                            pos,
                            quaternion.identity,
                            unitType.size
                        ));

                        ecb.AddComponent(unit, new URPMaterialPropertyBaseColor { Value = color });

                        ComputeStats(unitType, out int hp, out int atk, out int speed, out int atkSpd);

                        ecb.AddComponent(unit, new HealthComponent { Value = hp });
                        ecb.AddComponent(unit, new AttackComponent { Value = atk });
                        ecb.AddComponent(unit, new SpeedComponent { Value = speed });
                        ecb.AddComponent(unit, new AttackSpeedComponent { Value = atkSpd });

                        if (isFirstArmy)
                            ecb.AddComponent<ArmyOneTag>(unit);
                        else
                            ecb.AddComponent<ArmyTwoTag>(unit);

                        ecb.AddComponent(unit, new RetargetData { Timer = 0f, LastDistSq = -1f, StuckTicks = 0 });
                        ecb.AddComponent(unit, new AttackRangeComponent { Value = 4f });
                        ecb.AddComponent(unit, new AttackCooldownComponent { TimeLeft = 0f });
                        ecb.AddComponent<NeedsArmyMarkerInit>(unit);
                    }
                }
            }

            ecb.RemoveComponent<SpawnRequest>(spawnerEntity);
        }
      
        if (!SystemAPI.HasSingleton<GameStartedTag>())
        {
            var e = ecb.CreateEntity();
            ecb.AddComponent<GameStartedTag>(e);
        }
    }

    float3 GridOffset(int index, int perRow, float spacing, float xDir)
    {
        int x = index / perRow;
        int z = index % perRow;
        return new float3(x * spacing * xDir, 1f, z * spacing);
    }

    void ComputeStats(UnitData unit, out int hp, out int atk, out int speed, out int atkSpd)
    {
        hp = 100;
        atk = 10;
        speed = 10;
        atkSpd = 1;

        switch (unit.shape)
        {
            case UnitShape.Cube:
                hp += 100;
                atk += 10;
                break;
            case UnitShape.Sphere:
                hp += 50;
                atk += 20;
                break;
        }

        bool isBig = unit.size >= 1f;
        if (isBig)
            hp += 50;
        else
            hp -= 50;

        var colorType = GetColorType(unit.color);
        switch (colorType)
        {
            case UnitColorType.Blue:
                atk -= 15;
                atkSpd += 4;
                speed += 10;
                break;
            case UnitColorType.Green:
                hp -= 50;
                atk += 20;
                speed -= 5;
                break;
            case UnitColorType.Red:
                hp += 200;
                atk += 40;
                speed -= 9;
                break;
        }

        if (hp < 1)
            hp = 1;
        if (atk < 0)
            atk = 0;
        if (speed < 1)
            speed = 1;
        if (atkSpd < 1)
            atkSpd = 1;
    }

    private enum UnitColorType : byte { Blue, Green, Red }

    UnitColorType GetColorType(UnityEngine.Color c)
    {
        if (c.r >= c.g && c.r >= c.b)
            return UnitColorType.Red;
        if (c.g >= c.r && c.g >= c.b)
            return UnitColorType.Green;
        return UnitColorType.Blue;
    }
}