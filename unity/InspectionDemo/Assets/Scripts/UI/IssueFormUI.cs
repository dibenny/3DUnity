using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>
    /// 上报问题的表单弹窗。
    ///
    /// 整个界面用代码搭出来，不依赖手工拖拽的 Prefab。
    /// 对外只暴露两个事件：OnSubmitRequested（标题/描述/优先级）和 OnCancelRequested，
    /// 具体怎么提交由 InspectionManager 负责，这里只管界面和校验。
    /// </summary>
    public class IssueFormUI : MonoBehaviour
    {
        private const float FormWidth = 520f;
        private const float FormHeight = 420f;

        /// <summary>提交事件：title, description, priority。</summary>
        public event Action<string, string, string> OnSubmitRequested;

        /// <summary>取消事件。</summary>
        public event Action OnCancelRequested;

        private GameObject root;
        private Text positionText;
        private InputField titleInput;
        private InputField descriptionInput;
        private Text hintText;
        private Button submitButton;
        private Button cancelButton;
        private Button[] priorityButtons;
        private int selectedPriorityIndex = 1;

        /// <summary>表单当前是否显示。</summary>
        public bool IsVisible
        {
            get { return root != null && root.activeSelf; }
        }

        /// <summary>搭建界面。只需要调用一次。</summary>
        public void Build(Transform canvasTransform)
        {
            root = new GameObject("IssueForm", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            var rootRt = (RectTransform)root.transform;
            UIFactory.Place(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(FormWidth, FormHeight));

            var panel = UIFactory.CreatePanel("Panel", root.transform, GameConfig.PanelColor);
            UIFactory.Stretch(panel.rectTransform);

            var title = UIFactory.CreateText("Title", root.transform, "上报巡检问题", 19,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(472f, 26f));

            positionText = UIFactory.CreateText("Position", root.transform, "", 13,
                TextAnchor.MiddleLeft, GameConfig.TextDimColor);
            UIFactory.Place(positionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -46f), new Vector2(472f, 20f));

            var titleLabel = UIFactory.CreateText("TitleLabel", root.transform, "标题 *", 14,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(titleLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -76f), new Vector2(472f, 20f));

            titleInput = UIFactory.CreateInput("TitleInput", root.transform, "例如：3 号机柜温度异常",
                15, false);
            UIFactory.Place(titleInput.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -98f), new Vector2(472f, 34f));

            var priorityLabel = UIFactory.CreateText("PriorityLabel", root.transform, "优先级", 14,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(priorityLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -142f), new Vector2(472f, 20f));

            priorityButtons = new Button[GameConfig.PriorityKeys.Length];
            for (int i = 0; i < priorityButtons.Length; i++)
            {
                int index = i;
                var button = UIFactory.CreateButton("Priority_" + GameConfig.PriorityKeys[i], root.transform,
                    GameConfig.PriorityNames[i], 14, GameConfig.ButtonColor, GameConfig.TextColor);
                UIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f + i * 70f, -166f), new Vector2(62f, 30f));
                button.onClick.AddListener(() => SetPriority(index));
                priorityButtons[i] = button;
            }

            var descLabel = UIFactory.CreateText("DescLabel", root.transform, "描述（可选）", 14,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(descLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -206f), new Vector2(472f, 20f));

            descriptionInput = UIFactory.CreateInput("DescInput", root.transform, "补充说明（可留空）",
                14, true);
            UIFactory.Place(descriptionInput.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -228f), new Vector2(472f, 86f));

            hintText = UIFactory.CreateText("Hint", root.transform, "", 13,
                TextAnchor.MiddleLeft, GameConfig.TextDimColor);
            UIFactory.Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -322f), new Vector2(472f, 24f));

            cancelButton = UIFactory.CreateButton("CancelButton", root.transform, "取消", 15,
                GameConfig.ButtonColor, GameConfig.TextColor);
            UIFactory.Place(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 16f), new Vector2(100f, 36f));
            cancelButton.onClick.AddListener(() =>
            {
                if (OnCancelRequested != null) OnCancelRequested();
            });

            submitButton = UIFactory.CreateButton("SubmitButton", root.transform, "提交", 15,
                GameConfig.AccentColor, Color.white);
            UIFactory.Place(submitButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 16f), new Vector2(110f, 36f));
            submitButton.onClick.AddListener(HandleSubmitClicked);

            RefreshPriorityButtons();
            root.SetActive(false);
        }

        /// <summary>打开表单，并把点击位置显示出来。</summary>
        public void Open(Vector3 worldPosition)
        {
            if (root == null) return;

            root.SetActive(true);
            positionText.text = string.Format("位置: x {0:F2} / y {1:F2} / z {2:F2}",
                worldPosition.x, worldPosition.y, worldPosition.z);
            ShowHint("", false);
            SetBusy(false);

            // 打开就把焦点交给标题输入框，省得用户还要点一下
            titleInput.Select();
            titleInput.MoveTextEnd(false);
        }

        /// <summary>关闭表单。</summary>
        public void Close()
        {
            if (root != null) root.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>清空表单内容。</summary>
        public void Reset()
        {
            if (titleInput != null) titleInput.text = "";
            if (descriptionInput != null) descriptionInput.text = "";
            SetPriority(1);
            ShowHint("", false);
        }

        /// <summary>提交中禁用按钮，避免连点提交出重复数据。</summary>
        public void SetBusy(bool busy)
        {
            if (submitButton != null) submitButton.interactable = !busy;
            if (cancelButton != null) cancelButton.interactable = !busy;
        }

        /// <summary>显示提示信息。</summary>
        public void ShowHint(string message, bool isError)
        {
            if (hintText == null) return;
            hintText.text = message ?? "";
            hintText.color = isError ? GameConfig.DangerColor : GameConfig.TextDimColor;
        }

        /// <summary>设置选中的优先级下标（0=低 1=中 2=高）。</summary>
        public void SetPriority(int index)
        {
            selectedPriorityIndex = Mathf.Clamp(index, 0, GameConfig.PriorityKeys.Length - 1);
            RefreshPriorityButtons();
        }

        /// <summary>当前选中的优先级 key。</summary>
        public string CurrentPriority
        {
            get { return GameConfig.PriorityKeys[selectedPriorityIndex]; }
        }

        private void RefreshPriorityButtons()
        {
            if (priorityButtons == null) return;
            for (int i = 0; i < priorityButtons.Length; i++)
            {
                bool active = i == selectedPriorityIndex;
                var image = priorityButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? GameConfig.ButtonActiveColor : GameConfig.ButtonColor;
                }
            }
        }

        private void HandleSubmitClicked()
        {
            string title = titleInput != null ? titleInput.text.Trim() : "";
            if (string.IsNullOrEmpty(title))
            {
                // 标题必填：这是前端第一道校验，后端还会再校验一次
                ShowHint("标题不能为空", true);
                if (titleInput != null) titleInput.Select();
                return;
            }

            string description = descriptionInput != null ? descriptionInput.text.Trim() : "";
            if (OnSubmitRequested != null)
            {
                OnSubmitRequested(title, description, CurrentPriority);
            }
        }
    }
}
