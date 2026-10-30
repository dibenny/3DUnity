# AR Foundation XR模拟迁移说明

本文档解释了如何使用AR Foundation和XR Simulation将当前固定的虚拟检查演示迁移到真实的AR版本。桌面版演示故意保持为默认模式，以便在没有AR设备的情况下也能运行。

## 1. 目标架构

分离放置逻辑与其他检查流程：

- `IInspectionPlacementProvider`：一个小型接口，从屏幕点返回世界空间的命中姿态。
- `DesktopRaycastPlacement`：当前使用 `Physics.Raycast` 对虚拟场景进行实现。
- `ARRaycastPlacement`：新的实现方式，使用 `ARRaycastManager` 针对检测到的平面或点云。
- `InspectionManager`：保持不变；它仅接收命中姿态并打开问题表单。
- `IssueMarker`：已经存储了世界位置，在AR模式下还应将其作为AR锚点的子对象。

## 2. AR Foundation组件

使用以下AR Foundation组件：

- `ARSession`：会话生命周期和跟踪状态。
- `XR Origin`：AR摄像机和世界坐标原点。
- `ARPlaneManager` 和 `ARPlane`：平面检测。
- `ARRaycastManager`：屏幕点击到真实世界的命中测试。
- `ARAnchorManager`：在放置姿态处创建锚点。
- `ARPointCloudManager`：可选的基于特征点的命中测试的备用方案。
- `AROcclusionManager`：可选的视觉遮挡，以提供更真实的体验。

现有的 `RaycastClickHandler` 可以被替换为一个针对AR特定的手势处理器，在成功进行AR射线投射后调用相同的 `InspectionManager.PlaceMarker` API。

## 3. 数据模型变更

保留现有字段，并添加可选的元数据用于真实AR：

```json
{
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 },
  "anchorId": "optional-cloud-anchor-id",
  "deviceId": "DEV-01",
  "areaCode": "AREA-01"
}
```

原始的 x/y/z 坐标对于显示和调试很有用，但不足以用于跨会话持久化。存储锚点标识符或相对于持久锚点的姿态，并在必须在重启应用后再次找到同一物理位置的问题时使用云锚点、地理空间锚点、图像标记或二维码标记。

## 4. XR模拟测试流程

1. 安装AR Foundation包并在XR插件管理设置中启用XR Simulation。
2. 创建一个包含地板平面和至少一个墙壁或类似柜子对象的XR Simulation环境。
3. 在激活XR Simulation的情况下进入播放模式。
4. 点击模拟的地板并验证 `ARRaycastManager.Raycast` 是否返回有效的姿态。
5. 在命中姿态处创建AR锚点，放置问题标记，打开表单并提交问题。
6. 验证在重新加载保存的问题后标记是否再次出现，并且点击它能否打开详情面板。
7. 使用XR Simulation控件模拟初始化失败、无平面和跟踪丢失状态；验证UI显示清晰的恢复指令。

## 5. 异常处理要求

AR模式下绝不能让用户停留在空白或永久加载屏幕：

- 显示初始化进度和权限指导。
- 当跟踪无效时禁用放置功能。
- 显示可读的 `notTrackingReason` 并提供重试/重启会话的动作。
- 提供切换到桌面手动检查模式的备用方案。
- 保持当前后端和问题工作流程不变，确保在两种模式下Web管理员页面都能正常运行。

## 6. 迁移清单

- [ ] 添加AR Foundation包和XR Simulation设置。
- [ ] 引入 `IInspectionPlacementProvider` 并将当前的物理射线投射移动到 `DesktopRaycastPlacement` 中。
- [ ] 添加 `ARRaycastPlacement` 并将其绑定到 `ARRaycastManager`。
- [ ] 为标记创建锚点并持久化可选的锚点ID。
- [ ] 添加跟踪/权限/不支持设备UI状态。
- [ ] 使用相同的后端合同和React管理员流程针对AR生成的问题进行测试。
