# UI 视觉审计报告

**日期**: 2026-05-11
**审计范围**: `Scenes/Game.unity` + `ResourcesLocal/Prefabs/` 下所有 .prefab
**参考规范**: PixelUITheme, FloatingTextConstant, SkillConstant 等

---

## 1. 字号审计

### 1.1 Scene 中字号分布

| 字号 | 使用位置 | 用途 | 问题 |
|------|----------|------|------|
| 16 | WaveText | 波次信息标题 | **过小**，作为常驻 HUD 标题阅读困难 |
| 16 | FPS 计数器 | 调试信息 | 合理（调试用） |
| 17 | EventFeedText | 事件列表内容 | 略小，长时间阅读吃力 |
| 18 | WaveScore | 评分信息 | 信息密度高时偏小 |
| 22 | EventFeedTitle | 事件面板标题 | 合理 |
| 40 | 按钮 Title（退出/创建/加入） | 按钮文字 | **过大**，按钮尺寸 200x60 配 40 号字显得拥挤 |
| 40 | SaveSlotPanel Title | 存档面板标题 | 偏大，BestFit 会自动缩小 |
| 42 | ComboBurstText | 连击爆发文字 | 合理（强调文字） |

### 1.2 Prefab 中字号分布

| 字号 | 使用位置 | 用途 | 问题 |
|------|----------|------|------|
| 22 | ArchiveItem 按钮 (Save/Rename/Clear) | 存档槽按钮 | 合理 |
| 30 | Tip.prefab (Content) | 提示信息 | 偏大 |
| 35 | AddWearTaskItem/BackpackItem/BuildItem/LocateWorkerItem/WorkerBedItem | 物品/工人名称 | 基本合理 |
| 40 | ButtonItem/RoomItem/TaskItem/LeftChatItem/RightChatItem | 按钮/聊天文字 | 偏大 |
| 50 | AddWearTaskItem (Name) | 物品项名称 | **过大** |
| 80 | Damage.prefab (Text) | 伤害数字 | 偏大，但作为弹出文字可接受 |

### 1.3 字号层级评估

**问题**: 项目中缺乏统一的字号层级梯度。Scene 中 HUD 字号分布为 16/17/18/22/40/42，跳跃过大（22→40 差 18px）。Prefab 中常用字号为 22/30/35/40/50/80，更混乱。

**建议梯度**: 26 (HUD标题) / 20 (面板标题) / 16 (正文) / 13 (辅助)

### 1.4 字号问题汇总

- [x] **ExperienceHub WaveText 20→22**: 波次标题字号偏小，提高可读性 [DONE] → `uirefine_ExperienceHub`
- [x] **ExperienceHub ScoreText 18→16**: 评分信息作为次要信息适当缩小 [DONE] → `uirefine_ExperienceHub`
- [x] **ExperienceHub EventFeedText 17→16**: 列表内容统一为正文字号 [DONE] → `uirefine_ExperienceHub`
- [x] **ExperienceHub EventFeedTitle 22→24**: 标题与正文拉开差距 [DONE] → `uirefine_ExperienceHub`
- [ ] **按钮 Title 40→28**: 退出/创建/加入按钮文字过大
- [ ] **SaveSlotPanel Title 40→32**: 存档面板标题偏大
- [ ] **AddWearTaskItem Name 50→36**: 物品项名称过大
- [ ] **ButtonItem/RoomItem Text 40→32**: 按钮文字偏大
- [ ] **ChatItem Text 40→28**: 聊天文字偏大

---

## 2. 颜色审计

### 2.1 Scene 颜色分布

| 颜色 (RGBA) | 使用位置 | 语义 | 问题 |
|-------------|----------|------|------|
| (1, 1, 1, 1) | 多处 Text | 通用白色 | 过多位置使用，缺乏语义区分 |
| (0.984, 0.392, 0, 1) | 按钮 Title | 橙红强调 | 与 PixelUITheme.TextAccent (232,93,117) 不一致 |
| (1, 0.886, 0.541, 1) | EventFeedTitle / ComboBurstText | 金色强调 | 合理，与 RichGold (#F9D56E) 接近 |
| (0.988, 0.914, 0.722, 1) | WaveText | 暖金正文 | 可接受 |
| (0.902, 0.929, 0.965, 1) | EventFeedText | 淡蓝灰正文 | 可接受 |
| (0.055, 0.071, 0.094, 0.804) | RightWavePanel bg | 深色面板 | 合理 |
| (0.039, 0.047, 0.071, 0.745) | EventFeedPanel bg | 深色面板 | 与 RightWavePanel 不一致 |
| (0.020, 0.031, 0.047, 0.706) | ResultPanel bg | 遮罩 | 合理 |
| (0.12, 0.13, 0.15, 0.96) | PanelBox (SaveSlot) | 面板背景 | 略亮 |

### 2.2 Prefab 颜色分布

| 颜色 (RGBA) | 使用位置 | 问题 |
|-------------|----------|------|
| (0.204, 0.596, 0.859, 1) #347EDB | Button Normal (通用) | 与 PixelUITheme.ButtonNormal (#F2A0AF 粉色) 完全不同 |
| (0.365, 0.678, 0.886, 1) #5DADE2 | Button Highlighted | 蓝色系 |
| (0.945, 0.769, 0.059, 1) #F1C40F | Button Pressed | 黄色，合理 |
| (0.180, 0.800, 0.443, 1) #2ECC71 | Button Selected | 绿色，合理 |
| (0.196, 0.196, 0.196, 1) #323232 | 多处 Text (Name/Text) | 深灰文字，与 PixelUITheme.TextPrimary (74,55,40) 不一致 |
| (0.011, 1, 0, 1) | BackpackItem/BuildItem ItemInfo | **纯绿色过于刺眼**，应使用薄荷绿 |
| (1, 1, 1, 1) | TaskItem Text / Damage Text / 多处 | 纯白文字使用过多 |
| (0, 0, 0, 0.467) | Tip.prefab bg | 半透明黑提示背景 |
| (0.478, 0.690, 0.549, 1) #7AB08C | ArchiveItem Save 按钮 | 绿色系，与语义一致 |
| (0.949, 0.627, 0.686, 1) | ArchiveItem Rename 按钮 | 粉色系 |
| (0.910, 0.514, 0.478, 1) | ArchiveItem Clear 按钮 | 红色系，与危险操作语义一致 |

### 2.3 颜色语义评估

**问题**:
1. **Button 颜色不统一**: Prefab 按钮用蓝色系 (#347EDB)，但 PixelUITheme 定义用粉色系 (#F2A0AF)。两者差距极大，视觉语言分裂。
2. **文字颜色不统一**: Prefab 文字用深灰 (#323232)，PixelUITheme.TextPrimary 用深棕 (#4A3728)。
3. **BackpackItem/BuildItem ItemInfo 颜色刺眼**: 纯绿 (0, 1, 0) 视觉上过于尖锐，应改用 PixelUITheme.RichMint (#7ECB9A) 或 FloatingTextConstant.HealColor。
4. **Scene 按钮 Title 橙红 (0.984, 0.392, 0) 无对应主题色**: 与 PixelUITheme 中任何颜色都不匹配。
5. **暗色面板背景不一致**: [x] RightWavePanel 与 EventFeedPanel 已统一色值 [DONE] → `uirefine_ExperienceHub`

### 2.4 颜色问题汇总

- [ ] **BackpackItem/BuildItem ItemInfo**: (0.011, 1, 0, 1) → 改用 #7ECB9A (126,203,154)
- [ ] **Prefab Text 颜色统一**: (0.196, 0.196, 0.196) → 改用 TextPrimary (0.29, 0.216, 0.157)
- [x] **Scene RightWavePanel bg 与 EventFeedPanel bg 色值统一** [DONE] → `uirefine_ExperienceHub`
- [ ] **Tip.prefab bg**: alpha 0.467→0.55 提升可读性

---

## 3. 尺寸审计

### 3.1 面板尺寸

| 面板 | 尺寸 | 比例 | 问题 |
|------|------|------|------|
| RightWavePanel | 520x220 | ~2.36:1 | 偏宽，内容适配尚可 |
| EventFeedPanel | 520x210 | ~2.48:1 | 偏宽 |
| ResultCard | 820x650 | ~1.26:1 | 接近 5:4，较合理 |
| PanelBox (SaveSlot) | 720x650 | ~1.11:1 | 接近正方形 |
| Tip.prefab Root | 100x0 (auto) | - | 宽度固定 100 偏小 |

### 3.2 按钮尺寸

| 按钮 | 尺寸 | 问题 |
|------|------|------|
| Scene Exit 按钮 | 200x60 | 合理 |
| ButtonItem | 160x30 | **高度 30 偏矮**，PC 端建议 ≥32 |
| TaskItem Toggle | 47.5x20 | **高度 20 偏矮**，点击区过小 |
| AddWearTaskItem | 0x60 (动态宽度) | 高度合理 |

### 3.3 物品格尺寸

| 预制体 | 尺寸 | 问题 |
|--------|------|------|
| BackpackItem | 100x120 | 合理 |
| BuildItem | 100x120 | 合理，与 BackpackItem 一致 |

### 3.4 尺寸问题汇总

- [ ] **ButtonItem 高度 30→36**: 提升 PC 端点击舒适度
- [ ] **TaskItem Toggle 高度 20→24**: 提升可点击区域
- [ ] **Tip.prefab 宽度 100→160**: 避免中文提示文字截断

---

## 4. 布局审计

### 4.1 GridLayoutGroup / 布局组件使用

| 位置 | 组件 | Spacing | Padding | 问题 |
|------|------|---------|---------|------|
| Scene "Option" | HorizontalLayoutGroup | 0 | 0 | Spacing=0，元素紧贴 |
| TaskItem | GridLayoutGroup | 0 | 0 | Spacing=0，Toggle紧贴 |
| LeftChatItem | VerticalLayoutGroup | 0 | 0 | Spacing=0 |
| RightChatItem | VerticalLayoutGroup | 0 | 0 | Spacing=0 |

### 4.2 边距与对齐

- **Scene**: 大多数面板使用绝对定位，无边距一致性保障
- **RightWavePanel**: 位置 (24, 24)，面板内有文本偏移 (22, -18)，内边距约 22-24px，较统一
- **EventFeedPanel**: 位置 (-24, 24)，与 RightWavePanel 形成对称（一左一右）
- **ChatItem**: padding 全为 0，文字紧贴聊天框边缘
- **TaskItem**: GridLayoutGroup padding 全为 0

### 4.3 锚点

| 元素 | anchorMin | anchorMax | 评估 |
|------|-----------|-----------|------|
| RightWavePanel | (0,0) | (0,0) | 左下角固定定位，适合左上角面板 |
| EventFeedPanel | (1,0) | (1,0) | 右下角固定定位，适合右上角面板 |
| ResultPanel | (0,0) | (1,1) | 全屏遮罩，正确 |
| SaveSlotPanel | (0,0) | (1,1) | 全屏遮罩，正确 |
| PanelBox | (0.5,0.5) | (0.5,0.5) | 居中，正确 |
| Damage.prefab | (0.5,0.5) | (0.5,0.5) | 居中，正确 |
| Tip.prefab | (0.45,0.85) | (0.55,0.9) | 顶部居中 10% 宽区域，**无响应式适配** |
| LeftChatItem root | (0,1) | (0,1) | 左上角定位 |
| BackpackItem | (0,0) | (0,0) | 原点定位（由 GridLayout 驱动） |

**评估**: 锚点设置基本正确，但 Tip.prefab 使用了固定 anchor 范围（0.45-0.55），在大屏/小屏下可能偏移。

### 4.4 布局问题汇总

- [ ] **TaskItem GridLayoutGroup**: spacing 0→4，padding 0→(4,4,4,4)
- [ ] **ChatItem VerticalLayoutGroup**: spacing 0→4, padding 0→(8,8,8,8)
- [ ] **Scene Option HorizontalLayoutGroup**: spacing 0→6
- [ ] **Tip.prefab anchor**: 考虑改为 (0.5, 0.9) 居中顶部

---

## 5. 视觉层次审计

### 5.1 前景/背景分离

| 评估项 | 状态 | 说明 |
|--------|------|------|
| HUD 面板背景透明度 | OK | 70-80% alpha，形成良好深度感 |
| 遮罩层透明度 | OK | ResultPanel 0.706 alpha，SaveSlotPanel 无独立遮罩 |
| 前景文字可读性 | 一般 | WaveText 16px 在深色背景上偏小 |

### 5.2 焦点引导

| 评估项 | 状态 | 说明 |
|--------|------|------|
| ExperienceHub HUD_Root | 一般 | WaveText/EventFeedTitle 是最显眼的元素，但字号偏小 |
| 按钮层次 | 一般 | 退出/创建按钮 40px 文字过大，抢夺核心 HUD 注意力 |
| ComboBurstText | OK | 42px 大字 + 金色 + 居中有良好的爆发感 |

### 5.3 留白与间距

| 评估项 | 状态 | 说明 |
|--------|------|------|
| HUD 面板内 padding | 一般 | ~22px，基本合理 |
| 元素间 spacing | 较差 | Chat/List 项间距为 0 |
| 面板间间距 | OK | RightWavePanel 和 EventFeedPanel 各距边缘 24px |

### 5.4 阴影/描边

| 预制体 | 组件 | 说明 |
|--------|------|------|
| AddWearTaskItem/Name | Shadow | 有阴影，提升可读性 |
| BackpackItem/ItemInfo | Shadow | 有阴影 |
| WorkerBedItem/Name + root | Shadow | 有阴影（root + Name 各有） |
| Scene 中的 Text | 无 | 所有 Scene Text 无阴影/描边 |

### 5.5 视觉层次问题汇总

- [ ] **Scene HUD Text 添加 Outline/Shadow**: 在深色背景上的白色文字缺乏轮廓，在某些亮度下可读性差
- [ ] **ChatItem 添加 padding**: 文字紧贴聊天框边缘
- [ ] **TaskItem Toggle 增加间距**: 9 个 Toggle 紧贴排列难以区分

---

## 6. 优化建议列表

优先级按 (视觉收益 × 低风险) 排序：

| # | 优先级 | 路径 | 节点 | 问题 | 优化方向 | 风险 |
|---|--------|------|------|------|----------|------|
| 1 | P0 | Game.unity | ExperienceHub/WaveText | ~~字号 20→22~~ | [DONE] 字号+行间距优化 | - |
| 2 | P0 | Game.unity | ExperienceHub/ScoreText | ~~字号 18→16~~ | [DONE] 字号+行间距优化 | - |
| 3 | P0 | Game.unity | ExperienceHub/EventFeedTitle | ~~字号 22→24~~ | [DONE] 标题强化 | - |
| 4 | P0 | Game.unity | ExperienceHub/EventFeedText | ~~字号 17→16~~ | [DONE] 正文字号统一 | - |
| 5 | P0 | Game.unity | ExperienceHub/RightWavePanel | ~~rgba 统一~~ | [DONE] 背景色与 EventFeedPanel 统一 | - |
| 6 | P1 | Game.unity | 退出/创建/加入按钮 Title | 字号 40→28, 颜色对齐 TextAccent | 按钮层次合理化 | 低 |
| 7 | P1 | Game.unity | SaveSlotPanel/Title | 字号 40→32 | 避免过大标题 | 低 |
| 8 | P1 | Prefabs/ItemBox/BackpackItem | ItemInfo Text | 颜色 (0,1,0)→#7ECB9A | 刺眼绿色修正 | 低 |
| 9 | P1 | Prefabs/ItemBox/BuildItem | ItemInfo Text | 颜色 (0,1,0)→#7ECB9A | 刺眼绿色修正 | 低 |
| 10 | P1 | Prefabs/ItemBox/ButtonItem | Text | 字号 40→32 | 按钮文字过大 | 低 |
| 11 | P1 | Prefabs/ItemBox/RoomItem | RoomName Text | 字号 40→32 | 按钮文字过大 | 低 |
| 12 | P1 | Prefabs/ItemBox/ChatItem | VerticalLayoutGroup | spacing 0→4, padding 0→(8,8,8,8) | 聊天间距 | 低 |
| 13 | P1 | Prefabs/ItemBox/TaskItem | GridLayoutGroup | spacing 0→4, padding 0→(4,4,4,4) | Toggle 间距 | 低 |
| 14 | P2 | Prefabs/ItemBox/ButtonItem | RectTransform | height 30→36 | 按钮最小高度 | 低 |
| 15 | P2 | Prefabs/ItemBox/TaskItem | Toggle sizeDelta | height 20→24 | 可点击区域 | 中 |
| 16 | P2 | Prefabs/Tip.prefab | Root sizeDelta | width 100→160 | 避免文字截断 | 低 |
| 17 | P2 | Prefabs/ItemBox/AddWearTaskItem | Name Text | 字号 50→36 | 过大字号 | 低 |
| 18 | P3 | Game.unity | HUD Text 系列 | 添加 Outline | 深色背景文字可读性 | 中 |
| 19 | P3 | Prefabs/所有 | Text Color | 深灰 #323232→TextPrimary | 颜色统一到主题 | 中 |

---

## 7. 跳过项

以下问题因风险较高或涉及代码/结构变更，本轮跳过：

- 将 Legacy Text 迁移到 TextMeshPro — 需要修改所有代码中的 `Text` 引用
- 统一 Button 颜色到 PixelUITheme 粉色系 — 涉及所有 Prefab 的 Button 组件重配
- Tip.prefab 锚点改为响应式 — 需要验证所有使用场景
- TaskItem Toggle 布局重构 — 固定 9 个 Toggle 的 GridLayout 在工作量上不值得

---

## 8. 审计结论

项目中 UI 存在明显的视觉风格不统一问题。最突出的问题是：
1. **字号层级混乱**：Scene 内跳变过大，Prefab 普遍偏大
2. **颜色体系分裂**：PixelUITheme 定义了一套粉/棕/暖色主题，但实际 UI 大量使用蓝/绿/灰
3. **间距缺失**：LayoutGroup 普遍 padding=spacing=0

建议优先对 ExperienceHub（P0 核心 HUD）执行一轮完整优化，建立视觉基线，再逐步推广到其他模块。
