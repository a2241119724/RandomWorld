#!/usr/bin/env python3
"""Seedance 视频→序列帧：火山方舟 Doubao-Seedance-1.0-pro-fast 生成短视频后 ffmpeg 抽帧。

流程：创建生成任务（i2v 首帧驱动 / t2v 纯文本）→ 轮询 → 下载 mp4 → ffmpeg 均匀抽帧 → contact sheet。
依赖：ARK_API_KEY（env 或项目根 .env）、ffmpeg（PATH 中可用）、Python 3.8+。

用法示例：
  python generate_frames.py --prompt "a chibi character running in place, loop" \
    --first-frame Resources/Images/character/player/idle.png \
    --output-dir Resources/Images/character/player/run/ --frames 8
"""

import argparse
import base64
import json
import mimetypes
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

API_BASE = "https://ark.cn-beijing.volces.com/api/v3"
MODEL = "doubao-seedance-1-0-pro-fast-251015"
# pro-fast 单秒近似价（元/秒，以官方账单为准）：480p ≈ 0.126
COST_PER_SECOND = {"480p": 0.126, "720p": 0.21, "1080p": 0.42}


def log(msg: str) -> None:
    print(msg, file=sys.stderr, flush=True)


def load_api_key() -> str:
    key = os.environ.get("ARK_API_KEY", "").strip()
    if key:
        return key
    env_file = Path.cwd() / ".env"
    if env_file.exists():
        for line in env_file.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line.startswith("ARK_API_KEY"):
                _, _, val = line.partition("=")
                val = val.strip().strip('"').strip("'")
                if val:
                    return val
    sys.exit("ERROR: ARK_API_KEY not set（环境变量或项目根 .env）")


def http_json(method: str, url: str, key: str, payload=None) -> dict:
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"Bearer {key}")
    req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        if e.code == 401:
            sys.exit("ERROR: 401 —— ARK_API_KEY 无效或格式错误（检查是否完整、无空白）")
        if "ModelNotOpen" in body or e.code == 404:
            sys.exit(f"ERROR: 模型服务未开通（{MODEL}），到 Ark 控制台开通后重试。原始返回：{body}")
        sys.exit(f"ERROR: HTTP {e.code} {body}")
    except urllib.error.URLError as e:
        sys.exit(f"ERROR: 网络请求失败：{e.reason}")


def image_to_data_url(path: str) -> str:
    p = Path(path)
    if not p.is_absolute():
        p = Path.cwd() / p
    if not p.exists():
        sys.exit(f"ERROR: 首帧图片不存在：{p}")
    mime = mimetypes.guess_type(str(p))[0] or "image/png"
    b64 = base64.b64encode(p.read_bytes()).decode("ascii")
    return f"data:{mime};base64,{b64}"


def create_task(args, key: str) -> str:
    # 序列帧关键参数固定锁镜头（--camerafixed true），否则镜头运动会毁掉帧间一致性
    text = (args.prompt +
            f" --resolution {args.resolution}"
            f" --ratio {args.ratio}"
            f" --duration {args.duration}"
            " --camerafixed true"
            " --watermark false")
    if args.seed is not None:
        text += f" --seed {args.seed}"
    content = [{"type": "text", "text": text}]
    if args.first_frame:
        content.append({
            "type": "image_url",
            "image_url": {"url": image_to_data_url(args.first_frame), "role": "first_frame"},
        })
    body = {"model": MODEL, "content": content}
    resp = http_json("POST", f"{API_BASE}/contents/generations/tasks", key, body)
    task_id = resp.get("id")
    if not task_id:
        sys.exit(f"ERROR: 创建任务无 id 返回：{json.dumps(resp, ensure_ascii=False)}")
    log(f"[Seedance] 任务已创建：{task_id}")
    return task_id


def poll_task(key: str, task_id: str, timeout: int) -> dict:
    t0 = time.time()
    last = None
    while time.time() - t0 < timeout:
        resp = http_json("GET", f"{API_BASE}/contents/generations/tasks/{task_id}", key)
        status = resp.get("status")
        if status != last:
            log(f"[Seedance] 状态：{status}（{int(time.time() - t0)}s）")
            last = status
        if status == "succeeded":
            return resp
        if status in ("failed", "cancelled"):
            sys.exit(f"ERROR: 任务 {status}：{json.dumps(resp, ensure_ascii=False)}")
        time.sleep(3)
    sys.exit(f"ERROR: 轮询超时（{timeout}s），任务 {task_id} 可稍后手动查询")


def download(url: str, dst: Path) -> None:
    log(f"[Seedance] 下载视频 → {dst}")
    urllib.request.urlretrieve(url, dst)


def check_ffmpeg() -> None:
    try:
        subprocess.run(["ffmpeg", "-version"], capture_output=True, check=True)
    except (FileNotFoundError, subprocess.CalledProcessError):
        sys.exit("ERROR: ffmpeg 不可用。安装：winget install Gyan.FFmpeg（需重启终端生效）")


def extract_frames(video: Path, out_dir: Path, prefix: str, count: int, duration: float) -> list:
    src_fps = 24  # Seedance 固定 24fps
    want_fps = count / duration
    if want_fps > src_fps:
        log(f"[Seedance] 警告：{count} 帧超过 {duration}s×24fps 上限，改为全抽")
        want_fps = float(src_fps)
    pattern = str(out_dir / f"{prefix}_%d.png")
    cmd = [
        "ffmpeg", "-y", "-i", str(video),
        "-vf", f"fps={want_fps:.4f}",
        "-frames:v", str(count),
        "-start_number", "0",
        pattern,
    ]
    subprocess.run(cmd, capture_output=True, check=True)
    frames = sorted(out_dir.glob(f"{prefix}_*.png"))
    # 排除 contact sheet 等非帧文件（无数字后缀不匹配 %d 模式，glob 需过滤）
    frames = [f for f in frames if f.stem[len(prefix) + 1:].isdigit()]
    if not frames:
        sys.exit("ERROR: 抽帧结果为空，检查 ffmpeg 输出（可手动重跑命令排查）")
    return frames


def make_contact_sheet(frames: list, out_dir: Path, prefix: str) -> Path:
    n = len(frames)
    cols = min(4, n)
    rows = (n + cols - 1) // cols
    numbered = out_dir / f"{prefix}_%d.png"
    sheet = out_dir / f"{prefix}_contact.png"
    cmd = [
        "ffmpeg", "-y",
        "-framerate", "1", "-start_number", "0", "-i", str(numbered),
        "-vf", f"tile={cols}x{rows}",
        "-frames:v", "1",
        str(sheet),
    ]
    subprocess.run(cmd, capture_output=True)
    return sheet if sheet.exists() else None


def main() -> None:
    ap = argparse.ArgumentParser(description="Seedance 视频→序列帧（pro-fast 最便宜档）")
    ap.add_argument("--prompt", required=True, help="动作描述（英文建议，循环动画写 seamlessly looping）")
    ap.add_argument("--first-frame", help="首帧图片路径（i2v 驱动角色一致性，推荐）")
    ap.add_argument("--output-dir", required=True, help="输出目录，如 Resources/Images/character/player_run/")
    ap.add_argument("--prefix", help="帧文件名前缀（默认取输出目录名）")
    ap.add_argument("--frames", type=int, default=12, help="抽帧数量（默认 12）")
    ap.add_argument("--duration", type=float, default=2, help="视频时长秒（默认 2，pro-fast 支持 2-12）")
    ap.add_argument("--resolution", default="480p", choices=list(COST_PER_SECOND), help="分辨率（默认 480p 最便宜）")
    ap.add_argument("--ratio", default="adaptive", help="画幅比例（默认 adaptive 智能比例，i2v 跟随首帧）")
    ap.add_argument("--seed", type=int, help="随机种子（复现用）")
    ap.add_argument("--timeout", type=int, default=900, help="轮询超时秒（默认 900）")
    args = ap.parse_args()

    key = load_api_key()
    check_ffmpeg()

    out_dir = Path(args.output_dir)
    if not out_dir.is_absolute():
        out_dir = Path.cwd() / out_dir
    out_dir.mkdir(parents=True, exist_ok=True)
    prefix = args.prefix or out_dir.name.rstrip("/")

    task_id = create_task(args, key)
    result = poll_task(key, task_id, args.timeout)
    video_url = result.get("content", {}).get("video_url")
    if not video_url:
        sys.exit(f"ERROR: 任务成功但无 video_url：{json.dumps(result, ensure_ascii=False)}")

    video_path = out_dir / f"{prefix}_video.mp4"
    download(video_url, video_path)
    frames = extract_frames(video_path, out_dir, prefix, args.frames, args.duration)
    sheet = make_contact_sheet(frames, out_dir, prefix)

    summary = {
        "model": MODEL,
        "task_id": task_id,
        "resolution": args.resolution,
        "duration_s": args.duration,
        "ratio": args.ratio,
        "first_frame": args.first_frame or "(none, t2v)",
        "video": str(video_path),
        "frames": [str(f) for f in frames],
        "contact_sheet": str(sheet) if sheet else None,
        "estimated_cost_cny": round(COST_PER_SECOND[args.resolution] * args.duration, 3),
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    log(f"[Seedance] 完成：{len(frames)} 帧 → {out_dir}（contact sheet 供目检）")


if __name__ == "__main__":
    main()
