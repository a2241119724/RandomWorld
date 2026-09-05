namespace LAB2D.Render
{
    using LAB2D;
    using LAB2D.Character.Player;
    using LAB2D.Core;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 遮挡淡化 — 树/建筑等环境视觉遮挡本地玩家时，降低遮挡物透明度，保证玩家可见。
    ///
    /// 判定：遮挡物与玩家 SpriteRenderer.bounds 相交，且遮挡物 sortingOrder > 玩家
    /// （y 排序规则下 order 大者后绘制、盖住先绘制者 → 玩家被其遮住）。
    /// 命中后遮挡物 alpha 平滑渐变到 OccludedAlpha，离开遮挡后平滑恢复原透明度。
    ///
    /// 候选遮挡物由 TileVisualSpawner 注册（参与 y 排序的非恒底层环境视觉，含树/建筑/掉落物），
    /// 创建/销毁视觉时增删，保持候选集始终准确（无需每帧重新扫描地图）。
    ///
    /// 性能架构（2026-09-06 100 Worker 卡顿重构）：空间网格。
    /// 遮挡物全是静态视觉（树/建筑不动），按格分桶；每帧只查玩家所在格 ±1（3×3 格，
    /// 格边长 = CheckRadius，保证覆盖判定圆）≈ 十几个候选 + 一个"未恢复集合"继续收敛 alpha。
    /// 旧版每帧全量遍历 ~7 千候选（~2 万次引擎调用/帧，见 bug-fixes.md 2026-09-06）。
    /// 注意：注册发生在 TileVisualSpawner 配置 position 之前，注册只进 pending 队列，
    /// 下一次 LateUpdate（位置已落地）才计算格 key 分桶。
    /// </summary>
    [DefaultExecutionOrder(200)] // 晚于 WorldYSortManager(0)：读取本帧最新的 sortingOrder
    public class OcclusionFader : MonoBehaviour
    {
        /// <summary>遮挡时环境视觉的目标透明度。</summary>
        public const float OccludedAlpha = 0.3f;

        /// <summary>透明度渐变速率（alpha/秒）。</summary>
        public const float FadeSpeed = 6f;

        /// <summary>距离预过滤半径（世界单位）：仅检测树底与玩家距离在此内的遮挡物，减少 bounds 计算。</summary>
        private const float CheckRadius = 6f;

        /// <summary>网格格边长 = CheckRadius：玩家格 ±1（3×3）必然覆盖判定圆。</summary>
        private const float CellSize = CheckRadius;

        private class Entry
        {
            public SpriteRenderer renderer;
            public float originalAlpha;
            public int cellKey;          // 分桶 key（入列时按最终位置计算）
            public bool fading;          // alpha 未恢复到原值 → 在未恢复集合中
            public int lastProcessedFrame; // 本帧已处理标记（网格命中与未恢复集合去重）
        }

        private readonly Dictionary<SpriteRenderer, Entry> entryMap = new Dictionary<SpriteRenderer, Entry>();
        private readonly Dictionary<int, List<Entry>> grid = new Dictionary<int, List<Entry>>();
        private readonly List<Entry> pending = new List<Entry>(); // 已注册待分桶（等 TileVisualSpawner 配置位置）
        private readonly List<Entry> fading = new List<Entry>();  // alpha 收敛中（被遮挡渐隐或恢复中）

        private SpriteRenderer playerRenderer;

        private static OcclusionFader instance;

        /// <summary>
        /// 获取单例；不存在则懒创建（与 WorldYSortManager 同模式）。
        /// </summary>
        public static OcclusionFader Ensure()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<OcclusionFader>();
            if (instance == null)
            {
                GameObject go = new GameObject("OcclusionFader");
                instance = go.AddComponent<OcclusionFader>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }

        /// <summary>
        /// 注册一个候选遮挡物（环境视觉创建时，由 TileVisualSpawner 调用，幂等）。
        /// 注册时不读位置（调用方可能尚未配置），下一次 LateUpdate 才分桶。
        /// </summary>
        public void AddOccluder(SpriteRenderer sr)
        {
            if (sr == null || this.entryMap.ContainsKey(sr))
            {
                return;
            }

            Entry e = new Entry { renderer = sr, originalAlpha = sr.color.a, lastProcessedFrame = -1 };
            this.entryMap.Add(sr, e);
            this.pending.Add(e);
        }

        /// <summary>
        /// 移除一个候选遮挡物（环境视觉销毁时，由 TileVisualSpawner 调用）。
        /// </summary>
        public void RemoveOccluder(SpriteRenderer sr)
        {
            if (sr == null || !this.entryMap.TryGetValue(sr, out Entry e))
            {
                return;
            }

            this.entryMap.Remove(sr);
            this.pending.Remove(e);
            if (this.grid.TryGetValue(e.cellKey, out List<Entry> bucket))
            {
                bucket.Remove(e);
            }

            if (e.fading)
            {
                e.fading = false;
                this.fading.Remove(e);
            }
        }

        private void LateUpdate()
        {
            // 0. pending 分桶：此刻 TileVisualSpawner 的位置配置已落地
            for (int i = 0; i < this.pending.Count; i++)
            {
                Entry e = this.pending[i];
                if (e.renderer == null)
                {
                    this.entryMap.Remove(e.renderer);
                    continue;
                }

                e.cellKey = CellKey(e.renderer.transform.position);
                if (!this.grid.TryGetValue(e.cellKey, out List<Entry> bucket))
                {
                    bucket = new List<Entry>();
                    this.grid.Add(e.cellKey, bucket);
                }

                bucket.Add(e);
            }

            this.pending.Clear();

            this.RefreshPlayerRenderer();
            if (this.playerRenderer == null)
            {
                return;
            }

            Bounds playerBounds = this.playerRenderer.bounds;
            int playerOrder = this.playerRenderer.sortingOrder;
            Vector3 playerPos = this.playerRenderer.transform.position;
            float checkRadiusSq = CheckRadius * CheckRadius;
            float dt = Time.deltaTime;
            int frameId = Time.frameCount;

            // 1. 玩家格 ±1（3×3）内的候选做遮挡判定（仅 ~十几个，格边长=判定半径保证覆盖圆）
            int pcx = Mathf.FloorToInt(playerPos.x / CellSize);
            int pcy = Mathf.FloorToInt(playerPos.y / CellSize);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!this.grid.TryGetValue(((pcx + dx) * 65536) + (pcy + dy), out List<Entry> bucket))
                    {
                        continue;
                    }

                    for (int i = bucket.Count - 1; i >= 0; i--)
                    {
                        Entry e = bucket[i];
                        SpriteRenderer sr = e.renderer;
                        if (sr == null)
                        {
                            // 视觉已销毁（砍树等）：从候选移除，懒清扫兜底
                            bucket.RemoveAt(i);
                            this.entryMap.Remove(sr);
                            if (e.fading)
                            {
                                e.fading = false;
                                this.fading.Remove(e);
                            }

                            continue;
                        }

                        // 距离预过滤：树底远离玩家时树冠不可能盖住玩家，跳过遮挡判定（省 bounds 计算）。
                        // 恢复不设距离门：由下方未恢复集合兜底（玩家传送/快速离开半径时继续收敛回原值）。
                        Vector3 diff = sr.transform.position - playerPos;
                        bool occludes = diff.sqrMagnitude <= checkRadiusSq
                            && sr.sortingOrder > playerOrder
                            && sr.bounds.Intersects(playerBounds);

                        e.lastProcessedFrame = frameId;
                        this.ConvergeAlpha(e, occludes, dt);
                    }
                }
            }

            // 2. 未恢复集合：不在判定邻域内（玩家已离开）或已判定但仍收敛中的条目，继续向原值收敛；
            //    收敛完成即移出。本帧已在邻域处理过的跳过（避免双倍收敛速率）。
            for (int i = this.fading.Count - 1; i >= 0; i--)
            {
                Entry e = this.fading[i];
                if (e.renderer == null)
                {
                    this.fading.RemoveAt(i);
                    continue;
                }

                if (e.lastProcessedFrame == frameId)
                {
                    continue;
                }

                this.ConvergeAlpha(e, false, dt);
            }
        }

        /// <summary>alpha 平滑渐变（保留 RGB，不覆盖 tilemap 颜色），并维护未恢复集合。</summary>
        private void ConvergeAlpha(Entry e, bool occludes, float dt)
        {
            float target = occludes ? OccludedAlpha : e.originalAlpha;
            SpriteRenderer sr = e.renderer;
            Color c = sr.color;
            float alpha = Mathf.MoveTowards(c.a, target, FadeSpeed * dt);
            if (!Mathf.Approximately(c.a, alpha))
            {
                sr.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (Mathf.Approximately(alpha, e.originalAlpha))
            {
                if (e.fading)
                {
                    e.fading = false;
                    this.fading.Remove(e);
                }
            }
            else if (!e.fading)
            {
                e.fading = true;
                this.fading.Add(e);
            }
        }

        private static int CellKey(Vector3 pos)
        {
            int cx = Mathf.FloorToInt(pos.x / CellSize);
            int cy = Mathf.FloorToInt(pos.y / CellSize);
            return (cx * 65536) + cy;
        }

        private void RefreshPlayerRenderer()
        {
            // Unity == null 同时捕获引用为空与对象销毁
            if (this.playerRenderer != null)
            {
                return;
            }

            this.playerRenderer = null;
            if (Core.ServiceLocator.TryGet<PlayerManager>(out PlayerManager pm) && pm.Mine != null)
            {
                this.playerRenderer = pm.Mine.GetComponent<SpriteRenderer>();
            }
        }
    }
}
