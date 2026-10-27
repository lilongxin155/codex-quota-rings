# Codex 额度圆环

一个 Windows 桌面小挂件，在 Codex 头像上方显示剩余额度，并随窗口移动。它每 10 秒刷新一次，不用反复打开头像菜单。

[English](README.md) · [下载最新版本](https://github.com/lilongxin155/codex-quota-rings/releases/latest)

![额度圆环界面示意图，数字为虚构示例](assets/demo.png)

> 图中是使用虚构数字制作的界面示意图，不包含真实账号或聊天内容。

## 功能

- 两个圆环分别显示 Codex 返回的短周期和长周期剩余百分比，例如 5 小时和一周；实际周期以账号返回的数据为准。
- 每 10 秒刷新；点击立即刷新，悬停可查看重置时间和最近更新时间。
- 随 Codex 窗口移动；最大化、最小化、关闭和重新打开时同步显示状态。
- 检测到侧边栏更新按钮与挂件重叠时自动避让。自动识别支持中英文更新提示，也可以右键手动调整位置。
- 灰色表示数据过期；破折号表示数据不可用，不会将缺失数据当作零。

## 安装

需要 64 位 Windows 10/11、.NET Framework 4.8、已安装并登录的 Codex 桌面端，以及本机的 Codex CLI。CLI 路径应位于 `%LOCALAPPDATA%\OpenAI\Codex\bin`，并支持 `codex app-server`。

1. 从[最新版本页面](https://github.com/lilongxin155/codex-quota-rings/releases/latest)下载 `CodexQuotaRings-windows-x64.zip`，完整解压。
2. 双击 `Install.cmd`。安装程序将文件复制到 `%LOCALAPPDATA%\CodexQuotaRings`，创建当前用户登录时启动的计划任务，并立即启动挂件；通常不需要管理员权限。
3. 打开 Codex 桌面端，等待几秒，头像上方会出现圆环。

发布包的可执行文件没有代码签名，Windows 可能显示未知发布者或 SmartScreen 提示。也可以检查源码后自行编译。

## 使用和卸载

点击刷新；悬停查看详情；右键可刷新、上移、下移、恢复位置或暂停。暂停后，在 Windows 开始菜单打开 **Codex Quota Rings** 即可恢复。

卸载时运行 `%LOCALAPPDATA%\CodexQuotaRings\Uninstall.cmd`。它会移除本项目的计划任务、开始菜单快捷方式、程序文件和本项目写入的状态文件；你放入该目录的其他文件会保留。

## 隐私与限制

挂件通过本机 Codex app-server 的 `account/rateLimits/read` 读取额度，沿用现有登录；挂件本身不会单独收集或上传凭据和额度。Codex app-server 可能按正常功能与 OpenAI 通信。

安装目录内的状态和错误文件可能包含额度百分比、时间、窗口信息或本机路径。**提交问题前请检查并打码。** 更新按钮检查只在内存中读取窗口的辅助功能标签和坐标，不保存按钮文字。

额度周期由账号实际返回决定；只有接口真的返回日额度时才显示日额度。该项目是独立浮层，不是 OpenAI 官方应用，也不是 Codex 内置插件；Codex 未来更新可能影响兼容性。共用同一个桌面窗口的其他模式也会显示挂件。

## 从源码构建

在项目根目录打开 PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1
```

构建产物位于 `release/`。要安装自行构建的版本，请将 `scripts/` 中的 `Install.ps1`、`Install.cmd`、`Uninstall.ps1`、`Uninstall.cmd`、`Launch.vbs` 一并复制到 `release/`，再运行 `release\Install.cmd`。

遇到问题可查看 [English README](README.md) 中的排查步骤，或在 GitHub 提交 Issue。提交截图和日志时请先移除个人信息。

## 许可

使用 [MIT License](LICENSE)。本项目与 OpenAI 无关联，也未获其背书。
