using UnityEngine;

namespace InspectionDemo
{
    /// <summary>
    /// Scene marker for a temporary or persisted inspection issue.
    /// The temporary marker stores the click position. A persisted marker also
    /// stores the issue data and can be clicked to open issue details.
    /// </summary>
    public class IssueMarker : MonoBehaviour
    {
        private const float BaseSize = 0.24f;

        public Vector3 WorldPosition { get; private set; }
        public IssueDto Issue { get; private set; }
        public bool IsSaved { get; private set; }

        private Transform core;
        private float phase;
        private float baseScale = BaseSize;

        /// <summary>Create a temporary marker for a new report.</summary>
        public static IssueMarker Create(Vector3 position, Vector3 surfaceNormal, Transform parent = null)
        {
            return CreateInternal(position, surfaceNormal, null, parent);
        }

        /// <summary>Create a clickable marker restored from the backend.</summary>
        public static IssueMarker CreateSaved(IssueDto issue)
        {
            if (issue == null || issue.position == null) return null;
            return CreateInternal(issue.position.ToVector3(), Vector3.up, issue, null);
        }

        private static IssueMarker CreateInternal(Vector3 position, Vector3 surfaceNormal, IssueDto issue, Transform parent)
        {
            var root = new GameObject(issue == null ? "IssueMarker" : "IssueMarker_" + issue.id);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;

            Vector3 normal = surfaceNormal.sqrMagnitude < 0.001f ? Vector3.up : surfaceNormal.normalized;
            Vector3 coreLocal = Vector3.up * (BaseSize * 0.9f) + normal * (BaseSize * 0.75f);
            float scale = BaseSize;
            Color color = GameConfig.MarkerColor;

            if (issue != null)
            {
                if (issue.priority == "high") scale *= 1.35f;
                else if (issue.priority == "low") scale *= 0.85f;
                color = StatusColor(issue.status);
            }

            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreGo.name = "Core";
            coreGo.transform.SetParent(root.transform, false);
            coreGo.transform.localPosition = coreLocal;
            coreGo.transform.localScale = Vector3.one * scale;
            RemoveCollider(coreGo);
            coreGo.GetComponent<Renderer>().material = MaterialFactory.CreateOpaque(color, 0f, 0.35f);

            var stemGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stemGo.name = "Stem";
            stemGo.transform.SetParent(root.transform, false);
            stemGo.transform.localPosition = Vector3.up * (BaseSize * 0.45f) + normal * (BaseSize * 0.375f);
            stemGo.transform.localScale = new Vector3(0.025f, BaseSize * 0.45f, 0.025f);
            RemoveCollider(stemGo);
            stemGo.GetComponent<Renderer>().material = MaterialFactory.CreateOpaque(color * 0.72f, 0f, 0.2f);

            if (issue != null)
            {
                var hitCollider = root.AddComponent<SphereCollider>();
                hitCollider.center = coreLocal;
                hitCollider.radius = 0.34f;
            }

            var marker = root.AddComponent<IssueMarker>();
            marker.WorldPosition = position;
            marker.core = coreGo.transform;
            marker.baseScale = scale;
            marker.Issue = issue;
            marker.IsSaved = issue != null;
            return marker;
        }

        private static Color StatusColor(string status)
        {
            if (status == "resolved") return new Color(0.34f, 0.72f, 0.42f);
            if (status == "in_progress") return new Color(0.24f, 0.52f, 0.95f);
            return new Color(0.88f, 0.34f, 0.25f);
        }

        private void Update()
        {
            if (core == null) return;
            phase += Time.deltaTime * 3.2f;
            float pulse = 1f + Mathf.Sin(phase) * 0.14f;
            core.localScale = new Vector3(baseScale * pulse, baseScale * pulse, baseScale * pulse);
        }

        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
