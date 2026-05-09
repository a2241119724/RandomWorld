# A007 成就系统 — 回滚方案

## 回滚概述

A007 成就系统为纯新增独立系统，回滚简单安全：删除新增文件 + 还原 GlobalInit 局部修改即可。

## 回滚步骤

### 1. 删除新增文件

```bash
rm Scripts/2D/Enum/AchievementCategory.cs
rm Scripts/2D/Enum/AchievementCategory.cs.meta
rm Scripts/2D/Enum/AchievementState.cs
rm Scripts/2D/Enum/AchievementState.cs.meta
rm Scripts/2D/Constant/AchievementConstant.cs
rm Scripts/2D/Constant/AchievementConstant.cs.meta
rm Scripts/2D/Tool/AchievementTool.cs
rm Scripts/2D/Tool/AchievementTool.cs.meta
rm Scripts/2D/Gameplay/AchievementData.cs
rm Scripts/2D/Gameplay/AchievementData.cs.meta
rm Scripts/2D/Gameplay/AchievementManager.cs
rm Scripts/2D/Gameplay/AchievementManager.cs.meta
rm Scripts/2D/UI/AchievementPopup.cs
rm Scripts/2D/UI/AchievementPopup.cs.meta
rm Scripts/2D/UI/AchievementPanel.cs
rm Scripts/2D/UI/AchievementPanel.cs.meta
rm Scripts/2D/Editor/AchievementMenu.cs
rm Scripts/2D/Editor/AchievementMenu.cs.meta
```

### 2. 还原 GlobalInit.cs 修改

在 `Scripts/2D/GlobalInit.cs` 中删除两处 A007 修改：
- **Start 方法**：删除 `AchievementManager.Instance.Initialize()`、`AchievementPopup.EnsureRuntimePopup()`、`AchievementPanel.EnsureRuntimePanel()` 三行及注释
- **Update 方法**：删除 `// A007 成就系统` 到 `}` 之间的整个成就系统更新代码块

或直接 `git checkout Scripts/2D/GlobalInit.cs` 还原。

### 3. 删除任务目录

```bash
rm -r Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/
```

### 4. 还原 ambitious_discovery.md

在 `Agent/Reports/ambitious_discovery.md` 中：
- 将 A007 状态改回 `[TODO]`
- 删除 A007 历史记录条目
- 恢复推荐优先开发列表

或直接 `git checkout Agent/Reports/ambitious_discovery.md` 还原。

## 无需回滚的资源

- **Scene**: 未修改 Game.unity 或其他场景文件
- **Prefab**: 未创建或修改任何 Prefab
- **ScriptableObject**: 未创建或修改任何 SO
- **StreamingAssets**: 未新增或修改任何文件
- **存档**: 未修改存档数据结构
- **Photon**: 未涉及任何 Photon 同步逻辑
- **AssetBundle**: 未涉及任何 AB 配置

## 回滚后验证

1. Unity 编译通过（无缺失类型引用）
2. Play Mode 正常运行（无 NullReference for AchievementManager）
3. F7 按键按下无响应（确认面板已移除）
4. Game 场景中无 `Ambitious_A007_*` Canvas 节点

## 风险级别

**低** — 纯新增独立系统，无一修改已有业务逻辑，回滚安全可靠。
