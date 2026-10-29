# 巡检问题上报 Demo（Unity + Go + React）

一个端到端跑通的最小完整项目：

> **在 Unity 场景里点击设备 → 生成标记 → 填表上报 → Go 存进 SQLite → React 网页实时查看并改状态**

三端共用同一套 JSON 数据契约，没有任何 UI 框架、没有付费模型、没有第三方后端框架。

---

## 1. 快速开始

三个终端，按顺序来。

### 终端 A：Go 后端

```bash
cd server
go mod download          # 第一次需要
go run ./cmd/server
```

启动后：

```
巡检问题服务已启动
  监听地址: http://127.0.0.1:8080
  数据库文件: /绝对路径/server/data/inspection.db
  健康检查: http://127.0.0.1:8080/api/health
```

数据库文件会自动创建在 `server/data/inspection.db`，重启不丢数据。

可选参数：

```bash
go run ./cmd/server -addr :9090 -db /tmp/my.db
```

### 终端 B：React 管理端

```bash
cd web
npm install
npm run dev
```

打开 <http://localhost:5173>。开发服务器已经把 `/api` 代理到 `127.0.0.1:8080`，所以浏览器不会报跨域。

### 终端 C：Unity 客户端

**第 1 步：装 Unity（这一步只能你来做）**

我没法替你装——要 Unity 账号、许可激活、Hub 图形界面。

1. 下载 [Unity Hub](https://unity.com/download)（macOS 版）。
2. Hub → Install → 选 **Unity 2022.3 LTS**（Unity 6 也能开，工程会自动升级）。
3. 等下载完（几个 GB）。

> 本工程的 `ProjectSettings/ProjectVersion.txt` 写的是 `2022.3.20f1`。
> 你的版本比它新 → Unity 自动升级工程，正常。
> 比它旧 → Unity 会提示「工程由更高版本创建」，先升一下你的 Unity。

**第 2 步：打开工程**

Hub → Open → 选 `unity/InspectionDemo` 目录 → 选 2022.3 那个 Editor 版本打开。

第一次打开会做一次导入（Library 文件夹会很大，第一次可能要几分钟）。

**第 3 步：确认输入系统设置（重要）**

菜单 **Edit → Project Settings → Player → Other Settings → Active Input Handling**，
必须选 **「Input Manager (Old)」** 或 **「Both」**。

> 如果选的是纯「Input System Package (New)」，Unity 会报
> `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`，
> 本工程用的是旧输入 API（`Input.GetKey` / `Input.GetMouseButton`）。
> 选「Both」最保险。

**第 4 步：跑自检（推荐）**

菜单 **Tools → 巡检 Demo → 一键自检**。

它会一次性检查并打印报告：

| 检查项 | 判据 |
|---|---|
| 场景 | 当前场景是 Main，且根物体里挂了 `SceneBootstrap` |
| 运行时场景 | Play 模式下 Ground / Player / Camera / Canvas / EventSystem 都在 |
| InspectionManager / IssueApiClient | 单例已初始化、状态正常 |
| 中文字体 | 字体存在**且含中文字形**（这一步能提前发现「界面全是方块」） |
| 数据契约 | 用一段真实 JSON 反序列化，验证字段名和 Go 完全对得上 |
| 后端连通 | 实际请求 `/api/health` |

编辑模式跑一次、按 Play 后再跑一次（运行时那几项只有 Play 下才有意义）。

**万一 Main.unity 打不开**

菜单 **Tools → 巡检 Demo → 重建场景** —— 会重新生成一个只含 `SceneBootstrap` 的空场景并覆盖保存。
这是手写场景 YAML 万一有问题时的退路。

**第 5 步：按 Play**

场景里所有东西都是运行时生成的，**不用拖任何 Prefab、不用摆任何模型**。

**操作方式**

| 操作 | 按键 |
|---|---|
| 移动 | `W` `A` `S` `D`（或方向键） |
| 加速 | 按住 `Shift` |
| 转动视角 | 按住**右键**拖动鼠标 |
| 放置问题标记 | **左键**点击场景中的设备 |
| 取消标记 | 在表单里点「取消」 |

左下角 HUD 可以改后端地址（比如改成局域网 IP `http://192.168.1.102:8080`），点「连接」测一次连通性；「测试上报（固定数据）」按钮会绕过表单直接发一条固定数据，用来快速验证链路。

**第 6 步：按这 7 条验一遍**

| # | 做什么 | 应该看到 |
|---|---|---|
| 1 | 进入 Play | 灰蓝色地面、四面墙、4 台带中文标签的设备（机柜 A / 机柜 B / 配电箱 / 管道井） |
| 2 | 按 WASD、右键拖动 | 能走能转 |
| 3 | 左键点「机柜 A」 | 命中点冒出红色球体标记 + 细立杆，屏幕中间弹出表单，表单上显示「位置: x … / y … / z …」 |
| 4 | 点右上角「刷新」或表单上的「取消」 | **场景里不会多出标记**（这条是 UI 拦截，如果这里多出标记就是 bug） |
| 5 | 表单里标题留空，点「提交」 | 提示「标题不能为空」，不会发请求 |
| 6 | 填标题「3 号机柜温度异常」，选优先级「高」，点「提交」 | HUD 显示「上报成功：3 号机柜温度异常」，表单关闭，**标记消失** |
| 7 | 切到浏览器 <http://localhost:5173> 点「刷新」 | 列表里出现这条，优先级「高」，坐标和表单里显示的一致 |

出任何问题，把 **Console 里的红色报错原文**贴给我，我直接改。

---

## 2. 目录结构

```
3DUnity/
├── server/                        Go 后端
│   ├── cmd/server/main.go         入口：flag 解析、优雅退出
│   └── internal/
│       ├── handler/               HTTP 层：路由 + 参数校验 + 统一错误体
│       │   ├── handler.go
│       │   └── issue.go
│       ├── model/issue.go         ★ 数据契约（三端共用的字段定义）
│       ├── store/                 持久化层
│       │   ├── sqlite.go
│       │   └── uuid.go
│       └── middleware/            CORS / 日志 / panic 恢复
│   ├── data/inspection.db         SQLite 数据文件（不进版本库）
│   └── go.mod
│
├── web/                           React 管理端
│   ├── src/
│   │   ├── api/client.ts          fetch 封装 + 统一错误提示
│   │   ├── components/            ErrorBanner / IssueTable / StatusSelect / PriorityBadge
│   │   ├── types/index.ts         ★ 数据契约（TS 版）
│   │   ├── App.tsx                页面主体
│   │   └── styles.css
│   ├── vite.config.ts             含 /api 开发代理
│   └── package.json
│
├── unity/InspectionDemo/          Unity 客户端
│   ├── Assets/
│   │   ├── Scenes/Main.unity      场景里只有一个挂了 SceneBootstrap 的空物体
│   │   ├── Resources/             兜底材质（保证打包时 Standard 着色器不被剔除）
│   │   ├── Editor/                Editor 工具：一键自检 / 重建场景（不进打包产物）
│   │   └── Scripts/
│   │       ├── Core/              GameConfig / FontProvider / UIFactory / MaterialFactory
│   │       │                      SceneBootstrap / InspectionManager
│   │       ├── Data/IssueDto.cs   ★ 数据契约（C# 版）
│   │       ├── Interaction/       RaycastClickHandler / IssueMarker
│   │       ├── Network/           IssueApiClient
│   │       ├── Player/            PlayerController
│   │       └── UI/                IssueFormUI
│   ├── Packages/manifest.json
│   └── ProjectSettings/
│
├── scripts/gen_unity_meta.py      生成 Unity .meta（GUID 用路径 md5，可重复执行）
├── tools/csharp-verify/           C# 编译检查（手写 Unity API 桩 + Roslyn）
│   ├── UnityStubs.cs              覆盖本项目用到的 Unity API 签名
│   ├── Verify.csproj
│   └── verify.sh
├── README.md
├── AI_LOG.md
└── .gitignore
```

> Unity 端还有一组 Editor 工具在 `unity/InspectionDemo/Assets/Editor/InspectionDemoTools.cs`：
> **Tools → 巡检 Demo → 一键自检** / **重建场景**。详见第 1 节第 4 步。

---

## 3. 数据契约（三端共用，改一处必须改三处）

Go 定义在 `server/internal/model/issue.go`，Unity 在 `Assets/Scripts/Data/IssueDto.cs`，React 在 `web/src/types/index.ts`。

```json
{
  "id": "93b6b04f-cf86-44c0-aab1-3c6f66ec32ff",
  "title": "3 号机柜温度异常",
  "description": "机柜后侧温度明显偏高",
  "priority": "high",
  "status": "open",
  "position": { "x": -4.6, "y": 2.2, "z": 1.6 },
  "createdAt": "2026-10-29T07:45:11Z",
  "updatedAt": "2026-10-29T07:45:11Z"
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `id` | string | **后端生成**的 UUID v4 |
| `title` | string | 必填，后端会 trim 后判空 |
| `description` | string | 可空，默认 `""` |
| `priority` | string | `low` / `medium` / `high`，不填默认 `medium` |
| `status` | string | `open` / `in_progress` / `resolved`，新建固定 `open` |
| `position` | object | `{x,y,z}`，Unity 场景世界坐标 |
| `createdAt` / `updatedAt` | string | **后端生成**，UTC RFC3339 |

> `id` 和时间戳一律由后端生成，客户端不参与，避免三端时钟不一致。

### SQLite 表结构

```sql
CREATE TABLE IF NOT EXISTS issues (
    id          TEXT PRIMARY KEY,
    title       TEXT NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    priority    TEXT NOT NULL,
    status      TEXT NOT NULL DEFAULT 'open',
    pos_x       REAL NOT NULL,
    pos_y       REAL NOT NULL,
    pos_z       REAL NOT NULL,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);
```

坐标拆成三列存（而不是一个 JSON 字符串），方便以后直接按坐标做空间查询。

---

## 4. API

统一挂在 `/api` 下，请求和响应都是 JSON。

### `GET /api/health`

```json
{ "status": "ok", "time": "2026-10-29T07:45:11Z" }
```

### `GET /api/issues`

返回全部问题，按创建时间倒序。空库返回 `[]`（不是 `null`）。

### `POST /api/issues`

```bash
curl -X POST http://127.0.0.1:8080/api/issues \
  -H 'Content-Type: application/json' \
  -d '{"title":"3 号机柜温度异常","description":"机柜后侧温度明显偏高","priority":"high","position":{"x":1.25,"y":0.8,"z":2.4}}'
```

`201 Created`，响应体是完整的 Issue 对象。

失败 `400`：

```json
{ "error": { "code": "VALIDATION_ERROR", "message": "title is required" } }
```

### `PATCH /api/issues/{id}/status`

```bash
curl -X PATCH http://127.0.0.1:8080/api/issues/<id>/status \
  -H 'Content-Type: application/json' \
  -d '{"status":"resolved"}'
```

`200 OK`，响应体是更新后的完整 Issue 对象。只接受 `open` / `in_progress` / `resolved`。

### 错误码

| code | HTTP | 含义 |
|---|---|---|
| `VALIDATION_ERROR` | 400 | 参数不合法（空标题、非法优先级/状态） |
| `BAD_REQUEST` | 400 | 请求体不是合法 JSON |
| `NOT_FOUND` | 404 | 问题 id 不存在 / 路由不存在 |
| `METHOD_NOT_ALLOWED` | 405 | 方法不对 |
| `INTERNAL_ERROR` | 500 | 服务端异常 |

所有错误都是同一个形状，前端只需要认一种结构：

```json
{ "error": { "code": "...", "message": "..." } }
```

### CORS

`middleware/cors.go` 回显 `Origin`，放行 `GET/POST/PATCH/PUT/DELETE/OPTIONS` 和 `Content-Type`，`OPTIONS` 预检直接 `204` 返回。

开发期 Vite 代理已经绕过了跨域，但 CORS 还是必须写——前端不一定要跑在 Vite 上。

---

## 5. Unity 端设计要点

### 状态机

```
Idle ──点击场景──> MarkerPlaced ──打开表单──> FormOpen
                                              │
                          提交 ───────────────┤
                                              ▼
                                        Submitting
                                          │        │
                                     成功 ─┘        └─ 失败
                                       ▼              ▼
                                    Success         Error
```

实现在 `InspectionManager.cs`。

### 交互的三道关卡（`RaycastClickHandler.cs`）

1. **UI 拦截**：`EventSystem.current.IsPointerOverGameObject()` 为真就直接 return。
   没有这条，点「提交」按钮会在场景里也放一个标记——这是这类 Demo 最常见的 bug。
2. **状态检查**：表单开着 / 提交中不再放新标记。
3. **射线检测**：`Physics.Raycast` 必须真的打到东西。

### 临时标记的生命周期

| 场景 | 标记 |
|---|---|
| 点「取消」 | 销毁（问题没提交，不该留在场景里） |
| 提交成功 | 销毁（后端已经有记录了） |
| 提交失败 | **保留**，表单不关，用户改完可以直接重试；点取消才清掉 |

全部收在 `InspectionManager` 里，不会漏。

### 为什么不拖 Prefab

`Main.unity` 里只有一个空物体挂着 `SceneBootstrap`，地面、墙壁、四个设备、灯光、玩家、相机、整套 UI 全部在 `Awake()` 里用代码生成。

好处：拿到工程打开就能跑，不会因为「某个 Prefab 没提交」而白屏。代价是场景文件本身很空。

### 两个刻意的技术选择

- **移动用 `Input.GetKey` 而不是 `Input.GetAxis`**：不依赖 `InputManager.asset` 里的轴配置，工程里少提交一个文件也不会走不动。
- **转向用鼠标位置差（右键拖动）**：光标不锁定也能用，也不需要 `Mouse X/Y` 轴。

### 中文字体

Unity 内置的 `LegacyRuntime.ttf` 没有 CJK 字形，直接渲染中文会变成方块。
`FontProvider.cs` 会从系统字体里挑一个带中文的（macOS: `PingFang SC`，Windows: `微软雅黑`），找不到再退回内置字体。

---

## 6. C# 编译检查（本机没装 Unity 时的替代验证）

```bash
bash tools/csharp-verify/verify.sh
```

原理：`tools/csharp-verify/UnityStubs.cs` 里手写了一份覆盖本项目用到的 Unity API 桩，
用 .NET SDK 的 Roslyn 对 `Assets/Scripts` 下全部 C# 做一次真正的编译。

**它能抓什么**：拼写错误、方法签名对不上、类型不兼容、漏声明的字段、未定义的名字。
**它抓不到什么**：Unity 真实 API 是否存在、运行时行为、渲染效果。

这个工具在上线前抓到了两个会让 Unity 直接编译失败的 bug：

| Bug | 症状 |
|---|---|
| `CameraClearFlags.SolidColor` | 真实枚举里没有这一项，只有 `Skybox` / `Color` / `Depth` / `Nothing` |
| `SceneBootstrap` 里用了 `formUI` 但没声明字段 | 编译期直接报「当前上下文中不存在名称 formUI」 |

> 依赖 .NET SDK 8+。没装的话：
> `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet"`

---

## 7. 验收清单

状态说明：✅ = **已实测通过**；🟡 = 代码已写且编译通过，但**没在 Unity 里跑过**（本机没装 Unity）。

| # | 验收项 | 状态 | 怎么验 / 怎么查 |
|---|---|---|---|
| 1 | Unity 有地面、墙壁、≥2 个可点击对象 | 🟡 | `SceneBootstrap.BuildEnvironment()`：地面 + 4 面墙 + 4 台带标签的设备 |
| 2 | 可以移动和转动视角 | 🟡 | `PlayerController`：WASD 移动、Shift 加速、右键拖动转向 |
| 3 | 点击场景对象在命中位置生成标记 | 🟡 | `RaycastClickHandler` → `Physics.Raycast` → `IssueMarker.Create(hit.point)` |
| 4 | 点击 UI 不会误生成标记 | 🟡 | `IsPointerOverGameObject()` 在射线检测**之前** return |
| 5 | 表单标题必填，优先级可选 | 🟡 | `IssueFormUI.HandleSubmitClicked()` 判空 + 高亮提示；优先级 3 个按钮，默认「中」 |
| 6 | 提交成功/失败有提示 | 🟡 | HUD toast + 表单内 hint，失败文案取后端返回的 `error.message` |
| 7 | 取消表单后删除未保存标记 | 🟡 | `InspectionManager.Cancel()` → `ClearCurrentMarker()` → `Destroy` |
| 8 | Go 健康检查 | ✅ | `curl /api/health` → `{"status":"ok"}` |
| 9 | Go 创建问题 | ✅ | `POST /api/issues` → 201 + 完整 Issue + UUID v4 |
| 10 | Go 查询列表 | ✅ | `GET /api/issues` → 数组，按时间倒序 |
| 11 | Go 修改状态 | ✅ | `PATCH /api/issues/{id}/status` → 200 + 更新后的对象 |
| 12 | 标题为空返回 400 | ✅ | `{"error":{"code":"VALIDATION_ERROR","message":"title is required"}}` |
| 13 | 持久化，重启不丢 | ✅ | 重启 Go 服务再 `GET /api/issues`，数据还在 |
| 14 | 支持 CORS | ✅ | `OPTIONS` 预检 → 204 + `Access-Control-Allow-Origin` |
| 15 | React 拉取列表，显示标题/优先级/状态 | ✅ | 浏览器实测，4 条数据全部字段正确 |
| 16 | React 可切换 open/in_progress/resolved | ✅ | 下拉框改状态 → UI 立即更新 → `curl` 确认后端也是新值 |
| 17 | React 后端不可用时显示错误而不是白屏 | ✅ | 关掉 Go 服务刷新 → 红色错误条 +「重试」，页面结构完整 |

Unity 那 7 项虽然标 🟡，但**全部通过 Roslyn 编译检查**（`tools/csharp-verify/verify.sh`，0 error 0 warning），
其中 2 个会让 Unity 编译失败的 bug 已经被这个检查抓出来修掉了。
剩下的只有「实际跑起来是什么样」需要你在 Editor 里看一眼。

---

## 8. 常见问题

**Unity 里中文全是方块**
`FontProvider` 没找到系统 CJK 字体。在 `FontProvider.cs` 的 `Candidates` 里加一个你系统上真实存在的字体名。

**Unity 报「后端未连接」**
- Go 服务没起，或者端口不是 8080；
- 在 HUD 里改后端地址后要记得点「连接」。

**Unity 打包后连不上 http://127.0.0.1**
macOS 打包的 ATS 策略会拦明文 HTTP。Editor 里正常，打包可能失败。解决：把地址换成 `https`，或给打包的 plist 加 `NSAllowsLocalNetworking`。

**React 报跨域 / 拿不到数据**
- 开发期：`npm run dev` 用代理，确认 Go 在 8080；
- 用构建产物：`VITE_API_BASE=http://127.0.0.1:8080 npm run build`，构建产物里没有代理。

**端口被占用**

```bash
lsof -nP -iTCP:8080 -sTCP:LISTEN
go run ./cmd/server -addr :9090
```

**想清空数据重来**

```bash
rm -f server/data/inspection.db*
```

---

## 9. 待补充

原计划里的「三个 AR 基础问题」没有给出具体题目，留在这里等你补充：

1. ？
2. ？
3. ？

演示视频录制也还没做（需要你在 Unity Editor 里手动录 Game view，本机没装 Unity）。

---

## 10. 技术选型

| 部分 | 方案 | 理由 |
|---|---|---|
| Unity | Unity 2022.3 LTS，C# | 内置 3D 渲染够用，不需要付费资源 |
| React | Vite + React 18 + TypeScript | 启动快、类型安全、单页够用 |
| Go | Go 1.22+，标准 `net/http` | 接口就 4 个，上框架反而是负担 |
| 数据库 | SQLite（`modernc.org/sqlite`，纯 Go） | 免 cgo，`go build` 不用装 Xcode CLT |
| 通信 | REST + JSON | 三端都原生支持 |
| ID | 后端生成 UUID v4 | 客户端不参与，唯一性由服务端保证 |
| 时间 | 后端生成 RFC3339 | 避免客户端时钟不一致 |
