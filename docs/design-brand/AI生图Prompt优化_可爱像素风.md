# AI 生图 Prompt 优化 —— 可爱像素风

> 项目风格：RandomWorld 采用 **2D 可爱像素风**，配色以温暖柔和色调为主（粉、奶油、金、薄荷绿、天蓝）。
> 字体：方舟像素体（`ark-pixel-12px-monospaced-zh_cn`）
> 参考主题色：Button #F2A0AF, Dialog #FFF5EC, Gold #F9D56E, Sky #7CB8E4, Mint #7ECB9A, Text #4A3728

---

## 一、技能图标（4 格网格排列）

**原始意图**：生成 RPG 技能图标 4 个——火球、冰盾、治疗、剑斩。

**优化后 Prompt（英文）**：
```
Generate a set of 4 square skill icons for a 2D pixel-art survival game.
Style: Cute pixel art, warm pastel palette (soft pinks, creams, golds, mint greens), clean pixel edges, SNES/GameBoy aesthetics.
Items:
  1. Fireball — small cute flame with rounded shape, warm gold/orange tones (#F9D56E based)
  2. Ice Shield — soft blue crystal shield with rounded edges
  3. Heal — green cross with tiny leaves, soft mint green (#7ECB9A based)
  4. Sword Slash — white motion arc with soft pink trail (#F2A0AF based)
Layout: 2x2 grid, each icon 64x64 pixels.
Background: Solid warm dark brown (#4A3728) for easy keying. No anti-aliasing.
Technical: High contrast, readable at small mobile screen sizes. 16-bit style pixel art with thick outlines for visibility.
```

**优化后 Prompt（中文）**：
```
生成一组4个方形技能图标，用于2D像素风生存游戏。
风格：可爱像素风，温暖柔和配色（粉、奶油、金、薄荷绿），干净像素边缘，类似SNES/GameBoy风格。
图标：
  1. 火球术——可爱小火焰，圆润造型，暖金/橙色系
  2. 冰晶盾——柔蓝水晶盾牌，圆角设计
  3. 治愈术——绿色十字带小巧叶片，薄荷绿色系
  4. 剑斩——白色弧光斩击轨迹，带柔和粉色残影
布局：2x2网格排列，每个图标64x64像素。
背景：纯深棕色底（#4A3728），便于抠图。无抗锯齿。
技术要求：高对比度，手机小尺寸下清晰可辨。16位像素风格，粗轮廓线保证可见性。
```

---

## 二、关卡完成弹窗

**原始意图**：通关弹窗框架——金色边框、三颗星星、半透明文本框、绿色"下一关"和红色"退出"按钮。

**优化后 Prompt（英文）**：
```
Generate a "Level Complete" popup window frame for a 2D cute pixel-art colony survival game.
Style: Warm cute pixel art, pastel color palette. Matching PixelUITheme colors.
Design:
  - Outer frame: Soft gold rounded border with gentle decorative corners (not ornate — clean & cozy), 4px pixel line width
  - Top: Three small star-shaped empty slots (stars to be filled on level performance), soft gold color (#F9D56E)
  - Center: Semi-transparent warm cream parchment area (#FFF5EC at ~80% opacity) for level stats text overlay
  - Bottom buttons: Green "Next" button (#7ECB9A) and Coral "Exit" button (#F27A6B), rounded pixel corners, 2px dark brown text labels
Constraint: Flat 2D view, no perspective distortion. Background: solid warm brown (#4A3728) for easy cropping into Unity UI 9-slice.
Technical: Clean pixel edges, no anti-aliasing. Readable at mobile screen sizes.
```

**优化后 Prompt（中文）**：
```
生成一个"关卡完成"弹窗界面框，用于2D可爱像素风殖民地生存游戏。
风格：温暖可爱像素风，柔和配色。匹配 PixelUITheme 主题色。
设计：
  - 外框：柔和金色圆角边框，点缀简洁可爱装饰角（不要华丽繁琐），4px像素线宽
  - 顶部：三颗星星空槽位（根据表现填充），柔和金色（#F9D56E）
  - 中央：半透明暖奶油色羊皮纸区域（#FFF5EC约80%透明），用于关卡统计文字叠加
  - 底部按钮：绿色"下一关"按钮（#7ECB9A）和珊瑚色"退出"按钮（#F27A6B），圆角像素化按钮，深棕色文字标签
约束：平面2D视图，无透视变形。背景：纯暖棕色（#4A3728），方便Unity UI九宫格切割。
技术要求：干净像素边缘，无抗锯齿。手机屏幕大小下清晰可读。
```

---

## 三、行走动画精灵序列帧

**原始意图**：16位像素骑士银色盔甲，6帧行走循环，侧视图。

**优化后 Prompt（英文）**：
```
Generate a 2D pixel-art character sprite sheet for a survival colony-building game.
Character: A cute chibi-style worker/knight in simple armor (not heavy metal — more like a cozy villager with light gear).
Action: 6-frame walking animation cycle, horizontal row layout, side view (profile).
Style: Cute pixel art. Warm pastel colors — soft cream/beige tunic, light brown leather accents, small round helmet or headband.
Details:
  - Frame 1-6: Slight leg and arm movement variation to simulate casual walking pace
  - Character size: approx 32x48 pixels per frame
  - Simple, clean design — not overly detailed (to match colony village aesthetic)
Technical: Uniform 32x48 grid per frame, clean pixel edges, transparent background, 16bit/SNES aesthetic. No anti-aliasing. Thick outlines for character visibility against game backgrounds.
```

**优化后 Prompt（中文）**：
```
生成一个2D像素风角色精灵序列帧，用于生存殖民地建设游戏。
角色：可爱Q版风格的工人/村民，穿着简单轻甲（非重甲——更像温馨村民配轻便装备）。
动作：6帧行走循环动画，横向排列，侧视图（侧面）。
风格：可爱像素风。温暖柔和配色——柔米色外衣、浅棕色皮革点缀、小巧圆头盔或头巾。
细节：
  - 第1-6帧：手臂腿部轻微摆动变化，模拟轻松行走步伐
  - 角色大小：每帧约32x48像素
  - 简洁干净设计，不过分细致（匹配殖民地村庄风格）
技术要求：每帧32x48像素统一网格，干净像素边缘，透明背景，16位/SNES风格。无抗锯齿。粗轮廓线保证角色在游戏背景下可见。
```

---

## 四、掉落物品图标集

**原始意图**：金币、红药水、锈钥匙、宝箱——8位复古风格。

**优化后 Prompt（英文）**：
```
Generate a collection of game item sprites for a 2D cute pixel-art survival game.
Items (separated on sheet, not overlapping):
  1. Gold Coin — round coin with simple "$" or star mark, warm gold color (#F9D56E)
  2. Health Potion — small round bottle with cork, vibrant coral-red liquid (#F27A6B)
  3. Iron Key — simple key with rounded bow, warm grey-brown tone
  4. Treasure Chest — small wooden chest with rounded dome lid, brass/gold clasp
Style: Cute pixel art. Soft rounded shapes, thick outlines for game visibility, warm pastel color palette. 16x16 or 24x24 pixels per item.
Background: Transparent, items centered in their own cells. Ready for Unity Sprite Editor slicing.
Technical: Clean pixel edges, no anti-aliasing, 16-bit aesthetic. Clear icon readability at small sizes.
```

**优化后 Prompt（中文）**：
```
生成一组游戏物品精灵图，用于2D可爱像素风生存游戏。
物品（分散排列，不重叠）：
  1. 金币——圆形金币，简单"$"或星形标记，暖金色（#F9D56E）
  2. 生命药水——小圆瓶配软木塞，鲜艳珊瑚红色液体（#F27A6B）
  3. 铁钥匙——简约钥匙带圆润手柄，暖灰褐色调
  4. 小宝箱——小木箱配圆顶盖，黄铜/金色扣环
风格：可爱像素风。柔软圆润造型，粗轮廓线保证游戏中可见，温暖柔和配色。每个物品16x16或24x24像素。
背景：透明，物品居中于各自格子内。可直接用于Unity Sprite Editor切割。
技术要求：干净像素边缘，无抗锯齿，16位风格。小尺寸下图标清晰可辨。
```

---

## 五、地牢地面无缝平铺纹理

**原始意图**：3D写实风格带裂缝苔藓的石砖地面——改为2D像素可爱风。

**优化后 Prompt（英文）**：
```
Generate a seamless tiling dungeon floor texture for a 2D pixel-art game.
Subject: Stone dungeon floor bricks with cracks.
Style: Cute pixel art — warm grey-brown stone color palette (not dark/gritty dungeon). Tiny green moss patches in cracks for cozy detail, soft stone shading.
View: Direct top-down (orthographic), flat even lighting for seamless tiling.
Technical: 32x32 or 48x48 pixel tile, seamless on all 4 edges. Clean pixel edges, no anti-aliasing. 16-bit aesthetic.
Constraint: Warm and inviting tones — more like a cozy underground cellar than a grim dungeon. #8B7D6B base stone with #7ECB9A accent moss.
```

**优化后 Prompt（中文）**：
```
生成一个无缝平铺地牢地面纹理，用于2D像素风游戏。
主题：带裂缝的石砖地牢地板。
风格：可爱像素风——暖灰棕色调石砖（非黑暗阴森地牢）。裂缝中点缀小巧绿色苔藓增加可爱细节，柔和石头明暗。
视角：正俯视（正交），均匀平光，确保无缝平铺。
技术要求：32x32或48x48像素图块，四边无缝衔接。干净像素边缘，无抗锯齿。16位风格。
约束：温暖舒适色调——更像温馨地下酒窖而非阴暗地牢。石砖底色#8B7D6B，苔藓#7ECB9A点缀。
```

---

## 六、木板纹理

**原始意图**：WoW风格手绘木板——改为可爱像素风。

**优化后 Prompt（英文）**：
```
Generate a seamless wood plank texture for a 2D pixel-art game.
Subject: Wooden plank surface for building/flooring.
Style: Cute pixel art — warm honey-wood brown tones, simple clean wood grain lines (not overly detailed), soft shading.
View: Top-down flat view, rectangle aspect ratio. Seamless tiling on all edges.
Colors: Warm wood tones — #C4A882 base, #E0C9A6 highlights, #A08060 grain lines.
Technical: 32x32 pixel tile, seamless edges. Clean pixel edges, no anti-aliasing, 16-bit aesthetic.
Constraint: Matches cozy colony village building style. Simple, warm, inviting wood texture.
```

**优化后 Prompt（中文）**：
```
生成一个无缝木板纹理，用于2D像素风游戏。
主题：木板表面，用于建筑/地板。
风格：可爱像素风——温暖蜜色木纹色调，简洁干净木纹线条（不过分细致），柔和明暗。
视角：正俯视平面，矩形比例。四边无缝平铺。
配色：暖木色系——#C4A882基底，#E0C9A6亮部，#A08060纹路线条。
技术要求：32x32像素图块，无缝边缘。干净像素边缘，无抗锯齿，16位风格。
约束：匹配温馨殖民地村庄建筑风格。简洁、温暖、舒适的木材质感。
```

---

## 七、等角瞭望塔（改为2D像素侧视角）

**原始意图**：3D等角弓箭塔——改为2D可爱像素建筑。

**优化后 Prompt（英文）**：
```
Generate a 2D pixel-art defense tower sprite for a colony survival game.
Subject: Level 1 Archer Tower — simple wood and stone watchtower with a cute small blue flag waving on top.
Material: Light wood planks and grey cobblestone base (warm tones — not dark/grim). Rounded, chunky proportions for cute style.
View: Front/side flat 2D view (not isometric). No perspective.
Style: Cute pixel art — soft rounded shapes, warm wood tones, pastel blue flag (#7CB8E4).
Constraint: Isolated on transparent background. Building size approx 64x96 pixels.
Technical: Clean pixel edges, no anti-aliasing, 16-bit aesthetic. Clear silhouette for game readability.
```

**优化后 Prompt（中文）**：
```
生成一个2D像素风防御塔精灵，用于殖民地生存游戏。
主题：1级弓箭塔——简单木头与石材瞭望塔，顶部飘扬可爱小蓝旗。
材质：浅色木板配灰色鹅卵石底座（暖色调，非暗黑风格）。圆润厚重比例，呈现可爱风格。
视角：正面/侧面平面2D视角（非等角）。无透视。
风格：可爱像素风——柔软圆润造型，温暖木色调，柔蓝旗帜（#7CB8E4）。
约束：透明背景。建筑尺寸约64x96像素。
技术要求：干净像素边缘，无抗锯齿，16位风格。轮廓清晰，游戏内辨识度高。
```

---

## 八、六角地形格子

**原始意图**：文明类游戏六角地形格——森林、山地、水域。

**优化后 Prompt（英文）**：
```
Generate hexagonal game map tiles for a 2D pixel-art survival colony game.
Content: Three separate hexagonal tiles (top-down flat view, no isometric):
  Tile 1: Green Forest — cute pixel trees on grass, soft green (#7ECB9A based)
  Tile 2: Grey Mountain — rounded cute mountain with snow cap on grey stone, warm tones
  Tile 3: Blue Water — gentle wavy water tile, soft blue (#7CB8E4 based)
Style: Cute pixel art. Warm, soft colors. Thick outlines for game board readability. Uniform lighting.
Layout: Three tiles separated on sheet with transparent background, ready for grid placement.
Technical: Uniform hexagon size ~48px across. Clean pixel edges, no anti-aliasing. Matching art style across all tiles.
```

**优化后 Prompt（中文）**：
```
生成六角形游戏地图格子，用于2D像素风生存殖民地游戏。
内容：三个独立六角形格子（正俯视平面视角，非等角）：
  格子1：绿色森林——可爱像素树丛在草地，柔和绿色系（#7ECB9A）
  格子2：灰色山丘——圆润可爱山丘带雪顶，暖灰色调
  格子3：蓝色水域——轻柔波浪水面，柔蓝色系（#7CB8E4）
风格：可爱像素风。温暖柔和配色。粗轮廓线保证游戏棋盘清晰可见。均匀光照。
布局：三个格子分开排列，透明背景，可直接用于游戏网格放置。
技术要求：统一六角形尺寸约48像素宽。干净像素边缘，无抗锯齿。所有格子风格匹配统一。
```

---

## 九、卡牌边框（TCG UI 框架）

**原始意图**：暗黑幻想铁锈边框——改为可爱像素卡牌框。

**优化后 Prompt（英文）**：
```
Generate a card frame UI overlay for a 2D cute pixel-art game TCG system.
Usage: PNG overlay for Unity card display.
Style: Cute pixel art — warm cream/parchment tones, soft pink and gold accents. NOT dark or grim.
Structure:
  - Outer frame: Soft rounded wood/leather border with tiny cute decorative corners, warm brown (#4A3728 based)
  - Center: Transparent (for card art), or solid warm cream (#FFF5EC) for masking
  - Bottom text box: Small scroll/parchment area for card description text
  - Top-left: Empty circular gem socket in soft gold (#F9D56E) for element icon
Constraint: Flat 2D view, no character illustration. Only generate the UI frame. No anti-aliasing.
Technical: Clean pixel edges, 16-bit aesthetic. Designed for 9-slice Unity UI scaling.
```

**优化后 Prompt（中文）**：
```
生成一个卡牌边框UI覆盖层，用于2D可爱像素风游戏TCG系统。
用途：Unity卡牌显示的PNG叠加素材。
风格：可爱像素风——温暖奶油/羊皮纸色调，柔和粉色和金色点缀。非暗黑风格。
结构：
  - 外框：柔和圆润木纹/皮革边框，点缀小巧可爱装饰角，暖棕色系（#4A3728）
  - 中央：透明区域（用于卡牌插图），或纯暖奶油色（#FFF5EC）用于遮罩
  - 底部文本框：小卷轴/羊皮纸区域，用于卡牌描述文字
  - 左上角：空圆形宝石插槽，柔金色（#F9D56E），用于属性图标
约束：平面2D视图，不要生成角色插图。只生成UI框架。无抗锯齿。
技术要求：干净像素边缘，16位风格。支持Unity UI九宫格缩放。
```

---

## 十、火焰元素精灵卡牌插图

**原始意图**：万智牌风格油画火元素——改为可爱像素风。

**优化后 Prompt（英文）**：
```
Generate a cute pixel-art character illustration for a game card.
Subject: A cute Fire Elemental Spirit — a small round flame creature with big sparkling eyes and a cheerful expression. Made of soft warm flames and tiny glowing embers.
Composition: Centered, full body, floating/action pose. Full bleed.
Style: Cute pixel art — SNES/16-bit style, warm pastel flame colors (soft gold #F9D56E, warm coral #F27A6B, soft pink #F2A0AF), rounded shapes, adorable proportions (chibi/cute large head-to-body ratio).
Constraint: No text, no borders, no UI elements. Pure character artwork for card portrait crop. Transparent or dark warm brown background.
Technical: Approx 128x128 pixels. Clean pixel edges, no anti-aliasing. Thick outlines for visibility.
```

**优化后 Prompt（中文）**：
```
生成一个可爱像素风角色插图，用于游戏卡牌。
主题：可爱火焰元素精灵——小巧圆润火焰生物，大大的闪闪眼睛，愉悦表情。由柔和温暖火焰和小小发光余烬构成。
构图：居中，全身，漂浮/动态姿态。满版出血。
风格：可爱像素风——SNES/16位风格，温暖柔和火焰色彩（柔和金色#F9D56E、暖珊瑚#F27A6B、柔和粉色#F2A0AF），圆润造型，Q版可爱大头比例。
约束：无文字、无边框、无UI元素。纯角色美术，用于卡牌头像裁剪。透明或暖深棕色背景。
技术要求：约128x128像素。干净像素边缘，无抗锯齿。粗轮廓线保证可见性。
```

---

## 通用风格参考参数

在附加 prompt 时建议统一加上以下尾缀（中英文二选一）：

**English suffix**:
```
Overall style: Cute pixel art, 16-bit/SNES aesthetic. Warm pastel color palette matching PixelUITheme (Base colors: #F2A0AF pink, #F9D56E gold, #7CB8E4 sky blue, #7ECB9A mint green, #C5B4E3 lavender, #4A3728 warm brown). Clean pixel edges, no anti-aliasing. Thick outlines for small-screen readability. Cozy, inviting, Stardew Valley-like visual feel.
```

**中文尾缀**：
```
统一风格：可爱像素风，16位/SNES风格。温暖柔和配色，匹配 PixelUITheme 主题色（基础色：#F2A0AF粉、#F9D56E金、#7CB8E4天蓝、#7ECB9A薄荷绿、#C5B4E3淡紫、#4A3728暖棕）。干净像素边缘，无抗锯齿。粗轮廓线保证小屏可读。温馨舒适，类似星露谷物语的视觉感受。
```

---

## 适配说明

| 原始风格 | 本项目适配 |
|---------|-----------|
| Supercell 矢量手绘 | 可爱像素风 16-bit |
| 3D 写实 (UE5) | 2D 平铺像素纹理 |
| WoW 手绘风格 | 可爱像素风暖木纹理 |
| 3D 等角 (CoC) | 2D 侧面像素建筑 |
| 暗黑幻想 UI | 可爱像素风温暖UI |
| 万智牌油画 | 可爱像素风Q版角色 |
| 8-bit 复古 | 16-bit 可爱像素风 |
