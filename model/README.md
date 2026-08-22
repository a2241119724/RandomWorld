# Worker 行为决策模型（Python / PyTorch）

用机器学习替代/增强 `WorkerBrain.Decide()` 的硬编码优先级级联，训练一个
「状态 → 行为」决策模型：输入 Worker 状态数值（心情、饥饿、疲劳、人格、经济、
阶段、目标、好感度 + 全局/局部信息），输出 14 种行为之一（`WorkerDecisionType`）。
训练后导出权重给 Unity C# 侧推理。

数据不再依赖模拟器教师——训练/测试标签默认由 **DeepSeek LLM 按「现实生活优先级」
的常识直接判断**（`src/llm_teacher.py`）：训练集按**边界覆盖采样**（`boundary`，
连续特征只取极值 min/max、枚举取全值），测试集 20000 条独立现实分布。训练时按
`training.sample_proportions` 的目标比例**物理拷贝**训练集（对齐游戏内现实比例）。
规则表（`config/decision_rules.yaml`）保留但不参与训练，作为对比基线（分歧体检）与
LLM 失败兜底。

## 目录结构

```
model/
├── config/
│   ├── feature_schema.yaml      # 可扩展特征定义（新增输入只改这里）
│   ├── decision_rules.yaml      # 人工规则表（对比基线 + LLM 失败兜底；label_source=rule 时打标签）
│   └── model_config.yaml        # 数据/模型/训练/导出超参数（含 label_source 与 llm）
├── data/
│   ├── generate_data.py         # 数据生成入口（LLM/规则表打标签 → boundary 训练集 + 现实测试集）
│   ├── raw/                     # 原始 (state, action) CSV（train/test_states.csv）
│   ├── cache/                   # LLM 标签缓存（llm_labels_{train,test}.npy，重跑 0 成本）
│   └── processed/               # train_x/y.npy（boundary 训练集） + test_x/y.npy（现实测试集）
├── src/
│   ├── actions.py               # 14 种行为常量（与 C# WorkerDecisionType 对齐）
│   ├── features.py              # 特征提取/归一化（读 feature_schema）
│   ├── rules.py                 # 规则引擎：RuleSet 加载/校验 + 采样（边界/现实）+ 打标签
│   ├── llm_teacher.py           # LLM 教师：DeepSeek 按现实常识打标签（label_source=llm）
│   ├── config.py                # ModelConfig 配置门面
│   ├── dataio.py                # 训练/验证/测试数据加载与确定性切分
│   ├── dataset.py               # PyTorch Dataset
│   ├── training.py              # 共享 torch 训练循环（早停/类别权重）
│   ├── unity_export.py          # 共享导出助手（weights.json / .bytes / ONNX）
│   ├── models/
│   │   ├── base.py              # DecisionModel 抽象基类（新增模型的统一接口）
│   │   ├── registry.py          # 注册表 + create/load 工厂
│   │   ├── torch_adapter.py     # 统一 nn.Module 的适配器（fit/save/export 通用）
│   │   ├── mlp.py               # MLP 策略网络（注册名 mlp）
│   │   └── attention.py         # FT-Transformer（注册名 attention）
│   ├── train.py                 # 训练（--model all 遍历注册表）
│   ├── evaluate.py              # 评估与对比
│   ├── export.py                # 导出（遍历注册表）
│   └── visualize.py             # 可视化（遍历注册表）
├── unity_bridge/
│   └── WorkerModelInference.cs.example  # C# 推理层模板
└── experiments/                 # 训练/导出产物（.pt/.joblib/权重/.onnx/viz/）
```

## 快速开始

```bash
cd model

# 0. 环境：torch 对 Python 3.14 支持可能滞后，建议用 3.11/3.12 的 venv
python -m venv .venv && source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
export DEEPSEEK_API_KEY=sk-...      # llm 标签模式需要（Windows: $env:DEEPSEEK_API_KEY="sk-...")

# 1. 生成数据（boundary 训练集 + 现实测试集，标签 = DeepSeek LLM 按现实常识打）
python data/generate_data.py
#    重跑同 seed 命中缓存（data/cache/llm_labels_*.npy），0 次 API 调用；
#    想改回规则表标签：config/model_config.yaml 的 data.label_source 设回 rule 即可。

# 2. 训练（--model all = 注册表内全部；也可指定单个）
python src/train.py --model all

# 3. 评估对比（现实测试集）
python src/evaluate.py

# 4. 导出（ONNX + weights.json + Unity .bytes）
python src/export.py

# 5. 可视化报告（experiments/viz/）
python src/visualize.py
```

## 扩展新模型（设计模式）

新增模型只需三步，`train/evaluate/export/visualize` 因遍历注册表自动生效：

1. 实现 `DecisionModel` 子类（`src/models/base.py` 抽象基类）。
   普通网络继承 `TorchDecisionModel` 适配器，只需声明 `net_cls` / `meta_keys` /
   `flattenable` 三个类属性。
2. 加 `@register("模型名")` 装饰器注册。
3. 在 `config/model_config.yaml` 加一个超参段，并在 `src/models/__init__.py` import。

## 标签来源：LLM 教师（默认）与规则表（备选）

`data.label_source`（`config/model_config.yaml`）控制标签来源：

- **`llm`（默认）**：`src/llm_teacher.py` 把 14 种行为定义 + 每个状态字段的中文说明与
  数值范围组装成系统 prompt（范围由 `derive_state_bounds` 自动推导，新增特征无需改
  prompt），让 DeepSeek 按**现实生活优先级**判断每个状态该做什么。`temperature=0` +
  缓存（`data/cache/llm_labels_*.npy`，同 seed 重跑 0 次 API 调用）保证可复现。
- **`rule`**：`config/decision_rules.yaml` 规则表打标签（旧路径，确定性）。规则自上而下
  匹配，第一条命中生效，最后一条 `when:{}` 兜底。条件值语义：
  `{ max: X }`→`<=X`、`{ min: X }`→`>=X`、`{ min: A, max: B }`→区间、`{ eq: V }`、`{ in: [a,b] }`；
  连续键只用 min/max，枚举键只用 eq/in。

训练集 = 边界覆盖采样（`boundary`：连续特征只取极值 min/max × 枚举全值，确定性
小集合），训练时由 `training.sample_proportions` **物理拷贝**拉到游戏内现实比例
（不同状态数由 `data.repeats` / `data.n_train_per_rule` 控制）；测试集 = 现实分布
均匀采样 `n_test` 条、独立于训练集。已知局限：边界覆盖下模型看不到 hungry=50 等
中间态，学不会连续决策斜坡（hungry 降 → eat 概率升），现实测试集上易坍缩到主类——
这是当前方案的取舍。

`llm` 模式下生成数据时会额外打印**分歧报告**：同一批状态分别用 LLM 与规则表打标签，
输出分歧率 + 按行为明细（如「规则表打 self_gather 1200 条，LLM 同意 800 条」）。分歧大
说明「现实常识」与「游戏规则」（规则表复刻自 C# WorkerBrain）冲突，是部署前需关注的信号。
LLM 输出非法/超时重试失败时，自动回退到规则表兜底，确保数据永不残缺。

换标签来源后：`python data/generate_data.py` 重新生成 → `python src/train.py --model all`。

## 遗传算法定位（结论）

**遗传算法不适合作为「状态→行为」的实时决策器本体**——它是优化器而非决策器，
无法高效处理连续高维状态空间，逐帧决策慢且样本效率低。

GA 的正确位置是**离线调参**：把效用函数 baseline 的权重向量当基因，适应度 =
规则表/真实环境里的长期生存/收益，用 GA（或贝叶斯优化）搜索权重。神经进化（NEAT）可优化
网络结构，但收敛慢、难调，仅作备选。

推荐路径：**效用函数（可解释线性）→ MLP（行为克隆）→ 强化学习（PPO）**，GA 保留
为效用函数权重搜索的辅助工具。

## 如何扩展输入特征

1. 在 `config/decision_rules.yaml` 的 `sampling_overrides` / 规则条件里用到的
   状态字段需在规则表登记（`int_keys` 声明整数键、`ratio_max` 声明 ratio 上限）。
2. 在 `config/feature_schema.yaml` 加一个条目（`name/group/kind/key`）。
3. `src/features.py` 自动读入，特征向量维度随之增加；`derive_state_bounds` 自动
   推导采样边界，无需改其他代码。

`kind` 取值：`ratio`（当前/最大）、`minmax`（线性缩放）、`log`（log1p 压缩）、
`onehot`（类别展开）、`passthrough`（已归一化）。

## 人格维度（现有 + 建议扩展）

现有 4 维（`WorkerPersonality.cs`，[0,100]）：`Mood` 心情、`Ambition` 事业心、
`Diligence` 勤奋、`Sociality` 社交。

建议新增（现实社会数值，本框架已在 schema 预留特征位）：`Greed` 贪婪、`Laziness`
懒惰、`Curiosity` 好奇、`Bravery` 勇气、`Loyalty` 忠诚。本次 Python 侧预留
`greed`/`laziness` 两个特征位（默认 50），是否同步改 C# `WorkerPersonality` 属后续
Unity 接入阶段决定。

## Unity 对接（后续阶段）

- 简单 MLP：`export.py` 导出 `mlp_weights.json` + `mlp_weights.bytes`，C# 手写前向传播
  （见 `unity_bridge/WorkerModelInference.cs.example`），零外部依赖。
- 复杂模型（CNN/Attention 处理局部视野）：导出 ONNX + Unity Barracuda / Sentis。
- 注意力模型：只导出 `attention.onnx`（attention 无法压平成 Linear 层），
  Unity 侧需 ONNX + Sentis/Barracuda。
- 接入点：`WorkerBrain.Decide()` 替换为模型推理，输出映射回 `WorkerBrain.Decision`
  结构体，`WorkerSeekState` 的分派 switch 不动（稳定边界）。

## 模型结论（2026-08-22 LLM 标签 + boundary 训练集 + 现实比例拷贝）

当前方案：boundary 极值训练集（~1071 条不同状态，`repeats: 16`）+ 训练时按
`training.sample_proportions` 物理拷贝（~4000 条）拉现实比例。仅保留 mlp
（baseline / attention 旧产物已移除）。

- 测试真值 = LLM 常识，acc 是「模型 vs LLM 常识」一致性；准确率~50% **不是退化**——
  LLM 标签本身在 hungry=0.5 等中间态上「50% 吃、50% 不吃」，argmax 天然只对一半。
- **当前 mlp 在 boundary 极值训练下坍缩到 ~100% gather**（test_acc ≈ 主类占比）：
  训练集连续特征只取极值 {0,1}，模型看不到中间态（hungry=0.5 该吃还是该干活），
  学到的极值规则在现实测试集（连续中间态）上几乎不触发 → 全判 gather。
  `learning_rate` 调低（0.001→0.0001）只让坍缩更平滑、不改变坍缩本身——这不是超参
  问题，是**极值训练分布 ↔ 中间态测试分布不匹配**的结构性局限。
- **历史对照（uniform 10000 条时代，已弃用）**：数据量是 NN 学不会斜坡的根因——
  384 条过拟合坍缩到主类，10000 条不同状态 + hidden_dims [64,32] 后学会连续斜坡
  （hungry→eat 概率逐桶贴合 LLM 真值）。uniform 采样被用户否决，故当前保留 boundary。
- 长尾类（post_bounty/pickup/self_plant/store/withdraw 等）LLM 常识标签天然缺失
  （不是现实常见行为），训练/测试均无样本，模型学不到——这是标签来源的上限，
  非模型缺陷；macro-F1 被这些 0 召回类拉低。

## 注意事项

- **训练集规模（data.repeats / data.n_train_per_rule）**：boundary 下提高它们可枚举
  更多不同极值状态（364→1071），缓解坍缩但无法根治（仍无中间态）。LLM 标签缓存 key
  只含 seed + prompt + train_sampling + n_train_total，改 repeats 可复用已打标签。
- **检查模型健康看预测分布而非 val_acc**：重叠标签区间上 argmax 只有一半对是正常的，
  看输出分布是否贴真值（健康：gather 67%/eat 23%/…；坍缩：≈100% 单类）。
- **Unity .bytes 契约**：`experiments/mlp_weights.bytes` 布局
  `{int32 num_layers, 每层(int32 out, int32 in, float32[out*in] W, float32[out] b)}`
  与 C# `WorkerModelInference.Load` 一一对应；换配置后重新 `export.py` 并替换
  `Assets/Resources/model/mlp_weights.bytes`（不动 .meta）。
- **数据来源**：默认 `label_source: llm`（DeepSeek 常识打标签），切回规则表用 `rule`。
  模型上限 = 标签来源上限——LLM 常识是第一版教师；将来追求最优可换真实运行轨迹
  （在 `WorkerSeekState.ExecuteAutonomousDecision` 加 JSON 导出器）或上 RL。
