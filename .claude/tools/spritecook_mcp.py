"""极简 MCP streamable-HTTP 客户端，直接调 SpriteCook 远程 server。

用法:
  python spritecook_mcp.py list                              # tools/list
  python spritecook_mcp.py call <tool_name> '<json_args>'    # tools/call
"""
import json
import os
import sys
import urllib.request

API = "https://api.spritecook.ai/mcp/"
CFG = os.path.expanduser("~/.claude.json")


def load_key():
    cfg = json.load(open(CFG, encoding="utf-8"))
    return cfg["mcpServers"]["spritecook"]["headers"]["Authorization"]


class Client:
    def __init__(self):
        self.key = load_key()
        self.session_id = None
        self.next_id = 1

    def _headers(self):
        h = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
            "Authorization": self.key,
        }
        if self.session_id:
            h["Mcp-Session-Id"] = self.session_id
        return h

    def _post(self, payload):
        req = urllib.request.Request(
            API, data=json.dumps(payload).encode(), headers=self._headers()
        )
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                sid = r.headers.get("Mcp-Session-Id")
                if sid:
                    self.session_id = sid
                body = r.read().decode()
                ctype = r.headers.get("Content-Type", "")
        except urllib.error.HTTPError as e:
            print(f"HTTP {e.code}: {e.read().decode()[:500]}", file=sys.stderr)
            raise
        if "text/event-stream" in ctype:
            # 取最后一个 data: 行（JSON-RPC 响应）
            for line in reversed(body.splitlines()):
                if line.startswith("data:"):
                    return json.loads(line[5:].strip())
            raise RuntimeError("no data in SSE: " + body[:500])
        return json.loads(body) if body.strip() else {}

    def initialize(self):
        resp = self._post({
            "jsonrpc": "2.0", "id": self.next_id, "method": "initialize",
            "params": {
                "protocolVersion": "2025-03-26",
                "capabilities": {},
                "clientInfo": {"name": "claude-code", "version": "1.0"},
            },
        })
        self.next_id += 1
        # initialized 通知
        self._post({"jsonrpc": "2.0", "method": "notifications/initialized"})
        return resp

    def rpc(self, method, params=None):
        payload = {"jsonrpc": "2.0", "id": self.next_id, "method": method}
        if params is not None:
            payload["params"] = params
        self.next_id += 1
        return self._post(payload)

    def call(self, name, args):
        return self.rpc("tools/call", {"name": name, "arguments": args})


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "list"
    c = Client()
    init = c.initialize()
    server = init.get("result", {}).get("serverInfo", {})
    print(f"# server: {server.get('name')} {server.get('version')}", file=sys.stderr)

    if mode == "list":
        resp = c.rpc("tools/list")
        tools = resp.get("result", {}).get("tools", [])
        print(json.dumps([{"name": t["name"], "description": t.get("description", "")[:160]}
                          for t in tools], indent=2, ensure_ascii=False))
    elif mode == "schema":
        resp = c.rpc("tools/list")
        tools = resp.get("result", {}).get("tools", [])
        name = sys.argv[2]
        tool = next((t for t in tools if t["name"] == name), None)
        print(json.dumps(tool, indent=2, ensure_ascii=False))
    elif mode == "call":
        name = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        resp = c.call(name, args)
        print(json.dumps(resp, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
