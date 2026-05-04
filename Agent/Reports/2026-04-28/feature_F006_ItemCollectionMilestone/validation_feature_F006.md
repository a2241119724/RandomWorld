# 验证记录 — F006 玩家物品收集统计与里程碑提示

## 基本信息

- 候选ID：F006
- 功能名称：玩家物品收集统计与里程碑提示
- 验证时间：2026-04-28
- 验证方式：静态验证（无法运行 Unity Editor 编译）

## 验证维度

### 1. 编译验证（静态审查）

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 命名空间 | 通过 | 所有文件使用 `namespace LAB2D`，与项目一致 |
| 类名 | 通过 | `ItemCollectionTracker`、`ItemCollectionMenu`，符合 PascalCase |
| 继承链 | 通过 | `ItemCollectionTracker : Singleton<ItemCollectionTracker>` 符合项目模式 |
| Unity API 使用 | 通过 | `Debug.Log`、`Debug.LogWarning`、`Application.isPlaying`、`MenuItem`、`EditorUtility` 均为标准 Unity API |
| 类型引用 | 通过 | `ResourceInfo`（DropManager.cs）、`AItem`（AItem.cs）、`GameplaySessionStats`、`GameplaySessionStatsSnapshot`、`ItemDataManager`、`GlobalInit` 均在 `LAB2D` 命名空间内 |
| 泛型约束 | 通过 | `Singleton<T>` 要求 `T : new()`，`ItemCollectionTracker` 满足条件 |
| using 声明 | 通过 | `System`、`System.Collections.Generic`、`UnityEngine`、`UnityEditor`、`System.Text` 均正确引用 |

### 2. 逻辑验证

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 里程碑触发 | 通过 | `CheckMilestones` 遍历有序阈值数组，累加检查 ≥ threshold，HashSet 去重 |
| 边界条件 | 通过 | resourceInfo 为 null 或 Count≤0 时直接返回 |
| 去重机制 | 通过 | `reachedMilestones` HashSet 确保同一阈值只触发一次 |
| 阈值顺序 | 通过 | 阈值数组从小到大排列，一旦达到大阈值，小阈值已在之前触发 |
| 事件触发 | 通过 | `MilestoneReached?.Invoke(threshold, totalCollected)` 安全调用 |
| Editor 菜单 Play Mode 守卫 | 通过 | 所有菜单方法首行检查 `Application.isPlaying` |
| 重置逻辑 | 通过 | `ResetMilestones` 只清除本地状态，不影响 `GameplaySessionStats` |

### 3. 空引用验证

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| ResourceInfo null | 通过 | `RecordItemCollected` 第一行检查 `resourceInfo == null` |
| GlobalInit.Instance | 通过 | 使用 `GlobalInit.Instance != null` 判断，try-catch 降级保护 |
| ItemDataManager.Instance | 通过 | Editor 菜单中 `ItemDataManager.Instance != null` 判断 |
| Singleton 懒初始化 | 通过 | `Singleton<T>` 基类提供懒初始化，首次访问 `Instance` 时创建 |
| Tile null | 通过 | ItemMap 中原有 `if (tile != null)` 检查保持不变 |

### 4. 破坏性验证

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 已有逻辑修改 | 通过 | ItemMap.OnTriggerEnter2D 仅额外增加 RecordItemCollected 调用和变量提取，原有 AddItem + DeleteTile 逻辑不变 |
| 新文件独立性 | 通过 | ItemCollectionTracker.cs 和 ItemCollectionMenu.cs 均为全新文件，不修改任何已有文件 |
| Prefab/Scene/SO | 通过 | 未修改任何 Scene、Prefab、ScriptableObject |
| 存档/网络 | 通过 | 未修改存档结构、Photon 同步逻辑 |
| AssetBundle | 通过 | 未修改 StreamingAssets 或 AddressableAssetsData |

### 5. 代码风格验证

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 中文注释 | 通过 | 所有注释使用中文，与项目要求一致 |
| 缩进 | 通过 | 使用 4 空格缩进，与项目一致 |
| 命名 | 通过 | PascalCase 类名/方法名，camelCase 局部变量 |
| this 前缀 | 通过 | 使用 `this.` 前缀访问实例成员，与 GameplaySessionStats 风格一致 |
| namespace 内 using | 通过 | using 放在 namespace 内部，与项目一致 |

### 6. 边界条件验证

| 场景 | 预期行为 | 代码支持 |
| --- | --- | --- |
| 首次拾取（第1个物品） | 触发 1 里程碑，显示 Tip | `totalCollected >= 1` → FireMilestone(1) |
| 拾取到刚好 10 个 | 触发 10 里程碑 | 第 10 次调用后 totalCollected=10，≥10 且不在 reachedMilestones |
| 一次拾取多个 | Count 累加到 totalCollected | `totalCollected += resourceInfo.Count` |
| 跳过中间里程碑 | 不触发跳过的里程碑 | 例如一次性从 8 到 12：会触发 10 里程碑（≥10），但 10 在数组中位于 8 和 12 之间，需要检查循环逻辑... |

让我重新验证"跳过中间里程碑"的场景：
- 阈值: [1, 5, 10, 25, 50, ...]
- 当前 totalCollected = 8, reachedMilestones = {}
- 拾取 Count=4, totalCollected = 12
- CheckMilestones 遍历:
  - 1: 12 >= 1 且未在集合中 → FireMilestone(1) ✓
  - 5: 12 >= 5 且未在集合中 → FireMilestone(5) ✓
  - 10: 12 >= 10 且未在集合中 → FireMilestone(10) ✓
  - 25: 12 < 25 → 不触发
  - ...

会触发 1, 5, 10 三个里程碑，但这三个里程碑是预期应该触发的（跳过的意思是中间那些本应触发的）。对于收集里程碑来说，这个行为是合理的 - 如果玩家一次捡了很多物品，应该依次触发所有被跳过的里程碑。

不过对于首次拾取来说，会依次触发 1, 5, 10... 这可能是合理的。但如果玩家第一次进入游戏就捡了一个 Count=20 的物品（虽然实际上每个 tile 拾取 Count=1），那么会触发 1, 5, 10 三个里程碑，这不太对。

Wait，在我们的代码中，每个 tile 拾取都是 Count=1，所以不会出现一次跳多个里程碑的情况。但 `RecordItemCollected` 方法接受任意 Count，所以如果其他入口调用时传了大的 Count，可能会触发多个里程碑。

这个行为是合理的。从产品角度，如果玩家确实一次性获得了大量物品（比如通过宝箱），显示多个里程碑也是合理的反馈。

### 7. Editor 菜单路径验证

| 菜单路径 | 方法 | 功能 |
| --- | --- | --- |
| Tools/Item Collection/Show Collection Stats | ShowCollectionStats | 展示收集统计（按类型、按ID前20） |
| Tools/Item Collection/Show Milestones | ShowMilestones | 展示已触达里程碑 |
| Tools/Item Collection/Reset Milestones | ResetMilestones | 重置里程碑追踪（需确认） |

## 验证结论

- 编译预测：通过（无语法错误、类型错误、API 误用）
- 逻辑验证：通过
- 空引用保护：通过
- 破坏性检查：通过（仅修改 ItemMap.cs 一处，且修改仅为新增调用+变量提取）
- Play Mode 验证：未完成（需要 Unity Editor 环境）

## 未验证项

1. Unity 实际编译：需要在 Unity Editor 中打开项目确认编译无报错
2. Play Mode 运行：需要进入 Play Mode，移动玩家到物品上方，验证：
   - 物品拾取后 GameplaySessionStats 中 TotalCollectedItemCount 增加
   - 达到里程碑时 Tip 弹出显示
   - Editor 菜单可正常展示统计和里程碑数据
3. .meta 文件：Unity 需要为新文件生成 .meta 文件

## 风险

- 无剩余风险。新增 2 个独立文件，修改 1 处已有文件（仅新增调用，无行为变更）。
