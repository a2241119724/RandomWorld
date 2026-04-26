from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent
if str(BASE_DIR) not in sys.path:
    sys.path.insert(0, str(BASE_DIR))

try:
    from dotenv import load_dotenv

    load_dotenv(BASE_DIR / ".env")
    load_dotenv()
except ImportError:
    pass

from core.main_agent import MainAgent


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="AgentFull - Unity automation agent framework"
    )
    parser.add_argument(
        "--task",
        default="auto_discover_and_implement",
        choices=[
            "auto_discover_and_implement",
            "scan_project",
            "analyze_scripts",
            "generate_feature",
        ],
        help="Task type to execute.",
    )
    parser.add_argument(
        "--model",
        default=None,
        help="Model profile name from config/model_config.yaml, e.g. openai or deepseek.",
    )
    parser.add_argument(
        "--mock",
        action="store_true",
        help="Use local mock model responses and never call external AI APIs.",
    )
    parser.add_argument(
        "--project-root",
        default=None,
        help="Override the Unity project root path.",
    )
    parser.add_argument(
        "--output",
        default=None,
        help="Override report output directory.",
    )
    parser.add_argument(
        "--task-file",
        default=None,
        help="Optional JSON task file. Values are merged with CLI arguments.",
    )
    return parser


def load_task_file(path: str | None) -> dict:
    if not path:
        return {}
    task_path = Path(path).expanduser().resolve()
    with task_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    task_payload = load_task_file(args.task_file)
    task_payload.setdefault("task", args.task)
    task_payload.setdefault(
        "description",
        "Scan the Unity project, plan a low-risk automation feature, generate code, "
        "validate it, and write a report.",
    )
    task_payload.setdefault(
        "constraints",
        [
            "Do not overwrite existing Unity files.",
            "Prefer readonly Editor tooling.",
            "Write generated code to the configured Unity script or Editor folder.",
        ],
    )

    agent = MainAgent(
        base_dir=BASE_DIR,
        model_name=args.model,
        mock=args.mock,
        project_root=args.project_root,
        output=args.output,
    )
    result = agent.run_task(task_payload)

    print("AgentFull task finished.")
    print(f"Task ID: {result.get('task_id')}")
    print(f"Report: {result.get('report_path')}")
    print(json.dumps(result.get("summary", {}), ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
