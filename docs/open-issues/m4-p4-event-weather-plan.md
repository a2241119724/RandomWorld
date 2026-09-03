# M4 包 4 切片：事件天气（灵雨/血月）实施方案

> 状态：方案定稿待实施（2026-09-04 自动驾驶轮调查产出）。用户对局进行中暂缓改 .cs。
> 大纲定位：`docs/游戏大纲.md:645` 包 4「每局不一样」——事件天气（灵雨/血月）。

## 现状管线（调查结论）

- 天气枚举：`WeatherManager.WeatherTypeEnum`（Sunny/Rain/Snow），视觉按 `weathers` 字典激活（无视觉时 null 安全跳过）
- 每日滚动：`GameTimeManager.DayRolloverAction → WeatherManager.RandWeather()`——**均匀随机** `Random.Range(0, Length)`，新枚举自动入池
- 玩法效果：`WeatherGameplayEffect`（Singleton，订阅 WeatherChanged）→ `WeatherGameplayTool.MapToDomain` → Domain 纯函数 `WeatherGameplayRuleService` 乘数表（移速/工作进度/灵气恢复/疲劳衰减四通道）
- 灵气浓度：`LingQiRuleService` 总合成已乘 `IWeatherGameplayService.EnergyRecoveryMultiplier`（灵雨挂点现成）
- 波次：`WaveManager.CreateSpawnPlan → WaveSpawnPlanService.CreatePlan → WaveRuleService`（数量/难度/种类纯函数，血月挂点）

## 改动面（约 7 文件）

1. `Domain/Common/WeatherType.cs`：+`SpiritRain` +`BloodMoon`
2. `Domain/Gameplay/WeatherGameplayRuleService.cs`：乘数表两值——SpiritRain→EnergyRecovery 1.5x 其余 1.0；BloodMoon→全 1.0（敌人强化挂波次不挂这里）
3. `Tool/WeatherGameplayTool.cs`：`MapToDomain` +2 映射
4. `Manager/WeatherManager.cs`：`WeatherTypeEnum` +2 值；`RandWeather` 均匀随机改**加权随机**（Domain 新纯函数 `RollWeather`：Sunny/Rain/Snow 高权重、SpiritRain ~15%、BloodMoon ~10%）
5. `Gameplay/WaveManager.cs` + `Domain/Wave/WaveRuleService.cs`：波次配置加 `IsBloodMoon` → 数量×1.5（`GetEnemyCountForWave` 或 `CreateSpawnPlan` 的 adjustedEnemyCount 处乘）+ 混池提前一波（`PickEnemyKind` 的 waveIndex 门槛 -1）
6. `Manager/DayNightLightManager.cs`（或 DayNightRuleService）：血月夜晚全局光色偏红 tint（运行时自建 GlobalLight，纯代码可闭环）
7. 单测：`Editor/Tests/` 补乘数表两值 + 加权随机分布 + 血月波次数量

## 约束与顺序

- 灵雨视觉暂缺（复用 Rain prefab 需场景资产，后补）；血月视觉=光色 tint 先行
- 实施顺序：Domain 枚举+乘数+单测 → Tool/Manager 映射+加权池 → 波次血月 → 光色 tint
- 血月语义：全天天气（池中抽出即当日为血月日），效果只作用于当晚波次
