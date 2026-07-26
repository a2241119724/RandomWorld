# Unity UI 场景优化与视觉精炼 Prompt

你是 Unity UI 工程师和 YAML 场景文件专家。对 **RandomWorld** 项目的 `.unity` 场景和 `.prefab` 预制体进行布局优化与视觉精炼——直接编辑 YAML，保证文件完整、引用正确、Unity 正常打开。

---

## 一、项目约束（必须首先理解）

### 1.1 基础配置

| 项 | 值 |
|----|-----|
| Unity | 2022.3.62f2c1 |
| 序列化 | Force Text (`m_SerializationMode: 2`) |
| 渲染管线 | URP |
| 游戏类型 | 2D 殖民地模拟 / RPG，横屏（`defaultScreenOrientation: 4`） |
| 美术风格 | 可爱像素风（`PixelUITheme`） |
| 字体 | `ark-pixel-12px-monospaced-zh_cn`（`UIFontConfig` 管理） |
| 文本系统 | Legacy `Text` + `TextMeshProUGUI` 混用 |

### 1.2 Canvas 配置（禁止修改 Render Mode）

```text
Render Mode: Screen Space - Camera（m_RenderMode: 1）← 禁止改为 Overlay
UI Scale Mode: Scale With Screen Size（m_UiScaleMode: 1）
Reference Resolution: 1920 × 1080
Screen Match Mode: Match Width Or Height（m_ScreenMatchMode: 0）
Match: 0（优先匹配宽度）
```

Canvas 上挂载了自定义初始化脚本（`initPanel` / `initFont`, GUID `4d8eda0c8c2b6aa4e9d2ab21723ce68c`），修改 Canvas 时不得移除或改动该 MonoBehaviour。

### 1.3 UI 架构

- **根节点**：Tag = `"UIRoot"`（`TagConstant.UI_TAG`），代码通过 `GameObject.FindGameObjectWithTag("UIRoot")` 查找。禁止修改此 Tag。
- **面板系统**：`PanelController` 维护 `Stack<IBasePanel>`，基类 `ABasePanel<BP>`。面板通过 `Panel.Name` 匹配 UIRoot 下的子节点名称。
- **动态加载**：面板通过 `ResourceManager.Instantiate()` 从 `Resources/` 运行时加载——不是所有 UI 都在场景 YAML 中。
- **Prefab 存放**：`Resources/` 和 `ResourcesLocal/Prefabs/`（ItemBox 系列列表项等）。

### 1.4 脚本查找子节点的三种模式（修改名称/层级时必须评估影响）

```csharp
// ① 按名称递归查找
FindChildTransform(panelTransform, "SaveSlotPanel")
transform.Find("Center/Note")

// ② 按名称查组件（项目中最常用）
Tool.GetComponentInChildren<Button>(gameObject, "Start")
Tool.GetComponentInChildren<Text>(gameObject, "PlayerName")

// ③ GetChild(index) —— 修改父节点子顺序会直接影响
transform.GetChild(i)
```

**规则**：模式①②保持节点名称不变；模式③禁止修改子对象顺序。

### 1.5 PixelUITheme 配色

| 用途 | 颜色 | 色值 |
|------|------|------|
| 主按钮 Normal / Hover / Pressed / Selected | 粉 → 浅粉 → 金黄 → 薄荷绿 | `#F2A0AF` / `#FCC8D5` / `#F9D56E` / `#7ECB9A` |
| 危险按钮 | 珊瑚红 | `#E8837A` |
| 对话框背景 | 暖白 (alpha 245) | `#FFF5EC` |
| 模态遮罩 | 深棕 (alpha 0.5) | `#4A3829` |
| 文本主色 / 次要 / 强调 | 深棕 / 灰棕 / 粉红 | `#4A3728` / `#8B7D72` / `#E85D75` |
| 血量 / 魔法 / 经验 | 珊瑚 / 淡紫 / 金黄 | `#F27A6B` / `#C5B4E3` / `#F9D56E` |
| 正面 / 信息 / 特殊 | 薄荷绿 / 天蓝 / 薰衣草 | `#7ECB9A` / `#7CB8E4` / `#C5B4E3` |

修改颜色时**优先使用上表中的值**。不要因追求统一而破坏像素风美术风格。

---

## 二、修改范围

### 允许修改

| 类别 | 具体字段 |
|------|----------|
| **RectTransform** | `m_AnchorMin`, `m_AnchorMax`, `m_AnchoredPosition`, `m_SizeDelta`, `m_Pivot`, `m_LocalScale`, `m_LocalRotation` |
| **Canvas** | `m_RenderMode` 除外 |
| **CanvasScaler** | `m_ReferenceResolution`, `m_MatchWidthOrHeight`, `m_ScreenMatchMode` |
| **Image/RawImage** | `m_Color`（含 alpha） |
| **Text / TMP** | `m_FontSize`, `m_Color`, `m_Alignment`, `m_LineSpacing`, `m_OverflowMode` |
| **Button** | `m_Colors` 状态色 |
| **LayoutGroup** | `m_Spacing`, `m_Padding`, `m_ChildAlignment` |
| **ScrollRect** | `horizontal`, `vertical`, `movementType`, `scrollSensitivity` |
| **其他** | `CanvasGroup.alpha`, 层级顺序, 节点启用状态, 节点名称（确认无代码引用时） |

必要时可为已有 GameObject 添加布局组件（LayoutGroup / ContentSizeFitter / LayoutElement），前提：fileID 唯一、不破坏脚本行为、不与已有组件冲突。

### 严格禁止

1. 修改已有 `fileID`、`m_Script` GUID、Prefab source GUID
2. 修改 `m_GameObject`、`m_TransformParent`、`m_Father` 等引用字段
3. 删除未知用途的 MonoBehaviour 或业务序列化字段
4. 断开 Prefab 关联（`m_SourcePrefab` / `m_PrefabInstance`）
5. Unpack Prefab 或将 UI 全部重建
6. 修改动画、Animator、游戏逻辑对象、非 UI 内容
7. 重新格式化整个 `.unity` / `.prefab` 文件、改变 YAML 头部结构
8. 生成重复 `fileID`、凭空猜测资源 GUID
9. 切换 Canvas Render Mode
10. 修改由代码动态控制的位置/大小（除非确认仅用于初始布局）
11. 布局组件冲突（LayoutGroup + 脚本强设位置、ContentSizeFitter + 父 LayoutGroup 循环）
12. 修改字体资源 GUID、文本业务内容

---

## 三、设计规范

### 3.1 字号层级（像素字体：12px 整数倍）

| 层级 | 字号 | 用途 |
|------|------|------|
| 标题 | 36 | 面板标题、结算大标题 |
| 副标题 | 24 | 区块标题、Tab 标签 |
| 正文 | 12（单行）或 24（多行） | 物品名、描述、按钮文字 |
| 辅助 | 12 | 数量、倒计时、提示小字 |

行间距 ≥ 字号的 1.3 倍。同一面板内不超过 2 种字体。

### 3.2 间距规范（8px 像素网格）

| 用途 | 值 |
|------|-----|
| 元素间最小间距 | 8 |
| 常规元素间距 | 16 |
| 分组间距 | 24 |
| 面板内边距 | 16-24 |
| 页面边距 | 32 |
| 按钮最小点击区 | 44×44（推荐 48×48） |

同类元素必须保持相同高度、宽度策略、内外边距、对齐方式。

### 3.3 Anchor 速查

| 元素类型 | Anchor Min | Anchor Max | Pivot |
|----------|------------|------------|-------|
| 顶栏吸附 | (0, 1) | (1, 1) | (0.5, 1) |
| 底栏吸附 | (0, 0) | (1, 0) | (0.5, 0) |
| 左侧栏 | (0, 0) | (0, 1) | (0, 0.5) |
| 右侧栏 | (1, 0) | (1, 1) | (1, 0.5) |
| 全屏拉伸 | (0, 0) | (1, 1) | (0.5, 0.5) |
| 居中固定 | (0.5, 0.5) | (0.5, 0.5) | (0.5, 0.5) |

必须结合父节点和 UI 语义判断，不得机械套用。

### 3.4 视觉层次

- **前景/背景分离**：重要信息（血量、分数、倒计时）用更大字号 + 更亮颜色；装饰元素降低透明度。
- **焦点引导**：打开面板后第一眼落点应是核心内容，而非装饰或次要按钮。
- **留白**：元素之间保留足够间距，分组间距 > 元素间距。
- **状态色差异**：Normal / Hover / Pressed / Disabled 四种状态颜色必须有明确区分。

### 3.5 ScrollView 标准

```text
ScrollView → Viewport（含 Mask）→ Content

垂直列表 Content: AnchorMin (0,1)  AnchorMax (1,1)  Pivot (0.5,1)
```

避免 LayoutGroup + ContentSizeFitter 循环布局。

---

## 四、执行流程

### 步骤 1：扫描目标

- 读取 `ProjectVersion.txt`、`EditorSettings.asset`、`ProjectSettings.asset`、`GraphicsSettings.asset`
- 读取目标 `.unity` 场景和目标 UI 相关 `.prefab`
- 读取相关脚本（`Scripts/2D/UI/` 下的对应文件）
- 确认 Canvas Render Mode 和 CanvasScaler 参数

### 步骤 2：识别问题

解析 YAML，建立 GameObject → Component → Parent/Children 映射（**不得**只按文档出现顺序判断层级）。

逐项检查：

| 类别 | 检查点 |
|------|--------|
| **布局** | Anchor/Pivot 是否匹配用途、Stretch+SizeDelta 冲突、弹窗居中、顶底栏贴边 |
| **间距** | 同类元素间距统一、面板内边距对称、无重叠或异常空白 |
| **尺寸** | 同类按钮/图标/格子尺寸一致、按钮 ≥ 44×44、内容不被截断 |
| **文字** | 字号是否 12px 倍数、层级是否清晰、是否超出容器、行间距是否足够 |
| **颜色** | 语义是否一致、按钮状态色是否区分、是否使用 PixelUITheme 色值 |
| **层级** | 遮挡关系是否正确、是否影响 `GetChild(index)` |
| **ScrollView** | Viewport/Content 层级是否完整、LayoutGroup+ContentSizeFitter 是否冲突 |
| **适配** | CanvasScaler Match 值是否合理、宽屏/窄屏下表现 |

### 步骤 3：确定修改顺序

1. 低风险高收益优先：颜色、字号、间距（不改结构）
2. 中风险其次：Anchor、Pivot、SizeDelta（改 RectTransform 参数）
3. 高风险跳过或只给建议：需要理解业务逻辑、涉及 Prefab 嵌套、脚本动态控制

### 步骤 4：执行修改

- 场景中的 UI → 修改 `.unity` YAML
- Prefab 中的 UI → 修改 `.prefab` YAML，保留 `.meta`
- 每次修改后立即检查 YAML 引用完整性
- 一次只处理一个 UI 区域

### 步骤 5：验证

```text
YAML 检查：
  - 语法完整、文档块标记正确
  - 无重复 fileID、无悬空引用
  - m_Component 列表完整、RectTransform 父子引用一致
  - m_Script GUID 未变化、Prefab 引用未断开

分辨率检查（横屏游戏）：
  - 1920×1080 / 2560×1440 / 1366×768 / 1280×720（16:9）
  - 2560×1080 / 3440×1440（21:9 超宽屏）
  - 1080×1920（竖屏——验证意外情况）

如果环境可启动 Unity，执行批处理验证：
  & "C:\Program Files\Unity\Hub\Editor\2022.3.62f2c1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "D:\LAB\Unity\RandomWorld" `
    -logFile "D:\LAB\Unity\RandomWorld\Logs\ui-scene-validation.log"

  检查日志关键词: YAML parse error / Missing script / Prefab instance problem /
                  Failed to load / MissingReferenceException
```

---

## 五、输出要求

### 修改摘要

- 修改了哪些场景 / Prefab / UI 节点
- 修改了哪些参数（RectTransform / 字号 / 颜色 / 间距 / 层级 / Anchor）
- 是否修改了 CanvasScaler
- 是否修改了节点名称（标注是否影响脚本查找）

### 问题与修改对照

```text
问题：顶部状态栏使用中心 Anchor，宽屏下不能铺满。
修改：AnchorMin (0,1) → AnchorMax (1,1), Pivot (0.5,1), SizeDelta x=0。

问题：Pause/TitleText 字号 12 与正文无区分。
修改：fontSize 12→24, color #8B7D72→#4A3728。
```

### 风险说明

- 哪些节点可能受脚本动态控制、哪些修改影响了按名称查找的节点
- 哪些 Prefab 修改影响多个场景
- 哪些因引用不明确而未修改、哪些需进入 Unity Editor 人工确认

### 验证结果

```text
YAML 结构检查：通过 / 未通过
重复 fileID 检查：通过 / 未通过
引用完整性检查：通过 / 未通过
Unity 批处理加载：通过 / 未执行 / 未通过
多分辨率验证：通过 / 未执行 / 部分通过
```

无法启动 Unity 时：

```text
当前环境未执行 Unity Editor 实际加载验证，本次只完成了 YAML 静态检查。
```

---

## 六、版本控制

```bash
# 修改前
git status --short

# 修改后
git diff --stat
git diff -- <目标文件>
```

不自动提交、不自动推送。禁止 `git reset --hard`、`git checkout -- .`、`git clean -fd`。

---

## 七、任务参数

| 参数 | 值 |
|------|-----|
| 项目路径 | `D:\LAB\Unity\RandomWorld` |
| Unity 版本 | 2022.3.62f2c1 |
| 目标场景 | `Scenes/Menu.unity`, `Scenes/Game.unity`, `Scenes/RigisterOrLogin.unity` |
| 目标 Prefab | `ResourcesLocal/Prefabs/ItemBox/`, `Resources/` 下的 UI Prefab |
| 参考分辨率 | 1920×1080 横屏（16:9, MatchWidthOrHeight, Match=0） |
| Canvas | Screen Space - Camera, Scale With Screen Size |
| UI 根 | Tag `"UIRoot"`, 面板系统 PanelController + ABasePanel |
| 主题 | PixelUITheme 像素风, 字体 ark-pixel-12px |
| 重点区域 | `<主菜单 / 游戏 HUD / 设置弹窗 / 存档面板 / 对话面板 / ItemBox 列表项>` |
| 已知问题 | `<UI 错位、遮挡、比例不统一、超宽屏拉伸、颜色不一致、字号层级混乱>` |
