# Worker 行为决策模型（Python / PyTorch）

用机器学习替代/增强 `WorkerBrain.Decide()` 的硬编码优先级级联，训练一个
「状态 → 行为」决策模型：输入 Worker 状态数值（心情、饥饿、疲劳、人格、经济、
阶段、目标、好感度 + 全局/局部信息），输出 14 种行为之一（`WorkerDecisionType`）。
训练后导出权重给 Unity C# 侧推理。

## 目录结构

```
model/
├── config/
│   ├── feature_schema.yaml   # 可扩展特征定义（新增输入只改这里）
│   └── model_config.yaml     # 数据/模型/训练/导出超参数
├── data/
│   ├── generate_data.py      # 数据生成入口
│   ├── raw/                  # 原始 (state, action) CSV
│   └── processed/            # 归一化特征矩阵 X.npy / 标签 y.npy
├── src/
│   ├── actions.py            # 14 种行为常量（与 C# WorkerDecisionType 对齐）
│   ├── features.py           # 特征提取/归一化（读 feature_schema）
│   ├── simulator.py          # 决策教师模拟器（WorkerBrain 规则 + 现实规则）
│   ├── dataset.py            # PyTorch Dataset
│   ├── models/
│   │   ├── baseline_utility.py  # 效用函数 baseline（逻辑回归）
│   │   └── mlp.py               # MLP 策略网络
│   ├── train.py              # 训练（baseline / mlp）
│   ├── evaluate.py           # 评估与对比
│   └── export.py             # 导出 ONNX + 纯权重 JSON
├── unity_bridge/
│   └── WorkerModelInference.cs.example  # C# 推理层模板
└── experiments/              # 训练产物（模型/权重/ONNX）
```

## 快速开始

```bash
cd model

# 0. 环境：torch 对 Python 3.14 支持可能滞后，建议用 3.11/3.12 的 venv
python -m venv .venv && source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt

# 1. 生成训练数据（模拟器采样）
python data/generate_data.py

# 2. 训练（两个模型）
python src/train.py --model baseline
python src/train.py --model mlp

# 3. 评估对比
python src/evaluate.py

# 4. 导出（ONNX + 纯权重 JSON）
python src/export.py
```

## 遗传算法定位（结论）

**遗传算法不适合作为「状态→行为」的实时决策器本体**——它是优化器而非决策器，
无法高效处理连续高维状态空间，逐帧决策慢且样本效率低。

GA 的正确位置是**离线调参**：把效用函数 baseline 的权重向量当基因，适应度 =
模拟器里的长期生存/收益，用 GA（或贝叶斯优化）搜索权重。神经进化（NEAT）可优化
网络结构，但收敛慢、难调，仅作备选。

推荐路径：**效用函数（可解释线性）→ MLP（行为克隆）→ 强化学习（PPO）**，GA 保留
为效用函数权重搜索的辅助工具。

## 如何扩展输入特征

1. 在 `src/simulator.py` 的 `random_state()` 里新增状态字段。
2. 在 `config/feature_schema.yaml` 加一个条目（`name/group/kind/key`）。
3. `src/features.py` 自动读入，特征向量维度随之增加，无需改其他代码。

`kind` 取值：`ratio`（当前/最大）、`minmax`（线性缩放）、`log`（log1p 压缩）、
`onehot`（类别展开）、`passthrough`（已归一化）。

## 人格维度（现有 + 建议扩展）

现有 4 维（`WorkerPersonality.cs`，[0,100]）：`Mood` 心情、`Ambition` 事业心、
`Diligence` 勤奋、`Sociality` 社交。

建议新增（现实社会数值，本框架已在 schema 预留特征位）：`Greed` 贪婪、`Laziness`
懒惰、`Curiosity` 好奇、`Bravery` 勇气、`Loyalty` 忠诚。本次 Python 侧预留
`greed`/`laziness` 两个特征位（默认 50），是否同步改 C# `WorkerPersonality` 属后续
Unity 接入阶段决定。

## 模拟器说明（教师规则）

`src/simulator.py` 融合两层规则生成标注：

1. **WorkerBrain 规则基底**：复刻 `WorkerBrain.Decide()` 的优先级级联与阈值
   （`HungryThreshold=30`、`TiredThreshold=35`、`SpiritThreshold=30` 等），对应
   `Scripts/2D/AI/Worker/WorkerBrain.cs:78-118`。
2. **现实人际关系规则**（WorkerBrain 覆盖不到的权衡）：
   - 天气炎热/严寒 + 事业心低 + 无生存压力 → 不愿出门（Idle/Wander）；
   - 但食物少/没钱 → 生存压力压过「不愿出门」，强制出门采集；
   - 心情过低 → 拒绝社交行为（不发/不接悬赏）；
   - 好感度门控接受悬赏（阈值 35，对应 `FavorabilityRuleService`）。

## Unity 对接（后续阶段）

- 简单 MLP：`export.py` 导出 `mlp_weights.json`，C# 手写前向传播（见
  `unity_bridge/WorkerModelInference.cs.example`），零外部依赖。
- 复杂模型（CNN/Attention 处理局部视野）：导出 ONNX + Unity Barracuda / Sentis。
- 接入点：`WorkerBrain.Decide()` 替换为模型推理，输出映射回 `WorkerBrain.Decision`
  结构体，`WorkerSeekState` 的分派 switch 不动（稳定边界）。

## 注意事项

- **教师规则版本**：模拟器复刻的 WorkerBrain 阈值以 `WorkerBrain.cs` 常量为准，
  若 C# 侧规则变更，需同步更新 `simulator.py` 顶部常量。
- **数据来源**：本次训练数据为模拟器合成，接入 Unity 后可换成真实运行轨迹
  （在 `WorkerSeekState.ExecuteAutonomousDecision` 加 JSON 导出器）。
