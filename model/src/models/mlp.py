"""MLP 策略网络：状态特征向量 → 14 种行为的 logits。

前向：input_dim → Linear → 激活 → Dropout → ... → Linear → num_actions
训练用交叉熵（分类），推理取 argmax 或 softmax 概率。
"""
from __future__ import annotations

import torch
import torch.nn as nn


class WorkerMLP(nn.Module):
    def __init__(
        self,
        input_dim: int,
        num_actions: int,
        hidden_dims: list[int] | tuple[int, ...] = (128, 64),
        dropout: float = 0.1,
        activation: str = "relu",
    ):
        super().__init__()
        act = nn.ReLU() if activation == "relu" else nn.Tanh()

        layers: list[nn.Module] = []
        prev = input_dim
        for h in hidden_dims:
            layers.append(nn.Linear(prev, h))
            layers.append(act)
            if dropout > 0:
                layers.append(nn.Dropout(dropout))
            prev = h
        layers.append(nn.Linear(prev, num_actions))
        self.net = nn.Sequential(*layers)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        return self.net(x)
