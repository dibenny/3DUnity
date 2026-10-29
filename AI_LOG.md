# AI_LOG.md

这份文档记录这个项目**实际是怎么做出来的**：哪些是 AI 写的、关键决策是什么、哪些验证过、哪些没验证。

目的不是好看，是让你在答辩或者自己复盘的时候，能把每一块讲清楚，而不是只能说「AI 写的」。

---

## 0. 一句话总结

从 0 搭一个三端闭环项目，核心原则是**先定数据契约，再打通端到端最小闭环，最后补交互和健壮性**。

整个项目 13 个 Unity C# 文件（12 个运行时代码 + 1 个 Editor 工具）、7 个 Go 文件、8 个前端文件，全部由 AI 生成，**但每个文件都经过人工审查和真实验证**（除 Unity 运行时部分，原因见第 6 节）。

---

## 1. 开发顺序（按这个顺序做的，不是按模块做）

| 阶段 | 做了什么 | 验证方式 |
|---|---|---|
| 一、工具链 | 装 Go 1.27.3（Homebrew）、确认 Node 22 | `go version` / `node -v` |
| 二、数据契约 | 先写 `model/issue.go`，定死字段名和 JSON tag | — |
| 三、Go 服务 | model / store / middleware / handler 四层 | `go build` + `go vet` + `gofmt` |
| 四、Go 验收 | 13 项 curl 测试 | 全部通过（见第 5 节） |
| 五、React | api client / 表格 / 错误态 | `tsc --noEmit` + `vite build` + 浏览器实测 |
| 六、端到端 | Vite 代理 → Go → React | 浏览器实测截图 |
| 七、Unity | 脚本 + 场景 + 工程配置 | Roslyn 编译检查 + 人工审查（本机没装 Unity） |
| 八、文档 | README / AI_LOG | — |

**为什么先做 Go 不先做 Unity**：Unity 那一端最难验证（要开 Editor），把最容易验证的部分先做掉能尽早发现契约设计错误。如果先写 Unity，等三端都写完才发现字段名对不上，改动量是三倍。

---

## 2. 关键决策与理由

### 2.1 SQLite 用 `modernc.org/sqlite` 而不是 `mattn/go-sqlite3`

`mattn/go-sqlite3` 是 cgo 实现，需要本机装 Xcode Command Line Tools 才能编译。`modernc.org/sqlite` 是纯 Go 实现，`go build` 开箱即用。

**踩到的坑**：一开始 `go get modernc.org/sqlite@latest` 拉到了 v1.61.0，它的 `go.mod` 要求 `go 1.26.0`，直接把整个项目的 go 指令抬上去了——这跟 README 里承诺的「Go 1.22+」矛盾。

处理办法：查 `go list -m -f '{{.GoVersion}}' all` 找出是哪个传递依赖抬高的，然后把 `modernc.org/libc`、`golang.org/x/sys`、`modernc.org/memory` 等全部钉回 `sqlite v1.34.5` 原本要求的版本，最后手动把 `go.mod` 的 `go 1.26.0` 改回 `go 1.22` 再 `go mod tidy` 确认不会被顶回去。

**教训**：`go get` 只会升不会降，依赖升级要连带传递依赖一起看。

### 2.2 Unity 端全部运行时生成，不拖 Prefab

`Main.unity` 场景文件里只有一个空物体挂着 `SceneBootstrap`，地面、墙壁、四个设备、灯光、玩家、相机、整套 uGUI 界面都在 `Awake()` 里用代码搭。

- **好处**：拿到工程打开直接 Play，不会因为「某个 Prefab 没提交」白屏。对一个要被检查的项目，这个可靠性比「场景里东西多」重要得多。
- **代价**：场景文件本身很空，看不出「做了什么」。所以代码里的注释写得比较细。
- **配套**：`UIFactory.cs` 提供 `CreatePanel / CreateText / CreateButton / CreateInput / Stretch / Place` 六个小工具，所有界面都靠它们拼，避免每个面板重复写一遍 RectTransform 赋值。

### 2.3 Unity 场景文件和 .meta 用脚本生成

手写 `.unity`（YAML）很容易出错，尤其是脚本引用靠 GUID 匹配。

做法：
1. 写一个 Python 脚本 `scripts/gen_unity_meta.py`，GUID 用「相对路径的 md5」生成——确定性、不会撞、重复执行不会改已有文件；
2. 先跑脚本拿到 `SceneBootstrap.cs` 的 GUID；
3. 再手写 `Main.unity`，把那个 GUID 填进 `m_Script`；
4. 再跑一次脚本生成 `Main.unity.meta`；
5. 最后用场景 GUID 写 `EditorBuildSettings.asset`。

脚本保留在 `scripts/` 里，以后新增资产再跑一次就行。

### 2.4 UI 点击拦截放在最前面

`RaycastClickHandler.Update()` 的三道关卡顺序是**不能换**的：

```csharp
if (EventSystem.current.IsPointerOverGameObject()) return;   // ① UI 上就不放标记
if (manager.State != InspectionState.Idle) return;            // ② 表单开着不放
if (!Physics.Raycast(...)) return;                            // ③ 真打到了才放
```

第 ① 条漏了会怎样：在表单上点「提交」按钮，Unity 收到鼠标点击 → 射线打中场景里的地板 → 在那里多放一个标记。用户以为提交了一条问题，实际场景里多一个没保存的标记。**这是这类 Demo 最常见的 bug**，所以写在了代码注释最显眼的位置。

### 2.5 提交失败时保留标记和表单

`InspectionManager` 里三种结局：

| 场景 | 标记 | 表单 |
|---|---|---|
| 取消 | 销毁 | 关闭 |
| 提交成功 | 销毁 | 关闭 |
| 提交失败 | **保留** | **不关**，显示错误 |

失败保留是因为用户可能只是把标题打错了，直接关掉表单会让他重新点一遍设备、重新填。标记保留 + 表单不关 = 原地重试。

### 2.6 移动不用 `Input.GetAxis`

`PlayerController` 用 `Input.GetKey(KeyCode.W)` 而不是 `Input.GetAxis("Horizontal")`。

原因：`GetAxis` 依赖 `ProjectSettings/InputManager.asset` 里的轴配置。这个工程刻意没提交那个文件（Unity 会在首次打开时自动生成一份默认值），用 `GetKey` 就完全不依赖它。

同理，转向用「鼠标位置差（按住右键拖动）」而不是 `Input.GetAxis("Mouse X")`，也不依赖轴。

### 2.7 中文显示：自己找系统字体

Unity 内置的 `LegacyRuntime.ttf`（旧版本叫 `Arial.ttf`）只有拉丁字形，中文会渲染成方块。

`FontProvider.Get()`：
1. `Font.CreateDynamicFontFromOSFont(Candidates, 32)`，候选列表按 macOS → Windows → Linux 排（`PingFang SC` / `微软雅黑` / `Noto Sans CJK SC` …）；
2. 拿不到就退回 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`，旧版 Unity 再退 `Arial.ttf`（两个名字都要试，因为 2022.2 之后改名了，名字不对会抛异常）；
3. 全都没有就 `Debug.LogError` 明确报错，不静默失败。

### 2.8 Go 端不用 Go 1.22 的方法路由模式

`http.ServeMux` 在 1.22 支持 `"GET /api/issues"` 这种带方法前缀的 pattern，但那样 404/405 会返回 Go 默认的纯文本响应，跟项目统一的 JSON 错误体对不上。

所以用的是「按路径注册 + handler 内部判断 method」：

```go
mux.HandleFunc("/api/issues", h.handleIssues)   // 内部 switch GET / POST
mux.HandleFunc("/api/issues/", h.handleIssueSubPath)
mux.HandleFunc("/", h.handleFallback)            // 兜底 404，也返回 JSON
```

代价是代码多几行，收益是**所有**错误响应形状一致，前端只认一种结构。

### 2.9 SQLite 连接数限制为 1

```go
db.SetMaxOpenConns(1)
```

SQLite 是单写入者模型，并发写会直接 `database is locked`。配合 DSN 里的 `_pragma=busy_timeout(5000)` 和 `journal_mode(WAL)`，读不阻塞写，联调时 React 刷新不会卡住 Unity 的写入。

### 2.10 前端「后端不可用」的判定

这里有个坑：**通过 Vite 代理访问时，后端挂掉并不是网络错误，而是代理返回 HTTP 500 + 一个 HTML 错误页**。

如果只在 `catch` 里处理网络异常，页面会显示「请求失败（HTTP 500）」而不是人话。所以 `client.ts` 里多判了一层：

```ts
if (!parsedAsJson) {
  // 响应不是 JSON —— Vite 代理连不上 Go 时返回的就是这种错误页
  throw new ApiError('无法连接后端服务，请确认 Go 服务是否启动。', ...)
}
```

---

## 3. 三端数据契约怎么保持一致

同一份字段在三处各写了一遍：

- Go：`server/internal/model/issue.go`
- C#：`unity/InspectionDemo/Assets/Scripts/Data/IssueDto.cs`
- TS：`web/src/types/index.ts`

**约束手段**：
- Go 用 struct tag 固定 JSON 名（`json:"position"` 而不是 `json:"Position"`）；
- C# 用 `JsonUtility`，字段名必须和 JSON 完全一致（大小写敏感），所以 C# 里直接写成 `public string createdAt;` 这种 camelCase 命名，而不是 C# 惯例的 PascalCase；
- TS 用 `interface` + 字面量联合类型 `Priority` / `IssueStatus`，写错值编译期就报错。

C# 这块是最容易翻车的：写 `public string CreatedAt;` 的话 `JsonUtility` 反序列化会**静默丢掉**这个字段（不报错，就是 null），时间戳就没了。所以 C# 的 DTO 字段名故意不遵循 C# 命名惯例。

---

## 4. 我实际验证了什么

### Go 端（13 项，全过）

| # | 测试 | 结果 |
|---|---|---|
| 1 | `GET /api/health` | 200 `{"status":"ok"}` + CORS 头 |
| 2 | `POST /api/issues` | 201，返回完整 Issue，UUID v4 |
| 3 | `GET /api/issues` | 200，返回数组 |
| 4 | 空标题 | 400 `{"error":{"code":"VALIDATION_ERROR","message":"title is required"}}` |
| 5 | 非法优先级 | 400 `priority must be one of low\|medium\|high` |
| 6 | `PATCH .../status` | 200，返回更新后的完整对象 |
| 7 | 非法状态 | 400 `status must be one of open\|in_progress\|resolved` |
| 8 | 不存在的 id | 404 `issue not found` |
| 9 | 未知路由 | 404，JSON 格式 |
| 10 | CORS 预检 | 204 + `Access-Control-Allow-Origin: http://localhost:5173` |
| 11 | 优先级不填 | 默认 `medium`，状态默认 `open` |
| 12 | **重启服务后查列表** | 数据全在（SQLite 持久化生效） |
| 13 | `go vet` / `gofmt -l` | 干净 |

### React 端（浏览器实测）

| # | 场景 | 结果 |
|---|---|---|
| 1 | 打开页面 | 4 条数据，标题/优先级徽章/状态/坐标/时间/下拉框全部渲染正确 |
| 2 | 下拉改成「已解决」 | UI 立刻变绿，顶部统计从「待处理 3 / 已解决 0」变成「待处理 2 / 已解决 1」 |
| 3 | 后端确认 | `curl /api/issues` 里那条确实变成 `resolved`（乐观更新 + 后端落库都确认了） |
| 4 | **关掉 Go 服务后刷新** | 顶部红色错误条「无法连接后端服务，请确认 Go 服务是否启动。」+「重试」按钮，**页面没有白屏**，空状态文案照常显示 |
| 5 | 重启 Go 后点「重试」 | 列表恢复 |

### 静态检查

| 工具 | 结果 |
|---|---|
| `go build ./...` | 通过 |
| `go vet ./...` | 无告警 |
| `gofmt -l .` | 无输出 |
| `tsc --noEmit` | 无报错 |
| `vite build` | 成功，148 KB（gzip 48 KB） |
| `tools/csharp-verify/verify.sh` | **13 个 C# 文件，0 error 0 warning**（warning 也当 error） |

---

## 5. 验收清单对照

见 README 第 6 节。17 项里 **16 项已实测通过**，剩下 1 项（Unity 端交互）没法在本机验证，原因见下一节。

---

## 6. 没验证的部分（重要，别当成已交付）

### Unity 运行时完全没跑过

**本机没装 Unity**（`/Applications/Unity/Hub/Editor` 不存在），所以下面这些只是静态推理，不是实测：

- 场景里的地面/墙壁/设备是否真的渲染出来；
- 中文标签（`TextMesh` + 系统动态字体）是否正常显示；
- `EventSystem.current.IsPointerOverGameObject()` 的 UI 拦截是否真的生效；
- 射线点击是否真的命中设备；
- 标记的脉动动画、表单弹出/关闭动画；
- `CharacterController` 移动和右键拖动转向的手感。

### 但做了一个替代验证：C# 编译检查

装不了 Unity（`brew install mono` 需要完整 Xcode，失败），改用 .NET SDK 8 的 Roslyn：

`tools/csharp-verify/` 里手写了一份覆盖本项目用到的 Unity API 桩（`UnityStubs.cs`，约 600 行），
然后用 `dotnet build` 编译「桩 + Assets/Scripts 全部 C#」，并把 **warning 也当 error**。

```bash
bash tools/csharp-verify/verify.sh
# ✅ 编译通过：13 个 C# 文件，0 error 0 warning
```

**这个检查抓到了两个会让 Unity 直接编译失败的 bug：**

| Bug | 编译器报错 | 真实原因 |
|---|---|---|
| `camera.clearFlags = CameraClearFlags.SolidColor;` | `CS0117: “CameraClearFlags”未包含“SolidColor”的定义` | 真实枚举只有 `Skybox` / `Color` / `Depth` / `Nothing`，**没有 SolidColor** |
| `SceneBootstrap.BuildUI()` 里用了 `formUI` | `CS0103: 当前上下文中不存在名称“formUI”` | 这个字段根本没声明，`formUI = ...` 直接编译不过 |

这两个如果留到 Unity 里才炸，就是「打开工程一片红」，很伤。

**它抓不到什么**：Unity 真实 API 是否存在（比如桩里写了个 Unity 2022 其实没有的成员，这里就查不出来）、
运行时行为、渲染效果。所以它只是「代码自身没有写错」的保证，不等于「能跑」。

### 另外做的静态保证

- 场景 YAML 里 `m_Script` 的 GUID 与 `SceneBootstrap.cs.meta` 的 GUID 做了程序化比对，一致；
- `EditorBuildSettings.asset` 里的场景 GUID 与 `Main.unity.meta` 一致；
- 所有的 `AddComponent` 顺序检查过（`InspectionManager.Awake` → `Instance` 赋值 → `RaycastClickHandler` 才可能用到）；
- 事件绑定（`OnSubmitRequested` / `OnCancelRequested`）检查过不会空指针。

### 演示视频没录

录制需要在 Unity Editor 里手动录 Game view，本机没有 Unity。README 第 9 节留了位置。

### 为此补了两个 Editor 工具（`Assets/Editor/InspectionDemoTools.cs`）

既然没法自己跑，就把「要盯的点」变成机器可查的：

- **Tools → 巡检 Demo → 一键自检**
  自动检查并打印报告：场景是否就绪、运行时层级（Ground/Player/Camera/Canvas/EventSystem）齐不齐、
  两个单例有没有初始化、**字体含不含中文字形**（提前发现「界面全是方块」）、
  **数据契约反序列化对不对得上**（提前发现 JsonUtility 字段名大小写写错导致静默丢字段）、
  **后端 /api/health 实际连不连得上**。
- **Tools → 巡检 Demo → 重建场景**
  万一手写的 `Main.unity` YAML 打不开，两秒重新生成一个只含 `SceneBootstrap` 的场景。

这两个工具本身也纳入了 Roslyn 编译检查（合计 13 个 C# 文件，0 error 0 warning）。

---

## 7. 你应该能自己讲清楚的 6 个点

如果被问到「这个项目你做了什么」，下面 6 个是核心，能讲明白就说明真的懂了：

1. **为什么先定数据契约？**
   三端都要用同一套字段名。如果先写各自代码，最后对不上要改三遍。定契约时发现「`id` 和时间戳谁生成」，答案是后端——客户端生成会碰到时钟不一致和多端 ID 冲突。

2. **UI 点击是怎么被拦住的？**
   `EventSystem.current.IsPointerOverGameObject()`。必须放在射线检测**之前**。漏了的话点 UI 也会在场景里放标记。

3. **临时标记什么时候被删？**
   取消时删、提交成功时删、**提交失败时保留**（让用户原地重试）。逻辑集中在 `InspectionManager`，不散落在各回调里。

4. **为什么 SQLite 要限制连接数是 1？**
   SQLite 单写入者模型，并发写会 `database is locked`。配合 WAL 模式，读不阻塞写。

5. **前端怎么知道后端挂了？**
   两种情况：直连时 fetch 抛异常；走 Vite 代理时后端挂了代理返回 500 + HTML 错误页。所以要额外判「响应是不是 JSON」，不是 JSON 就按「后端不可用」处理。

6. **Unity 场景为什么几乎是空的？**
   全部运行时生成。为了「打开就能跑」的可靠性。代价是场景文件看不出内容，所以逻辑都写在 `SceneBootstrap.cs` 里。

---

## 8. 如果要继续做，优先级建议

1. 打开 Unity 跑一遍，把第 6 节列的 4 个点验掉；
2. 录演示视频；
3. 补「三个 AR 基础问题」；
4. 想加分的话：给标记加一个「已上报」和「未上报」的颜色区分；React 加个筛选框；Go 加个 `GET /api/issues?status=open` 查询参数。

---

## 9. 生成工具

- Unity C#、Go、TypeScript/CSS/YAML/Python 全部由 AI 生成；
- 人工介入的地方：
  - 装 Go、跑 `go mod tidy`、做依赖降级；
  - 写并调试 `gen_unity_meta.py`；
  - 逐条跑 13 项 curl 验收；
  - 浏览器里实测 React 的 5 个场景；
  - 审查全部 13 个 C# 文件，并用 Roslyn 编译验证（本机无 Unity，做不了真机运行验证）；
  - 修 3 个自己写出来的 bug：
    - `main.go` 里监听地址被拼了两遍（`http://127.0.0.1127.0.0.1:8080`）；
    - `uuid.go` 引用了不存在的 `randFallback()`；
    - `InspectionManager` 里一个多余的 `payload_title()` 辅助函数（写的时候脑子乱了）。
- 主动复查时发现并修掉的 3 个逻辑缺陷：
  - **玩家没有重力**：原来 `CharacterController.Move` 只给了水平位移，走出平台会直接悬在半空。加了垂直速度积分。
  - **状态卡死**：固定数据测试上报失败后，`State` 停在 `Error` 永不回 `Idle`，而 `RaycastClickHandler` 只在 `Idle` 时才放标记 —— 等于用户再也点不出新问题。改成用 `resetStateLater` 标记，超时自动回 `Idle`；表单打开的场景下才保持 `Error` 让用户原地重试。
  - **Unity 编译期错误**（被 Roslyn 抓到）：`CameraClearFlags.SolidColor` 不存在、`SceneBootstrap` 漏声明 `formUI` 字段。
