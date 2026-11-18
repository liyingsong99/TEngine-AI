# 资源、实例与池

[返回能力索引](../AI-Capabilities.md)。优先复用 GameModule.Resource，底层为本地 YooAsset。这里只列选型与释放契约，详细加载竞争说明沿用[现有资源规程](../../UnityProject/.codex/skills/tengine-dev/references/resource-api.md)。

## 先按所有权选择 API

|需要什么|入口与源码|持有/释放|
|---|---|---|
|实例化一个 Prefab|[IResourceModule.LoadGameObjectAsync](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/IResourceModule.cs)，通过 GameModule.Resource，参数 location/parent/cancellationToken/packageName|返回场景实例；业务销毁实例，[AssetsReference.OnDestroy](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsReference.cs) 归还源引用，不重复 UnloadAsset 同一份源|
|读 TextAsset/材质等普通 Asset|同接口 LoadAssetAsync<T>/LoadAsset<T>|每次成功取得后配对 UnloadAsset；即便结果已过期未显示也要归还|
|显示 Image 或 SpriteRenderer|[SetSpriteExtensions.SetSprite/SetSubSprite](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Extension/Implement/SetSpriteExtensions.cs)|图片扩展跟踪引用；不额外写一套普通 Asset 加载器。散图/子图并发与重复引用限制需读实现|
|需要自己管理 YooAsset handle|IResourceModule.LoadAssetAsyncHandle<T> 与 [YooAsset 源码](../../UnityProject/Packages/YooAsset/Runtime/)|检查有效性/完成/失败并 Dispose；不能混用对象池的 UnloadAsset 当句柄释放|
|检查资源地址或预加载标签|CheckLocationValid、GetAssetInfos|地址来自实际收集器/Package，文件名/文件路径不保证可作为地址；标签检索不等于资源已加载|
|同步或旧回调调用方|LoadGameObject、LoadAsset<T> 及 LoadAssetAsync(...LoadAssetCallbacks...)|真实存在，按调用链使用；新业务优先异步，不全局替换启动器的 Resources 或同步接口|

普通 Asset 的短期读取示例（location 与 token 必须来自当前任务）：

```csharp
var asset = await GameModule.Resource.LoadAssetAsync<UnityEngine.TextAsset>(location, token);
try
{
    if (asset == null || token.IsCancellationRequested) return;
    string contents = asset.text;
    // 当前业务读取 contents。
}
finally
{
    if (asset != null) GameModule.Resource.UnloadAsset(asset);
}
```

这不表示所有异常都能简化成取消：缓存/同地址等待分支的 token 行为要核对；加载失败可能抛错或返回空。更换/销毁目标后晚到实例仍归原任务清理。

## Unload 不等于立即卸载

[ResourceModule.Pool.UnloadAsset](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/ResourceModule.Pool.cs) 实际调用资源池 Unspawn；[AssetObject.Release](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/ResourceModule.AssetObject.cs) 才处理底层 handle Dispose（Shutdown 分支另有路径）。因此：

- 归还引用与 Bundle 立即卸载是两件事；不要为看到内存未立刻下降而重复卸载。
- AssetAutoReleaseInterval/AssetCapacity/AssetExpireTime 是池参数，不能替代配对归还。
- 不在普通窗口关闭时强制清整个包或调用全局资源释放。
- 自己 new 的 Material/运行时几何体没有 TEngine Asset 引用，创建者自行 Destroy，不交给资源池误释放。

## 两种池不能混用

|场景|能力|生命周期|
|---|---|---|
|短命托管对象/事件记录|[MemoryPool.Acquire<T>/Release](../../UnityProject/Assets/TEngine/Runtime/Core/MemoryPool/MemoryPool.cs)，T 为 class、IMemory、new()|[IMemory.Clear](../../UnityProject/Assets/TEngine/Runtime/Core/MemoryPool/IMemory.cs) 清回收状态；释放后不保留使用引用，不重复 Release。不是 GameObject 实例池|
|需要初始化/回收回调的托管对象|[MemoryObject、MemoryPool.Alloc/Dealloc](../../UnityProject/Assets/TEngine/Runtime/Core/MemoryPool/MemoryPoolExtension.cs)|Alloc 额外调 InitFromPool，Dealloc 调 RecycleToPool 再 Release；不能随意和仅 Acquire/Release 混搭遗漏回调|
|带名称/容量/过期/优先级的重对象缓存|[IObjectPoolModule](../../UnityProject/Assets/TEngine/Runtime/Module/ObjectPoolModule/IObjectPoolModule.cs) 的 CreateSingleSpawnObjectPool/CreateMultiSpawnObjectPool、GetObjectPool、DestroyObjectPool|当前 GameModule 未暴露 ObjectPool；框架已有模块，不因此另写池管理器。业务需要时在 GameModule 增加最薄入口，保持统一访问|
|池内对象借还|[IObjectPool<T>.Register/Spawn/Unspawn](../../UnityProject/Assets/TEngine/Runtime/Module/ObjectPoolModule/IObjectPool.cs)、[ObjectBase](../../UnityProject/Assets/TEngine/Runtime/Module/ObjectPoolModule/ObjectBase.cs)|T 是 ObjectBase 包装，Initialize 绑定 target；OnSpawn/OnUnspawn/Release 定义使用与实际销毁。Single/Multi 指是否可多次获取，不是 Unity 单/多线程池|

现有资源池是 MultiSpawn 的实际示例：同个资源可被多个消费者持有。对象池本身不会自动替你 SetActive、重置位置、注销事件或清空子弹伤害状态，这些归具体包装者/业务。不要因为 Demo 当前直接创建三辆敌人就为首版引入复杂池；出现实际复用需求再使用已有池。

## 场景与常用引用组件

- [ISceneModule.LoadSceneAsync/UnloadAsync/ActivateScene/UnSuspend](../../UnityProject/Assets/TEngine/Runtime/Module/SceneModule/ISceneModule.cs) 经 GameModule.Scene：管理定位地址、Single/Additive、挂起激活和进度。当前接口没有 cancellationToken 参数，不编造可取消重载；调用方需管理退出后的状态与回调。
- [AssetsReference](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsReference.cs)：Prefab 实例与额外 Asset 引用关联，销毁时归还。克隆/手动 Instantiate 不能假设继承正确引用归属，优先走 LoadGameObjectAsync。
- [ResourceExtComponent](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Extension/ResourceExtComponent.cs)：图片/扩展资源跟踪入口；细节由 SetSprite 实现和生命周期决定，不因隐藏就宣称卸载。
- [ResourceModuleDriver](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/ResourceModuleDriver.cs)：框架初始化与资源运行模式/卸载策略的组件，不要在每个业务对象另挂一份驱动。

旧概念说明可以读[内存池手册](../3-3-内存池模块.md)、[对象池手册](../3-4-对象池模块.md)、[资源手册](../3-1-资源模块.md)，但以本页源码链接的真实签名为准。
