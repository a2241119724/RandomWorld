namespace LAB2D.AI.Worker
{
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item.Build.Furniture.Bed;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 动态生成的房间布局数据，替代旧的静态硬编码常量。
    /// 注意：实例由 WorkerHomeLayout.s_roomLayoutCache 按参数组合共享（见 GetRoomLayout），
    /// 仅允许 GenerateRoomLayout 在发布前填充；调用方一律只读，不得修改任何列表/字段。
    /// </summary>
    public class RoomLayout
    {
        /// <summary>
        /// 床的定义（1×2，RectType=BottomLeft）。用于向 RegisterCollisionTile 提供
        /// Width/Height/RectType 等建造元数据。副格坐标取主格 tile-x+1（BedSecondOffset），
        /// 与修正后的 GetOccupiedPositions（height→x, width→y，见 bug-fixes.md 2026-08-14）
        /// 一致：SingleBed 1×2 逻辑副格与物理足迹同为 (x+1,y)。
        /// </summary>
        internal static readonly SingleBed BedDef = new SingleBed();

        public List<Vector3Int> WallOffsets = new List<Vector3Int>();
        public List<int> WallDirections = new List<int>();
        public Vector3Int DoorOffset;
        public Vector3Int BedOffset;  // 床参考点（左下角）在房间内的偏移（家具布局）

        /// <summary>
        /// 床副格偏移 — 取床 sprite 实际延伸的格（主格 tile-x + 1），
        /// 而非 GetOccupiedPositions 的逻辑副格（tile-y + 1）。
        /// 床 sprite 在"主格世界坐标绘制并向上延伸"（视觉竖向 1×2），
        /// 碰撞体随 sprite 覆盖主格与 tile-x+1 格；若碰撞瓦片注册在逻辑副格（y+1），
        /// 则 WalkabilityCache 与实际物理阻挡错位——A* 认为 tile-x+1 可通行而径直穿过床，
        /// 实际移动被 sprite 碰撞体挡住 → 触发 Sliding 无限重寻路（观测 53 次/人，从不入睡）。
        /// </summary>
        public Vector3Int BedSecondOffset => new Vector3Int(this.BedOffset.x + 1, this.BedOffset.y, 0);

        public List<Vector3Int> StorageOffsets = new List<Vector3Int>();     // 4 格仓库偏移
        public List<int> StorageDirections = new List<int>();                // 4 格仓库方向

        public int WallCount => this.WallOffsets.Count;
        public int DoorStage => this.WallCount;
        public int BedStage => this.WallCount + 1;
        public int StorageStage1 => this.WallCount + 2;
        public int StorageStage2 => this.WallCount + 3;
        public int StorageStage3 => this.WallCount + 4;
        public int StorageStage4 => this.WallCount + 5;
        public int CompleteStage => this.WallCount + 6;
    }

    /// <summary>
    /// Worker 房间布局生成（纯函数 + 缓存）— 墙/门/床/仓库的偏移与方向编排。
    /// H1 拆分：从 WorkerBrain 巨石文件迁出，行为零变化。
    /// </summary>
    public static class WorkerHomeLayout
    {
        /// <summary>仓库建造瓦片名称前缀（实际使用时追加方向后缀 _0~_7）。</summary>
        internal const string StorageTileName = "InventoryWall";

        /// <summary>
        /// 根据房间参数动态生成墙壁布局。
        /// 支持宽度 5/7、高度 5/7 的矩形房间（5×5~7×7），门在任意一边。
        /// 家具块 tile 空间 3×2（床+仓库），5×5 房间（内部 3×3）也能放下。
        /// </summary>
        /// <param name="width">外墙宽度（奇数 5 或 7）</param>
        /// <param name="height">外墙高度（奇数 5 或 7）</param>
        /// <param name="doorSide">门所在边: 0=左 1=右 2=上 3=下</param>
        /// <param name="doorIndex">门在该边的非角位置索引（0-based）</param>
        internal static RoomLayout GenerateRoomLayout(int width, int height, int doorSide, int doorIndex)
        {
            RoomLayout layout = new RoomLayout();
            int hw = (width - 1) / 2;   // 半宽
            int hh = (height - 1) / 2;  // 半高

            // 门位置
            layout.DoorOffset = GetDoorPosition(hw, hh, doorSide, doorIndex);

            // 上边 (y = hh): x 从 -hw 到 hw → 方向 _4
            for (int x = -hw; x <= hw; x++)
            {
                Vector3Int pos = new Vector3Int(x, hh, 0);
                if (doorSide == 2 && pos == layout.DoorOffset) continue; // 门在顶上
                layout.WallOffsets.Add(pos);
                // 角: x==-hw→_7, x==hw→_2, 中间→_4
                int dir = (x == -hw) ? 7 : (x == hw) ? 2 : 4;
                layout.WallDirections.Add(dir);
            }

            // 右边 (x = hw): y 从 hh-1 到 -hh+1 → 方向 _1
            for (int y = hh - 1; y >= -hh + 1; y--)
            {
                Vector3Int pos = new Vector3Int(hw, y, 0);
                if (doorSide == 1 && pos == layout.DoorOffset) continue;
                layout.WallOffsets.Add(pos);
                layout.WallDirections.Add(1);
            }

            // 下边 (y = -hh): x 从 hw 到 -hw → 方向 _3
            for (int x = hw; x >= -hw; x--)
            {
                Vector3Int pos = new Vector3Int(x, -hh, 0);
                if (doorSide == 3 && pos == layout.DoorOffset) continue;
                layout.WallOffsets.Add(pos);
                // 角: x==hw→_0, x==-hw→_5, 中间→_3
                int dir = (x == hw) ? 0 : (x == -hw) ? 5 : 3;
                layout.WallDirections.Add(dir);
            }

            // 左边 (x = -hw): y 从 -hh+1 到 hh-1 → 方向 _6
            for (int y = -hh + 1; y <= hh - 1; y++)
            {
                Vector3Int pos = new Vector3Int(-hw, y, 0);
                if (doorSide == 0 && pos == layout.DoorOffset) continue;
                layout.WallOffsets.Add(pos);
                layout.WallDirections.Add(6);
            }

            // 床+仓库布局：根据门的位置动态摆放，确保家具在门的对面一侧。
            // 注意：真实坐标是 Tile 坐标沿 45° 转置（世界X=tileY、世界Y=tileX）。
            // 家具块 tile 空间 3 宽 × 2 高（仓库 2×2 在左 + 床 1×2 竖放在右），
            // 转置到屏幕后床落在仓库正上方（上下布局），与墙保持至少 1 格间距。
            // 3×2 是最紧凑布局，允许房间最小 5×5（内部 3×3 恰好放下）。
            int interiorW = 2 * (hw - 1) + 1; // 内部可走区域宽度
            int interiorH = 2 * (hh - 1) + 1; // 内部可走区域高度
            const int furnW = 3; // 家具块宽度（仓库 2 列 + 床 1 列）
            const int furnH = 2; // 家具块高度（仓库 2 行）

            int furnLeft, furnBottom; // 家具块左下角（相对于房间中心）

            // 内部高度充裕（7 高：内部 5 ≥ furnH+3）时，家具与对面墙留 2 格间距；
            // 5 高内部仅 3 格高，只能留 1 格。
            bool roomyV = interiorH >= furnH + 3;

            switch (doorSide)
            {
                case 2: // 门在上边 → 家具靠下，水平居中
                    // 家具块 tile x 下移 1 格（屏幕 Y+1）：床视觉 Y 落到 [0,1]，
                    // 配合 GenerateRandomRoomParams 让门 index 避开床所在列，
                    // 使门（屏幕右墙）不再紧贴床。
                    furnLeft = -(hw - 1) + System.Math.Max(0, (interiorW - furnW) / 2) + 1;
                    furnBottom = roomyV ? -(hh - 2) : -(hh - 1);
                    break;
                case 3: // 门在下边 → 家具靠上，水平居中
                    // 同 doorSide=2：下移 1 格使床避开屏幕左墙的门。
                    furnLeft = -(hw - 1) + System.Math.Max(0, (interiorW - furnW) / 2) + 1;
                    // 床主格 y=furnBottom+2 必须落在内部（≤hh-1，即 5 高房间 y=1），
                    // 否则床 sprite 转置后会在屏幕右侧墙上（X=hh）。故取 furnBottom=-1。
                    furnBottom = roomyV ? (hh - furnH - 2) : (hh - furnH - 1);
                    break;
                case 0: // 门在左边 → 家具靠右，垂直居中
                    furnLeft = hw - furnW; // 右边 = hw-1（距右墙 1 格）
                    furnBottom = -(hh - 1) + System.Math.Max(0, (interiorH - furnH) / 2);
                    break;
                case 1: // 门在右边 → 家具靠左，垂直居中
                    furnLeft = -(hw - 1); // 左边距左墙 1 格
                    furnBottom = -(hh - 1) + System.Math.Max(0, (interiorH - furnH) / 2);
                    break;
                default:
                    furnLeft = -(hw - 1);
                    furnBottom = -(hh - 1);
                    break;
            }

            // 床 1×2 横放在仓库正上方（tile 空间，主格+副格 x+1），转置到屏幕后成竖向 1×2，
            // 视觉上位于仓库正上方并向上延伸。副格（世界/屏幕方向的上格）由 BedSecondOffset 派生。
            // 只设置床参考点（左下角，RectType=BottomLeft）。
            layout.BedOffset = new Vector3Int(furnLeft, furnBottom + 2, 0);  // 床参考点（左下角）

            // 4 格仓库形成 2×2 "田"字方块，放在床左侧
            layout.StorageOffsets.Add(new Vector3Int(furnLeft, furnBottom + 1, 0));     // 左上
            layout.StorageDirections.Add(7);
            layout.StorageOffsets.Add(new Vector3Int(furnLeft, furnBottom, 0));         // 左下
            layout.StorageDirections.Add(5);
            layout.StorageOffsets.Add(new Vector3Int(furnLeft + 1, furnBottom + 1, 0)); // 右上
            layout.StorageDirections.Add(2);
            layout.StorageOffsets.Add(new Vector3Int(furnLeft + 1, furnBottom, 0));     // 右下
            layout.StorageDirections.Add(0);

            return layout;
        }

        /// <summary>根据门参数计算门在房间外墙上的绝对偏移。</summary>
        private static Vector3Int GetDoorPosition(int hw, int hh, int doorSide, int doorIndex)
        {
            return doorSide switch
            {
                0 => new Vector3Int(-hw, hh - 1 - doorIndex, 0),       // 左边，从上往下
                1 => new Vector3Int(hw, hh - 1 - doorIndex, 0),        // 右边，从上往下
                2 => new Vector3Int(-hw + 1 + doorIndex, hh, 0),       // 上边，从左往右
                3 => new Vector3Int(-hw + 1 + doorIndex, -hh, 0),      // 下边，从左往右
                _ => new Vector3Int(-hw, 0, 0),
            };
        }

        /// <summary>为 Worker 随机生成房间参数并存储到 WorkerData。</summary>
        internal static void GenerateRandomRoomParams(AWorker.WorkerData wd)
        {
            // 重试生成参数，直到门位可用。逐个候选门位置验证进门第一格（entry）不落在
            // 家具占位（床主格/床副格/仓库）上——否则从预注册那一刻起该格被注册为不可通行
            // 碰撞体，房间入口封死，Worker 永远无法进入（观测 50 次/人"没有找到路"重试）。
            // 旧逻辑只避开床主格所在列，漏掉床副格列与仓库列，5 宽房间（家具块占满内部
            // 宽度）doorSide=0 时所有门位全堵，必须换参数重试而非接受一个封死的门。
            const int maxAttempts = 8;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 宽高都随机 5/7（5×5~7×7）：家具块 tile 空间 3×2，5×5 房间（内部 3×3）也能放下。
                wd.HomeRoomWidth = UnityEngine.Random.value < 0.5f ? 5 : 7;
                wd.HomeRoomHeight = UnityEngine.Random.value < 0.5f ? 5 : 7;
                // 随机门朝向
                wd.HomeDoorSide = UnityEngine.Random.Range(0, 4);
                // 随机门位置（非角位置数量 = 对应边长度 - 2）
                int sideLen = (wd.HomeDoorSide == 0 || wd.HomeDoorSide == 1)
                    ? wd.HomeRoomHeight : wd.HomeRoomWidth;
                int maxIndex = sideLen - 3; // 非角位置数量 - 1
                if (maxIndex < 0) maxIndex = 0;

                List<int> validIndices = new List<int>();
                for (int i = 0; i <= maxIndex; i++)
                {
                    RoomLayout probe = GenerateRoomLayout(
                        wd.HomeRoomWidth, wd.HomeRoomHeight, wd.HomeDoorSide, i);
                    if (!IsDoorEntryBlockedByFurniture(probe, wd.HomeDoorSide))
                    {
                        validIndices.Add(i);
                    }
                }

                if (validIndices.Count == 0) continue; // 该组合所有门位都堵，换参数重试

                wd.HomeDoorIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];

                // 调试：打印新房间布局字符画（屏幕/世界坐标，即转置后视角）
                PrintRoomLayout(wd);
                return;
            }

            // 兜底：极端几何下随机组合连续全堵。7×7 内部宽度/高度 5，家具块仅占 3 列，
            // 任一墙面（左右墙 x 向、上下墙 y 向）的进门第一格都与家具块相距 ≥1 格，永不堵门
            // ——用确定的可用组合收尾，而不是接受一个封死房间（那会复现本次修复要消灭的 bug）。
            wd.HomeRoomWidth = 7;
            wd.HomeRoomHeight = 7;
            wd.HomeDoorSide = 1; // 右墙：家具靠左，进门格在右侧
            wd.HomeDoorIndex = 0;
            PrintRoomLayout(wd);
        }

        /// <summary>
        /// 判断门位是否会导致"进门第一格"落在家具占位上（房间被封死）。
        /// 门在墙上，Worker 从门外朝内走 1 格即进房；若这一格是床/仓库占位，
        /// 则该占位在预注册阶段就被注册为不可通行碰撞体，入口形同虚设。
        /// </summary>
        /// <param name="layout">该门位的候选布局</param>
        /// <param name="doorSide">门所在边: 0=左 1=右 2=上 3=下</param>
        /// <returns>true = 进门第一格被家具挡住</returns>
        private static bool IsDoorEntryBlockedByFurniture(RoomLayout layout, int doorSide)
        {
            Vector3Int entry = doorSide switch
            {
                0 => layout.DoorOffset + new Vector3Int(1, 0, 0),  // 左墙 → 向右进
                1 => layout.DoorOffset + new Vector3Int(-1, 0, 0), // 右墙 → 向左进
                2 => layout.DoorOffset + new Vector3Int(0, -1, 0), // 上墙 → 向下进
                3 => layout.DoorOffset + new Vector3Int(0, 1, 0),  // 下墙 → 向上进
                _ => layout.DoorOffset,
            };

            if (entry == layout.BedOffset) return true;
            if (entry == layout.BedSecondOffset) return true;
            foreach (Vector3Int so in layout.StorageOffsets)
            {
                if (entry == so) return true;
            }

            return false;
        }

        /// <summary>
        /// 用字符画打印房间布局到日志（屏幕/世界坐标，即 45° 转置后视角）。
        /// 图例: #=墙 D=门 B=床 S=仓库 .=空地
        /// 屏幕 (X,Y) 对应 tile 偏移 (ox=Y, oy=X)，与 MapPosToWorldPos 一致。
        /// </summary>
        private static void PrintRoomLayout(AWorker.WorkerData wd)
        {
            var layout = GenerateRoomLayout(
                wd.HomeRoomWidth, wd.HomeRoomHeight,
                wd.HomeDoorSide, wd.HomeDoorIndex);
            int hw = (wd.HomeRoomWidth - 1) / 2;  // tile 半宽
            int hh = (wd.HomeRoomHeight - 1) / 2; // tile 半高

            // 床 sprite 永远竖向（上下）显示。
            // 床物理足迹 = 主格 BedOffset + 副格 tile-x+1（BedSecondOffset），转置到屏幕即世界坐标竖向延伸；
            // 与修正后的 GetOccupiedPositions（height→x, width→y）逻辑副格一致（见 bug-fixes.md 2026-08-14）。
            // 打印按视觉显示：主格 tile 与其 tile-x+1 相邻格（世界坐标竖向）。
            Vector3Int bedMain = layout.BedOffset;
            Vector3Int bedVis = new Vector3Int(bedMain.x + 1, bedMain.y, 0);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"== 房间布局 {wd.HomeRoomWidth}x{wd.HomeRoomHeight} | 门=边{wd.HomeDoorSide} idx{wd.HomeDoorIndex} ==");
            sb.AppendLine("图例: #=墙 D=门 B=床 S=仓库 .=空地");
            sb.Append("    ");
            for (int X = -hh; X <= hh; X++)
            {
                sb.Append($"{X,2} ");
            }
            sb.AppendLine();

            // 行 Y 从上到下（hw→-hw），列 X 从左到右（-hh→hh）
            for (int Y = hw; Y >= -hw; Y--)
            {
                sb.Append($"{Y,3} ");
                for (int X = -hh; X <= hh; X++)
                {
                    Vector3Int off = new Vector3Int(Y, X, 0); // tile 偏移 (ox=Y, oy=X)
                    char c = '.';
                    if (off == layout.DoorOffset) c = 'D';
                    else if (layout.StorageOffsets.Contains(off)) c = 'S';
                    else if (off == bedMain || off == bedVis) c = 'B';
                    else if (layout.WallOffsets.Contains(off)) c = '#';
                    sb.Append($" {c} ");
                }
                sb.AppendLine();
            }

            AWorkerTask.LogProvider(sb.ToString(), LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 房间布局缓存：布局仅由 (宽, 高, 门边, 门位) 四参数决定，组合有限
        /// （5|7 × 5|7 × 4 边 × ≤5 门位 &lt; 64 种），而决策/存取/建造路径每 tick 反复调用
        /// GetRoomLayout（每次 new RoomLayout + 4 个 List + 逐墙填充 → 决策级高频 GC 压力）。
        /// 已审计全部调用方对布局只读（唯一写入点 GenerateRoomLayout 在实例发布前完成），
        /// 命中直接共享同一实例，消除每次调用的容器分配； RoomLayout 必须保持"发布后不可变"约定。
        /// </summary>
        private static readonly Dictionary<int, RoomLayout> s_roomLayoutCache
            = new Dictionary<int, RoomLayout>();

        /// <summary>从 WorkerData 参数生成房间布局。如果参数未设置则自动生成。</summary>
        public static RoomLayout GetRoomLayout(AWorker.WorkerData wd)
        {
            if (wd.HomeRoomWidth == 0)
            {
                GenerateRandomRoomParams(wd);
            }

            // 参数紧凑打包为单个 int key（宽/高各 4bit、门边 3bit、门位 8bit，取值远小于位宽）
            int key = wd.HomeRoomWidth
                | (wd.HomeRoomHeight << 4)
                | (wd.HomeDoorSide << 8)
                | (wd.HomeDoorIndex << 12);
            if (!s_roomLayoutCache.TryGetValue(key, out RoomLayout layout))
            {
                layout = GenerateRoomLayout(
                    wd.HomeRoomWidth, wd.HomeRoomHeight,
                    wd.HomeDoorSide, wd.HomeDoorIndex);
                s_roomLayoutCache[key] = layout;
            }

            return layout;
        }
    }
}
