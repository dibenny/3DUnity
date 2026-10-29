using UnityEngine;

namespace InspectionDemo
{
    /// <summary>
    /// 全局配置：后端地址、颜色、射线参数都集中在这里，改配置不用翻别的脚本。
    /// </summary>
    public static class GameConfig
    {
        /// <summary>Go 后端默认地址。Unity Editor 和桌面端都能直连本机。</summary>
        public const string DefaultServerBaseUrl = "http://127.0.0.1:8080";

        /// <summary>射线最大检测距离。</summary>
        public const float RaycastMaxDistance = 500f;

        /// <summary>优先级取值，顺序和后端 model.Priority 常量一致。</summary>
        public static readonly string[] PriorityKeys = { "low", "medium", "high" };

        /// <summary>优先级显示名。</summary>
        public static readonly string[] PriorityNames = { "低", "中", "高" };

        // 场景配色：低饱和偏冷，跟现实主义静帧一个路子。
        public static readonly Color GroundColor = new Color(0.40f, 0.41f, 0.38f);
        public static readonly Color WallColor = new Color(0.53f, 0.54f, 0.52f);
        public static readonly Color CabinetColor = new Color(0.26f, 0.32f, 0.38f);
        public static readonly Color BoxColor = new Color(0.45f, 0.36f, 0.25f);
        public static readonly Color MarkerColor = new Color(0.83f, 0.30f, 0.24f);

        // UI 配色
        public static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.94f);
        public static readonly Color InputColor = new Color(0.05f, 0.06f, 0.07f, 1f);
        public static readonly Color TextColor = new Color(0.90f, 0.91f, 0.92f);
        public static readonly Color TextDimColor = new Color(0.60f, 0.63f, 0.66f);
        public static readonly Color AccentColor = new Color(0.24f, 0.53f, 0.85f);
        public static readonly Color ButtonColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        public static readonly Color ButtonActiveColor = new Color(0.24f, 0.53f, 0.85f, 1f);
        public static readonly Color DangerColor = new Color(0.86f, 0.36f, 0.32f);
        public static readonly Color OkColor = new Color(0.40f, 0.74f, 0.48f);
    }
}
