"""PyTorch Dataset：封装特征矩阵与行为标签。"""
from __future__ import annotations

import numpy as np
import torch
from torch.utils.data import Dataset


class WorkerDecisionDataset(Dataset):
    def __init__(self, X: np.ndarray, y: np.ndarray):
        self.X = torch.as_tensor(X, dtype=torch.float32)
        self.y = torch.as_tensor(y, dtype=torch.long)

    def __len__(self) -> int:
        return len(self.y)

    def __getitem__(self, idx: int):
        return self.X[idx], self.y[idx]
