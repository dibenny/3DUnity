using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace InspectionDemo
{
    /// <summary>
    /// 封装对 Go 后端的 HTTP 调用（基于 UnityWebRequest）。
    ///
    /// 所有回调统一成 (bool success, string message, IssueDto issue)，
    /// message 在失败时就是可以直接显示给用户的中文提示。
    /// </summary>
    public class IssueApiClient : MonoBehaviour
    {
        public static IssueApiClient Instance { get; private set; }

        private string baseUrl = GameConfig.DefaultServerBaseUrl;

        /// <summary>当前后端根地址（不带结尾斜杠）。</summary>
        public string BaseUrl
        {
            get { return baseUrl; }
        }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>切换后端地址。会去掉结尾多余的斜杠。</summary>
        public void SetBaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            baseUrl = url.Trim().TrimEnd('/');
        }

        /// <summary>健康检查：GET /api/health。用来在界面上显示「已连接 / 连不上」。</summary>
        public IEnumerator CheckHealth(Action<bool, string> onComplete)
        {
            using (var request = UnityWebRequest.Get(baseUrl + "/api/health"))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete(true, "已连接");
                }
                else
                {
                    onComplete(false, DescribeError(request));
                }
            }
        }

        /// <summary>创建问题：POST /api/issues。</summary>
        public IEnumerator PostIssue(CreateIssueDto payload, Action<bool, string, IssueDto> onComplete)
        {
            string json = JsonUtility.ToJson(payload);

            using (var request = new UnityWebRequest(baseUrl + "/api/issues", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    IssueDto issue = null;
                    try
                    {
                        issue = JsonUtility.FromJson<IssueDto>(request.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[IssueApiClient] 解析响应失败: " + e.Message);
                    }
                    onComplete(true, "上报成功", issue);
                }
                else
                {
                    onComplete(false, DescribeError(request), null);
                }
            }
        }

        /// <summary>修改状态：PATCH /api/issues/{id}/status。</summary>
        public IEnumerator PatchStatus(string id, string status, Action<bool, string, IssueDto> onComplete)
        {
            string json = JsonUtility.ToJson(new UpdateStatusDto(status));
            string url = baseUrl + "/api/issues/" + UnityWebRequest.EscapeURL(id) + "/status";

            using (var request = new UnityWebRequest(url, "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    IssueDto issue = null;
                    try
                    {
                        issue = JsonUtility.FromJson<IssueDto>(request.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[IssueApiClient] 解析响应失败: " + e.Message);
                    }
                    onComplete(true, "状态已更新", issue);
                }
                else
                {
                    onComplete(false, DescribeError(request), null);
                }
            }
        }

        /// <summary>把 UnityWebRequest 的失败翻译成用户看得懂的中文。</summary>
        private static string DescribeError(UnityWebRequest request)
        {
            // 后端返回了标准错误体，优先用它自己的 message
            if (request.responseCode >= 400 && request.downloadHandler != null &&
                !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<ErrorResponseDto>(request.downloadHandler.text);
                    if (parsed != null && parsed.error != null && !string.IsNullOrEmpty(parsed.error.message))
                    {
                        return parsed.error.message;
                    }
                }
                catch (Exception)
                {
                    // 不是 JSON 错误体，走下面的兜底
                }
            }

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return "无法连接后端服务，请确认 Go 服务是否启动。";
            }
            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                return "请求失败（HTTP " + request.responseCode + "）";
            }
            if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return "响应数据解析失败。";
            }
            return "请求失败：" + request.error;
        }
    }
}
