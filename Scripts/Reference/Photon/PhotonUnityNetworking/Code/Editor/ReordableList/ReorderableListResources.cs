// Copyright (c) 2012-2013 Rotorz Limited. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System;
using UnityEditor;
using UnityEngine;

namespace Photon.Pun
{

    /// <summary>
    /// 用于辅助可重排序列表控件的资源。
    /// </summary>
    internal static class ReorderableListResources
    {

        static ReorderableListResources()
        {
            GenerateSpecialTextures();
            LoadResourceAssets();
        }

        #region Texture Resources

        private enum ResourceName
        {
            add_button = 0,
            add_button_active,
            container_background,
            grab_handle,
            remove_button,
            remove_button_active,
            title_background,
        }

        /// <summary>
        /// 浅色皮肤的纹理资源。
        /// </summary>
        /// <remarks>
        /// <para>资源文件是PNG图像，已使用base-64字符串编码，
        /// 因此不需要实际的资源文件。</para>
        /// </remarks>
        private static string[] s_LightSkin = {
            "iVBORw0KGgoAAAANSUhEUgAAAB4AAAAQCAYAAAABOs/SAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAZdEVYdFNvZnR3YXJlAEFkb2JlIEltYWdlUmVhZHlxyWU8AAAAW0lEQVRIS+3NywnAQAhF0anI4mzVCmzBBl7QEBgGE5JFhBAXd+OHM5gZZgYRKcktNxu+HRFF2e6qhtOjtQM7K/tZ+xY89wSbazg9eqOfw6oag4rcChjY8coAjA2l1RxFDY8IFAAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAB4AAAAQCAYAAAABOs/SAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAGlJREFUeNpiFBER+f/jxw8GNjY2BnqAX79+MXBwcDAwMQwQGHoWnzp1CoxHjo8pBSykBi8+MTMzs2HmY2QfwXxKii9HExdZgNwgHuFB/efPH7pZCLOL8f///wyioqL/6enbL1++MAIEGAB4GSLA+9GPZwAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAECAYAAABGM/VAAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAZdEVYdFNvZnR3YXJlAEFkb2JlIEltYWdlUmVhZHlxyWU8AAAAMElEQVQYV2P4//8/Q1FR0X8YBvHBAp8+ffp/+fJlMA3igwUfPnwIFgDRYEFM7f8ZAG1EOYL9INrfAAAAAElFTkSuQmCC",
            "iVBORw0KGgoAAAANSUhEUgAAAAkAAAAFCAYAAACXU8ZrAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAZdEVYdFNvZnR3YXJlAEFkb2JlIEltYWdlUmVhZHlxyWU8AAAAIElEQVQYV2P49OnTf0KYobCw8D8hzPD/P2FMLesK/wMAs5yJpK+6aN4AAAAASUVORK5CYII=",
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAACCAIAAADq9gq6AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAABVJREFUeNpiVFZWZsAGmBhwAIAAAwAURgBt4C03ZwAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAACCAIAAADq9gq6AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAABVJREFUeNpivHPnDgM2wMSAAwAEGAB8VgKYlvqkBwAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAECAYAAABGM/VAAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAEFJREFUeNpi/P//P0NxcfF/BgRgZP78+fN/VVVVhpCQEAZjY2OGs2fPNrCApBwdHRkePHgAVwoWnDVrFgMyAAgwAAt4E1dCq1obAAAAAElFTkSuQmCC"
        };
        /// <summary>
        /// 深色皮肤的纹理资源。
        /// </summary>
        /// <remarks>
        /// <para>资源文件是PNG图像，已使用base-64字符串编码，
        /// 因此不需要实际的资源文件。</para>
        /// </remarks>
        private static string[] s_DarkSkin = {
            "iVBORw0KGgoAAAANSUhEUgAAAB4AAAAQCAYAAAABOs/SAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAIBJREFUeNpiVFZW/u/i4sLw4sULBnoACQkJhj179jAwMQwQGHoWl5aWgvHI8TGlgIXU4MUn1t3dPcx8HB8fD2cvXLgQQ0xHR4c2FmMzmBTLhl5QYwt2cn1MtsXkWjg4gvrt27fgWoMeAGQXCDD+//+fQUVF5T89fXvnzh1GgAADAFmSI1Ed3FqgAAAAAElFTkSuQmCC",
            "iVBORw0KGgoAAAANSUhEUgAAAB4AAAAQCAYAAAABOs/SAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAHlJREFUeNpiFBER+f/jxw8GNjY2BnqAX79+MXBwcDAwMQwQGHoWv3nzBoxHjo8pBSykBi8+MWAOGWY+5uLigrO/ffuGIYbMppnF5Fg2tFM1yKfk+pbkoKZGEA+OVP3nzx+6WQi/B/H///8MoqKi/+np2y9fvjACBBgAoTYjgvihfz0AAAAASUVORK5CYII=",
            "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAECAYAAABGM/VAAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAD1JREFUeNpi/P//P4OKisp/Bii4c+cOIwtIwMXFheHFixcMEhISYAVMINm3b9+CBUA0CDCiazc0NGQECDAAdH0YelA27kgAAAAASUVORK5CYII=",
            "iVBORw0KGgoAAAANSUhEUgAAAAkAAAAFCAYAAACXU8ZrAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAACRJREFUeNpizM3N/c9AADAqKysTVMTi5eXFSFAREFPHOoAAAwBCfwcAO8g48QAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAECAYAAACzzX7wAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAACJJREFUeNpi/P//PwM+wHL06FG8KpgYCABGZWVlvCYABBgA7/sHvGw+cz8AAAAASUVORK5CYII=",
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAECAYAAACzzX7wAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAACBJREFUeNpi/P//PwM+wPKfgAomBgKAhYuLC68CgAADAAxjByOjCHIRAAAAAElFTkSuQmCC",
            "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAECAYAAABGM/VAAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAADtJREFUeNpi/P//P4OKisp/Bii4c+cOIwtIQE9Pj+HLly9gQRCfBcQACbx69QqmmAEseO/ePQZkABBgAD04FXsmmijSAAAAAElFTkSuQmCC"
        };

        /// <summary>
        /// 获取浅色或深色纹理 "add_button.png"。
        /// </summary>
        public static Texture2D texAddButton
        {
            get { return s_Cached[(int)ResourceName.add_button]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "add_button_active.png"。
        /// </summary>
        public static Texture2D texAddButtonActive
        {
            get { return s_Cached[(int)ResourceName.add_button_active]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "container_background.png"。
        /// </summary>
        public static Texture2D texContainerBackground
        {
            get { return s_Cached[(int)ResourceName.container_background]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "grab_handle.png"。
        /// </summary>
        public static Texture2D texGrabHandle
        {
            get { return s_Cached[(int)ResourceName.grab_handle]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "remove_button.png"。
        /// </summary>
        public static Texture2D texRemoveButton
        {
            get { return s_Cached[(int)ResourceName.remove_button]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "remove_button_active.png"。
        /// </summary>
        public static Texture2D texRemoveButtonActive
        {
            get { return s_Cached[(int)ResourceName.remove_button_active]; }
        }
        /// <summary>
        /// 获取浅色或深色纹理 "title_background.png"。
        /// </summary>
        public static Texture2D texTitleBackground
        {
            get { return s_Cached[(int)ResourceName.title_background]; }
        }

        #endregion

        #region Generated Resources

        public static Texture2D texItemSplitter { get; private set; }

        /// <summary>
        /// 生成特殊纹理。
        /// </summary>
        private static void GenerateSpecialTextures()
        {
            var splitterColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.14f)
                : new Color(0.59f, 0.59f, 0.59f, 0.55f)
                ;
            texItemSplitter = CreatePixelTexture("(Generated) Item Splitter", splitterColor);
        }

        /// <summary>
        /// 创建指定颜色的1x1像素纹理。
        /// </summary>
        /// <param name="name">纹理对象的名称。</param>
        /// <param name="color">像素颜色。</param>
        /// <returns>
        /// 新的 <c>Texture2D</c> 实例。
        /// </returns>
        public static Texture2D CreatePixelTexture(string name, Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            tex.name = name;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        #endregion

        #region Load PNG from Base-64 Encoded String

        private static Texture2D[] s_Cached;

        /// <summary>
        /// 从base-64编码的字符串中读取纹理。根据当前使用的是浅色还是深色(pro)皮肤自动选择资源。
        /// </summary>
        private static void LoadResourceAssets()
        {
            var skin = EditorGUIUtility.isProSkin ? s_DarkSkin : s_LightSkin;
            s_Cached = new Texture2D[skin.Length];

            for (int i = 0; i < s_Cached.Length; ++i)
            {
                // 从base64编码字符串获取图像数据(PNG)。
                byte[] imageData = Convert.FromBase64String(skin[i]);

                // 从图像数据中获取图像尺寸。
                int texWidth, texHeight;
                GetImageSize(imageData, out texWidth, out texHeight);

                // 生成纹理资源。
                var tex = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false, true);
                tex.hideFlags = HideFlags.HideAndDontSave;
                tex.name = "(Generated) ReorderableList:" + i;
                tex.filterMode = FilterMode.Point;
                tex.LoadImage(imageData);

                s_Cached[i] = tex;
            }

            s_LightSkin = null;
            s_DarkSkin = null;
        }

        /// <summary>
        /// 读取PNG文件的宽度和高度（以像素为单位）。
        /// </summary>
        /// <param name="imageData">PNG图像数据。</param>
        /// <param name="width">图像宽度（像素）。</param>
        /// <param name="height">图像高度（像素）。</param>
        private static void GetImageSize(byte[] imageData, out int width, out int height)
        {
            width = ReadInt(imageData, 3 + 15);
            height = ReadInt(imageData, 3 + 15 + 2 + 2);
        }

        private static int ReadInt(byte[] imageData, int offset)
        {
            return (imageData[offset] << 8) | imageData[offset + 1];
        }

        #endregion

        #region GUI Helper
        private static GUIStyle s_TempStyle = new GUIStyle();

        /// <summary>
        /// 使用 <see cref="GUIStyle"/> 绘制纹理，以解决Unity中
        /// <see cref="GUI.DrawTexture"/> 嵌入属性绘制器时会闪烁的问题。
        /// </summary>
        /// <param name="position">在GUI空间中绘制纹理的位置。</param>
        /// <param name="texture">纹理。</param>
        public static void DrawTexture(Rect position, Texture2D texture)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            s_TempStyle.normal.background = texture;

            s_TempStyle.Draw(position, GUIContent.none, false, false, false, false);
        }
        #endregion

    }

}