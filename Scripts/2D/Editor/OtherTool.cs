namespace LAB2D.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 其他工具
    /// </summary>
    public class OtherTool
    {
        private const string Prefix = "工具/其他/";

        /// <summary>
        /// 打AB包
        /// 在预制体的Inspector最下面New AssetBundle,再点打包
        /// 打包到StreamingAssets文件夹下
        /// 使用方法
        /// AssetBundle assetBundleObj = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/snow");
        /// GameObject g = Instantiate(assetBundleObj.LoadAsset<GameObject>("snow"));
        /// g.name = "AB资源加载方式";
        /// </summary>
        [MenuItem(Prefix + "打AB包")]
        private static void BuildAB()
        {
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            AssetDatabase.Refresh();
            Debug.Log("打包完成");
        }

        /// <summary>
        /// 打印所有代码行数
        /// </summary>
        [MenuItem(Prefix + "计算代码行数")]
        private static void PrintTotalLine()
        {
            string[] fileName = Directory.GetFiles("Assets/Scripts/2D", "*.cs", SearchOption.AllDirectories);

            int totalLine = 0;
            foreach (var temp in fileName)
            {
                int nowLine = 0;
                bool isInsideMultiLineComment = false;

                using (StreamReader sr = new StreamReader(temp))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // 跳过多行注释内部的所有行
                        if (isInsideMultiLineComment)
                        {
                            if (line.Contains("*/"))
                            {
                                isInsideMultiLineComment = false;

                                // 检查这一行在注释结束后的部分是否有有效代码
                                string restOfLine = line.Substring(line.IndexOf("*/") + 2).Trim();
                                if (!string.IsNullOrEmpty(restOfLine))
                                {
                                    nowLine++;
                                }
                            }
                        }
                        else
                        {
                            // 去除行首和行尾的空白字符
                            string trimmedLine = line.Trim();

                            // 检查单行注释和多行注释的开始
                            if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("//") && !trimmedLine.StartsWith("/*"))
                            {
                                // 如果行以*/结束，则检查前面的部分是否包含有效代码
                                if (trimmedLine.EndsWith("*/"))
                                {
                                    string beforeEndComment = trimmedLine.Substring(0, trimmedLine.Length - 2).Trim();
                                    if (!string.IsNullOrEmpty(beforeEndComment))
                                    {
                                        nowLine++;
                                    }
                                }
                                else
                                {
                                    nowLine++;
                                }
                            }
                            else if (trimmedLine.StartsWith("/*"))
                            {
                                isInsideMultiLineComment = true;
                            }
                        }
                    }
                }

                Debug.Log(string.Format("{0}——{1}", temp, nowLine));
                totalLine += nowLine;
            }

            Debug.Log(string.Format("总有效代码行数：{0}", totalLine));
        }
    }
}
