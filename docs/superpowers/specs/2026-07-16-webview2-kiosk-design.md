# KioskWin — WebView2 全屏锁定程序设计规格

- **日期**：2026-07-16
- **状态**：已批准（待实现规划）
- **目标平台**：Windows 工控机（Win10/11 x64），单显示器
- **实现方案**：方案 A — WinForms + 自包含单文件 + Evergreen WebView2 + 启动文件夹自启

---

## 1. 背景与目标

构造一个 C# 程序，使用 WebView2 加载并展示一个可配置的远程网页
（默认 `https://position.lmding.cn/public/wjbl/xcj/dist/index.html#/`）。
程序部署在 Windows 工控机上，打开后自动全屏，**无最小化/关闭按钮，不响应关闭与最小化操作**，
目的是防止普通用户故意关闭程序或把它切到后台。

### 锁定强度：基础锁定

经确认，威胁模型为"普通操作工"级别（只会点关闭按钮 / 按 Alt+F4 / 尝试 Alt+Tab）。
因此采用**基础锁定**：无边框全屏 + 隐藏按钮 + 拦截 Alt+F4 与窗口关闭消息。
**不**包含看门狗进程、低级键盘钩子、注册表/组策略锁定、替换 Windows Shell。

## 2. 需求

### 功能性需求

| 编号 | 需求 |
|---|---|
| F1 | 加载并全屏展示配置文件中指定的 URL |
| F2 | URL 可在配置文件中配置，无需重新编译 |
| F3 | 启动即全屏（无边框、覆盖主屏），无最小化/关闭按钮 |
| F4 | 拦截 Alt+F4 与用户触发的关闭操作（`CloseReason.UserClosing`） |
| F5 | 防止切到后台：始终置顶、不在任务栏显示、失焦自动抢回 |
| F6 | 开机后自动启动（免管理员权限） |
| F7 | 网络失败 / 页面加载失败时显示全屏"正在重试"遮罩，后台自动重连，恢复后自动回到网页 |
| F8 | 隐藏的管理员逃生通道：隐藏键组合 + 密码验证，提供 退出 / 重载 / DevTools / 解除锁定 |
| F9 | 单实例：重复启动时激活已有窗口而非新开一个 |

### 非功能性需求

| 编号 | 需求 |
|---|---|
| N1 | 自包含单文件部署，工控机无需预装 .NET 运行时 |
| N2 | 可测的纯逻辑（配置、密码哈希、重试状态机）有单元测试覆盖 |
| N3 | 不静默崩溃：未捕获异常写日志并显示错误遮罩 |
| N4 | 配置缺失/非法时使用安全默认值，不崩溃 |

## 3. 范围之外 / 已知限制（基础锁定档位）

- **不拦截** Alt+Tab、Win 键、Ctrl+Shift+Esc（任务管理器）、Ctrl+Alt+Del。
  懂行的用户可通过任务管理器结束进程。`TopMost` + 失焦抢回保证本窗口始终可见在最前，
  但无法阻止进程被强杀。
- **无看门狗**：进程被结束后不会自动重启。
- 若将来需要更强锁定，可在此基础上扩展为"中等锁定"（看门狗 + 键盘钩子）或
  "高强度 Kiosk"（自定义 Shell + 注册表策略），见第 11 节演进路径。

## 4. 架构与组件

```
position-win/
├── KioskWin.sln
├── src/KioskWin/
│   ├── KioskWin.csproj          # WinForms; net8.0-windows; 自包含单文件发布
│   ├── Program.cs                    # 入口：DPI、单实例互斥锁、异常兜底、日志初始化
│   ├── Forms/
│   │   ├── MainForm.cs(.Designer)    # 无边框全屏窗口，宿主 WebView2
│   │   ├── RetryOverlay.cs(.Designer)# 全屏"网络失败·正在重试"遮罩
│   │   └── AdminDialog.cs(.Designer) # 密码框 + 工具按钮
│   ├── Core/
│   │   ├── KioskConfig.cs            # 配置 POCO + 加载/校验/热重载
│   │   ├── PasswordHasher.cs         # SHA-256(salt + pwd)，纯函数可测
│   │   ├── RetryController.cs        # 重试状态机，纯逻辑可测
│   │   └── FileLogger.cs             # 写 %LocalAppData%\KioskWin\logs\
│   ├── appsettings.json              # 配置文件（随 exe 同目录）
│   └── install-shortcut.ps1          # 一键创建开机自启快捷方式
├── tests/KioskWin.Tests/        # xUnit，覆盖 Core 层纯逻辑
└── docs/superpowers/specs/…
```

### 分层原则

- `Forms`：Win32 / WebView2 / 窗口行为（不可单元测试，用手动 QA 清单覆盖）。
- `Core`：可测的纯逻辑（配置、密码哈希、重试状态机），用 xUnit 覆盖。
- 单元职责清晰、可独立理解、通过明确接口通信。

## 5. 窗口与基础锁定行为（`MainForm`）

| 行为 | 实现 |
|---|---|
| 无边框全屏 | `FormBorderStyle = FormBorderStyle.None` + `WindowState = FormWindowState.Maximized`（覆盖主屏） |
| 无最小化/关闭按钮 | `None` 样式本身无标题栏、无系统按钮 |
| 拦截 Alt+F4 | `FormClosing` 事件：`e.CloseReason == CloseReason.UserClosing` 且**非管理员授权**关闭时 `e.Cancel = true` |
| 允许的关闭 | `WindowsShutDown`（系统关机）、`ApplicationExitCall`（管理员退出）放行 |
| 防切到后台 | `TopMost = true`（始终置顶）+ `ShowInTaskbar = false`（无任务栏条目）+ `Deactivate` 事件中 `BeginInvoke(() => Activate())` 重新抢焦 |
| 键盘预览 | `KeyPreview = true`，使窗口优先于子控件接收按键（用于管理员组合键） |

### 关闭控制状态

`MainForm` 维护一个布尔字段 `private bool _authorizedClose;`。默认 `false`，
`FormClosing` 中：若 `!_authorizedClose && e.CloseReason == UserClosing` → 取消。
管理员"退出程序"按钮先置 `_authorizedClose = true` 再 `Application.Exit()`。

## 6. WebView2 配置与导航（`MainForm`）

### 初始化

`EnsureCoreWebView2Async(null)` → **Evergreen 运行时**（默认；机器联网，首次运行自动引导安装）。
`InitializationCompleted` 成功后配置 `CoreWebView2Settings`：

- `AreDevToolsEnabled = false`
- `AreBrowserAcceleratorKeysEnabled = false`（禁掉 F12 / Ctrl+F / Ctrl+P / Ctrl+加号缩放等）
- `IsStatusBarEnabled = false`
- `IsZoomControlEnabled = false`
- `IsBuiltInErrorPageEnabled = false`（错误页由我们自己画）
- `IsGeneralAutofillEnabled = false`

### 导航与重试流程

- 监听 `NavigationCompleted`：`!e.IsSuccess` → 显示 `RetryOverlay`，启动 `RetryController`。
- `RetryController` 内部 `System.Windows.Forms.Timer`，每 `RetryIntervalSeconds` 秒触发
  `CoreWebView2.Navigate(Url)`；导航成功 → 隐藏遮罩、停定时器。
- 监听 `CoreWebView2.ProcessFailed`（渲染进程崩溃）→ 重新初始化 WebView2 / 重载。

> 接口约定：`RetryController` 暴露 `event Action ShouldRetry` 与 `Show()/Hide()` 之外的纯状态转换
> （Idle / Waiting / Retrying），使状态机可在无 UI 依赖下单测。
>
> **`RetryOverlay` 是唯一的全屏遮罩组件**，可复用于不同场景，仅文本/状态不同：
> 网络重试（"网络连接失败，正在重试…"）、配置错误（"URL 配置非法，请联系管理员"）、
> WebView2 运行时缺失、未捕获异常的通用错误页。不要为每个场景新建不同组件。

## 7. 管理员逃生通道（`AdminDialog`）

### 触发

`KeyPreview=true` + 重写 `ProcessCmdKey(ref Message, Keys)`：检测组合键
（默认 `Ctrl+Shift+Alt+Q`，可在配置 `AdminKeyCombination` 修改），命中时吞掉按键并弹出 `AdminDialog`。
普通用户不知道该组合，故碰不到。

### 鉴权

- 密码框 `TextBox`，`PasswordChar = '*'`。
- 提交时 `PasswordHasher.Verify(input, config.AdminPasswordHash, config.PasswordSalt)`，
  内部为 `SHA-256(salt + input)`，常数时间比较。
- 验证通过才启用下方四个工具按钮；失败则提示并清空，累计失败次数写日志。

### 工具按钮

| 按钮 | 行为 |
|---|---|
| 退出程序 | 置 `_authorizedClose = true` 后 `Application.Exit()` |
| 重载页面 | 热重读配置 + `CoreWebView2.Navigate(config.Url)` |
| 打开 DevTools | 临时 `AreDevToolsEnabled = true` + `OpenDevToolsWindow()` |
| 解除锁定 | 临时恢复普通窗口：`TopMost=false` + `FormBorderStyle=Sizable` + `WindowState=Normal`，便于维护时挪开窗口操作桌面 |

> **关于"最小化"**：因 `ShowInTaskbar=false` 且 `TopMost=true`，传统"最小化到任务栏"在本程序中
> 无法点回，没有意义。故用"解除锁定（恢复普通窗口）"替代，更适合维护场景。

## 8. 配置文件 `appsettings.json`

```json
{
  "Url": "https://position.lmding.cn/public/wjbl/xcj/dist/index.html#/",
  "AdminKeyCombination": "Ctrl+Shift+Alt+Q",
  "AdminPasswordHash": "<SHA-256 hex，见下方生成方式>",
  "PasswordSalt": "<random hex>",
  "RetryIntervalSeconds": 10,
  "TopMost": true,
  "ShowInTaskbar": false,
  "UserDataFolder": "WebView2Data"
}
```

- 用 `Microsoft.Extensions.Configuration.Json` 加载。启动时读一次；"重载页面"工具会热重读。
- 校验：`Url` 必须是合法 http/https 绝对 URL；`RetryIntervalSeconds` ∈ [1, 600]。
- 配置缺失/非法 → 其余字段用上表默认值 + 日志告警；`Url` 非法时不回退 `about:blank`，
  而是显示 `RetryOverlay` 的"配置错误"状态（"URL 配置非法，请联系管理员"），不崩溃。
  修正配置后通过管理员"重载页面"热重读即可恢复。

### 生成密码哈希

程序内置隐藏 CLI 模式：

```
KioskWin.exe --hash-password <密码>
```

打印生成的 `AdminPasswordHash` 与随机 `PasswordSalt`，填入 `appsettings.json`。
**禁止在配置中存明文密码。**

## 9. 打包与部署 / 开机自启

### 发布命令

```
dotnet publish src/KioskWin/KioskWin.csproj \
  -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

产物为单 exe，**工控机无需预装 .NET**，拷贝即用。

> **已知注意点**：WebView2 的原生 loader（`WebView2Loader.dll`）在单文件模式下必须配合
> `IncludeNativeLibrariesForSelfExtract=true`，运行时解压到临时目录。实现时需验证 WebView2 能正常初始化；
> 若单文件与 WebView2 冲突，退路是关闭 `PublishSingleFile`（仍 `self-contained`，多文件目录部署）。

### 部署

- 拷贝发布产物到 `C:\KioskWin\`。
- `appsettings.json` 与 exe 同目录，可直接改 URL 无需重新编译。

### 开机自启（免管理员权限）

- `install-shortcut.ps1` 在当前用户的「启动」文件夹创建快捷方式：
  `%AppData%\Microsoft\Windows\Start Menu\Programs\Startup\KioskWin.lnk`
- 启动文件夹方案可见、易管理、无需提权。
- **可选增强**：随包附带 Evergreen 引导器 `MicrosoftEdgeWebview2Setup.exe`；
  程序首次运行若 `EnsureCoreWebView2Async` 抛"未安装运行时"异常，则静默执行引导器安装后重试。

## 10. 错误处理与日志

| 场景 | 处理 |
|---|---|
| 未检测到 WebView2 运行时 | catch `EnsureCoreWebView2Async` 异常 → 全屏提示遮罩；可选自动跑引导器 |
| 渲染进程崩溃 | `CoreWebView2ProcessFailed` → 重新初始化 / 重载 |
| 未捕获异常 | `AppDomain.UnhandledException` + `Application.ThreadException` → 写日志 + 显示通用错误遮罩，不静默退出 |
| 第二实例 | 命名 `Mutex`（`Global\KioskWin_SingleInstance`）→ 激活已有窗口到前台后退出新进程 |
| 配置缺失/非法 | 安全默认值 + 日志告警 |

### 日志

- 路径：`%LocalAppData%\KioskWin\logs\yyyy-MM-dd.log`
- 内容：启动、URL、导航结果（成功/失败）、重试事件、异常堆栈、管理员操作（鉴权成功/失败、执行的工具）。
- 实现：轻量 `FileLogger`（追加写、按天分文件），不引入重依赖。

## 11. 测试策略

### 单元测试（xUnit，`tests/KioskWin.Tests/`）

- `KioskConfig`：合法 JSON 解析、各字段校验、缺失/非法字段回退默认值、热重载。
- `PasswordHasher`：正确密码通过、错误密码失败、不同 salt 产生不同 hash、常数时间比较。
- `RetryController`：状态转换 Idle→Waiting→Retrying→(Idle on success / 继续 Retrying on failure)、
  定时器启停、事件触发。
- URL 合法性校验函数。

### 手动 QA 清单（窗口/Win32 行为）

- [ ] 全屏覆盖主屏，无标题栏与最小化/关闭按钮
- [ ] Alt+F4 无效，窗口不关闭
- [ ] 任务栏无条目
- [ ] 窗口始终置顶，Alt+Tab 切换后仍可见在最前
- [ ] 断网 → 出现重试遮罩 → 恢复网络 → 自动回到网页
- [ ] 管理员组合键弹出密码框
- [ ] 错误密码不通过；正确密码解锁工具按钮
- [ ] 退出 / 重载 / DevTools / 解除锁定 均符合预期
- [ ] 开机自启生效
- [ ] 重复启动只激活已有窗口

### 冒烟部署

在真实工控机或 Win10/11 虚拟机上跑一遍发布产物，过完 QA 清单。

## 12. 演进路径（非本期范围）

如未来需要更强锁定：

- **中等锁定**：新增独立看门狗进程（主进程被结束则重启）+ 低级键盘钩子（`SetWindowsHookEx`）屏蔽 Alt+Tab/Win/Ctrl+Shift+Esc。
- **高强度 Kiosk**：注册为自定义 Shell（替换 `explorer.exe`）开机即启动 + 注册表/组策略禁用任务管理器 + 打包固定版本 WebView2 离线运行时。

## 13. 决策记录

| 决策 | 选择 | 理由 |
|---|---|---|
| 锁定强度 | 基础锁定 | 威胁模型为普通操作工，YAGNI |
| UI 框架 | WinForms | 无边框全屏代码量最小、构建快 |
| WebView2 运行时 | Evergreen | 机器联网，自动维护，无需离线 |
| 打包 | 自包含单文件 | 工控机无需装 .NET，拷贝即用 |
| 自启 | 启动文件夹快捷方式 | 免提权、可见易管理 |
| 维护退出 | 隐藏组合键 + 密码 | 普通用户碰不到，维护方便 |
| "最小化"语义 | 解除锁定（普通窗口） | 任务栏最小化在 `ShowInTaskbar=false` 下无意义 |
