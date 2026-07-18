using System.Collections;
using UnityEngine;


namespace TMPro.Examples
{
    public class TextConsoleSimulator : MonoBehaviour
    {
        private TMP_Text m_TextComponent;
        private bool hasTextChanged;

        void Awake()
        {
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        }


        void Start()
        {
            StartCoroutine(RevealCharacters(m_TextComponent));
            //StartCoroutine(RevealWords(m_TextComponent));
        }


        void OnEnable()
        {
            // 订阅文本对象重新生成时触发的事件。
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
        }


        // 文本对象更改时收到的事件。
        void ON_TEXT_CHANGED(Object obj)
        {
            hasTextChanged = true;
        }


        /// <summary>
        /// 一次显示一个字符的文本展示方法。
        /// </summary>
        /// <returns></returns>
        IEnumerator RevealCharacters(TMP_Text textComponent)
        {
            textComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = textComponent.textInfo;

            int totalVisibleCharacters = textInfo.characterCount; // 获取文本对象中的可见字符数
            int visibleCount = 0;

            while (true)
            {
                if (hasTextChanged)
                {
                    totalVisibleCharacters = textInfo.characterCount; // 更新可见字符计数。
                    hasTextChanged = false; 
                }

                if (visibleCount > totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(1.0f);
                    visibleCount = 0;
                }

                textComponent.maxVisibleCharacters = visibleCount; // TextMeshPro 应显示多少字符？

                visibleCount += 1;

                yield return null;
            }
        }


        /// <summary>
        /// 一次显示一个单词的文本展示方法。
        /// </summary>
        /// <returns></returns>
        IEnumerator RevealWords(TMP_Text textComponent)
        {
            textComponent.ForceMeshUpdate();

            int totalWordCount = textComponent.textInfo.wordCount;
            int totalVisibleCharacters = textComponent.textInfo.characterCount; // 获取文本对象中的可见字符数
            int counter = 0;
            int currentWord = 0;
            int visibleCount = 0;

            while (true)
            {
                currentWord = counter % (totalWordCount + 1);

                // 获取当前单词的最后一个字符索引。
                if (currentWord == 0) // 不显示单词。
                    visibleCount = 0;
                else if (currentWord < totalWordCount) // 显示除最后一个单词之外的所有其他单词。
                    visibleCount = textComponent.textInfo.wordInfo[currentWord - 1].lastCharacterIndex + 1;
                else if (currentWord == totalWordCount) // 显示最后一个单词和所有剩余字符。
                    visibleCount = totalVisibleCharacters;

                textComponent.maxVisibleCharacters = visibleCount; // TextMeshPro 应显示多少字符？

                // 一旦最后一个字符显示完毕，等待 1.0 秒后重新开始。
                if (visibleCount >= totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(1.0f);
                }

                counter += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}