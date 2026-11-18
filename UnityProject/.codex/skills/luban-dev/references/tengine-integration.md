# TEngine 集成与导出

输入：[luban.conf](../../../../../Configs/GameConfig/luban.conf)、[加载器模板](../../../../../Configs/GameConfig/CustomTemplate/ConfigSystem.cs)、[懒加载模板](../../../../../Configs/GameConfig/CustomTemplate/CustomTemplate_Client_LazyLoad/cs-bin/tables.sbn)。配置源从 UnityProject 出发为 ../Configs/GameConfig。

|模式|现有入口|产物|
|---|---|---|
|standard|[标准 .bat](../../../../../Configs/GameConfig/gen_code_bin_to_project.bat) / 同名 .sh|cs-bin + bin|
|lazyload|[懒加载 .bat](../../../../../Configs/GameConfig/gen_code_bin_to_project_lazyload.bat) / 同名 .sh|cs-bin + bin，首次访问加载整表|

两者均覆盖 UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig 与 Assets/AssetRaw/Configs/bytes，并复制 ConfigSystem.cs、ExternalTypeUtil.cs 到 GameProto。生产导出前预览输入、模式、模板和全部覆盖路径，取得相应授权。

## 工具事实与隔离校验

脚本固定从仓库 Tools/Luban/Luban.dll 调用 dotnet，不能只改 LUBAN_DLL 环境变量改变路径。2026-11-18 本地核对未找到该 DLL；`dotnet Tools/Luban/Luban.dll --help` 失败，因此未运行生成器、未声称导出通过。不自动安装或重建。

工具具备后，先依据本地 --help 核对源码脚本参数。脚本使用 `-t client -c cs-bin -d bin --conf ... -x code.lineEnding=crlf -x outputCodeDir=... -x outputDataDir=...`；懒加载另指定 `--customTemplateDir`。这是脚本事实，不代表本轮实际导出已执行。

隔离生成应将当前配置/模板与现有工具快照放到独立任务目录，保持 Configs/GameConfig、Tools/Luban、UnityProject/Assets 的相对结构，或基于已核实参数明确指定隔离输出。现有批处理先 copy 模板，故只改生成器输出参数不足以隔离整份批处理。生成前核对所有写入目标，不在生产目录随便切模式比较。

检查进程退出码、生成器日志、实际代码/bytes/模板和模式；批处理尾部 pause 不是成功证据。记录准确命令及输出，不能用旧文件冒充本次产物。生产执行见[操作规程](operating-guide.md)。完成后取得 Unity 编译与对应配置加载行为证据；代码、bytes、模板按同版本配对。

## 分片能力与资源归属

当前普通与整表懒加载脚本不等于按关卡分片。核对 exporter、模板和生成 API 后才判断功能；不要调用未证实的 LoadTbXPartition/ReleaseTbXPartition/ClearPartitions。

[fixture](../examples/regression/Defines/tables.xml) 保留 partition 标签需求样例；标签存在、生成器接受或旧记录成功均不证明分片文件、路由索引和释放 API 实现。需要分片时另行明确实施及兼容验收，不能用普通 bin 产物替代。

cs-bin 按生成布局读行；新字段不保证旧客户端兼容。资源句柄与托管表缓存归属不同，加载器限制见[运行时配置](runtime.md)。本轮文档清理不修补模板或生成产物。
