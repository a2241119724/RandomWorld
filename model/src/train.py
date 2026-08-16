"""训练入口：训练效用函数 baseline 或 MLP。

用法（在 model/ 目录下）：
    python src/train.py --model baseline
    python src/train.py --model mlp
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import yaml

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.models.baseline_utility import UtilityBaseline  # noqa: E402
from src.actions import NUM_ACTIONS  # noqa: E402


def load_data(cfg: dict):
    processed = ROOT / cfg["paths"]["processed_dir"]
    X = np.load(processed / "X.npy")
    y = np.load(processed / "y.npy")
    return X, y


def split(X: np.ndarray, y: np.ndarray, val_ratio: float, test_ratio: float, seed: int):
    rng = np.random.default_rng(seed)
    n = len(y)
    idx = rng.permutation(n)
    n_test = int(n * test_ratio)
    n_val = int(n * val_ratio)
    test_idx = idx[:n_test]
    val_idx = idx[n_test:n_test + n_val]
    train_idx = idx[n_test + n_val:]
    return (X[train_idx], y[train_idx]), (X[val_idx], y[val_idx]), (X[test_idx], y[test_idx])


def accuracy(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    return float((y_true == y_pred).mean())


def train_baseline(X, y, cfg, seed):
    (X_tr, y_tr), (X_va, y_va), (X_te, y_te) = split(
        X, y, cfg["split"]["val_ratio"], cfg["split"]["test_ratio"], seed)

    model = UtilityBaseline(
        C=cfg["baseline"]["C"], max_iter=cfg["baseline"]["max_iter"], seed=seed)
    model.fit(X_tr, y_tr)

    results = {
        "train_acc": accuracy(y_tr, model.predict(X_tr)),
        "val_acc": accuracy(y_va, model.predict(X_va)),
        "test_acc": accuracy(y_te, model.predict(X_te)),
    }

    # 保存完整模型（供 evaluate 复用）与权重（供 export 复用）
    import joblib
    export_dir = ROOT / cfg["paths"]["export_dir"]
    export_dir.mkdir(parents=True, exist_ok=True)
    joblib.dump(model, export_dir / "baseline.joblib")
    np.savez(
        export_dir / "baseline.npz",
        coef=model.weights, intercept=model.bias,
    )
    return model, results


def train_mlp(X, y, cfg, seed):
    import torch
    import torch.nn as nn
    from torch.utils.data import DataLoader
    from src.dataset import WorkerDecisionDataset
    from src.models.mlp import WorkerMLP

    torch.manual_seed(seed)

    (X_tr, y_tr), (X_va, y_va), (X_te, y_te) = split(
        X, y, cfg["split"]["val_ratio"], cfg["split"]["test_ratio"], seed)

    input_dim = X.shape[1]
    train_loader = DataLoader(WorkerDecisionDataset(X_tr, y_tr),
                              batch_size=cfg["training"]["batch_size"], shuffle=True)
    val_loader = DataLoader(WorkerDecisionDataset(X_va, y_va),
                            batch_size=cfg["training"]["batch_size"], shuffle=False)

    model = WorkerMLP(
        input_dim=input_dim,
        num_actions=NUM_ACTIONS,
        hidden_dims=cfg["mlp"]["hidden_dims"],
        dropout=cfg["mlp"]["dropout"],
        activation=cfg["mlp"]["activation"],
    )
    optimizer = torch.optim.Adam(
        model.parameters(),
        lr=cfg["training"]["learning_rate"],
        weight_decay=cfg["training"]["weight_decay"],
    )
    criterion = nn.CrossEntropyLoss()

    epochs = cfg["training"]["epochs"]
    patience = cfg["training"]["patience"]
    best_val_acc = 0.0
    best_state = None
    bad_epochs = 0

    for epoch in range(1, epochs + 1):
        model.train()
        total_loss = 0.0
        for xb, yb in train_loader:
            optimizer.zero_grad()
            logits = model(xb)
            loss = criterion(logits, yb)
            loss.backward()
            optimizer.step()
            total_loss += loss.item() * len(yb)
        avg_loss = total_loss / len(y_tr)

        model.eval()
        correct = 0
        total = 0
        with torch.no_grad():
            for xb, yb in val_loader:
                logits = model(xb)
                pred = logits.argmax(dim=1)
                correct += (pred == yb).sum().item()
                total += len(yb)
        val_acc = correct / total

        if epoch % 5 == 0 or epoch == 1:
            print(f"    epoch {epoch:3d}/{epochs}  loss={avg_loss:.4f}  val_acc={val_acc:.4f}")

        if val_acc > best_val_acc:
            best_val_acc = val_acc
            best_state = {k: v.clone() for k, v in model.state_dict().items()}
            bad_epochs = 0
        else:
            bad_epochs += 1
            if bad_epochs >= patience:
                print(f"    早停于 epoch {epoch}（val_acc 连续 {patience} 轮不升）")
                break

    model.load_state_dict(best_state)
    model.eval()

    # 测试集评估
    test_loader = DataLoader(WorkerDecisionDataset(X_te, y_te),
                             batch_size=cfg["training"]["batch_size"], shuffle=False)
    correct = 0
    total = 0
    with torch.no_grad():
        for xb, yb in test_loader:
            pred = model(xb).argmax(dim=1)
            correct += (pred == yb).sum().item()
            total += len(yb)
    test_acc = correct / total

    # 保存
    export_dir = ROOT / cfg["paths"]["export_dir"]
    export_dir.mkdir(parents=True, exist_ok=True)
    torch.save({
        "state_dict": {k: v.cpu() for k, v in best_state.items()},
        "input_dim": input_dim,
        "num_actions": NUM_ACTIONS,
        "hidden_dims": cfg["mlp"]["hidden_dims"],
        "activation": cfg["mlp"]["activation"],
    }, export_dir / "mlp.pt")

    results = {"val_acc": best_val_acc, "test_acc": test_acc}
    return model, results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", choices=["baseline", "mlp"], required=True)
    parser.add_argument("--config", default="config/model_config.yaml")
    args = parser.parse_args()

    cfg = yaml.safe_load((ROOT / args.config).read_text(encoding="utf-8"))
    seed = cfg["data"]["seed"]

    print(f"[train] 加载数据 ...")
    X, y = load_data(cfg)
    print(f"[train] X={X.shape}, y={y.shape}, 行为数={NUM_ACTIONS}")

    if args.model == "baseline":
        print("[train] 训练效用函数 baseline（逻辑回归）...")
        model, results = train_baseline(X, y, cfg, seed)
    else:
        print("[train] 训练 MLP ...")
        model, results = train_mlp(X, y, cfg, seed)

    print(f"[train] 完成 {args.model}: {results}")


if __name__ == "__main__":
    main()
