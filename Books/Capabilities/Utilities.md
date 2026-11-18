# 集合、数值与基础工具

[返回能力索引](../AI-Capabilities.md)。`Utility` 是按职责拆分的静态 partial 类，不是所有能力都已注册可用的服务。

## 集合、时间、随机和数学

|何时复用|入口|限制|
|---|---|---|
|一键多个监听/对象|[GameFrameworkMultiDictionary<TKey,TValue>](../../UnityProject/Assets/TEngine/Runtime/Core/DataStruct/GameFrameworkMultiDictionary.cs)：Add、Contains、TryGetValue、Remove、索引器|值为 GameFrameworkLinkedListRange，Count 是键数；非线程安全集合，不把 range 当独立 List 缓存|
|高频链表节点增删|[GameFrameworkLinkedList<T>](../../UnityProject/Assets/TEngine/Runtime/Core/DataStruct/GameFrameworkLinkedList.cs)：AddFirst/AddLast/AddBefore、Remove、Clear、CachedNodeCount|内部复用 LinkedListNode；归还节点后不能继续使用旧节点引用，不承诺零分配|
|普通映射/列表/集合|System.Collections.Generic 的 Dictionary/List/HashSet/Queue|无特殊需求不造 CollectionHelper；本项目大量直接使用 BCL 集合|
|本帧时间|[GameTime.StartFrame 与字段](../../UnityProject/Assets/TEngine/Runtime/Core/GameTime/GameTime.cs)：time/deltaTime/unscaledDeltaTime/fixedDeltaTime/frameCount/unscaledTime|由 RootModule 采样 Unity Time；不是服务器时间、UTC 或持久时间戳；字段实际可写，但业务不应改采样值|
|插值、向量、角度、Clamp|UnityEngine.Mathf/Vector2/Vector3/Quaternion；[TankUnit](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/TankBattle/TankUnit.cs) 与 [World](../../UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/TankBattle/TankBattleWorld.cs) 为当前调用例|Demo 的运动参数不当通用数学工具。纯数值另可用 System.Math|
|随机采样|UnityEngine.Random 或独立 System.Random（Unity/.NET 标准能力）|本次在 Assets/TEngine 与 GameScripts 未定位通用随机封装；不据此断言全工程不存在。确定性回放需明确种子/实例/状态，不把全局随机当隔离状态|

## JSON、字符串、转换

|需求|入口与来源|关键边界|
|---|---|---|
|框架 JSON|[Utility.Json.ToJson/ToObject/SetJsonHelper](../../UnityProject/Assets/TEngine/Runtime/Extension/Json/Utility.Json.cs)|需要初始化 helper；RootModule 负责配置默认帮助器|
|默认 JSON 实现|[DefaultJsonHelper](../../UnityProject/Assets/TEngine/Runtime/Extension/Json/DefaultJsonHelper.cs)|实际使用 Unity JsonUtility，settings 参数未传给底层；不要假设字典、多态或 Newtonsoft 设置生效|
|复杂 JSON 已有第三方|Newtonsoft.Json，依赖见[manifest](../../UnityProject/Packages/manifest.json)，实际用例 [HtmlToUGUIBakeApi](../../UnityProject/Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBakeApi.cs)|它与 Utility.Json 是不同入口，业务使用先核对 asmdef、目标平台和序列化契约，不全局替换 helper|
|框架字符串格式化|[Utility.Text.Format/SetTextHelper](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Text.cs)、[DefaultTextHelper](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/DefaultHelper/DefaultTextHelper.cs)|跟随日志/本地化现有用法；helper 初始化前调用会失败，不因泛型重载宣传绝对零 GC|
|原始字节/数值/编码/DPI|[Utility.Converter](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Converter.cs)：GetBytes、GetInt32/GetSingle、GetString、像素/厘米换算|显式检查 buffer、offset、长度、编码与布局；这是原始转换，不是网络协议自动编解码|
|结构体与非托管缓存|[Utility.Marshal](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Marshal.cs)：StructureToBytes、BytesToStructure、EnsureCachedHGlobalSize、FreeCachedHGlobal|核对结构体布局/平台；静态缓存不要假设可并发。一般配置直接用已有 Luban ByteBuf，不重复转结构体|

## 文件、路径和偏好存储

- [Utility.Path.GetRegularPath/GetRemotePath](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Path.cs)：路径规范化与远程形式；**不是**路径授权/越界防护。RemoveEmptyDirectory 会写文件系统，不能当纯查询。
- [Utility.File](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.File.cs)：CreateFile、Md5ByPathName、GetFileSize、GetByteLengthString 等。注意带 info 的 CreateFile 对现有文件是 AppendText，非覆盖写；空文件重载的 File.Create 返回流未在此函数中 Dispose，使用前注意句柄归属。本知识库不修代码。
- 同文件的 GetPersistentDataPlatformPath 名称容易误导：实现使用 streamingAssetsPath，Android Player 用 dataPath+`!assets`；**不是 Application.persistentDataPath 可写存档路径**。真正持久写入先明确平台目录与权限。
- MD5 用于完整性/缓存比较，不当安全签名或机密保护。文件写入不要借路径工具覆盖用户资源。
- [Utility.PlayerPrefs](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.PlayerPrefs.cs)：Get/SetInt/Float/String、Set/GetBool、HasKey、Save，适合少量偏好；SetInt/Float/String 默认 save=true，频繁设置可按签名传 false 并集中 Save。
- SetUserId 与 Get/SetUser* 使用 `userId_key`，不等于加密或完整账号存档。普通 Get/Set/DeleteKey 不自动带用户前缀；DeleteAll 影响全部偏好，不能只为清当前角色调用。

```csharp
Utility.PlayerPrefs.SetInt("music_enabled", 1, save: false);
Utility.PlayerPrefs.Save();
```

## 反射、Tween 与材质辅助

|能力|入口|使用条件|
|---|---|---|
|程序集类型查询|[Utility.Assembly.GetAssemblies/GetTypes/GetType](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Assembly.cs)|启动/生成器用途；新增热更程序集后的缓存与裁剪需核对，不以反射代替清晰业务接口|
|字段/属性/方法反射|[Utility.Reflection](../../UnityProject/Assets/TEngine/Runtime/Core/Utility/Utility.Reflection.cs)：InvokeMethod、Get/SetFieldValue、Get/SetPropertyValue|核对重载与访问标志；低频工具可复用，不在热路径反复字符串查找，不假设异常被吞掉|
|Tween 门面|[Utility.Tween](../../UnityProject/Assets/TEngine/Runtime/Extension/Tween/Utility.Tween.cs)、[ITweenHelper](../../UnityProject/Assets/TEngine/Runtime/Extension/Tween/ITweenHelper.cs)：SetTweenHelper、Delay、Scale、Stop、Complete|帮助器初始 null；本次 Assets 搜索只找到 SetTweenHelper 声明，未确认实现注册。需要先落实接入，不能把门面当作已可用的动画库；普通延时已有 Timer/UniTask|
|Editor 材质 Shader 修复|[Utility.MaterialHelper](../../UnityProject/Assets/TEngine/Runtime/Extension/Material/Utility.MaterialHelper.cs)|主体受 UNITY_EDITOR && EditorFixedMaterialShader 控制；会操作 sharedMaterials/Shader，不是 Player 通用运行组件，也不应随意调用修 UI|

未核对专门的全局数学/随机/数据库封装、所有第三方扩展。遇到目录未列出的需求，先搜真实类与调用方，不能以本索引缺条目认定没有实现。
