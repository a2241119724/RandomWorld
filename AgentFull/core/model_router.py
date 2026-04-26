from __future__ import annotations

from pathlib import Path
from typing import Any

from .file_utils import load_yaml
from .model_client import ModelClient


class ModelRouter:
    def __init__(
        self,
        config_path: Path,
        model_name: str | None = None,
        mock: bool = False,
        logger: Any | None = None,
        logging_config: dict[str, Any] | None = None,
    ) -> None:
        self.config_path = config_path
        self.config = load_yaml(config_path)
        self.model_name = model_name
        self.mock = mock
        self.logger = logger
        self.logging_config = logging_config or {}
        self.client = ModelClient(
            self.config,
            model_name=model_name,
            mock=mock,
            logger=logger,
            llm_log_dir=config_path.parent.parent / "cache" / "llm_calls",
            logging_config=self.logging_config,
        )

    def chat_for_task(
        self,
        task_type: str,
        messages: list[dict[str, str]],
    ) -> dict[str, Any]:
        # Routing is intentionally simple in v1; task-specific routing can be
        # added by mapping task_type to a model profile here.
        if self.logger:
            self.logger.info(
                "Model route selected | task_type=%s model_profile=%s",
                task_type,
                self.model_name or self.config.get("models", {}).get("default"),
            )
        return self.client.chat(messages, self.model_name, task_type=task_type)
