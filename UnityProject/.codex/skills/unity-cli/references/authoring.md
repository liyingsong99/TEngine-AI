# 内容操作与项目 API

先查本地 AIBridge help 与目标源码。Unity 对象、序列化及资源数据库通过 Unity API；普通源码通过编辑工具，不手写 Scene/Prefab YAML、GUID 或 meta。

## 找到目标再写入

- 资源：asset search/find，按 help 提供 filter 与 format；先确定实际路径。
- 场景：scene get_active/get_hierarchy；场景只列当前根不等于持久对象不存在。
- 对象：gameobject find；组件/字段：inspector get_components/get_properties/get_property。
- Prefab：prefab get_hierarchy/get_info；查询参数先查 action help。

instanceId 仅在本次实例/重载周期有效。同名路径可能不唯一；先解析唯一目标，再使用核实过的路径、assetPath/objectPath 或 instanceId。

预览列目标、原值/新值、层级关系、输入与写盘/覆盖范围。保留脏场景、实例 overrides 和用户修改，不自动 SaveAll。写后重查组件、引用、GUID 及持久化结果。

|需求|路径与验收|
|---|---|
|少量字段|inspector set_property/set_properties；前后读回|
|Transform/对象|对应命令；确认父子关系、作用域和释放|
|复杂 Prefab|按 aibridge-prefab-patch 技能查看当前 patch schema、预检与应用；不猜 save_contents 等旧工具 action|
|导入/Importer|AssetDatabase API，核对路径/GUID/导入设置，不改 meta|
|Scene|明确是否加载/保存、哪些脏内容；不以资源存在替代场景内部验收|
|HTML/UI-DSL|[html-to-ugui](../../html-to-ugui/SKILL.md)，由共享烘焙器生成，不手工重搭 UI 树|

## 批量与失败

多条 Unity 操作使用已安装 aibridge-batch-script 技能，先审核每项 command/参数/依赖。批处理外层成功不覆盖单项失败；不默认有全工程事务。Undo 不普遍撤销资产写盘、设置、构建或导入。

写入中断后查实际目标和原状态，不自动删除产物或恢复整个工作树。等待与测试/构建状态的 ID 各有契约，不能套用旧 job wait 接口。

## 可复用项目入口

重复使用的项目操作放项目 Editor-only 公共 API，窗口与自动化共享同一实现；不要把项目领域知识塞入通用 AIBridge 包，也不要复制生成逻辑。核对 asmdef 和 Editor 边界。

一次性调用脚本放 `.aibridge/code`，通过 code execute 串行执行；新增 API 先由 Unity 导入并 compile unity，再调用。解析输入、限定路径、明确覆盖选项，失败应明确抛错或返回失败，清理临时对象，不自动保存用户场景。

可复用代码必须位于正常版本管理范围；`.aibridge` 被忽略，不能作为共享实现的唯一落点。Git 纳入、提交与发布是不同动作，按授权执行。
