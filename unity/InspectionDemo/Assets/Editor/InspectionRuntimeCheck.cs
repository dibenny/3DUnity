using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InspectionDemo.EditorTools
{
    [InitializeOnLoad]
    public static class InspectionRuntimeCheck
    {
        const string ActiveKey = "InspectionRuntimeCheck.Active";

        static InspectionRuntimeCheck()
        {
            if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
        }
        const string ScenePath = "Assets/Scenes/Main.unity";
        static readonly StringBuilder R = new StringBuilder();
        static int pass, fail;
        static double t0, submitT0, cancelCheckAt;
        static int stage;

        [MenuItem("Tools/Inspection Demo/Runtime E2E Self Check")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            R.Length = 0; pass = 0; fail = 0; stage = 0;
            SessionState.SetBool(ActiveKey, true);
            t0 = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (EditorApplication.isPlaying == false) return;
            if (t0 <= 0) { t0 = EditorApplication.timeSinceStartup; return; }
            if (stage == 0)
            {
                if (EditorApplication.timeSinceStartup - t0 < 1.5) return;
                try
                {
                    CheckScene();
                    CheckRaycast();
                    CheckMaterials();
                    CheckMarker();
                    CheckTitle();
                    CheckSavedMarkers();
                    stage = 2;
                }
                catch (Exception e)
                {
                    Fail("runtime check exception", e.ToString());
                    Finish();
                }
                return;
            }
            if (stage == 2)
            {
                if (EditorApplication.timeSinceStartup - cancelCheckAt < 0.2) return;
                CheckCancelAfterFrame();
                BeginSubmit();
                stage = 1;
                return;
            }
            if (stage == 1)
            {
                var m = InspectionManager.Instance;
                if (m == null)
                {
                    Fail("backend submit", "manager is null");
                    Finish();
                    return;
                }
                if (m.State == InspectionState.Submitting)
                {
                    if (EditorApplication.timeSinceStartup - submitT0 > 10)
                    {
                        Fail("backend submit", "timeout");
                        Finish();
                    }
                    return;
                }
                if (m.State == InspectionState.Success || BackendHasText("Unity runtime self check"))
                {
                    Pass("Unity to Go submit", "success response or backend record found");
                    Check("success feedback visible", m.toastText == null ? "toastText null" : (m.toastText.text + " active=" + (m.toastPanel != null && m.toastPanel.activeSelf)), m.toastText != null && m.toastText.text.Length > 0 && m.toastPanel != null && m.toastPanel.activeSelf);
                    CheckBackend();
                }
                else
                {
                    Fail("Unity to Go submit", "state=" + m.State);
                }
                Finish();
            }
        }

        static void CheckScene()
        {
            var ground = GameObject.Find("Ground");
            var player = GameObject.Find("Player");
            var canvas = GameObject.Find("Canvas");
            var cam = Camera.main;
            var es = EventSystem.current;
            bool ok = ground != null && player != null && canvas != null && cam != null && es != null;
            Check("runtime scene", string.Format("Ground={0} Player={1} Canvas={2} Camera={3} EventSystem={4}",
                ground != null, player != null, canvas != null, cam != null, es != null), ok);
            Check("InspectionManager", InspectionManager.Instance != null ? "initialized" : "null",
                InspectionManager.Instance != null);
            Check("IssueApiClient", IssueApiClient.Instance != null ? "initialized" : "null",
                IssueApiClient.Instance != null);
            Check("RaycastClickHandler", "attached to Camera", cam != null && cam.GetComponent<RaycastClickHandler>() != null);
        }

        static void CheckRaycast()
        {
            var cam = Camera.main;
            if (cam == null) { Fail("physics raycast", "camera is null"); return; }
            var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            RaycastHit hit;
            bool ok = Physics.Raycast(ray, out hit, GameConfig.RaycastMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            Check("physics raycast", ok ? ("hit=" + hit.collider.name + " point=" + hit.point) : "no hit", ok);
        }

        static void CheckMaterials()
        {
            var standard = Shader.Find("Standard");
            var unlit = Shader.Find("Unlit/Color");
            var template = Resources.Load<Material>("InspectionDefault");
            var ground = GameObject.Find("Ground");
            var renderer = ground == null ? null : ground.GetComponent<Renderer>();
            Check("shader Standard", standard == null ? "null" : standard.name, standard != null);
            Check("shader Unlit/Color", unlit == null ? "null" : unlit.name, unlit != null);
            Check("fallback material", template == null ? "null" : (template.shader == null ? "shader=null" : "shader=" + template.shader.name), template != null && template.shader != null);
            Check("ground material", renderer == null || renderer.sharedMaterial == null ? "null" : (renderer.sharedMaterial.shader == null ? "shader=null" : "shader=" + renderer.sharedMaterial.shader.name), renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null);
        }

        static void CheckSavedMarkers()
        {
            var markers = UnityEngine.Object.FindObjectsOfType<IssueMarker>();
            int saved = 0;
            IssueMarker first = null;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i].IsSaved)
                {
                    saved++;
                    if (first == null) first = markers[i];
                }
            }
            Check("saved markers loaded", "count=" + saved, saved > 0);
            var manager = InspectionManager.Instance;
            Check("issue detail UI", manager != null && manager.detailUI != null ? "present" : "null",
                manager != null && manager.detailUI != null);
            if (first != null && manager != null && manager.detailUI != null)
            {
                manager.ShowIssueDetail(first.Issue);
                Check("issue detail content", first.Issue.title, manager.detailUI.IsVisible);
                manager.detailUI.Hide();
            }
        }

        static void CheckMarker()
        {
            var m = InspectionManager.Instance;
            if (m == null || m.formUI == null)
            {
                Fail("marker/form", "manager or form is null");
                return;
            }
            m.PlaceMarker(new Vector3(1.25f, 0.8f, 2.4f));
            bool marker = GameObject.Find("IssueMarker") != null;
            bool form = m.formUI.IsVisible;
            Check("place marker", "marker=" + marker + " form=" + form + " state=" + m.State,
                marker && form && m.State == InspectionState.FormOpen);
            m.Cancel();
            cancelCheckAt = EditorApplication.timeSinceStartup;
        }

        static void CheckCancelAfterFrame()
        {
            var m = InspectionManager.Instance;
            bool gone = GameObject.Find("IssueMarker") == null;
            Check("cancel marker", "marker removed=" + gone + " form closed=" + (m.formUI.IsVisible == false) + " state=" + m.State,
                gone && m.formUI.IsVisible == false && m.State == InspectionState.Idle);
        }

        static void CheckTitle()
        {
            var m = InspectionManager.Instance;
            var form = m == null ? null : m.formUI;
            if (form == null) { Fail("empty title", "form is null"); return; }
            try
            {
                var tf = typeof(IssueFormUI).GetField("titleInput", BindingFlags.NonPublic | BindingFlags.Instance);
                var hf = typeof(IssueFormUI).GetField("hintText", BindingFlags.NonPublic | BindingFlags.Instance);
                var sm = typeof(IssueFormUI).GetMethod("HandleSubmitClicked", BindingFlags.NonPublic | BindingFlags.Instance);
                if (tf == null || hf == null || sm == null) { Fail("empty title", "reflection target missing"); return; }
                var input = (InputField)tf.GetValue(form);
                var hint = (Text)hf.GetValue(form);
                input.text = "   ";
                sm.Invoke(form, null);
                bool ok = hint == null ? false : hint.text == "\u6807\u9898\u4e0d\u80fd\u4e3a\u7a7a";
                Check("empty title validation", "hint=" + (hint == null ? "null" : hint.text), ok);
                input.text = "";
            }
            catch (Exception e) { Fail("empty title validation", e.ToString()); }
        }

        static void BeginSubmit()
        {
            var m = InspectionManager.Instance;
            if (m == null) { Fail("begin submit", "manager is null"); return; }
            m.PlaceMarker(new Vector3(2.2f, 1.1f, -0.5f));
            submitT0 = EditorApplication.timeSinceStartup;
            m.Submit("Unity runtime self check", "generated by InspectionRuntimeCheck", "high");
        }

        static bool BackendHasText(string needle)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8080/api/issues");
                req.Method = "GET"; req.Timeout = 5000;
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var rd = new StreamReader(res.GetResponseStream()))
                    return res.StatusCode == HttpStatusCode.OK && rd.ReadToEnd().Contains(needle);
            }
            catch { return false; }
        }

        static void CheckBackend()
        {
            Check("Go persistence", "GET /api/issues contains self check record", BackendHasText("Unity runtime self check"));
        }

        static void Check(string name, string detail, bool ok) { if (ok) Pass(name, detail); else Fail(name, detail); }

        static void Pass(string name, string detail)
        {
            pass++;
            R.AppendLine("[PASS] " + name + ": " + detail);
            Debug.Log("[RuntimeSelfCheck][PASS] " + name + ": " + detail);
        }

        static void Fail(string name, string detail)
        {
            fail++;
            R.AppendLine("[FAIL] " + name + ": " + detail);
            Debug.LogError("[RuntimeSelfCheck][FAIL] " + name + ": " + detail);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            R.AppendLine("");
            R.AppendLine("Result: " + pass + " passed, " + fail + " failed");
            string text = R.ToString();
            Debug.Log(text);
            string outPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "runtime-selfcheck.txt");
            File.WriteAllText(outPath, text);
            Debug.Log("[RuntimeSelfCheck] report: " + outPath);
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
            else EditorUtility.DisplayDialog("Runtime Self Check", text, "OK");
        }
    }
}
