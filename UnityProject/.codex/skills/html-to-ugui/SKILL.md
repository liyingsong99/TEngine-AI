---
name: html-to-ugui
description: 使用工程浏览器转换器将 UI-DSL HTML 转为 JSON，再经 AIBridge 调用项目 Editor API 生成 UGUI Prefab。处理本地图片、控件、字体和布局；普通网站或仅改 UIWindow 逻辑不触发。
---

# HTML 到 UGUI

## 输入、产物与文字模式

先读 git status，明确需要 HTML、JSON 还是 Prefab，确认分辨率、字体、图片与输出路径。首读 [UI-DSL](references/ui-dsl-spec.md)，按需读 [JSON](references/json-schema.md) 与[控件映射](references/control-mapping.md)。只接收已审阅的被动 UI-DSL，不执行不可信脚本或拉取外部资源。

**当前坦克 Demo 使用 UnityEngine.UI.Text，1920×1080，明确 legacyText:true 并传工程已有 Font；不导入 TMP Settings 或 TMP 字体。** 通用 API 保留 TMP/UGUI 两种模式，其他任务按明确约定选择。CSS 字体不自动生成 Unity 字体资产，中文必须在实际 Unity 显示验证。

## 当前真实入口

- [HTML→JSON 浏览器转换器](../../../Assets/TEngine/Extension/HtmlToUGUI/HtmlToJson/HTML%20转%20JSON%20坐标烘焙器.html)。加载已审阅 HTML、检查设计尺寸、等待图片/字体完成，使用转换器自身烘焙功能输出 JSON，不另造转换器。
- [HtmlToUGUIBakeApi.cs](../../../Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBakeApi.cs) 的 Bake：json、canvasPathOrName、outputPrefabPath，另有 configPath、previewOnly、overwrite、legacyText、legacyFont。调用前再读当前签名。
- API 与窗口共用 HtmlToUGUIBaker.GenerateTree，在 PreviewScene 生成，不保存用户场景。不存在可直接照抄的旧自动化烘焙命令；按当前 API 调用。

## 执行顺序

1. 确保单个命名根、有效 type/name；检查零值、层级、尺寸、图片路径与文字。仅要求 HTML/JSON 时到此交付，不连接 Editor。
2. 需要 Prefab 时读取 [unity-cli](../unity-cli/SKILL.md)，明确目标与覆盖授权。源码若刚变更，先让 Unity 导入并 compile unity。
3. 编写 `.aibridge/code/<本任务>.csx`，从已确认路径读取 JSON，加载配置及字体，调用 Bake。先 previewOnly:true 读回目标、覆盖状态、节点/图片数和 referenceResolution。
4. 沿用有效覆盖授权后调用 previewOnly:false；已有目标必须 overwrite:true。不要自动覆盖未知资源。调用层使用 `code execute --file ...` 串行执行；超时查 code status，不重放或换 backend。
5. 读回 Prefab 路径、GUID、组件及层级；验证 Slider=0、文字模式、中文、按钮与宿主 Canvas 下的布局。浏览器图不能替代 Unity 图。

API 当前要求输出位于 Assets 下已存在目录；图片预检要求已导入的 Assets/Packages Sprite，不提供外部图片复制入口。共享生成器仍调用 EnsureTextureImportedAsSprite，可能修改 alphaIsTransparency 等 importer 设置并 SaveAndReimport，使用图片时须核对并纳入授权范围，不能承诺生成无 importer 副作用。新目录/图片准备另列范围。预览不返回图片内容哈希或 Importer 差异，也不证明完整运行正确性。

保存的是窗口根，不保存临时宿主 Canvas；窗口根含 Canvas/GraphicRaycaster。Demo 由 BattleMainUI 加载到现有 UIRoot/UICanvas，不新增全局 UI 根。同路径覆盖保留资产 GUID 不代表手工内容与所有 fileID 保留。

v1 固定坐标、v2 anchor/layoutHint 按当前实现；safeArea 不等于已有运行时安全区组件。详细数据约束见 references，不能只凭标签宣称功能完成。

## 失败与交付

空输入、非法路径、重复节点、无效图片、所选文字模式缺字体均明确失败；不得切换文字模式掩盖问题。中断先查真实资产，不自动回滚用户文件。

交付 HTML/JSON、预览与生成结果、Prefab 路径/GUID、实际 Text 类型、截图及验证范围。源码和共享说明放正常 Git 范围；`.aibridge` 仅本机证据与调用脚本。静态、编译、运行态分开报告。
