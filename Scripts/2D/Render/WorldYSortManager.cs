namespace LAB2D.Render
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 全局"按视觉底端世界 y"排序渲染顺序的管理器。
    /// 每帧 LateUpdate 将注册的 SpriteRenderer 按底端 y 降序分配唯一 sortingOrder：
    /// 底端 y 大（屏幕上方/远处）→ order 小（先绘制，被覆盖）；
    /// 底端 y 小（屏幕下方/近处）→ order 大（后绘制，盖住上方）。
    /// 约束：所有被排序的 renderer 必须位于同一 sorting layer（Character），
    /// 且该层内不得存在未注册的 renderer（否则其 order 固定为 0 会错乱）。
    /// 注册方：角色在 Character.Start 注册（动态），建筑/树 sprite 由 TileVisualSpawner 注册（静态）；
    /// 注销不强制，LateUpdate 懒清扫已销毁的 renderer（覆盖延迟销毁/漏注销）。
    ///
    /// 性能架构（2026-09-06 100 Worker 卡顿重构）：静态/动态分层。
    /// 树/建筑等静态视觉（全图 ~1.5 万）bottomY 在入列时缓存一次、仅在批量脏时排一次序；
    /// 每帧只有 ~200 个动态角色读 position/sprite（约 600 次引擎调用），
    /// 再与有序静态表做纯内存双指针归并派发唯一 order——语义与旧版全量排序一致。
    /// 旧版每帧全量重算+全量排序（1.5 万条 × 3 引擎调用 + 1.5 万元素比较排序 ≈ 3-7ms/帧，
    /// 见 bug-fixes.md 2026-09-06）。
    /// 注意：注册发生在 TileVisualSpawner 配置 position/sprite 之前，故注册只进 pending 队列，
    /// 下一次 LateUpdate（配置已落地）才读 bounds/位置入列；原地换图（RuleTile 邻居变化）由
    /// TileVisualSpawner 调 <see cref="MarkStaticDirty"/> 打脏重算。
    /// </summary>
    public class WorldYSortManager : MonoBehaviour
    {
        /// <summary>
        /// 不参与 y 排序的"恒底层"渲染器固定 sortingOrder。
        /// 动态排序占用 0..N-1，恒底层用负值固定在最底（永远先绘制、被覆盖），
        /// 角色/其他建筑在其后绘制永远盖住它。
        /// </summary>
        public const int BottomLayerOrder = -1000;

        private class Entry
        {
            public SpriteRenderer renderer;
            public Transform transform;
            public float bottomOffset; // bounds.min.y - position.y（动态：sprite/scale 变化时重算）
            public float bottomY;      // 静态：入列时缓存；动态：每帧重算
            public int lastOrder = int.MinValue;
            public bool isDynamic;
            public bool isPending; // 注册后等待入列（TileVisualSpawner 的 position/sprite 配置落地）
            public Sprite lastSprite;  // 仅动态使用：静态视觉的原地换图走 MarkStaticDirty 显式打脏
            public Vector3 lastScale;
        }

        /// <summary>bottomY 降序比较器（静态/动态共用）。List.Sort 不稳定，但同 y 并列时
        /// 旧版也只是注册序（无视觉语义）；输入顺序确定 → 输出确定，不会帧间闪烁。</summary>
        private static readonly BottomYDescComparer SortComparer = new BottomYDescComparer();

        private sealed class BottomYDescComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y) => y.bottomY.CompareTo(x.bottomY);
        }

        private readonly Dictionary<SpriteRenderer, Entry> entryMap = new Dictionary<SpriteRenderer, Entry>();
        private readonly List<Entry> pending = new List<Entry>();  // 已注册待入列（等 TileVisualSpawner 配置落地）
        private readonly List<Entry> statics = new List<Entry>();  // 静态，按缓存 bottomY 降序
        private readonly List<Entry> dynamics = new List<Entry>(); // 动态，每帧重排
        private bool staticsDirty = true;

        private static WorldYSortManager instance;

        /// <summary>
        /// 获取单例；不存在则懒创建。
        /// </summary>
        public static WorldYSortManager Ensure()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<WorldYSortManager>();
            if (instance == null)
            {
                GameObject go = new GameObject("WorldYSortManager");
                instance = go.AddComponent<WorldYSortManager>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }

        /// <summary>
        /// 注册一个参与 y 排序的 SpriteRenderer（幂等，重复注册忽略）。
        /// 注册时不读 bounds/位置（调用方可能尚未配置），下一次 LateUpdate 才入列。
        /// </summary>
        /// <param name="sr">渲染器。</param>
        /// <param name="isDynamic">是否移动实体（角色）：每帧读位置重排；静态（树/建筑）bottomY 入列时缓存、仅批量脏时排序。</param>
        public void Register(SpriteRenderer sr, bool isDynamic = false)
        {
            if (sr == null || this.entryMap.ContainsKey(sr))
            {
                return;
            }

            Entry e = new Entry { renderer = sr, transform = sr.transform, isDynamic = isDynamic, isPending = true };
            this.entryMap.Add(sr, e);
            this.pending.Add(e);

            // 注册不打日志——注册时序早于 sprite/位置赋值，快照全是初始态（单局曾 800+ 条误导性 Debug）。
        }

        /// <summary>
        /// 注销一个 SpriteRenderer。一般不强制调用（LateUpdate 懒清扫兜底）。
        /// </summary>
        public void Unregister(SpriteRenderer sr)
        {
            if (sr == null || !this.entryMap.TryGetValue(sr, out Entry e))
            {
                return;
            }

            this.entryMap.Remove(sr);
            this.pending.Remove(e);
            if (e.isDynamic)
            {
                this.dynamics.Remove(e);
            }
            else
            {
                this.statics.Remove(e); // 从有序表移除不破坏有序性，无需重排
            }
        }

        /// <summary>
        /// 外部原地改了 sprite/scale（如 RuleTile 邻居变化触发的 CreateOrUpdate 原地换图）时
        /// 强制重算该静态条目的底端偏移。动态条目每帧自动检测，无需调用。
        /// </summary>
        public void MarkStaticDirty(SpriteRenderer sr)
        {
            if (sr == null || !this.entryMap.TryGetValue(sr, out Entry e) || e.isDynamic)
            {
                return;
            }

            if (e.isPending)
            {
                return; // 尚未入列，入列时会读最新状态（标志位 O(1)，地图生成期 pending 上万不可 Contains 线性扫）
            }

            this.RefreshOffset(e);
            this.staticsDirty = true;
        }

        private void LateUpdate()
        {
            // 1. pending 入列：此刻 TileVisualSpawner 的 position/sprite 配置已落地，读 bounds/位置才有效
            for (int i = 0; i < this.pending.Count; i++)
            {
                Entry e = this.pending[i];
                if (e.renderer == null)
                {
                    this.entryMap.Remove(e.renderer);
                    continue;
                }

                e.isPending = false;
                this.RefreshOffset(e);
                if (e.isDynamic)
                {
                    this.dynamics.Add(e);
                }
                else
                {
                    this.statics.Add(e);
                    this.staticsDirty = true;
                }
            }

            this.pending.Clear();

            // 2. 懒清扫已销毁条目（角色延迟销毁/Player 永不销毁/漏注销均兜底；移除不破坏静态有序性）
            this.SweepDestroyed(this.statics);
            this.SweepDestroyed(this.dynamics);

            // 3. 静态重排：仅批量脏时（地图生成完成/补种/MarkStaticDirty），一次性排序
            if (this.staticsDirty)
            {
                this.statics.Sort(SortComparer);
                this.staticsDirty = false;
            }

            // 4. 动态：读当前位置（本管理器唯一的每帧引擎调用，~200 条 × 3 次），重算 bottomY
            for (int i = 0; i < this.dynamics.Count; i++)
            {
                Entry e = this.dynamics[i];
                if (e.renderer.sprite != e.lastSprite || e.transform.lossyScale != e.lastScale)
                {
                    this.RefreshOffset(e);
                }

                e.bottomY = e.transform.position.y + e.bottomOffset;
            }

            this.dynamics.Sort(SortComparer);

            // 5. 双指针归并按 bottomY 降序派发唯一 order（等 y 取静态先行：同基线角色盖住树基，
            //    与旧版"地图生成先注册静态、角色后出生"的稳定序一致），只写变化项
            int s = 0, d = 0, order = 0;
            while (s < this.statics.Count || d < this.dynamics.Count)
            {
                Entry e;
                if (d >= this.dynamics.Count
                    || (s < this.statics.Count && this.statics[s].bottomY >= this.dynamics[d].bottomY))
                {
                    e = this.statics[s++];
                }
                else
                {
                    e = this.dynamics[d++];
                }

                if (e.lastOrder != order)
                {
                    e.renderer.sortingOrder = order;
                    e.lastOrder = order;
                }

                order++;
            }
        }

        private void SweepDestroyed(List<Entry> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].renderer == null)
                {
                    this.entryMap.Remove(list[i].renderer);
                    list.RemoveAt(i);
                }
            }
        }

        private void RefreshOffset(Entry e)
        {
            Bounds bounds = e.renderer.bounds;
            e.bottomOffset = bounds.min.y - e.transform.position.y;
            e.lastSprite = e.renderer.sprite;
            e.lastScale = e.transform.lossyScale;
            e.bottomY = e.transform.position.y + e.bottomOffset;
        }
    }
}
