# 3D 坦克大战 Demo：第一版战斗与 HUD

## 2026-11-18 正式 HUD 联调更新

本节为当前状态；下方第一阶段记录保留当时验证，不再代表当前 HUD 接入状态。

- GameApp 改为 GameModule.UI.ShowUIAsync<BattleMainUI>()；BattleMainUI 的 ScriptGenerator 成功绑定、OnCreate 设置布局后才 StartBattle，避免 UI 加载期间敌人先行动。
- 正式 HUD 使用 UI设计师的 html-to-ugui 产物，全部绑定 UnityEngine.UI.Text。数据来自 GameModule.TankBattle.Snapshot，按变化刷新 HP/波次/剩余/击毁/基地及结算文案。
- Button.onClick 配对 AddListener/RemoveListener；框架 AddUIEvent 是 GameEvent 的 int 事件入口，不是 Button 重载。本次无新增全局事件订阅。
- 重开按钮调用统一 RestartBattle，立即刷新并隐藏结果；窗口 hideTimeToClose=0，销毁停止战斗并恢复宿主 CanvasScaler。
- CanvasScaler 仅运行时改 referenceResolution=1920×1080、matchWidthOrHeight=0.5，关闭后恢复原值。未改资产或项目显示设置。
- TankBattleModule 不再创建 TankBattlePlaceholderHud；旧类保留未使用，本轮不删除源码/meta。

### 本轮新增验证

最终 `compile unity --timeout 120000`：success=true、idle、statusConfirmed=true、0 error、0 warning，duration=3.05。证据：UnityProject/.aibridge/tmp/tank-hud-final-compile.json。

通过 code execute 执行 tank-hud-scenarios.csx，在实际 UIWindow 中受控推进真实战斗与 Physics：初始数据/结果隐藏、旧占位不存在、玩家失败文案、基地失败文案与状态、胜利数据五项通过；关闭后重新 ShowUIAsync，再跑五项通过。证据 tank-hud-scenarios.json、tank-hud-reopen.json。

真实 AIBridge `input click --instanceId -41546` 命中当前窗口的 m_btnRestart（raycastTarget/pointerPress/clickHandler 均为该按钮）。不是直接调用 onClick。随后读回：Playing、HP 3/3、击毁 0、剩余 3、result=false、占位数 0。证据 tank-hud-after-click.json。早先用户试玩期间结果层隐藏导致一次点击失败，该次不算通过；重新独占后才验证成功。

`GameModule.UI.CloseUI(type)` 实测战斗停止、宿主恢复 750×1334/match0、Main Camera 恢复 (0,0,-10)/orthographicSize5；测试 TimeScale 恢复1。证据 tank-hud-close-result.json。最后退出 Play，Launcher未脏、Editor空闲，Error查询为空。

截图采用实际渲染帧：

- UnityProject/.aibridge/screenshots/game_20261118_161041_da8cde3f.jpg：正式 HUD 重开后初始数据。
- UnityProject/.aibridge/screenshots/game_20261118_161318_3f6d929a.jpg：真实战斗逻辑结算后的中文胜利 HUD。

暂停帧中第一次截图滞留旧画面，未采用；之后短暂 TimeScale=0、取消 Editor 暂停刷新渲染，再截图并读图核对，退出前已恢复。受控 Tick 中命中特效延迟销毁可能停留于截图，不能作为实时特效寿命测量。

三态：静态限定 diff 检查通过，战斗核心此前独立审查通过，HUD 增量待复查；Unity 编译通过；上述 HUD 业务与实际按钮检查通过。原生 WASD/Space/R 端到端仍未验证；真实炮弹同帧专项已由下节补齐。不将用户简单试玩代替证据。不运行 Python，不导入 TMP，不提交推送。

## 2026-11-18 同帧真实炮弹补测

本轮未修改业务 C#、Prefab 或设置，只新增本机验证脚本与本记录。

- `compile unity --timeout 120000`：success=true、idle、statusConfirmed=true，0 error/0 warning，duration=3.78。
- `code execute --file .aibridge/code/tank-real-same-frame.csx --timeout 30000`：四个用例通过，完整输出 `.aibridge/tmp/tank-real-same-frame-result.json`。
- 前两个敌人通过正常 Fire/TankProjectile/Tick/Physics 命中击毁；玩家最后一点 HP 也通过实际敌弹消耗得到。测试只布置对象位置、朝向与开火冷却，不写血量、击杀数或胜负字段，不直接调用 Hit/EvaluateResult。
- 最终每例恰好两枚正式 TankProjectile，经一次正常 Tick(0.05) 扫掠命中并统一结算。测量前后均为 frameCount=1593，击毁数 2→3。

|目标|玩家炮弹先处理|玩家生命|基地生命|最终结果|
|---|---|---|---|---|
|玩家死亡|否|1→0|1→1|Defeat / PlayerDestroyed|
|玩家死亡|是|1→0|1→1|Defeat / PlayerDestroyed|
|基地死亡|否|3→3|1→0|Defeat / BaseDestroyed|
|基地死亡|是|3→3|1→0|Defeat / BaseDestroyed|

首次耗血前置因 AI 绕开玩家未达到 1 HP，首次基地前置因护墙挡住未命中；修正测试布置后四例完整通过，没有因此修改业务。基地最终从南侧无遮挡位置布置射击。期间一次 Play 被外部退出，脚本以 Requires Play Mode 停止；确认空闲后重新进入完成测试，该失败不计通过。

键盘仅做一次受前台进程身份保护的尝试：`.aibridge/tmp/tank-keyboard-test.ps1`。本次目标进程校验通过，发送 WASD/Space，但读回玩家仍在 (-4,0,-7)、HP2、击毁0，无法确认 Game View 消费输入；没有继续盲发或把它判为按键通过，R 未发送。原生三类按键端到端仍需后续有可靠证据的验证。

结束已恢复 TimeScale=1，editor stop 后 isPlaying/isPaused/isCompiling/isUpdating 均 false，Launcher isDirty=false，Error日志为空，已明确交还 Editor。

## 第一阶段记录（占位 HUD，历史）

更新：2026-11-18。工程 UnityProject，Unity 6000.3.28f1，C# 9.0。范围由群内管理员确认：先不等待正式 HUD，跑通战斗；无提交、推送、真实构建、包依赖或项目设置修改。

## 启动与操作

从既有 Launcher 进入 Play，沿原 Procedure/热更加载链到 GameApp.StartGameLogic，再调用 GameModule.TankBattle.StartBattle()。不新增 Procedure、Scene 或场景资源。

- WASD 四方向移动；同时按键按 W、S、A、D 顺序选一个方向。
- Space 按住连续开火，玩家冷却 0.3 秒。
- 玩家 3 HP；敌人/基地各 1 HP；1 波共 3 个敌人，最多同时 2 个，出生位置被占时等待。
- 可破坏砖墙、不可破坏外墙；玩家炮弹不伤自己和基地，敌方炮弹不伤友军。
- 敌人向南推进，接近基地后横向靠拢；遇砖墙/基地保持朝向射击，遇其它障碍短暂换向。
- 全灭敌人且玩家/基地仍存活为胜利；玩家死亡或基地被毁失败。同帧先汇总命中，失败优先。
- 结算清理炮弹并停止战斗更新，R 触发统一 RestartBattle()。进行中调用 RestartBattle() 不重复开局。
- 程序化几何体、8 份共享运行时材质、单方向光、固定斜俯视正交相机。相机根据实际宽高比容纳地图。
- Game View 已选择现有 1920×1080 横屏选项；未改 PlayerSettings。战斗结束释放场景根和材质并恢复主相机参数。

## 修改范围

业务源码根：UnityProject/Assets/GameScripts/HotFix/GameLogic。

|文件|职责|
|---|---|
|GameApp.cs|将原示例 UI 启动改为坦克战斗启动。|
|GameModule.cs|新增 TankBattle 统一入口，不另写模块管理器。|
|Module/TankBattle/TankBattleModule.cs|复用 Singleton<T>/IUpdate，管理输入、敌人调度、炮弹、胜负、重开及退出。|
|Module/TankBattle/TankBattleWorld.cs|几何体战场、材质、灯光、砖墙和相机，释放运行时资源。|
|Module/TankBattle/TankUnit.cs|共用坦克生命、碰撞移动、朝向、冷却与敌人行为。|
|Module/TankBattle/TankProjectile.cs|SphereCast 扫掠取最近命中，过滤自身/友军，控制寿命与越界。|
|Module/TankBattle/TankBattleState.cs|HUD 数据快照、战斗状态和失败原因。|
|Module/TankBattle/TankBattlePlaceholderHud.cs|正式 HUD 前的临时 OnGUI 数据读数，无 Prefab 节点依赖。|

6 个新增 C# 文件的 .meta 由 Unity AssetDatabase 刷新生成，未手写。未改 BattleMainUI.cs、UI prefab、Scene、asmdef、主包 Procedure 或 GameEntry。

同一工作树中的 AGENTS.md、HtmlToUGUIBaker.cs 及相关 UI 修改由其他工作产生，不属于本战斗改动。

## 给 UI设计师的接入契约

读取 `GameModule.TankBattle.Snapshot` 得到只读值类型 `TankBattleSnapshot`：

- PlayerHp / PlayerMaxHp
- WaveIndex / WaveTotal（当前固定 1/1）
- RemainingEnemies（包含待生成） / KillCount / TotalEnemies（总数 3）
- BaseHp
- BattleState：Preparing / Playing / Victory / Defeat
- FailureReason：None / PlayerDestroyed / BaseDestroyed

模块同步建立战场，因此 Preparing 尚无独立加载界面；正式 UI 接入时用 IsRunning 区分未运行状态，不应在 StopBattle 后继续显示旧快照。重开按钮调用 `GameModule.TankBattle.RestartBattle()`；退出调用 StopBattle()。不要求 UI 读取 private 字段。

正式 HUD 由 UI设计师通过工程 html-to-ugui 生成，沿用 BattleMainUI 窗口/资源地址，挂在现有 UIRoot/UICanvas，窗口根保留 Canvas/GraphicRaycaster。尚未接入生成产物；接入后移除临时 OnGUI 组件的创建，窗口仅负责展示和操作，战斗仍由模块更新。当前我不操作 UI 资源。

## 本轮验证结果

用户要求停用所有 Python 流程后，本实施轮未运行 Python。没有修复旧 workflow 或安装依赖。

### 静态

实际执行：

- 仓库根 `git diff --check`：退出 0。
- 对 6 个新增 C# 文件检查 .meta 存在：全部存在。
- PowerShell 扫描新增文件中的 UnityEditor、Task.Run、async void、文件范围 namespace、required/file 类型等禁用模式：未命中。该扫描不是完整 C# 分析器。
- 文件行数：202 / 205 / 104 / 55 / 35 / 41，均低于单文件限制；未引入业务程序集之外的新引用。

结论：上述限定检查通过；静态整体验收未通过（尚无独立只读审查及完整分析器结果）。没有执行检查的项目不标为通过。

### Unity 编译

工作目录 UnityProject，命令为 `.aibridge/cli/AIBridgeCLI.exe compile unity --timeout 120000`。

首次调用失败，CS0246 找不到新增 TankBattleModule；新增目录尚未导入。经 `asset refresh` 由 Unity 导入源文件、生成 meta 后通过。AI 修正后最终一次编译返回 success=true、status=idle、startedByInvocation=true、statusConfirmed=true、duration=4.57、errorCount=0、warningCount=0。

Unity Editor 编译通过；未执行 .NET 解决方案构建、IL2CPP/AOT/真机构建或热更包验证。

### 运行态

通过 AIBridge `editor play` 进入本项目 Editor Play；在 1920×1080 截图核对战场和玩家/敌人外观。受控测试使用 `code execute --file .aibridge/code/<name>.csx --timeout 30000`，通过反射调用实际 Tick、命中与结算方法，使用实际 Unity Physics；不直接把状态字段改成胜利。测试中暂停 Editor 并设置位置作为可复现前置。

执行脚本与证据（本机 .aibridge 被 git 忽略，不是共享测试包）：

|脚本|断言|证据|
|---|---|---|
|tank-battle-verify.csx|13 项通过：初始 HP/待生成数量、移动、砖墙阻挡、炮弹破砖、不自伤、炮弹击毁、3 敌胜利、结算炮弹集合为空、3 次数据重置、玩家失败、破护墙后基地失败。|.aibridge/tmp/tank-battle-verify-result.json|
|tank-battle-boundary-verify.csx|5 项通过：边界阻挡、炮弹胜利、剩余为 0、全程敌人不超过 2、结算 OnUpdate 不移动玩家。|.aibridge/tmp/tank-boundary-result.json|
|tank-battle-cleanup-verify.csx|4 项通过：同帧最后击杀与基地被毁时失败优先、重开仅 1 个活动战场根、StopBattle 禁用战场对象、主相机恢复。此组的同帧条件通过实际 Hit API 布置，不是键盘对局。|.aibridge/tmp/tank-cleanup-result.json|

首次基地失败用例未通过，定位为敌人在砖墙前来回绕行；已修正为可破坏障碍前保持朝向射击，完整复测 13 项全部通过。

原生键盘自动化未通过前台窗口归属确认，脚本在发送按键前中止。WASD/Space/R 的代码路径已实现，但不把受控 Tick 测试等同于真实键盘全流程；需要在 Game View 聚焦后人工或可用输入工具完成验收。

`get_logs --logType Error` 在最终 Play 结束后返回 logs=[]。最后 `editor stop` 后 `editor get_state` 为 isPlaying=false/isPaused=false/isCompiling=false/isUpdating=false。已在群内明确交还 Editor 给 UI设计师；之后不再触发 Unity 操作。

截图：

- `.aibridge/screenshots/game_20261118_151735_3429db7f.jpg`：1920×1080 初始战场，HP 3/3、1 波、剩余 3。
- `.aibridge/screenshots/game_20261118_152124_9952009e.jpg`：1920×1080 胜利，击毁 3/3、剩余 0、基地 1。由于在暂停的受控测试帧截图，短暂命中特效仍留在画面，不能据此推断实时特效寿命异常。

三态最终口径：静态未通过（限定自检已过，待独立审查）；Unity Editor 编译通过；运行态受控战斗检查 22 项通过，真实键盘端到端和正式 HUD 尚未验收，不宣称完整运行验收通过。

## 最短人工复测

1. 从 Launcher 按 Play，聚焦横屏 Game View；确认一张战场、绿色玩家、红色敌人、基地、临时数据读数。
2. 按 WASD 验证四向移动、朝向与墙阻挡；按住 Space 击毁敌人和砖墙；观察最多同屏 2 敌、总共 3 敌。
3. 全部击毁后显示 Victory、剩余 0；按 R，确认血量、击毁数与场景重置。
4. 不射击让玩家被击毁，确认 PlayerDestroyed；移动避开，让敌人击毁基地，确认 BaseDestroyed。
5. 连续重开三次，退出 Play，再次 Play；确认没有重复战场或错误日志。

需要独立审查关注：边界碰撞、阵营过滤、同帧胜负、输入路径、StopBattle/Release 生命周期、程序化 Material 归属及热更依赖方向。
