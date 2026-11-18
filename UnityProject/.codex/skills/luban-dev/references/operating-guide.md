# 配置源操作与导出

配置源为仓库根 Configs/GameConfig。直接核对 luban.conf、Defines、Datas 与 CustomTemplate，不依赖退役的表编辑助手。

## 读取与修改

1. 从表注册确定目标名称、数据文件、主键与客户端/服务端组；不能凭字段名推断。
2. 使用已有 Excel 编辑器或明确可用的非 Python 表格工具读取实际 Sheet/多行表头；纯文本定义用代码工具编辑。工具不能保留结构时停止自动批量改写，不退化成平铺 CSV 覆盖。
3. 行号与主键不同。明确 0/false/空字符串/null，保留公式、格式、注释与非目标单元格。
4. 删除枚举/Bean/字段前搜索所有引用与消费者，说明兼容性；不跳过引用校验。
5. 修改后读回目标并检查 diff；最终 Schema/类型/引用以真实 Luban 导出和代码消费验证为准。

## 可执行入口

先读实际[标准脚本](../../../../../Configs/GameConfig/gen_code_bin_to_project.bat)和[懒加载脚本](../../../../../Configs/GameConfig/gen_code_bin_to_project_lazyload.bat)。脚本会覆盖生产产物与复制模板，不是只读校验。

取得生产覆盖授权、工具就绪后，从仓库根使用对应入口：

```powershell
# 只选择批准的模式；AI_MODE 仅跳过脚本末尾 pause，不是安全/预检参数。
$env:AI_MODE = '1'
cmd.exe /d /c Configs\GameConfig\gen_code_bin_to_project_lazyload.bat
```

标准模式使用同目录 gen_code_bin_to_project.bat。不得同时跑两种模式覆盖同一目录；用完恢复此前 AI_MODE 环境值。

隔离校验与实际输出路径见[集成规程](tengine-integration.md)。工具缺失时只报告阻塞，不能用文件存在或旧生成物证明当前导出成功，不自动下载/升级工具。
