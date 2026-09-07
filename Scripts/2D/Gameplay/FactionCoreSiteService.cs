namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.AncientCave;
    using LAB2D.Domain.Gameplay.DangerZone;
    using LAB2D.Domain.Gameplay.Faction;
    using LAB2D.Map;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 阵营核心选址服务 — 成员房间质心 → 间距校验 + Chebyshev 环搜空地。
    /// 环搜照 MountainGateManager.TryFindCoreArea 模式（IsAvailableMap.IsTileFreeForDrop 多格版），
    /// 间距约束走 FactionPlacementRuleService（平方距离比较，零开方）。
    /// 纯静态（ServiceLocator + 参数，无实例状态）；选址是低频事件点，允许一次性分配。
    /// </summary>
    public static class FactionCoreSiteService
    {
        /// <summary>质心周边环搜最大半径（格）。</summary>
        public const int SiteSearchRadius = 20;

        /// <summary>
        /// 为阵营核心选址：质心自身合法且 3×3 空 → 直接用；否则以质心为原点
        /// Chebyshev 环搜 r=1..20 取第一块「间距合法 + 整片空地」的位置。
        /// </summary>
        /// <param name="memberHomeAnchors">成员房间锚点（已建家/规划位；玩家阵营传玩家位置）。</param>
        /// <param name="otherCorePositions">其他核心（其他阵营核心 + 山门核心）。</param>
        /// <param name="mapCenter">地图中心格。</param>
        /// <returns>核心主格（BottomLeft 参考点）位置；找不到返回 null（调用方每日重试）。</returns>
        public static Vector3Int? FindCorePosition(
            List<Vector3Int> memberHomeAnchors,
            List<Vector3Int> otherCorePositions,
            Vector3Int mapCenter)
        {
            if (!Core.ServiceLocator.TryGet(out IsAvailableMap isAvailableMap))
            {
                return null;
            }

            // Domain 输入组装（一次性，低频事件点）
            var anchors = new List<GameVector2>(memberHomeAnchors.Count);
            foreach (Vector3Int anchor in memberHomeAnchors)
            {
                anchors.Add(new GameVector2(anchor.x, anchor.y));
            }

            var otherCores = new List<GameVector2>(otherCorePositions.Count);
            foreach (Vector3Int core in otherCorePositions)
            {
                otherCores.Add(new GameVector2(core.x, core.y));
            }

            List<GameVector2> pois = CollectPoiPositions();
            var center = new GameVector2(mapCenter.x, mapCenter.y);

            GameVector2 centroid = FactionPlacementRuleService.ComputeCentroid(anchors, center);
            var centroidInt = new Vector3Int((int)centroid.X, (int)centroid.Y, 0);

            for (int layer = 0; layer <= SiteSearchRadius; layer++)
            {
                for (int dx = -layer; dx <= layer; dx++)
                {
                    for (int dy = -layer; dy <= layer; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != layer)
                        {
                            continue; // 只检查外围环
                        }

                        var candidate = new Vector3Int(centroidInt.x + dx, centroidInt.y + dy, 0);
                        var candidateGv = new GameVector2(candidate.x, candidate.y);
                        if (!FactionPlacementRuleService.IsPlacementValid(candidateGv, otherCores, pois, center))
                        {
                            continue;
                        }

                        if (!IsAreaFree(isAvailableMap, candidate, FactionManager.CoreSize))
                        {
                            continue;
                        }

                        return candidate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 收集地图兴趣点中心（洞府 + 毒雾圈），服务未注册时跳过对应类。
        /// </summary>
        internal static List<GameVector2> CollectPoiPositions()
        {
            var pois = new List<GameVector2>();
            if (Core.ServiceLocator.TryGet(out AncientCaveManager caves) && caves.Caves != null)
            {
                foreach (AncientCaveRuleService.AncientCaveModel cave in caves.Caves)
                {
                    pois.Add(cave.Pos);
                }
            }

            if (Core.ServiceLocator.TryGet(out DangerZoneManager dangerZones) && dangerZones.Zones != null)
            {
                foreach (DangerZoneRuleService.DangerZoneModel zone in dangerZones.Zones)
                {
                    pois.Add(zone.Center);
                }
            }

            return pois;
        }

        /// <summary>
        /// 检查以 pos 为左下角、边长 size 的方形区域是否全部为空地（照 MountainGateManager.IsAreaFree）。
        /// </summary>
        private static bool IsAreaFree(IsAvailableMap isAvailableMap, Vector3Int pos, int size)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!isAvailableMap.IsTileFreeForDrop(new Vector3Int(pos.x + i, pos.y + j, 0)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
