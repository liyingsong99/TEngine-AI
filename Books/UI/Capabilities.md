# UI 能力速查：先复用，再扩展

本页是 AI 开发 UI 前的源码导航，不替代详细手册。核对日期：2026-11-18。范围是本仓库现有实现；当前坦克 Demo 使用 **UnityEngine.UI.Text**，不运行 Python，不导入 TMP 基础资源。

## 1. 按任务定位

| 要做什么 | 优先复用 | 约束与下一跳 |
|---|---|---|
| 打开、刷新、隐藏、关闭窗口 | `GameModule.UI` → `UIModule`、`UIWindow` | 类型级复用、Hide 延时关闭；见第2节 |
| 拆一个嵌套面板、物品格、动态条目 | `UIWidget` + `UIBase.CreateWidget*` | 由父 UI 管生命周期，不给每个条目造一套窗口；见第3节 |
| 监听按钮或业务消息 | Button/Toggle 的 UnityEvent；`AddUIEvent` 管 GameEvent | 两类不是同一套接口，见第4节 |
| 查子控件、生成绑定 | `FindChildComponent<T>`、`ScriptGenerator`、`UIBindComponent` | 路径绑定与索引绑定不能混用，见第5节 |
| 显示图标、子图、加载 UI 实例 | `SetSprite`、`SetSubSprite`、`IUIResourceLoader` | 使用已有加载和释放契约，见第6节 |
| 处理层级、全屏遮挡、安全区 | `WindowAttribute`、窗口 Visible/Depth、`SetUIFit` | 不另建全局 Canvas；见第7节 |
| HTML 原型变成可加载 Prefab | 工程浏览器转换器 → `HtmlToUGUIBakeApi.Bake` | 不是任意网页转换器，不是自动业务绑定器；见第8节 |
| 查坦克 HUD 的具体业务接入 | `BattleMainUI`、本目录交付说明 | Demo 专用，不把战斗逻辑写入通用烘焙器；见第9节 |

## 2. 窗口：显示、缓存与销毁

**源码入口**：[UIModule](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs)、[UIWindow](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs)、[UIBase](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs)、[WindowAttribute / UILayer](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs)。详细说明：[UI 生命周期](../../UnityProject/.codex/skills/tengine-dev/references/ui-lifecycle.md)。

- `GameLogic.UIWindow : UIBase` 是普通 C# 对象，不是 MonoBehaviour。不要给它写 Unity `Start/Update/OnEnable` 期待自动执行，也不要自行 `AddComponent<UIWindow>()`。
- `[Window(UILayer.UI, location: "资源地址", hideTimeToClose: 10)]` 声明层级、资源定位、全屏标记和隐藏缓存秒数；默认 HideTimeToClose 为10。`UILayer` 有 Bottom/UI/Top/Tips/System，它与 GameObject.layer 不是同一个枚举。
- `ShowUI<T>()` 同步加载；`ShowUIAsync<T>()` 返回 void；`ShowUIAsyncAwait<T>()` 返回 `UniTask<T>`。调用窗口前先确保启动流程和 UIModule 初始化完成。
- 管理器按类型 FullName 查找已存在窗口；再次 Show 会重新排序、更新 UserDatas 并触发准备回调/刷新，不会每次都生成新窗口实例。
- 首次创建顺序：`Inject → ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate`；之后由 `OnWindowPrepare` 调 `InternalRefresh → OnRefresh`。绑定放 ScriptGenerator/BindMemberProperty，注册放 RegisterEvent，刷新放 OnRefresh，避免每次刷新叠加监听。
- `CloseUI<T>()` 真正销毁并出栈。窗口先清框架 GameEvent，再销毁子 Widget，随后自身 OnDestroy，最后销毁面板并取消隐藏计时器。自己发起的任务/UnityEvent/外部订阅仍由自己收尾。
- `HideUI<T>()`：HideTimeToClose≤0 时直接 Close；否则设置不可见并计时关闭。Hide 不是销毁，不保证取消任务或移除事件；再次 Show 会取消隐藏关闭计时器。
- 窗口 InternalUpdate 在未准备或不可见时不执行。战斗、网络等业务推进不能依赖窗口 OnUpdate；窗口适合做显示刷新。
- 等待返回窗口不等于“业务初始化成功”：当前 ShowUIAsyncAwait 内部用 IsLoadDone 和累计 Time.deltaTime 等待，约60秒超时后仍返回对象；已有窗口路径也可能直接返回。UIWindow 在设置 IsPrepare/执行创建回调之前就置 IsLoadDone。关键流程应有自身初始化成功条件并检查日志，不能只检查非 null。

真实调用形式（窗口类型需带 WindowAttribute，且资源已存在）：

```csharp
GameModule.UI.ShowUIAsync<BattleMainUI>();
GameModule.UI.HideUI<BattleMainUI>(); // Demo hideTimeToClose=0，所以实际关闭
GameModule.UI.CloseUI<BattleMainUI>();
```

## 3. Widget、条目与小型列表

**入口**：[UIWidget](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWidget.cs)、[UIBase 的 CreateWidget 系列与列表辅助](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs)。详细模式：[Widget 与列表](../../UnityProject/.codex/skills/tengine-dev/references/ui-patterns.md)。

| 来源 | 现有方法 | 何时使用 |
|---|---|---|
| 窗口已有节点 | `CreateWidget<T>(string goPath, bool visible=true)` 或 GameObject 重载 | 包装一块已有面板，不重复实例化 |
| 显式资源地址 | `CreateWidgetByPath<T>(parent, assetLocation)` / `CreateWidgetByPathAsync<T>` | 通过项目 UI 加载器创建条目 |
| 现成 Prefab 模板 | `CreateWidgetByPrefab<T>(goPrefab, parent)` | 已持有模板时实例化，避免重复查资源 |
| 类型名就是地址 | `CreateWidgetByType<T>` / `CreateWidgetByTypeAsync<T>` | 仅在 `typeof(T).Name` 与资源地址确实相同时使用 |
| 数量随数据变化 | `AdjustIconNum<T>` / `AsyncAdjustIconNum<T>` | 小型格子/条目集合，调用方持有 `List<T>` |

泛型创建要求 `T : UIWidget, new()`；创建会加入父 UI 的 ListChild，执行注入/绑定/事件/OnCreate/OnRefresh。不要给同一个 GameObject 重复包装 Widget。

- `CreateWidgetByPathAsync` 使用宿主 GameObject 的销毁 CancellationToken，没有额外业务 token 参数；仍须处理返回 null 和请求过期。
- `AdjustIconNum` 的同步实现没有使用它声明的 assetPath 参数：无 prefab 时走类型名地址。`AsyncAdjustIconNum` 返回 void，内部 Forget，不可 await；maxNumPerFrame 应为正数，调用方避免同一列表的并行调整。
- 不要传 null 列表并期待辅助函数替你回填引用；调用方先创建列表，再逐项写业务数据。
- `widget.Visible` 设置激活状态，但当前 Widget.InternalUpdate 只检查 IsPrepare；父窗口仍更新时，不能假定隐藏 Widget 已停更。
- 主动移除调用 `widget.Destroy()`，不是只 `list.Clear()` 或只销毁 Transform。它先从父列表移除，调用自身 OnDestroy，再内部清事件/子项/实例；父窗口关闭也会级联清理。
- 这些方法不是虚拟滚动列表引擎。若要大列表，先进一步查当前包/实现，不能把 `AdjustIconNum` 宣称为对象池或虚拟化列表。

## 4. 两种监听：按钮不属于 AddUIEvent

**入口**：[UIBase.AddUIEvent](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs)、[GameEventMgr](../../UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEventMgr.cs)、[BattleMainUI.RegisterEvent / OnDestroy](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs)。

| 监听对象 | 正确复用方式 | 清理责任 |
|---|---|---|
| 业务事件 ID + Action | `AddUIEvent(eventType, handler)`，有0～4参数包装 | 记录进 GameEventMgr；UI销毁时通过回收到 MemoryPool 调 Clear，移除 GameEvent 监听 |
| Button.onClick / Toggle.onValueChanged 等 | UnityEvent 的 AddListener | 自己 RemoveListener，同一委托配对；AddUIEvent 不会管理它 |
| 非 UI 类的全局 GameEvent | `GameEvent.AddEventListener` 等真实总线 API | 持有者生命周期内移除；不依赖任意窗口关闭替你清理 |

当前 Demo 的真实模式，可作为生命周期示例：

```csharp
protected override void RegisterEvent() => _restart.onClick.AddListener(Restart);
protected override void OnDestroy()
{
    if (_restart != null) _restart.onClick.RemoveListener(Restart);
    // 本窗口另外持有的业务任务/布局修改也在自己的生命周期释放。
}
```

隐藏缓存期内 GameEvent 仍可能回调。回调要明确是否应更新隐藏 UI；不要在 OnRefresh 中反复注册，也不要用全局 Shutdown 代替局部取消订阅。

## 5. 控件绑定、命名与生成器

**入口**：[ScriptGenerator](../../UnityProject/Assets/Editor/UIScriptGenerator/ScriptGenerator.cs)、[ScriptAutoGenerator](../../UnityProject/Assets/Editor/UIScriptGenerator/ScriptAutoGenerator.cs)、[ScriptGeneratorSetting](../../UnityProject/Assets/Editor/UIScriptGenerator/ScriptGeneratorSetting.cs)、[UIBindComponent](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBindComponent/UIBindComponent.cs)。详细规则：[命名与绑定](../../UnityProject/.codex/skills/tengine-dev/references/naming-rules.md)。

- 路径绑定：在 ScriptGenerator 中调用 `FindChild(path)` / `FindChildComponent<T>(path)`；路径相对窗口/Widget 根，类型必须与真实组件一致。关键控件应检查 null，示例见 BattleMainUI.Require<T>。
- 非 BindComponent 模式：菜单 `GameObject/ScriptGenerator/UIProperty`、`UIPropertyAndListener`（及 UniTask 变体）读取当前选中层级，生成代码到剪贴板，不等于自动写业务类。
- BindComponent 模式由 `ScriptGeneratorSetting.Instance.UseBindComponent` 控制；`GenerateUIComponentScript()` 构建组件列表并保存 Prefab，`GenerateCSharpScript(...)` 生成带 UIBindComponent 索引读取的代码。索引列表与代码必须同一轮生成，不能手改顺序后沿用旧索引。
- 生成器读取配置中的组件规则/前缀，不要用旧 wiki 的前缀替代当前配置。HTML 生成也通过 `HtmlToUGUIBaker.MakeScriptGeneratorNodeName` 规范化子节点；以生成后实际层级为准。
- 当前 HUD 的真实约定为 `m_go*`、`m_text*`、`m_btn*`。旧节点名带 `m_tmp` 不能证明其组件真是 TMP；检查组件类型。
- 不论路径或索引绑定，都应保留手写业务实现、审阅生成差异；烘焙/生成脚本不会替你实现数据绑定、事件取消和异步生命周期。

## 6. UI 图片、实例与释放

**入口**：[SetSpriteExtensions](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Extension/Implement/SetSpriteExtensions.cs)、[ResourceExtComponent.Resource](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Extension/ResourceExtComponent.Resource.cs)、[SetSpriteObject](../../UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Extension/Implement/SetSpriteObject.cs)、[IUIResourceLoader / UIResourceLoader](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/IUIResourceLoader.cs)。更多资源契约：[资源 API 与所有权](../../UnityProject/.codex/skills/tengine-dev/references/resource-api.md)。

| 需求 | 优先方法 | 注意 |
|---|---|---|
| 为 Image/SpriteRenderer 切换图标 | `SetSprite(location, ...)` | 返回 void，内部异步；可传取消 token、Image回调和 setNativeSize；空地址直接返回，不是清图命令 |
| 切换子图片 | `SetSubSprite(location, spriteName, ...)` | 要同时确认资源地址和子图名，不能只传文件名猜图集 |
| 窗口/Widget实例加载 | `UIModule.Resource.LoadGameObject[Async]` | 默认UIResourceLoader转发IResourceModule；异步可带parent/token/packageName |
| 直接拿普通 Asset | 资源模块 `LoadAssetAsync<T>` 等 | 按资源模块契约配对 UnloadAsset；不要与SetSprite内部持有关系混为一谈 |

- SetSprite 复用已有请求替换/取消及缓存管理；不要再造全局图标请求字典。散图实现检查“当前请求”以避免旧结果覆盖新图。
- SetSpriteObject 在目标销毁、sprite清空或替换后可释放；单纯隐藏对象不等于解除图片持有。子图另有实现，不把散图竞争保证无条件推给所有加载API。
- UI加载器的 LoadGameObject 系列已实例化，实例Destroy时自动释放绑定资源；不要再对同一个实例做第二次Instantiate或额外按普通Asset路径Unload。
- 自己发起的异步工作要明确取消/结果过期条件；UIWindow不是MonoBehaviour，需要使用其实际gameObject生命周期或业务自持有CTS。

## 7. Canvas、字体、安全区与基础控件

**入口**：[UIModule.OnInit / ApplyScreenSafeRect](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs)、[UIWindow.SetUIFit / SetUINotFit](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs)、[SetUISafeFitHelper](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/SetUISafeFitHelper.cs)。

- UIModule 从场景 `UIRoot` 找子 Canvas/Camera，`UIModule.UIRoot` 实际指向该 Canvas Transform。复用它，不另外创建全局 UI 根或 EventSystem。
- 窗口资源根必须有 Canvas；UIWindow.Handle_Completed 会检查它并设置排序。交互窗口配 GraphicRaycaster。排序、全屏遮挡与 Visible 由 UIModule/Window 管理，不用任意 SetActive 代替整个窗口管理流程。
- Canvas排序层、UILayer业务层与GameObject.layer是三种概念。当前框架显示层为5（UI）、隐藏层为2（Ignore Raycast）；UI相机须可见对应层。
- **本 Demo 使用 UGUI Text，复用现有 LegacyRuntime 字体**。烘焙调用传 legacyText:true/legacyFont，控件绑定用 UnityEngine.UI.Text；字体引用存在不等于字形覆盖，中文要检查实际显示。
- 宿主 CanvasScaler 决定设计尺寸适配。坦克窗口临时使用1920×1080/match0.5并在销毁恢复原值，这是 Demo 窗口代码约定，不是所有窗口的全局默认。
- 异形屏先查框架 `ApplyScreenSafeRect`、`SetUIFit`/`SetUINotFit` 与 SetUISafeFitHelper；HTML safeArea标签目前不能替代运行时安全区逻辑。
- 普通 Button、InputField、Toggle、Slider、Dropdown、ScrollRect 复用 UnityEngine.UI 原生组件；本页没有发现需要新造通用按钮系统的理由。复杂列表/输入规则须另核对实际需求，不能仅凭HTML控件标签视为业务完整。

## 8. 非 Python 的 HTML → JSON → Prefab

**权威入口**：[HTML转换器](../../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/HtmlToJson/HTML%20转%20JSON%20坐标烘焙器.html)、[共享BakeAPI](../../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBakeApi.cs)、[Baker.GenerateTree](../../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBaker.cs)、[配置类型](../../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/HtmlToUGUIConfig.cs)。操作规则以已更新的 [html-to-ugui skill](../../UnityProject/.codex/skills/html-to-ugui/SKILL.md) 为准；[组件映射](../../UnityProject/.codex/skills/html-to-ugui/references/control-mapping.md)用于看实际控件拆分。

1. 写单命名根的被动 UI-DSL HTML（type/name、本地图片、明确尺寸），复用工程浏览器转换器测量并输出 JSON。转换器使用 DOM innerHTML，不是执行不可信网页的安全沙箱；只输入已审阅源稿。
2. 图片与字体准备完成后检查 JSON 的根尺寸、层级、文字、零值；浏览器里的CSS布局测量不等于原生LayoutGroup。字体文件不会自动转成Unity字体。
3. AIBridge `code execute --file .aibridge/code/<task>.csx` 调用项目 API；先 previewOnly:true，审阅目标路径、覆盖标志、计数、referenceResolution，再在授权范围内实际生成。
4. 入口位于Assets/Editor，由项目Git分发；`.aibridge/code`只是本地调用脚本。新增入口后先导入并 `compile unity`，再执行；单次串行，超时查询 `code status`，不重放或自动换backend。
5. API在PreviewScene生成并清理临时对象，只保存窗口根。输出目录须已经存在于Assets；默认拒绝覆盖，显式overwrite:true才替换旧内容。GUID保留不保证旧手工组件/fileID保留。
6. 通用API保留两种文字模式，默认legacyText=false。本Demo必须显式true并提供已有Font，不依赖TMP资源。UI层和根Canvas/GraphicRaycaster由API写入；宿主Scaler仍由接入方配置。

真实的只读预览调用（从UnityProject目录，脚本正文）：

```csharp
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
    "Assets/AssetRaw/UI/BattleMainUI.prefab");
var font = prefab.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
var json = System.IO.File.ReadAllText("../Books/UI/BattleMainUI.json");
return HtmlToUGUI.HtmlToUGUIBakeApi.Bake(json, "",
    "Assets/AssetRaw/UI/BattleMainUI.prefab", previewOnly: true,
    legacyText: true, legacyFont: font);
```

边界要读清：

- `canvasPathOrName` 非空时，必须唯一匹配已加载场景Canvas，只作尺寸/图层参考，不把生成物直接留在该场景Canvas下；空串使用临时Canvas。
- 预览只做输入校验；generatedObjectCount在预览为0。它不返回图片哈希或Importer差异，也不保证类型/排版/交互完整正确。
- API图片预检仅接收已导入Sprite的Assets/Packages路径，不是外部图片导入接口。**共享生成器仍调用 EnsureTextureImportedAsSprite，实际生成可能更改alphaIsTransparency等并SaveAndReimport**；必须检查涉及图片的Importer范围，不能承诺没有副作用。无图HUD不涉及该项。
- 当前控制映射支持div/image/text/button/input/scroll/toggle/slider/dropdown；v1固定坐标、v2锚点/布局意图，`cover`不等于浏览器裁切。完整转换和限制查上面的skill/reference，别新增另一套生成器。
- 输出Prefab还需真实框架Canvas下验收。动态绑定、按钮回调、初始隐藏状态、失败状态和资源释放不是JSON自动完成的功能。

## 9. Demo实例与不应照搬的部分

[当前BattleMainUI代码](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs)展示：路径绑定、Button监听配对、OnCreate后开战、Snapshot刷新、结果层切换、关闭停战与恢复Scaler。[GameApp](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/GameApp.cs)负责窗口加载入口。[HUD源稿](BattleMainUI.html)、[JSON](BattleMainUI.json)、[交付记录](README.md)用于查生成结构；交付记录包含历史阶段，不替代当前代码与最新测试证据。

- `BattleMainUI`使用hideTimeToClose:0，关窗口即停战，是Demo决策；不要默认所有背包/商店窗口都必须如此。
- Snapshot/RestartBattle及“1波3敌”等属于战斗模块，不是通用UI组件能力。
- [Launcher的UIBase](../../UnityProject/Assets/Launcher/Scripts/UIBase.cs)是启动界面体系；本页讲的GameLogic.UIBase在热更UIModule内。同名不代表相同生命周期，不跨程序集直接套用。

## 10. 开发前后最短检查

开发前：先选现有Window/Widget/资源辅助 → 确认资源地址与真实组件 → 确认隐藏/销毁和取消责任 → 再决定是否确实缺能力。

改动后按影响验证：绑定路径/组件/字体/图层，重复Show/Hide/Close/重开是否重复监听，异步过期是否覆盖新状态；布局变化看Unity实际截图。静态、编译、运行态分别报告，不用浏览器截图或旧报告代替本轮运行验证。

**本页验证范围**：源码逐项阅读、相对链接和关键符号检查、文档diff检查；文档任务不触发Editor编译/Play，不宣称新的运行验收。网络UI、复杂虚拟列表、第三方控件、国际化配置全流程未在本专题全面盘点，需要扩展时先查基础能力总索引和对应源码。
