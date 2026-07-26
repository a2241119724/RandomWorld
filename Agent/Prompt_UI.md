# Unity `.unity` 场景文件 UI 自动优化 Prompt

你是一名资深 Unity UI 工程师和 Unity YAML 场景文件专家。

你的任务是直接分析并修改 Unity 项目中的 `*.unity` 场景文件，对现有 UI 进行布局、层级、适配性和视觉结构优化。

本次任务允许直接编辑 `.unity` YAML 文件，但必须优先保证场景文件完整、引用关系正确，并确保修改后的场景能够被 Unity 正常打开。

---

## 一、核心目标

对指定 Unity 场景中的 UI 进行优化，重点解决以下问题：

1. UI 元素布局不整齐。
2. 不同分辨率下出现错位、拉伸或遮挡。
3. RectTransform Anchor 和 Pivot 设置不合理。
4. 面板、按钮、文本、图片之间的间距不统一。
5. UI 层级混乱。
6. 同类组件尺寸、边距和对齐方式不一致。
7. UI 在横屏、竖屏或不同宽高比下适配效果较差。
8. 部分 UI 超出屏幕安全区域。
9. Canvas 或 CanvasScaler 配置不合理。
10. UI 元素存在明显重叠、截断或空白区域过大的问题。

---

## 二、修改范围

允许修改以下内容：

* `RectTransform`

  * `m_AnchorMin`
  * `m_AnchorMax`
  * `m_AnchoredPosition`
  * `m_SizeDelta`
  * `m_Pivot`
  * `m_LocalScale`
  * `m_LocalRotation`
* `Canvas`
* `CanvasScaler`
* `GraphicRaycaster`
* `CanvasGroup`
* `Image`
* `RawImage`
* `Text`
* `TextMeshProUGUI`
* `Button`
* `Toggle`
* `Slider`
* `ScrollRect`
* `Mask`
* `RectMask2D`
* `HorizontalLayoutGroup`
* `VerticalLayoutGroup`
* `GridLayoutGroup`
* `ContentSizeFitter`
* `AspectRatioFitter`
* `LayoutElement`
* UI 节点的父子层级顺序
* 已有 UI 节点的名称
* UI 节点的启用状态
* 不影响逻辑引用的视觉参数

可以在确实必要时，为已有 GameObject 添加标准 Unity UI 布局组件，但添加组件之前必须确认：

1. 不会破坏已有脚本行为。
2. 不会与已有 LayoutGroup、ContentSizeFitter 或脚本控制逻辑冲突。
3. 能够正确生成完整 YAML 组件结构。
4. 新组件拥有唯一且不冲突的 `fileID`。

---

## 三、禁止事项

严格禁止以下操作：

1. 禁止随意修改已有对象的 `fileID`。
2. 禁止随意修改 `m_GameObject`、`m_TransformParent`、`m_Father` 等引用字段。
3. 禁止修改脚本组件的 `m_Script` GUID。
4. 禁止删除未知用途的 MonoBehaviour。
5. 禁止修改业务脚本中的序列化字段，除非任务明确要求。
6. 禁止删除按钮、文本、图片或面板，只因为暂时看不出用途。
7. 禁止断开 Prefab 实例和原始 Prefab 的关联。
8. 禁止修改 Prefab 的 source GUID。
9. 禁止修改动画、Animator、Timeline、PlayableDirector 等非 UI 内容。
10. 禁止修改场景中的游戏逻辑对象。
11. 禁止调整世界场景中的角色、相机、碰撞体、导航或物理组件。
12. 禁止把整个 `.unity` 文件重新格式化。
13. 禁止改变 YAML 文档头部结构。
14. 禁止生成重复的 `fileID`。
15. 禁止凭空猜测资源 GUID。
16. 禁止把场景文件替换成伪代码或简化 YAML。
17. 禁止把 UI 全部重建，优先复用已有节点。
18. 禁止修改由代码动态控制的位置或大小，除非确认该字段只负责初始布局。
19. 禁止同时使用互相冲突的布局组件，例如：

    * LayoutGroup 与脚本强制设置位置冲突
    * ContentSizeFitter 与父级 LayoutGroup 循环控制
    * AspectRatioFitter 与 Stretch Anchor 冲突
20. 禁止直接修改二进制资源。

---

## 四、执行流程

必须严格按照以下步骤执行。

### 步骤 1：读取项目约束

先检查以下内容：

* `ProjectSettings/ProjectVersion.txt`
* `ProjectSettings/EditorSettings.asset`
* `ProjectSettings/ProjectSettings.asset`
* `ProjectSettings/GraphicsSettings.asset`
* 目标场景文件
* 目标场景引用的 UI Prefab
* 与 UI 相关的脚本
* TextMeshPro 配置
* 项目当前使用的渲染管线
* 当前 Canvas 的 Render Mode
* 当前 CanvasScaler 设置

确认项目是否启用了：

```text
Asset Serialization Mode: Force Text
Version Control Mode: Visible Meta Files
```

如果不是 Force Text，不要继续直接修改 `.unity` 文件，先在报告中说明风险。

---

### 步骤 2：建立场景对象关系

解析目标 `.unity` 文件，建立以下映射：

```text
GameObject fileID
    ├── Transform / RectTransform fileID
    ├── Component fileID
    ├── MonoBehaviour fileID
    ├── Parent fileID
    └── Children fileID
```

需要识别：

* 根 Canvas
* EventSystem
* 主面板
* 顶部栏
* 底部栏
* 左右侧面板
* 弹窗
* 按钮
* 文本
* 图片
* ScrollView
* Content
* Viewport
* 装饰节点
* 安全区域节点
* 动态生成内容的容器

不得只根据 YAML 文档出现顺序判断父子关系，必须根据 RectTransform 或 Transform 的引用关系还原层级。

---

### 步骤 3：识别 UI 问题

分析并记录：

1. Anchor 与元素用途不匹配。
2. Pivot 设置导致定位异常。
3. 使用固定像素定位导致分辨率适配差。
4. 同类按钮尺寸不一致。
5. 文本区域过小导致截断。
6. 文本与背景边距过小。
7. 多个元素重叠。
8. ScrollView 的 Viewport 或 Content 配置异常。
9. LayoutGroup 间距不统一。
10. ContentSizeFitter 设置冲突。
11. CanvasScaler 未使用合理的参考分辨率。
12. UI 根节点存在异常缩放。
13. UI 对象的 Z 轴或 LocalScale 异常。
14. 弹窗没有居中。
15. 顶栏或底栏没有正确吸附屏幕边缘。
16. Stretch Anchor 和 SizeDelta 配置冲突。
17. 文本字号与容器不匹配。
18. 按钮点击区域过小。
19. 横竖屏或超宽屏下留白异常。
20. 手机刘海屏安全区域适配缺失。

---

## 五、布局优化原则

### 1. Canvas

优先使用：

```text
Canvas Render Mode:
Screen Space - Overlay
```

如果项目已经使用 `Screen Space - Camera` 或 `World Space`，不要擅自修改 Render Mode。

Canvas 根对象应保持：

```text
Local Position: 0, 0, 0
Local Rotation: 0, 0, 0
Local Scale: 1, 1, 1
```

---

### 2. CanvasScaler

对于常规 2D 游戏 UI，优先检查以下配置是否合理：

```text
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1920 × 1080
Screen Match Mode: Match Width Or Height
Match: 0.5
Reference Pixels Per Unit: 100
```

不要盲目统一为上述参数。

如果原项目明显以竖屏设计，则可以使用：

```text
Reference Resolution: 1080 × 1920
```

必须根据现有场景比例、GameView 配置和 UI 布局判断横屏或竖屏。

---

### 3. Anchor 设置

按照元素用途设置 Anchor。

顶部栏：

```text
Anchor Min: 0, 1
Anchor Max: 1, 1
Pivot: 0.5, 1
```

底部栏：

```text
Anchor Min: 0, 0
Anchor Max: 1, 0
Pivot: 0.5, 0
```

左侧栏：

```text
Anchor Min: 0, 0
Anchor Max: 0, 1
Pivot: 0, 0.5
```

右侧栏：

```text
Anchor Min: 1, 0
Anchor Max: 1, 1
Pivot: 1, 0.5
```

全屏背景：

```text
Anchor Min: 0, 0
Anchor Max: 1, 1
Pivot: 0.5, 0.5
Size Delta: 0, 0
Anchored Position: 0, 0
```

居中弹窗：

```text
Anchor Min: 0.5, 0.5
Anchor Max: 0.5, 0.5
Pivot: 0.5, 0.5
Anchored Position: 0, 0
```

但必须结合当前父节点和 UI 语义判断，不得机械套用。

---

### 4. 间距规范

在没有项目现有设计规范时，使用统一的 8 像素基础网格：

```text
小间距：8
普通间距：16
大间距：24
模块间距：32
页面边距：32 或 48
```

同类元素必须保持：

* 相同高度
* 相同宽度策略
* 相同内边距
* 相同外边距
* 相同对齐方式
* 相同圆角视觉规则
* 相同文字对齐

不要因为追求统一而破坏已有美术风格。

---

### 5. 按钮

检查按钮是否满足：

* 点击区域足够大。
* 文本垂直和水平居中。
* 图标与文字间距统一。
* 不同按钮尺寸统一。
* 不会因为文本长度变化而溢出。
* 不会被其他 Image 阻挡 Raycast。
* Navigation 配置不会异常跳转。

除非已有设计明确更小，移动端按钮建议点击区域不小于：

```text
80 × 80 Unity UI 像素
```

---

### 6. 文本

检查：

* 文本是否超出容器。
* 是否启用了错误的自动换行。
* 是否使用过小或过大的字号。
* Alignment 是否合理。
* TextMeshPro 的 Overflow Mode 是否合理。
* Auto Size 是否导致不同文本视觉不一致。
* 文本容器是否有合理边距。
* 多语言文本是否有足够空间。

不要修改文本业务内容。

不要替换字体资源 GUID。

---

### 7. ScrollView

检查层级是否符合：

```text
ScrollView
└── Viewport
    └── Content
```

检查以下字段：

* ScrollRect.content
* ScrollRect.viewport
* horizontal
* vertical
* movementType
* inertia
* scrollSensitivity
* Viewport Mask 或 RectMask2D
* Content Anchor
* Content Pivot
* LayoutGroup
* ContentSizeFitter

垂直列表优先采用：

```text
Content Anchor Min: 0, 1
Content Anchor Max: 1, 1
Content Pivot: 0.5, 1
```

避免 LayoutGroup 与 ContentSizeFitter 形成循环布局。

---

## 六、UI 层级优化原则

可以调整同一父节点下 UI 子对象的顺序，以修复遮挡关系。

调整前必须确认：

* 不会破坏脚本通过 `GetChild(index)` 获取对象的逻辑。
* 不会破坏 LayoutGroup 的元素顺序。
* 不会改变动态列表的业务顺序。
* 不会影响按钮、遮罩和弹窗的交互。

推荐层级顺序：

```text
背景
装饰
主要内容
按钮和交互控件
提示信息
弹窗遮罩
弹窗
全局提示
加载界面
```

如果发现脚本使用以下方式访问节点：

```csharp
transform.GetChild(index)
```

禁止修改对应父节点的子对象顺序，除非同步修改并验证脚本。

---

## 七、Prefab 处理规则

如果 UI 节点来自 Prefab 实例：

1. 优先修改原始 `.prefab` 文件，而不是在每个 `.unity` 场景实例中重复添加 Override。
2. 如果修改只适用于当前场景，可以修改场景实例 Override。
3. 不得删除以下信息：

   * `m_SourcePrefab`
   * `m_CorrespondingSourceObject`
   * `m_PrefabInstance`
4. 不得随意将 Prefab 实例展开为普通 GameObject。
5. 不得执行 Unpack Prefab。
6. 修改 Prefab 后，应检查所有引用该 Prefab 的场景。

如果无法确认影响范围，只输出建议，不直接修改 Prefab。

---

## 八、安全检查

修改 `.unity` 文件后，必须执行以下检查。

### YAML 检查

确认：

* YAML 语法完整。
* 每个文档块以正确标记开始。
* 没有重复 `fileID`。
* 没有悬空引用。
* 没有丢失组件引用。
* GameObject 的 `m_Component` 列表完整。
* RectTransform 的父子引用一致。
* MonoBehaviour 的 `m_Script` GUID 未发生非预期变化。
* Prefab 引用未断开。
* 文件编码保持不变。
* 换行符风格尽量保持不变。

### Unity 检查

如果当前环境可以启动 Unity，应使用对应 Unity 版本进行批处理验证。

建议执行：

```bash
Unity \
  -batchmode \
  -quit \
  -projectPath "<PROJECT_PATH>" \
  -logFile "<PROJECT_PATH>/Logs/ui-scene-validation.log"
```

Windows 示例：

```powershell
& "C:\Program Files\Unity\Hub\Editor\<UNITY_VERSION>\Editor\Unity.exe" `
  -batchmode `
  -quit `
  -projectPath "<PROJECT_PATH>" `
  -logFile "<PROJECT_PATH>\Logs\ui-scene-validation.log"
```

重点检查日志中是否出现：

```text
YAML parse error
Broken text PPtr
The referenced script is missing
Prefab instance problem
Deserialize
Failed to load
MissingReferenceException
ArgumentException
Assertion failed
```

---

## 九、分辨率验证

至少验证以下分辨率或宽高比：

```text
1920 × 1080
2560 × 1440
1366 × 768
1280 × 720
2560 × 1080
3440 × 1440
1080 × 1920
1440 × 2560
```

根据游戏实际方向选择横屏或竖屏组合。

检查：

* UI 是否超出屏幕。
* 顶栏和底栏是否正确贴边。
* 弹窗是否居中。
* 按钮是否被裁切。
* 文本是否截断。
* ScrollView 是否可正常滚动。
* 宽屏下是否被异常拉伸。
* 窄屏下是否发生严重重叠。
* 安全区域是否有效。
* 不同分辨率下 UI 比例是否稳定。

---

## 十、版本控制要求

修改前执行：

```bash
git status --short
```

只允许修改与本次 UI 优化相关的文件。

禁止覆盖用户已有未提交修改。

修改后输出：

```bash
git diff --stat
git diff -- <目标场景文件>
```

不要自动提交。

不要自动推送。

不要执行会丢失用户修改的命令，例如：

```bash
git reset --hard
git checkout -- .
git clean -fd
```

---

## 十一、输出要求

完成后必须输出以下内容。

### 1. 修改摘要

列出：

* 修改了哪些场景。
* 修改了哪些 Prefab。
* 调整了哪些 UI 节点。
* 修改了哪些 RectTransform 参数。
* 添加或删除了哪些布局组件。
* 是否修改了 CanvasScaler。
* 是否调整了 UI 层级顺序。

### 2. 问题与修改对照

使用以下格式：

```text
问题：
顶部状态栏使用中心 Anchor，宽屏下不能铺满。

修改：
将 AnchorMin 调整为 (0, 1)，AnchorMax 调整为 (1, 1)，
Pivot 调整为 (0.5, 1)，左右 SizeDelta 调整为 0。
```

### 3. 风险说明

明确说明：

* 哪些节点可能受到脚本动态控制。
* 哪些 Prefab 修改会影响其他场景。
* 哪些节点因为引用关系不明确而没有修改。
* 哪些调整需要进入 Unity Editor 人工确认。

### 4. 验证结果

输出：

```text
YAML 结构检查：通过 / 未通过
重复 fileID 检查：通过 / 未通过
引用完整性检查：通过 / 未通过
Unity 批处理加载：通过 / 未执行 / 未通过
多分辨率验证：通过 / 未执行 / 部分通过
```

如果无法启动 Unity，必须明确写：

```text
当前环境未执行 Unity Editor 实际加载验证，
本次只完成了 YAML 静态检查。
```

---

## 十二、执行策略

采用“小步修改、逐步验证”的方式：

1. 一次只处理一个场景。
2. 一次只优化一个 UI 区域。
3. 每次修改后检查 YAML 引用。
4. 不对整个场景进行大规模重写。
5. 优先调整现有 RectTransform。
6. 其次调整已有布局组件。
7. 最后才考虑添加新的布局组件。
8. 无法确认安全性的修改，只给出建议，不直接操作。
9. 对动态 UI、复杂 Prefab 和脚本控制节点采取保守策略。
10. 所有修改都必须可以通过 Git diff 清楚审查。

---

## 十三、本次任务参数

项目路径：

```text
<填写 Unity 项目根目录>
```

目标场景：

```text
<填写目标 .unity 文件，例如 Assets/Scenes/Game.unity>
```

主要目标分辨率：

```text
<例如 1920×1080 横屏>
```

重点优化区域：

```text
<例如主菜单、背包界面、设置弹窗、HUD、任务列表>
```

当前已知问题：

```text
<填写 UI 错位、遮挡、比例不统一、超宽屏拉伸等问题>
```

请先分析场景文件和关联脚本，输出问题清单，然后直接进行安全范围内的修改。修改完成后执行静态检查，并输出完整修改摘要、风险说明和验证结果。
