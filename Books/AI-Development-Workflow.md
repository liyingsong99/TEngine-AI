# TEngine AI 开发工作流

维护核对：2026-11-18。入口是[仓库 AGENTS](../AGENTS.md)及[Unity 项目 AGENTS](../UnityProject/AGENTS.md)。本仓库统一使用项目内 AIBridge 和 Editor API，不使用 Python 工作流或其依赖。

## 职责与边界

| 层 | 职责与维护位置 |
|---|---|
| 工作约定 | 两级 AGENTS：范围、授权、已有修改、验证与交付 |
| 项目技能 | `.codex/skills`：框架导航、领域知识、按任务读取的规程 |
| Unity 自动化 | `UnityProject/.aibridge/cli/AIBridgeCLI.exe`；以本地 help、已安装源码和实际响应为准 |
| 配置生成 | 仓库根 `Configs/GameConfig` 的现有 Luban 导表脚本和源模板 |
| UI 生成 | 工程 HTML→JSON 浏览器转换器、项目 Editor 烘焙 API；AIBridge 只负责调用 |
| 实际工程 | 当前工作树、Unity Editor/Player 与设备行为 |

不假设另一个 CLI、服务或包必定存在。AIBridge 的 native 与可选 Pipeline backend 是不同契约；不把 Pipeline 文档命令直接当作 AIBridge action。

## 研发闭环

需求确认 → 读取源码与技能 → 实现 → 按影响验证 → 审查 → 证据交付。

小改直接实施并交付摘要。跨模块或多阶段任务使用[任务模板](../UnityProject/.codex/templates/task.md)，记录目标、范围、授权和证据。先检查工作区，不恢复用户删除的文件，不覆盖同事修改。

| 任务 | 技能 |
|---|---|
| 框架、UI 生命周期、事件、资源、热更 | [tengine-dev](../UnityProject/.codex/skills/tengine-dev/SKILL.md) |
| Editor/Player、资源、编译、测试、截图 | [unity-cli](../UnityProject/.codex/skills/unity-cli/SKILL.md) |
| 配置结构、数据与普通/整表懒加载 | [luban-dev](../UnityProject/.codex/skills/luban-dev/SKILL.md) |
| HTML/UI-DSL 到 UGUI | [html-to-ugui](../UnityProject/.codex/skills/html-to-ugui/SKILL.md) |

## 本地命令发现

Unity 版本以 [ProjectVersion](../UnityProject/ProjectSettings/ProjectVersion.txt) 为准，本项目使用 6000.3.28f1、C# 9.0。下列命令在 UnityProject 执行：

```powershell
$CLI = './.aibridge/cli/AIBridgeCLI.exe'
& $CLI --help
& $CLI editor --help
& $CLI inspector get_property --help
```

`--help` 用于读取命令/参数，不代表已经验证 Editor 行为。实际操作前核对本项目唯一实例及状态：

```powershell
& $CLI editor get_state
& $CLI scene get_active
```

服务可连接不等于 Editor 空闲。编译、导入、测试、Play 和资产写入串行，与其他操作者交接。禁止连接其他工程替代本工程验证，禁止读取或输出鉴权描述文件。

## 验证选择

| 改动 | 所需证据 |
|---|---|
| 文档、技能、模板 | 内容自检、活动入口残留扫描、相对链接检查、`git diff --check` |
| C# | 新文件经 Unity 导入、本次 Editor 编译终态、相关行为测试；具备有效生成解决方案时补 .NET 检查 |
| Prefab/场景/资源 | 明确对象与资产的结构、引用、加载行为；需要时补实际截图 |
| HTML/JSON | 浏览器实际布局、图片字体加载、零值与控件层级；不能替代 Unity 显示验收 |
| 配置 | 源表/模板/引用检查、真实导出结果、代码与 bytes 配对及加载验证 |
| 热更/AOT/设备 | 目标平台专项证据，真实构建/发布另外授权 |

```powershell
& $CLI compile unity --timeout 120000
& $CLI get_logs --logType Error
```

新增源码若尚未导入，先在获准范围内 `asset refresh`，再编译。检查 success、终态、errors 等全部字段，不能只看 errorCount 或进程退出码。

Play 验证按已批准的测试场景执行：`editor play` 后用 `editor get_state` 确认，`screenshot game` 保存并读取实际图像，完成后 `editor stop` 并确认状态。截图成功不能证明输入、资源释放或整局玩法通过。

测试先查 `test run --help` 与 `test status --help`。当前默认 native backend 使用 EditMode/PlayMode 和 test-name/group-name/assembly-name；两种测试均从 Edit Mode 启动。`test list` 当前是显式 Pipeline backend 的 EditMode 枚举，不假定可枚举 PlayMode。按源码或测试面板核对实际集合；零用例、全跳过、过期结果不能通过。详见[验证规程](../UnityProject/.codex/skills/unity-cli/references/verification.md)。

## Editor C# 与授权

常规对象操作优先已发现的 inspector/prefab/scene 命令。需要调用项目公共 API 时，脚本写到 `.aibridge/code/*.csx` 或 `.cs`，经审查后调用：

```powershell
& $CLI code execute --file .aibridge/code/task.csx --timeout 30000
```

此处文件名是任务脚本示例，须先实际创建并审核。native 默认会执行，Pipeline backend 默认 dry-run，不能混用默认值。单次执行串行；超时先查 `code status` 和实际目标，不重放、不自动换 backend。

沿用会话中的有效授权；范围改变再确认。覆盖/删除、包依赖/项目设置、平台切换、真实构建单独授权。命令的 confirm 不代替用户授权，不把预览或 Undo 当全工程事务。不提交、推送、签名或发布。

## 领域入口

**Luban**：先读源表、注册、模板与现有 `.bat/.sh`，核对覆盖路径。普通与 lazyload 均输出 cs-bin + bin，后者是整表懒加载，不是分片。现有脚本会复制模板并写生产目录，不能当作只读校验。隔离生成需显式规划目录和参数，详见[导出规程](../UnityProject/.codex/skills/luban-dev/references/tengine-integration.md)。本次本地缺 Luban.dll，未执行导出；不自动安装工具。

**HTML→UGUI**：使用[工程转换器](../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/HtmlToJson/HTML%20转%20JSON%20坐标烘焙器.html)生成 JSON，核对零值、尺寸和引用；再通过 `code execute` 调用 [HtmlToUGUIBakeApi](../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBakeApi.cs)。按当前签名指定预览/覆盖和文字模式，不使用旧文档中的未注册烘焙命令。当前坦克 Demo 明确选择 UGUI Text，绑定 UnityEngine.UI.Text；不要求导入 TMP 资源。通用烘焙器保留两种模式。布局、中文字体、实际宿主 Canvas 和重开按钮必须在 Unity 验证。

## 报告与历史归档

报告必须区分静态、编译、运行态，列出实际命令、目标、数量、终态、截图或日志路径，以及未运行项。传输成功不覆盖内层失败；没有检查不能标通过。CI 仅在有实际部署与执行证据时才能声称可用。

`.codex/tasks` 的旧任务和 `.codex/runs` 是历史归档，记录当时工具与结果，不是当前执行入口。保留历史，不因清理关键词删除证据。新的临时产物放 `.aibridge/tmp`；该目录被忽略，需要共享的代码和文档必须进入仓库正常目录，新增文件仍需由维护者审查纳入版本管理。

技能行为场景见[场景集](../UnityProject/.codex/evals/scenarios.json)和[评估规程](../UnityProject/.codex/evals/evaluation.md)。结构检查不能替代实际任务判断；改动只覆盖确认范围，不为清理文档改框架或第三方包。
