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
| 模型 | `doubao-seedance-1-0-pro-fast-251015` | pro 提速版，价格约 pro 的 1/3；旧 ID `250528` 已下线 |
| 分辨率 | `480p` | 最低档 |
| 时长 | `2` 秒 | 24fps，共 48 帧，默认从中均匀抽 12 帧（6 帧/秒） |
| 比例 | `adaptive` | 只会输出标准比例（1:1/4:3/3:4/16:9/9:16），选**最接近**首帧的一种，然后对首帧**放大裁切填满**——并非跟随原比例，见 Step 1 比例规则 |
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

**比例规则（必须，否则主体被裁切）**：模型只输出标准比例（1:1、4:3、3:4、16:9、9:16），`ratio adaptive` 会选与首帧最接近的一种，再对首帧**放大裁切填满**（crop-to-fill）——首帧非标准比例时主体必被裁。实例：300×340 的树 → 640×640 视频，按宽放大后树高 725 超出画布，顶底各被裁 ~42px。

生成前把首帧 **pad 到能完整容纳主体的标准比例画布**：纯色底（与后续抠图底色一致）居中，四周留少量余量；留白会在后处理抠图时自动消失。拿不准就 pad 成 1:1（边长 = max(宽, 高) + 余量）。

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
| `--frames` | 12 | 均匀抽帧数量 |
| `--duration` | 2 | 2-12 秒；仅当 2 秒装不下动作时加长（成本线性上涨） |

**提示词模板**：`[主体]. [动作分解，明确每个手臂/腿在做什么]. [循环或一次性]. [2d game sprite style].` —— 与 image-gen 相同的完整性要求，缺项必然漂移。

输出：`{prefix}_0..N.png` 帧序列 + `{prefix}_contact.png` 拼图（一屏目检全部帧）+ `{prefix}_video.mp4`（保留供换帧率重抽）+ summary JSON（含 `estimated_cost`）。

### Step 3 — 目检与取舍

用 Read 看 `{prefix}_contact.png`：确认动作正确、角色无漂移、背景干净。不满意→调整动作描述重跑（成本仅一条视频）；动作对但帧不够→对保留的 mp4 用更大 `--frames` 重抽（**免费**，不重新生成）。

### Step 4 — 后处理

- **透明底**：首帧为纯色底时抽出的帧同为纯色底，接 `bg-remove` 技能批量抠图。
- **Unity 集成**：帧图导入后切成 Multiple Sprite，跑 工具/动画/序列帧动画生成器 生成 `.anim`；帧命名 `{prefix}_N` 已对齐其自然排序。

## Troubleshooting

- **`ModelNotOpen` / 404（模型不存在）** — Ark 控制台未开通模型服务，或模型 ID 已更新（脚本里的 ID 过期就用账号模型列表 `GET /api/v3/models` 查当前 ID）。
- **401** — `ARK_API_KEY` 格式错误（检查完整性与空白）。
- **ffmpeg 不可用** — `winget install Gyan.FFmpeg` 后重启终端（当前会话可临时 `export PATH` 加入其 bin 目录）。
- **主体被裁切（缺顶/缺底/缺边）** — 首帧比例非标准比例被 crop-to-fill 裁掉，见 Step 1 比例规则：pad 首帧到标准比例画布后重生成。
- **抽出的帧动作重叠/残影** — 视频本身有运动模糊，属模型特性；加大 `--frames` 密抽后挑选，或 prompt 里减少动作幅度。
- **角色漂移（画风/比例变化）** — 换更标准的首帧图，prompt 里强化画风关键词；必要时 `--seed` 复现排查。
- **非循环元素闪现（如花瓣时有时无）** — 视频内容本身非严格循环；介意就在 prompt 里去掉该元素重生成，或改用无缝摆动类描述。
