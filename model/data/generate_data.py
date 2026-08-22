"""数据生成入口：训练集（纯极值随机组合）与测试集（现实分布），标签由 LLM 打。

标签来源固定为 DeepSeek LLM 按「现实生活优先级」常识打标签（`src/llm_teacher.py`）；
规则表已删除，`src/rules.py` 只负责采样不再打标签。

用法（在 model/ 目录下）：
    DEEPSEEK_API_KEY=... python data/generate_data.py

产出（data/processed/）：
    train_x.npy / train_y.npy   —— 纯极值随机组合训练集（连续特征取 {min,max}、枚举取全值）
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
from src.rules import generate_training_samples, generate_test_samples  # noqa: E402


def _save_states_csv(states, labels, path: Path) -> None:
    """把 (state dict, action label) 落盘 CSV 供人工抽查。"""
    df = pd.DataFrame(states)
    df["action"] = [ACTIONS[i] for i in labels]
    df.to_csv(path, index=False, encoding="utf-8")


def _sampling_signature(data_cfg: dict, seed: int, system_prompt: str, split: str,
                        teacher_source: str = "api") -> str:
    """LLM 标签缓存键：种子 + 采样配置 + 系统 prompt 的哈希。

    标签 = f(采样到的状态, prompt, 教师)，任一变化缓存即失效。此前缓存只比对长度，
    若采样方式变化但条数不变会静默复用旧标签（数据污染），故把配置纳入键。
    训练集 key 依赖 n_train_total + 教师来源；测试集 key 只依赖 n_test：
    - 改训练规模不连带重打测试标签；
    - 切换教师（api↔web 免费打标签）只重打训练集，测试集继续复用旧缓存（0 成本）。
      注意：测试集 key 不含教师来源，仅当 prompt/采样变化时才失效。
    """
    if split not in ("train", "test"):
        raise ValueError(f"未知 split: {split}（可选 train/test）")
    parts = [str(seed), hashlib.sha256(system_prompt.encode("utf-8")).hexdigest()[:16]]
    if split == "train":
        # 训练集 key 依赖采样方式 + 规模 + 教师来源：任一变化重打训练标签
        parts += [str(data_cfg.get("train_sampling")), str(data_cfg.get("n_train_total")),
                  str(teacher_source)]
    else:
        # 测试集始终现实分布采样，与 train_sampling / 教师无关：改训练配置不重打测试集
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

    raw_dir = cfg.raw_dir
    processed_dir = cfg.processed_dir
    raw_dir.mkdir(parents=True, exist_ok=True)
    processed_dir.mkdir(parents=True, exist_ok=True)

    schema = FeatureSchema.load(cfg.schema_path)
    seed = cfg.data_seed

    # ---- 标签器：api=OpenAI 兼容端点（付费）/ web=网页版浏览器自动化多平台并行（免费）----
    from src.llm_teacher import LLMTeacher
    from src.web_labeler import make_pool

    llm_cfg = cfg["llm"]
    provider = llm_cfg.get("provider", "deepseek")
    names = llm_cfg.get("web", {}).get("platforms", [])
    if not names:
        names = ["deepseek", "wenxin", "qianwen"]
    if provider == "rules":
        # 规则打标：训练集从多模型投票生成的规则文件查表（离线，不依赖浏览器），测试集沿用网页版
        from src.rule_teacher import RuleTeacher
        rule_file = llm_cfg.get("rule_file")
        if not rule_file:
            raise ValueError("provider=rules 需配置 llm.rule_file（先运行 --vote-rules 生成）")
        train_teacher = RuleTeacher(cfg, schema, rule_file)
        test_teacher = make_pool(cfg, schema, names)
    elif provider == "web":
        train_teacher = test_teacher = make_pool(cfg, schema, names)
    elif provider == "deepseek":
        train_teacher = test_teacher = LLMTeacher(cfg, schema)
    else:
        raise ValueError(f"未知 llm.provider: {provider}（可选 deepseek/web/rules）")

    cache_dir = llm_cfg["cache_dir"]
    cache_dir = cache_dir if Path(cache_dir).is_absolute() else ROOT / cache_dir
    tr_cache_key = _sampling_signature(data_cfg, seed, train_teacher._system_prompt,
                                       "train", train_teacher.source)
    te_cache_key = _sampling_signature(data_cfg, seed, test_teacher._system_prompt,
                                       "test", test_teacher.source)

    # 网页版打标签：训练集断点续跑文件（进程中断后重跑从断点继续）
    if provider == "web":
        train_teacher.progress_file = Path(cache_dir) / f"web_progress_{tr_cache_key}.json"

    # 护栏：网页版/规则模式不自动重打超大规模测试集（625 批不可行）
    if provider in ("web", "rules"):
        max_test_batches = llm_cfg.get("web", {}).get("max_test_relabel_batches", 60)
        n_test = data_cfg["n_test"]
        te_cache_path = Path(cache_dir) / f"llm_labels_test_{te_cache_key}.npy"
        te_hit = te_cache_path.exists() and len(np.load(te_cache_path)) == n_test
        if not te_hit and (n_test / test_teacher.batch_size) > max_test_batches:
            raise SystemExit(
                f"[generate_data] 网页版不打超大规模测试集：{n_test} 条 = "
                f"{n_test / test_teacher.batch_size:.0f} 批 > 上限 {max_test_batches} 批。\n"
                f"请先用 API 教师（llm.provider: deepseek）打测试集，或调小 data.n_test。"
            )

    tr_label_fn = _CachedTeacher(train_teacher, cache_dir, "train", tr_cache_key).label
    te_label_fn = _CachedTeacher(test_teacher, cache_dir, "test", te_cache_key).label
    print(f"[generate_data] 标签来源 = {train_teacher.source}（{train_teacher.model}）  seed={seed}  "
          f"train_cache={tr_cache_key}  test_cache={te_cache_key}")

    # ---- 训练集：纯极值随机组合 ----
    tr_states, tr_y = generate_training_samples(
        schema, cfg.raw, seed, tr_label_fn)
    X_tr = encode_many(schema, tr_states)
    print(f"[generate_data] 训练集 = {X_tr.shape}（纯极值随机组合，确定性可复现）")

    # ---- 测试集：现实分布 ----
    te_states, te_y = generate_test_samples(
        data_cfg["n_test"], schema, cfg.raw, seed, te_label_fn)
    X_te = encode_many(schema, te_states)
    print(f"[generate_data] 测试集 = {X_te.shape}（现实分布，独立于训练）")

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
