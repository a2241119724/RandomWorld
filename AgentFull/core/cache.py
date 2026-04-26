from __future__ import annotations

import hashlib
from datetime import datetime
from pathlib import Path
from typing import Any, Callable

from .file_utils import ensure_dir, load_json, save_json


class FileCache:
    def __init__(self, cache_dir: Path, enabled: bool = True) -> None:
        self.cache_dir = ensure_dir(cache_dir)
        self.enabled = enabled

    def get(self, cache_name: str, fingerprint: str) -> Any | None:
        if not self.enabled:
            return None
        cache_path = self.cache_dir / cache_name
        payload = load_json(cache_path, {})
        if payload.get("fingerprint") == fingerprint:
            return payload.get("data")
        return None

    def set(self, cache_name: str, fingerprint: str, data: Any) -> None:
        if not self.enabled:
            return
        save_json(
            self.cache_dir / cache_name,
            {
                "fingerprint": fingerprint,
                "updated_at": datetime.now().isoformat(timespec="seconds"),
                "data": data,
            },
        )

    def get_or_compute(
        self,
        cache_name: str,
        fingerprint: str,
        compute: Callable[[], Any],
    ) -> Any:
        cached = self.get(cache_name, fingerprint)
        if cached is not None:
            return cached
        data = compute()
        self.set(cache_name, fingerprint, data)
        return data


def build_fingerprint(
    root: Path,
    patterns: list[str],
    excluded_dirs: set[str] | None = None,
) -> str:
    excluded = {item.lower() for item in (excluded_dirs or set())}
    parts: list[str] = []
    if not root.exists():
        return "missing"
    for pattern in patterns:
        for path in sorted(root.rglob(pattern)):
            if not path.is_file():
                continue
            lowered_parts = {part.lower() for part in path.parts}
            if lowered_parts.intersection(excluded):
                continue
            try:
                stat = path.stat()
            except OSError:
                continue
            rel = path.relative_to(root).as_posix()
            parts.append(f"{rel}:{stat.st_mtime_ns}:{stat.st_size}")
    digest = hashlib.sha256("\n".join(parts).encode("utf-8")).hexdigest()
    return digest
