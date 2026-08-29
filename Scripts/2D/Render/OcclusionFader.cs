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

        private class Entry
        {
            public SpriteRenderer renderer;
            public float originalAlpha;
        }

        private readonly List<Entry> occluders = new List<Entry>();
        private readonly HashSet<SpriteRenderer> occludedNow = new HashSet<SpriteRenderer>();
        private readonly HashSet<SpriteRenderer> lastOccluded = new HashSet<SpriteRenderer>();

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
        /// </summary>
        public void AddOccluder(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            for (int i = 0; i < this.occluders.Count; i++)
            {
                if (this.occluders[i].renderer == sr)
                {
                    return;
                }
            }

            this.occluders.Add(new Entry { renderer = sr, originalAlpha = sr.color.a });
        }

        /// <summary>
        /// 移除一个候选遮挡物（环境视觉销毁时，由 TileVisualSpawner 调用）。
        /// </summary>
        public void RemoveOccluder(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            for (int i = this.occluders.Count - 1; i >= 0; i--)
            {
                if (this.occluders[i].renderer == sr)
                {
                    this.occluders.RemoveAt(i);
                    return;
                }
            }
        }

        private void LateUpdate()
        {
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

            this.occludedNow.Clear();

            for (int i = this.occluders.Count - 1; i >= 0; i--)
            {
                Entry e = this.occluders[i];
                SpriteRenderer sr = e.renderer;
                if (sr == null)
                {
                    // 视觉已销毁（砍树等）：从候选移除，懒清扫兜底
                    this.occluders.RemoveAt(i);
                    continue;
                }

                // 距离预过滤：树底远离玩家时树冠不可能盖住玩家，跳过遮挡判定（省 bounds 计算）。
                // 恢复不设距离门：树在半透明状态下因传送/重生等位置突变出半径时，
                // 若一并跳过将永久停留半透明，故距离外 target 恒为 originalAlpha 继续收敛。
                Vector3 diff = sr.transform.position - playerPos;
                bool occludes = false;
                if (diff.sqrMagnitude <= checkRadiusSq)
                {
                    // 玩家被遮挡 = 遮挡物绘制在玩家之上（order 大）且视觉相交
                    occludes = sr.sortingOrder > playerOrder && sr.bounds.Intersects(playerBounds);
                    if (occludes)
                    {
                        this.occludedNow.Add(sr);
                    }
                }

                // alpha 平滑渐变（保留 RGB，不覆盖 tilemap 颜色）
                float target = occludes ? OccludedAlpha : e.originalAlpha;
                float alpha = Mathf.MoveTowards(sr.color.a, target, FadeSpeed * dt);
                if (!Mathf.Approximately(sr.color.a, alpha))
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, alpha);
                }
            }

            // 诊断（事件点：遮挡集合变化时打一条）：暴露淡化的遮挡物，验证功能生效
            if (!this.occludedNow.SetEquals(this.lastOccluded))
            {
                AWorkerTask.LogProvider(
                    $"[OccluDiag] occluded={this.occludedNow.Count} " +
                    $"top={(this.occludedNow.Count > 0 ? this.OccluderName(this.occludedNow) : "-")} " +
                    $"player=({playerPos.x:F1},{playerPos.y:F1}) order={playerOrder}",
                    LogManager.LogLevelEnum.Debug);
                this.lastOccluded.Clear();
                foreach (SpriteRenderer sr in this.occludedNow)
                {
                    this.lastOccluded.Add(sr);
                }
            }
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

        private string OccluderName(HashSet<SpriteRenderer> set)
        {
            foreach (SpriteRenderer sr in set)
            {
                return sr.name;
            }

            return string.Empty;
        }
    }
}
