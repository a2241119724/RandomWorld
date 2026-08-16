namespace LAB2D.Render
{
    using System.Collections.Generic;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;
    using UnityEngine;

    /// <summary>
    /// 全局"按视觉底端世界 y"排序渲染顺序的管理器。
    /// 每帧 LateUpdate 将注册的 SpriteRenderer 按底端 y 降序分配唯一 sortingOrder：
    /// 底端 y 大（屏幕上方/远处）→ order 小（先绘制，被覆盖）；
    /// 底端 y 小（屏幕下方/近处）→ order 大（后绘制，盖住上方）。
    /// 约束：所有被排序的 renderer 必须位于同一 sorting layer（Character），
    /// 且该层内不得存在未注册的 renderer（否则其 order 固定为 0 会错乱）。
    /// 注册方：角色在 Character.Start 注册、建筑/树 sprite 由 TileVisualSpawner 注册；
    /// 注销不强制，LateUpdate 懒清扫已销毁的 renderer（覆盖延迟销毁/漏注销）。
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
            public float bottomOffset; // bounds.min.y - position.y（缓存，sprite/scale 变化时重算）
            public float bottomY;      // 当前底端世界 y
            public int lastOrder = int.MinValue;
            public Sprite lastSprite;
            public Vector3 lastScale;
        }

        private readonly List<Entry> entries = new List<Entry>();

        /// <summary>上一帧"最顶条目"名（诊断：顶部变化时打一条，暴露恒最顶的根因）。</summary>
        private string lastTopName;

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
        /// </summary>
        public void Register(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            for (int i = 0; i < this.entries.Count; i++)
            {
                if (this.entries[i].renderer == sr)
                {
                    return;
                }
            }

            Entry e = new Entry
            {
                renderer = sr,
                transform = sr.transform,
            };
            this.RefreshOffset(e);
            this.entries.Add(e);

            // 诊断（事件点：每个 renderer 注册一次）：暴露注册时 sprite/pivot/offset，排查恒最顶问题
            AWorkerTask.LogProvider(
                $"[SeekDiag] YSortRegister name={sr.name} layer={sr.sortingLayerName} " +
                $"sprite={sr.sprite?.name ?? "null"} pivot={sr.sprite?.pivot} size={sr.sprite?.bounds.size} " +
                $"pos=({sr.transform.position.x:F2},{sr.transform.position.y:F2}) " +
                $"offset={e.bottomOffset:F4} bottomY={e.bottomY:F4} entries={this.entries.Count}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 注销一个 SpriteRenderer。一般不强制调用（LateUpdate 懒清扫兜底）。
        /// </summary>
        public void Unregister(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            for (int i = this.entries.Count - 1; i >= 0; i--)
            {
                if (this.entries[i].renderer == sr)
                {
                    this.entries.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 外部改了 sprite/scale 时强制重算底端偏移（通常不需要，
        /// LateUpdate 会自动检测 sprite/scale 变化；用于 RuleTile 邻居变化等场景）。
        /// </summary>
        public void MarkStaticDirty(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            for (int i = 0; i < this.entries.Count; i++)
            {
                Entry e = this.entries[i];
                if (e.renderer == sr)
                {
                    this.RefreshOffset(e);
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            // 1. 懒清扫已销毁条目（角色延迟销毁/Player 永不销毁/漏注销均兜底）
            for (int i = this.entries.Count - 1; i >= 0; i--)
            {
                if (this.entries[i].renderer == null)
                {
                    this.entries.RemoveAt(i);
                }
            }

            int count = this.entries.Count;
            if (count == 0)
            {
                return;
            }

            // 2. 计算当前底端 y；sprite 引用或 lossyScale 变化时重算 offset（Player 换 sprite 等）
            float[] bottomYs = new float[count];
            for (int i = 0; i < count; i++)
            {
                Entry e = this.entries[i];
                if (e.renderer.sprite != e.lastSprite || e.transform.lossyScale != e.lastScale)
                {
                    this.RefreshOffset(e);
                }

                e.bottomY = e.transform.position.y + e.bottomOffset;
                bottomYs[i] = e.bottomY;
            }

            // 3. 按底端 y 降序分配唯一 order（纯函数，可单测）
            int[] orders = YSortAlgorithm.AssignOrders(bottomYs);

            // 4. 仅写变化的 order，减少 SetProperty 开销
            for (int i = 0; i < count; i++)
            {
                Entry e = this.entries[i];
                int order = orders[i];
                if (e.lastOrder != order)
                {
                    e.renderer.sortingOrder = order;
                    e.lastOrder = order;
                }
            }

            // 诊断（事件点：最顶条目变化时打一条）：直接暴露"谁在最顶 + 其 bottomY/offset"，排查恒最顶问题
            int topIdx = 0;
            for (int i = 1; i < count; i++)
            {
                if (bottomYs[i] < bottomYs[topIdx])
                {
                    topIdx = i;
                }
            }

            int secondIdx = -1;
            float secondMin = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (i == topIdx || bottomYs[i] >= secondMin)
                {
                    continue;
                }

                secondMin = bottomYs[i];
                secondIdx = i;
            }

            Entry top = this.entries[topIdx];
            if (top.renderer.name != this.lastTopName)
            {
                this.lastTopName = top.renderer.name;
                Entry second = secondIdx >= 0 ? this.entries[secondIdx] : null;
                AWorkerTask.LogProvider(
                    $"[SeekDiag] YSortTop top={top.renderer.name} " +
                    $"pos=({top.transform.position.x:F2},{top.transform.position.y:F2}) " +
                    $"sprite={top.renderer.sprite?.name ?? "null"} offset={top.bottomOffset:F4} bottomY={top.bottomY:F4} " +
                    $"next={second?.renderer?.name ?? "null"} nextBottomY={(second != null ? second.bottomY : 0f):F4} entries={count}",
                    LogManager.LogLevelEnum.Debug);
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
