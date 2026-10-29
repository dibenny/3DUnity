using UnityEngine;

namespace InspectionDemo
{
    /// <summary>
    /// 场景里的问题标记。
    ///
    /// 职责很窄：记住被点击点的世界坐标（提交时原样发给后端），并做一个轻微脉动，
    /// 让远处也能一眼看到标记在哪。
    ///
    /// 标记本体是运行时生成的（等价于实例化一个 Prefab），好处是不用手工拖 Prefab，
    /// 打开场景直接按 Play 就有。
    /// </summary>
    public class IssueMarker : MonoBehaviour
    {
        private const float BaseSize = 0.24f;

        /// <summary>标记对应的世界坐标。</summary>
        public Vector3 WorldPosition { get; private set; }

        private Transform core;
        private float phase;

        /// <summary>在指定世界坐标生成一个标记。</summary>
        public static IssueMarker Create(Vector3 position, Vector3 surfaceNormal, Transform parent = null)
        {
            var root = new GameObject("IssueMarker");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;
            Vector3 normal = surfaceNormal.sqrMagnitude < 0.001f ? Vector3.up : surfaceNormal.normalized;
            // 球体
            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreGo.name = "Core";
            coreGo.transform.SetParent(root.transform, false);
            coreGo.transform.localPosition = Vector3.up * (BaseSize * 0.9f) + normal * (BaseSize * 0.75f);
            coreGo.transform.localScale = Vector3.one * BaseSize;
            RemoveCollider(coreGo);
            coreGo.GetComponent<Renderer>().material =
                MaterialFactory.CreateOpaque(GameConfig.MarkerColor, 0f, 0.35f);

            // 立杆：让「标记落在哪」更清楚
            var stemGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stemGo.name = "Stem";
            stemGo.transform.SetParent(root.transform, false);
            stemGo.transform.localPosition = Vector3.up * (BaseSize * 0.45f) + normal * (BaseSize * 0.375f);
            stemGo.transform.localScale = new Vector3(0.025f, BaseSize * 0.45f, 0.025f);
            RemoveCollider(stemGo);
            stemGo.GetComponent<Renderer>().material =
                MaterialFactory.CreateOpaque(new Color(0.62f, 0.24f, 0.20f), 0f, 0.2f);

            var marker = root.AddComponent<IssueMarker>();
            marker.WorldPosition = position;
            marker.core = coreGo.transform;
            return marker;
        }

        private void Update()
        {
            if (core == null) return;

            phase += Time.deltaTime * 3.2f;
            float scale = BaseSize * (1f + Mathf.Sin(phase) * 0.14f);
            core.localScale = new Vector3(scale, scale, scale);
        }

        /// <summary>去掉碰撞体，避免标记挡住后续的射线点击。</summary>
        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
