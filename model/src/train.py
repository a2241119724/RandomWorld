"""训练入口：按注册表分发，训练指定模型或全部模型。

用法（在 model/ 目录下）：
    python src/train.py --model all
    python src/train.py --model mlp

数据：训练集 = 全部纯极值边界集（不内部切）；验证集/测试集 = 现实分布集确定性切
前段/后段（val 供早停监控中间态泛化，test 独立评估，互不相交）。
新增模型无需改本文件——实现 DecisionModel 子类并注册即自动纳入。
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_LABELS_ZH, NUM_ACTIONS  # noqa: E402
from src.config import ModelConfig  # noqa: E402
from src.dataio import load_train_val, load_test  # noqa: E402
from src.models import list_models, create_model  # noqa: E402
from src.training import sample_training_data  # noqa: E402


def print_action_distribution(y, title):
    """按样本数降序打印训练集各类 action 的数量与占比（行为用中文标签辅助辨识）。"""
    counts = np.bincount(y, minlength=NUM_ACTIONS)
    total = counts.sum()
    print(f"[train] {title}（共 {total} 条）:")
    for c in np.argsort(-counts):
        if counts[c] == 0:
            continue
        zh = ACTION_LABELS_ZH.get(ACTIONS[c], ACTIONS[c])
        print(f"    {ACTIONS[c]:<16s} {zh:<6s} {counts[c]:6d}  {counts[c] / total * 100:6.2f}%")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="all",
                        help=f"模型名，可选: {'/'.join(list_models())}；默认 all 训练全部")
    parser.add_argument("--config", default="config/model_config.yaml")
    args = parser.parse_args()

    cfg = ModelConfig(args.config)
    seed = cfg.data_seed

    print(f"[train] 加载数据 ...")
    (X_tr, y_tr), (X_va, y_va) = load_train_val(cfg, seed)
    X_te, y_te = load_test(cfg)
    input_dim = X_tr.shape[1]
    print(f"[train] 训练集 {X_tr.shape}（全量，无内部切分）"
          f"  验证集 {X_va.shape}（现实分布，早停）"
          f"  测试集 {X_te.shape}（现实分布，独立评估）  行为数={NUM_ACTIONS}")
    print_action_distribution(y_tr, "训练集 action 分布（原始标签）")
    # sample_mode 拷贝后的实际训练分布（none 时与原始一致，无需重打一份）
    _, yo, sample_mode, sample_desc = sample_training_data(X_tr, y_tr, cfg.raw)
    if sample_mode != "none":
        print_action_distribution(yo, f"训练集 action 分布（{sample_desc}）")

    if args.model == "all":
        names = list_models()
    elif args.model in list_models():
        names = [args.model]
    else:
        parser.error(f"未知模型 '{args.model}'，可选: {'/'.join(list_models())}")

    for name in names:
        print(f"[train] 训练 {name} ...")
        model = create_model(name, cfg.raw, input_dim, NUM_ACTIONS, seed)
        results = model.fit(X_tr, y_tr, X_va, y_va, X_te, y_te, cfg.raw, seed)
        path = model.save(cfg.export_dir, cfg.raw, seed)
        print(f"[train] 完成 {name}: {results}")
        print(f"[train] 产物 -> {path}")


if __name__ == "__main__":
    main()
