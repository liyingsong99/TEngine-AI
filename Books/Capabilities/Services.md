# 配置、通信与基础服务

[返回能力索引](../AI-Capabilities.md)。本页分清现有封装、第三方组件和未确认的接入；不因为接口名字存在就保证完整运行能力。

## 配置与 Luban

|用途|入口|约束|
|---|---|---|
|修改表定义/数据/类型|仓库根 [Configs/GameConfig/luban.conf](../../Configs/GameConfig/luban.conf)、[Defines](../../Configs/GameConfig/Defines/)、[Datas](../../Configs/GameConfig/Datas/)|不在 UnityProject/Configs 新建副本；表注册、组、引用和主键从实际源查|
|维护运行时加载器|[ConfigSystem 源模板](../../Configs/GameConfig/CustomTemplate/ConfigSystem.cs)：Instance、Tables、Load、LoadByteBuf|模板中 Load 构造 Tables，LoadByteBuf 同步取 TextAsset 再构造 Luban.ByteBuf；修改追溯模板，非直接补丁生成产物|
|普通导表|[gen_code_bin_to_project.bat](../../Configs/GameConfig/gen_code_bin_to_project.bat)|调用现有 Luban.dll，cs-bin + bin，复制两个加载辅助模板并覆盖 GameProto/bytes；不是只读校验|
|整表懒加载|[lazyload 脚本](../../Configs/GameConfig/gen_code_bin_to_project_lazyload.bat)、[tables.sbn](../../Configs/GameConfig/CustomTemplate/CustomTemplate_Client_LazyLoad/cs-bin/tables.sbn)|首次访问加载整表；不证明分片文件/路由/缓存 API 存在|

ConfigSystem 模板当前没有对加载的 TextAsset 配对 UnloadAsset；不要把它当资源释放完善的范例，也不要在文档任务顺手改模板。实际生成类与本次配置输出是否存在需查磁盘，不能只凭模板推断已可访问某张 TbX。

工具与覆盖流程沿用 [luban-dev](../../UnityProject/.codex/skills/luban-dev/SKILL.md) / [集成规程](../../UnityProject/.codex/skills/luban-dev/references/tengine-integration.md)。本次知识盘点未执行导出、不安装工具；本地工具可用性与配置加载属于另外的验证。一般可调小参数无需先扩配置框架，需求确认后再用现有表体系。

## HTTP 与网络边界

[Utility.Http](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Http.cs) 是当前确认的项目 HTTP 封装，底层 UnityWebRequest + UniTask/协程。

|需求|方法|先注意|
|---|---|---|
|简单文本 GET|Get(string url, float timeout=5f) → UniTask<string>|内部创建 timeout CTS，没有调用方 token 参数；取消可返回空字符串，不能把空字符串直接当有效结果|
|表单 POST|Post(url,string/Dictionary<string,string>/WWWForm,timeout)|string 重载在当前 Unity 使用 PostWwwForm，不是 JSON application/json 上传接口；请求体语义不要猜|
|自己构造请求与取消|SendWebRequest(UnityWebRequest,CancellationTokenSource)|此 helper 会 Dispose 请求，调用方不应在 await 后再访问它。非取消的网络/协议异常没有统一业务错误模型；需要调用链观察失败|
|旧回调 GET/下载/纹理/音频|Get(url,Action<UnityWebRequest>)、DownloadFile、GetTexture、GetAudioClip|不同协程的失败回调和 Dispose 行为不统一，结果 Texture/AudioClip 归属要看使用场景；不宣传为全自动资源管理|
|PUT 上传|UploadByPut|声明存在，但当前 OnUploadByPut 未设置请求 url/method，不能直接推荐为已验证上传能力；需求涉及上传时先最小修复并验证，不另造整套网络层|

现有 [网络手册](../3-8-网络模块.md) 只有“待补充”。在本次检查的 Assets/TEngine 与 GameScripts 中没有确认 Socket/TCP/WebSocket 会话、RPC、重连或统一 GameModule.Network 接口；AIBridge 的通信是 Editor 工具链，不是业务网络 SDK。第三方包、其他仓库或未来服务实现未穷举，不能概括成“项目绝无网络能力”。

网络请求不要直接套 UI 生命周期：窗口隐藏不销毁，必须明确业务结束、取消与晚到响应归属。HTTP 认证、重试、幂等和协议消息应按已确认需求接入，现有 helper 不等于这些功能均具备。

## 日志与调试

- [TEngine.Log](../../UnityProject/Assets/TEngine/Runtime/Core/Log/Log.cs)：Debug/Info/Warning/Error/Fatal 等，复用框架输出，不到处创建独立日志文件器。
- [GameFrameworkLog](../../UnityProject/Assets/TEngine/Runtime/Core/Log/GameFrameworkLog.cs) 与 [ILogHelper](../../UnityProject/Assets/TEngine/Runtime/Core/Log/GameFrameworkLog.ILogHelper.cs)：底层帮助器契约；RootModule 初始化帮助器。日志级别不自动替业务恢复/抛异常语义，核对实际调用。
- [DefaultLogHelper](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/DefaultHelper/DefaultLogHelper.cs)：现有实现与格式；不把 Debug 输出当持久审计保证，不记录密钥或连接描述文件。
- [IDebuggerModule](../../UnityProject/Assets/TEngine/Runtime/Module/DebugerModule/IDebuggerModule.cs) 经 GameModule.Debugger，配合现有 [Debugger 组件目录](../../UnityProject/Assets/TEngine/Runtime/Module/DebugerModule/Component/)；资源/内存/对象池面板是诊断工具，不在业务中另搭全局调试 UI。
- AIBridge get_logs、compile unity、code execute 属于开发验证，使用[现有工作流指南](../AI-Development-Workflow.md)。没有 Error 不等于业务通过，文档任务不触发 Unity。

## 音频、语言与设置组件

|场景|复用入口|生命周期/限制|
|---|---|---|
|背景音乐、音效、UI 音、语音|[IAudioModule.Play/Stop/StopAll](../../UnityProject/Assets/TEngine/Runtime/Module/AudioModule/IAudioModule.cs)，GameModule.Audio|Play(type,path,bLoop,volume,bAsync,bInPool) 返回 AudioAgent；不要虚构 PlaySoundAsync。音频地址必须在资源系统有效|
|音量/开关与预载|同接口 Volume/MusicVolume/SoundVolume/UISoundVolume/VoiceVolume、PutInAudioPool/RemoveClipFromPool/CleanSoundPool|按 AudioType 分组；全局 StopAll/CleanSoundPool 会影响他人声音，不用于普通局部按钮退出|
|音频初始化|[AudioModule.OnInit/Initialize](../../UnityProject/Assets/TEngine/Runtime/Module/AudioModule/AudioModule.cs)|读取 Settings.AudioSetting.audioGroupConfigs、AudioMixer 和资源模块；配置/混音器缺失不能由接口存在保证可播放|
|切换语言/加载语言表|[ILocalizationModule](../../UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/ILocalizationModule.cs)：LoadLanguageTotalAsset、LoadLanguage、SetLanguage、CheckLanguage|通过 GameModule.Localization；设置当前语言与加载资源是不同步骤，无 token 参数不编造取消重载|
|查译文|[LocalizationManager.GetTranslation](../../UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Manager/LocalizationManager_Translation.cs)|依赖语言源/term 与参数；不虚构 GameModule.Localization.GetText。更改语言之后 UI 刷新要验证|
|组件式本地化|[Localize](../../UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Localize.cs)|按实际 target/语言源配置；本地化存储、协程辅助是该子系统实现，不能随意当作全项目 SaveSystem|
|启动设置|[Settings 目录](../../UnityProject/Assets/TEngine/Runtime/Module/Settings/)、[RootModule](../../UnityProject/Assets/TEngine/Runtime/Module/RootModule.cs)|设置对象与框架根由已有启动 prefab 承载；不每个功能创建第二个 RootModule 或重复初始化资源包|

## 边界提示

当前包清单有 Newtonsoft、HybridCLR、UGUI、UI Extensions、AIBridge；是否存在本地安装目录、可用 API、所需 asmdef 引用、目标平台支持需进一步核对。该知识库未完整验证 UI Extensions 控件、数据库、存档迁移、账号登录、服务器或加密协议，不作默认可用承诺。使用已有基础能力能满足时优先复用；确需扩展时先写清为什么现有接口不足。
