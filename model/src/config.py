"""模型配置封装：集中读取 model_config.yaml，提供类型化门面与路径助手。

不做 schema 校验（保持轻量）；所有入口统一经此访问，替代各处的
``yaml.safe_load((ROOT / "config" / "model_config.yaml").read_text(...))`` 样板。
"""
from __future__ import annotations

from pathlib import Path

import yaml

PROJECT_ROOT = Path(__file__).resolve().parent.parent


class ModelConfig:
    """轻量配置门面：路径解析 + 原始 dict 访问。"""

    def __init__(self, path: str | Path = "config/model_config.yaml"):
        self._path = PROJECT_ROOT / path
        self._raw = yaml.safe_load(self._path.read_text(encoding="utf-8"))

    # ---- 原始 dict 访问 ----
    @property
    def raw(self) -> dict:
        return self._raw

    def __getitem__(self, key):
        return self._raw[key]

    def get(self, key, default=None):
        return self._raw.get(key, default)

    # ---- 路径助手 ----
    @property
    def schema_path(self) -> Path:
        return PROJECT_ROOT / self._raw["paths"]["schema"]

    @property
    def processed_dir(self) -> Path:
        return PROJECT_ROOT / self._raw["paths"]["processed_dir"]

    @property
    def raw_dir(self) -> Path:
        return PROJECT_ROOT / self._raw["paths"]["raw_dir"]

    @property
    def export_dir(self) -> Path:
        return PROJECT_ROOT / self._raw["paths"]["export_dir"]

    # ---- 数据参数 ----
    @property
    def data_seed(self) -> int:
        return self._raw["data"]["seed"]
