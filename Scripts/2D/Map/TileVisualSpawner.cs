namespace LAB2D.Map
{
    using System.Collections.Generic;
    using LAB2D.Manager;
    using LAB2D.Render;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Tilemap 视觉拆分器：把 tile 视觉渲染到独立 SpriteRenderer（挂 hostTransform 下
    /// "VisualSprites" 子节点，sortingLayer=Character），使建筑/树能参与按
    /// "视觉底端世界 y" 的全局排序（WorldYSortManager）。
    /// Tilemap 本体仍承担碰撞体/寻路/数据/存档/网络，其 TilemapRenderer 由宿主
    /// （BuildMap/ResourceMap）保持启用，负责渲染恒底层（Bottom）格的视觉；非恒底层格
    /// 的 tile 被置透明隐藏，视觉拆到独立 SpriteRenderer（防双重渲染）。
    ///
    /// 分层模式（layerModeResolver）：可选委托按 cell 判定分层模式（ItemData.LayerMode 开关，
    /// 建筑/掉落物/资源通用）：
    /// - Bottom：不建独立 SpriteRenderer，tile 恢复不透明颜色，直接由宿主 TilemapRenderer
    ///   渲染在地图层，永远在角色/其他建筑之下。
    /// - Alpha：tile 透明隐藏，独立 sprite 注册 WorldYSortManager 参与 y 排序，
    ///   并注册 OcclusionFader（角色在后面时淡化）。
    /// - Normal：tile 透明隐藏，独立 sprite 参与 y 排序，但不注册 OcclusionFader（不淡化）。
    ///
    /// 帧动画（animationResolver）：可选委托按 cell 返回帧动画前缀（ItemData.IsAnimation 开启且
    /// LayerMode != Bottom 时的英文名 Name）。非空时独立 sprite 视觉挂 SpriteFrameAnimator，
    /// 按名称从 AnimationManager 取 AnimationClip 经 legacy Animation 组件循环播放，
    /// 动画格 sprite 由组件接管；取不到 clip 时回退静态 tile 图。
    ///
    /// 幂等约束：
    /// - CreateOrUpdate 以 tilemap 当前状态为准（GetSprite/GetColor/GetTransformMatrix）；
    ///   无 tile 的 cell 视为删除 → 多格物品副格（纯碰撞无 tile）自动不建视觉。
    /// - 每 cell 至多一个 SpriteRenderer，字典去重；删除用 Object.Destroy 延迟销毁
    ///   （WorldYSortManager 懒清扫兜底，无需显式 Unregister）。
    /// - RuleTile 的 sprite 依赖 8 邻域 tile，写路径在 SetTile/删除后调 RefreshAround
    ///   刷新墙角/直墙形态。
    /// </summary>
    public class TileVisualSpawner
    {
        private const string ParentName = "VisualSprites";

        private readonly Tilemap tilemap;
        private readonly Transform parent;
        private readonly string sortingLayerName;
        private readonly string objectNamePrefix;
        private readonly Material material; // tile 视觉材质（复制宿主 TilemapRenderer，保证拆分后仍接收 2D Light）
        private readonly System.Func<Vector3Int, ItemLayerMode> layerModeResolver; // 非空时按 cell 判定分层模式（SO 开关）
        private readonly System.Func<Vector3Int, Color> colorProvider; // 非空时提供该格视觉应显示的颜色（如 BuildMap 建造中状态色）
        private readonly bool useTilemapColor; // false 时 SpriteRenderer 颜色强制白色（不读 tilemap 颜色）
        private readonly System.Func<Vector3Int, string> prefabResolver; // 非空时该格优先用预制体视觉（ItemData.VisualMode == Prefab 时返回 Name）
        private readonly System.Func<Vector3Int, string> animationResolver; // 非空时该格独立 sprite 视觉挂帧动画（ItemData.IsAnimation 开启时返回英文名 Name 前缀）
        private readonly Dictionary<Vector3Int, SpriteRenderer> visual = new Dictionary<Vector3Int, SpriteRenderer>();
        private readonly Dictionary<Vector3Int, GameObject> prefabVisuals = new Dictionary<Vector3Int, GameObject>();

        /// <summary>
        /// 注入 tilemap（数据源）与 hostTransform（视觉挂载点）。
        /// </summary>
        /// <param name="sortingLayerName">视觉 sprite 所在 sorting layer（须与角色同层才能交叉排序）。</param>
        /// <param name="objectNamePrefix">视觉对象命名前缀（如 "BuildVisual"）。</param>
        /// <param name="layerModeResolver">可选：按 cell 判定分层模式（如 ItemData.LayerMode）。
        /// Bottom=恒底层不建独立 sprite、直接由 TilemapRenderer 渲染；Alpha=参与 y 排序且淡化；
        /// Normal=参与 y 排序不淡化；null 默认 Alpha（参与 y 排序且淡化）。</param>
        /// <param name="colorProvider">可选：提供该格视觉应显示的颜色（含状态色，如 BuildMap 建造中半透明）；
        /// null 时 Bottom tile 恢复白色、非 Bottom sprite 按 useTilemapColor 决定。</param>
        /// <param name="useTilemapColor">true 时 SpriteRenderer 颜色跟随 tilemap 颜色；false 时强制白色
        /// （非恒底层 tile 用透明隐藏 TilemapRenderer 双重渲染，拆出视觉需不透明）。</param>
        /// <param name="prefabResolver">可选：按 cell 返回预制体名称（如 ItemData.VisualMode == Prefab 时的 Name）。
        /// 非空时该格视觉用完整预制体实例呈现（经 ResourceManager 按名加载，可带多部件/动画/组件）；
        /// 空/实例化失败走默认 tile → 单 SpriteRenderer。</param>
        /// <param name="animationResolver">可选：按 cell 返回帧动画前缀（如 ItemData.IsAnimation 开启且
        /// LayerMode != Bottom 时的英文名 Name）。非空时该格独立 SpriteRenderer 挂 SpriteFrameAnimator，
        /// 按名称从 AnimationManager 取 AnimationClip 循环播放；取不到 clip 时回退静态 tile 图。</param>
        public TileVisualSpawner(Tilemap tilemap, Transform hostTransform, string sortingLayerName, string objectNamePrefix,
            System.Func<Vector3Int, ItemLayerMode> layerModeResolver = null,
            System.Func<Vector3Int, Color> colorProvider = null,
            bool useTilemapColor = true,
            System.Func<Vector3Int, string> prefabResolver = null,
            System.Func<Vector3Int, string> animationResolver = null)
        {
            this.tilemap = tilemap;
            this.sortingLayerName = sortingLayerName;
            this.objectNamePrefix = objectNamePrefix;
            this.layerModeResolver = layerModeResolver;
            this.colorProvider = colorProvider;
            this.useTilemapColor = useTilemapColor;
            this.prefabResolver = prefabResolver;
            this.animationResolver = animationResolver;
            this.material = ResolveMaterial(tilemap);
            this.parent = new GameObject(ParentName).transform;
            this.parent.SetParent(hostTransform, false);

            // 诊断（事件点：每 TileVisualSpawner 构造一次）：暴露材质解析结果，验证拆分后仍接收 2D Light
            AWorkerTask.LogProvider(
                $"[BuildDiag] TileVisualSpawner layer={sortingLayerName} prefix={objectNamePrefix} " +
                $"mat={(this.material != null ? this.material.shader.name : "null(退默认unlit)")}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 解析 tile 视觉材质：优先复制宿主 TilemapRenderer 的材质（拆分前 Tilemap 用的
        /// lit 材质，保证建筑/树继续接收 2D Light）；宿主无 renderer 时退回 URP 2D lit shader。
        /// </summary>
        private static Material ResolveMaterial(Tilemap tilemap)
        {
            TilemapRenderer renderer = tilemap != null ? tilemap.GetComponent<TilemapRenderer>() : null;
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (lit != null)
            {
                return new Material(lit);
            }

            return null; // 极端兜底：不设置则退 SpriteRenderer 默认 unlit 材质（无光照但不会崩）
        }

        /// <summary>
        /// 以 tilemap 当前状态创建/更新 cell 的视觉（幂等，位置/图/颜色/缩放均同步）。
        /// cell 无 tile 时删除其视觉并返回 null。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>该 cell 的 SpriteRenderer（无 tile 时为 null）。</returns>
        public SpriteRenderer CreateOrUpdate(Vector3Int cell)
        {
            if (!this.tilemap.HasTile(cell))
            {
                this.Delete(cell);
                return null;
            }

            // [BuildDiag] 重影巡检：字典已有该格独立视觉（上次已按 Alpha/Normal 隐藏 tile），
            // 但 tile 当前不透明 = 隐藏被某条 SetTile 路径重置（Unity SetTile 会把 instance 颜色还原为资产默认白）
            // → TilemapRenderer 与独立 SpriteRenderer 同位置双渲染。仅在异常时告警。
            if (this.visual.TryGetValue(cell, out SpriteRenderer existingSr)
                && existingSr != null
                && this.tilemap.GetColor(cell).a > 0.5f)
            {
                AWorkerTask.LogProvider(
                    $"[BuildDiag] 重影实锤 cell=({cell.x},{cell.y}) tile={this.tilemap.GetTile(cell)?.name} " +
                    $"a={this.tilemap.GetColor(cell).a} flags={this.tilemap.GetTileFlags(cell)}（SR 在而 tile 不透明，隐藏被重置）",
                    LogManager.LogLevelEnum.Warning);
            }

            // 统一前置：解除 LockColor，否则 SetColor 不生效（收敛各 Map 层 ApplyTileVisual 的做法）
            this.tilemap.RemoveTileFlags(cell, TileFlags.LockColor);

            ItemLayerMode mode = this.layerModeResolver != null ? this.layerModeResolver(cell) : ItemLayerMode.Alpha;
            if (mode == ItemLayerMode.Bottom)
            {
                // 恒底层（SO 开关）：不建独立 SpriteRenderer，直接由宿主 TilemapRenderer 渲染在地图层。
                // tile 恢复该格应显示的颜色（colorProvider 状态色 / 默认白），并清残留视觉（sprite + prefab）。
                this.tilemap.SetColor(cell, this.colorProvider != null ? this.colorProvider(cell) : Color.white);
                this.Delete(cell);
                return null;
            }

            // 非恒底层：tile 透明隐藏 TilemapRenderer（避免双重渲染，数据/碰撞/存档保留），
            // 视觉由独立 SpriteRenderer 或配置的预制体呈现。
            this.tilemap.SetColor(cell, new Color(1f, 1f, 1f, 0f));

            // [BuildDiag] 隐藏回读：SetColor(0) 未生效（如 LockColor 残留）→ tile 将以原色显示，同位置双渲染。
            Color hiddenVerify = this.tilemap.GetColor(cell);
            if (hiddenVerify.a > 0.5f)
            {
                AWorkerTask.LogProvider(
                    $"[BuildDiag] tile 隐藏失败 cell=({cell.x},{cell.y}) tile={this.tilemap.GetTile(cell)?.name} " +
                    $"回读a={hiddenVerify.a} flags={this.tilemap.GetTileFlags(cell)}（SetColor 被吞）",
                    LogManager.LogLevelEnum.Warning);
            }

            // 预制体分支：该格物品/建筑 VisualMode == Prefab（resolver 返回其英文名 Name），用完整预制体呈现视觉
            //（可带多部件/动画/组件），而非单 SpriteRenderer。tile 仍承担数据/碰撞/存档/网络。
            string prefabName = this.prefabResolver != null ? this.prefabResolver(cell) : null;
            if (!string.IsNullOrEmpty(prefabName))
            {
                // 实例化成功用预制体视觉；失败（名字拼错/AssetBundle 未加载）落回下方 sprite 视觉
                GameObject instance = this.CreateOrUpdatePrefab(cell, prefabName);
                if (instance != null)
                {
                    return instance.GetComponent<SpriteRenderer>();
                }
            }

            // 无预制体（或实例化失败回退）：清理可能残留的 prefab 视觉（配置回退/读档切格等场景）
            this.DeletePrefab(cell);

            // 帧动画：非恒底层 + ItemData.IsAnimation 时，独立 sprite 视觉按英文名(Name)取
            // AnimationClip 循环播放（SpriteFrameAnimator 接管 sprite 显示，跳过下方静态 tile 图赋值）。
            string animationPrefix = this.animationResolver != null ? this.animationResolver(cell) : null;

            if (!this.visual.TryGetValue(cell, out SpriteRenderer sr))
            {
                GameObject go = new GameObject(this.NameOf(cell));
                go.transform.SetParent(this.parent, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = this.sortingLayerName;
                sr.sortingOrder = 0; // 每帧由 WorldYSortManager 统一分配
                WorldYSortManager.Ensure().Register(sr);
                if (mode == ItemLayerMode.Alpha)
                {
                    // 遮挡淡化：仅 Alpha 层注册为候选遮挡物（玩家走到其后变半透明）
                    OcclusionFader.Ensure().AddOccluder(sr);
                }

                if (this.material != null)
                {
                    sr.sharedMaterial = this.material; // 沿用宿主 lit 材质，保持接收 2D Light
                }

                this.visual.Add(cell, sr);
            }

            // 动画组件调和：每次以 resolver 结果为准（首次挂载 / 动画↔非动画切换 / 同格换物品重载帧序列）。
            bool isAnimated = this.SyncAnimation(cell, sr, animationPrefix);

            // 位置：GetCellCenterWorld 自动应用网格 transform（45° 转置坐标系）
            sr.transform.position = this.tilemap.GetCellCenterWorld(cell);

            // 图：GetSprite 可解析 RuleTile（依据 8 邻域）。引用变化才写，减少 SetProperty。
            // 动画格由 SpriteFrameAnimator 接管 sprite 切换（跳过，避免覆盖动画帧）。
            if (!isAnimated)
            {
                Sprite sprite = this.tilemap.GetSprite(cell);
                if (sr.sprite != sprite)
                {
                    sr.sprite = sprite;
                }
            }

            // 颜色：colorProvider 优先（状态色，如 BuildMap 建造中半透明）；否则 useTilemapColor
            // 跟随 tilemap 颜色 / 强制白色（非恒底层 tile 已置透明，独立 sprite 需不透明）。
            Color color = this.colorProvider != null ? this.colorProvider(cell)
                : (this.useTilemapColor ? this.tilemap.GetColor(cell) : Color.white);
            if (sr.color != color)
            {
                sr.color = color;
            }

            // 缩放：RuleTile 翻转等 transform matrix 的影响（本项目的墙无翻转，恒为 1）。
            Vector3 scale = this.tilemap.GetTransformMatrix(cell).lossyScale;
            if (sr.transform.localScale != scale)
            {
                sr.transform.localScale = scale;
            }

            return sr;
        }

        /// <summary>
        /// 调和 cell 的帧动画组件状态：以 animationResolver 结果为准（每次 CreateOrUpdate 调用）。
        /// 幂等重入安全，覆盖三类状态变化：
        /// - 非动画→动画：首次挂载 SpriteFrameAnimator（补挂到已有 SpriteRenderer 所在 GameObject）。
        /// - 动画→非动画：移除多余组件（同格 tile 换成非动画物品时，避免残留 Animation 组件每帧覆盖静态 sprite）。
        /// - 动画→动画但前缀变化（同格换成另一动画物品）：按新前缀重载动画 clip。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <param name="sr">该格独立 SpriteRenderer。</param>
        /// <param name="animationPrefix">当前 resolver 返回的帧动画前缀（空 = 无需动画）。</param>
        /// <returns>该格动画是否实际生效（组件已挂载且取到有效 clip）；false 时由调用方回退静态 tile 图。</returns>
        private bool SyncAnimation(Vector3Int cell, SpriteRenderer sr, string animationPrefix)
        {
            bool shouldAnimate = !string.IsNullOrEmpty(animationPrefix);
            SpriteFrameAnimator animator = sr.GetComponent<SpriteFrameAnimator>();
            if (!shouldAnimate)
            {
                // 动画→非动画：移除旧动画组件（延迟销毁，帧末后 Update 不再覆盖 sprite）
                if (animator != null)
                {
                    Object.Destroy(animator);
                }

                return false;
            }

            if (animator != null && animator.Prefix == animationPrefix)
            {
                return true; // 组件已在且帧序列一致，动画生效
            }

            // 无组件（非动画→动画）或前缀变化（同格换成另一动画物品）：挂载并加载动画 clip
            if (animator == null)
            {
                animator = sr.gameObject.AddComponent<SpriteFrameAnimator>();
            }

            if (!animator.Init(animationPrefix))
            {
                // 无有效帧序列（找不到 {prefix}_0）：移除组件，回退静态 tile 图
                Object.Destroy(animator);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 预制体视觉分支：以 prefab 实例呈现 cell 的视觉（ItemData.VisualMode == Prefab 配置）。
        /// 经 ResourceManager 按名加载/实例化（Resources/Prefabs/ 或 AssetBundle 的 prefabDic）；
        /// 实例化失败（名字拼错/未加载）返回 null，由调用方回退 sprite 视觉。
        /// 位置跟随 tilemap 格中心；实例内所有 SpriteRenderer 注册 WorldYSortManager 参与 y 排序
        /// （满足 Character 层"无未注册 renderer"约束），颜色仅应用状态色 alpha（建造中半透明/完成实心），
        /// 保留 prefab 各部件原 RGB。不注册 OcclusionFader：multi-SR 整体淡化与逐格 originalAlpha
        /// 状态冲突，prefab 视觉暂不参与遮挡淡化。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <param name="prefabName">预制体名称（ResourceManager prefabDic 的 key）。</param>
        /// <returns>预制体实例；加载/实例化失败时返回 null。</returns>
        private GameObject CreateOrUpdatePrefab(Vector3Int cell, string prefabName)
        {
            if (!this.prefabVisuals.TryGetValue(cell, out GameObject go))
            {
                // 走 ResourceManager 统一通道（含 AssetBundle 加载/失败日志）。
                // 视觉非网络对象（跨端由 tile 数据同步各自本地重建），isLocal 默认 true 本地实例化。
                go = Core.ServiceLocator.Get<ResourceManager>().Instantiate(prefabName, this.parent, false);
                if (go == null)
                {
                    // 实例化失败（名字拼错/AssetBundle 未加载）：由调用方回退 sprite 视觉
                    AWorkerTask.LogProvider(
                        $"[BuildDiag] 预制体实例化失败 cell=({cell.x},{cell.y}) prefab={prefabName}，回退 tile 视觉",
                        LogManager.LogLevelEnum.Warning);
                    return null;
                }

                go.name = this.NameOf(cell);
                this.prefabVisuals.Add(cell, go);

                // 一次性注册：实例内全部 SpriteRenderer 参与 y 排序
                foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = this.sortingLayerName;
                    sr.sortingOrder = 0; // 每帧由 WorldYSortManager 统一分配
                    WorldYSortManager.Ensure().Register(sr);
                }

                // 诊断（事件点：每格 prefab 视觉创建一次）：暴露预制体实例化结果
                AWorkerTask.LogProvider(
                    $"[BuildDiag] TileVisualSpawner 预制体视觉 cell=({cell.x},{cell.y}) prefab={prefabName}",
                    LogManager.LogLevelEnum.Debug);
            }

            go.transform.position = this.tilemap.GetCellCenterWorld(cell);

            // 状态色 alpha：仅覆盖 alpha 通道（建造中半透明/完成实心），保留 prefab 各部件原色。
            // 调用点仅事件点（AddBuild/SetComplete/同步），非每帧，遍历代价可接受。
            if (this.colorProvider != null)
            {
                float alpha = this.colorProvider(cell).a;
                foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null)
                    {
                        continue;
                    }

                    if (!Mathf.Approximately(sr.color.a, alpha))
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }

            return go;
        }

        /// <summary>
        /// 删除 cell 的预制体视觉（tile 已移除/取消建造/回退 tile 视觉时）。
        /// </summary>
        private void DeletePrefab(Vector3Int cell)
        {
            if (!this.prefabVisuals.TryGetValue(cell, out GameObject go))
            {
                return;
            }

            this.prefabVisuals.Remove(cell);
            if (go != null)
            {
                // 延迟销毁，WorldYSortManager 懒清扫兜底（无需显式 Unregister）
                Object.Destroy(go);
            }
        }

        /// <summary>
        /// 删除 cell 的视觉（tile 已移除/取消建造时）。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        public void Delete(Vector3Int cell)
        {
            this.DeletePrefab(cell);

            if (!this.visual.TryGetValue(cell, out SpriteRenderer sr))
            {
                return;
            }

            this.visual.Remove(cell);
            if (sr != null)
            {
                // 视觉销毁：从遮挡淡化候选移除（若曾注册）
                OcclusionFader.Ensure().RemoveOccluder(sr);
                Object.Destroy(sr.gameObject);
            }
        }

        /// <summary>
        /// 刷新 cell 周围 radius 邻域的视觉（RuleTile 邻居变化后更新墙角/直墙形态）。
        /// </summary>
        /// <param name="cell">中心坐标。</param>
        /// <param name="radius">邻域半径（默认 1 = 3x3）。</param>
        public void RefreshAround(Vector3Int cell, int radius = 1)
        {
            foreach (Vector3Int offset in Around(cell, radius))
            {
                this.CreateOrUpdate(offset);
            }
        }

        /// <summary>
        /// 全量重建：以 tilemap 当前状态为准（读档/全量网络同步后调用）。
        /// 先删除已无 tile 的视觉，再扫描 cellBounds 内全部有 tile 的 cell。
        /// </summary>
        public void RebuildAll()
        {
            List<Vector3Int> stale = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> kv in this.visual)
            {
                if (!this.tilemap.HasTile(kv.Key))
                {
                    stale.Add(kv.Key);
                }
            }

            foreach (KeyValuePair<Vector3Int, GameObject> kv in this.prefabVisuals)
            {
                if (!this.tilemap.HasTile(kv.Key))
                {
                    stale.Add(kv.Key);
                }
            }

            foreach (Vector3Int cell in stale)
            {
                this.Delete(cell);
            }

            BoundsInt bounds = this.tilemap.cellBounds;
            Vector3Int min = bounds.min;
            Vector3Int max = bounds.max;
            for (int x = min.x; x < max.x; x++)
            {
                for (int y = min.y; y < max.y; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (this.tilemap.HasTile(cell))
                    {
                        this.CreateOrUpdate(cell);
                    }
                }
            }
        }

        /// <summary>
        /// 清空所有视觉（场景卸载/切换地图）。
        /// </summary>
        public void ClearAll()
        {
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> kv in this.visual)
            {
                if (kv.Value != null)
                {
                    Object.Destroy(kv.Value.gameObject);
                }
            }

            this.visual.Clear();

            foreach (KeyValuePair<Vector3Int, GameObject> kv in this.prefabVisuals)
            {
                if (kv.Value != null)
                {
                    Object.Destroy(kv.Value);
                }
            }

            this.prefabVisuals.Clear();
        }

        /// <summary>
        /// 生成 cell 的 radius 邻域坐标集合（纯函数，供 RefreshAround 与单测复用）。
        /// </summary>
        /// <param name="cell">中心坐标。</param>
        /// <param name="radius">半径（>=0）。</param>
        /// <returns>包含中心在内的 (2*radius+1)^2 个坐标。</returns>
        public static Vector3Int[] Around(Vector3Int cell, int radius = 1)
        {
            int side = radius * 2 + 1;
            Vector3Int[] result = new Vector3Int[side * side];
            int i = 0;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    result[i++] = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                }
            }

            return result;
        }

        private string NameOf(Vector3Int cell)
        {
            return $"{this.objectNamePrefix}_{cell.x}_{cell.y}";
        }
    }
}
