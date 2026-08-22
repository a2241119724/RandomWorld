"""FT-Transformer 风格注意力模型：逐特征 token + [CLS] 自注意力 + MLP 分类头。

前向：
    x (B, input_dim)
    -> 逐特征 embedding：tokens = x.unsqueeze(-1) * feature_embed   # (B, input_dim, d_model)
    -> 拼接 [CLS] token                                            # (B, input_dim+1, d_model)
    -> n_layers × TransformerEncoderLayer（多头自注意力 + FFN，norm_first）
    -> 取 [CLS] 输出                                                # (B, d_model)
    -> MLP 分类头 -> num_actions logits

不加位置编码：特征是无序集合，顺序不应被学习到（FT-Transformer 的标准做法）。

注意：逐特征 embedding 用 nn.Parameter(input_dim, d_model) 实现「标量 -> 向量」，
不能用 nn.Linear(input_dim, d_model)——那会把单个 41 维向量映射成 42 个完全相同的 token。
"""
from __future__ import annotations

import torch
import torch.nn as nn

from .registry import register
from .torch_adapter import TorchDecisionModel


class WorkerAttention(nn.Module):
    def __init__(
        self,
        input_dim: int,
        num_actions: int,
        d_model: int = 64,
        n_heads: int = 4,
        n_layers: int = 2,
        dim_feedforward: int = 128,
        dropout: float = 0.1,
        head_dims: list[int] | tuple[int, ...] = (128,),
    ):
        super().__init__()
        if d_model % n_heads != 0:
            raise ValueError(f"d_model({d_model}) 必须能被 n_heads({n_heads}) 整除")

        # 逐特征 embedding：每个标量特征 -> d_model 维向量
        self.feature_embed = nn.Parameter(torch.randn(input_dim, d_model) * 0.02)
        # 可学习 [CLS] token，用于汇聚全局信息后交给分类头
        self.cls_token = nn.Parameter(torch.randn(1, 1, d_model) * 0.02)

        encoder_layer = nn.TransformerEncoderLayer(
            d_model=d_model,
            nhead=n_heads,
            dim_feedforward=dim_feedforward,
            dropout=dropout,
            activation="relu",
            batch_first=True,
            norm_first=True,
        )
        # enable_nested_tensor=False：norm_first=True 本就不支持 nested tensor，
        # 显式关闭以消除构造时的 UserWarning。
        self.encoder = nn.TransformerEncoder(
            encoder_layer, num_layers=n_layers, enable_nested_tensor=False
        )

        # MLP 分类头：d_model -> head_dims... -> num_actions
        head_layers: list[nn.Module] = []
        prev = d_model
        for h in head_dims:
            head_layers.append(nn.Linear(prev, h))
            head_layers.append(nn.ReLU())
            if dropout > 0:
                head_layers.append(nn.Dropout(dropout))
            prev = h
        head_layers.append(nn.Linear(prev, num_actions))
        self.head = nn.Sequential(*head_layers)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        # x: (B, input_dim)
        tokens = x.unsqueeze(-1) * self.feature_embed        # (B, input_dim, d_model)
        cls = self.cls_token.expand(x.size(0), -1, -1)        # (B, 1, d_model)
        tokens = torch.cat([cls, tokens], dim=1)              # (B, input_dim+1, d_model)
        out = self.encoder(tokens)                            # (B, input_dim+1, d_model)
        cls_out = out[:, 0]                                   # (B, d_model)
        return self.head(cls_out)                             # (B, num_actions)


@register("attention")
class AttentionModel(TorchDecisionModel):
    """注意力模型的 DecisionModel 适配（注册名 "attention"）。"""

    filename = "attention.pt"
    section = "attention"
    net_cls = WorkerAttention
    meta_keys = ("d_model", "n_heads", "n_layers", "dim_feedforward",
                 "dropout", "head_dims")
    flattenable = False
