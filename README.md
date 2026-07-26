# RandomWorld

沙盒建造/生存类 2D 游戏（Unity），支持 Photon PUN 多人联机。

> 当前状态：开发中（v0.1.2-arch），部分功能未完成。

## 项目结构

```
Assets/
├── Scenes/                  # 游戏场景 (RigisterOrLogin / Menu / Game)
├── Scripts/
│   ├── 2D/                  # 玩法代码
│   │   ├── Domain/          # 纯规则层：伤害/波次/工人/技能/成就/天气等
│   │   ├── Gameplay/        # 运行时管理器：Wave / Skill / Achievement / Session 等
│   │   ├── Character/       # 角色系统 (Player / Enemy / Worker)
│   │   ├── Map/             # 地图与 Tile 逻辑
│   │   ├── MVC/             # MVC 界面框架
│   │   ├── UI/              # HUD / 面板展示
│   │   ├── Tool/            # 无状态工具函数（18+ 工具）
│   │   ├── Data/            # 存档与同步数据
│   │   ├── Network/         # Photon PUN 网络适配层
│   │   ├── AI/              # Worker AI 对话系统
│   │   ├── Editor/          # 自定义编辑器菜单与检查器
│   │   ├── UnityAdapter/    # Domain 接口的 Unity 实现
│   │   ├── SO/              # ScriptableObject 数据定义
│   │   └── Enum/            # 枚举 (16+ 类型)
│   └── Reference/           # 第三方库 (Photon PUN / MainThreadDispatcher)
├── Resources/               # 运行时资源
├── ResourcesLocal/          # 本地资源（不上传公开库）
├── Materials/               # 材质
├── Animation/               # 动画
├── AddressableAssetsData/   # Addressables 配置
├── StreamingAssets/         # 流式资源
└── Agent/                   # AI Agent 配置与文档
```

## 架构分层

| 层 | 说明 |
|----|------|
| **Domain** | 纯 C# 规则计算，无 Unity 依赖 |
| **Tool** | 无状态工具函数，供所有层调用 |
| **Gameplay** | 运行时管理器，连接 Domain 与 Unity |
| **UnityAdapter** | Domain 接口的 Unity 实现 |
| **UI / MVC** | 展示与交互层 |
| **Network** | Photon PUN 多人同步适配 |

## 进行中 / TODO

* Worker 现实事件行为与 AI 对话
* 任务树优化
* 种植任务
* 房间判定
* 道具实例化优化（ItemDataSO -> Common 工厂模式）
* 打击震动反馈

## 开发环境

- Unity 编辑器中打开项目根目录（含本 `Assets` 文件夹）
- 代码规范：`LAB2D` 命名空间，4 空格缩进，花括号独占一行
- 修改预制体 / Addressables 后需重新构建 AB 包
- 构建输出位于 `Build/` 目录
- 详细规范见 [AGENTS.md](./AGENTS.md)

## 常用命令

```powershell
git status
git config core.hooksPath .githooks
powershell -NoProfile -ExecutionPolicy Bypass -File .\.gitarchive\Set-ArchivePassword.ps1
```

## 注

* `transform.Find("a/b/c")` 可获取 active 为 false 的对象
* Photon RPC 同步时大量数据不建议使用 buffer（缓存有上限）
* 必须上传 `*.meta` 文件，否则配置出问题
* 修改完 Prefab 之后需要重新打 AB 包
* RuleTile 以 `y=x` 对称
* Button 界面添加点击函数需先将脚本放到物体上，再添加该物体
* 道具数据 ItemData 与地图瓦片 Tile 的名称关联绑定
* 数据传输: Character -> Weapon -> WeaponEffect -> Character

## 代码仓库

项目使用双仓库策略：

- **私有库** (`origin`)：完整项目，包含所有资源
- **公开库** (`public`)：仅代码，不含美术资源

```powershell
# 日常开发推送（仅私有库）
git push origin <branch>

# 一次性推送双仓库
powershell -ExecutionPolicy Bypass -File .\Push-To-BothRepos.ps1

# 指定分支 / 仅推公开库
.\Push-To-BothRepos.ps1 -Branch main
.\Push-To-BothRepos.ps1 -SkipPrivate
```
