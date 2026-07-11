namespace LAB2D.Editor
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 数据工具
    /// </summary>
    public class DataTool
    {
        private const string Prefix = "工具/数据/";

        [MenuItem(Prefix + "根据代码生成道具数据")]
        private static void BuildAB()
        {
            List<Type> types = Tool.GetChildByParent<AItem>();
            foreach (var type in types)
            {
                Debug.Log(type.Name);
            }
        }
    }
}
