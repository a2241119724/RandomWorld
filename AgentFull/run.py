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
        description="AgentFull - Unity 自动开发 Agent 框架"
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
        help="要执行的任务类型。",
    )
    parser.add_argument(
        "--model",
        default=None,
        help="config/model_config.yaml 中的模型配置名，例如 openai 或 deepseek。",
    )
    parser.add_argument(
        "--mock",
        action="store_true",
        help="使用本地 mock 响应，不调用外部 AI API。",
    )
    parser.add_argument(
        "--project-root",
        default=None,
        help="覆盖 Unity 项目根目录路径。",
    )
    parser.add_argument(
        "--output",
        default=None,
        help="覆盖报告输出目录。",
    )
    parser.add_argument(
        "--task-file",
        default=None,
        help="可选 JSON 任务文件，内容会与命令行参数合并。",
    )
    parser.add_argument(
        "--quiet",
        action="store_true",
        help="不打印主 Agent/子 Agent/技能调用进度，只输出最终结果。",
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
        "扫描 Unity 项目，自动识别适合当前项目的新功能，生成实现代码，"
        "执行静态验证，并写出运行报告。",
    )
    task_payload.setdefault(
        "constraints",
        [
            "不要覆盖已有 Unity 文件。",
            "优先生成可手动挂载或接入的独立运行时功能脚本。",
            "生成代码写入配置中的 Unity Scripts 或 Editor 目录。",
        ],
    )

    agent = MainAgent(
        base_dir=BASE_DIR,
        model_name=args.model,
        mock=args.mock,
        project_root=args.project_root,
        output=args.output,
        verbose=not args.quiet,
    )
    result = agent.run_task(task_payload)

    print("AgentFull 任务完成。")
    print(f"任务 ID: {result.get('task_id')}")
    print(f"报告: {result.get('report_path')}")
    print(f"日志: {result.get('log_path')}")
    print(json.dumps(result.get("summary", {}), ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
