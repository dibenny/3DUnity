using UnityEngine;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>
    /// 运行时搭 UI 的小工具。
    ///
    /// 整个界面都是代码生成的，不依赖任何手工拖拽的 Prefab，
    /// 好处是打开 Main.unity 直接按 Play 就能看到完整界面。
    /// </summary>
    public static class UIFactory
    {
        /// <summary>创建一块带 Image 的面板（默认会挡住鼠标点击，用作 UI 拦截）。</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        /// <summary>创建文本。raycastTarget 关掉，避免文字挡住父级 UI 的点击。</summary>
        public static Text CreateText(string name, Transform parent, string content,
            int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = FontProvider.Get();
            text.fontSize = fontSize;
            text.text = content;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>创建按钮（自带背景色和居中文字）。</summary>
        public static Button CreateButton(string name, Transform parent, string label,
            int fontSize, Color background, Color foreground)
        {
            var image = CreatePanel(name, parent, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;

            var text = CreateText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, foreground);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>创建输入框（带 placeholder）。</summary>
        public static InputField CreateInput(string name, Transform parent, string placeholder,
            int fontSize, bool multiline)
        {
            var image = CreatePanel(name, parent, GameConfig.InputColor);
            var input = image.gameObject.AddComponent<InputField>();

            var text = CreateText("Text", image.transform, "", fontSize,
                multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, GameConfig.TextColor);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, multiline ? 8f : 0f);
            textRt.offsetMax = new Vector2(-10f, multiline ? -8f : 0f);
            input.textComponent = text;

            var ph = CreateText("Placeholder", image.transform, placeholder, fontSize,
                multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, GameConfig.TextDimColor);
            var phRt = ph.rectTransform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10f, multiline ? 8f : 0f);
            phRt.offsetMax = new Vector2(-10f, multiline ? -8f : 0f);
            input.placeholder = ph;

            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.characterLimit = 0;
            input.caretColor = GameConfig.AccentColor;
            input.customCaretColor = true;
            return input;
        }

        /// <summary>铺满父对象。</summary>
        public static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>按锚点 + 尺寸定位（anchor 是父对象的归一化坐标，pivot 是自身轴心）。</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }
    }
}
