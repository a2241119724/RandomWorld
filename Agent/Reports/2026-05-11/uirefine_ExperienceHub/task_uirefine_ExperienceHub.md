# UI 视觉优化任务卡 — ExperienceHub HUD

**日期**: 2026-05-11
**审计来源**: `Agent/Reports/2026-05-11/uirefine_audit_001.md`
**优化模块**: ExperienceHub (HUD_Root) — P0 游戏核心 HUD
**风险等级**: 低（仅调整字号、行间距、颜色参数）

---

## 当前视觉问题

| # | 节点 | 参数 | 当前值 | 问题描述 |
|---|------|------|--------|----------|
| 1 | RightWavePanel (Image) | m_Color | (0.055,0.071,0.094,0.804) | 与 EventFeedPanel 背景色不一致 |
| 2 | EventFeedPanel (Image) | m_Color | (0.039,0.047,0.071,0.745) | 与 RightWavePanel 背景色不一致 |
| 3 | WaveText | m_FontSize | 20 | HUD 标题字号偏小，长时间阅读吃力 |
| 4 | WaveText | m_LineSpacing | 1 | 多行文本行间距过紧 |
| 5 | ScoreText | m_FontSize | 18 | 评分次要信息字号应比标题小 |
| 6 | ScoreText | m_LineSpacing | 1 | 多行文本行间距过紧 |
| 7 | EventFeedTitle | m_FontSize | 22 | 标题与正文差距不够大 |
| 8 | EventFeedTitle | m_LineSpacing | 1 | 行间距过紧 |
| 9 | EventFeedText | m_FontSize | 17 | 非标准字号的尴尬位置（16/18/20更常见） |
| 10 | EventFeedText | m_LineSpacing | 1 | 行间距过紧 |
| 11 | ComboBurstText | m_LineSpacing | 1 | 行间距过紧 |

---

## 优化方案

### 字号层级统一（HUD 内形成 3 级梯度）

| 层级 | 目标字号 | 节点 | 当前→目标 |
|------|----------|------|-----------|
| H1 爆发文字 | 42 | ComboBurstText | 42（不变） |
| H2 面板标题 | 24 | EventFeedTitle | 22→**24** |
| H2 波次标题 | 22 | WaveText | 20→**22** |
| H3 正文/评分 | 16 | ScoreText | 18→**16** |
| H3 正文/事件 | 16 | EventFeedText | 17→**16** |

### 颜色统一

| 节点 | 当前值 | 目标值 | 说明 |
|------|--------|--------|------|
| RightWavePanel (Image) | (0.055,0.071,0.094,0.804) | (0.039,0.047,0.071,0.745) | 统一到 EventFeedPanel 标准 |
| 其他 Text 颜色 | 维持现状 | — | WaveText 天蓝、ScoreText 白、EventFeedTitle 金、EventFeedText 蓝灰均合理 |

### 行间距

| 节点 | 当前 | 目标 |
|------|------|------|
| WaveText | 1 | **1.2** |
| ScoreText | 1 | **1.2** |
| EventFeedTitle | 1 | **1.1**（单行标题，适度即可） |
| EventFeedText | 1 | **1.2** |
| ComboBurstText | 1 | **1.1** |

---

## 影响路径

- `Scenes/Game.unity` — ExperienceHub 区域 (lines 34260-34915)
  - `900100000000000033` — RightWavePanel Image m_Color
  - `900100000000000043` — WaveText FontData (m_FontSize, m_LineSpacing)
  - `900100000000000053` — ScoreText FontData (m_FontSize, m_LineSpacing)
  - `900100000000000073` — EventFeedTitle FontData (m_FontSize, m_LineSpacing)
  - `900100000000000083` — EventFeedText FontData (m_FontSize, m_LineSpacing)
  - `900100000000000093` — ComboBurstText FontData (m_LineSpacing)

---

## 不应修改项

- 节点名称（WaveText, ScoreText, EventFeedTitle, EventFeedText, ComboBurstText, RightWavePanel, EventFeedPanel）
- 脚本挂载（ExperienceHub MonoBehaviour `b88750edcccc483681623bf0a73df5de`）
- RectTransform 参数（anchor, position, sizeDelta, pivot）
- m_Font 引用（guid: `994464cadda06394eb1598617cdd2c57`）
- 其他 FontData 参数（m_FontStyle, m_BestFit, m_MinSize, m_MaxSize, m_Alignment）
- 其他 Image 参数（m_Sprite, m_Type, m_RaycastTarget）

---

## 回滚方案

所有修改仅限于 Game.unity YAML 中的数值参数。通过 `git checkout Scenes/Game.unity` 可完全回滚。

或逐个回滚：
1. 恢复 RightWavePanel Image m_Color: `{r: 0.05490196, g: 0.07058824, b: 0.09411765, a: 0.8039216}`
2. 恢复 WaveText m_FontSize: 20, m_LineSpacing: 1
3. 恢复 ScoreText m_FontSize: 18, m_LineSpacing: 1
4. 恢复 EventFeedTitle m_FontSize: 22, m_LineSpacing: 1
5. 恢复 EventFeedText m_FontSize: 17, m_LineSpacing: 1
6. 恢复 ComboBurstText m_LineSpacing: 1

---

## 使用的常量/枚举参考

- PixelUITheme (无直接引用，设计参考)
- 字号层级: H1=42, H2=22-24, H3=16

---

## 执行结果

**状态**: ✅ 完成
**验证记录**: `validation_uirefine_ExperienceHub.md`

### 优化前后参数对比

| 节点 | 参数 | 优化前 | 优化后 |
|------|------|--------|--------|
| RightWavePanel | m_Color | (0.055,0.071,0.094,0.804) | (0.039,0.047,0.071,0.745) |
| WaveText | m_FontSize | 20 | **22** |
| WaveText | m_LineSpacing | 1 | **1.2** |
| ScoreText | m_FontSize | 18 | **16** |
| ScoreText | m_LineSpacing | 1 | **1.2** |
| EventFeedTitle | m_FontSize | 22 | **24** |
| EventFeedTitle | m_LineSpacing | 1 | **1.1** |
| EventFeedText | m_FontSize | 17 | **16** |
| EventFeedText | m_LineSpacing | 1 | **1.2** |
| ComboBurstText | m_LineSpacing | 1 | **1.1** |

### 字号层级变化

优化前: 42 / 22 / 20 / 18 / 17 （梯度模糊）
优化后: 42 / 24 / 22 / 16 / 16 （3级清晰梯度：爆发 > 标题 > 正文）

### 修改文件

- `Scenes/Game.unity` — 10 处参数修改

### 验证结果

- YAML 格式正确，文件结构完整
- 节点名称、脚本绑定、Rect 参数均未修改
- 代码绑定不受影响

### 剩余视觉问题

参见审计报告 #6-#19（本轮未处理，P1/P2/P3 项）

### 人工调优建议

- ExperienceHub 目前使用 Legacy Text，如果后续迁移到 TMP 可以进一步优化渲染质量
- 两个面板 (RightWavePanel / EventFeedPanel) 宽度均为 520px，可考虑统一高度（220 vs 210）

