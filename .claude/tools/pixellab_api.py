"""PixelLab REST v2 极简客户端。token 从注册表 HKCU\\Environment\\PIXELLAB_SECRET 读取，
仅作为 Authorization header 使用，绝不打印/落盘。

用法:
  python pixellab_api.py balance
  python pixellab_api.py create-tileset '<json_body>'
  python pixellab_api.py job <background_job_id>
  python pixellab_api.py tileset <tileset_id>
  python pixellab_api.py schema <endpoint_path_fragment>   # 从 openapi.json 提取 schema
"""
import json
import sys
import time
import urllib.request
import winreg

BASE = "https://api.pixellab.ai/v2"


def get_token():
    k = winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment")
    v, _ = winreg.QueryValueEx(k, "PIXELLAB_SECRET")
    return v


def request(method, path, body=None):
    req = urllib.request.Request(
        BASE + path,
        data=json.dumps(body).encode() if body is not None else None,
        headers={
            "Authorization": "Bearer " + get_token(),
            "Content-Type": "application/json",
        },
        method=method,
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        return {"_http_error": e.code, "_body": e.read().decode()[:2000]}


def main():
    cmd = sys.argv[1]
    if cmd == "balance":
        out = request("GET", "/balance")
    elif cmd == "create-tileset":
        out = request("POST", "/create-tileset", json.loads(sys.argv[2]))
    elif cmd == "job":
        out = request("GET", f"/background-jobs/{sys.argv[2]}")
    elif cmd == "tileset":
        out = request("GET", f"/tilesets/{sys.argv[2]}")
    elif cmd == "schema":
        spec = request("GET", "/openapi.json")
        frag = sys.argv[2]
        paths = spec.get("paths", {})
        for p, ops in paths.items():
            if frag in p:
                print(json.dumps({p: ops}, indent=1, ensure_ascii=False)[:6000])
                return
        print("not found; paths containing 'tile':",
              [p for p in paths if "tile" in p])
        return
    else:
        print("unknown cmd")
        sys.exit(1)
    print(json.dumps(out, indent=1, ensure_ascii=False)[:12000])


if __name__ == "__main__":
    main()
