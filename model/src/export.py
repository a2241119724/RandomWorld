"""导出：把训练好的模型导出为 ONNX 与纯权重 JSON（供 Unity C# 侧推理）。

用法（在 model/ 目录下）：
    python src/export.py
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
import yaml

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS  # noqa: E402


def _tolist(x) -> list:
    return x.tolist()


def export_baseline_weights(export_dir: Path) -> None:
    """把 baseline 的线性效用权重导出为单层权重 JSON。"""
    npz_path = export_dir / "baseline.npz"
    if not npz_path.exists():
        print("[export] 未找到 baseline.npz，跳过 baseline 导出")
        return
    data = np.load(npz_path)
    coef = data["coef"]          # (num_actions, input_dim)
    intercept = data["intercept"]  # (num_actions,)

    feature_names = _load_feature_names()
    payload = {
        "model": "baseline",
        "input_dim": int(coef.shape[1]),
        "num_actions": int(coef.shape[0]),
        "activation": "none",     # 单层线性 + softmax
        "layers": [{"W": _tolist(coef), "b": _tolist(intercept)}],
        "action_names": ACTIONS,
        "feature_names": feature_names,
    }
    out = export_dir / "baseline_weights.json"
    out.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    print(f"[export] baseline 权重 -> {out}")


def export_mlp(export_dir: Path, cfg: dict) -> None:
    """导出 MLP：ONNX + 纯权重 JSON。"""
    import torch

    ckpt_path = export_dir / "mlp.pt"
    if not ckpt_path.exists():
        print("[export] 未找到 mlp.pt，跳过 MLP 导出")
        return

    from src.models.mlp import WorkerMLP

    ckpt = torch.load(ckpt_path, map_location="cpu", weights_only=False)
    model = WorkerMLP(
        input_dim=ckpt["input_dim"],
        num_actions=ckpt["num_actions"],
        hidden_dims=ckpt["hidden_dims"],
        activation=ckpt["activation"],
    )
    model.load_state_dict(ckpt["state_dict"])
    model.eval()

    input_dim = ckpt["input_dim"]

    # ---- 1. 提取纯权重（Linear 层顺序 + 统一激活函数）----
    linear_layers = []
    for module in model.net:
        if isinstance(module, torch.nn.Linear):
            linear_layers.append({
                "W": _tolist(module.weight.detach()),  # (out, in)
                "b": _tolist(module.bias.detach()),    # (out,)
            })

    payload = {
        "model": "mlp",
        "input_dim": input_dim,
        "num_actions": ckpt["num_actions"],
        "activation": ckpt["activation"],   # 除最后一层外每层 Linear 后应用
        "layers": linear_layers,
        "action_names": ACTIONS,
        "feature_names": _load_feature_names(),
    }
    weights_path = export_dir / "mlp_weights.json"
    weights_path.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    print(f"[export] MLP 权重 -> {weights_path}")

    # ---- 2. 导出 ONNX ----
    if cfg.get("export", {}).get("onnx", True):
        onnx_path = export_dir / "mlp.onnx"
        dummy = torch.randn(1, input_dim)
        # opset 18：torch 2.x 新 exporter 的最低支持版本（opset 13 会触发版本转换失败）。
        # 注：Unity 侧走 Barracuda（需低 opset）时，优先用 mlp_weights.json 纯权重手写推理。
        torch.onnx.export(
            model,
            dummy,
            str(onnx_path),
            input_names=["state"],
            output_names=["logits"],
            opset_version=18,
        )
        print(f"[export] MLP ONNX -> {onnx_path}")


def _load_feature_names() -> list[str]:
    processed = ROOT / "data" / "processed"
    f = processed / "feature_names.txt"
    if f.exists():
        return f.read_text(encoding="utf-8").splitlines()
    return []


def main():
    cfg = yaml.safe_load((ROOT / "config" / "model_config.yaml").read_text(encoding="utf-8"))
    export_dir = ROOT / cfg["paths"]["export_dir"]
    export_dir.mkdir(parents=True, exist_ok=True)

    export_baseline_weights(export_dir)
    export_mlp(export_dir, cfg)


if __name__ == "__main__":
    main()
