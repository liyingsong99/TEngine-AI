---
name: luban-dev
description: 维护本仓库 Luban 游戏配置的表、数据、枚举、Bean、模板、校验和普通/整表懒加载导出，核实分片需求的实际能力。普通应用设置、Unity ProjectSettings 或仅讨论游戏技能设计不触发。
---

# Luban 开发

## 定位与任务终点

先读 git status、表注册、字段、数据、引用和当前导出脚本，确定目标表/主键、
客户端/服务端分组、导出模式与兼容性。不要按字段关键词推断业务分组。
源位于仓库 `Configs/GameConfig`，从 Unity 项目出发为 `../Configs/GameConfig`；
命名空间为 `GameConfig`。修改生成 C#/bytes 时回溯源表或模板。

| 用户目标 | 执行到哪里 |
|---|---|
| 查表、解释结构或讨论方案 | 只读源数据与实际实现，不导出、不连接 Editor |
| 修改源记录/字段/模板 | 按授权编辑，检查引用和 diff，再以真实生成器隔离校验 |
| 更新生产生成产物 | 展示输入、模式和覆盖范围，按有效授权执行现有导表脚本 |
| 运行时加载问题 | 读真实加载器/生成类；组合 tengine-dev，实际运行验证再用 unity-cli |
| 按关卡/区间分片 | 先查 exporter、模板与生成 API，当前未实现，不能直接套 tags 或宣称完成 |

普通源修改不自动扩大到生产覆盖；删除/重命名需说明兼容性影响并取得对应确认。
不安装工具、不重建 Luban，不用普通导出掩盖分片要求。

## 校验与生产导出

1. 直接读源表、`luban.conf`、模板和生成产物核对结构；复杂结构先看实现，不使用会破坏格式的扁平 CRUD。
2. 校验与导出通过仓库现有 Luban 工具（`Tools/Luban`）执行，输出写到临时目录或 runs，不写生产产物。
   工具路径与覆盖行为先读[现有脚本](references/tengine-integration.md)；本地工具缺失则报告阻塞，不自动安装。
3. 需要生产导出时先展示配置输入、模板、生成 C# 和 bytes 的覆盖范围，说明将覆盖哪些文件。
4. 在具体确认后执行导出；范围或输入改变则重新预览。
5. 核对实际产物、模板与模式，随后按生成改动取得 .NET、Editor 及运行证据。

当前两个脚本均使用 `-d bin`。`lazyload` 增加整表懒加载模板，**不是分片导出**；
没有经源码证实的 `LoadTbXPartition/ReleaseTbXPartition/ClearPartitions` API。
分片是另一个需实现和验收的能力，详情见集成参考。
隔离导出证据只覆盖实际输入与模式，不代表分片或所有业务配置通过。

## 按需读取

| 任务 | 参考 |
|---|---|
| 项目导出、加载器和分片能力边界 | [tengine-integration.md](references/tengine-integration.md) |
| 源表修改、导表操作 | [operating-guide.md](references/operating-guide.md) |
| 类型和 Schema | [type-system.md](references/type-system.md)、[schema.md](references/schema.md) |
| 配置文件、生成参数 | [luban-conf.md](references/luban-conf.md)、[command-reference.md](references/command-reference.md) |
| 数据源 | [excel-format.md](references/excel-format.md)、[data-sources.md](references/data-sources.md) |
| 校验和运行时 | [validators.md](references/validators.md)、[runtime.md](references/runtime.md) |

配置源检查和隔离导出不必连接 Editor；实际生成 C#/资源后按项目
[unity-cli](../unity-cli/SKILL.md) 验证，框架接入读取 [tengine-dev](../tengine-dev/SKILL.md)。
交付数据/模板 diff、引用与兼容性、模式、日志、产物及实际验证范围。
失败保留输入和日志，不回滚用户已有数据，不将缺失产物或 Editor 离线写成通过。
