namespace LAB2D.Item.Build
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 建造物品。
    /// 可直接实例化用于简单建造物品，也可被子类化用于有特殊行为的物品(如房间、床)。
    /// </summary>
    [Serializable]
    public class ABuildItem : AItem
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public int Width = 1;

        /// <summary>
        /// 高度
        /// </summary>
        public int Height = 1;

        /// <summary>
        /// 是否左下，对于大于1*1的建造物，鼠标所在的位置
        /// </summary>
        public AWorkerTask.RectType RectType = AWorkerTask.RectType.Center;

        /// <summary>
        /// 是否可以自定义尺寸
        /// </summary>
        public bool IsCustomSize = false;

        /// <summary>
        /// 瓦片名称
        /// </summary>
        public string TileName;

        /// <summary>
        /// 无参构造器（供子类和反射使用）。
        /// TileName 默认为类名。
        /// </summary>
        public ABuildItem()
        {
            this.TileName = this.GetType().Name;
        }

        /// <summary>
        /// 带瓦片名的构造器（供直接实例化使用）。
        /// </summary>
        /// <param name="tileName">瓦片名称</param>
        public ABuildItem(string tileName)
        {
            this.TileName = tileName;
        }

        /// <summary>
        /// 墙的方向
        /// </summary>
        public enum WallDirectionEnum
        {
            /// <summary>上</summary>
            TOP,

            /// <summary>下</summary>
            DOWN,

            /// <summary>左</summary>
            LEFT,

            /// <summary>右</summary>
            RIGHT,

            /// <summary>右上</summary>
            RIGHT_TOP,

            /// <summary>右下</summary>
            RIGHT_DOWN,

            /// <summary>左上</summary>
            LEFT_TOP,

            /// <summary>左下</summary>
            LEFT_DOWN,
        }

        /// <summary>
        /// 建筑解锁检查提供者 — 返回 false 时拦截玩家手动建造。
        /// 默认实现查 TechManager（科技未解锁的建筑，如聚灵阵）。
        /// 可替换为测试桩。注意：只拦 AddBuildTask（玩家放置统一入口），
        /// 房间墙/农田自动建造走 BuildMap.AddBuild，不经此处，不受影响。
        /// </summary>
        public static System.Func<string, bool> UnlockCheckProvider { get; set; }
            = (tileName) => LAB2D.Gameplay.TechManager.Instance.IsBuildUnlocked(tileName);

        /// <summary>
        /// 添加建造任务。对于多格物品（如 1×2 SingleBed、2×2 DoubleBed）：
        /// 主格注册为建造任务（visual tile + 任务），副格注册为碰撞体（IsComplete=true，无建造任务）。
        /// </summary>
        /// <param name="centerMap">参考位置（Center=中心，BottomLeft=左下角，TopLeft=左上角）</param>
        /// <param name="extra">额外信息（尺寸/RectType 覆盖）</param>
        /// <param name="priority">任务优先级，默认系统默认</param>
        public virtual void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            // 科技 gating：未解锁的建筑拦截玩家放置（建造列表仍可见，放置时提示）
            if (!UnlockCheckProvider(this.TileName))
            {
                AWorkerTask.LogProvider(
                    $"[BuildDiag] 建造被拦截：{this.TileName} 科技未解锁",
                    LogManager.LogLevelEnum.Debug);
                try
                {
                    Core.GameServices.ShowTipProvider($"「{this.TileName}」尚未研究解锁，请先在科技面板（T）研究");
                }
                catch (Exception)
                {
                    // Tip 不可用时静默降级（测试环境）
                }

                return;
            }

            var buildMap = Core.ServiceLocator.Get<Map.BuildMap>();
            // 使用 extra 中的尺寸（如果有的话），否则使用自身的 Width/Height
            int effectiveWidth = extra?.Width ?? this.Width;
            int effectiveHeight = extra?.Height ?? this.Height;
            AWorkerTask.RectType effectiveRectType = extra?.RectType ?? this.RectType;

            // 视觉宽高直接传入：GetOccupiedPositions 内部已统一 height→x、width→y 轴语义，
            // 床 1×2（BottomLeft）自动得到主格 (x,y)+副格 (x+1,y)，与 sprite 物理足迹一致
            // （见 bug-fixes.md 2026-08-14）。AddBuild 存入的也是视觉宽高，SetComplete
            // 多格传播用同一函数自然一致；UI 预览 ShowRect 用视觉宽高，三方对齐。
            var allPositions = GetOccupiedPositions(centerMap, effectiveWidth, effectiveHeight, effectiveRectType);

            // 主格：正常注册（创建建造任务 + visual tile）
            Vector3Int primaryPos = allPositions[0];
            buildMap.AddBuild(primaryPos, this.TileName, priority,
                effectiveWidth, effectiveHeight, effectiveRectType);

            // 副格：注册为碰撞体（IsComplete=true，无建造任务，不广播）
            // SetComplete 的多格逻辑会在建造完成时自动同步所有副格
            for (int i = 1; i < allPositions.Count; i++)
            {
                buildMap.RegisterCollisionTile(allPositions[i], this.TileName, null,
                    effectiveWidth, effectiveHeight, effectiveRectType);
            }

            // 建造诊断（Debug）：记录物品类型、主格坐标与全部占用格，
            // 用于比对"副格 vs sprite 足迹错位"历史 bug（床逻辑副格与碰撞注册一致性）。
            AWorkerTask.LogProvider(
                $"[BuildDiag] 建造注册 item={this.TileName} primary=({primaryPos.x},{primaryPos.y}) " +
                $"size={effectiveWidth}x{effectiveHeight} rect={effectiveRectType} " +
                $"cells=[{string.Join(",", allPositions)}]",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 计算多格建造物品占用的所有地图坐标。
        /// 轴语义与 IsAvailableMap.ShowRect、ARoom.GetBoundary 保持一致：
        /// height 沿 tile-x 扩展、width 沿 tile-y 扩展（因 MapPosToWorldPos 的 (x,y)→(y,x) 转置，
        /// 视觉竖向的宽高映射到 tile 空间需交换轴）。例如 SingleBed 1×2（BottomLeft）
        /// 占用主格 (x,y) + 副格 (x+1,y)，与 sprite 物理足迹一致（见 bug-fixes.md 2026-08-14）。
        /// </summary>
        /// <param name="centerMap">参考位置（含义由 rectType 决定）</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="rectType">矩形类型：
        ///   Center=centerMap 为中心；
        ///   BottomLeft=centerMap 为左下角，向上+向右延伸；
        ///   TopLeft=centerMap 为左上角，向下+向右延伸</param>
        /// <returns>所有占用的地图坐标列表（第一项为主格）</returns>
        public static List<Vector3Int> GetOccupiedPositions(Vector3Int centerMap, int width, int height, AWorkerTask.RectType rectType)
        {
            List<Vector3Int> positions = new List<Vector3Int>();

            int h_start, h_end, w_start, w_end;
            if (rectType == AWorkerTask.RectType.Center)
            {
                h_start = -height / 2;
                h_end = height - (height / 2);
                w_start = -width / 2;
                w_end = width - (width / 2);
            }
            else if (rectType == AWorkerTask.RectType.BottomLeft)
            {
                // 参考点为左下角，向上+向右延伸
                h_start = 0;
                h_end = height;
                w_start = 0;
                w_end = width;
            }
            else // TopLeft
            {
                // 参考点为左上角，向下+向右延伸
                h_start = 1 - height;
                h_end = 1;
                w_start = 0;
                w_end = width;
            }

            for (int i = h_start; i < h_end; i++)
            {
                for (int j = w_start; j < w_end; j++)
                {
                    // height（i）沿 tile-x 扩展，width（j）沿 tile-y 扩展。
                    // 与 ShowRect/ARoom 轴语义一致；旧实现 (x+j, y+i) 写反导致
                    // 床副格落在 (x,y+1) 与物理足迹 (x+1,y) 错位（见 bug-fixes.md 2026-08-14）。
                    positions.Add(new Vector3Int(centerMap.x + i, centerMap.y + j, 0));
                }
            }

            return positions;
        }

        /// <summary>
        /// 计算该物品占用的所有地图坐标（使用自身 Width/Height/RectType）。
        /// 第一项为主格（参考点），其余为副格。
        /// </summary>
        /// <param name="centerMap">参考位置（含义由 RectType 决定）</param>
        public List<Vector3Int> GetOccupiedPositions(Vector3Int centerMap)
        {
            return GetOccupiedPositions(centerMap, this.Width, this.Height, this.RectType);
        }

        public class Extra
        {
            /// <summary>
            /// TopLeft自定义大小需要
            /// </summary>
            public int Width = 1;

            /// <summary>
            /// TopLeft自定义大小需要
            /// </summary>
            public int Height = 1;

            public AWorkerTask.RectType RectType = AWorkerTask.RectType.Center;

            public Extra(int width, int height, AWorkerTask.RectType rectType)
            {
                this.Width = width;
                this.Height = height;
                this.RectType = rectType;
            }
        }
    }
}
