# 架构、调度与生命周期

[返回能力索引](../AI-Capabilities.md)。以下为当前源码导航，不是运行验收声明。

## 启动与程序集边界

|场景|入口与用途|限制|
|---|---|---|
|开始业务|[GameEntry.OnInit/Awake](../../UnityProject/Assets/GameScripts/GameEntry.cs) → [ProcedureSetting.StartProcedure](../../UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureSetting.cs) → [ProcedureLoadAssembly](../../UnityProject/Assets/GameScripts/Procedure/ProcedureLoadAssembly.cs) → [GameApp.Entrance](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameApp.cs)|Launcher 进入 main 后初始化框架；热更入口反射调用，不能在程序集可用前调用其业务类|
|调用基础模块|[GameModule](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs)：Resource/Audio/UI/Scene/Timer/Fsm/Procedure/Localization/Debugger/Base|业务走统一入口；TankBattle 是 Demo 专用；没有 Network/ObjectPool 公共属性，别编造|
|放置业务代码|[GameLogic.asmdef](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameLogic.asmdef)、[GameProto.asmdef](../../UnityProject/Assets/GameScripts/HotFix/GameProto/GameProto.asmdef)|热更在 HotFix；GameEntry、Procedure 与 [Launcher.asmdef](../../UnityProject/Assets/Launcher/Launcher.asmdef) 为主包边界。不要新造 GameScripts/Main|
|扩展底层模块|[Module](../../UnityProject/Assets/TEngine/Runtime/Core/Module.cs)、[ModuleSystem.GetModule/RegisterModule](../../UnityProject/Assets/TEngine/Runtime/Core/ModuleSystem.cs)|Module 是普通 C# 类，OnInit/Shutdown；需轮询时实现 IUpdateModule。GetModule<T> 要接口，默认按命名定位实现；普通业务不必再造一套模块容器|

当前 GameApp 通过 UI 打开 Demo，BattleMainUI 初始化成功后才开战。功能是否要新增 Procedure，先看是否涉及启动资源/热更流程，不能把普通玩法直接塞进主包初始化链。

## 单例、Update 与协程

|用途/优先场景|入口|所有权与约束|
|---|---|---|
|需要统一释放的热更服务|[Singleton<T>.Instance/IsValid/Release](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem/Singleton.cs)|Instance 懒创建，OnInit 后登记；Release 调 OnRelease 并注销。查询存活先看 IsValid，避免释放阶段重新创建|
|服务帧更新|[SingletonSystem](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem/SingletonSystem.cs)：IUpdate/IFixedUpdate/ILateUpdate、Retain/Release|实现对应接口即可被单例系统挂到 UpdateDriver。注销与回调集合修改要核对当前遍历；非单例业务对象别无差别永久登记|
|已有对象使用 Unity 回调|[IUpdateDriver](../../UnityProject/Assets/TEngine/Runtime/Module/UpdataDriver/IUpdateDriver.cs)、[Utility.Unity](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Unity.cs)：Add/RemoveUpdateListener、Fixed/Late、Destroy、Pause|保存同一委托，退出配对 Remove；三类帧监听的 Add 存在延后注册窗口，快速退出限制见下文；全局 Destroy 不是业务对象的销毁 token|
|兼容旧 IEnumerator 流程|Utility.Unity.StartCoroutine/StartGlobalCoroutine、StopCoroutine 重载|旧同步与协程能力仍在。带 GameObject/MonoBehaviour 的绑定与全局运行器归属不同；GameCoroutine 包装器对 BindBehaviour=null 的停止路径不可当作已成功停止。新业务异步优先 UniTask|

帧监听快速退出限制：`Utility.Unity` 的 Update/FixedUpdate/LateUpdate 注册会先 `await UniTask.Yield`；`IUpdateDriver` 当前对应的 [UpdateDriver 实现](../../UnityProject/Assets/TEngine/Runtime/Module/UpdataDriver/UpdateDriver.cs) 在三类 Add 内部也会 Yield 后再挂入监听，而 Remove 立即执行。因此同帧 Add 后立刻 Remove，或在延后注册完成前退出，可能先移除、随后又挂回；仅保存同一委托并配对调用仍不足以覆盖此时序，直接换用 `IUpdateDriver` 也不能规避。需要快速退出的调用方必须处理尚未完成的注册及最终清理，不能把 Add 方法返回当作注册完成；本页只记录现有实现限制，不修改框架。

RootModule 的 Update 驱动 ModuleSystem；热更 SingletonSystem 经 IUpdateDriver 驱动，是两套职责不同的生命周期，不要重复登记同一业务更新。示例看 [TankBattleModule](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/TankBattle/TankBattleModule.cs) 的 IUpdate/OnRelease，而非复制其玩法常量。

## UniTask 与取消

第三方 [本地 UniTask](../../UnityProject/Packages/UniTask/package.json) 已存在。常用源码：

- [UniTask.Delay/Yield/DelayFrame](../../UnityProject/Packages/UniTask/Runtime/UniTask.Delay.cs)：等待时间/帧，明确缩放时间与 PlayerLoopTiming。
- [UniTask.WaitUntil](../../UnityProject/Packages/UniTask/Runtime/UniTask.WaitUntil.cs)：条件等待，给取消 token，避免对象消失后永远等待。
- [UniTask.WhenAll](../../UnityProject/Packages/UniTask/Runtime/UniTask.WhenAll.cs)：并行等待，不等于自动取消其他任务或自动释放成功结果。
- [GetCancellationTokenOnDestroy](../../UnityProject/Packages/UniTask/Runtime/Triggers/AsyncTriggerExtensions.cs)：MonoBehaviour/GameObject/Component 生命周期；UIWindow 是普通对象，使用其面板或自己的 CancellationTokenSource。
- [Forget](../../UnityProject/Packages/UniTask/Runtime/UniTaskExtensions.cs)：有异常处理重载；不等于吞掉失败或自动取消。

```csharp
// token 由当前操作的持有者提供，并在退出时取消。
await UniTask.Delay(250, cancellationToken: token);
```

业务用返回 UniTask 的方法形成可观察调用链，明确取消、失败、重开版本与晚到结果。Unity 对象的销毁 token 不覆盖仅隐藏的窗口；资源缓存分支也不保证完整遵守 token，await 后还需判状态与归还过期结果。详细边界复用[资源异步规程](../../UnityProject/.codex/skills/tengine-dev/references/resource-patterns.md)。

## 事件与消息

|用途|真实入口|边界|
|---|---|---|
|跨业务通知|[GameEvent.AddEventListener/Send/RemoveEventListener](../../UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs)|支持 int 与 string ID；UI AddUIEvent 接口只接 int。发送/接收泛型参数必须一致，不能靠反射或 object 猜载荷|
|一组监听随持有者回收|[GameEventMgr.AddEvent/Clear](../../UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEventMgr.cs)|Clear 逐个注销；它实现 IMemory，可由 MemoryPool 持有，释放后勿再使用。UI 框架已有该机制，不重写 EventBus|
|接口式事件|[EventInterfaceAttribute](../../UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventInterfaceAttribute.cs)、[EventMgr.RegWrapInterface/GetInterface](../../UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventMgr.cs)、GameEvent.Get<T>()|需现有生成链注册 wrapper；未注册 GetInterface 返回 default。入口 GameApp 调 GameEventHelper.Init。参照 [ILoginUI](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/IEvent/ILoginUI.cs)，不要假设任意接口可直接广播|

非 UI 对象负责自身订阅移除，UI 使用已有 AddUIEvent；按钮 UnityEvent 配对 AddListener/RemoveListener。隐藏不必然销毁，别靠场景退出或全局 GameEvent.Shutdown 代替局部清理。更多见[事件规程](../../UnityProject/.codex/skills/tengine-dev/references/event-system.md)。

## 计时器与状态机

- [ITimerModule.AddTimer](../../UnityProject/Assets/TEngine/Runtime/Module/TimerModule/ITimerModule.cs) 经 GameModule.Timer 调用；[TimerHandler](../../UnityProject/Assets/TEngine/Runtime/Module/TimerModule/TimerModule.cs) 接 object[]。返回 int ID；Stop/Resume 是暂停恢复，RemoveTimer 才移除。Restart 重置倒计时；isLoop/isUnscaled 明确选择，持有者退出移除自己的 ID，不调用 RemoveAllTimer 清掉他人任务。
- [IFsmModule.CreateFsm/GetFsm/DestroyFsm](../../UnityProject/Assets/TEngine/Runtime/Module/FsmModule/IFsmModule.cs) 经 GameModule.Fsm；适合有 Enter/Update/Leave 的状态序列，[FsmState<T>](../../UnityProject/Assets/TEngine/Runtime/Module/FsmModule/FsmState.cs) 提供状态基类。区分 owner 类型与同 owner 的命名实例，结束 DestroyFsm。
- [IProcedureModule](../../UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/IProcedureModule.cs) 用于项目启动流程管理，实际可用流程由 ProcedureSetting 配置；它不是每个 UI 页签都必须新建的工具。

```csharp
int id = GameModule.Timer.AddTimer(_ => Log.Info("elapsed"), 1f);
// 持有者提前退出时：
GameModule.Timer.RemoveTimer(id);
```

上述短例只展示真实签名，不要求为所有延时引入计时器或重建 FSM；简单等待优先已有 UniTask，长期回调再选 Timer。
