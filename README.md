# kiwiprojekt.tourbox

Windows 开源驱动与配置工具，用于 [TourBox](https://www.tourboxtech.com/) 系列控制器。

基于 [kiwiprojekt/tourbox](https://github.com/kiwiprojekt/tourbox) 的串口通信库，参考 [TuxBox](https://github.com/AndyCappDev/tuxbox) 的协议逆向成果和 UI 设计，增加了设备初始化握手、完整的 WPF 图形配置界面和可视化按键组合编辑器。

## 项目结构

```
kiwiprojekt.tourbox/              # 核心库 — 串口通信 + 协议解析
├── TourBoxHandler.cs             #   设备连接/初始化/事件读取
├── EventParser.cs                #   字节码 → TourBoxEvent 解析
├── TourBoxKey.cs                 #   按键枚举 (14 种)
├── ActionType.cs                 #   动作类型枚举
└── TourBoxEvent.cs               #   事件数据模型

kiwiprojekt.tourbox.consoleapp/   # WinForms 托盘应用 (旧版, 已弃用)
└── ...

kiwiprojekt.tourbox.ui/           # WPF 配置工具 (当前主力)
├── Controls/
│   ├── KeyComboEditor.*          #   可视化按键组合编辑器
│   └── TourBoxDevice.*           #   设备图示控件
├── ViewModels/
│   ├── MainViewModel.cs          #   主窗口 VM
│   └── EditorViewModel.cs        #   映射编辑器 VM
├── Services/
│   ├── TourBoxService.cs         #   设备连接管理
│   └── InputService.cs           #   映射执行器 (键盘/鼠标/文本)
├── Models/                       #   数据模型
└── MainWindow.*                  #   主窗口
```

## 功能

- **自动设备初始化** — 发送 unlock 命令和配置消息，无需先运行官方驱动
- **图形配置界面** — WPF 托盘应用，双击打开配置窗口
- **可视化按键编辑** — 修饰键复选框 + 主键下拉 + 键盘录制捕获，无需手写 `VK_` 码
- **全按键映射** — 14 个实体按键 + Knob/Scroll/Dial 旋钮
- **多动作类型** — 键盘组合、鼠标点击、滚轮滚动、文本输入
- **Tap/Hold 模式** — 点击触发或按住保持
- **组合键支持** — 两个实体按键组合触发独立动作
- **配置文件持久化** — 存储在 `%APPDATA%\kiwiprojekt.tourbox\`

## 支持的设备

| 型号 | 连接方式 | 触觉反馈 | 状态 |
|------|---------|---------|------|
| TourBox Elite / Elite Plus | USB CDC Serial | 需固件支持 | ✅ |
| TourBox Neo | USB CDC Serial | — | ✅ |
| TourBox Lite | USB CDC Serial | — | ✅ |

> 协议基于 Elite 系列逆向，Neo/Lite 的初始化序列可能不同。如果设备不响应，
> 可在 `appsettings.json` 中将 `RequiresInit` 设为 `false`。

## 依赖

- .NET 8.0 SDK
- Windows 10 / 11
- [H.InputSimulator](https://github.com/HavenDV/H.InputSimulator) — 键盘鼠标模拟

```bash
dotnet build kiwiproject.tourbox.sln
```

## 使用

运行 `kiwiprojekt.tourbox.ui` 项目：

```
dotnet run --project kiwiprojekt.tourbox.ui
```

应用启动后常驻系统托盘：
- **双击托盘图标** → 打开配置窗口
- **右键菜单** → 状态 / 重载 / 退出
- **顶部选择 COM 口** → 点击连接
- **左侧表格选中控件** → 下方编辑面板修改映射
- **保存** → 实时生效

## 按键映射配置格式

```jsonc
{
  "PortName": "COM3",
  "RequiresInit": true,
  "DebugMode": false,
  "Keys": {
    "Tall": { "Action": "VK_CONTROL",       "Mode": "Hold" },
    "Short": { "Action": "VK_MENU",          "Mode": "Hold" },
    "C1":   { "Action": "VK_BROWSER_BACK",   "Mode": "Tap" },
    "Tour": { "Action": "VK_SPACE",          "Mode": "Tap" },
    "Side": { "Action": "LeftClick" }
  },
  "Combos": {
    "Top+Tall":   { "Action": "LeftClick" },
    "Top+Short":  { "Action": "RightClick" },
    "Top+Scroll": { "Action": "VK_CONTROL+VK_C" }
  },
  "Rotary": {
    "Scroll": {
      "Clockwise":        { "Action": "VerticalScroll", "Value": "Up" },
      "CounterClockwise": { "Action": "VerticalScroll", "Value": "Down" }
    },
    "Knob": {
      "Clockwise":        { "Action": "HorizontalScroll", "Value": "Right" },
      "CounterClockwise": { "Action": "HorizontalScroll", "Value": "Left" }
    }
  }
}
```

## 协议来源

- **初始化握手** — 来自 [TuxBox](https://github.com/AndyCappDev/tuxbox) 通过 Microsoft BTVS 蓝牙嗅探器在 Windows 上捕获，经 USB 验证确认
- **按键码映射** — [kiwiprojekt/tourbox](https://github.com/kiwiprojekt/tourbox) 原始串口抓包 + [jasonrohrer/tourBoxEliteLinuxDriver](https://github.com/jasonrohrer/tourBoxEliteLinuxDriver) libusb 驱动交叉验证
- **配置消息格式** — TuxBox `haptic.py` 和 jasonrohrer 的 `tourBoxSetupMap` 共同确认

## 许可

[![MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
