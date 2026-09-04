# -*- coding: utf-8 -*-
"""编译验证：从 Assembly-CSharp.csproj 提取源文件与引用，调 Unity 自带 Roslyn csc.dll 编译。
输出落工程根 _verify_out（严禁落 Assets 内）。用法：python tmp/verify_compile.py
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # Assets/
PROJ = ROOT.parent / "Assembly-CSharp.csproj"          # 工程根 csproj
OUT = ROOT.parent / "_verify_out"
UNITY = Path(r"D:/APP/Unity/Unity/2022.3.62f2c1/Editor/Data")
CSC = UNITY / "DotNetSdkRoslyn" / "csc.dll"
DOTNET = UNITY / "NetCoreRuntime" / "dotnet.exe"

text = PROJ.read_text(encoding="utf-8")
refs = re.findall(r"<HintPath>([^<]+)</HintPath>", text)
# Photon Reference 有独立 asmdef（分属 Photon 程序集），从 ScriptAssemblies 引入其编译产物
refs += [str(p) for p in (ROOT.parent / "Library" / "ScriptAssemblies").glob("Photon*.dll")
         if "Editor" not in p.name]
# 源文件：磁盘全扫（csproj 严重过期）。排除：Reference（独立 asmdef）、Editor/Tests（Editor 程序集）、tmp
sources = []
for p in ROOT.rglob("*.cs"):
    rel = p.relative_to(ROOT).as_posix()
    if rel.startswith("Scripts/Reference/Photon/"):
        continue  # 仅 Photon 有独立 asmdef；Reference 下散装源文件仍属主程序集
    if "/Editor/" in rel or rel.startswith("Scripts/2D/Editor/") or "/Tests/" in rel or rel.startswith("tmp/"):
        continue
    if rel.startswith("Library/") or rel.startswith("TextMesh Pro/Editor/"):
        continue
    sources.append(str(p))
rsp = OUT / "uinorm_sources.rsp"
with rsp.open("w", encoding="utf-8") as f:
    for s in sources:
        f.write('"%s"\n' % s)
    for r in refs:
        f.write('-r:"%s"\n' % (ROOT.parent / r))
f.write("") if False else None
# 追加编译参数（追加在引用之后即可）
with rsp.open("a", encoding="utf-8") as f:
    f.write('-out:"%s"\n' % (OUT / "uinorm_verify.dll"))
    f.write("-target:library -nologo -nowarn:CS0169;CS0649\n")

print("sources=%d refs=%d" % (len(sources), len(refs)))
r = subprocess.run([str(DOTNET), "exec", str(CSC), "@%s" % rsp],
                   capture_output=True, cwd=str(OUT), timeout=600)


def dec(b):
    for enc in ("utf-8", "gbk", "latin-1"):
        try:
            return b.decode(enc)
        except UnicodeDecodeError:
            continue
    return ""


log = dec(r.stdout or b"") + dec(r.stderr or b"")
(OUT / "uinorm_csc.log").write_text(log, encoding="utf-8")
errs = [l for l in log.splitlines() if "error CS" in l]
print("退出码 %d；错误 %d 条" % (r.returncode, len(errs)))
for l in errs[:30]:
    print(l)
if not errs:
    print("== 编译通过 ==")
