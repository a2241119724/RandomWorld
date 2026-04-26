from __future__ import annotations

import os
from typing import Any

import requests


class ModelClient:
    def __init__(
        self,
        config: dict[str, Any],
        model_name: str | None = None,
        mock: bool = False,
        logger: Any | None = None,
    ) -> None:
        self.config = config
        self.model_name = model_name
        self.mock = mock
        self.logger = logger

    def chat(
        self,
        messages: list[dict[str, str]],
        model_name: str | None = None,
    ) -> dict[str, Any]:
        selected_name, model_config = self._select_model(model_name)
        if self.mock:
            return self._mock_reply(messages, selected_name, "mock mode enabled")

        provider = model_config.get("provider")
        try:
            if provider in {"openai", "openai_compatible"}:
                return self._chat_openai_compatible(messages, selected_name, model_config)
            if provider == "anthropic":
                return self._chat_anthropic(messages, selected_name, model_config)
            if provider == "ollama":
                return self._chat_ollama(messages, selected_name, model_config)
            return self._mock_reply(
                messages,
                selected_name,
                f"unsupported provider '{provider}'",
            )
        except Exception as exc:  # Network/API problems fall back to local mock.
            if self.logger:
                self.logger.warning("Model call failed; using mock fallback: %s", exc)
            return self._mock_reply(messages, selected_name, str(exc))

    def _select_model(self, model_name: str | None) -> tuple[str, dict[str, Any]]:
        models = self.config.get("models", {})
        selected = model_name or self.model_name or models.get("default")
        if selected not in models:
            available = ", ".join(key for key in models if key != "default")
            raise KeyError(f"Model profile '{selected}' not found. Available: {available}")
        return selected, models[selected]

    def _api_key(self, model_config: dict[str, Any]) -> str | None:
        env_name = model_config.get("api_key_env")
        if not env_name:
            return None
        return os.getenv(env_name)

    def _chat_openai_compatible(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
    ) -> dict[str, Any]:
        api_key = self._api_key(model_config)
        if not api_key:
            return self._mock_reply(
                messages,
                selected_name,
                f"missing API key env {model_config.get('api_key_env')}",
            )
        endpoint = model_config["base_url"].rstrip("/") + "/chat/completions"
        response = requests.post(
            endpoint,
            headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
            json={
                "model": model_config["model"],
                "messages": messages,
                "temperature": model_config.get("temperature", 0.2),
            },
            timeout=60,
        )
        response.raise_for_status()
        payload = response.json()
        content = payload["choices"][0]["message"]["content"]
        return {
            "model_profile": selected_name,
            "provider": model_config.get("provider"),
            "model": model_config.get("model"),
            "mock": False,
            "content": content,
        }

    def _chat_anthropic(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
    ) -> dict[str, Any]:
        api_key = self._api_key(model_config)
        if not api_key:
            return self._mock_reply(
                messages,
                selected_name,
                f"missing API key env {model_config.get('api_key_env')}",
            )
        system = "\n".join(item["content"] for item in messages if item.get("role") == "system")
        anth_messages = [
            {"role": item.get("role", "user"), "content": item.get("content", "")}
            for item in messages
            if item.get("role") != "system"
        ]
        endpoint = model_config["base_url"].rstrip("/") + "/v1/messages"
        response = requests.post(
            endpoint,
            headers={
                "x-api-key": api_key,
                "anthropic-version": "2023-06-01",
                "Content-Type": "application/json",
            },
            json={
                "model": model_config["model"],
                "system": system,
                "messages": anth_messages,
                "temperature": model_config.get("temperature", 0.2),
                "max_tokens": 2048,
            },
            timeout=60,
        )
        response.raise_for_status()
        payload = response.json()
        content = "\n".join(block.get("text", "") for block in payload.get("content", []))
        return {
            "model_profile": selected_name,
            "provider": "anthropic",
            "model": model_config.get("model"),
            "mock": False,
            "content": content,
        }

    def _chat_ollama(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
    ) -> dict[str, Any]:
        endpoint = model_config["base_url"].rstrip("/") + "/api/chat"
        response = requests.post(
            endpoint,
            json={
                "model": model_config["model"],
                "messages": messages,
                "stream": False,
                "options": {"temperature": model_config.get("temperature", 0.2)},
            },
            timeout=120,
        )
        response.raise_for_status()
        payload = response.json()
        return {
            "model_profile": selected_name,
            "provider": "ollama",
            "model": model_config.get("model"),
            "mock": False,
            "content": payload.get("message", {}).get("content", ""),
        }

    def _mock_reply(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        reason: str,
    ) -> dict[str, Any]:
        last_user = next(
            (item.get("content", "") for item in reversed(messages) if item.get("role") == "user"),
            "",
        )
        return {
            "model_profile": selected_name,
            "provider": "mock",
            "model": "local-mock",
            "mock": True,
            "fallback_reason": reason,
            "content": (
                "Mock response: local deterministic mode is active. "
                f"Last user request summary: {last_user[:240]}"
            ),
        }
