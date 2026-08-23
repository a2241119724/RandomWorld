# Project Structure

## Technology Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Unity 2D | URP, Tilemap |
| Language | C# | .NET Standard 2.1 |
| Networking | Photon PUN 2 | 联机房间同步 |
| AI Dialogue | llama-server (本地) | OpenAI 兼容 API |
| UI | uGUI + 自定义 MVC | 像素风主题 PixelUITheme |
| Serialization | BinaryFormatter | .lab 存档格式 |

## File Tree

```
RandomWorld/
├── Assets/
│   ├── Scripts/2D/
│   │   ├── Domain/            # 纯 C# 领域规则引擎
│   │   │   ├── Common/        # 接口、值对象、EventBus
│   │   │   ├── Character/     # 伤害计算、等级升级
│   │   │   ├── Gameplay/      # 9 规则服务(连击、技能、装备等)
│   │   │   ├── Worker/        # 工人状态、补给、任务分配、人格/目标/货币/好感度
│   │   │   ├── Player/        # 伤害策略、移动策略
│   │   │   ├── Wave/          # 波次规则
│   │   │   ├── Inventory/     # 库存规则
│   │   │   └── Dialogue/      # LLM 提示词组装
│   │   ├── Character/         # 角色系统(泛型状态机)
│   │   │   ├── Player/
│   │   │   ├── Worker/Task/   # 10+ 种工人任务（建造/搬运/采集/挖掘/种植/吃饭/睡觉/锻炼/穿戴/建家/悬赏）
│   │   │   └── Enemy/         # 两种 AI(寻路/追踪)
│   │   ├── Core/              # 基础架构
│   │   │   ├── Seek/          # A*, 可步行性缓存, 对象池
│   │   │   └── KDTree/        # 空间索引
│   │   ├── Gameplay/          # 运行时玩法系统(商店/任务板/市场/货币/悬赏/好感度等)
│   │   ├── Manager/           # 跨领域管理(日志、存档、资源)
│   │   ├── Render/            # y-sort 渲染排序(WorldYSortManager, YSortAlgorithm) + 遮挡淡化(OcclusionFader)
│   │   ├── Map/               # 地图(TileMap, BuildMap, TileVisualSpawner 等)
│   │   ├── Item/              # 物品(背包、建造、装备)
│   │   ├── AI/Dialogue/       # LLM 对话(客户端、记忆、RAG)
│   │   ├── AI/Worker/          # Worker AI(交易/大脑决策/自主行为)
│   │   ├── MVC/               # UI 模型-视图-控制器
│   │   ├── UI/                # HUD、面板、特效
│   │   ├── Tool/              # 工具类(领域服务→表现层桥接)
│   │   ├── UnityAdapter/      # Domain 接口的 Unity 实现
│   │   ├── Enum/              # 共享枚举(Shared Kernel)
│   │   ├── Constant/          # 配置常量
│   │   ├── Serializable/      # 可序列化值类型
│   │   ├── SO/                # ScriptableObject 数据
│   │   ├── Data/              # 数据持久化
│   │   └── Editor/            # 编辑器工具 + 测试
│   ├── Agent/                 # AI 辅助开发工作流文档
│   ├── Scenes/
│   └── Resources/
├── AgentFull/                 # Python AI 代码生成框架(已移出 Assets)
└── docs/ai-context/           # AI 开发上下文文档
```

## Directory Conventions

- `Domain/` 禁止引用 UnityEngine，所有外部依赖通过接口+委托注入
- `Tool/` 是 Domain → Unity 的适配桥接层，不做运行时状态管理
- `Gameplay/XXXManager` 遵循统一模式: Enable/Disable/Tick/Refresh/BuildReport
- `UI/XXXHUD` 热键通过 GlobalInit 统一分发，避免子对象 inactive 失效
- 枚举统一在 `Enum/` 目录，作为 Shared Kernel 供各层引用
