# 七彩虹 B760M Artemis ARGB 插件

## 介绍

为七彩虹 **BATTLE-AX B760M-PLUS S WIFI7 V20** 提供 Artemis 灯光控制，适配 USB 控制器 `2F4C:1017`。

- 三路 ARGB 接口独立控制，每路 60 颗灯珠。
- 支持 Artemis 灯效与设备同步，刷新率跟随 Artemis 设置。
- 适用于 Windows x64；其他主板型号兼容性需另行确认。

## 使用教程

1. 安装并运行 [Artemis](https://artemis-rgb.com/)。
2. 退出 SignalRGB、iGameCenter 等正在控制主板灯光的软件及其灯光服务。
3. 在 Artemis 的 **设置 → 插件** 中导入 `Artemis.Plugins.Colorful-1.0.3.0.zip`，启用 **Colorful B760M ARGB**。
4. 在设备布局中启用并摆放 **ARGB 1、ARGB 2、ARGB 3** 三条灯带。
5. 应用覆盖灯带的 Profile，即可显示灯效。灯带不足 60 颗时，按实际灯珠数量选择效果范围。

更新插件后若未生效，重启 Artemis；重新连接设备后可禁用再启用插件。
