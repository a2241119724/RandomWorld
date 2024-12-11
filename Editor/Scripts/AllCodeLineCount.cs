using System.IO;
using UnityEngine;
using UnityEditor;

namespace LAB2D
{
    public class AllCodeLineCount
    {
        [MenuItem("Tools/All Code Line Count")]
        private static void printTotalLine()
        {
            string[] fileName = Directory.GetFiles("Assets/Scripts/2D", "*.cs", SearchOption.AllDirectories);

            int totalLine = 0;
            foreach (var temp in fileName)
            {
                int nowLine = 0;
                StreamReader sr = new StreamReader(temp);
                while (sr.ReadLine() != null)
                {
                    nowLine++;
                }

                //文件名+文件行数
                //Debug.Log(String.Format("{0}——{1}", temp, nowLine));

                totalLine += nowLine;
            }
            Debug.Log(string.Format("总代码行数：{0}", totalLine));
        }
    }
}