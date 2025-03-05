using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;

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
                bool isInsideMultiLineComment = false;

                using (StreamReader sr = new StreamReader(temp))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // ��������ע���ڲ���������
                        if (isInsideMultiLineComment)
                        {
                            if (line.Contains("*/"))
                            {
                                isInsideMultiLineComment = false;
                                // �����һ����ע�ͽ�����Ĳ����Ƿ�����Ч����
                                string restOfLine = line.Substring(line.IndexOf("*/") + 2).Trim();
                                if (!string.IsNullOrEmpty(restOfLine))
                                {
                                    nowLine++;
                                }
                            }
                        }
                        else
                        {
                            // ȥ�����׺���β�Ŀհ��ַ�
                            string trimmedLine = line.Trim();
                            // ��鵥��ע�ͺͶ���ע�͵Ŀ�ʼ
                            if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("//") && !trimmedLine.StartsWith("/*"))
                            {
                                // �������*/����������ǰ��Ĳ����Ƿ������Ч����
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
                Debug.Log(String.Format("{0}����{1}", temp, nowLine));
                totalLine += nowLine;
            }
            Debug.Log(string.Format("����Ч����������{0}", totalLine));
        }
    }
}