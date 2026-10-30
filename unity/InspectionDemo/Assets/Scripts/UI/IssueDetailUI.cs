using UnityEngine;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>Read-only issue detail panel opened by clicking a saved marker.</summary>
    public class IssueDetailUI : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text bodyText;

        public bool IsVisible
        {
            get { return root != null && root.activeSelf; }
        }

        public void Build(Transform canvasTransform)
        {
            root = new GameObject("IssueDetail", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var backdrop = UIFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.56f));
            UIFactory.Stretch(backdrop.rectTransform);

            var panel = UIFactory.CreatePanel("Panel", root.transform, new Color(0.08f, 0.09f, 0.11f, 0.98f));
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(660f, 480f));

            titleText = UIFactory.CreateText("Title", panel.transform, "\u95ee\u9898\u8be6\u60c5", 21,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -20f), new Vector2(600f, 32f));

            bodyText = UIFactory.CreateText("Body", panel.transform, "", 15,
                TextAnchor.UpperLeft, GameConfig.TextColor);
            UIFactory.Place(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -66f), new Vector2(612f, 326f));

            var close = UIFactory.CreateButton("Close", panel.transform, "\u5173\u95ed", 15,
                GameConfig.AccentColor, Color.white);
            UIFactory.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 18f), new Vector2(110f, 38f));
            close.onClick.AddListener(Hide);

            root.SetActive(false);
        }

        public void Show(IssueDto issue)
        {
            if (root == null || issue == null) return;
            titleText.text = string.Format("\u95ee\u9898\u8be6\u60c5  {0}", issue.title);
            bodyText.text = string.Format(
                "ID: {0}\n\n\u6807\u9898: {1}\n\n\u63cf\u8ff0: {2}\n\n\u4f18\u5148\u7ea7: {3}\n\n\u72b6\u6001: {4}\n\n\u4f4d\u7f6e: x {5:F2} / y {6:F2} / z {7:F2}\n\n\u521b\u5efa\u65f6\u95f4: {8}\n\n\u66f4\u65b0\u65f6\u95f4: {9}",
                issue.id, issue.title,
                string.IsNullOrEmpty(issue.description) ? "\u65e0\u63cf\u8ff0" : issue.description,
                PriorityText(issue.priority), StatusText(issue.status),
                issue.position.x, issue.position.y, issue.position.z,
                issue.createdAt, issue.updatedAt);
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static string PriorityText(string value)
        {
            if (value == "high") return "\u9ad8";
            if (value == "low") return "\u4f4e";
            return "\u4e2d";
        }

        private static string StatusText(string value)
        {
            if (value == "resolved") return "\u5df2\u89e3\u51b3";
            if (value == "in_progress") return "\u5904\u7406\u4e2d";
            return "\u5f85\u5904\u7406";
        }
    }
}
