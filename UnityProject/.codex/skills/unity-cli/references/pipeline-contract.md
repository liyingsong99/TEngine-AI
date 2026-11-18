# AIBridge 与可选后端契约

文件名保留以避免现有引用断裂；本页当前说明项目使用的 AIBridge，不把历史 Pipeline 配置视为仍已安装。

## 权威顺序

1. 当前用户约束和两级 AGENTS。
2. 本地 CLI help、实际目标响应、已安装源码。
3. 本项目具体 API 实现和资源数据。
4. 文档仅辅助导航，旧版本命令不是兼容保证。

## 当前基线与发现

- Unity 版本见 [ProjectVersion](../../../../ProjectSettings/ProjectVersion.txt)，本项目为 6000.3.28f1，C# 9.0。
- 包来源见 [manifest](../../../../Packages/manifest.json)。AIBridge 从项目本地 `.aibridge/cli/AIBridgeCLI.exe` 调用。
- 本轮未找到旧文档引用的独立 Pipeline 包目录与旧 CLI；不以不存在的默认入口指导执行。
- 不主动升级、安装工具或开启 Code Index。Root Rule 已声明 Code Index disabled。

## 不可混用的契约

|类别|当前 AIBridge|
|---|---|
|Editor 状态|editor get_state|
|当前场景与层级|scene get_active / get_hierarchy|
|编译|compile unity|
|资源搜索|asset find --filter ... 或 asset search|
|字段|inspector get_property，参数按 help|
|截图|screenshot game；返回路径按实际响应读取|
|Editor C#|code execute；文件仅 .aibridge/code 下 .cs/.csx|
|Runtime|runtime list_targets/status，target/runtime-dir 等参数见 help|

code execute 的 native 默认执行，显式 Pipeline backend 默认预检；test native 与 Pipeline 的筛选器、状态 ID 不同。某命令列出 backend 选项不表示该后端已安装或本工程可用。不能静默换工具/后端将阻塞包装成通过。

CLI 响应结构按命令读取：外层 success、业务状态、errors、数量与产物共同构成证据。不要将领域结果当普通 envelope 剥离，也不要只依赖旧版本退出码或计数。

## 维护

工具升级后核对本次涉及的 action、参数、默认值、结果和目标身份；先查 help/源码，再按授权实际验证。纯文档修正只做文本/链接/JSON 检查，不声称验证运行能力。

连接、超时和串行约束见[操作规程](operations.md)；内容扩展见[内容操作](authoring.md)。保留领域知识，不通过修改第三方包迎合文档。
