# AI 开发能力速查

源码盘点：2026-11-18。适用本仓库 UnityProject（Unity 6000.3.28f1、C# 9.0）。这是已有 Books 的查询入口，不是第二套 API 手册；**开发前先查下面的任务表，再读所链接源码的实现和最近调用方**。包清单、编译定义、资源地址与运行状态仍以当前工程为准。

## 按任务找已有能力

|任务关键词|先复用什么|专题|
|---|---|---|
|新功能入口、主包/热更、模块依赖|GameModule、GameApp、ProcedureLoadAssembly、实际 asmdef|[架构与生命周期](Capabilities/Architecture.md)|
|全局服务、每帧/物理帧、退出清理|Singleton<T> / SingletonSystem；IUpdateDriver|[架构与生命周期](Capabilities/Architecture.md)|
|异步、延时等待、取消、并行加载|UniTask；业务持有取消与资源归属|[架构与生命周期](Capabilities/Architecture.md)|
|通知、消息广播、接口事件|GameEvent、GameEventMgr、现有接口事件生成链|[架构与生命周期](Capabilities/Architecture.md)|
|冷却、轮询、倒计时、状态切换|GameModule.Timer、GameModule.Fsm|[架构与生命周期](Capabilities/Architecture.md)|
|Prefab、图标、TextAsset、场景|GameModule.Resource / Scene；SetSprite；AssetsReference|[资源与池](Capabilities/Resources.md)|
|复用对象、缓存资源、临时托管对象|IObjectPoolModule / IObjectPool<T>、MemoryPool、ObjectBase|[资源与池](Capabilities/Resources.md)|
|集合、一键多值、时间、随机、数学|GameFrameworkMultiDictionary / LinkedList、GameTime、Unity/.NET 标准能力|[基础工具](Capabilities/Utilities.md)|
|JSON、文本格式、路径、文件、偏好设置|Utility.Json / Text / Path / File / PlayerPrefs|[基础工具](Capabilities/Utilities.md)|
|HTTP、下载、日志、调试、音频、多语言|Utility.Http、Log、GameModule.Debugger / Audio / Localization|[配置与服务](Capabilities/Services.md)|
|配置表、导表、懒加载、二进制|Configs/GameConfig、ConfigSystem 模板、Luban 既有脚本|[配置与服务](Capabilities/Services.md)|
|窗口、控件、监听、列表、安全区、HTML→UGUI|UIWindow/UIWidget、项目辅助、HtmlToUGUIBakeApi|[UI 能力](UI/Capabilities.md)|
|Editor 查询、编译、截图、代码调用|项目 AIBridge；不是业务网络或框架更新驱动|[现有工作流指南](AI-Development-Workflow.md)|

## 优先复用顺序

1. **业务入口**：先看 [GameModule](../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs) 已暴露什么。没有入口不等于底层不存在；例如对象池有 IObjectPoolModule，但当前没有 GameModule.ObjectPool。
2. **项目封装**：查接口、实现和相邻业务调用。已有同步/回调 API 不能凭异步偏好说成不存在；新业务仍使用 UniTask 管理异步结束。
3. **已安装第三方/Unity/.NET**：使用包自带能力，不另造计时、JSON、任务调度或向量数学库。确有封装缺口时只补业务需要的一层，不扩建通用平台。
4. **生命周期**：选择 API 的同时写清谁取消、谁移除监听、谁归还池对象/资源、失败或晚到结果归谁。
5. **核验**：按现有项目技能选择检查；静态存在不等于接入已配置或运行通过。当前不运行 Python，不直接编辑 Scene/Prefab YAML/meta，不自动改包设置。

## 能力分层

|层级|事实入口|含义|
|---|---|---|
|框架源码|[TEngine Runtime](../UnityProject/Assets/TEngine/Runtime/)|模块、事件、内存池、Utility 等可复用实现|
|业务统一入口|[GameModule](../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs)|业务访问资源/场景/音频/UI 等；UI 是热更层单例|
|第三方本地包|[UniTask](../UnityProject/Packages/UniTask/package.json)、[YooAsset](../UnityProject/Packages/YooAsset/package.json)|本次文件版本 2.5.11 / 2.3.17；源码可读，后续版本须重核|
|声明依赖|[manifest](../UnityProject/Packages/manifest.json)、[锁文件](../UnityProject/Packages/packages-lock.json)|HybridCLR、AIBridge、Newtonsoft.Json、UGUI、UI Extensions 等；声明不证明每个特性可直接调用|
|Demo 专用|[TankBattleModule](../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/TankBattle/TankBattleModule.cs)、[Demo 记录](TankBattle-Demo.md)|3 敌/单波、Snapshot、程序化战场；不能当作通用战斗引擎|

## 已核实的误用提醒

- [旧框架概览](2-框架概览.md) 的 Module/ModuleImp 示例与当前 [Module](../UnityProject/Assets/TEngine/Runtime/Core/Module.cs) 不同：当前 Module 是普通 C# 类，不是 MonoBehaviour。
- [网络手册](3-8-网络模块.md) 仍是占位。当前查到 Utility.Http；没有据此确认可用的 GameModule.Network、会话/RPC/重连封装，勿凭标题造调用。
- Utility.Json 默认帮助器使用 Unity JsonUtility，**不是**已安装 Newtonsoft.Json 的自动别名。
- Utility.Tween 有接口门面，但本次 Assets 搜索未找到 ITweenHelper 的具体接入/注册调用；不能直接按“已配置 Tween 服务”使用。
- 配置 lazyload 是整表懒加载，不等于分片；源模板路径在仓库根 Configs，而非 UnityProject/Configs。
- 当前 Demo 选择 UnityEngine.UI.Text；通用烘焙 API 两种模式仍保留。Button.onClick 与 AddUIEvent 是两类监听。
- [旧 repowiki 索引](../UnityProject/repowiki/zh/content/index.md) 的部分链接重复拼接 repowiki 路径；其叙述和示例不是当前 API 证据。本索引直接链接已核对源码/手册，不要求一次读完整 wiki。

## 阅读范围与维护

本基础库覆盖常用 Runtime 接入、生命周期、资源/池、Utility、配置、服务与 UI 导航；不穷举 Unity API、第三方包全部功能、所有 Editor 工具或服务器实现。UI Extensions、平台构建、底层渲染、数据库/账号/网络协议不在本次逐项实现核验范围。需用时再查本地包和任务源码。

条目更新格式保持“用途/何时复用 → 真实符号与源码 → 约束/归属”，不复制长实现。失效链接/旧示例应局部修正；缺实现只报告检索范围与未确认项。验证流程与权限只维护在 [AGENTS](../AGENTS.md) / [项目技能](../UnityProject/.codex/skills/tengine-dev/SKILL.md)，本知识库不另造规则。
