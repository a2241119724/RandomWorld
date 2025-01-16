using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerTaskManager : MonoBehaviour
    {
        public static WorkerTaskManager Instance { get; private set; }

        /// <summary>
        /// 所有任务
        /// </summary>
        private Dictionary<WorkerTask,bool> tasks;
        /// <summary>
        /// 饥饿任务与pos挂钩，TODO与worker数量挂钩
        /// </summary>
        private List<HungryTask> hungrys;

        public WorkerTaskManager() {
            tasks = new Dictionary<WorkerTask, bool>();
            hungrys = new List<HungryTask>();
        }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Worker获取任务，LAB_TODO优先级没有实现
        /// </summary>
        private void Update()
        {
            List<Worker> workers = WorkerManager.Instance.Characters;
            foreach (Worker worker in workers)
            {
                if (worker.Manager.Task != null) continue;
                WorkerTask closedTask = null;
                float minDistance = 999999.0f;
                foreach (WorkerTask task in tasks.Keys)
                {
                    // 饥饿值小于一定值只可以接收饥饿任务
                    if (worker.CurHungry < Worker.ThresholdHungry && task.TaskType != TaskType.Hungry) continue;
                    // 如果没饿，不接受饥饿任务
                    if (worker.CurHungry >= Worker.ThresholdHungry && task.TaskType == TaskType.Hungry) continue;
                    // 该任务是否正在被做
                    if (tasks[task]) continue;
                    // 是否满足做任务的基础条件
                    if (!task.isCanWork(worker)) continue;
                    if (closedTask == null)
                    {
                        minDistance = Mathf.Pow(worker.transform.position.y - task.TargetMap.x, 2) +
                            Mathf.Pow(worker.transform.position.x - task.TargetMap.y, 2);
                        closedTask = task;
                    }
                    else
                    {
                        float distance = Mathf.Pow(worker.transform.position.y - task.TargetMap.x, 2) +
                            Mathf.Pow(worker.transform.position.x - task.TargetMap.y, 2);
                        if(distance < minDistance)
                        {
                            minDistance = distance;
                            closedTask = task;
                        }
                    }        
                }
                // 获得任务
                if (closedTask != null)
                {
                    // 先设置任务
                    worker.Manager.Task = closedTask;
                    closedTask.start(worker);
                    // 同一个饥饿任务还可以继续接
                    if (closedTask.TaskType != TaskType.Hungry)
                    {
                        tasks[closedTask] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="task"></param>
        public void addTask(WorkerTask task) {
            if (task == null) return;
            // 如果是饥饿任务,一个位置仅对应一个任务
            if(task.TaskType == TaskType.Hungry)
            {
                foreach (HungryTask hungryTask in hungrys)
                {
                    if (hungryTask.TargetMap.x == task.TargetMap.x && hungryTask.TargetMap.y == task.TargetMap.y) {
                        return;
                    }
                }
                hungrys.Add((HungryTask)task);
            }
            tasks.Add(task, false);
            //DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        public void completeTask(WorkerTask task)
        {
            // 不能删除饥饿任务，需要在deleteHungryTask中删除
            if (task.TaskType != TaskType.Hungry)
            {
                tasks.Remove(task);
            }
            //DebugUI.Instance.updateTaskInfo(getTaskInfo());
        }

        public void giveUpTask(WorkerTask task) {
            if (task == null) return;
            tasks[task] = false;
        }

        public string getTaskInfo()
        {
            int count = 0;
            foreach (WorkerTask task in tasks.Keys)
            {
                if (tasks[task])
                {
                    count++;
                }
            }
            return $"任务总数量: {tasks.Count}\n" +
                $"正在做的任务数量: {count}";
        }

        public void deleteHungryTask(Vector3Int pos) {
            foreach (HungryTask hungryTask in hungrys)
            {
                if (hungryTask.TargetMap.x == pos.x && hungryTask.TargetMap.y == pos.y)
                {
                    tasks.Remove(hungryTask);
                    hungrys.Remove(hungryTask);
                    return;
                }
            }
        }
    }
}
