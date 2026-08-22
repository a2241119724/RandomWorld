"""训练入口：按注册表分发，训练指定模型或全部模型。

用法（在 model/ 目录下）：
    python src/train.py --model all
    python src/train.py --model mlp

数据：训练边界集内部确定性切 train/val（早停监控），测试用独立现实分布集。
新增模型无需改本文件——实现 DecisionModel 子类并注册即自动纳入。
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import NUM_ACTIONS  # noqa: E402
from src.config import ModelConfig  # noqa: E402
from src.dataio import load_train_val, load_test  # noqa: E402
from src.models import list_models, create_model  # noqa: E402


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
    print(f"[train] 训练集 {X_tr.shape}（val {len(y_va)} 条早停）"
          f"  测试集 {X_te.shape}  行为数={NUM_ACTIONS}")

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
