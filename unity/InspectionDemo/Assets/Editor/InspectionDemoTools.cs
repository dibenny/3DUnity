using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InspectionDemo.EditorTools
{
    /// <summary>
    /// Unity Editor 下的辅助工具。
    ///
    /// 这个文件放在 Assets/Editor/ 下，只在编辑器里编译，不会进打包产物。
    ///
    /// 提供两个菜单：
    ///   Tools / 巡检 Demo / 一键自检   —— 把「要盯的点」全部跑一遍并打印报告
    ///   Tools / 巡检 Demo / 重建场景   —— 万一 Main.unity 打不开，用这个重新生成
    /// </summary>
    public static class InspectionDemoTools
    {
        private const string MenuRoot = "Tools/巡检 Demo/";

        // ---------------------------------------------------------------
        // 一键自检
        // ---------------------------------------------------------------

        [MenuItem(MenuRoot + "一键自检")]
        public static void RunSelfCheck()
        {
            // batchmode 下 Unity 默认不开场景（活动场景是空的 untitled），
            // 这里主动打开 Main，顺带验证手写的场景文件能否被引擎正常加载
            if (Application.isBatchMode)
            {
                var mainScene = EditorSceneManager.OpenScene(
                    "Assets/Scenes/Main.unity", OpenSceneMode.Single);
                Debug.Log($"[SelfCheck] opened scene '{mainScene.name}', " +
                          $"roots={mainScene.GetRootGameObjects().Length}");
            }

            var report = new StringBuilder();
            int passed = 0;
            int failed = 0;

            report.AppendLine("========== 巡检 Demo 自检 ==========");
            report.AppendLine($"Unity 版本: {Application.unityVersion}");
            report.AppendLine($"运行模式: {(Application.isPlaying ? "Play（运行时）" : "Edit（编辑模式）")}");
            report.AppendLine("");

            // 1. 场景
            var scene = SceneManager.GetActiveScene();
            bool sceneOk = scene.IsValid() && scene.name == "Main";
            var roots = scene.IsValid() ? scene.GetRootGameObjects() : new GameObject[0];
            bool bootstrapOk = false;
            foreach (var root in roots)
            {
                if (root.GetComponent<SceneBootstrap>() != null)
                {
                    bootstrapOk = true;
                    break;
                }
            }
            Report(report, ref passed, ref failed, "场景",
                $"{scene.name}（根物体 {roots.Length} 个）",
                sceneOk && bootstrapOk,
                sceneOk && bootstrapOk ? "正常" : "场景不对，用「Tools/巡检 Demo/重建场景」重新生成");

            // 2. 场景内容（只有 Play 模式下运行时才搭好了）
            if (Application.isPlaying)
            {
                bool hasGround = GameObject.Find("Ground") != null;
                bool hasPlayer = GameObject.Find("Player") != null;
                bool hasCamera = Camera.main != null;
                bool hasCanvas = GameObject.Find("Canvas") != null;
                bool hasEventSystem = EventSystemExists();

                Report(report, ref passed, ref failed, "运行时场景",
                    $"Ground={hasGround} Player={hasPlayer} Camera={hasCamera} Canvas={hasCanvas} EventSystem={hasEventSystem}",
                    hasGround && hasPlayer && hasCamera && hasCanvas && hasEventSystem,
                    "有缺失，看 Console 里 SceneBootstrap 的报错");

                var manager = InspectionManager.Instance;
                Report(report, ref passed, ref failed, "InspectionManager",
                    manager != null ? $"已初始化，当前状态 {manager.State}" : "Instance 为 null",
                    manager != null,
                    manager != null ? "正常" : "SceneBootstrap 没跑起来");

                var api = IssueApiClient.Instance;
                Report(report, ref passed, ref failed, "IssueApiClient",
                    api != null ? $"已初始化，后端 {api.BaseUrl}" : "Instance 为 null",
                    api != null,
                    api != null ? "正常" : "SceneBootstrap 没跑起来");
            }
            else
            {
                report.AppendLine("[提示] 编辑模式下场景对象还没搭出来，按 Play 后再自检一次可以验证运行时。");
                report.AppendLine("");
            }

            // 3. 字体（中文会不会显示成方块）
            var font = FontProvider.Get();
            bool fontOk = font != null;
            string fontNote;
            if (!fontOk)
            {
                fontNote = "没找到任何字体";
            }
            else
            {
                // 「巡」这个字在系统字体里有没有，决定中文能不能正常显示
                bool hasChinese = font.HasCharacter('巡');
                fontNote = hasChinese
                    ? $"字体 {font.name}，含中文字形"
                    : $"字体 {font.name} 不含中文字形，界面中文会显示成方块 → 改 FontProvider.Candidates 加一个系统字体";
                fontOk = hasChinese;
            }
            Report(report, ref passed, ref failed, "中文字体", fontNote, fontOk,
                fontOk ? "正常" : "按上面的说明改 FontProvider.Candidates");

            // 4. 数据契约（JsonUtility 字段名大小写写错会静默丢字段）
            const string sample = "{\"id\":\"x\",\"title\":\"契约自检\",\"description\":\"d\"," +
                                 "\"priority\":\"high\",\"status\":\"open\"," +
                                 "\"position\":{\"x\":1.5,\"y\":2.5,\"z\":3.5}," +
                                 "\"createdAt\":\"2026-10-29T00:00:00Z\",\"updatedAt\":\"2026-10-29T00:00:00Z\"}";
            var issue = JsonUtility.FromJson<IssueDto>(sample);
            bool dtoOk = issue != null
                         && issue.title == "契约自检"
                         && issue.priority == "high"
                         && issue.status == "open"
                         && issue.position != null
                         && Mathf.Approximately(issue.position.x, 1.5f)
                         && !string.IsNullOrEmpty(issue.createdAt);
            Report(report, ref passed, ref failed, "数据契约（JsonUtility）",
                dtoOk
                    ? "字段名与 Go 完全对得上"
                    : $"字段对不上：title={issue?.title} priority={issue?.priority} " +
                      $"position.x={issue?.position?.x} createdAt={issue?.createdAt}",
                dtoOk,
                dtoOk ? "正常" : "检查 IssueDto.cs 的字段名（必须和 JSON 完全一致，含大小写）");

            // 5. 后端连通
            var apiClient = IssueApiClient.Instance;
            string url = (apiClient != null ? apiClient.BaseUrl : GameConfig.DefaultServerBaseUrl) + "/api/health";
            string healthNote;
            bool healthOk = false;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 5000;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string body = reader.ReadToEnd();
                    healthOk = response.StatusCode == HttpStatusCode.OK && body.Contains("\"ok\"");
                    healthNote = $"{url} → HTTP {(int)response.StatusCode} {body}";
                }
            }
            catch (Exception e)
            {
                healthNote = $"{url} → 连不上：{e.Message}";
            }
            Report(report, ref passed, ref failed, "后端连通", healthNote, healthOk,
                healthOk ? "正常" : "确认 Go 服务已启动（cd server && go run ./cmd/server）");

            // 6. Unity API 桩自检（如果项目里带了 tools/csharp-verify）
            report.AppendLine("");
            report.AppendLine($"结果: {passed} 项通过, {failed} 项失败");
            report.AppendLine("======================================");

            string text = report.ToString();
            Debug.Log(text);

            // batchmode（命令行自检）下不能弹窗，报告写文件
            if (Application.isBatchMode)
            {
                string outPath = Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ?? ".", "selfcheck.txt");
                File.WriteAllText(outPath, text);
                Debug.Log($"[SelfCheck] report written to {outPath}");
                return;
            }

            if (failed == 0)
            {
                EditorUtility.DisplayDialog("巡检 Demo 自检", $"全部 {passed} 项通过。\n\n{text}", "好");
            }
            else
            {
                EditorUtility.DisplayDialog("巡检 Demo 自检",
                    $"{failed} 项未通过，{passed} 项通过。\n\n详细结果已打印到 Console。", "知道了");
            }
        }

        // ---------------------------------------------------------------
        // 重建场景
        // ---------------------------------------------------------------

        [MenuItem(MenuRoot + "重建场景")]
        public static void RebuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("SceneBootstrap");
            go.AddComponent<SceneBootstrap>();

            bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? "[巡检 Demo] 场景已重建：Assets/Scenes/Main.unity（只有一个挂着 SceneBootstrap 的空物体）"
                : "[巡检 Demo] 场景重建失败，请检查 Assets/Scenes 目录是否可写");
        }

        [MenuItem(MenuRoot + "重建场景", true)]
        private static bool RebuildSceneValidate()
        {
            return !Application.isPlaying;
        }

        // ---------------------------------------------------------------
        // 小工具
        // ---------------------------------------------------------------

        private static void Report(StringBuilder sb, ref int passed, ref int failed,
            string name, string detail, bool ok, string advice)
        {
            if (ok) passed++; else failed++;
            sb.AppendLine($"[{(ok ? "通过" : "失败")}] {name}: {detail}");
            if (!ok)
            {
                sb.AppendLine($"         → {advice}");
            }
        }

        private static bool EventSystemExists()
        {
            var go = GameObject.Find("EventSystem");
            return go != null && go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null;
        }
    }
}
