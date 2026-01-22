using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

public struct ArmyOneTag : IComponentData { }
public struct ArmyTwoTag : IComponentData { }
public struct NeedsArmyMarkerInit : IComponentData { }

public struct HealthComponent : IComponentData
{
    public int Value;
}

public struct SpeedComponent : IComponentData
{
    public int Value;
}

public struct AttackSpeedComponent : IComponentData
{
    public int Value;
}

public struct AttackComponent : IComponentData
{
    public int Value;
}

public struct TargetLockedData : IComponentData
{
    public Entity Target;
}

public struct RetargetData : IComponentData
{
    public float Timer;
    public float LastDistSq;
    public int StuckTicks;
}

public struct AttackRangeComponent : IComponentData
{
    public float Value;
}

public struct AttackCooldownComponent : IComponentData
{
    public float TimeLeft;
}

public struct ArmyMarkerRef : IComponentData
{
    public Entity MarkerEntity;
}

public class UnitAuthoring : MonoBehaviour
{
    [Header("Marker child")]
    public Transform ArmyMarker;

    class Backer : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TargetLockedData
            {
                Target = Entity.Null
            });

            var markerEntity = GetEntity(authoring.ArmyMarker, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new ArmyMarkerRef { MarkerEntity = markerEntity });
        }
    }
}
