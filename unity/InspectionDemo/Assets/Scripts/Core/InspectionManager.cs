using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>巡检上报的状态机。</summary>
    public enum InspectionState
    {
        /// <summary>空闲，可以点击场景。</summary>
        Idle,
        /// <summary>刚放好标记。</summary>
        MarkerPlaced,
        /// <summary>表单已打开。</summary>
        FormOpen,
        /// <summary>正在提交。</summary>
        Submitting,
        /// <summary>提交成功。</summary>
        Success,
        /// <summary>提交失败。</summary>
        Error,
    }

    /// <summary>
    /// 巡检流程总控：标记 → 表单 → 提交 → 成功 / 失败。
    ///
    /// 临时标记的生命周期全部收在这里，避免「取消了但标记还在」这类泄漏：
    ///   - 取消 → 删掉标记
    ///   - 提交成功 → 删掉标记（问题已经在后端有记录了，场景里不用再留一个）
    ///   - 提交失败 → 保留标记和表单，用户改完可以直接重试；点取消才会清掉
    /// </summary>
    public class InspectionManager : MonoBehaviour
    {
        public static InspectionManager Instance { get; private set; }

        /// <summary>当前状态。</summary>
        public InspectionState State { get; private set; } = InspectionState.Idle;

        [Header("运行时引用（由 SceneBootstrap 赋值）")]
        public IssueApiClient apiClient;
        public IssueFormUI formUI;
        public Text toastText;
        public GameObject toastPanel;

        private IssueMarker currentMarker;
        private float stateResetTime;
        private bool resetStateLater;
        private Coroutine toastRoutine;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // 成功 / 失败后停留一下再回到 Idle，让用户能看清提示，
            // 同时避免状态永久卡住 —— RaycastClickHandler 只在 Idle 时才放标记。
            if (resetStateLater && Time.time >= stateResetTime)
            {
                resetStateLater = false;
                State = InspectionState.Idle;
            }
        }

        /// <summary>在点击位置放标记，并打开表单。</summary>
        public void PlaceMarker(Vector3 point)
        {
            PlaceMarker(point, Vector3.up);
        }

        public void PlaceMarker(Vector3 point, Vector3 surfaceNormal)
        {
            if (State != InspectionState.Idle) return;

            // 只允许一个临时标记，重复点击时先把上一个清掉
            ClearCurrentMarker();

            currentMarker = IssueMarker.Create(point, surfaceNormal);
            State = InspectionState.MarkerPlaced;
            State = InspectionState.FormOpen;

            if (formUI != null)
            {
                formUI.Open(point);
            }
        }

        /// <summary>表单点了「提交」。</summary>
        public void Submit(string title, string description, string priority)
        {
            if (State == InspectionState.Submitting) return;

            if (currentMarker == null)
            {
                ShowToast("没有待上报的标记", true);
                return;
            }

            State = InspectionState.Submitting;
            if (formUI != null)
            {
                formUI.SetBusy(true);
                formUI.ShowHint("正在上报…", false);
            }

            var payload = new CreateIssueDto
            {
                title = title,
                description = description,
                priority = priority,
                position = new PositionDto(currentMarker.WorldPosition),
            };

            StartCoroutine(PostIssue(payload));
        }

        /// <summary>表单点了「取消」：删掉没保存的标记。</summary>
        public void Cancel()
        {
            ClearCurrentMarker();

            if (formUI != null)
            {
                formUI.Reset();
                formUI.Close();
            }

            State = InspectionState.Idle;
            ShowToast("已取消，标记已删除", false);
        }

        /// <summary>
        /// 固定数据的测试上报，用来快速验证 Unity → Go 链路通不通。
        /// 正常流程是「点场景 → 填表单」，这个按钮只是给联调用的。
        /// </summary>
        public void SubmitFixedTestIssue()
        {
            if (State == InspectionState.Submitting) return;

            // 表单开着的时候不要用测试按钮，避免把表单的状态搅乱
            if (formUI != null && formUI.IsVisible) return;

            State = InspectionState.Submitting;

            var payload = new CreateIssueDto
            {
                title = "测试上报：Unity → Go → React",
                description = "由 Unity 界面上的「测试上报」按钮触发",
                priority = "medium",
                position = new PositionDto(new Vector3(0f, 1f, -3f)),
            };

            StartCoroutine(PostIssue(payload));
        }

        private IEnumerator PostIssue(CreateIssueDto payload)
        {
            if (apiClient == null)
            {
                HandlePostFinished(false, "缺少 API 客户端", null);
                yield break;
            }

            yield return apiClient.PostIssue(payload, HandlePostFinished);
        }

        private void HandlePostFinished(bool success, string message, IssueDto issue)
        {
            if (success)
            {
                State = InspectionState.Success;
                stateResetTime = Time.time + 1.5f;
                resetStateLater = true;

                // 提交成功：临时标记没用了，删掉
                ClearCurrentMarker();

                if (formUI != null)
                {
                    formUI.SetBusy(false);
                    formUI.ShowHint("上报成功", false);
                    formUI.Close();
                    formUI.Reset();
                }

                ShowToast(issue != null ? "上报成功：" + issue.title : "上报成功", false);
                return;
            }

            State = InspectionState.Error;

            if (formUI != null)
            {
                formUI.SetBusy(false);
                if (formUI.IsVisible)
                {
                    formUI.ShowHint(message, true);
                }
                else
                {
                    // 没有表单打开 = 走的是「测试上报」按钮。
                    // 这时必须把状态放回 Idle，否则 RaycastClickHandler 会一直拒绝放标记，
                    // 用户就再也点不出新的问题了。
                    stateResetTime = Time.time + 1.5f;
                    resetStateLater = true;
                }
            }
            else
            {
                stateResetTime = Time.time + 1.5f;
                resetStateLater = true;
            }

            // 失败时保留标记和表单，用户可以直接改完重试；点「取消」才会清掉标记
            ShowToast("上报失败：" + message, true);
        }

        /// <summary>在 HUD 上显示一行提示。</summary>
        public void ShowToast(string message, bool isError)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.color = isError ? GameConfig.DangerColor : GameConfig.OkColor;
            if (toastPanel != null) toastPanel.SetActive(true);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToastLater());
        }

        private IEnumerator HideToastLater()
        {
            yield return new WaitForSeconds(2.8f);
            if (toastPanel != null) toastPanel.SetActive(false);
            toastRoutine = null;
        }

        private void ClearCurrentMarker()
        {
            if (currentMarker == null) return;
            Destroy(currentMarker.gameObject);
            currentMarker = null;
        }
    }
}
