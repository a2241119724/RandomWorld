from __future__ import annotations

import json
import os
import time
import uuid
from pathlib import Path
from typing import Any

import requests


class ModelClient:
    def __init__(
        self,
        config: dict[str, Any],
        model_name: str | None = None,
        mock: bool = False,
        logger: Any | None = None,
        llm_log_dir: Path | None = None,
        logging_config: dict[str, Any] | None = None,
    ) -> None:
        self.config = config
        self.model_name = model_name
        self.mock = mock
        self.logger = logger
        self.logging_config = logging_config or {}
        self.llm_log_dir = (
            Path(llm_log_dir)
            if self.logging_config.get("write_llm_call_files", True) and llm_log_dir
            else None
        )
        self.payload_log_chars = int(self.logging_config.get("llm_payload_max_chars", 8000))
        self.log_llm_payloads = bool(self.logging_config.get("log_llm_payloads", True))
        self._call_artifacts: dict[str, dict[str, str]] = {}

    def chat(
        self,
        messages: list[dict[str, str]],
        model_name: str | None = None,
        task_type: str | None = None,
    ) -> dict[str, Any]:
        selected_name, model_config = self._select_model(model_name)
        provider = model_config.get("provider")
        call_id = uuid.uuid4().hex[:12]
        started = time.perf_counter()
        self._log_call_start(call_id, task_type, messages, selected_name, model_config)
        if self.mock:
            self._write_call_artifact(
                call_id,
                "request",
                {
                    "mode": "mock",
                    "task_type": task_type,
                    "model_profile": selected_name,
                    "provider": provider,
                    "model": model_config.get("model"),
                    "messages": messages,
                },
            )
            result = self._mock_reply(messages, selected_name, "mock mode enabled")
            result["call_id"] = call_id
            self._write_call_artifact(call_id, "response", result)
            self._attach_call_metadata(result, call_id, messages)
            self._log_call_result(call_id, result, started, "mock")
            return result

        try:
            if provider in {"openai", "openai_compatible"}:
                result = self._chat_openai_compatible(
                    messages,
                    selected_name,
                    model_config,
                    call_id,
                )
                result.setdefault("call_id", call_id)
                self._log_call_result(
                    call_id,
                    result,
                    started,
                    "fallback" if result.get("mock") else "success",
                )
                self._attach_call_metadata(result, call_id, messages)
                return result
            if provider == "anthropic":
                result = self._chat_anthropic(messages, selected_name, model_config, call_id)
                result.setdefault("call_id", call_id)
                self._log_call_result(
                    call_id,
                    result,
                    started,
                    "fallback" if result.get("mock") else "success",
                )
                self._attach_call_metadata(result, call_id, messages)
                return result
            if provider == "ollama":
                result = self._chat_ollama(messages, selected_name, model_config, call_id)
                result.setdefault("call_id", call_id)
                self._log_call_result(
                    call_id,
                    result,
                    started,
                    "fallback" if result.get("mock") else "success",
                )
                self._attach_call_metadata(result, call_id, messages)
                return result
            result = self._mock_reply(
                messages,
                selected_name,
                f"unsupported provider '{provider}'",
            )
            result["call_id"] = call_id
            self._write_call_artifact(
                call_id,
                "request",
                {
                    "task_type": task_type,
                    "model_profile": selected_name,
                    "provider": provider,
                    "model": model_config.get("model"),
                    "messages": messages,
                },
            )
            self._write_call_artifact(call_id, "response", result)
            self._attach_call_metadata(result, call_id, messages)
            self._log_call_result(call_id, result, started, "fallback")
            return result
        except Exception as exc:  # Network/API problems fall back to local mock.
            if self.logger:
                self.logger.warning(
                    "LLM call failed; using mock fallback | call_id=%s duration_ms=%s error=%s",
                    call_id,
                    int((time.perf_counter() - started) * 1000),
                    self._safe_text(str(exc), 500),
                )
            self._write_call_artifact(
                call_id,
                "error",
                {
                    "task_type": task_type,
                    "model_profile": selected_name,
                    "provider": provider,
                    "model": model_config.get("model"),
                    "error": str(exc),
                },
            )
            result = self._mock_reply(messages, selected_name, self._safe_text(str(exc), 500))
            result["call_id"] = call_id
            self._write_call_artifact(call_id, "response", result)
            self._attach_call_metadata(result, call_id, messages)
            self._log_call_result(call_id, result, started, "fallback")
            return result

    def _select_model(self, model_name: str | None) -> tuple[str, dict[str, Any]]:
        models = self.config.get("models", {})
        selected = model_name or self.model_name or models.get("default")
        if selected not in models:
            available = ", ".join(key for key in models if key != "default")
            raise KeyError(f"Model profile '{selected}' not found. Available: {available}")
        return selected, models[selected]

    def _api_key(self, model_config: dict[str, Any]) -> str | None:
        direct_key = model_config.get("api_key")
        if direct_key:
            return str(direct_key)
        env_name = model_config.get("api_key_env")
        if not env_name:
            return None
        env_value = os.getenv(str(env_name))
        if env_value:
            return env_value
        if self._looks_like_literal_key(env_name):
            return str(env_name)
        return None

    def _chat_openai_compatible(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
        call_id: str,
    ) -> dict[str, Any]:
        api_key = self._api_key(model_config)
        if not api_key:
            return self._mock_reply(
                messages,
                selected_name,
                f"missing API key env {self._safe_key_label(model_config.get('api_key_env'))}",
            )
        endpoint = model_config["base_url"].rstrip("/") + "/chat/completions"
        payload = {
            "model": model_config["model"],
            "messages": messages,
            "temperature": model_config.get("temperature", 0.2),
        }
        if model_config.get("reasoning_effort"):
            payload["reasoning_effort"] = model_config.get("reasoning_effort")
        if isinstance(model_config.get("extra_body"), dict):
            payload.update(model_config["extra_body"])
        if model_config.get("max_tokens"):
            payload["max_tokens"] = model_config.get("max_tokens")
        response = self._post_json(
            call_id,
            endpoint,
            headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
            payload=payload,
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
            "usage": payload.get("usage", {}),
            "call_id": call_id,
        }

    def _chat_anthropic(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
        call_id: str,
    ) -> dict[str, Any]:
        api_key = self._api_key(model_config)
        if not api_key:
            return self._mock_reply(
                messages,
                selected_name,
                f"missing API key env {self._safe_key_label(model_config.get('api_key_env'))}",
            )
        system = "\n".join(item["content"] for item in messages if item.get("role") == "system")
        anth_messages = [
            {"role": item.get("role", "user"), "content": item.get("content", "")}
            for item in messages
            if item.get("role") != "system"
        ]
        endpoint = model_config["base_url"].rstrip("/") + "/v1/messages"
        payload = {
            "model": model_config["model"],
            "system": system,
            "messages": anth_messages,
            "temperature": model_config.get("temperature", 0.2),
            "max_tokens": 2048,
        }
        response = self._post_json(
            call_id,
            endpoint,
            headers={
                "x-api-key": api_key,
                "anthropic-version": "2023-06-01",
                "Content-Type": "application/json",
            },
            payload=payload,
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
            "usage": payload.get("usage", {}),
            "call_id": call_id,
        }

    def _chat_ollama(
        self,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
        call_id: str,
    ) -> dict[str, Any]:
        endpoint = model_config["base_url"].rstrip("/") + "/api/chat"
        payload = {
            "model": model_config["model"],
            "messages": messages,
            "stream": False,
            "options": {"temperature": model_config.get("temperature", 0.2)},
        }
        response = self._post_json(
            call_id,
            endpoint,
            headers=None,
            payload=payload,
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
            "usage": {
                key: payload.get(key)
                for key in ["prompt_eval_count", "eval_count", "total_duration"]
                if key in payload
            },
            "call_id": call_id,
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

    def _post_json(
        self,
        call_id: str,
        endpoint: str,
        headers: dict[str, str] | None,
        payload: dict[str, Any],
        timeout: int,
    ) -> requests.Response:
        if self.logger:
            self.logger.info(
                "LLM HTTP request | call_id=%s endpoint=%s timeout=%s request_log=%s payload=%s",
                call_id,
                endpoint,
                timeout,
                self._write_call_artifact(
                    call_id,
                    "request",
                    {
                        "endpoint": endpoint,
                        "timeout": timeout,
                        "payload": payload,
                    },
                ),
                self._safe_json(self._summarize_payload(payload), self.payload_log_chars)
                if self.log_llm_payloads
                else "<payload logging disabled>",
            )
        else:
            self._write_call_artifact(
                call_id,
                "request",
                {
                    "endpoint": endpoint,
                    "timeout": timeout,
                    "payload": payload,
                },
            )
        started = time.perf_counter()
        response = requests.post(
            endpoint,
            headers=headers,
            json=payload,
            timeout=timeout,
        )
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        if self.logger:
            self.logger.info(
                "LLM HTTP response | call_id=%s status_code=%s duration_ms=%s response_log=%s body=%s",
                call_id,
                response.status_code,
                elapsed_ms,
                self._write_call_artifact(
                    call_id,
                    "response",
                    {
                        "status_code": response.status_code,
                        "duration_ms": elapsed_ms,
                        "body": response.text,
                    },
                ),
                self._safe_text(response.text, self.payload_log_chars)
                if self.log_llm_payloads
                else "<payload logging disabled>",
            )
        else:
            self._write_call_artifact(
                call_id,
                "response",
                {
                    "status_code": response.status_code,
                    "duration_ms": elapsed_ms,
                    "body": response.text,
                },
            )
        return response

    def _log_call_start(
        self,
        call_id: str,
        task_type: str | None,
        messages: list[dict[str, str]],
        selected_name: str,
        model_config: dict[str, Any],
    ) -> None:
        if not self.logger:
            return
        message_chars = sum(len(item.get("content", "")) for item in messages)
        self.logger.info(
            "LLM call started | call_id=%s task_type=%s profile=%s provider=%s model=%s mock=%s messages=%s chars=%s temperature=%s base_url=%s",
            call_id,
            task_type or "",
            selected_name,
            model_config.get("provider"),
            model_config.get("model"),
            self.mock,
            len(messages),
            message_chars,
            model_config.get("temperature", 0.2),
            model_config.get("base_url", ""),
        )

    def _log_call_result(
        self,
        call_id: str,
        result: dict[str, Any],
        started: float,
        status: str,
    ) -> None:
        if not self.logger:
            return
        self.logger.info(
            "LLM call finished | call_id=%s status=%s duration_ms=%s provider=%s model=%s mock=%s content_preview=%s usage=%s",
            call_id,
            status,
            int((time.perf_counter() - started) * 1000),
            result.get("provider"),
            result.get("model"),
            result.get("mock"),
            self._safe_text(result.get("content", ""), 1000),
            self._safe_json(result.get("usage", {}), 1000),
        )

    def _summarize_payload(self, payload: dict[str, Any]) -> dict[str, Any]:
        summary: dict[str, Any] = {}
        for key, value in payload.items():
            if key == "messages" and isinstance(value, list):
                summary[key] = self._summarize_messages(value)
            elif key == "system" and isinstance(value, str):
                summary[key] = {
                    "chars": len(value),
                    "preview": self._safe_text(value, 500),
                }
            else:
                summary[key] = value
        return summary

    def _summarize_messages(
        self,
        messages: list[dict[str, str]],
        preview_chars: int = 500,
    ) -> list[dict[str, Any]]:
        summarized = []
        for index, message in enumerate(messages):
            content = message.get("content", "")
            summarized.append(
                {
                    "index": index,
                    "role": message.get("role", ""),
                    "chars": len(content),
                    "preview": self._safe_text(content, preview_chars),
                }
            )
        return summarized

    def _safe_json(self, value: Any, max_chars: int) -> str:
        try:
            content = json.dumps(value, ensure_ascii=False, sort_keys=True)
        except TypeError:
            content = str(value)
        return self._safe_text(content, max_chars)

    def _safe_text(self, value: Any, max_chars: int) -> str:
        content = str(value).replace("\r", "\\r").replace("\n", "\\n")
        if len(content) <= max_chars:
            return content
        return content[:max_chars] + "...<truncated>"

    def _safe_key_label(self, value: Any) -> str:
        label = str(value or "")
        if not label:
            return "<not configured>"
        if self._looks_like_literal_key(label):
            return "<literal key redacted>"
        return label

    def _looks_like_literal_key(self, value: Any) -> bool:
        label = str(value or "").strip().lower()
        return label.startswith(("sk-", "sk_", "key-", "apikey-"))

    def _attach_call_metadata(
        self,
        result: dict[str, Any],
        call_id: str,
        messages: list[dict[str, str]],
    ) -> None:
        if not self._call_artifacts.get(call_id):
            self._write_call_artifact(call_id, "request", {"messages": messages})
            self._write_call_artifact(call_id, "response", result)
        result["request_preview"] = self._summarize_messages(messages, preview_chars=1200)
        result.update(self._call_artifacts.get(call_id, {}))

    def _write_call_artifact(self, call_id: str, kind: str, payload: dict[str, Any]) -> str:
        if not self.llm_log_dir:
            return ""
        self.llm_log_dir.mkdir(parents=True, exist_ok=True)
        path = self.llm_log_dir / f"{call_id}_{kind}.json"
        with path.open("w", encoding="utf-8") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
        artifact_key = f"{kind}_log_path"
        self._call_artifacts.setdefault(call_id, {})[artifact_key] = str(path)
        return str(path)
