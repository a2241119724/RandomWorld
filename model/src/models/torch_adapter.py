"""Torch 模型适配器：把任意 nn.Module 决策网络统一为 DecisionModel 接口。

子类只需声明三个类属性，其余（fit/save/load/export/feature_importance/structure）
全部由适配器提供：
    net_cls      —— nn.Module 构造类（如 WorkerMLP / WorkerAttention）
    meta_keys    —— 需随 checkpoint 保存的超参 key（取自已命名 config 段）
    flattenable  —— 能否压平成 Linear 层列表（mlp=True；attention=False）
"""
from __future__ import annotations

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

from .base import DecisionModel
from ..training import train_torch


class TorchDecisionModel(DecisionModel):
    net_cls: type[nn.Module] = None
    meta_keys: tuple[str, ...] = ()
    flattenable: bool = False
    _infer_batch: int = 2048  # 推理分批上限：防 CPU 上 attention 大测试集 OOM

    # ---- 构造 ----
    @classmethod
    def _build_net(cls, params: dict) -> nn.Module:
        """由超参 dict（含 input_dim/num_actions）构造 nn.Module。"""
        kwargs = {k: params[k] for k in cls.meta_keys}
        return cls.net_cls(
            input_dim=params["input_dim"],
            num_actions=params["num_actions"],
            **kwargs,
        )

    @classmethod
    def from_config(cls, cfg, input_dim: int, num_actions: int,
                    seed: int | None = None):
        section = cfg[cls.section]
        params = {"input_dim": input_dim, "num_actions": num_actions,
                  **{k: section[k] for k in cls.meta_keys}}
        inst = cls()
        inst._cfg = cfg
        inst._params = params
        inst.net = cls._build_net(params)
        return inst

    # ---- 训练 ----
    def fit(self, X_tr, y_tr, X_va, y_va, X_te, y_te, cfg, seed: int) -> dict:
        metrics, best_state = train_torch(
            self.net, X_tr, y_tr, X_va, y_va, X_te, y_te, cfg, seed)
        self._best_state = best_state
        return metrics

    # ---- 推理 ----
    def _device(self):
        return next(self.net.parameters()).device

    @torch.no_grad()
    def _logits(self, X) -> torch.Tensor:
        dev = self._device()
        Xt = torch.as_tensor(X, dtype=torch.float32, device=dev)
        self.net.eval()
        if Xt.shape[0] <= self._infer_batch:
            return self.net(Xt)
        # 大批量分批前向：attention 在 2 万条测试集单次前向会一次分配 ~2.4GB（CPU OOM）
        chunks = [self.net(Xt[i:i + self._infer_batch])
                  for i in range(0, Xt.shape[0], self._infer_batch)]
        return torch.cat(chunks, dim=0)

    def predict_proba(self, X) -> np.ndarray:
        return F.softmax(self._logits(X), dim=1).cpu().numpy()

    def predict(self, X) -> np.ndarray:
        return self._logits(X).argmax(dim=1).cpu().numpy()

    # ---- 持久化 ----
    def _ckpt_meta(self) -> dict:
        return dict(self._params)

    def save(self, export_dir, cfg, seed: int):
        export_dir.mkdir(parents=True, exist_ok=True)
        state = getattr(self, "_best_state", None) or self.net.state_dict()
        payload = {
            "state_dict": {k: v.cpu() for k, v in state.items()},
            **self._ckpt_meta(),
        }
        path = export_dir / self.filename
        torch.save(payload, path)
        return path

    @classmethod
    def load(cls, export_dir, cfg=None, device="auto"):
        import torch as _torch
        path = export_dir / cls.filename
        if not path.exists():
            return None
        ckpt = _torch.load(path, map_location="cpu", weights_only=False)
        inst = cls()
        inst._params = {k: ckpt[k] for k in ("input_dim", "num_actions", *cls.meta_keys)}
        inst._ckpt = ckpt
        inst.net = cls._build_net(inst._params)
        inst.net.load_state_dict(ckpt["state_dict"])
        inst.net.eval()
        # 注意：默认 device="auto" 保持 CPU。ONNX 导出要求 CPU 模型，
        # 若 auto→cuda 会致 export.py 设备不匹配；大测试集 OOM 由 _logits 分批推理兜底。
        if device == "cuda" and torch.cuda.is_available():
            inst.net = inst.net.to("cuda")
        return inst

    # ---- 导出（Unity 侧）----
    def export(self, export_dir, cfg):
        from ..unity_export import export_torch_model
        return export_torch_model(self, export_dir, cfg)

    # ---- 可视化 ----
    def feature_importance(self) -> np.ndarray:
        if self.flattenable:
            # 第一层 Linear 权重列范数（输入特征对第一隐层的影响度）
            for module in self.net.children():
                if isinstance(module, nn.Linear):
                    return module.weight.detach().cpu().norm(dim=0).numpy()
            return np.zeros(self._params["input_dim"])
        # attention：逐特征 embedding 向量范数
        return self.net.feature_embed.detach().cpu().norm(dim=1).numpy()

    def structure(self) -> dict:
        p = self._params
        if self.flattenable:
            return {
                "kind": "mlp",
                "input": p["input_dim"],
                "hidden": list(p["hidden_dims"]),
                "output": p["num_actions"],
                "activation": p.get("activation", "relu"),
                "dropout": float(p.get("dropout", 0.0)),
            }
        return {
            "kind": "attention",
            "input": p["input_dim"],
            "d_model": p["d_model"],
            "n_heads": p["n_heads"],
            "n_layers": p["n_layers"],
            "dim_feedforward": p["dim_feedforward"],
            "head": list(p["head_dims"]),
            "output": p["num_actions"],
            "dropout": float(p.get("dropout", 0.0)),
        }
