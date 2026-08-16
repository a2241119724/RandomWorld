"""数据生成入口：调用模拟器批量采样，产出原始 CSV 与归一化特征矩阵。

用法（在 model/ 目录下）：
    python data/generate_data.py
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
import pandas as pd
import yaml

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.simulator import generate_samples, to_label_index  # noqa: E402
from src.features import build_schema, encode_many  # noqa: E402
from src.actions import ACTIONS  # noqa: E402


def main() -> None:
    cfg = yaml.safe_load((ROOT / "config" / "model_config.yaml").read_text(encoding="utf-8"))
    paths = cfg["paths"]
    num_samples = cfg["data"]["num_samples"]
    seed = cfg["data"]["seed"]

    raw_dir = ROOT / paths["raw_dir"]
    processed_dir = ROOT / paths["processed_dir"]
    raw_dir.mkdir(parents=True, exist_ok=True)
    processed_dir.mkdir(parents=True, exist_ok=True)

    print(f"[generate_data] 采样 {num_samples} 条 (seed={seed}) ...")
    samples = generate_samples(num_samples, seed)

    states = [s["state"] for s in samples]
    actions = [s["action"] for s in samples]
    labels = np.array([to_label_index(a) for a in actions], dtype=np.int64)

    # 特征向量化
    schema = build_schema(paths["schema"])
    X = encode_many(schema, states)
    print(f"[generate_data] 特征维度 = {schema.dim}, 行为数 = {len(ACTIONS)}")

    # 落盘处理后的数据（训练用）
    np.save(processed_dir / "X.npy", X)
    np.save(processed_dir / "y.npy", labels)
    (processed_dir / "feature_names.txt").write_text(
        "\n".join(schema.feature_names()), encoding="utf-8")
    (processed_dir / "action_names.txt").write_text("\n".join(ACTIONS), encoding="utf-8")

    # 落盘原始 CSV（人工检查/调试用）
    if cfg["data"].get("save_raw_csv", True):
        df = pd.DataFrame(states)
        df["action"] = actions
        df.to_csv(raw_dir / "samples.csv", index=False, encoding="utf-8")

    # 行为分布统计
    dist = pd.Series(actions).value_counts(normalize=True)
    print("[generate_data] 行为分布（前 5）:")
    for name, ratio in dist.head(5).items():
        print(f"    {name:<16s} {ratio * 100:5.2f}%")

    print(f"[generate_data] 完成 -> {processed_dir}")
    print(f"[generate_data] 原始 CSV -> {raw_dir / 'samples.csv'}")


if __name__ == "__main__":
    main()
