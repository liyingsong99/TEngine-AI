# Unity Project Instructions

公共工作方式见 [仓库入口](../AGENTS.md)。开发前按[已有能力索引](../Books/AI-Capabilities.md)查模块、组件和真实源码入口。本文件只补充 Unity 项目约束。

## 工程事实

- 项目根是本目录，CLI 为 `.aibridge/cli/AIBridgeCLI.exe`；生成解决方案是否存在需实际检查，不合成或借用其他工程。
- 配置源位于仓库根 `Configs/GameConfig`，不是 Unity 项目内的 `Configs`。
- AIBridge 能力以本地 help、已安装源码和本项目实际响应为准，native 与可选 backend 不混用。

## 编码边界

- 业务模块通过 `GameModule` 访问；框架启动层按其现有模块初始化方式工作。
- 业务异步使用 UniTask，明确取消、失败和资源归属。不要把框架现存的同步 API 当作不存在。
- Sprite 优先使用 `SetSprite`；实例化资源使用 `LoadGameObjectAsync`；普通 Asset 加载与释放配对。
- `Assets/GameScripts/GameEntry.cs`、`Procedure` 和 `Assets/Launcher` 属于主包；热更业务位于 `Assets/GameScripts/HotFix`。以 asmdef 验证边界，不虚构 `GameScripts/Main`。
- UI 使用 `AddUIEvent` 管理监听；非 UI 类管理自身订阅。隐藏不等于销毁。
- 修改配置加载器、生成类时追溯 `Configs/GameConfig` 中模板；不直接修补生成产物。
- 不直接编辑 Scene/Prefab YAML、GUID 或 `.meta` 来替代资源数据库操作；新增源文件的 `.meta` 由 Unity 生成。


## 验证选择
C# / 资源变更执行 Unity 编译与相关运行验证；有有效生成解决方案时补 .NET 检查，缺项如实列出。纯文档只做内容、链接与 diff 检查，不触发 Unity。
本仓库不使用 Python 工作流：Unity 侧一律通过项目内 AIBridge CLI 执行，不做 Python 依赖预检。
Editor 断开、忙、多实例或选中零用例时，必需验证返回阻塞/失败，不能降级声称验收通过。

Unity 自动化使用项目 AIBridge；编译、导入、测试、Play 与写入串行。一次性 Editor C# 通过 code execute 调用，超时先查 code status，不重放。
主资产存在或 importer 检查不能替代场景内部验收；Luban lazyload 是整表懒加载，不代表分片能力。当前坦克 Demo 使用 UnityEngine.UI.Text，不导入 TMP 基础资源；通用烘焙器保留两种文字模式。

工具连接描述文件可能含鉴权信息，禁止读出到日志、提交或分享。不得连接本机其他工程代替本项目验证。

<!-- AIBRIDGE:START {"assistant":"aibridge","templateId":"unity-integration","version":11,"target":"root-rule"} -->
## AIBridge Bootstrap

**CLI Alias**: `$CLI = ./.aibridge/cli/AIBridgeCLI.exe`
- `$CLI` 指向项目本地 AIBridge CLI。PowerShell 中可先设 `$CLI = "./.aibridge/cli/AIBridgeCLI.exe"`，再用 `& $CLI <command> [action] [options]` 调用

**常用命令**:
```bash
$CLI compile unity
$CLI get_logs --logType Error
$CLI editor log --message "Hello" --logType Warning
```

**路由原则**:
- 快速任务：纯问答、代码解释、简单查找/显示，且不需要修改代码或 Unity 资源、不输出审查/验证/根因结论时，直接回答或执行，不加载 `aibridge-development-workflow`
- 工作流任务：当任务需要修改代码或 Unity 资源、修改持久化 AGENTS/Skill/workflow 规则、调试根因、采集 Runtime/日志证据，或输出风险审查/验证结论时，必须优先加载 `aibridge-development-workflow`
- 进入工作流后，由 `aibridge-development-workflow` 读取 harness-readiness 作为 Preflight gate、选择任务分支，并决定是否继续加载其它 Skill

**Skill 加载**:
- 工作流任务先加载 `/.aibridge/skills/aibridge-development-workflow/SKILL.md` 中的 `aibridge-development-workflow`
- AIBridge Skills 安装在 `/.aibridge/skills/<skill-name>/SKILL.md`；当本根规则或工作流要求时，从该目录加载同级 Skill

**Temporary Files**:
- Store short-lived Agent and script output under `.aibridge/tmp/`; do not write `tmp_*` or `tmp-*` files to `.aibridge/`

**项目版本**:
- 当前项目 Unity 版本：6000.3.28f1
- 当前项目 C# 语言版本要求：兼容 C# 9.0，禁止使用更高版本语法
- 对象标识：当前 Unity 优先使用 `--instanceId`；`--entityId` 仅适用于 Unity 6000.4+

**当前能力状态**:
- 项目侧能力以本 Root Rule 与已安装 workflow 规则为准；不要为 Code Index、Skill 或 assistant 开关调用 `$CLI harness status` 做 enablement/freshness 预检。可选诊断快照：`.aibridge/harness/capabilities.json`。已选助手：codex。Skill 根目录：.aibridge/skills。Code Index：disabled。外部 agent/sub-agent 能力：Unity 无法判断，按 unknown 处理
- Code Index：已关闭。不要加载 `aibridge-code-index`，也不要调用 `code_index`
<!-- AIBRIDGE:END -->
