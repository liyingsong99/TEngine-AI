# 诊断、截图与 Player

## 状态与日志

使用 `editor get_state`、`get_logs --logType Error`；具体级别、数量、正则先查 `get_logs get --help`。保留异常堆栈，不先清 Console、Library 或资源缓存。一次查询不能证明长期稳定或峰值性能。

## 图像证据

```powershell
$CLI = './.aibridge/cli/AIBridgeCLI.exe'
& $CLI screenshot --help
& $CLI screenshot game
```

按本地 help 选择 game/scene_view/editor_window/gif，不照搬其它工具的 output-path/source 参数。记录响应中的实际图像路径、尺寸、时间和 Play 状态，打开图像核对目标窗口与 HUD。工具成功返回不证明 Overlay UI 已显示；离屏相机图不能替代所需的窗口截图。输出缩放也不证明布局在该分辨率真实运行。

捕获可能切换窗口或写文件，按范围执行。不将证据写进 Assets 触发导入；保留工具分配的截图路径或任务临时目录。浏览器、Unity、设备截图分别留证，中文字体、安全区和交互需相应实际验证。

## Profiler 与内存

先查 `profiler --help`、`profiler get_status --help`、`memory --help`。当前 action 包括 profiler start/stop/get_status/capture_frame/save_data，以及 memory capture_snapshot/snapshot_status；具体参数和输出看该 action 的 help。

核对当前连接目标，不能把其他 Player 的样本算成本工程。录制、清历史、切换目标、GC、快照有副作用与成本；明确范围再执行。保留本轮 ID、目标、时间、终态和文件，不能混用不同工具的 captureId/snapshotId 字段。超时先观察，不重复采样覆盖证据。不自动改 Deep Profile、项目设置或后台运行来凑结果。

## Runtime Player

```powershell
& $CLI runtime list_targets --quick true
& $CLI runtime status --help
```

从实际目标列表选取本项目 target ID，再调用 runtime status --target <实际ID>。当前 help 支持 target、runtime-dir、transport、url、projectHint 等；不要套用其它工具的目标选择参数。默认 latest 不能代替身份核对，默认不扫描端口；仅在明确需要时使用 probe。

连接已有 Player 不授权启用 Runtime 插件、改 define 或构建 Player。不打印 token/鉴权描述文件，不把 Editor 测试替代真机结果。

## 一次性代码与热重载

普通查询/操作优先已注册 action。确需 Editor C# 时，审核代码与写入范围，在 `.aibridge/code/*.csx` 执行 `code execute`；超时查 `code status`，不重放、不切 backend。原生默认执行，编译预检只证明语法和引用。

方法热重载、反射调用或运行时解释执行成功不证明完整编译、字段/序列化变更、HybridCLR/AOT 或发布链正确；相应能力需从当前源码和 help 核实，不自动开启 watcher 或修改全局刷新设置。
