using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SpawnSystem))]
public partial struct ArmyMarkerColorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NeedsArmyMarkerInit>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (markerRef, e) in SystemAPI.Query<ArmyMarkerRef>().WithAll<NeedsArmyMarkerInit>().WithEntityAccess())
        {
            float4 color =
                SystemAPI.HasComponent<ArmyOneTag>(e) ? new float4(1, 0, 1, 1) : // magenta
                SystemAPI.HasComponent<ArmyTwoTag>(e) ? new float4(1, 1, 0, 1) : // yellow
                new float4(1, 1, 1, 1);

            var marker = markerRef.MarkerEntity;

            if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(marker))
                ecb.SetComponent(marker, new URPMaterialPropertyBaseColor { Value = color });
            else
                ecb.AddComponent(marker, new URPMaterialPropertyBaseColor { Value = color });

            ecb.RemoveComponent<NeedsArmyMarkerInit>(e);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}