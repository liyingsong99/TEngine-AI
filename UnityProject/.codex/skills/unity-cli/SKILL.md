---
name: unity-cli
description: 使用本项目 AIBridge 查询和操作 Unity Editor/Player，验证资源、编译、测试、运行与截图。纯文本定位无需连接 Editor；不使用 Unity MCP。
---

# Unity 与 AIBridge

入口为 Unity 项目内 `.aibridge/cli/AIBridgeCLI.exe`。本项目不使用 Python 工作流，不自动安装依赖。先读 Root Rule、当前项目状态和本地 help；不要把可选 Pipeline backend 的命令/参数当成 AIBridge native 接口。

## 起步

从 UnityProject 执行：

```powershell
$CLI = './.aibridge/cli/AIBridgeCLI.exe'
& $CLI --help
& $CLI editor get_state
```

确认本项目唯一 Editor、版本及允许的状态；可连接不等于空闲。文档任务仅查 help/源码，不为格式检查操作 Editor。缺失连接时保留静态工作并报告运行验证阻塞，不借用其他工程。

## 按任务读取

|任务|规程|
|---|---|
|连接、help、参数、请求与失败恢复|[操作规程](references/operations.md)|
|编译、测试与证据|[验证规程](references/verification.md)|
|对象、组件、资源与 Prefab|[内容操作](references/authoring.md)|
|截图、Profiler、Player、脚本|[诊断与运行时](references/diagnostics.md)|
|native/可选 backend 差异及升级核对|[工具契约](references/pipeline-contract.md)|

## 不变量

- 先查 `<command> --help` 和 `<command> <action> --help`，按返回的参数名/大小写调用，不猜别名。
- 原始源码通过编辑工具修改；Unity 对象与序列化使用 Unity API，不手写 Scene/Prefab YAML 或 meta。
- 编译、导入、测试、Play 与写入串行；团队交接 Editor 后不并行启动操作。
- 写入先明确目标、范围和覆盖影响，沿用既有有效授权；删除、项目设置、包依赖、平台切换及真实构建单独确认。
- 一次性 Editor C# 使用 `.aibridge/code/*.csx` 或 `.cs`，`code execute` 串行；超时先 `code status`，不重放或换 backend。
- 缺命令、依赖、匹配用例、终态或目标身份时报告阻塞，不自动升级和伪造成功。
- 报告实际命令、目标、执行数量、终态及产物；静态、编译、运行态分开。`success=true` 不能覆盖内层错误。
