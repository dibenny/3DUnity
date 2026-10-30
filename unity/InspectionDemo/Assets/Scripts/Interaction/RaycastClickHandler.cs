using UnityEngine;
using UnityEngine.EventSystems;

namespace InspectionDemo
{
    /// <summary>
    /// 左键点击场景 → 在命中位置生成问题标记。
    ///
    /// 三道关卡（顺序不能换）：
    /// 1. 指针在 UI 上 → 直接返回，不放标记。这是最容易翻车的一条：
    ///    不判断的话，点「提交」按钮会在场景里也放一个标记。
    /// 2. 表单开着 → 不放新标记，避免误操作。
    /// 3. 射线检测 → 必须真的打到东西才放。
    /// </summary>
    public class RaycastClickHandler : MonoBehaviour
    {
        public Camera targetCamera;

        [Tooltip("参与点击检测的层。默认全选。")]
        public LayerMask clickableLayers = ~0;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // 1. UI 拦截
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var manager = InspectionManager.Instance;
            if (manager == null) return;

            // 2. 表单/提交进行中不再放标记
            if (manager.State != InspectionState.Idle) return;

            var cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null) return;

            // 3. 射线检测
            Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, GameConfig.RaycastMaxDistance,
                    clickableLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var marker = hit.collider.GetComponentInParent<IssueMarker>();
            if (marker != null && marker.IsSaved)
            {
                manager.ShowIssueDetail(marker.Issue);
                return;
            }

            manager.PlaceMarker(hit.point, hit.normal);
        }
    }
}
