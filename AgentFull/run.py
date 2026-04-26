from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

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


TASK_TYPES = [
    "auto_discover_and_implement",
    "scan_project",
    "analyze_scripts",
    "generate_feature",
    "user_request",
    "fix_bug",
    "implement_feature",
]


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="AgentFull - Unity 自动开发 Agent 框架"
    )
    parser.add_argument(
        "--task",
        default=None,
        choices=TASK_TYPES,
        help="要执行的一次性任务类型；不传时进入交互模式。",
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
        "--interactive",
        action="store_true",
        help="执行完 --task 后继续停留在交互模式。",
    )
    parser.add_argument(
        "--quiet",
        action="store_true",
        help="不打印主 Agent/子 Agent/技能调用进度，只输出最终结果。",
    )
    return parser


def load_task_file(path: str | None) -> dict[str, Any]:
    if not path:
        return {}
    task_path = Path(path).expanduser().resolve()
    with task_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def default_task_payload(task_type: str) -> dict[str, Any]:
    return {
        "task": task_type,
        "description": (
            "扫描 Unity 项目，自动识别适合当前项目的新功能，生成实现代码，"
            "执行静态验证，并写出运行报告。"
        ),
        "constraints": [
            "不要覆盖已有 Unity 文件。",
            "优先生成可手动挂载或接入的独立运行时功能脚本。",
            "生成代码写入配置中的 Unity Scripts 或 Editor 目录。",
            "生成的 C# 代码注释使用中文。",
        ],
    }


def make_agent(args: argparse.Namespace) -> MainAgent:
    return MainAgent(
        base_dir=BASE_DIR,
        model_name=args.model,
        mock=args.mock,
        project_root=args.project_root,
        output=args.output,
        verbose=not args.quiet,
    )


def run_payload(agent: MainAgent, payload: dict[str, Any]) -> dict[str, Any]:
    result = agent.run_task(payload)
    print("AgentFull 任务完成。")
    print(f"任务 ID: {result.get('task_id')}")
    print(f"报告: {result.get('report_path')}")
    print(f"日志: {result.get('log_path')}")
    print(json.dumps(result.get("summary", {}), ensure_ascii=False, indent=2))
    return result


def interactive_loop(agent: MainAgent) -> None:
    print("AgentFull 交互模式。输入任务、bug 或新功能描述即可运行；输入 /help 查看命令，/exit 退出。")
    while True:
        try:
            user_input = input("agentfull> ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break

        if not user_input:
            continue
        if user_input.lower() in {"/exit", "exit", "quit", "q", "退出"}:
            break
        if user_input.lower() in {"/help", "help", "帮助"}:
            print_help()
            continue

        payload = payload_from_interactive_input(user_input)
        run_payload(agent, payload)


def payload_from_interactive_input(user_input: str) -> dict[str, Any]:
    lowered = user_input.strip().lower()
    command_map = {
        "/auto": "auto_discover_and_implement",
        "auto": "auto_discover_and_implement",
        "/scan": "scan_project",
        "scan": "scan_project",
        "扫描": "scan_project",
        "/analyze": "analyze_scripts",
        "analyze": "analyze_scripts",
        "分析": "analyze_scripts",
        "/feature": "generate_feature",
        "feature": "generate_feature",
        "新功能": "implement_feature",
        "/bug": "fix_bug",
        "bug": "fix_bug",
        "修复": "fix_bug",
    }
    if lowered in command_map:
        task_type = command_map[lowered]
        payload = default_task_payload(task_type)
        payload["user_request"] = user_input
        return payload

    task_type = classify_freeform_task(user_input)
    payload = default_task_payload(task_type)
    payload["user_request"] = user_input
    payload["description"] = (
        "用户在交互模式输入了以下需求，请结合完整项目上下文、会话历史和最近模型调用处理：\n"
        f"{user_input}"
    )
    payload["parameters"] = {
        "user_input": user_input,
        "interactive": True,
        "intent": task_type,
    }
    return payload


def classify_freeform_task(user_input: str) -> str:
    lowered = user_input.lower()
    bug_terms = ["bug", "报错", "错误", "异常", "修复", "fix", "crash", "崩溃", "不生效"]
    feature_terms = ["新功能", "功能", "实现", "增加", "添加", "feature", "implement", "add"]
    scan_terms = ["扫描项目", "项目扫描", "scan project"]
    analyze_terms = ["分析脚本", "分析代码", "analyze scripts"]

    if any(term in lowered for term in scan_terms):
        return "scan_project"
    if any(term in lowered for term in analyze_terms):
        return "analyze_scripts"
    if any(term in lowered for term in bug_terms):
        return "fix_bug"
    if any(term in lowered for term in feature_terms):
        return "implement_feature"
    return "user_request"


def print_help() -> None:
    print(
        "\n".join(
            [
                "可用命令：",
                "  /auto      自动发现并实现一个适合项目的新功能",
                "  /scan      扫描 Unity 项目并做只读引用检查",
                "  /analyze   扫描并分析 C# 脚本",
                "  /feature   只生成候选功能和任务卡，不标记完成",
                "  /bug       按 bug/修复意图运行，输入后也可以直接跟具体问题",
                "  /exit      退出",
                "也可以直接输入自然语言，例如：给工人系统增加士气恢复功能。",
            ]
        )
    )


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    agent = make_agent(args)

    if args.task:
        task_payload = default_task_payload(args.task)
        task_payload.update(load_task_file(args.task_file))
        task_payload.setdefault("task", args.task)
        run_payload(agent, task_payload)
        if args.interactive:
            interactive_loop(agent)
        return 0

    if args.task_file:
        task_payload = load_task_file(args.task_file)
        task_payload.setdefault("task", "auto_discover_and_implement")
        run_payload(agent, task_payload)
        if args.interactive:
            interactive_loop(agent)
        return 0

    interactive_loop(agent)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
