using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

public struct MeteorSpawnRequest : IComponentData { }
public struct MeteorMovementRequest : IComponentData { }
public struct MeteorImpactRequest : IComponentData { }
public struct MeteorPrefabData : IComponentData
{
    public Entity Prefab;
    public float3 SpawnPosition;
}

public struct MeteorImpactData : IComponentData
{
    public int Radius;
    public int Damage;
}

public class MeteorSpawnerAuthoring : MonoBehaviour
{
    public GameObject MeteorPrefab;
    public float3 SpawnPositon;
    public int Radius;
    public int Damage;

    public class Baker : Baker<MeteorSpawnerAuthoring>
    {
        public override void Bake(MeteorSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MeteorPrefabData
            {
                Prefab = GetEntity(authoring.MeteorPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
                SpawnPosition = authoring.SpawnPositon
            });

            AddComponent(entity, new MeteorImpactData
            {
                Radius = authoring.Radius,
                Damage = authoring.Damage
            });
        }
    }
}
