# Worker 行为决策模型（Python / PyTorch）

用机器学习替代/增强 `WorkerBrain.Decide()` 的硬编码优先级级联，训练一个
「状态 → 行为」决策模型：输入 Worker 状态数值（心情、饥饿、疲劳、人格、经济、
阶段、目标、好感度 + 全局/局部信息），输出 14 种行为之一（`WorkerDecisionType`）。
训练后导出权重给 Unity C# 侧推理。

数据不依赖模拟器教师——训练/测试标签由 **LLM 按「现实生活优先级」的常识直接判断**。
三种教师任选（`llm.provider`）：
- `deepseek` = OpenAI 兼容 API（付费、快）；
- `web` = **网页版浏览器自动化**（免费，特征/输出扩展后反复重打不花钱）；
- `rules` = **各模型写的结构化规则文件**（离线：训练集用每模型规则推导 + 多数投票，
  生成规则文件时一次性调用网页版，之后打标完全离线、不碰平台风控/欠费）。

训练集为**纯极值随机组合**（连续特征只取极值 {min,max}、枚举取全值，随机组合
`n_train_total` 条，全部用于训练不再内部切）。现实分布集（`n_test` 条）确定性
切成两份：前 `split.val_ratio` 做验证集（早停监控中间态真实泛化）、其余做独立
测试集。训练时按 `training.sample_mode` 决定训练集**物理拷贝**模式：`real` 按
`training.sample_proportions` 拉游戏内现实比例（默认）/ `balanced` 1:1 均衡拷贝 /
`none` 不拷贝。
采样边界信息全在 `feature_schema.yaml`（`max_value` / `dtype:int`），`src/rules.py`
只负责采样。

## 目录结构

```
model/
├── config/
│   ├── feature_schema.yaml      # 特征定义 + 采样边界（ratio.max_value / dtype:int；新增输入只改这里）
│   └── model_config.yaml        # 数据/模型/训练/导出超参数（含 llm）
├── data/
│   ├── generate_data.py         # 数据生成入口（LLM/规则打标签 → 纯极值训练集 + 现实测试集）
│   ├── raw/                     # 原始 (state, action) CSV（train/test_states.csv）
│   ├── cache/                   # LLM 标签缓存（llm_labels_{train,test}.npy，重跑 0 成本）
│   ├── rules/models/            # 规则文件（--gen-rules 生成：每可用网页平台一份 + deepseek_api.json；provider: rules 的输入）
│   └── processed/               # train_x/y.npy（纯极值训练集） + test_x/y.npy（现实测试集）
├── src/
│   ├── actions.py               # 14 种行为常量（与 C# WorkerDecisionType 对齐）
│   ├── features.py              # 特征提取/归一化（读 feature_schema）
│   ├── rules.py                 # 采样：纯极值随机组合训练集 + 现实分布测试集（边界推导读 feature_schema）
│   ├── llm_teacher.py           # LLM 教师：DeepSeek API 按现实常识打标签（provider=deepseek）
│   ├── web_labeler.py           # 网页版教师：浏览器自动化免费打标签（provider=web；--gen-rules 生成规则文件）
│   ├── rule_teacher.py          # 规则教师：加载各模型规则文件，推导 + 多数投票打标（provider=rules）
│   ├── config.py                # ModelConfig 配置门面
│   ├── dataio.py                # 数据加载：训练集全量 + 现实分布集切 val/test（确定性）
│   ├── dataset.py               # PyTorch Dataset
│   ├── training.py              # 共享 torch 训练循环（早停/类别权重）
│   ├── unity_export.py          # 共享导出助手（weights.json / .bytes / ONNX）
│   ├── models/
│   │   ├── base.py              # DecisionModel 抽象基类（新增模型的统一接口）
│   │   ├── registry.py          # 注册表 + create/load 工厂
│   │   ├── torch_adapter.py     # 统一 nn.Module 的适配器（fit/save/export 通用）
│   │   ├── mlp.py               # MLP 策略网络（注册名 mlp）
│   │   ├── attention.py         # FT-Transformer（注册名 attention）
│   │   └── gbdt.py              # sklearn GBDT/RandomForest（注册名 gbdt，algorithm 可切）
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
#    provider=deepseek（付费 API）需要：export DEEPSEEK_API_KEY=sk-...
#    provider=web（免费网页版，默认）需要：一次性 `python -m playwright install chromium`
#    web 首次运行会弹出浏览器，手动登录一次 chat.deepseek.com（profile 复用，不存密码）

# 1. 生成数据（纯极值随机组合训练集 + 现实测试集，标签按 llm.provider 由 API 或网页版打）
python data/generate_data.py
#    重跑同 seed 命中缓存（data/cache/llm_labels_*.npy），0 次调用；
#    web 教师自动关「深度思考/联网搜索」开关、自动开新会话、断点续跑；

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

## 标签来源：LLM 教师（API / 网页版 / 规则文件）

把 14 种行为定义 + 每个状态字段的中文说明与数值范围组装成系统 prompt（范围由
`derive_state_bounds` 从 `feature_schema.yaml` 自动推导，新增特征无需改 prompt），
让 LLM 按**现实生活优先级**判断每个状态该做什么。三种教师（`llm.provider`）：

- **`deepseek`**（`src/llm_teacher.py`，付费 API）：`temperature=0` + 缓存
  （`data/cache/llm_labels_*.npy`，同 seed 重跑 0 次 API 调用）保证可复现。
- **`web`**（`src/web_labeler.py`，免费网页版）：Playwright 驱动真实浏览器并行访问
  多平台（deepseek/wenxin/qianwen/doubao/yuanbao/kimi/chatgpt），复用持久化
  profile（一次性手动登录，不存密码）；自动关「深度思考/联网搜索」开关、每批开新
  会话、逐批落盘进度（崩溃/登录过期后重跑断点续传）。平台级 batch_size 与批间
  `delay_sec` 可调（风控严的平台调小批量，如 doubao=16）。
- **`rules`**（`src/rule_teacher.py`，离线）：训练集用**各模型写的结构化规则**推导 +
  多数投票打标，不依赖浏览器/网络。规则文件由 `--gen-rules` 一次性生成：每个可用
  通道各写一份 `data/rules/models/<通道>.json`（程序填充字段描述/取值范围 + 模型写的
  `{"when": {字段: 约束}, "action": 行为}` 规则集），之后打标完全离线。**写规则启用
  深度思考**（规则是泛化决策，需推理字段语义/覆盖边界）：有「深度思考」开关的网页平
  台开它、关「联网搜索」（wenxin 2026-08-23 改版已无此开关，按普通模式生成），DeepSeek
  API 优先 `deepseek-reasoner`（思维链）模型；不选该开关会得到肤浅规则（只有主类兜底、
  覆盖率差）。

web/deepseek 教师：非法/超时/解析失败重试 `max_retries` 次后，逐条回退到主类
`idle`，确保数据永不残缺。rules 教师相反：**规则推导不到的状态不兜底、直接抛错**
（数据不残缺、暴露规则覆盖不全），需补全规则文件或重跑 `--gen-rules`。缓存 key
纳入教师来源（train 段）：切换 provider 只重打训练集，测试集 key 不含教师、继续
复用旧缓存（0 成本）。`data/browser_profile/`（登录 cookie）已 gitignore 严禁入库。

### 规则打标流程（provider: rules）

1. **生成规则文件**（一次性；通道 = 每个可用网页平台 + DeepSeek API）：
   ```bash
   python -m src.web_labeler --gen-rules --platforms kimi,chatgpt   # 小规模验证
   python -m src.web_labeler --gen-rules                            # 全平台（默认 web.platforms）
   ```
   产出 `data/rules/models/<通道>.json`（网页平台 `<平台>.json`、DeepSeek API
   `deepseek_api.json`；已存在则跳过 = 断点续跑）。写规则**启用深度思考**（有开关的
   网页平台开「深度思考」；wenxin 新版无开关则普通模式；API 优先 `deepseek-reasoner`）。
   DeepSeek API 需设置
   `DEEPSEEK_API_KEY`，未设置只跳过该通道不阻塞网页平台；欠费/风控平台冒烟失败自动
   跳过；字段/动作/值类型非法的规则被 `_validate_rules` 丢弃；prompt 建议模型末尾
   写一条兜底规则（when 为空 → 主类），提高覆盖。
2. **规则推导 + 多数投票打标**：把 `model_config.yaml` 的 `llm.provider` 改为
   `rules`，然后：
   ```bash
   python data/generate_data.py
   ```
   训练集逐状态用每模型规则推导（无命中 = 该模型此状态无票）→ 多数投票取标签最多者
   （平局按 `VOTE_PRIORITY`：干活/生产优先）。测试集仍走网页版（规则文件只覆盖训练集）。

规则覆盖要求：极值训练集（每字段仅 {min,max} 两值）下，规则只要覆盖关键场景
（饥饿/疲劳/精气神/目标/附近资源/金币/社交/夜晚）+ 一条兜底，即可 100% 命中
（实测模拟规则对 5000 条全命中）。规则文件可人工审查/调整后重跑——推导完全离线，
无需重新调用平台。

训练集 = **纯极值随机组合**（`n_train_total` 条：连续特征每维随机取 {min,max} 极值、
枚举取全值随机、固定值取定值，组合成不同状态），训练时由
`training.sample_mode` 决定**物理拷贝**模式（`real` 按 `sample_proportions` 拉现实
比例，或 `balanced` 1:1 均衡、`none` 不拷贝）；现实分布集
（`n_test` 条、独立于训练集）确定性切成前 `split.val_ratio` 验证集 + 后段独立
测试集——早停监控的是中间态真实泛化（不再用纯极值内部切 val）。已知局限：纯极值
下模型看不到 hungry=50 等中间态，学不会连续决策斜坡（hungry 降 → eat 概率升），
现实测试集上易坍缩到主类——这是当前方案的取舍。

## 遗传算法定位（结论）

**遗传算法不适合作为「状态→行为」的实时决策器本体**——它是优化器而非决策器，
无法高效处理连续高维状态空间，逐帧决策慢且样本效率低。

GA 的正确位置是**离线调参**：把效用函数的权重向量当基因，适应度 =
真实环境里的长期生存/收益，用 GA（或贝叶斯优化）搜索权重。神经进化（NEAT）可优化
网络结构，但收敛慢、难调，仅作备选。

推荐路径：**效用函数（可解释线性）→ MLP（行为克隆）→ 强化学习（PPO）**，GA 保留
为效用函数权重搜索的辅助工具。

## 如何扩展输入特征

1. 在 `config/feature_schema.yaml` 加一个条目（`name/group/kind/key`）。
   - `ratio` 特征补 `max_value`（max_key 的取值，缺省 100）；
   - 整数键补 `dtype: int`（采样/生成时取整）。
2. `src/features.py` 自动读入，特征向量维度随之增加；`derive_state_bounds` 自动
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

## 模型结论（2026-08-22 LLM 标签 + 纯极值训练集 + 现实比例拷贝）

当前方案：纯极值随机组合训练集（`n_train_total: 1000` 条不同状态，**全部用于
训练**）+ 训练时按 `training.sample_proportions` 物理拷贝（~10000 条）拉现实比例；
现实分布集（`n_test: 500` 条）确定性切 250 验证（早停）+ 250 测试（独立评估，
`split.val_ratio: 0.5`）。注册表含 mlp / attention / gbdt 三个模型（旧采样规则表已弃，
标签教师见上文三种 provider）。

- 测试真值 = LLM 常识，acc 是「模型 vs LLM 常识」一致性；准确率~50% **不是退化**——
  LLM 标签本身在 hungry=0.5 等中间态上「50% 吃、50% 不吃」，argmax 天然只对一半。
- **当前 mlp 在纯极值训练下坍缩到 ~100% gather**（test_acc ≈ 主类占比）：
  训练集连续特征只取极值 {0,1}，模型看不到中间态（hungry=0.5 该吃还是该干活），
  学到的极值规则在现实测试集（连续中间态）上几乎不触发 → 全判 gather。
  `learning_rate` 调低（0.001→0.0001）只让坍缩更平滑、不改变坍缩本身——这不是超参
  问题，是**极值训练分布 ↔ 中间态测试分布不匹配**的结构性局限。
- **拷贝模式对照（`training.sample_mode`）**：按真实比例拷贝（`real`）会让训练集
  60% 是 gather、进一步放大坍缩；`balanced`（1:1 均衡拷贝）消除大类 bias、
  `none`（不拷贝）保留 LLM 原始标签分布——二者均为对照实验项，根因仍是极值训练集
  无中间态斜坡。
- **加深层数无改善（2026-08-22 对照）**：mlp 3 层→5 层（[64,64,64,32]）test_acc
  持平 0.408；attention 2→5 层 encoder + 分类头加深，test_acc 反而 0.404→0.376
  （加深加剧过拟合）。容量不是瓶颈——训练集不含中间态斜坡，加再深也学不到。
  config 现保留 5 层结构作对照，结论依赖 gbdt（树浅 + 集成，抗过拟合）。
- **降低学习率对照（2026-08-22）**：training.lr 0.0001→0.00001（epochs 50→100）：
  mlp test_acc 纹丝不动（0.408）——学习率不是 mlp 坍缩根因；attention 5 层
  0.376→0.408（低 lr 缓解其过拟合），但 KL 反升（0.61→0.69）、macro-F1 反降，
  属过拟合↔拟合不足的混合信号，仍不敌 gbdt（0.416）。根因始终是极值训练分布
  无中间态斜坡。config 现保留 lr=0.00001 + epochs 100。
- **gbdt（sklearn HGB，2026-08-22 新增）**：决策树阈值分裂能从极值点泛化到
  中间态，但训练标签本身不含斜坡（hungry 两端 eat 都是 13%），故 test_acc 仍
  ≈ 主类基线（250 条现实 test 实测 0.416 vs mlp 0.408、attention 0.404）。
  真实收益在**概率分布**：预测 gather 占比 95% < mlp 100%、KL 0.274（≈mlp），
  对 Unity 侧带概率决策门控更友好，且提供 sklearn 可解释性基线
  （feature_importance）。导出 `gbdt_tree.json`（自定义树结构，C# 树遍历，
  零新依赖；叶值已含 lr，推理**不再乘 lr**）。
  RF 备选（`algorithm: random_forest`）实测同样坍缩，仅作对照。
- **历史对照（uniform 10000 条时代，已弃用）**：数据量是 NN 学不会斜坡的根因——
  384 条过拟合坍缩到主类，10000 条不同状态 + hidden_dims [64,32] 后学会连续斜坡
  （hungry→eat 概率逐桶贴合 LLM 真值）。uniform 采样与规则表均已被用户否决/删除，
  故当前保留纯极值。
- 长尾类（post_bounty/pickup/self_plant/store/withdraw 等）LLM 常识标签天然缺失
  （不是现实常见行为），训练/测试均无样本，模型学不到——这是标签来源的上限，
  非模型缺陷；macro-F1 被这些 0 召回类拉低。

## 注意事项

- **训练集规模（data.n_train_total）**：纯极值随机组合的条数。提高它只枚举更多
  不同极值状态组合，缓解坍缩但无法根治（仍无中间态）。LLM 标签缓存 key 只含 seed +
  prompt + train_sampling + n_train_total，改 n_train_total 会重打训练标签。
- **教师来源（llm.provider）**：训练集缓存 key 含教师来源，deepseek↔web↔rules 切换
  会重打训练标签（web/rules 均免费）；测试集 key 不含教师、继续复用旧缓存。若测试集
  缓存缺失（如 prompt 变化）且条数超 `web.max_test_relabel_batches`，网页版会拒绝
  自动重打（2 万条 = 625 批不可行）——大规模重打测试集前先评估 `n_test` 或用 API 教师。
  切 `rules` 前先跑 `--gen-rules` 生成规则文件，否则 generate_data 会报「规则目录不存在」。
- **规则生成需深度思考**：`--gen-rules` 写规则时有「深度思考」开关的网页平台开它
  （wenxin 2026-08-23 改版已移除该开关，按普通模式生成）、DeepSeek API 优先
  `deepseek-reasoner`——关闭深度思考只能得到浅层规则（主类兜底、覆盖差），会直接
  表现为 rules 打标分布严重坍缩。批量打标（打标签本身）仍关闭深度思考以提速。
- **检查模型健康看预测分布而非 val_acc**：重叠标签区间上 argmax 只有一半对是正常的，
  看输出分布是否贴真值（健康：gather 67%/eat 23%/…；坍缩：≈100% 单类）。
- **Unity .bytes 契约**：`experiments/mlp_weights.bytes` 布局
  `{int32 num_layers, 每层(int32 out, int32 in, float32[out*in] W, float32[out] b)}`
  与 C# `WorkerModelInference.Load` 一一对应；换配置后重新 `export.py` 并替换
  `Assets/Resources/model/mlp_weights.bytes`（不动 .meta）。
- **数据来源**：标签来自三种教师之一——DeepSeek API / 网页版平台（`llm_teacher.py`、
  `web_labeler.py`）/ 各模型规则文件（`rule_teacher.py`，`--gen-rules` 生成）；采样
  边界由 `feature_schema.yaml`（`max_value` / `dtype:int`）决定。模型上限 = 标签
  来源上限——LLM 常识是第一版教师；将来追求最优可换真实运行轨迹
  （在 `WorkerSeekState.ExecuteAutonomousDecision` 加 JSON 导出器）或上 RL。
