from __future__ import annotations

import logging
from pathlib import Path


def get_logger(
    name: str = "AgentFull",
    log_dir: Path | None = None,
    level: str = "INFO",
    console: bool = True,
) -> logging.Logger:
    logger = logging.getLogger(name)
    logger.setLevel(getattr(logging, level.upper(), logging.INFO))
    logger.propagate = False

    if console and not any(
        isinstance(handler, logging.StreamHandler) and not isinstance(handler, logging.FileHandler)
        for handler in logger.handlers
    ):
        stream_handler = logging.StreamHandler()
        stream_handler.setFormatter(_formatter())
        logger.addHandler(stream_handler)

    if log_dir:
        log_dir.mkdir(parents=True, exist_ok=True)
        log_path = log_dir / "agentfull.log"
        if not any(
            isinstance(handler, logging.FileHandler)
            and Path(handler.baseFilename) == log_path
            for handler in logger.handlers
        ):
            file_handler = logging.FileHandler(log_path, encoding="utf-8")
            file_handler.setFormatter(_formatter())
            logger.addHandler(file_handler)

    return logger


def _formatter() -> logging.Formatter:
    return logging.Formatter(
        "%(asctime)s | %(levelname)s | %(name)s | %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )
