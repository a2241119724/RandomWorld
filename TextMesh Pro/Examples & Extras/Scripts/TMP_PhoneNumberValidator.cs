using System;
using UnityEngine;

namespace TMPro
{
    /// <summary>
    /// 自定义字符输入验证器示例，仅允许 (800) 555-1212 格式的电话号码。
    /// </summary>
    [Serializable]
    //[CreateAssetMenu(fileName = "InputValidator - Phone Numbers.asset", menuName = "TextMeshPro/Input Validators/Phone Numbers")]
    public class TMP_PhoneNumberValidator : TMP_InputValidator
    {
        // 自定义文本输入验证函数
        public override char Validate(ref string text, ref int pos, char ch)
        {
            Debug.Log("Trying to validate...");
            
            // 除非字符是有效数字，否则返回
            if (ch < '0' && ch > '9') return (char)0;

            int length = text.Length;

            // 为每个字符输入强制应用电话号码格式。
            for (int i = 0; i < length + 1; i++)
            {
                switch (i)
                {
                    case 0:
                        if (i == length)
                            text = "(" + ch;
                        pos = 2;
                        break;
                    case 1:
                        if (i == length)
                            text += ch;
                        pos = 2;
                        break;
                    case 2:
                        if (i == length)
                            text += ch;
                        pos = 3;
                        break;
                    case 3:
                        if (i == length)
                            text += ch + ") ";
                        pos = 6;
                        break;
                    case 4:
                        if (i == length)
                            text += ") " + ch;
                        pos = 7;
                        break;
                    case 5:
                        if (i == length)
                            text += " " + ch;
                        pos = 7;
                        break;
                    case 6:
                        if (i == length)
                            text += ch;
                        pos = 7;
                        break;
                    case 7:
                        if (i == length)
                            text += ch;
                        pos = 8;
                        break;
                    case 8:
                        if (i == length)
                            text += ch + "-";
                        pos = 10;
                        break;
                    case 9:
                        if (i == length)
                            text += "-" + ch;
                        pos = 11;
                        break;
                    case 10:
                        if (i == length)
                            text += ch;
                        pos = 11;
                        break;
                    case 11:
                        if (i == length)
                            text += ch;
                        pos = 12;
                        break;
                    case 12:
                        if (i == length)
                            text += ch;
                        pos = 13;
                        break;
                    case 13:
                        if (i == length)
                            text += ch;
                        pos = 14;
                        break;
                }
            }

            return ch;
        }
    }
}
