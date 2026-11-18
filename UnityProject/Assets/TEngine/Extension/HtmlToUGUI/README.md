# HTML → UGUI（Editor API）

本入口属于 TEngine-AI 工程，不依赖 AIBridge 编译程序集；AIBridge 仅作为调用工具。

## 输入

1. 用 UI-DSL 编写本地 HTML：单个命名根、生成节点声明 `data-u-type` 与 `data-u-name`，设计尺寸默认 1920×1080。
2. 在浏览器打开本目录 `HtmlToJson/HTML 转 JSON 坐标烘焙器.html`，粘贴 HTML，点击“烘焙并复制 JSON”，保存输出。
3. 当前坦克 Demo 明确使用 UGUI Text：传 `legacyText: true` 和已有窗口的 `legacyFont`，不需要 TMP Settings/字体。通用 API 保留 TMP 默认模式供其他调用方选择；仅选择 TMP 时检查其 Settings/默认字体，不自动改变文字模式。

## 可复用调用

```csharp
var window = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
    "Assets/AssetRaw/UI/BattleMainUI.prefab");
var font = window.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
var preview = HtmlToUGUI.HtmlToUGUIBakeApi.Bake(
    json, "", "Assets/AssetRaw/UI/ExampleUI.prefab", previewOnly: true,
    legacyText: true, legacyFont: font);
// 审阅路径、覆盖标记、节点/图片数量后执行：
var result = HtmlToUGUI.HtmlToUGUIBakeApi.Bake(
    json, "", "Assets/AssetRaw/UI/ExampleUI.prefab", legacyText: true, legacyFont: font);
```

- `canvasPathOrName` 为空时创建临时 Canvas；指定名称/层级路径时必须唯一匹配一个已加载场景 Canvas，仅取尺寸作为参考，不修改该 Canvas。
- 生成树只存在于 PreviewScene；保存窗口根（带 Canvas、GraphicRaycaster），临时宿主、窗口对象及 PreviewScene 在 finally 中清理，不保存当前场景。
- 窗口与 API 都调用 `HtmlToUGUIBaker.GenerateTree`，继续使用既有控件、命名和布局实现。
- 输出目录须已存在且位于 Assets 下。已有 Prefab 默认拒绝覆盖；确认后显式传 `overwrite: true`。保存保留资产 GUID，但生成树会替换旧内容，不能保证旧节点 fileID 或手工添加组件保留。
- 返回 `prefabPath`、`guid`、`nodeCount`（DSL 节点）、`generatedObjectCount`（含 Button/Slider 等辅助节点）、`imageCount`（图片引用节点）、`referenceResolution`、`previewOnly`、`overwritesExisting`。
- 预览只检查输入，不生成对象；`generatedObjectCount` 在预览时为 0，不表示最终没有对象。
- API 当前图片范围：已导入为 Sprite 的 `Assets/...` 或 `Packages/...` 路径。外部文件/相对图片路径须先经 Unity 正常导入，再把 JSON 图片字段映射到资产路径；API 不复制图片、不修改 importer。交互窗口保留原有图片导入行为。
- CSS 字体不会自动变成 TMP 字体，默认字体不保证中文等全部字形。检查实际 Unity 字体覆盖、CanvasScaler 和 Game View，浏览器截图不证明 Unity 显示正确。
- 宿主 CanvasScaler 使用返回的 referenceResolution、ScaleWithScreenSize 和 matchWidthOrHeight=0.5；正式接入由 UIWindow 所属 Canvas 管理。

## AIBridge 调用

将一次性脚本放入 `.aibridge/code/example-ui.csx`：

```csharp
var json = System.IO.File.ReadAllText("本地 JSON 路径");
var window = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/AssetRaw/UI/BattleMainUI.prefab");
var font = window.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
return HtmlToUGUI.HtmlToUGUIBakeApi.Bake(
    json, "", "Assets/AssetRaw/UI/ExampleUI.prefab", previewOnly: true,
    legacyText: true, legacyFont: font);
```

从 UnityProject 目录依次运行：

```powershell
& ./.aibridge/cli/AIBridgeCLI.exe asset refresh
& ./.aibridge/cli/AIBridgeCLI.exe compile unity --timeout 120000
& ./.aibridge/cli/AIBridgeCLI.exe code execute --file .aibridge/code/example-ui.csx --timeout 30000
```

确认编译无 error 后才能执行。预览通过后在脚本中取消 previewOnly，再单次执行生成。超时先 `code status` 查询原请求，不重放、不切换 backend。脚本放在被忽略的 .aibridge 下；可复用 API 和本文位于 Assets，随仓库分发。不要手写 Scene/Prefab YAML 或 .meta。
