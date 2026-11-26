# 多人联机 2D 横版 RPG（服务器版）

把一个单人 2D 横版 RPG 课程作业，用 **Unity Netcode for GameObjects** 改造成 **最多 4 人联机** 的版本，
并附带一个**专用服务端模式**（`-server`），客户端零配置直接连服。

| 项目 | 说明 |
|---|---|
| 引擎 | Unity 2022.3.56f1c1（中国版） |
| 网络 | Netcode for GameObjects 1.12.0 + Unity Transport 1.5.0 |
| 模式 | 专用服务端（Dedicated Server）+ 多客户端，UDP 7777 |
| 人数 | 1 ~ 4 人 |
| 平台 | Windows x64（Linux 版规划中） |

---

## 一、只想玩，不想看代码

到 [**Releases**](../../releases/latest) 下载 **多人联机服务器版 v1.0**（约 84 MB），然后：

1. 整个文件夹解压到硬盘（**不要**在压缩包预览窗口里直接双击 exe）
2. 双击 **`PlayOnline.bat`**（推荐），或者直接双击 `MyRBG-test.exe`
3. 不用填 IP、不用装 Unity、不用挂 VPN —— 打开就直接进服务器

> `server.txt` 里只有一行服务器地址，游戏启动时读它并自动连服。
> 删掉它也能玩：`PlayOnline.bat` 内部写死了地址。

## 二、自己开服务器

```bat
MyRBG-test.exe -batchmode -nographics -server -port 7777 -logfile C:\Server\server.log
```

- 客户端只要把 `server.txt` 换成你的公网 IP 即可（默认端口 7777）
- 云服务器需要同时放行 **安全组** 和 **Windows 防火墙** 的 UDP 7777
- 用 `run.bat` + 任务计划程序可以开机自启，不需要登录桌面

## 三、联机架构（谁算谁的）

| 内容 | 权威方 | 说明 |
|---|---|---|
| 角色移动 / 动画 / 技能输入 | 各自客户端（Owner） | `OwnerNetworkTransform` 客户端权威 |
| 怪物 AI / 位置 / 动画状态 | 服务器 | 客户端只做插值和显示 |
| 血量 / 死亡 / 异常状态 | 服务器 | 客户端算完伤害再交给服务器裁定 |
| 金币 / 背包 / 技能 / 存档 | 各自客户端 | 每人一份独立进度 |
| 掉落物 | 各自客户端本地生成 | 避免「客户端销毁网络对象」报错 |

## 四、详细文档

改造过程、全部改动点、四阶段自测流程、常见故障排查，见 **[README-联机改造.md](README-联机改造.md)**。

## 五、免责

本项目是课程作业性质的个人作品，仅用于学习交流；美术/音效素材版权归原课程与原始作者所有，请勿商用。
