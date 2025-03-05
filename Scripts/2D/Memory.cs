using UnityEngine;
using System;

public class Memory : MonoBehaviour
{
    //private float gameTime = 0;

    void Update()
    {
        //gameTime += Time.deltaTime;
        //if (gameTime > 1)
        //{
        //    gameTime = 0;
        //    printMemory();
        //}
    }

    void printMemory()
    {
        // 打印初始托管堆大小
        long initialHeapSize = GC.GetTotalMemory(false);
        LogManager.Instance.log($"Initial Heap Size: {initialHeapSize / 1024 / 1024} M", LogManager.LogLevel.Info);

        // 执行一些操作来分配内存，例如创建对象
        // ...

        // 请求垃圾收集以更新内存使用情况
        // System.GC.Collect();

        // 打印最新托管堆大小
        // long finalHeapSize = System.GC.GetTotalMemory(true);
        //LogManager.Instance.log($"Final Heap Size: {GC.GetTotalMemory(false) / 1024 / 1024} M", LogManager.LogLevel.Info);
    }
}