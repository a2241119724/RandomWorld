"""数据生成入口：训练集（边界覆盖）与测试集（现实分布），标签来源可切换。

标签来源由 config/model_config.yaml 的 data.label_source 决定：
- rule：规则表（src/rules.py）打标签 —— 传统确定性路径
- llm：DeepSeek LLM 按「现实生活优先级」常识打标签（src/llm_teacher.py），
  规则表保留作对比基线（分歧报告）与失败兜底。

用法（在 model/ 目录下）：
    DEEPSEEK_API_KEY=... python data/generate_data.py   # llm 模式
    python data/generate_data.py                        # rule 模式

产出（data/processed/）：
    train_x.npy / train_y.npy   —— 边界覆盖训练集（连续特征取 min/max、枚举取全值）
    test_x.npy / test_y.npy     —— 独立现实分布测试集（连续随机小数、枚举随机）
    feature_names.txt / action_names.txt
原始 (state, action) CSV 落 data/raw/（train_states.csv / test_states.csv，供人工抽查）。
LLM 标签缓存 data/cache/llm_labels_{train,test}.npy（同 seed + 同 prompt 幂等复用，
重跑 0 次 API 调用）。
"""
from __future__ import annotations

import hashlib
import sys
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_INDEX  # noqa: E402
from src.config import ModelConfig  # noqa: E402
from src.features import FeatureSchema, encode_many  # noqa: E402
from src.rules import RuleSet, generate_training_samples, generate_test_samples  # noqa: E402


def _save_states_csv(states, labels, path: Path) -> None:
    """把 (state dict, action label) 落盘 CSV 供人工抽查。"""
    df = pd.DataFrame(states)
    df["action"] = [ACTIONS[i] for i in labels]
    df.to_csv(path, index=False, encoding="utf-8")


def _disagreement_report(states, labels, ruleset, tag: str) -> None:
    """体检（LLM 模式专用）：LLM 标签 vs 规则表标签的分歧率 + 按行为明细。

    规则表 = 复刻 C# WorkerBrain 优先级的旧教师；LLM = 现实生活常识教师。
    分歧大的行为说明「游戏规则」与「常识」冲突，是部署前需关注的信号。
    """
    rule_y = np.array([ACTION_INDEX[ruleset.evaluate(st)] for st in states], dtype=np.int64)
    agree = rule_y == labels
    print(f"[generate_data] {tag}: LLM vs 规则表 分歧率 = {(1 - agree.mean()) * 100:.1f}%")
    for a in range(len(ACTIONS)):
        mask = rule_y == a
        cnt = int(mask.sum())
        if cnt == 0:
            continue
        print(f"    规则表打「{ACTIONS[a]:<12s}」{cnt:>5} 条, LLM 同意 {agree[mask].mean() * 100:5.1f}%")


def _sampling_signature(data_cfg: dict, seed: int, system_prompt: str, split: str) -> str:
    """LLM 标签缓存键：种子 + 采样配置 + 系统 prompt 的哈希。

    标签 = f(采样到的状态, prompt)，任一变化缓存即失效。此前缓存只比对长度，
    若采样方式变化但条数不变会静默复用旧标签（数据污染），故把配置纳入键。
    训练集 key 依赖 n_train_total、测试集 key 依赖 n_test：改训练规模不连带
    重打测试标签（此前两者混入同一 key，改 n_train_total 会重打 2 万条测试集）。
    """
    if split not in ("train", "test"):
        raise ValueError(f"未知 split: {split}（可选 train/test）")
    parts = [str(seed), hashlib.sha256(system_prompt.encode("utf-8")).hexdigest()[:16]]
    if split == "train":
        # 训练集 key 依赖采样方式 + 规模：标签随训练采样配置变化
        parts += [str(data_cfg.get("train_sampling")), str(data_cfg.get("n_train_total"))]
    else:
        # 测试集始终现实分布采样，与 train_sampling 无关：改训练配置不重打测试集
        parts.append(str(data_cfg.get("n_test")))
    return hashlib.md5("|".join(parts).encode("utf-8")).hexdigest()[:12]


class _CachedTeacher:
    """带 npy 缓存的 LLM 标签器：同 seed + 同采样配置 + 同 prompt 幂等复用，重跑 0 次 API 调用。"""

    def __init__(self, teacher, cache_dir: Path, split: str, cache_key: str):
        self.teacher = teacher
        self.cache_path = Path(cache_dir) / f"llm_labels_{split}_{cache_key}.npy"

    def label(self, states: list[dict]) -> list[str]:
        n = len(states)
        if self.cache_path.exists():
            arr = np.load(self.cache_path)
            if len(arr) == n:
                print(f"[generate_data] 命中 LLM 标签缓存 {self.cache_path.name}（{n} 条，0 次 API 调用）")
                return [ACTIONS[int(i)] for i in arr]
        labels = self.teacher.label(states)
        arr = np.array([ACTION_INDEX[a] for a in labels], dtype=np.int64)
        self.cache_path.parent.mkdir(parents=True, exist_ok=True)
        np.save(self.cache_path, arr)
        print(f"[generate_data] LLM 标签已缓存 -> {self.cache_path}")
        return labels


def main() -> None:
    cfg = ModelConfig()
    data_cfg = cfg["data"]
    label_source = data_cfg.get("label_source", "rule")

    raw_dir = cfg.raw_dir
    processed_dir = cfg.processed_dir
    raw_dir.mkdir(parents=True, exist_ok=True)
    processed_dir.mkdir(parents=True, exist_ok=True)

    schema = FeatureSchema.load(cfg.schema_path)
    ruleset = RuleSet.load(cfg.rules_path)
    seed = cfg.data_seed

    # ---- 标签器 ----
    if label_source == "rule":
        tr_label_fn = te_label_fn = None  # 采样函数默认用 ruleset.evaluate
        print(f"[generate_data] 标签来源 = 规则表（{len(ruleset.rules)} 条）  seed={seed}")
    elif label_source == "llm":
        from src.llm_teacher import LLMTeacher  # 延迟导入：rule 模式不依赖 openai

        teacher = LLMTeacher(cfg, schema, ruleset)
        cache_dir = cfg["llm"]["cache_dir"]
        cache_dir = cache_dir if Path(cache_dir).is_absolute() else ROOT / cache_dir
        tr_cache_key = _sampling_signature(data_cfg, seed, teacher._system_prompt, "train")
        te_cache_key = _sampling_signature(data_cfg, seed, teacher._system_prompt, "test")
        tr_label_fn = _CachedTeacher(teacher, cache_dir, "train", tr_cache_key).label
        te_label_fn = _CachedTeacher(teacher, cache_dir, "test", te_cache_key).label
        print(f"[generate_data] 标签来源 = DeepSeek LLM（{teacher.model}）  seed={seed}  "
              f"train_cache={tr_cache_key}  test_cache={te_cache_key}")
    else:
        raise SystemExit(f"未知 label_source: {label_source}（可选 rule / llm）")

    # ---- 训练集：边界覆盖 ----
    tr_states, tr_y, hits = generate_training_samples(
        ruleset, schema, cfg.raw, seed, tr_label_fn)
    X_tr = encode_many(schema, tr_states)
    print(f"[generate_data] 训练集 = {X_tr.shape}（边界覆盖，确定性小集合）")

    # ---- 测试集：现实分布 ----
    te_states, te_y = generate_test_samples(
        data_cfg["n_test"], ruleset, schema, cfg.raw, seed, te_label_fn)
    X_te = encode_many(schema, te_states)
    print(f"[generate_data] 测试集 = {X_te.shape}（现实分布，独立于训练）")

    # ---- 体检报告 ----
    if label_source == "rule":
        # 覆盖度体检：实际命中自身 action 的样本数（0 → 被更早规则遮蔽）
        uncovered = [name for name, h in hits.items() if h == 0]
        if uncovered:
            print(f"[generate_data] WARN 不可达规则 {len(uncovered)} 条（在极值采样下从未命中自身 action）：")
            for name in uncovered:
                print(f"    - {name}")
        else:
            print("[generate_data] 所有规则在训练集均有样本命中自身 action")
    else:
        _disagreement_report(tr_states, tr_y, ruleset, "训练集")
        _disagreement_report(te_states, te_y, ruleset, "测试集")

    # ---- 落盘 ----
    np.save(processed_dir / "train_x.npy", X_tr)
    np.save(processed_dir / "train_y.npy", tr_y)
    np.save(processed_dir / "test_x.npy", X_te)
    np.save(processed_dir / "test_y.npy", te_y)
    (processed_dir / "feature_names.txt").write_text(
        "\n".join(schema.feature_names()), encoding="utf-8")
    (processed_dir / "action_names.txt").write_text("\n".join(ACTIONS), encoding="utf-8")

    if data_cfg.get("save_raw_csv", True):
        _save_states_csv(tr_states, tr_y, raw_dir / "train_states.csv")
        _save_states_csv(te_states, te_y, raw_dir / "test_states.csv")

    # ---- 行为分布 ----
    for tag, y, n in (("train", tr_y, len(tr_y)), ("test", te_y, len(te_y))):
        dist = pd.Series([ACTIONS[i] for i in y]).value_counts(normalize=True)
        print(f"[generate_data] {tag} 行为分布:")
        for name, ratio in dist.items():
            print(f"    {name:<16s} {ratio * 100:5.2f}%  ({int(ratio * n)} 条)")

    print(f"[generate_data] 完成 -> {processed_dir}")
    print(f"[generate_data] 原始 CSV -> {raw_dir / 'train_states.csv'} / {raw_dir / 'test_states.csv'}")


if __name__ == "__main__":
    main()
