using System;
using UnityEngine;

namespace InspectionDemo
{
    /// <summary>
    /// 统一提供 UI 字体。
    ///
    /// 为什么要自己找字体：Unity 内置的 LegacyRuntime.ttf 只有拉丁字形，
    /// 直接拿它渲染中文会变成一排方块。这里从操作系统字体里挑一个带 CJK 的
    /// （macOS: PingFang SC，Windows: 微软雅黑），找不到再退回内置字体。
    /// </summary>
    public static class FontProvider
    {
        private static Font cached;
        private static bool resolved;

        private static readonly string[] Candidates =
        {
            "PingFang SC",       // macOS
            "Hiragino Sans GB",  // macOS 备选
            "Heiti SC",         // macOS 旧版
            "STHeiti",
            "Microsoft YaHei",   // Windows
            "SimHei",
            "SimSun",
            "Noto Sans CJK SC",
            "Source Han Sans SC",
            "Arial Unicode MS",
            "WenQuanYi Micro Hei",
        };

        /// <summary>取 UI 字体（只解析一次，之后走缓存）。</summary>
        public static Font Get()
        {
            if (resolved) return cached;
            resolved = true;

            try
            {
                // 传入候选列表，系统会自动挑第一个存在的
                cached = Font.CreateDynamicFontFromOSFont(Candidates, 32);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FontProvider] 创建系统字体失败: " + e.Message);
            }

            if (cached == null)
            {
                cached = TryBuiltin("LegacyRuntime.ttf") ?? TryBuiltin("Arial.ttf");
            }

            if (cached == null)
            {
                Debug.LogError("[FontProvider] 没找到任何可用字体，界面文字可能显示不出来。");
            }

            return cached;
        }

        private static Font TryBuiltin(string fileName)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(fileName);
            }
            catch (Exception)
            {
                // 2022.2 之后内置字体改名了，旧名字会抛异常，属于正常情况
                return null;
            }
        }
    }
}
