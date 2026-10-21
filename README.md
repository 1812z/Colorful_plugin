# 七彩虹 B760M 的 Artemis ARGB 插件

将本机已经可用的 `chinese-motherboard-signalrgb-plugin/Colorful_Motherboard_Controller.js` 中的 HID 控制协议移植为 Artemis 的原生 C# 设备插件，直接控制主板 USB 灯光控制器。

## 适配硬件

- 主板：**BATTLE-AX B760M-PLUS S WIFI7 V20**（已通过本机 SMBIOS 确认）。
- USB 控制器：**VID 2F4C / PID 1017**。
- 灯光端点：接口 1 或 2，Usage Page `FF01` / Usage `0001`，65 字节输出报告；本机实际使用 **MI_01**。
- 三个 ARGB 口分别显示为 `Colorful BATTLE-AX B760M-PLUS S WIFI7 ARGB 1/2/3`，每口 60 个可独立寻址的灯珠。
- 此项目按上述具体型号适配；其他 B760M 型号可能使用不同的灯珠偏移，不能仅凭芯片组名称判定兼容。

## 安装与使用

已生成的插件位于：

```text
src\Artemis.Plugins.Colorful\bin\Release\net10.0\
```

本次开发已通过官方 BuildTask 部署到：

```text
C:\ProgramData\Artemis\Plugins\Artemis.Plugins.Colorful-5ab22b31\
```

1. 退出 SignalRGB，并停止 `iGameCenter.Service.exe`，避免多个软件同时向同一控制器发送灯光帧。
2. **退出并重新启动 Artemis**，让它扫描新安装的插件。
3. 在 **Settings → Plugins** 中找到并启用 **Colorful B760M ARGB**。
4. 在设备设置中启用三个 ARGB 灯带，将它们安排到灯光画布中，然后应用灯效。每个接口的 LED 顺序为 1–60。
5. 可以用纯红、纯绿、纯蓝分别测试三个设备，确认软件通道与主板上物理接口的对应关系。

每口固定提供 60 个灯珠。连接更短的灯带时，在 Artemis 中只选取实际存在的灯珠制作效果；多余地址仍会发送。插件使用原脚本默认的 **RGB** 色序。禁用插件、退出 Artemis 或挂起设备时会尝试发送全黑帧。恢复时重新打开控制器；若 USB 断开后未恢复，可禁用再启用插件。

## 构建

需要 **.NET 10 SDK** 和 Artemis。依赖固定到本机安装的 Artemis API 版本 `1.2026.101.2`，使用 RGB.NET 3.1.0 / HidSharp 2.6.4。

在项目根目录执行：

```powershell
dotnet build .\src\Artemis.Plugins.Colorful\Artemis.Plugins.Colorful.csproj -c Release -p:EnablePluginCopy=true
```

开发时下载的临时 SDK 可通过以下路径调用（没有修改系统 PATH）：

```powershell
& "$env:LOCALAPPDATA\Temp\opencode\dotnet\dotnet.exe" build .\src\Artemis.Plugins.Colorful\Artemis.Plugins.Colorful.csproj -c Release -p:EnablePluginCopy=true
```

如果手动安装，复制输出目录中的全部文件到 Artemis 的专用插件子目录，不要只复制 DLL。

## 验证

```powershell
dotnet run --project .\tools\Verify\Verify.csproj -c Release
dotnet run --project .\tools\Verify\Verify.csproj -c Release -- --probe
```

- 第一条检查三路数据范围、所有报告负载、未使用尾部、包序号和提交帧，并验证拒绝长度错误的帧。
- `--probe` 额外枚举本机端点并尝试打开正确的 HID 端点；**不向硬件写入灯光数据**。
- 已完成 Release 构建（0 警告、0 错误）、协议验证和本机端点打开验证。
- 1.0.1 修正刷新间隔单位：RGB.NET 3.1 的限速参数为秒/次，必须传入 `1.0 / 60.0`；旧版误传 `60` 导致约一分钟更新一次。验证程序包含真实触发器计时检查。
- 1.0.3 修正 1.0.2 注入受保护的 `ISettingsService` 导致无法加载的问题。移除独立限速，由 Artemis 渲染器按照软件 FPS 设置提供颜色，发送队列有新数据才更新。无需读取受保护设置，也无需重启插件来同步 FPS。验证程序检查 30/120 FPS 数据输入与无输入时停止更新。
- Artemis 首次部署时日志提示尚未识别新插件，需要重启扫描；实际灯光显示、三口物理顺序和休眠恢复仍需在 Artemis 启用后确认。

排查日志位置：`C:\ProgramData\Artemis\Logs`。搜索 `Colorful`，成功打开时可看到 `Colorful B760M lighting controller opened`。写入错误会记录 Warning，同一轮连续失败不会逐帧刷屏。

## 协议与来源

协议移植来源：Sky 发布的 [Chinese Motherboard SignalRGB plugin](https://gitlab.com/nollie/chinese-motherboard-signalrgb-plugin) 的 `Colorful_Motherboard_Controller.js`，具体使用本地源码中 `BATTLE-AX B760M-PLUS S WIFI7` 的配置。

| 参数 | 值 |
|---|---|
| 三路灯珠偏移（以灯珠为单位） | 0 / 60 / 120 |
| 每路灯珠数 | 60 |
| 完整帧 | 220 × 3 = 660 字节 |
| 每包 | 20 灯珠 / 60 字节 RGB |
| HID 报告 | `00 01 00 88 <包序号> <60 字节 RGB>` |
| 数据包 | 序号 0–10 |
| 提交包 | `00 01 00 88 FF` + 60 个零 |

三路共享同一发送队列，先合并 RGB.NET 的增量颜色，再发送完整帧，避免一条灯带更新时清除其他灯带。未使用的 180–219 地址保持黑色。刷新目标跟随 Artemis 的 FPS 设置；实际发送速度受颜色变化、渲染速度及 HID 传输耗时影响。

开发接口参考：[Artemis 插件入门](https://wiki.artemis-rgb.com/guides/developer/getting-started)。
