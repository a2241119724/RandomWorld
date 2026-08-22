"""共享导出助手：把训练好的模型导出为 Unity 可消费的产物。

产物：
- ``weights.json`` —— 纯权重 JSON（C# 手写前向传播）
- ``*.bytes``       —— 扁平二进制权重（C# BinaryReader 直接读，布局见下）
- ``*.onnx``        —— 完整网络（Unity Sentis/Barracuda）

布局约定（与 C# ``WorkerModelInference.Load`` 一一对应，**不可改动**）：
    int32  num_layers
    对每层：
        int32  out_dim
        int32  in_dim
        float32[out_dim * in_dim]  W（行主序，第 o 行 = 第 o 个输出神经元的权重）
        float32[out_dim]           b
"""
from __future__ import annotations

import json
import struct
from pathlib import Path

import numpy as np
import torch.nn as nn

from .actions import ACTIONS


def _processed_dir(cfg) -> Path:
    """兼容 ModelConfig（有 processed_dir 属性）与原始 dict 两种配置形态。"""
    if hasattr(cfg, "processed_dir"):
        return cfg.processed_dir
    return Path(cfg["paths"]["processed_dir"])


def load_feature_names(processed_dir) -> list[str]:
    """从 processed 目录读特征名（与 feature_schema 顺序一致）。"""
    f = Path(processed_dir) / "feature_names.txt"
    if f.exists():
        return f.read_text(encoding="utf-8").splitlines()
    return []


def _tolist(x) -> list:
    return x.tolist()


def extract_linear_layers(net: nn.Module) -> list[dict]:
    """按前向顺序收集所有 Linear 层权重，返回 [{"W": (out,in) list, "b": [out] list}]。

    适用于 Sequential 平铺结构（WorkerMLP）；递归收集保持顺序。
    """
    layers = []
    for module in net.modules():
        if isinstance(module, nn.Linear):
            layers.append({
                "W": _tolist(module.weight.detach().cpu()),  # (out, in)
                "b": _tolist(module.bias.detach().cpu()),    # (out,)
            })
    return layers


def write_binary_bytes(path: Path, linear_layers: list[dict]) -> None:
    """写 Unity 扁平二进制权重（小端 float32）。"""
    with open(path, "wb") as f:
        f.write(struct.pack("<i", len(linear_layers)))
        for layer in linear_layers:
            w = np.asarray(layer["W"], dtype=np.float32)  # (out, in)
            b = np.asarray(layer["b"], dtype=np.float32)  # (out,)
            out_dim, in_dim = w.shape
            f.write(struct.pack("<ii", out_dim, in_dim))
            w.tofile(f)  # C-order 行主序
            b.tofile(f)


def export_onnx(net: nn.Module, input_dim: int, path: Path) -> None:
    """导出 ONNX。opset 18：torch 2.x 新 exporter 的最低支持版本。"""
    import torch
    dummy = torch.randn(1, input_dim)
    torch.onnx.export(
        net, dummy, str(path),
        input_names=["state"], output_names=["logits"],
        opset_version=18,
    )


def export_torch_model(model, export_dir, cfg, processed_dir=None) -> list[Path]:
    """TorchDecisionModel 的导出：flattenable → weights.json + .bytes + onnx；
    非 flattenable（attention）→ 仅 onnx。

    返回产物路径列表。
    """
    export_dir.mkdir(parents=True, exist_ok=True)
    params = model._params
    input_dim = params["input_dim"]
    out = []

    export_cfg = cfg.get("export", {}) if hasattr(cfg, "get") else {}
    feature_names = load_feature_names(processed_dir or _processed_dir(cfg))

    if model.flattenable:
        linear_layers = extract_linear_layers(model.net)

        payload = {
            "model": model.name,
            "input_dim": input_dim,
            "num_actions": params["num_actions"],
            "activation": params.get("activation", "relu"),
            "layers": linear_layers,
            "action_names": ACTIONS,
            "feature_names": feature_names,
        }
        weights_path = export_dir / f"{model.name}_weights.json"
        weights_path.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
        out.append(weights_path)
        print(f"[export] {model.name} 权重 -> {weights_path}")

        binary_path = export_dir / f"{model.name}_weights.bytes"
        write_binary_bytes(binary_path, linear_layers)
        out.append(binary_path)
        print(f"[export] Unity 二进制权重 -> {binary_path}")

    if export_cfg.get("onnx", True):
        onnx_path = export_dir / f"{model.name}.onnx"
        export_onnx(model.net, input_dim, onnx_path)
        out.append(onnx_path)
        print(f"[export] {model.name} ONNX -> {onnx_path}")

    return out
