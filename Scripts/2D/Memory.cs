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
        // ��ӡ��ʼ�йܶѴ�С
        long initialHeapSize = GC.GetTotalMemory(false);
        LogManager.Instance.log($"Initial Heap Size: {initialHeapSize / 1024 / 1024} M", LogManager.LogLevel.Info);

        // ִ��һЩ�����������ڴ棬���紴������
        // ...

        // ���������ռ��Ը����ڴ�ʹ�����
        // System.GC.Collect();

        // ��ӡ�����йܶѴ�С
        // long finalHeapSize = System.GC.GetTotalMemory(true);
        //LogManager.Instance.log($"Final Heap Size: {GC.GetTotalMemory(false) / 1024 / 1024} M", LogManager.LogLevel.Info);
    }
}