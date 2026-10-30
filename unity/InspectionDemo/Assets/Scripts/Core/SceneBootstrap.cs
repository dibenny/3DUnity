using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>
    /// 场景初始化器。
    ///
    /// 地面、墙壁、可点击设备、光照、玩家、相机、整套 UI 全部在运行时用代码搭出来，
    /// 所以 Main.unity 场景文件里只有一个挂着本脚本的空物体 —— 打开直接按 Play 就能跑，
    /// 不需要手工拖任何 Prefab 或摆任何模型。
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("场景参数")]
        public float groundSize = 24f;
        public float wallHeight = 3.2f;

        private IssueApiClient apiClient;
        private InspectionManager manager;
        private IssueFormUI formUI;
        private IssueDetailUI detailUI;
        private int deviceCounter;
        private InputField serverInput;
        private Text healthText;
        private Text toastText;
        private GameObject toastPanel;
        private void Awake()
        {
            BuildLighting();
            BuildEnvironment();
            BuildPlayer();
            BuildUI();
        }

        private void Start()
        {
            StartCoroutine(CheckHealth());
        }

        // ------------------------------------------------------------------
        // 光照
        // ------------------------------------------------------------------

        private void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.44f, 0.47f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.29f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.16f);

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(transform, false);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;

            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        // ------------------------------------------------------------------
        // 环境：地面 + 四面墙 + 可点击设备
        // ------------------------------------------------------------------

        private void BuildEnvironment()
        {
            // 地面
            CreateBox("Ground", new Vector3(0f, -0.25f, 0f),
                new Vector3(groundSize, 0.5f, groundSize), GameConfig.GroundColor);

            float half = groundSize / 2f;
            CreateBox("Wall_North", new Vector3(0f, wallHeight / 2f, half),
                new Vector3(groundSize, wallHeight, 0.4f), GameConfig.WallColor);
            CreateBox("Wall_South", new Vector3(0f, wallHeight / 2f, -half),
                new Vector3(groundSize, wallHeight, 0.4f), GameConfig.WallColor);
            CreateBox("Wall_East", new Vector3(half, wallHeight / 2f, 0f),
                new Vector3(0.4f, wallHeight, groundSize), GameConfig.WallColor);
            CreateBox("Wall_West", new Vector3(-half, wallHeight / 2f, 0f),
                new Vector3(0.4f, wallHeight, groundSize), GameConfig.WallColor);

            // 可点击的设备：验收要求至少两个，这里给四个
            CreateDevice("机柜 A", new Vector3(-4.6f, 1.1f, 1.6f),
                new Vector3(1.6f, 2.2f, 1.0f), GameConfig.CabinetColor);
            CreateDevice("机柜 B", new Vector3(-2.4f, 1.1f, 1.6f),
                new Vector3(1.6f, 2.2f, 1.0f), GameConfig.CabinetColor);
            CreateDevice("配电箱", new Vector3(3.6f, 1.0f, -1.2f),
                new Vector3(1.2f, 2.0f, 0.7f), GameConfig.BoxColor);
            CreateDevice("管道井", new Vector3(1.4f, 0.9f, 3.8f),
                new Vector3(1.0f, 1.8f, 1.0f), GameConfig.BoxColor);
        }

        private GameObject CreateBox(string name, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = MaterialFactory.CreateOpaque(color);
            return go;
        }

        /// <summary>带世界空间标签的可点击设备。</summary>
        private GameObject CreateDevice(string label, Vector3 position, Vector3 size, Color color)
        {
            var go = CreateBox(label, position, size, color);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, size.y / 2f + 0.4f, 0f);

            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.font = FontProvider.Get();
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.02f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.90f, 0.91f, 0.90f);

            deviceCounter++;
            var codeGo = new GameObject("DeviceCode");
            codeGo.transform.SetParent(go.transform, false);
            codeGo.transform.localPosition = new Vector3(0f, size.y / 2f + 0.08f, 0f);
            var codeMesh = codeGo.AddComponent<TextMesh>();
            codeMesh.text = "DEV-" + deviceCounter.ToString("00") + "  AREA-" + (((deviceCounter + 1) / 2)).ToString("00");
            codeMesh.font = FontProvider.Get();
            codeMesh.fontSize = 48;
            codeMesh.characterSize = 0.016f;
            codeMesh.anchor = TextAnchor.UpperCenter;
            codeMesh.alignment = TextAlignment.Center;
            codeMesh.color = new Color(0.76f, 0.81f, 0.85f);

            return go;
        }

        // ------------------------------------------------------------------
        // 玩家 + 相机
        // ------------------------------------------------------------------

        private void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.SetParent(transform, false);
            player.transform.position = new Vector3(0f, 0f, -9f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.4f;

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(pivot.transform, false);

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = new Color(0.47f, 0.51f, 0.54f);
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            cameraGo.AddComponent<AudioListener>();

            var playerController = player.AddComponent<PlayerController>();
            playerController.cameraPivot = pivot.transform;

            var clickHandler = cameraGo.AddComponent<RaycastClickHandler>();
            clickHandler.targetCamera = camera;
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------

        private void BuildUI()
        {
            // 显式带上 RectTransform：Canvas 走的是 RectTransform，显式声明避免
// 不同 Unity 版本在 AddComponent 时是否自动替换 Transform 的行为差异。
var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // UI 要能接收点击就必须有 EventSystem
            if (EventSystem.current == null)
            {
                var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystemGo.transform.SetParent(transform, false);
            }

            manager = gameObject.AddComponent<InspectionManager>();
            apiClient = gameObject.AddComponent<IssueApiClient>();

            BuildHud(canvasGo.transform);
            BuildHintBar(canvasGo.transform);

            var crosshair = UIFactory.CreatePanel("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.55f));
            UIFactory.Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(4f, 4f));

            formUI = gameObject.AddComponent<IssueFormUI>();
            formUI.Build(canvasGo.transform);
            BuildToast(canvasGo.transform);
            detailUI = gameObject.AddComponent<IssueDetailUI>();
            detailUI.Build(canvasGo.transform);

            manager.apiClient = apiClient;
            manager.formUI = formUI;
            manager.detailUI = detailUI;
            manager.toastText = toastText;
            manager.toastPanel = toastPanel;

            formUI.OnSubmitRequested += HandleFormSubmit;
            formUI.OnCancelRequested += HandleFormCancel;
        }

        private void BuildHud(Transform canvasTransform)
        {
            var hud = UIFactory.CreatePanel("HUD", canvasTransform, new Color(0f, 0f, 0f, 0.72f));
            UIFactory.Place(hud.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(430f, 158f));

            var title = UIFactory.CreateText("Title", hud.transform, "巡检上报 Demo", 18,
                TextAnchor.MiddleLeft, GameConfig.TextColor);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -12f), new Vector2(402f, 24f));

            var urlLabel = UIFactory.CreateText("UrlLabel", hud.transform, "后端地址", 12,
                TextAnchor.MiddleLeft, GameConfig.TextDimColor);
            UIFactory.Place(urlLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -38f), new Vector2(402f, 18f));

            serverInput = UIFactory.CreateInput("ServerInput", hud.transform, "http://127.0.0.1:8080",
                13, false);
            UIFactory.Place(serverInput.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -58f), new Vector2(300f, 28f));
            serverInput.contentType = InputField.ContentType.Standard;
            serverInput.text = GameConfig.DefaultServerBaseUrl;

            var connectButton = UIFactory.CreateButton("ConnectButton", hud.transform, "连接", 13,
                GameConfig.ButtonColor, GameConfig.TextColor);
            UIFactory.Place(connectButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(320f, -58f), new Vector2(96f, 28f));
            connectButton.onClick.AddListener(HandleConnectClicked);

            healthText = UIFactory.CreateText("Health", hud.transform, "后端状态：检测中…", 12,
                TextAnchor.MiddleLeft, GameConfig.TextDimColor);
            UIFactory.Place(healthText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -90f), new Vector2(402f, 18f));

            var testButton = UIFactory.CreateButton("TestButton", hud.transform, "测试上报（固定数据）", 13,
                GameConfig.ButtonColor, GameConfig.TextColor);
            UIFactory.Place(testButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -114f), new Vector2(170f, 28f));
            testButton.onClick.AddListener(HandleTestClicked);

            var refreshMarkersButton = UIFactory.CreateButton("RefreshMarkersButton", hud.transform,
                "\u5237\u65b0\u6807\u8bb0", 13, GameConfig.ButtonColor, GameConfig.TextColor);
            UIFactory.Place(refreshMarkersButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(192f, -114f), new Vector2(120f, 28f));
            refreshMarkersButton.onClick.AddListener(HandleRefreshMarkersClicked);
        }

        private void BuildHintBar(Transform canvasTransform)
        {
            var hintPanel = UIFactory.CreatePanel("HintBar", canvasTransform, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(hintPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(860f, 32f));

            var hint = UIFactory.CreateText("Hint", hintPanel.transform,
                "左键点击设备放置标记 · 右键拖动转动视角 · WASD 移动 · Shift 加速 · 标题必填",
                14, TextAnchor.MiddleCenter, GameConfig.TextColor);
            UIFactory.Stretch(hint.rectTransform);
        }

        private void BuildToast(Transform canvasTransform)
        {
            var panel = UIFactory.CreatePanel("ToastPanel", canvasTransform, new Color(0.04f, 0.05f, 0.06f, 0.95f));
            toastPanel = panel.gameObject;
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 64f), new Vector2(720f, 48f));

            toastText = UIFactory.CreateText("ToastText", panel.transform, "", 16,
                TextAnchor.MiddleCenter, GameConfig.OkColor);
            UIFactory.Stretch(toastText.rectTransform, 16f, 0f, 16f, 0f);
            toastPanel.SetActive(false);
        }
        // ------------------------------------------------------------------
        // 交互回调
        // ------------------------------------------------------------------

        private void HandleConnectClicked()
        {
            if (apiClient == null || serverInput == null) return;
            apiClient.SetBaseUrl(serverInput.text);
            serverInput.text = apiClient.BaseUrl;
            StartCoroutine(CheckHealth());
        }

        private void HandleTestClicked()
        {
            if (manager != null) manager.SubmitFixedTestIssue();
        }

        private void HandleRefreshMarkersClicked()
        {
            if (manager != null) manager.RefreshSavedIssues(true);
        }

        private void HandleFormSubmit(string title, string description, string priority)
        {
            if (manager != null) manager.Submit(title, description, priority);
        }

        private void HandleFormCancel()
        {
            if (manager != null) manager.Cancel();
        }

        private IEnumerator CheckHealth()
        {
            if (apiClient == null || healthText == null) yield break;

            healthText.text = "后端状态：检测中…";
            healthText.color = GameConfig.TextDimColor;

            yield return apiClient.CheckHealth((ok, message) =>
            {
                if (ok)
                {
                    healthText.text = "后端状态：已连接 · " + apiClient.BaseUrl;
                    healthText.color = GameConfig.OkColor;
                }
                else
                {
                    healthText.text = "后端状态：未连接 · " + message;
                    healthText.color = GameConfig.DangerColor;
                }
            });
        }
    }
}
