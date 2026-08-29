---
name: video-frames
description: 生成动画序列帧：用火山方舟 Doubao-Seedance-1.0-pro-fast 图生视频（480p·智能比例·2秒·带首帧·无水印·最便宜档），再从视频均匀抽帧得到帧序列。当用户要求生成角色动作动画、序列帧、动画帧时使用（首帧静态图先用 image-gen 生成）。关键词：序列帧、动作动画、抽帧、Seedance 视频。
user_invocable: true
---

# Seedance 视频 → 动画序列帧

用 Seedance 1.0 pro-fast 图生视频，再从视频抽帧获得动作连贯的帧序列。与 `image-gen`（静态图）互补：**静态首帧先走 `image-gen`（`--ref` 保角色一致性），本 skill 让角色动起来**。视频生成天然保证帧间连贯，比逐帧出图再拼条更不容易动作漂移。

## 固定参数（最便宜档，脚本已内置）

| 参数 | 值 | 说明 |
|---|---|---|
| 模型 | `doubao-seedance-1-0-pro-fast-250528` | pro 提速版，价格约 pro 的 1/3 |
| 分辨率 | `480p` | 最低档 |
| 时长 | `2` 秒 | 24fps，共 48 帧，默认从中均匀抽 8 帧 |
| 比例 | `adaptive` | 智能比例，i2v 自动跟随首帧图比例 |
| 镜头 | `camerafixed true` | **序列帧必须锁镜头**，否则镜头运动毁掉帧间一致性（脚本已内置） |
| 水印 | `false` | |
| 成本 | ~¥0.25/条（480p·2s） | 1080p 或更长时长按 `COST_PER_SECOND` 上浮 |

## Prerequisites

- **ARK_API_KEY**（火山方舟，需实名认证，且在 Ark 控制台**开通模型服务** `doubao-seedance-1-0-pro-fast`，否则报 `ModelNotOpen`）：https://console.volcengine.com/ark 。key 走环境变量或项目根 `.env`，不落库。
- **ffmpeg**（PATH 中可用）：`winget install Gyan.FFmpeg`。
- Python 3.8+（标准库，无第三方依赖）。

## Workflow

### Step 1 — 准备首帧

首帧决定角色身份与画风（视频全程跟随首帧）。用 `image-gen` 技能生成角色标准姿势静态图（纯白/纯色底，无阴影），确认达标后再进本步。

### Step 2 — 写提示词并生成

```bash
python .claude/skills/video-frames/scripts/generate_frames.py \
  --prompt "chibi character swings sword once then returns to idle, seamlessly looping, 2d game sprite style" \
  --first-frame Resources/Images/character/player/idle.png \
  --output-dir Resources/Images/character/player/attack/ \
  [--frames 8] [--duration 2] [--seed 42]
```

| Flag | Default | 说明 |
|---|---|---|
| `--prompt` | 必填 | **动作描述**（英文建议）。循环动画写 `seamlessly looping`；一次性动作（攻击/跳跃）不写 loop |
| `--first-frame` | 无 | 首帧图 → i2v（推荐，保角色一致性）；缺省则纯文生视频 |
| `--output-dir` | 必填 | `Resources/Images/<Category>/<Name>/`（相对仓库根 `Assets/`，**不带** `Assets/` 前缀） |
| `--prefix` | 输出目录名 | 帧命名 `{prefix}_0.png ...`（对齐序列帧动画生成器自然排序） |
| `--frames` | 8 | 均匀抽帧数量 |
| `--duration` | 2 | 2-12 秒；仅当 2 秒装不下动作时加长（成本线性上涨） |

**提示词模板**：`[主体]. [动作分解，明确每个手臂/腿在做什么]. [循环或一次性]. [2d game sprite style].` —— 与 image-gen 相同的完整性要求，缺项必然漂移。

输出：`{prefix}_0..N.png` 帧序列 + `{prefix}_contact.png` 拼图（一屏目检全部帧）+ `{prefix}_video.mp4`（保留供换帧率重抽）+ summary JSON（含 `estimated_cost`）。

### Step 3 — 目检与取舍

用 Read 看 `{prefix}_contact.png`：确认动作正确、角色无漂移、背景干净。不满意→调整动作描述重跑（成本仅一条视频）；动作对但帧不够→对保留的 mp4 用更大 `--frames` 重抽（**免费**，不重新生成）。

### Step 4 — 后处理

- **透明底**：首帧为纯色底时抽出的帧同为纯色底，接 `bg-remove` 技能批量抠图。
- **Unity 集成**：帧图导入后切成 Multiple Sprite，跑 工具/动画/序列帧动画生成器 生成 `.anim`；帧命名 `{prefix}_N` 已对齐其自然排序。

## Troubleshooting

- **`ModelNotOpen` / 404** — Ark 控制台未开通 `doubao-seedance-1-0-pro-fast` 模型服务。
- **401** — `ARK_API_KEY` 格式错误（检查完整性与空白）。
- **ffmpeg 不可用** — `winget install Gyan.FFmpeg` 后重启终端。
- **抽出的帧动作重叠/残影** — 视频本身有运动模糊，属模型特性；加大 `--frames` 密抽后挑选，或 prompt 里减少动作幅度。
- **角色漂移（画风/比例变化）** — 换更标准的首帧图，prompt 里强化画风关键词；必要时 `--seed` 复现排查。
