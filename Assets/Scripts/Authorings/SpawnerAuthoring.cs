using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEngine;

public struct SpawnerData : IComponentData {
    public Entity SpherePrefab;
    public Entity CubePrefab;

    public float3 Army0Origin;
    public float3 Army1Origin;

    public int UnitsPerRow;
    public float Spacing;
}

public struct SpawnRequest : IComponentData { }
public struct GameStartedTag : IComponentData { }

public class SpawnerAuthoring : MonoBehaviour {
    public GameObject Sphere;
    public GameObject Cube;

    [Header("Spawn Points")]
    public Transform Army0SpawnPoint;
    public Transform Army1SpawnPoint;

    [Header("Layout")]
    public int UnitsPerArmy = 20;
    public int UnitsPerRow = 5;
    public float Spacing = 1.5f;

    [Header("Marker")]
    public Transform ArmyMarker;

    public class Baker : Baker<SpawnerAuthoring> {
        public override void Bake(SpawnerAuthoring a) {
            var e = GetEntity(TransformUsageFlags.None);

            AddComponent(e, new SpawnerData {
                SpherePrefab = GetEntity(a.Sphere, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
                CubePrefab = GetEntity(a.Cube, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),

                Army0Origin = a.Army0SpawnPoint.position,
                Army1Origin = a.Army1SpawnPoint.position,

                UnitsPerRow = math.max(1, a.UnitsPerRow),
                Spacing = math.max(0.01f, a.Spacing),
            });

            AddComponent<SpawnRequest>(e);
        }
    }
}