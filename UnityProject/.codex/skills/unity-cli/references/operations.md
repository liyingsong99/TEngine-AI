# 连接、发现与执行

以下示例从 UnityProject 执行，`$CLI = './.aibridge/cli/AIBridgeCLI.exe'`。不自动启动或安装 Editor。

## 目标绑定

1. 读取 git status、ProjectVersion、包清单和任务范围；保留已有修改。
2. 核对本项目绝对路径与唯一 Editor，随后 `editor get_state` 读取编译、导入、Play 状态。响应里的 Editor 安装路径不等于项目路径，须结合项目本地 CLI 与进程身份。
3. 新编译、导入、测试及写入前确保状态适合操作，并串行使用 Editor；状态观察无需空闲。
4. 不读取或输出鉴权描述文件，不连接其他工程替代验证；弹窗不自动保存、丢弃或关闭。

## 本地 help 与查询示例

```powershell
& $CLI --help
& $CLI prefab --help
& $CLI inspector get_property --help
& $CLI scene get_active
& $CLI scene get_hierarchy --depth 3
& $CLI asset find --filter 't:Prefab' --format paths
```

help 确认命令语法，实际目标仍需执行查询并核对。当前场景查询 action 是 get_active/get_hierarchy；asset find 必须提供 filter。不要照搬其它工具的 help 参数或场景命令。

复杂参数先检查是否支持 `--stdin`，批量任务按已安装 aibridge-batch-script 规程；不要自行改字段命名。默认后端与可选 Pipeline 后端按本地 help 区分，缺失能力不能靠换旧 CLI 路径绕过。

## Editor C#

先读取实际项目公共 API。长脚本写到 `.aibridge/code` 下的 `.csx/.cs`；正式可复用实现放正常版本管理目录，脚本只是调用层。

```powershell
& $CLI code execute --help
# task.csx 必须是本任务已创建、审阅的脚本。
& $CLI code execute --file .aibridge/code/task.csx --timeout 30000
```

native 默认执行；Pipeline backend 默认 dry-run，实际运行要求该后端的明确参数。不得将编译预检当作运行验收。code execute 单次串行；超时先 `code status`，查原请求和实际写入结果，不重放、不自动换后端。

## 授权与恢复

预览应列项目、输入、对象/资源、覆盖/删除及保存范围。沿用会话中有效授权，范围变化才补确认。无 dry-run 时先静态审查，不编造预览成功。某些 Unity API 会保存全局脏资源，执行前检查真实实现。

|情况|处理|
|---|---|
|零实例/错误项目/本项目多实例|报告阻塞，不借用其它项目|
|正在导入/编译/重载|只读观察原操作，不重复发起|
|写入超时|结果未知；查原请求、路径、GUID 与对象实际状态|
|内层 failed/canceled/error|报告失败，不能只用进程退出 0 验收|
|零测试或结果不属于本轮|报告未验证，核对筛选器和运行身份|

不自动删除缓存/锁或回滚工作树。详细验证见[验证规程](verification.md)，对象写入见[内容操作](authoring.md)。
