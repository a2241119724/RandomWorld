namespace LAB2D.Domain.Worker
{
    using System.Collections.Generic;

    /// <summary>
    /// Worker 任务队列 — 纯 C# 多优先级任务存储。
    /// 不依赖 UnityEngine，不依赖具体的 WorkerTask 类型。
    /// 负责存储、标记运行状态、按优先级查询，不包含任务分配逻辑。
    /// </summary>
    public sealed class WorkerTaskQueue<TTask>
    {
        private readonly List<Dictionary<TTask, bool>> priorityLevels;

        public int PriorityCount
        {
            get { return this.priorityLevels.Count; }
        }

        public WorkerTaskQueue(int priorityCount = 4)
        {
            this.priorityLevels = new List<Dictionary<TTask, bool>>(priorityCount);
            for (int i = 0; i < priorityCount; i++)
            {
                this.priorityLevels.Add(new Dictionary<TTask, bool>());
            }
        }

        public void Add(TTask task, int priority)
        {
            if (priority < 0 || priority >= this.priorityLevels.Count)
            {
                return;
            }

            this.priorityLevels[priority].Add(task, false);
        }

        public bool Remove(TTask task)
        {
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                if (this.priorityLevels[i].Remove(task))
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkRunning(TTask task)
        {
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                if (this.priorityLevels[i].ContainsKey(task))
                {
                    this.priorityLevels[i][task] = true;
                    return;
                }
            }
        }

        public void MarkIdle(TTask task)
        {
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                if (this.priorityLevels[i].ContainsKey(task))
                {
                    this.priorityLevels[i][task] = false;
                    return;
                }
            }
        }

        public bool Contains(TTask task)
        {
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                if (this.priorityLevels[i].ContainsKey(task))
                {
                    return true;
                }
            }

            return false;
        }

        public int TotalCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < this.priorityLevels.Count; i++)
                {
                    count += this.priorityLevels[i].Count;
                }

                return count;
            }
        }

        public int GetCount(int priority)
        {
            if (priority < 0 || priority >= this.priorityLevels.Count)
            {
                return 0;
            }

            return this.priorityLevels[priority].Count;
        }

        public IReadOnlyDictionary<TTask, bool> GetTasksAtPriority(int priority)
        {
            if (priority < 0 || priority >= this.priorityLevels.Count)
            {
                return new Dictionary<TTask, bool>();
            }

            return this.priorityLevels[priority];
        }

        public int GetRunningCountByType(System.Func<TTask, int> typeSelector)
        {
            var counts = new System.Collections.Generic.Dictionary<int, int>();
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                foreach (KeyValuePair<TTask, bool> pair in this.priorityLevels[i])
                {
                    if (pair.Value)
                    {
                        int typeIndex = typeSelector(pair.Key);
                        counts.TryGetValue(typeIndex, out int current);
                        counts[typeIndex] = current + 1;
                    }
                }
            }

            int total = 0;
            foreach (int count in counts.Values)
            {
                total += count;
            }

            return total;
        }

        public int GetRunningCountByType(int typeIndex, System.Func<TTask, int> typeSelector)
        {
            int count = 0;
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                foreach (KeyValuePair<TTask, bool> pair in this.priorityLevels[i])
                {
                    if (pair.Value && typeSelector(pair.Key) == typeIndex)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public bool RemoveWhere(System.Func<TTask, bool> predicate)
        {
            bool removed = false;
            for (int i = 0; i < this.priorityLevels.Count; i++)
            {
                var toRemove = new List<TTask>();
                foreach (TTask task in this.priorityLevels[i].Keys)
                {
                    if (predicate(task))
                    {
                        toRemove.Add(task);
                    }
                }

                foreach (TTask task in toRemove)
                {
                    this.priorityLevels[i].Remove(task);
                    removed = true;
                }
            }

            return removed;
        }
    }
}
