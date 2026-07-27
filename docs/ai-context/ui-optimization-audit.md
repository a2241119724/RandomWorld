# RandomWorld UI 优化 — 5轮循环综合审计报告

> 日期: 2026-07-27 | 分支: feature/dev-0.1.3 | 执行模式: AUTO_SELECT × 5 轮

---

## 1. 执行摘要

5 轮自主优化共修改 **2 个场景文件** (10 行变更)、生成 **1 个 HTML 原型**。所有修改均为低风险颜色值对齐，零结构/引用/事件变更。

| 轮次 | 目标 | 模式 | 修改数 | 状态 |
|:---:|---|---|:---:|:---:|
| 1 | Menu.unity — StartGame 区域 | SAFE_EDIT | 3 | ✅ |
| 2 | RigisterOrLogin.unity — 登录表单 | SAFE_EDIT | 4 | ✅ |
| 3 | Menu.unity — NoteClose 按钮反馈 | SAFE_EDIT | 1 | ✅ |
| 4 | Menu.unity — HTML 像素主题原型 | PROTOTYPE | 1 文件 | ✅ |
| 5 | 全项目审计 + Editor 工具规划 | AUDIT | 本报告 | ✅ |

---

## 2. 变更清单

### Menu.unity (7 行变更)

| # | 对象 | 属性 | 旧值 | 新值 | 依据 |
|---|---|---|---|---|---|
| 1 | Clause/Label | m_Color | `#000000` (pure black) | `#4A3728` (TextPrimary) | 同场景其他 Text 一致 |
| 2 | Start 按钮 | HighlightedColor | `#F6C8C8` (custom) | `#FCC8D5` (ButtonHighlighted) | PixelUITheme |
| 3 | Start 按钮 | PressedColor | `#C3C3C3` (gray) | `#F9D56E` (ButtonPressed) | PixelUITheme |
| 4 | NoteClose 按钮 | HighlightedColor | `rgba(0,0,0,0.20)` (≡Normal) | `rgba(0,0,0,0.39)` (×2) | Hover 反馈缺失修复 |

### RigisterOrLogin.unity (8 行变更)

| # | 对象 | 属性 | 旧值 | 新值 | 依据 |
|---|---|---|---|---|---|
| 5-6 | Register 按钮 | Highlighted/PressedColor | Unity default | PixelUITheme | 与项目主题一致 |
| 7-8 | Login 按钮 | Highlighted/PressedColor | Unity default | PixelUITheme | 与项目主题一致 |

### 新增文件

| 文件 | 说明 |
|---|---|
| `docs/ai-context/menu-prototype.html` | Menu StartGame 完整像素主题 HTML 原型，含 Scale=1.0 补偿规格 |

---

## 3. 全项目 UI 审计发现

### 3.1 已发现模式

| 模式 | 场景文件 (Scenes/) | Prefab 文件 (ResourcesLocal/) |
|---|---|---|
| Legacy Text 字体 | **Arial** (built-in) ❌ | **ark-pixel** ✅ |
| Button 颜色 | **Unity default** (部分) ❌ | **PixelUITheme** ✅ |
| TextPrimary 颜色 | **不一致** (部分已修复) | **一致** ✅ |
| Scale ≠ 1.0 | **StartGame/InputField 等** (0.5) ⚠️ | **Text 子节点** (0.5) ⚠️ |
| m_SelectedTrigger | 按钮: Highlighted / InputField: Selected | 按钮: Selected |

**结论**: 项目存在"场景 UI 未同步主题"的系统性问题。Prefab 实例均已正确配置，但场景内建的静态 GameObject 使用了 Unity 默认值。

### 3.2 各场景未处理问题

#### Menu.unity

| 优先级 | 问题 | 风险 | 建议 |
|---|---|---|---|
| P2 | 所有 Legacy Text 使用 Arial → 应切换 ark-pixel | 中 | Editor 工具批处理 + 视觉验证 |
| P2 | StartGame/InputField Scale=0.5 → 应 Scale=1.0 + 补偿 | 中 | Editor 工具等比放大 |
| P3 | Clause Toggle m_TargetGraphic 为 null | 低 | 手动设置指向 Background Image |

#### RigisterOrLogin.unity

| 优先级 | 问题 | 风险 | 建议 |
|---|---|---|---|
| P2 | 所有 Legacy Text 使用 Arial | 中 | 同 Menu.unity |
| P2 | RegisterAndLogin Scale=0.5 | 中 | 同 Menu.unity |
| P2 | InputField 颜色为 Unity default | 低 | 对齐 PixelUITheme 按钮色 |
| P3 | Btns HorizontalLayoutGroup padding L150/R160 不对称 | 低 | 统一为 L150/R150 或居中 |

#### Game.unity

| 优先级 | 问题 | 风险 | 建议 |
|---|---|---|---|
| P2 | 30+ 组件使用 Unity default HighlightedColor | 高 | **必须通过 Editor 工具处理**，不可手改 YAML |
| P2 | 18+ 组件使用 pure black 文本色 | 高 | 同上 |
| P1 | 大量脚本运行时控制 UI 状态，必须保留所有引用 | 阻断 | 任何修改前必须建立完整运行时引用图谱 |

---

## 4. Editor 工具规范 (建议)

### 4.1 字体批量替换工具

```
路径: Assets/Scripts/2D/Editor/UIFontReplacer.cs
功能:
  - 选择目标场景 (.unity) 或 Prefab
  - 扫描所有 Legacy Text 组件 (guid: 5f7201a12d95ffc409449d95f23cf332)
  - 将 m_Font 从 {fileID: 10102, guid: 0000000000000000e000000000000000}
    替换为 {fileID: 12800000, guid: 994464cadda06394eb1598617cdd2c57}
  - 使用 Undo.RecordObject 支持撤销
  - 限定目标范围 (按 GameObject 名称前缀或 Tag 过滤)
  - 替换后自动标脏并刷新 AssetDatabase
安全:
  - 不修改 m_Script、m_Text、m_Color
  - 不触碰非 Legacy Text 组件
```

### 4.2 Scale 补偿工具

```
路径: Assets/Scripts/2D/Editor/UIScaleNormalizer.cs
功能:
  - 选择 Scale ≠ (1,1,1) 的 RectTransform
  - 自动计算补偿: Scale → 1.0, SizeDelta ×2, FontSize ×2
  - 递归处理子节点
  - 使用 Undo 全程可撤销
  - 白名单过滤 (跳过已知需要 Scale 的特效节点)
安全:
  - 不修改 Anchor/Pivot
  - 不修改 LayoutGroup/ContentSizeFitter
  - 不在 LayoutGroup 直接子节点上执行
```

### 4.3 按钮颜色对齐工具

```
路径: Assets/Scripts/2D/Editor/UIButtonColorAligner.cs
功能:
  - 扫描 Button/InputField/Toggle 组件
  - 检测使用 Unity default 颜色的组件
  - 根据组件类型自动对齐 PixelUITheme:
    - Button → ButtonNormal/Highlighted/Pressed/Selected/Disabled
    - InputField → 白色 Normal, 微粉 Highlight/Focus
    - Toggle → 白色 Normal, Selected 绿色
  - 支持按场景/Prefab/选中范围执行
```

---

## 5. 已排除的候选

| 候选 | 排除原因 |
|---|---|
| Game.unity | 63K+ 行、30+ 组件，大量运行时脚本依赖，全自动修改风险不可控 |
| ItemBox/*.prefab | 已使用 PixelUITheme 颜色 + ark-pixel 字体，无需修改 |
| Tip.prefab | 已使用 ark-pixel 字体，颜色设计合理 |
| 第三方 Scripts/Reference | Prompt 明确禁止 |

---

## 6. 静态验证结论

```
YAML 完整性:        ✅ 通过 — 仅值变更，无结构破坏
fileID 唯一性:       ✅ 通过 — 无 fileID 修改
m_Script GUID:      ✅ 通过 — 所有组件 GUID 未变
Prefab 关系:         ✅ N/A — 修改为场景内建对象
按钮事件:            ✅ 通过 — PersistentCall 未变
业务文本:            ✅ 通过 — m_Text 内容未变
编码/换行:           ✅ 通过 — 仅值替换
无关大面积 diff:     ✅ 通过 — diff 精确匹配修改项
```

---

## 7. 下一步建议

1. **P0**: 创建上述 Editor 工具（字体替换 + Scale 补偿），用工具而非手改 YAML 处理 Game.unity
2. **P1**: 在 Editor 中打开 `docs/ai-context/menu-prototype.html` 作为视觉参考
3. **P2**: 批量替换场景中的 Arial → ark-pixel 字体（用 Editor 工具）
4. **P2**: 消除 Scale=0.5 hack（用 Editor 工具等比补偿）
5. **P3**: 统一 RigisterOrLogin Btns 的 LayoutGroup padding
