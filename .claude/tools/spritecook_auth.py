"""SpriteCook device-flow 授权 + 写入 ~/.claude.json 的 MCP 配置。

复刻 spritecook-mcp setup 的非交互路径：
start -> 浏览器授权 -> poll 拿 api_key -> 合并写入 ~/.claude.json
"""
import json
import os
import sys
import time
import urllib.request

API = os.environ.get("SPRITECOOK_API_URL", "https://api.spritecook.ai")
CLAUDE_JSON = os.path.expanduser("~/.claude.json")


def post_json(url, payload=None):
    data = json.dumps(payload or {}).encode()
    req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode())


def get_json(url):
    with urllib.request.urlopen(url, timeout=30) as r:
        return json.loads(r.read().decode())


def main():
    session = post_json(f"{API}/v1/api/device-auth/start")
    print("CONNECT_URL:", session["connect_url"])
    print("DEVICE_CODE:", session["device_code"])
    print("EXPIRES_IN:", session.get("expires_in"), flush=True)

    deadline = time.time() + int(session.get("expires_in", 600)) * 1
    api_key = None
    while time.time() < deadline:
        time.sleep(3)
        try:
            data = get_json(f"{API}/v1/api/device-auth/poll/{session['session_id']}")
        except Exception:
            continue
        if data.get("status") == "completed" and data.get("api_key"):
            api_key = data["api_key"]
            break
        if data.get("status") == "expired":
            print("STATUS: expired")
            sys.exit(2)

    if not api_key:
        print("STATUS: timeout")
        sys.exit(3)

    print("STATUS: authorized")
    print("KEY_PREFIX:", api_key[:12] + "...")

    config = {}
    if os.path.exists(CLAUDE_JSON):
        with open(CLAUDE_JSON, encoding="utf-8") as f:
            config = json.load(f)
    config.setdefault("mcpServers", {})
    config["mcpServers"]["spritecook"] = {
        "type": "http",
        "url": f"{API}/mcp/",
        "headers": {"Authorization": f"Bearer {api_key}"},
    }
    with open(CLAUDE_JSON, "w", encoding="utf-8") as f:
        json.dump(config, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print("WROTE:", CLAUDE_JSON)


if __name__ == "__main__":
    main()
