namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 物品所有权服务 — 纯 C# 服务，查询和转移物品所有权。
    /// 不依赖 UnityEngine，供 Character 层和 Gameplay 层共享使用。
    ///
    /// 所有权规则：
    /// - OwnerId = 0: 无主之物，属于 Player，任何人可取
    /// - OwnerId > 0: Worker instance ID，只有该 Worker 可取
    /// - 采集资源时 OwnerId = 采集者 Worker instance ID
    /// - 悬赏任务产出的资源 OwnerId = 悬赏发布者
    /// - 自然生成资源 OwnerId = 0
    /// </summary>
    public static class ItemOwnershipService
    {
        /// <summary>Player 的 OwnerId 常量。</summary>
        public const int PlayerOwnerId = 0;

        /// <summary>无主之物的 OwnerId（同 Player）。</summary>
        public const int UnownedId = 0;

        /// <summary>
        /// 根据 OwnerId 解析拥有者名称的回调。
        /// 由 Character 层注入（避免 Domain 层反向依赖）。
        /// 默认实现返回 "Worker#id" 格式。
        /// </summary>
        public static System.Func<int, string> OwnerNameProvider { get; set; }
            = (ownerId) => ownerId == UnownedId ? "无主(Player)" : $"Worker#{ownerId}";

        /// <summary>
        /// 检查指定角色是否可以拾取该资源。
        /// </summary>
        /// <param name="resource">资源信息</param>
        /// <param name="pickerOwnerId">拾取者的 OwnerId（Player=0, Worker=instanceId）</param>
        /// <returns>可以拾取返回 true</returns>
        public static bool CanPickUp(ResourceInfo resource, int pickerOwnerId)
        {
            if (resource == null)
            {
                return false;
            }

            // 无主之物，谁都可以捡
            if (resource.OwnerId == UnownedId)
            {
                return true;
            }

            // 自己的东西可以捡
            if (resource.OwnerId == pickerOwnerId)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 转移所有权 — 用于出售给市场或交易。
        /// </summary>
        /// <param name="resource">资源信息（原地修改）</param>
        /// <param name="newOwnerId">新拥有者 ID</param>
        public static void TransferOwnership(ResourceInfo resource, int newOwnerId)
        {
            if (resource != null)
            {
                resource.OwnerId = newOwnerId;
            }
        }

        /// <summary>
        /// 设置为无主（出售给市场后）。
        /// </summary>
        public static void SetUnowned(ResourceInfo resource)
        {
            TransferOwnership(resource, UnownedId);
        }

        /// <summary>
        /// 生成资源的所有权描述文本（使用 OwnerNameProvider 解析名称）。
        /// </summary>
        public static string GetOwnerLabel(ResourceInfo resource)
        {
            if (resource == null)
            {
                return "无";
            }

            return OwnerNameProvider(resource.OwnerId);
        }
    }
}
