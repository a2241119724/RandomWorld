# Technical Specification

## Project Overview

RandomWorld 是一款 2D 像素风生存殖民地建设游戏。玩家在随机生成的地图上建立殖民地，管理工人完成建造、采集、搬运等任务，抵御波次敌人进攻，并通过 LLM 驱动的 NPC 对话系统与角色互动。

## Platform Support

- Windows PC (主要)
- Android (PackageType.Android 已定义)

## Core Features

### 殖民地管理
- **工人系统:** 招募工人执行建造、搬运、采集、种植、吃饭、睡觉、锻炼、穿戴 8 种任务
- **任务队列:** 优先级队列(0-3)，KD 树空间查询分配最近任务
- **补给监控:** 饥饿/疲劳状态机 (Healthy → Hungry → Tired → Exhausted → Critical)
- **殖民地指挥中心 (F10):** 实时诊断报告 — 人力分析、任务阻塞原因、补给缺口、拥堵等级

### 战斗系统
- **波次敌人:** 普通波 + Boss 波(每 3 波)，难度渐进缩放
- **主动技能 (Q/E/R/F):** 旋风斩、冲刺、力量爆发、治疗之光
- **连击系统:** 多阶连击伤害/经验加成
- **装备稀有度:** Common → Uncommon → Rare → Epic → Legendary → Mythic，属性倍率递增
- **死亡惩罚:** 经验损失 + 复活计时器

### LLM NPC 对话
- 本地 llama-server 驱动的 NPC 对话
- 短期记忆 + 长期记忆压缩(每 8 轮)
- RAG 游戏知识检索
- 对话期间工人任务暂停

### 天气系统
- 晴/雨/雪三种天气
- 影响玩家/工人移动速度、任务进度、灵气恢复

### 成就系统
- 5 类别(战斗/收集/生存/波次/工人)，最多 20 个成就
- F7 切换成就面板

### 存档系统
- 10 槽位多存档，二进制序列化(.lab)
- 自定义存档名、覆盖确认、清除

## Architecture

```
UI/Panel 层
    |
Gameplay/Manager 层 (运行时状态 + 事件驱动)
    |
Tool 层 (格式化、转换、Unity 适配)
    |
Domain 层 (纯 C# 规则引擎，零 Unity 依赖)
    |- 12 接口, 20 RuleService, EventBus
    |- 值对象: GameGridPosition, GameVector2
    |- 委托注入: ColonyDiagnosticContext
```

### 关键设计决策
- **Domain 纯 C#:** 无 UnityEngine 引用，12 个接口定义在最内层
- **UnityAdapter:** 适配器实现 Domain 接口，通过 ServiceLocator 注册
- **Shared Kernel:** `LAB2D.Enum` 含 16 个跨层枚举(DDD 模式)
- **HUD 热键:** 通过 GlobalInit.Update() 统一分发，避免子对象 inactive 时失效
