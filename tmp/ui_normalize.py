# -*- coding: utf-8 -*-
"""UI 像素规范化批改脚本（P2）。

子命令：
  font-size          字号对齐 12 整数倍（m_FontSize，仅 legacy Text；TMP 的 m_fontSize 大小写不同天然隔离）
  arial-replace      Unity 内置字体(Arial) 10 处 → ark-pixel（断言总数）
  canvas-match       屏幕空间 CanvasScaler match 统一 0.5（断言 4 处；世界空间 9x9 排除）
  strip-nested-scaler 删除 2 个嵌套 Canvas 的冗余 CanvasScaler 组件（多重断言）
  bestfit-report     报告 m_BestFit: 1 位置（不改）

默认 dry-run 只报告；--apply 生效写回。所有写回操作幂等（重跑无变化）。
"""
import argparse
import re
import sys
from pathlib import Path

ASSETS = Path(__file__).resolve().parent.parent  # Assets/
ARK_GUID = "994464cadda06394eb1598617cdd2c57"
ARIAL_REF = "{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}"
ARK_REF = "{fileID: 12800000, guid: %s, type: 3}" % ARK_GUID
SCALER_GUID = "0cd44c1031e13a943bb63640046fad76"  # CanvasScaler
EXCLUDE_PARTS = ("TextMesh Pro", "Scripts" + "\\" + "Reference", "Scripts/Reference")

STRIP_TARGETS = {  # prefab 相对路径 → 冗余 CanvasScaler 组件 fileID
    "ResourcesLocal/Prefabs/UI/EquipmentPanel.prefab": 6582212638441723531,
    "ResourcesLocal/Prefabs/UI/ComparePopupPanel.prefab": 4344674208031083039,
}

BOUNDS = [(18, 12), (30, 24), (42, 36), (54, 48), (66, 60)]


def snap(v):
    for hi, m in BOUNDS:
        if v <= hi:
            return m
    return 72


def iter_targets():
    files = list((ASSETS / "Scenes").glob("*.unity"))
    files += [p for p in ASSETS.rglob("*.prefab")
              if not any(part in str(p) for part in EXCLUDE_PARTS)]
    return files


def split_blocks(text):
    """按 '--- !u!' 切块，返回 [(块头行, 块全文, 起始偏移)]。"""
    out, idx = [], 0
    for m in re.finditer(r"^--- !u!.*$", text, re.M):
        if out:
            out[-1] = (out[-1][0], text[out[-1][2]:m.start()], out[-1][2])
        out.append((m.group(0), "", m.start()))
    if out:
        out[-1] = (out[-1][0], text[out[-1][2]:], out[-1][2])
    return out


def cmd_font_size(apply):
    total, dist = 0, {}
    pat = re.compile(r"^(\s*)m_FontSize:\s*(\d+)\s*$", re.M)
    for f in iter_targets():
        text = f.read_text(encoding="utf-8")
        n = 0

        def repl(m):
            nonlocal n
            old, new = int(m.group(2)), snap(int(m.group(2)))
            if old != new:
                n += 1
                dist[new] = dist.get(new, 0) + 1
            return "%sm_FontSize: %d" % (m.group(1), new)

        new_text = pat.sub(repl, text)
        if n and apply:
            f.write_text(new_text, encoding="utf-8")
        if n:
            print("  %-70s %3d 处" % (f.relative_to(ASSETS), n))
            total += n
    print("font-size: 共 %d 处%s → 分布 %s" % (total, "（已写回）" if apply else "（dry-run）", dict(sorted(dist.items()))))


def cmd_arial_replace(apply):
    hits = []
    for f in iter_targets():
        text = f.read_text(encoding="utf-8")
        c = text.count(ARIAL_REF)
        if c:
            hits.append((f, c))
            if apply:
                f.write_text(text.replace(ARIAL_REF, ARK_REF), encoding="utf-8")
    total = sum(c for _, c in hits)
    for f, c in hits:
        print("  %-70s %d 处" % (f.relative_to(ASSETS), c))
    print("arial-replace: 共 %d 处%s" % (total, "（已写回）" if apply else "（dry-run）"))
    if total != 10:
        print("!! 断言失败：总数 %d != 10，中止（未写回任何文件）" % total)
        sys.exit(1)


def cmd_canvas_match(apply):
    changed, skipped = [], []
    for f in iter_targets():
        text = f.read_text(encoding="utf-8")
        blocks = split_blocks(text)
        file_dirty = False
        for header, body, off in blocks:
            if SCALER_GUID not in body:
                continue
            if "m_UiScaleMode: 1" not in body:
                continue
            if "m_ReferenceResolution: {x: 1920, y: 1080}" not in body:
                if "m_PresetInfoIsWorld: 1" in body:
                    skipped.append((f, "世界空间 9x9 头顶 HUD（按计划排除）"))
                continue
            if "m_PresetInfoIsWorld: 1" in body:
                skipped.append((f, "世界空间但分辨率 1920x1080，人工确认"))
                continue
            m = re.search(r"^(\s*)m_MatchWidthOrHeight:\s*([\d.]+)\s*$", body, re.M)
            if not m:
                print("!! %s 块 %s 无 m_MatchWidthOrHeight，中止" % (f.name, header))
                sys.exit(1)
            if m.group(2) == "0.5":
                skipped.append((f, "已 0.5 幂等跳过"))
                continue
            changed.append((f, header, m.group(2)))
            if apply:
                s, e = off, off + len(body)
                text = text[:s] + body.replace(m.group(0), "%sm_MatchWidthOrHeight: 0.5" % m.group(1), 1) + text[e:]
                file_dirty = True
        if file_dirty:
            f.write_text(text, encoding="utf-8")
    for f, header, old in changed:
        print("  %-70s %s %s → 0.5" % (f.relative_to(ASSETS), header[:30], old))
    for f, why in skipped:
        print("  skip %-66s %s" % (f.relative_to(ASSETS), why))
    print("canvas-match: 改 %d 处%s" % (len(changed), "（已写回）" if apply else "（dry-run）"))
    if len(changed) > 4:
        print("!! 断言失败：改动 %d 处 > 预期 4，中止" % len(changed))
        sys.exit(1)


def cmd_strip_nested_scaler(apply):
    for rel, file_id in STRIP_TARGETS.items():
        f = ASSETS / rel
        text = f.read_text(encoding="utf-8")
        blocks = split_blocks(text)
        fid = str(file_id)
        hits = [(h, b, o) for h, b, o in blocks if SCALER_GUID in b and fid in h]
        assert len(hits) == 1, "%s: CanvasScaler guid 块数 %d != 1" % (rel, len(hits))
        _, body, off = hits[0]
        assert text.count(fid) == 2, "%s: fileID 出现 %d 次 != 2（块头+组件列表）" % (rel, text.count(fid))
        comp_line = "  - component: {fileID: %s}" % fid
        assert comp_line in text, "%s: 组件列表行不存在" % rel
        if not apply:
            print("  dry-run: %s 将删块 %s（%d 行）+ 组件行" % (rel, fid, body.count("\n")))
            continue
        # 先删块（off 基于原始文本有效），再删前部组件行（字符串锚点，与偏移无关）
        new_text = text[:off] + text[off + len(body):]
        new_text = new_text.replace(comp_line + "\n", "", 1)
        # 后置断言
        assert SCALER_GUID not in new_text, "%s: 残留 CanvasScaler guid" % rel
        assert fid not in new_text, "%s: 残留 fileID" % rel
        assert new_text.count("--- !u!") == text.count("--- !u!") - 1, "%s: 块数未减 1" % rel
        f.write_text(new_text, encoding="utf-8")
        print("  %s 已删冗余 CanvasScaler（块+组件行），后置断言全过" % rel)
    # 跨文件断言：被删 fileID 在所有场景零引用
    for scene in (ASSETS / "Scenes").glob("*.unity"):
        t = scene.read_text(encoding="utf-8")
        for rel, file_id in STRIP_TARGETS.items():
            assert str(file_id) not in t, "%s 场景仍引用 %s 的 fileID" % (scene.name, rel)
    print("strip-nested-scaler: 完成%s" % "（已写回）" if apply else "")


def cmd_bestfit_report(apply):
    n = 0
    for f in iter_targets():
        for i, line in enumerate(f.read_text(encoding="utf-8").splitlines(), 1):
            if "m_BestFit: 1" in line:
                n += 1
                print("  %s:%d" % (f.relative_to(ASSETS), i))
    print("bestfit-report: %d 处 m_BestFit: 1（只报告，按计划不改）" % n)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("cmd", choices=["font-size", "arial-replace", "canvas-match",
                                    "strip-nested-scaler", "bestfit-report", "all"])
    ap.add_argument("--apply", action="store_true", help="生效写回（默认 dry-run）")
    a = ap.parse_args()
    cmds = {"font-size": cmd_font_size, "arial-replace": cmd_arial_replace,
            "canvas-match": cmd_canvas_match, "strip-nested-scaler": cmd_strip_nested_scaler,
            "bestfit-report": cmd_bestfit_report}
    todo = cmds.values() if a.cmd == "all" else [cmds[a.cmd]]
    for fn in todo:
        print("== %s ==" % fn.__name__)
        fn(a.apply)


if __name__ == "__main__":
    main()
