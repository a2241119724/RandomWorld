"""导出入口：按注册表遍历，把已训练模型导出为 Unity 可消费产物。

用法（在 model/ 目录下）：
    python src/export.py

每个模型实现自己的 ``export``（unity_export 提供共享助手），本文件只负责
遍历 registry + 加载产物 + 调用，新增模型无需改动。
"""
from __future__ import annotations

import sys
from pathlib import Path

# Windows 控制台默认 GBK 编码，会撞上 torch.onnx 新导出器打印的 emoji（✅ 等）
# 导致 UnicodeEncodeError。强制 stdout 用 UTF-8 输出，保证导出在中文 Windows 下不崩。
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.config import ModelConfig  # noqa: E402
from src.models import list_models, load_model  # noqa: E402


def main():
    cfg = ModelConfig()
    export_dir = cfg.export_dir
    export_dir.mkdir(parents=True, exist_ok=True)

    for name in list_models():
        model = load_model(name, export_dir, cfg.raw)
        if model is None:
            print(f"[export] 未找到 {name} 产物，跳过")
            continue
        print(f"[export] 导出 {name} ...")
        paths = model.export(export_dir, cfg.raw)
        for p in paths:
            print(f"[export]   -> {p}")


if __name__ == "__main__":
    main()
