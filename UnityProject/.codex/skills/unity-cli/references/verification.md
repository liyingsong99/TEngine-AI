# 验证与证据

从 UnityProject 使用项目 AIBridge。根据改动选择检查，不运行已退役的包装流程。

```powershell
$CLI = './.aibridge/cli/AIBridgeCLI.exe'
& $CLI compile unity --timeout 120000
& $CLI get_logs --logType Error
```

## 影响与证据

|改动|证据|
|---|---|
|文档/技能|结构、相对链接、失效入口扫描、git diff --check；不触发 Unity|
|C#|本次 Unity 编译终态和受影响行为；有有效生成解决方案时补 .NET 构建|
|Prefab/Scene/资源|明确目标的组件、引用、加载与实际场景；视觉变更需要截图|
|HTML/配置|实际转换/导出及错误路径，产物检查与运行消费|
|热更/AOT/裁剪|目标平台专项证据；真实构建、设备测试和发布需相应授权|

新增源码需 Unity 导入。旧 csproj 或旧程序集可用不能证明新源码参与编译；不合成解决方案、不借其他工程。编译需检查 success、statusConfirmed、终态及 errors，不能仅看计数；超时/断开记录未知或阻塞。

## Play 与截图

在已批准的运行用例中调用 `editor play`，再 `editor get_state` 确认实际进入。`screenshot game` 返回图像路径，必须读取图像并核对状态与尺寸。结束 `editor stop` 后读状态确认释放 Editor。Error 日志为空只覆盖查询范围，不等于玩法或生命周期通过。

## 测试

每次先看本地 `test run --help`、`test status --help`：

- native 默认 backend；mode 为 EditMode/PlayMode，两者都需从 Edit Mode 发起。
- 原生筛选参数：test-name（完整名）、group-name（正则/夹具/命名空间）、assembly-name。
- `test list --backend pipeline --mode EditMode` 当前只支持可选 Pipeline 的 EditMode 枚举；缺该能力时据源码或测试面板确定集合，不伪造 PlayMode 枚举。
- Pipeline 的 pipeline-filter/filter-type 是另一套筛选参数，不混用原生参数。
- 原生 `test status --run-id <实际ID>`；Pipeline 使用原 requestId/backend。ID 来自本轮响应，不虚构。
- 启动前确认没有活跃测试，保存启动响应、筛选器与用例集合，核对终态、逐项结果、数量。零用例、全跳过、失败、inconclusive 或不匹配结果不能通过。
- 超时不自动重新启动、取消或释放他人操作；先观察原请求。

## 资产与构建

查询 Prefab 层级/组件和场景内容，逐项验证本任务要求。资源存在/importer 可用不证明内部引用、YooAsset 地址、字体字形或布局正确。不假定项目注册了旧验证命令，先核实实现。

`build run --help` 显示本地 native 构建默认 dry-run，只验证当前 BuildTarget，不切平台；scenes 为保存的 Assets 场景 JSON 数组，输出由工具分配新的构建目录。真实构建要求单独授权和 dry-run=false/confirm=true；读取实际 buildId 与 status/report 终态。预检成功不代表真实构建或发布通过，不示范自动真实构建。

报告静态、编译、运行态三类结果，明确限定检查的范围、未运行项与阻塞原因。截图、结构检查、受控脚本与真实输入/设备验收分别标注。
