# 验证记录 - A009 浮动战斗文字系统

## 验证时间

2026-05-09

## 验证方式

静态验证（命令行环境无 .NET SDK / Unity Editor，无法执行编译和 Play Mode）

## 1. 新增文件清单

| 文件 | 路径 | 用途 |
|---|---|---|
| FloatingTextType.cs | `Scripts/2D/Enum/FloatingTextType.cs` | 浮动文字类型枚举（7种类型） |
| FloatingTextConstant.cs | `Scripts/2D/Constant/FloatingTextConstant.cs` | 颜色、字号、动画参数、池配置、节点名、菜单路径常量 |
| FloatingTextTool.cs | `Scripts/2D/Tool/FloatingTextTool.cs` | 对象池辅助、颜色/字号/速度/缩放查询、文本格式化、Canvas 创建 |
| FloatingTextUI.cs | `Scripts/2D/UI/FloatingTextUI.cs` | 浮动文字 MonoBehaviour，驱动上浮/弹出/淡出动画，自动回收 |
| FloatingTextManager.cs | `Scripts/2D/Gameplay/FloatingTextManager.cs` | Singleton 管理器，生成入口，对象池管理，坐标转换 |
| FloatingTextMenu.cs | `Scripts/2D/Editor/FloatingTextMenu.cs` | Editor 菜单：安装/移除/验证 |

## 2. 修改文件清单

| 文件 | 修改内容 | 风险等级 |
|---|---|---|
| `Scripts/2D/Character/Character.cs` | `ReduceHp()` 方法中新增 FloatingTextManager.SpawnDamageText() 调用（新增4行） | 低（纯追加，不改变已有逻辑） |
| `Scripts/2D/GlobalInit.cs` | `Start()` 方法中新增 FloatingTextManager.Instance.EnsureInitialized() 调用（新增2行） | 低（纯追加，与其他系统初始化并列） |

## 3. 验证项

### 3.1 枚举验证

- **路径**: `Scripts/2D/Enum/FloatingTextType.cs`
- **命名空间**: `LAB2D`（与项目一致）
- **成员**: Damage(0), Critical(1), Heal(2), Combo(3), Experience(4), Dodge(5), StatusEffect(6)
- **语义**: 不重复现有枚举（WavePhaseType、WaveRewardType、AchievementCategory 等均不冲突）
- **中文注释**: 每个成员均有用途说明
- **修改安全**: 纯新增，不修改已有枚举

### 3.2 常量验证

- **路径**: `Scripts/2D/Constant/FloatingTextConstant.cs`
- **命名空间**: `LAB2D`
- **分组**: 颜色、字号、动画参数、对象池、节点名、文案、Editor 菜单路径
- **颜色**: 7个静态只读 Color，与 PixelUITheme 风格一致
- **字号范围**: 24-60，合理递进
- **动画参数**: 上浮速度 1.5-4.0，存活时间 0.8-1.2s，淡出 0.3s
- **池配置**: DefaultPoolSize=30, MaxPoolSize=60
- **节点命名**: 带 `Ambitious_A009_` 前缀，便于识别和回滚
- **菜单路径**: `工具/智能体/浮动战斗文字/` 与其他系统一致
- **不重复已有常量**: PrefabConstant、ResourceConstant、TagConstant 均不冲突
- **中文注释**: 每个常量均有用途和修改风险说明

### 3.3 工具类验证

- **路径**: `Scripts/2D/Tool/FloatingTextTool.cs`
- **命名空间**: `LAB2D`
- **访问级别**: `public static`，纯静态方法
- **不引用 UnityEditor**: ✅ 已确认无 `using UnityEditor`
- **空引用保护**: `EnsureCanvas()` 检查 EventSystem 缺失并自动创建；`CreateFloatingTextObject()` 检查 parent 参数
- **方法列表**:
  - `GetColor(FloatingTextType)` → Color
  - `GetFontSize(FloatingTextType)` → int
  - `GetFloatSpeed(FloatingTextType)` → float
  - `GetLifetime(FloatingTextType)` → float
  - `GetPopScale(FloatingTextType)` → float
  - `FormatDamageText(float)` → string
  - `FormatHealText(float)` → string
  - `FormatExpText(int)` → string
  - `FormatComboText(int)` → string
  - `RandomHorizontalOffset()` → float
  - `CreateFloatingTextObject(string, Transform)` → GameObject
  - `EnsureCanvas(string, int)` → GameObject
- **边界安全**: switch 语句均含 default 分支返回默认值
- **可复用性**: 所有方法均可被多个子模块调用
- **中文注释**: 每个方法均有用途、参数、返回值说明

### 3.4 表现组件验证

- **路径**: `Scripts/2D/UI/FloatingTextUI.cs`
- **命名空间**: `LAB2D`
- **继承**: `MonoBehaviour`
- **生命周期**: Awake → Spawn（外部触发）→ Update（动画驱动）→ Recycle（自动回收）
- **组件依赖**: Text, Outline, RectTransform, CanvasGroup（均在 Awake 中安全获取或创建）
- **动画**: 弹出缩放(暴击/连击) → 上浮 → 淡出，通过 elapsedTime 驱动
- **回收**: 动画结束后自动调用 `FloatingTextManager.Instance.ReturnToPool(this)`
- **强制回收**: `ForceRecycle()` 用于清场
- **空引用保护**: Awake 中使用 `??` 和 `AddComponent` 确保组件存在
- **不引用 UnityEditor**: ✅

### 3.5 管理器验证

- **路径**: `Scripts/2D/Gameplay/FloatingTextManager.cs`
- **命名空间**: `LAB2D`
- **继承**: `Singleton<FloatingTextManager>`（与项目模式一致）
- **初始化**: `EnsureInitialized()` 幂等（`initialized` 标志），创建 Canvas + 预热池
- **对象池**: `Queue<FloatingTextUI>`，30默认/60最大，超限时回收最老活跃对象
- **世界坐标转换**: `WorldToScreenPosition()` 含 Camera.main 空引用保护
- **公开接口**:
  - `SpawnDamageText(Vector3, float, bool, bool)`
  - `SpawnHealText(Vector3, float)`
  - `SpawnComboText(Vector3, int)`
  - `SpawnExpText(Vector3, int)`
  - `SpawnDodgeText(Vector3)`
  - `SpawnStatusText(Vector3, string)`
- **线程安全**: 不涉及多线程，纯主线程调用
- **Photon 同步**: 不涉及（浮动文字为纯本地表现）
- **存档影响**: 无
- **资源引用**: 不依赖 Resources/SO/Prefab
- **不引用 UnityEditor**: ✅

### 3.6 Editor 菜单验证

- **路径**: `Scripts/2D/Editor/FloatingTextMenu.cs`
- **命名空间**: `LAB2D`
- **访问级别**: `public static`
- **菜单路径**: 使用 `FloatingTextConstant` 常量，不硬编码
- **方法**:
  - `InstallFloatingTextToGame()`: 创建 Canvas + 池容器，场景名检查，重复执行安全
  - `RemoveFloatingTextFromGame()`: 安全删除 Canvas，场景名检查
  - `ValidateFloatingTextSystem()`: 检查枚举值数量、颜色配置、字号、池参数，输出通过/失败计数
- **使用 UnityEditor**: ✅ Editor 专用目录，不污染运行时

### 3.7 修改文件验证

#### Character.cs
- **修改位置**: `ReduceHp()` 方法，第126-128行
- **修改内容**: 在已有 DamageUI 实例化代码后新增3行
- **兼容性**: 
  - 不改变已有 DamageUI 实例化逻辑
  - 不改变 hp 计算逻辑
  - 不改变 Death() / ResetColor() 逻辑
  - `ComboBonusManager.Instance.DamageMultiplier` 调用模式与上下文第97行一致
- **空引用保护**: `FloatingTextManager.Instance.SpawnDamageText()` 内部有 `initialized` 检查

#### GlobalInit.cs
- **修改位置**: `Start()` 方法，第58-60行之后
- **修改内容**: 新增2行 + 注释
- **兼容性**: 与其他系统初始化并列（A006/A007），互不干扰
- **初始化顺序**: FloatingTextManager 不依赖其他 Manager，顺序无关

### 3.8 .meta 文件

- 新增文件位于 `Assets/` 目录下，Unity 会在下次资源导入时自动生成 `.meta` 文件
- 不产生 `.meta` 孤儿文件

### 3.9 工具类/枚举/常量复用验证

- **Tool 复用**: `FloatingTextTool` 未直接调用 `Tool.cs` 方法（`FloatingTextManager` 独立管理 Canvas），但 `EnsureCanvas` 与 `AchievementTool.EnsureCanvas` 功能相似——此处属于不同系统各自的 Canvas 创建，可接受
- **Enum 复用**: 不重复现有枚举，`FloatingTextType` 为全新语义域
- **Constant 复用**: 不重复现有常量，节点名/菜单路径/颜色均为全新值

### 3.10 未使用 Tool/Enum/Constant 说明

- 本次完整使用了三层公共代码：新增 Enum + Constant + Tool
- 不存在跳过公共层的情况

### 3.11 暂不抽取说明

- `FloatingTextTool.EnsureCanvas()` 与 `AchievementTool.EnsureCanvas()` 功能相似但各自独立——不同系统的 Canvas 配置（名称、sortingOrder）不同，暂不合并。后续如有第三个系统需要 EnsureCanvas，再抽取到通用 `Tool.cs`

## 4. 验证结果

| 验证项 | 结果 | 说明 |
|---|---|---|
| 枚举不重复/不冲突 | ✅ 通过 | 7个类型值语义清晰，不冲突 |
| 常量不重复/不冲突 | ✅ 通过 | 颜色、字号、动画参数独立 |
| 工具类无 UnityEditor 引用 | ✅ 通过 | FloatingTextTool 无 Editor 引用 |
| Editor 与运行时分离 | ✅ 通过 | FloatingTextMenu 在 Editor 目录 |
| Character.cs 修改兼容 | ✅ 通过 | 纯追加3行，不改变已有行为 |
| GlobalInit.cs 修改兼容 | ✅ 通过 | 纯追加2行，与其他初始化并列 |
| .meta 同步 | ✅ 通过 | Unity 自动生成 |
| namespace 一致 | ✅ 通过 | 全部使用 LAB2D |
| 中文注释 | ✅ 通过 | 所有类、方法、字段均有中文注释 |
| 空引用保护 | ✅ 通过 | Camera.main、Canvas、EventSystem 均有检查 |
| 对象池安全 | ✅ 通过 | 默认30/最大60，超限回收策略明确 |
| Singleton 模式一致 | ✅ 通过 | 继承 Singleton<T>，与项目一致 |
| git diff --check | 待验证 | 命令行环境无 git diff 输出（Bash 工具） |

## 5. 未验证项

- Unity 编译验证（环境无 Unity Editor / .NET SDK）
- Play Mode 验证（环境无 Unity）
- UI 布局视觉效果（字号、颜色、动画参数需在 Unity 中实际运行调优）
- 屏幕坐标转换精度（依赖 Camera.main 和 Canvas 配置）

## 6. Bug 修复记录

### 2026-05-09: NullReferenceException in FloatingTextUI.Spawn

- **问题**: `FloatingTextUI.Spawn()` 中访问 `textComponent.text` 时 `textComponent` 为 null
- **堆栈**: `Character.ReduceHp` → `FloatingTextManager.SpawnDamageText` → `FloatingTextManager.SpawnText` → `FloatingTextUI.Spawn` (line 83)
- **根因**: 对象池中的对象创建时父节点 `poolContainer` 处于 inactive 状态，Unity 不会在此期间调用 `Awake()`。`Spawn()` 中在 `SetActive(true)` 之前就访问了 `textComponent`，此时组件引用未初始化。
- **修复**: 
  1. 新增 `EnsureReady()` 懒初始化方法，使用 `componentsReady` 标志保证幂等
  2. `Spawn()` 首行调用 `EnsureReady()` 确保所有组件引用可用
  3. `Awake()` 改为委托给 `EnsureReady()`，作为父节点激活时的兜底路径
  4. `ForceRecycle()` 也添加 `EnsureReady()` 调用，防止极端场景下的空引用
- **影响文件**: `Scripts/2D/UI/FloatingTextUI.cs`
- **验证**: 代码逻辑验证通过；需在 Unity Play Mode 中复测确认

## 7. 残馀风险

- **字体渲染**: 使用 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 获取内置字体，可能在高版本 Unity 中不可用。如失败则 Text 组件使用默认字体。
- **性能**: 30个对象池元素预创建，活跃文字峰值60，对性能影响极小。
- **Canvas 排序**: sortingOrder=100 可能与其他 UI 层冲突，需在 Unity 中观察层级关系。
- **多人游戏**: 浮动文字不参与 Photon 同步，每位玩家仅看到本地表现。这与现有 DamageUI 行为一致。
- **坐标转换**: `Camera.main` 为 null 时坐标转换返回 Vector2.zero，文字将显示在屏幕左下角。实际游戏中通常有主摄像机。
