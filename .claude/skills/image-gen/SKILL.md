---
name: image-gen
description: 使用国产 AI 画图模型（硅基流动 + 火山方舟 Seedream）生成角色图和图片变体，支持参考图保持风格和角色一致性。当用户要求生成新角色姿势、吉祥物变体、美术资源、插画或任何 AI 生成图片时使用。
user_invocable: true
---

# AI 图像生成（国产模型：硅基流动 + Seedream）

用国产画图模型生成游戏素材，按「单价最低·批量冲量 + Seedream 角色一致性」双轨选择模型。参考图用于风格与角色一致性（仅 Seedream / Qwen-Image-Edit 支持图生图）。

## 双轨选型

| 场景 | Provider | 模型 | 单价 |
|---|---|---|---|
| 道具/图标/UI 批量（无参考图） | 硅基流动 | `Tongyi-MAI/Z-Image-Turbo` | ~$0.005/张 |
| 场景/特效（无参考图，质量优先） | 硅基流动 | `Qwen/Qwen-Image` | ~$0.042/张 |
| 道具免费试跑 | 硅基流动 | `Kwai-Kolors/Kolors` | 免费 |
| 角色立绘/精灵图/序列帧（有参考图） | 火山方舟 | `doubao-seedream-5-0-lite-260128` | ~$0.035/张 |

**关键**：纯文生图模型（Z-Image/Kolors/Qwen-Image）**不支持参考图**。需要角色一致性的素材（角色姿势/多视图/序列帧）必须走 Seedream（传 `--ref` 自动路由），或硅基流动的 `Qwen-Image-Edit`（仅单参考图）。

## Prerequisites

- **SILICONFLOW_API_KEY**（硅基流动，需实名认证）：https://cloud.siliconflow.cn
- **ARK_API_KEY**（火山方舟，需实名认证，且需在 Ark 控制台**开通模型服务** `doubao-seedream-5-0-lite`，否则报 `ModelNotOpen`）：https://console.volcengine.com/ark
- 至少配置一个；key 只走环境变量（或项目根 `.env`），不落库。
- **Deno** 运行时（用于生成脚本）：`winget install DenoLand.Deno`

## Workflow

### Step 1 — 明确需求

确认主体、姿态、表情、画风、用途（角色/道具/场景/UI/序列帧）。按用途选择 `--category`，影响自动选模型。

### Step 2 — 决定是否用参考图

- **角色立绘/精灵图/序列帧**：必须传 1-2 张参考图（`--ref`，脚本自动路由到 Seedream）。
  1. **主参考（第一个）**：角色最标准的图，锚定身份（脸型/配色/特征）。
  2. **风格/姿态参考（第二个）**：锚定比例与画风。Seedream 支持多参考图（数组）；硅基流动编辑模型仅支持单张。
- **道具/图标/UI/场景**：通常无参考图，直接批量，走最便宜的 Z-Image-Turbo。

### Step 3 — 写提示词

1. **主体描述** — 定义角色/物品的特征（防止漂移）
2. **姿态与表情** — 明确每个手臂/手在做什么
3. **画风指令** — 如 "flat color fills, 2D game sprite"、"pixel art"、"watercolor"
4. **背景** — 颜色、场景，或透明（透明底后接 bg-remove）
5. **取景** — 全身/半身/四分之三视角

**模板：**
```
[主体描述]. [姿态与表情]. [画风指令]. [背景]. [取景].
```

### Step 4 — 生成

```bash
deno run --allow-env --allow-read --allow-write --allow-net \
  .claude/skills/image-gen/scripts/generate.ts \
  --prompt "your prompt here" \
  [--ref path/to/primary-reference.png] \
  [--ref path/to/style-reference.png] \
  --category character|item|map|effect|ui \
  --output-dir Resources/Images/<Category>/<Name>/ \
  [--variants 4] \
  [--aspect "3:4"] \
  [--size "2K"] \
  [--provider auto|siliconflow|ark] \
  [--model "MODEL_ID"] \
  [--negative "unwanted things"]
```

**Parameters:**
| Flag | Default | Options |
|------|---------|---------|
| `--category` | 空 | character / item / map / effect / ui（辅助自动选模型） |
| `--provider` | auto | auto / siliconflow / ark |
| `--model` | 空 | 显式模型 ID，覆盖自动选择 |
| `--variants` | 1 | 1-8（每个是一次独立 API 调用；**默认 1，先选中最优再批量**） |
| `--aspect` | 1:1 | 1:1, 3:4, 4:3, 9:16, 16:9, 2:3, 3:2 |
| `--size` | 2K | 512, 1K, 2K, 4K（Seedream 无 1K 档，512/1K 均按 2K） |
| `--negative` | 空 | 负面提示词（Z-Image/Qwen-Image-Edit 生效） |

**自动选模型规则：** 有 `--ref` → Seedream（有 ARK key 时）；无参考图 + item/ui → Z-Image-Turbo；map/effect → Qwen-Image；`--provider/--model` 显式指定优先。

**按用途选宽高比：**
| 用途 | 宽高比 |
|------|--------|
| 角色全身立绘 | `3:4` |
| 道具/图标 | `1:1` |
| 序列帧（每帧动作） | `3:4` 或 `1:1` |
| 场景背景横幅 | `16:9` 或 `4:3` |

**输出目录直接落在 `Resources/Images/<Category>/<Name>/`**（相对仓库根 `Assets/`，即运行时的 CWD）——注意不要再加 `Assets/` 前缀，否则会生成嵌套 `Assets/Assets/`。遵守 Unity 资源约定：单图 `{英文名}.png`（与 `ItemData.Name` 绑定）；序列帧 `{prefix}_0/_1/_2...`（对齐序列帧动画生成器自然排序）。summary 末尾含 `provider`、`model`、`estimated_cost`，供选图决策。

### Step 5 — 选最优变体

用 Read 工具目检所有生成图，按以下打分：

**一致性（最重要）：**
- 与参考图是否吻合（脸型/比例/配色/画风）——仅 Seedream 参考图链路
- 画风是否稳定（未漂移成写实/3D 等）

**质量（次之）：**
- 是否有表现力，能否作为生产资源

**选最优 1 张**，用最终英文名重命名落盘到对应资源目录，并说明选择理由。不满意则说明问题并调整提示词重生成。

### Step 6 — 后处理

- **透明底**：角色/道具图需要透明背景时，接 `bg-remove` 技能（`birefnet-general`，magenta 合成验证）。
- **序列帧**：帧图在 Unity 里切成 Multiple Sprite 后，跑 工具/动画/序列帧动画生成器 生成 `.anim`。
- **清理**：删除弃用变体和临时目录；保留最优变体。

## Rate Limits

- **硅基流动**：`20012`（TPM 超限）、`50505`（模型过载）。
- **火山方舟**：`429`。
- 若部分变体失败：等 60 秒，只补缺失数量重跑，不要全量重试。
- 批量生成建议分片 + 失败换 fallback 模型（显式 `--model`）。

## Troubleshooting

- **"SILICONFLOW_API_KEY not set"** — 在 https://cloud.siliconflow.cn 实名认证后创建 key。
- **"ARK_API_KEY not set"** — 在 https://console.volcengine.com/ark 创建 key（开通 Seedream）。
- **参考图不生效** — Z-Image/Kolors/Qwen-Image 不支持图生图；带 `--ref` 自动走 Seedream，或显式 `--provider ark`。
- **多参考图被截断** — 硅基流动编辑模型仅支持单张；多参考图用 `--provider ark`（Seedream 支持数组）。
- **Ark 401 AuthenticationError** — key 格式错误，确认 `ARK_API_KEY` 完整且无空白。
- **角色长歪/画风漂移** — 加强物理特征描述、强化画风关键词、确认参考图已包含。
