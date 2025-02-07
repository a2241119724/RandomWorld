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
        Debug.Log($"Initial Heap Size: {initialHeapSize/1024 / 1024} M");

        // 执行一些操作来分配内存，例如创建对象
        // ...

        // 请求垃圾收集以更新内存使用情况
        // System.GC.Collect();

        // 打印最新托管堆大小
        // long finalHeapSize = System.GC.GetTotalMemory(true);
        // Debug.Log($"Final Heap Size: {finalHeapSize / 1024 / 1024} M");
    }
}