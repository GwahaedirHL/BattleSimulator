using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct AvoidGridItem
{
    public Entity Entity;
    public float3 Pos;
}
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuildAvoidDataSystem : ISystem
{
    public NativeParallelMultiHashMap<int, AvoidGridItem> Grid;
    public NativeParallelHashMap<Entity, float3> PositionByEntity;

    EntityQuery buildQuery;

    float cellSize;

    public void OnCreate(ref SystemState state)
    {
        cellSize = 2f;

        Grid = new NativeParallelMultiHashMap<int, AvoidGridItem>(1024, Allocator.Persistent);
        PositionByEntity = new NativeParallelHashMap<Entity, float3>(1024, Allocator.Persistent);

        buildQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform>()
            .WithAny<ArmyOneTag, ArmyTwoTag>()
            .Build();

        state.RequireForUpdate(buildQuery);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (Grid.IsCreated)
            Grid.Dispose();
        if (PositionByEntity.IsCreated)
            PositionByEntity.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int count = buildQuery.CalculateEntityCount();

        EnsureCapacity(ref Grid, count);
        EnsureCapacity(ref PositionByEntity, count);

        Grid.Clear();
        PositionByEntity.Clear();

        var job = new BuildAvoidDataJob
        {
            CellSize = cellSize,
            GridWriter = Grid.AsParallelWriter(),
            PositionWriter = PositionByEntity.AsParallelWriter()
        };

        state.Dependency = job.ScheduleParallel(buildQuery, state.Dependency);
    }

    static void EnsureCapacity(ref NativeParallelMultiHashMap<int, AvoidGridItem> map, int required)
    {
        if (map.Capacity < required)
            map.Capacity = math.max(required, map.Capacity * 2);
    }

    static void EnsureCapacity(ref NativeParallelHashMap<Entity, float3> map, int required)
    {
        if (map.Capacity < required)
            map.Capacity = math.max(required, map.Capacity * 2);
    }

    [BurstCompile]
    public partial struct BuildAvoidDataJob : IJobEntity
    {
        public float CellSize;

        public NativeParallelMultiHashMap<int, AvoidGridItem>.ParallelWriter GridWriter;
        public NativeParallelHashMap<Entity, float3>.ParallelWriter PositionWriter;

        void Execute(Entity entity, in LocalTransform transform)
        {
            float3 pos = transform.Position;

            int2 cell = (int2)math.floor(pos.xz / CellSize);
            int key = (int)math.hash(cell);

            GridWriter.Add(key, new AvoidGridItem { Entity = entity, Pos = pos });
            PositionWriter.TryAdd(entity, pos);
        }
    }
}

