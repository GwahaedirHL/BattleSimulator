using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TargetAcquireGridSystem))]
[UpdateAfter(typeof(BuildAvoidDataSystem))]
public partial struct MoveToTargetSystem : ISystem
{
    const float stopDistance = 3f;
    const float avoidRadius = 2f;
    const float avoidWeight = 2f;

    EntityQuery moveQuery;

    float cellSize;

    public void OnCreate(ref SystemState state)
    {
        cellSize = avoidRadius;

        moveQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, TargetLockedData, SpeedComponent>()
            .Build();

        state.RequireForUpdate(moveQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        var buildHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<BuildAvoidDataSystem>();
        ref var buildSystem = ref state.WorldUnmanaged.GetUnsafeSystemRef<BuildAvoidDataSystem>(buildHandle);

        var grid = buildSystem.Grid;
        var positionByEntity = buildSystem.PositionByEntity;

        var storageInfoLookup = SystemAPI.GetEntityStorageInfoLookup();

        var job = new MoveJob
        {
            DeltaTime = dt,
            StopDistance = stopDistance,
            AvoidRadiusSq = avoidRadius * avoidRadius,
            AvoidWeight = avoidWeight,
            CellSize = cellSize,
            Grid = grid,
            PositionByEntityRO = positionByEntity,
            StorageInfoLookupRO = storageInfoLookup
        };

        state.Dependency = job.ScheduleParallel(moveQuery, state.Dependency);
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        public float StopDistance;
        public float AvoidRadiusSq;
        public float AvoidWeight;
        public float CellSize;

        [ReadOnly] public NativeParallelMultiHashMap<int, AvoidGridItem> Grid;
        [ReadOnly] public NativeParallelHashMap<Entity, float3> PositionByEntityRO;
        [ReadOnly] public EntityStorageInfoLookup StorageInfoLookupRO;

        void Execute(Entity entity, ref LocalTransform selfTransform, in SpeedComponent speed, ref TargetLockedData targetData)
        {
            Entity target = targetData.Target;
            if (target == Entity.Null)
                return;

            if (!StorageInfoLookupRO.Exists(target))
            {
                targetData.Target = Entity.Null;
                return;
            }

            if (!PositionByEntityRO.TryGetValue(target, out float3 targetPos))
            {
                targetData.Target = Entity.Null;
                return;
            }

            float3 selfPos = selfTransform.Position;

            float3 to = targetPos - selfPos;
            to.y = 0f;

            float dist = math.length(to);
            if (dist <= StopDistance || dist < 0.0001f)
                return;

            float3 seekDir = to / dist;

            float3 separation = ComputeSeparation(entity, selfPos);

            float3 dir = seekDir;
            if (!separation.Equals(float3.zero))
            {
                separation = math.normalizesafe(separation);
                dir = math.normalizesafe(seekDir + separation * AvoidWeight);
            }

            float step = speed.Value * DeltaTime;
            float move = math.min(step, dist - StopDistance);

            float3 newPos = selfPos + dir * move;
            newPos.y = selfPos.y;

            selfTransform.Position = newPos;
        }

        float3 ComputeSeparation(Entity selfEntity, float3 selfPos)
        {
            int2 baseCell = (int2)math.floor(selfPos.xz / CellSize);

            int range = 1;

            float3 sum = float3.zero;

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    int2 cell = baseCell + new int2(dx, dz);
                    int key = (int)math.hash(cell);

                    AvoidGridItem item;
                    NativeParallelMultiHashMapIterator<int> it;

                    if (!Grid.TryGetFirstValue(key, out item, out it))
                        continue;

                    do
                    {
                        if (item.Entity == selfEntity)
                            continue;

                        float3 away = selfPos - item.Pos;
                        away.y = 0f;

                        float dSq = math.lengthsq(away);
                        if (dSq <= 0.000001f || dSq > AvoidRadiusSq)
                            continue;

                        sum += away / dSq;
                    }
                    while (Grid.TryGetNextValue(out item, ref it));
                }
            }

            return sum;
        }
    }
}
