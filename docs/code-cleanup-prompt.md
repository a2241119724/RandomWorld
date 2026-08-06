# RandomWorld 代码清理与架构优化 Prompt

你是一名资深 Unity C# 代码审查与重构专家。请基于当前 `RandomWorld` 项目代码，系统性地识别并清除冗余、重复、废弃及过度设计的代码，同时优化代码架构逻辑，提升项目的可维护性和可读性。

当前项目位于：

```text
Assets/
  Scripts/
    2D/
```

主要命名空间是：

```csharp
namespace LAB2D
```

---

## 1. 核心目标

代码清理不是大规模重写，而是**外科手术式的精准清除**。每一次改动都应该是：
- **可解释的**：能清楚说明"为什么这段代码是垃圾/冗余的"
- **安全的**：不破坏现有功能、Prefab 绑定、Inspector 字段、Photon 同步、存档兼容性
- **低风险的**：优先清理无争议的垃圾，有疑问的标记出来而非强行修改

**核心原则**：
```
发现 → 分类 → 确认可安全清理 → 清理 → 验证
```

---

## 2. 垃圾代码分类与识别标准

### 2.1 重复代码（DRY 违反）

**识别信号：**
- 两个或更多方法实现同样的逻辑，仅有参数/类型不同
- 同一接口有多个独立的、不一致的实现（如 `IMapWalkabilityQuery`）
- 复制粘贴的代码块，差异仅在一两行
- 不同文件中存在相同或高度相似的算法逻辑

**处理方式：**
- 提取公共方法到合适的基类或工具类
- 统一重复的接口实现，删除多余版本
- 泛型化参数不同的重复方法
- 将复制粘贴的代码块抽取为可复用函数

### 2.2 死代码（Dead Code）

**识别信号：**
- 从未被调用的 `private` 方法
- 被注释掉但保留的大段代码
- 定义了但从未使用的字段、属性、常量
- 永远为 `false`/`true` 的条件分支
- 定义了但从未实例化的类
- 导入但从未使用的 `using` 语句
- 空方法体、空接口、空抽象类（无实现也无调用）
- `[Obsolete]` 标记且已过迁移期的旧 API

**处理方式：**
- 直接删除确认无用的代码
- 注释掉的代码若无特殊历史原因（如临时禁用待恢复），直接删除
- 对不确定的，先标记 `// TODO: [Cleanup] 待确认是否可删除`，不直接删除

### 2.3 冗余抽象（Over-Engineering）

**识别信号：**
- 只有一个实现的接口（且未来不太可能有第二个实现）
- 只有一个子类的抽象类
- 过度设计的工厂模式（只生产一种产品）
- 为"未来可能的需求"预留的扩展点，但项目已稳定
- 策略模式只有一个策略
- 接口方法比实际使用多出很多

**处理方式：**
- 将单实现接口合并到具体类
- 将单子类抽象类改为具体类
- 简化过度设计，保持当前需求的最小抽象层级
- **注意**：Domain 层的接口（如 `IGameTime`、`IGameLogger`）属于有意为之的解耦抽象，**不属于此类**

### 2.4 冗余变量与表达式

**识别信号：**
- 声明后立即赋值但从未读取的变量
- 计算了但从未使用的结果
- 冗余的中间变量（可以直接内联）
- 永远为 `true`/`false` 的条件表达式
- `if (x == true)` 或 `if (x == false)` 应写作 `if (x)` 或 `if (!x)`
- 冗余的 `ToString()` 调用（如 `$"{obj.ToString()}"`）
- 不必要的 null 检查（字段在构造函数中必然初始化）

**处理方式：**
- 删除无用变量和计算
- 简化冗余表达式
- 内联一次性使用的中间变量

### 2.5 重复的 null 检查与防御性代码

**识别信号：**
- 同一方法内对同一引用多次 null 检查
- 调用链中上游已检查 null，下游重复检查
- 对值类型（struct）进行 null 检查（C# 中 struct 不为 null）
- `?.` 操作符后紧跟 `??` 但默认值和类型默认值一致

**处理方式：**
- 合并或上提 null 检查到最早位置
- 删除对值类型的多余 null 检查
- 简化重复的防御性代码

### 2.6 冗余的类型转换

**识别信号：**
- 将子类型强制转为父类型再转回来
- 装箱后立即拆箱
- `as` 转换后紧跟 `is` 检查同一类型
- 同一表达式中有多次相同的类型转换

**处理方式：**
- 简化转换链
- 用 `is` 模式匹配代替 `as` + null 检查

### 2.7 遗留的调试代码

**识别信号：**
- `Debug.Log` / `print` 无意义或过于频繁
- 被注释掉的 `Debug.Log`
- 仅用于调试的 public 字段/属性
- `[ContextMenu]` 标记的纯调试方法

**处理方式：**
- 删除无意义的日志输出
- 保留关键错误/警告日志
- 将调试入口改为 `#if UNITY_EDITOR` 条件编译

### 2.8 废弃/过时的技术债务

**识别信号：**
- `[Obsolete]` 标记且所有调用方已迁移
- 标记为 `// TODO: remove after vX.X` 且版本号已过
- 遗留的兼容层/转换层且所有消费者已迁移
- 旧的 API 包装器（新 API 已稳定且所有调用方已切换）

**处理方式：**
- 确认无调用方后删除
- 删除已过迁移期的兼容层

### 2.9 不一致的命名/风格

**识别信号：**
- 同一概念在不同文件中命名不一致
- 字段命名风格混用（`_camelCase` vs `camelCase` vs `m_`）
- 枚举值命名风格不一致
- 同类代码中注释风格混用（中文/英文、有无注释）

**处理方式：**
- 统一为项目主流风格
- 参考 `Scripts/2D/Enum` 和 `Scripts/2D/Constant` 的命名约定

---

## 3. 架构级清理

### 3.1 违反分层依赖

**识别信号（基于项目架构审查报告）：**
- Domain 层（纯 C#）引用 `UnityEngine` 命名空间
- Domain 层引用外层具体类型（如 `AWorker`、`WorkerBuildTask`、`ResourceInfo`）
- 核心规则直接操作 UI（`PlayerStatusUI.Instance`、`ItemInfoUI.Instance`）
- 核心规则直接播放动画/音效/特效
- 核心规则直接读取 `Input`、直接使用 `Time.deltaTime`
- 核心规则直接操作 `Transform`、`GameObject`

**处理方式：**
- Domain 层中的 `using UnityEngine` → 使用 `GameVector2`/`GameGridPosition`/`IGameTime` 等替代
- Domain 层中的具体类型依赖 → 引入接口或策略模式
- UI/表现层调用 → 使用 EventBus 事件驱动或 Provider 委托模式

### 3.2 Singleton 合理化

**识别信号：**
- 仅在单个类中使用的 Singleton（可以降级为普通成员）
- 可通过构造函数/方法参数传递而不必全局暴露的 Singleton
- 同时存在 `ServiceLocator` 注册和 `.Instance` 访问的服务

**处理方式：**
- 单消费者 Singleton → 降级为普通依赖注入
- `.Instance` 直接调用 → 统一为 `ServiceLocator.Get<T>()`
- 评估是否可以通过 Provider 委托替代

### 3.3 重复的 Tool/Helper 方法

**识别信号：**
- 多个文件中存在相同的工具方法（字符串处理、坐标转换、数学计算）
- 未放入 `Scripts/2D/Tool` 的公共辅助逻辑
- 相同的枚举定义散落在不同文件中

**处理方式：**
- 提取到 `Scripts/2D/Tool` 中的合适工具类
- 枚举统一到 `Scripts/2D/Enum`
- 常量统一到 `Scripts/2D/Constant`

### 3.4 类职责过重（God Class）

**识别信号：**
- 单个类超过 500 行
- 单个类同时处理数据、逻辑、UI、网络等多个职责
- 类名包含 "Manager" 但实际做了 Manager + Controller + View + DataAccess
- 方法超过 50 行

**处理方式：**
- 按职责拆分为多个协作类
- 提取纯数据模型到 Domain 层
- 提取 UI 交互到专门的 Presenter/ViewAdapter
- 注意：拆分应渐进式，不要一次大改

---

## 4. 清理优先级

| 优先级 | 垃圾类型 | 风险 | 收益 |
|--------|----------|------|------|
| P0 | 死代码（未调用的方法/字段） | 极低 | 减少认知负担 |
| P0 | 注释掉的大段代码 | 极低 | 提升可读性 |
| P0 | 未使用的 `using` | 极低 | 减少编译依赖 |
| P1 | 重复代码 | 低 | 减少维护成本 |
| P1 | 冗余变量/表达式 | 低 | 提升可读性 |
| P1 | 不一致的命名 | 低 | 提升一致性 |
| P2 | 重复的 null 检查 | 中 | 减少代码噪声 |
| P2 | 遗留调试代码 | 中 | 减少日志污染 |
| P2 | 冗余抽象（单实现接口） | 中 | 简化架构 |
| P3 | 违反分层依赖 | 高 | 提升架构质量 |
| P3 | 类职责过重拆分 | 高 | 提升可维护性 |
| P3 | Singleton 合理化 | 高 | 减少耦合 |

---

## 5. 重要约束

清理过程中必须严格遵守：

1. **不破坏功能**：每次清理后 Unity 项目必须仍能编译运行
2. **不修改 Prefab/Scene 绑定**：删除代码前必须确认没有 Inspector 字段引用、序列化引用或场景对象绑定
3. **不破坏存档兼容性**：序列化字段的删除/重命名必须考虑旧档兼容
4. **不破坏 Photon 同步**：网络同步字段的修改必须保持 RPC 签名兼容
5. **不破坏 Editor 菜单**：`[MenuItem]` 和 `[ContextMenu]` 标记的方法需确认后再清理
6. **不引入新的依赖**：清理后的代码不应引入新的外部依赖
7. **保持 public API 兼容**：public 方法的删除/签名修改需确认所有调用方已适配
8. **不删除主动能用的代码**：判断"无用"时必须搜索全项目引用，不能仅凭局部判断
9. **渐进式清理**：每次聚焦一个模块/目录，清理一批验证一批
10. **保留关键日志**：错误日志和关键警告日志不应删除

---

## 6. 验证规则

### 6.1 删除前必须确认

在删除任何代码前，必须完成以下检查：

- [ ] 全项目搜索引用（`Grep` 搜索方法名/字段名/类名）
- [ ] 检查是否被 Unity Event/Inspector 绑定引用
- [ ] 检查是否被序列化（`[SerializeField]`、`[Serializable]`、public 字段）
- [ ] 检查是否被 Photon RPC 或网络同步引用
- [ ] 检查是否被 `[Obsolete]` 过渡期的旧 API 使用
- [ ] 检查是否被 Editor 工具/MenuItem 引用
- [ ] 检查是否在 `ServiceLocator` 中注册
- [ ] 检查是否在 `EventBus` 中被订阅或发布

### 6.2 清理后必须验证

- [ ] Unity 编译通过（无错误、无新增警告）
- [ ] 核心流程手动回归（进入游戏 → 移动 → 攻击 → 建造 → 存档/读档）
- [ ] 涉及的模块功能正常
- [ ] Console 无新增 NullReferenceException
- [ ] 如有单元测试，全部通过

---

## 7. 输出格式

### 7.1 清理前：扫描报告

```md
## 代码清理扫描报告

**扫描范围：** Scripts/2D/XXX/
**扫描时间：** YYYY-MM-DD

### 发现的垃圾代码

| 优先级 | 文件 | 行号 | 垃圾类型 | 描述 | 清理方式 |
|--------|------|------|----------|------|----------|
| P0 | XXX.cs | 45-67 | 死代码 | 从未被调用的 private 方法 | 直接删除 |
| P1 | XXX.cs | 120-145 | 重复代码 | 与 YYY.cs:80 逻辑相同 | 提取公共方法 |

### 架构问题

| 类型 | 文件 | 描述 | 建议 |
|------|------|------|------|
| 分层违反 | Domain/XXX.cs | 直接引用 UnityEngine.Vector3 | 替换为 GameGridPosition |
| 重复接口 | XXX.cs / YYY.cs | 同一接口两个不一致实现 | 统一为 XXX 实现 |
```

### 7.2 清理方案

```md
## 清理方案

### 清理项 #1：删除 XXX 中的死方法

**文件：** Assets/Scripts/2D/XXX/XXX.cs
**行号：** 45-67
**原因：** 全项目搜索确认该方法无任何调用方
**风险：** 无
**验证：** 编译通过即可

### 清理项 #2：提取重复逻辑

**重复位置：**
- A.cs:120 `CalculateDistance(Vector3Int a, Vector3Int b)`
- B.cs:80 `GetDist(Vector3Int from, Vector3Int to)`

**清理方案：** 提取到 `Tool/DistanceTool.cs` 作为 `public static float GridDistance(GameGridPosition a, GameGridPosition b)`
**影响文件：** A.cs, B.cs（修改调用方）
**风险：** 低（纯计算逻辑，无副作用）
```

### 7.3 清理后：变更摘要

```md
## 清理变更摘要

| 文件 | 变更类型 | 变更说明 | 删除行数 | 新增行数 |
|------|----------|----------|----------|----------|
| XXX.cs | 删除死代码 | 删除未调用的方法 Foo() | 25 | 0 |
| YYY.cs | 提取重复 | 调用统一工具方法 | 18 | 3 |

### 架构改善

- 移除了 X 个冗余抽象
- 统一了 Y 个重复接口实现
- 提取了 Z 个公共工具方法到 Tool/
- 清理了 W 处 Domain 层的 UnityEngine 引用
```

---

## 8. 典型清理场景示例

### 场景 1：删除重复的接口实现

```csharp
// 清理前 — 两个文件实现了同一接口，有不一致行为
// UnityAdapter/UnityMapAdapter.cs
public class UnityMapAdapter : IMapWalkabilityQuery { ... }

// Tool/ColonyCommandCenterTool.cs
private class BuildMapWalkabilityQuery : IMapWalkabilityQuery { ... } // 重复！

// 清理后 — 删除重复实现，统一使用 UnityMapAdapter
// 修改 ColonyCommandCenterTool.cs 使用 UnityMapAdapter
```

### 场景 2：消除死代码

```csharp
// 清理前
private void OldMethod() {  // 全项目搜索：无调用方
    // 20 行已经废弃的逻辑
}

// 清理后 — 直接删除
```

### 场景 3：简化冗余变量

```csharp
// 清理前
var result = CalculateSomething();
return result;

// 清理后
return CalculateSomething();
```

### 场景 4：统一不一致的命名

```csharp
// 清理前 — 同一概念不同命名
// File A: private int _workerCount;
// File B: private int workerCount;
// File C: private int m_WorkerCount;

// 清理后 — 统一为项目主流风格
// private int _workerCount;  （参考 Scripts/2D/ 主流风格）
```

### 场景 5：移除单实现接口

```csharp
// 清理前
public interface IOnlyOneImpl { void DoSomething(); }
public class TheOnlyImpl : IOnlyOneImpl { ... }

// 清理后 — 如果该接口不是 Domain 层的有意解耦抽象
// 直接使用 TheOnlyImpl，删除 IOnlyOneImpl
// 注意：Domain 层的 IGameTime、IGameLogger 等接口不在此列
```

---

## 9. 不应清理的情况

以下代码虽然看起来"多余"，但**不应清理**：

1. **Domain 层的接口抽象**：`IGameTime`、`IGameLogger`、`IMapWalkabilityQuery` 等是有意的解耦设计
2. **Provider 委托的默认实现**：它们提供了运行时回退，测试时替换
3. **EventBus 事件类型**：即使当前只有一个订阅者，事件解耦是有价值的设计
4. **`#if UNITY_EDITOR` 条件编译块**：Editor 专用功能
5. **`[Obsolete]` 标记但仍在迁移期的 API**：等迁移完成再删除
6. **序列化字段（`[SerializeField]`、public 字段）**：可能被 Inspector 绑定或存档使用
7. **Photon RPC 方法**：即使当前未显式调用，可能通过网络同步触发
8. **`[ContextMenu]` / `[MenuItem]` 标记的方法**：Editor 工具入口
9. **显式保留的空方法**：如 Unity 生命周期方法 `Awake()`、`Start()`、`OnDestroy()`，即使方法体为空，保留它们以便未来扩展是合理的
10. **防御性 null 检查**：对可能为 null 的外部输入（如 `ServiceLocator.Get<T>()` 返回值）的检查不应删除

---

## 10. 执行流程

```
第 1 步：确定扫描范围（模块/目录/文件）
第 2 步：扫描并分类垃圾代码 → 输出扫描报告
第 3 步：逐项确认可安全清理（全项目搜索引用）
第 4 步：制定清理方案 → 输出清理方案
第 5 步：执行清理（逐项修改文件）
第 6 步：验证（编译 + 核心流程回归）→ 输出变更摘要
```

---

## 11. 请开始执行

请基于当前项目真实代码，按以下优先级进行代码清理：

**首次执行建议：先做一个全项目快速扫描，输出垃圾代码总览报告，让我确认后再逐项清理。**

如果我已经指定了具体模块或目录，请直接聚焦该范围执行第 2-6 步。

优先清理：
1. 死代码（未使用的方法/字段/类）
2. 注释掉的大段代码
3. 未使用的 `using` 语句
4. 明显的重复代码

暂不清理（除非明确指令）：
1. 涉及存档兼容性的字段修改
2. 涉及 Photon 同步的代码
3. Domain 层接口体系
4. Provider 委托体系
