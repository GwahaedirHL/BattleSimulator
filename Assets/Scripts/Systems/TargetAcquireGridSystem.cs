using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct GridItem
{
    public Entity Entity;
    public float3 Pos;
}

[BurstCompile]
public partial struct TargetAcquireGridSystem : ISystem
{
    NativeParallelMultiHashMap<int, GridItem> armyOneGrid;
    NativeParallelMultiHashMap<int, GridItem> armyTwoGrid;

    EntityQuery armyOneBuildQuery;
    EntityQuery armyTwoBuildQuery;

    EntityQuery armyOneFindQuery;
    EntityQuery armyTwoFindQuery;

    float cellSize;

    float baseSearchRadiusSq;
    float maxSearchRadiusSq;

    public void OnCreate(ref SystemState state)
    {
        float baseSearchRadius = 6f;
        float maxSearchRadius = 50f;

        cellSize = baseSearchRadius;

        baseSearchRadiusSq = baseSearchRadius * baseSearchRadius;
        maxSearchRadiusSq = maxSearchRadius * maxSearchRadius;

        armyOneGrid = new NativeParallelMultiHashMap<int, GridItem>(1024, Allocator.Persistent);
        armyTwoGrid = new NativeParallelMultiHashMap<int, GridItem>(1024, Allocator.Persistent);

        armyOneBuildQuery = SystemAPI.QueryBuilder()
            .WithAll<ArmyOneTag, LocalTransform>()
            .Build();

        armyTwoBuildQuery = SystemAPI.QueryBuilder()
            .WithAll<ArmyTwoTag, LocalTransform>()
            .Build();

        armyOneFindQuery = SystemAPI.QueryBuilder()
            .WithAll<ArmyOneTag, LocalTransform, TargetLockedData>()
            .Build();

        armyTwoFindQuery = SystemAPI.QueryBuilder()
            .WithAll<ArmyTwoTag, LocalTransform, TargetLockedData>()
            .Build();

        state.RequireForUpdate(armyOneBuildQuery);
        state.RequireForUpdate(armyTwoBuildQuery);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (armyOneGrid.IsCreated)
            armyOneGrid.Dispose();
        if (armyTwoGrid.IsCreated)
            armyTwoGrid.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int armyOneCount = armyOneBuildQuery.CalculateEntityCount();
        int armyTwoCount = armyTwoBuildQuery.CalculateEntityCount();

        EnsureCapacity(ref armyOneGrid, armyOneCount);
        EnsureCapacity(ref armyTwoGrid, armyTwoCount);

        armyOneGrid.Clear();
        armyTwoGrid.Clear();

        var buildOne = new BuildGridJob
        {
            CellSize = cellSize,
            Writer = armyOneGrid.AsParallelWriter()
        }.ScheduleParallel(armyOneBuildQuery, state.Dependency);

        var buildTwo = new BuildGridJob
        {
            CellSize = cellSize,
            Writer = armyTwoGrid.AsParallelWriter()
        }.ScheduleParallel(armyTwoBuildQuery, buildOne);

        var storageInfoLookup = SystemAPI.GetEntityStorageInfoLookup();

        var findForArmyOne = new FindTargetExpandingJob
        {
            EnemyGrid = armyTwoGrid,
            CellSize = cellSize,
            BaseSearchRadiusSq = baseSearchRadiusSq,
            MaxSearchRadiusSq = maxSearchRadiusSq,
            StorageInfoLookupRO = storageInfoLookup
        }.ScheduleParallel(armyOneFindQuery, buildTwo);

        var findForArmyTwo = new FindTargetExpandingJob
        {
            EnemyGrid = armyOneGrid,
            CellSize = cellSize,
            BaseSearchRadiusSq = baseSearchRadiusSq,
            MaxSearchRadiusSq = maxSearchRadiusSq,
            StorageInfoLookupRO = storageInfoLookup
        }.ScheduleParallel(armyTwoFindQuery, findForArmyOne);

        state.Dependency = findForArmyTwo;
    }

    static void EnsureCapacity(ref NativeParallelMultiHashMap<int, GridItem> map, int required)
    {
        if (map.Capacity < required)
            map.Capacity = math.max(required, map.Capacity * 2);
    }

    [BurstCompile]
    public partial struct BuildGridJob : IJobEntity
    {
        public float CellSize;
        public NativeParallelMultiHashMap<int, GridItem>.ParallelWriter Writer;

        void Execute(Entity entity, in LocalTransform transform)
        {
            int key = HashCell(transform.Position, CellSize);
            Writer.Add(key, new GridItem { Entity = entity, Pos = transform.Position });
        }
    }

    [BurstCompile]
    public partial struct FindTargetExpandingJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, GridItem> EnemyGrid;
        [ReadOnly] public EntityStorageInfoLookup StorageInfoLookupRO;

        public float CellSize;
        public float BaseSearchRadiusSq;
        public float MaxSearchRadiusSq;


        void Execute(in LocalTransform selfLt, ref TargetLockedData target)
        {
            if (target.Target != Entity.Null && !StorageInfoLookupRO.Exists(target.Target))
                target.Target = Entity.Null;

            if (target.Target != Entity.Null)
                return;

            float3 selfPos = selfLt.Position;

            Entity best = Entity.Null;
            float bestDistSq = float.MaxValue;
            
            int baseRange = RangeFromRadiusSq(BaseSearchRadiusSq, CellSize);
            SearchInRange(selfPos, baseRange, BaseSearchRadiusSq, ref best, ref bestDistSq);

            
            if (best == Entity.Null)
            {
                int maxRange = RangeFromRadiusSq(MaxSearchRadiusSq, CellSize);

                int range = math.max(2, baseRange * 2);
                while (range <= maxRange && best == Entity.Null)
                {
                    float radiusSq = math.min(MaxSearchRadiusSq, (range * CellSize) * (range * CellSize));
                    SearchInRange(selfPos, range, radiusSq, ref best, ref bestDistSq);
                    range *= 2;
                }

               
                if (best == Entity.Null && range / 2 != maxRange)
                    SearchInRange(selfPos, maxRange, MaxSearchRadiusSq, ref best, ref bestDistSq);
            }

            target.Target = best;
        }

        void SearchInRange(float3 selfPos, int range, float radiusSq, ref Entity best, ref float bestDistSq)
        {
            int2 baseCell = (int2)math.floor(selfPos.xz / CellSize);

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    int2 cell = baseCell + new int2(dx, dz);
                    int key = (int)math.hash(cell);

                    GridItem item;
                    NativeParallelMultiHashMapIterator<int> it;

                    if (!EnemyGrid.TryGetFirstValue(key, out item, out it))
                        continue;

                    do
                    {
                        float d = math.distancesq(selfPos, item.Pos);
                        if (d <= radiusSq && d < bestDistSq)
                        {
                            bestDistSq = d;
                            best = item.Entity;
                        }
                    }
                    while (EnemyGrid.TryGetNextValue(out item, ref it));
                }
            }
        }

        static int RangeFromRadiusSq(float radiusSq, float cellSize)
        {
            float radius = math.sqrt(radiusSq);
            return (int)math.ceil(radius / cellSize);
        }
    }

    static int HashCell(float3 pos, float cellSizeValue)
    {
        int2 c = (int2)math.floor(pos.xz / cellSizeValue);
        return (int)math.hash(c);
    }
}