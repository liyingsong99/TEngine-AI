# 坦克大战 HUD 交付（2026-11-18）

## 产物与生成

- 源稿：`BattleMainUI.html`，1920×1080，单命名根，32 个 UI-DSL 节点，0 张图片。
- 浏览器实测输出：`BattleMainUI.json`，由工程自带 HTML 转 JSON 坐标烘焙器生成，不是手写坐标 JSON。
- 正式资源：`UnityProject/Assets/AssetRaw/UI/BattleMainUI.prefab`，GUID `ecec8ee629e7e1945b209218a435b9cc`（与原资源一致）。33 个 GameObject、23 个 UnityEngine.UI.Text、0 TMP、0 图片引用。
- 探针资源：`UnityProject/Assets/AssetRaw/UI/PipelineProbeUI.prefab`，GUID `9b4209d9acf4bdb459425815077222d1`，4 DSL/10 GameObject/2 UGUI Text，滑条值0保留。
- 入口：`UnityProject/Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBakeApi.cs`。窗口/API 共用 GenerateTree；本 Demo 显式 legacyText:true，并复用原窗口 LegacyRuntime 字体。没有执行 TMP 导入脚本。

从 UnityProject 通过 `.aibridge/code/*.csx` 调用 Bake，先 previewOnly:true 审阅范围，再 overwrite:true 生成；完整参数见 HtmlToUGUI/README.md。此 HUD 生成后需用 PrefabUtility.LoadPrefabContents 将 m_goResult 设 inactive，再 SaveAsPrefabAsset 并 UnloadPrefabContents；这是初始显示状态设置，所有 UI 结构仍由 HTML 产线生成。旧窗口节点会被替换，旧业务绑定必须同步更新。

## 业务绑定交接

由 TEngine工程师 实现 BattleMainUI : UIWindow，GameModule.UI 加载到现有 UIRoot/UICanvas；我未改业务代码。

| 相对窗口根的节点 | 组件 | 数据 |
|---|---|---|
| m_goHud/m_textPlayerHp | Text | PlayerHp / PlayerMaxHp |
| m_goHud/m_textWave | Text | WaveIndex / WaveTotal |
| m_goHud/m_textRemainingEnemies | Text | RemainingEnemies，含待生成 |
| m_goHud/m_textKillCount | Text | KillCount / TotalEnemies |
| m_goHud/m_goBasePanel/m_textBaseHp | Text | BaseHp；安全或已摧毁 |
| m_goResult | GameObject | Playing隐藏，Victory/Defeat显示 |
| m_goResult/m_goResultPanel/m_textResultTitle | Text | 防守成功 / 防守失败 |
| m_goResult/m_goResultPanel/m_textResultReason | Text | 胜利文案或PlayerDestroyed/BaseDestroyed对应原因 |
| m_goResult/m_goResultPanel/m_textResultStats | Text | 击毁数与剩余HP |
| m_goResult/m_goResultPanel/m_btnRestart | Button | AddUIEvent → RestartBattle() |

宿主 CanvasScaler 原值是750×1334/match0。本Demo运行时需 ScaleWithScreenSize、1920×1080、matchWidthOrHeight=0.5；生命周期结束恢复。Prefab全树已UI层5，根Canvas+GraphicRaycaster；不另建全局根或EventSystem。现有顶部占位GUI由程序接入时关闭。原 GameApp 当前仅启动战斗、未打开窗口；需接通正式UI加载。数据接口是 GameModule.TankBattle.Snapshot。

## 三态证据

### 静态通过

本轮资产结构检查通过：32 DSL/33对象，所有节点尺寸有效；23 Text均有字体，逐字符HasCharacter无缺字，0 TMP，UI层正确，无Missing Script；Prefab GUID保留；结果默认隐藏。输入拒绝测试与零值回归此前已记录。仅本轮手写文件执行diff检查；完整差异含Unity自动meta空字段尾空格，未手改meta。未宣称独立代码审查或工程全量静态门禁通过。

### 编译通过

最终 `AIBridgeCLI compile unity --timeout 120000` 返回 idle、statusConfirmed=true、0 error、0 warning。证据：`UnityProject/.aibridge/tmp/ui-pipeline-probe/legacy-final-compile.json`（2026-11-18 15:55左右）。脚本包装器在部分循环+return脚本报告CS0162 warning，未有CS error；不是项目Unity编译错误。

### 运行态通过

Play 下实际 UIRoot/UICanvas 临时挂载最终生成资源的视觉检查通过：

- HUD：`UnityProject/.aibridge/screenshots/game_20261118_155548_c9f9ab98.jpg`（15:55:48）
- 结算布局样例：`UnityProject/.aibridge/screenshots/game_20261118_155626_a447b748.jpg`（15:56:26）

两图是Unity真实画面，但数据为静态设计样例，结算是手动显示层，不能声称真实胜利、动态血量或按钮重开已联调通过。实际UIWindow加载、绑定、失败分支和按钮闭环待工程师接入后验证。截图中的DEBUGGER为工程原有开发界面。

退出检查：15:57:48，isPlaying=false，Launcher场景未脏，TimeScale=1，Error日志空。已交还Editor给TEngine工程师。没有保存场景、手写YAML/meta、提交推送或构建。

## 已发现并修正

1. 浏览器转换器把slider=0通过 `||0.5`变为0.5：已改Number.isFinite并实际重烘焙验证。
2. 自动生成节点默认层0，被UICamera排除：API按来源Canvas层或UI层设置整棵树，重新生成并不做运行时补丁复测通过。
3. 工程原CanvasScaler为竖屏：以运行时临时配置验证横屏，正式生命周期配置交由业务接入处理。

## TMP目录说明

本轮开始时工作区出现未跟踪的 `Assets/TextMesh Pro/` 内容（字体、Settings、shader等）。我之前仅准备了导入脚本，没有执行；来源未确认。本轮没有删除、导入或git add该目录。最终HUD TMP组件数量为0。
