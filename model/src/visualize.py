"""可视化注册表内所有已训练模型：评估指标、混淆矩阵、per-class 召回、
特征权重/重要性、行为分布与模型结构。

从 experiments/ 加载模型，在独立现实测试集上计算，导出：
- ``experiments/viz/*.png`` —— 6 张 matplotlib 静态图（插入文档用）
- ``experiments/viz/model_report.html`` —— 自包含交互面板（浏览器直接打开）

用法（在 model/ 目录下）：
    python src/visualize.py

新增模型注册后自动纳入，无需改本文件。
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_LABELS_ZH, NUM_ACTIONS  # noqa: E402
from src.config import ModelConfig  # noqa: E402
from src.dataio import load_test  # noqa: E402
from src.evaluate import per_class_report, top_k_accuracy, kl_divergence  # noqa: E402
from src.models import list_models, load_model  # noqa: E402

# ---------------------------------------------------------------------------
# 调色板（dataviz 参考实例，categorical slot 1/2/3 分配给注册表前三模型）
# ---------------------------------------------------------------------------
CAT_LIGHT = {"mlp": "#eb6834", "attention": "#1baf7a"}
CAT_DARK = {"mlp": "#d95926", "attention": "#199e70"}
FALLBACK_COLORS = ["#2a78d6", "#eb6834", "#1baf7a", "#8e6fd8", "#c9a227"]
SURFACE_LIGHT = "#fcfcfb"
SURFACE_DARK = "#1a1a19"
INK_LIGHT = "#0b0b0b"
INK_DARK = "#ffffff"
MUTED = "#898781"
GRID_LIGHT = "#e1e0d9"
GRID_DARK = "#2c2c2a"


def _color(name: str, dark: bool = False) -> str:
    table = CAT_DARK if dark else CAT_LIGHT
    if name in table:
        return table[name]
    idx = list_models().index(name) if name in list_models() else 0
    return FALLBACK_COLORS[idx % len(FALLBACK_COLORS)]


# ---------------------------------------------------------------------------
# 汇总计算
# ---------------------------------------------------------------------------
def compute_all(cfg: dict) -> dict:
    export_dir = cfg.export_dir
    device = "cpu"  # 小模型 + 一次性评估，CPU 稳定，避免 attention 全批 GPU OOM
    X_te, y_te = load_test(cfg)
    feature_names = (cfg.processed_dir / "feature_names.txt") \
        .read_text(encoding="utf-8").splitlines()

    true_dist = np.bincount(y_te, minlength=NUM_ACTIONS).astype(float) / len(y_te)
    models = {}
    for name in list_models():
        model = load_model(name, export_dir, cfg.raw, device)
        if model is None:
            print(f"[visualize] 未找到 {name} 产物，跳过")
            continue
        proba = model.predict_proba(X_te)
        pred = proba.argmax(axis=1)
        rep = per_class_report(y_te, pred, NUM_ACTIONS)
        entry = {
            "acc": float((pred == y_te).mean()),
            "top3": top_k_accuracy(y_te, proba, 3),
            "kl": kl_divergence(y_te, proba),
            "macro_f1": rep["macro_f1"],
            "recall": [float(rep["recall"][i]) for i in range(NUM_ACTIONS)],
            "precision": [float(rep["precision"][i]) for i in range(NUM_ACTIONS)],
            "f1": [float(rep["f1"][i]) for i in range(NUM_ACTIONS)],
            "cm": rep["cm"].tolist(),
            "pred_dist": (np.bincount(pred, minlength=NUM_ACTIONS).astype(float)
                          / len(pred)).tolist(),
            "importance": model.feature_importance().tolist(),
            "structure": model.structure(),
        }
        models[name] = entry

    return {
        "actions": ACTIONS,
        "action_labels_zh": [ACTION_LABELS_ZH.get(a, a) for a in ACTIONS],
        "feature_names": feature_names,
        "true_dist": true_dist.tolist(),
        "n_test": int(len(y_te)),
        "models": models,
        "models_order": [m for m in list_models() if m in models],
    }


# ---------------------------------------------------------------------------
# PNG 渲染
# ---------------------------------------------------------------------------
def _style_axes(ax):
    ax.set_facecolor(SURFACE_LIGHT)
    ax.grid(axis="y", color=GRID_LIGHT, linewidth=0.6)
    ax.set_axisbelow(True)
    for s in ("top", "right"):
        ax.spines[s].set_visible(False)
    for s in ("left", "bottom"):
        ax.spines[s].set_color(MUTED)
    ax.tick_params(colors=MUTED, labelsize=8)


def render_pngs(data: dict, out_dir: Path):
    out_dir.mkdir(parents=True, exist_ok=True)
    models = data["models"]
    order = data["models_order"]
    actions = data["actions"]
    n_act = len(actions)

    # 1) 概览：acc / top3 / macro-F1
    fig, axes = plt.subplots(1, 3, figsize=(12, 3.6))
    metrics = [("acc", "Accuracy"), ("top3", "Top-3"), ("macro_f1", "Macro-F1")]
    for ax, (key, label) in zip(axes, metrics):
        vals = [models[m][key] for m in order]
        bars = ax.bar(order, vals, color=[_color(m) for m in order], width=0.62)
        ax.set_title(label, color=INK_LIGHT, fontsize=11, pad=8)
        ax.set_ylim(0, 1.0)
        for b, v in zip(bars, vals):
            ax.text(b.get_x() + b.get_width() / 2, v + 0.02, f"{v*100:.1f}%",
                    ha="center", fontsize=9, color=INK_LIGHT)
        _style_axes(ax)
    fig.suptitle("Model overview (test set)", color=INK_LIGHT, fontsize=13, y=1.02)
    fig.tight_layout()
    fig.savefig(out_dir / "perf_overview.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)

    # 2) per-class recall（横向分组条形，稀有类靠上）
    fig, ax = plt.subplots(figsize=(8, 7))
    y = np.arange(n_act)
    h = 0.26
    for i, m in enumerate(order):
        rec = models[m]["recall"]
        ax.barh(y - (i - 1) * h, rec, height=h, color=_color(m),
                label=m, zorder=3)
    ax.set_yticks(y)
    ax.set_yticklabels(actions, fontsize=8)
    ax.set_xlim(0, 1)
    ax.set_xlabel("Recall", fontsize=10)
    ax.set_title("Per-class recall (rare actions on top of gap)", fontsize=12)
    ax.legend(frameon=False, fontsize=9, loc="lower right")
    _style_axes(ax)
    fig.tight_layout()
    fig.savefig(out_dir / "recall_by_action.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)

    # 3) 混淆矩阵（并排）
    fig, axes = plt.subplots(1, len(order), figsize=(6 * len(order), 6))
    if len(order) == 1:
        axes = [axes]
    for ax, m in zip(axes, order):
        cm = np.array(models[m]["cm"], dtype=float)
        rowsum = cm.sum(axis=1, keepdims=True)
        cm = np.divide(cm, rowsum, out=np.zeros_like(cm), where=rowsum > 0)
        im = ax.imshow(cm, cmap="Blues", vmin=0, vmax=1, aspect="auto")
        ax.set_title(m, color=INK_LIGHT, fontsize=12)
        ax.set_xticks(range(n_act)); ax.set_xticklabels(actions, rotation=90, fontsize=6)
        ax.set_yticks(range(n_act)); ax.set_yticklabels(actions, fontsize=6)
        for i in range(n_act):
            for j in range(n_act):
                v = cm[i, j]
                if v > 0.05:
                    ax.text(j, i, f"{v:.0%}", ha="center", va="center",
                            fontsize=5, color="white" if v > 0.5 else INK_LIGHT)
    fig.suptitle("Confusion matrices (row-normalized)", color=INK_LIGHT, fontsize=13)
    fig.tight_layout()
    fig.savefig(out_dir / "confusion_matrices.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)

    # 4) 特征重要性（top 15 特征，多模型对比）
    imp = np.array([models[m]["importance"] for m in order])  # (n_models, n_feat)
    mean_imp = imp.mean(axis=0)
    top_idx = np.argsort(mean_imp)[::-1][:15]
    fig, ax = plt.subplots(figsize=(9, 4.5))
    x = np.arange(len(top_idx))
    w = 0.26
    for i, m in enumerate(order):
        vals = imp[i][top_idx]
        vals = vals / vals.max() if vals.max() > 0 else vals
        ax.bar(x - (i - 1) * w, vals, width=w, color=_color(m),
               label=m, zorder=3)
    ax.set_xticks(x)
    ax.set_xticklabels([data["feature_names"][i] for i in top_idx],
                       rotation=45, ha="right", fontsize=8)
    ax.set_ylabel("Importance (per-model normalized)")
    ax.set_title("Feature importance — top 15 by mean rank")
    ax.legend(frameon=False, fontsize=9)
    _style_axes(ax)
    fig.tight_layout()
    fig.savefig(out_dir / "feature_importance.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)

    # 5) 模型结构示意（mlp / attention，跳过 linear）
    struct_models = [m for m in order if models[m]["structure"]["kind"] != "linear"]
    if struct_models:
        fig, axes = plt.subplots(1, len(struct_models), figsize=(6.5 * len(struct_models), 5))
        if len(struct_models) == 1:
            axes = [axes]
        for ax, m in zip(axes, struct_models):
            ax.axis("off")
            st = models[m]["structure"]
            if st["kind"] == "mlp":
                layers = [("input", st["input"])] + [("h", h) for h in st["hidden"]] \
                    + [("out", st["output"])]
                _draw_fc(ax, layers)
                ax.set_title(f"MLP (activation={st['activation']})",
                             color=INK_LIGHT, fontsize=12)
            else:
                _draw_attention(ax, st)
                ax.set_title("FT-Transformer", color=INK_LIGHT, fontsize=12)
        fig.tight_layout()
        fig.savefig(out_dir / "model_architecture.png", dpi=150, bbox_inches="tight",
                    facecolor=SURFACE_LIGHT)
        plt.close(fig)

    # matplotlib 3.11 可能输出带 alpha 的 RGBA；统一转成不透明 RGB，兼容性更好
    from PIL import Image
    for p in out_dir.glob("*.png"):
        im = Image.open(p)
        if im.mode == "RGBA":
            bg = Image.new("RGB", im.size, SURFACE_LIGHT)
            bg.paste(im, mask=im.split()[3])
            bg.save(p)


def _draw_fc(ax, layers):
    xs = np.linspace(0, 1, len(layers))
    for x, (tag, n) in zip(xs, layers):
        ax.text(x, 0.5, f"{tag}\n{n}", ha="center", va="center", fontsize=9,
                color=INK_LIGHT,
                bbox=dict(boxstyle="round,pad=0.4", fc="#cde2fb", ec="#3987e5"))
    for (x0, _), (x1, _) in zip(zip(xs[:-1], layers[:-1]), zip(xs[1:], layers[1:])):
        ax.plot([x0 + 0.03, x1 - 0.03], [0.5, 0.5], color=MUTED, lw=0.8)
    ax.set_xlim(-0.08, 1.08); ax.set_ylim(0.3, 0.7)


def _draw_attention(ax, st):
    ax.text(0.02, 0.78, f"input\n{st['input']}", ha="center", va="center", fontsize=8,
            color=INK_LIGHT, bbox=dict(boxstyle="round,pad=0.3", fc="#cde2fb", ec="#3987e5"))
    ax.text(0.28, 0.78, f"feature_embed\n→{st['d_model']}d", ha="center", va="center",
            fontsize=8, color=INK_LIGHT,
            bbox=dict(boxstyle="round,pad=0.3", fc="#b7d3f6", ec="#3987e5"))
    ax.text(0.56, 0.78, f"[CLS] + {st['n_layers']}×Enc\n(heads={st['n_heads']}, ff={st['dim_feedforward']})",
            ha="center", va="center", fontsize=8, color=INK_LIGHT,
            bbox=dict(boxstyle="round,pad=0.3", fc="#86b6ef", ec="#3987e5"))
    ax.text(0.84, 0.78, f"head\n{st['head'][0]}→{st['output']}", ha="center", va="center",
            fontsize=8, color=INK_LIGHT,
            bbox=dict(boxstyle="round,pad=0.3", fc="#cde2fb", ec="#3987e5"))
    for x0, x1 in [(0.06, 0.24), (0.32, 0.52), (0.60, 0.80)]:
        ax.plot([x0, x1], [0.78, 0.78], color=MUTED, lw=0.8)
    ax.set_xlim(0, 1); ax.set_ylim(0.6, 0.95)


# ---------------------------------------------------------------------------
# HTML 渲染
# ---------------------------------------------------------------------------
def render_html(data: dict, out_path: Path):
    payload = json.dumps(data, ensure_ascii=False)
    order_json = json.dumps(data["models_order"])
    html = f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Worker 行为决策模型 — 可视化报告</title>
<style>
  :root {{
    color-scheme: light;
    --surface: {SURFACE_LIGHT}; --page: #f9f9f7;
    --ink: {INK_LIGHT}; --ink2: #52514e; --muted: {MUTED};
    --grid: {GRID_LIGHT};
    --m: {_color('mlp')}; --a: {_color('attention')};
    --ring: rgba(11,11,11,0.10);
  }}
  :root[data-theme="dark"] {{
    color-scheme: dark;
    --surface: {SURFACE_DARK}; --page: #0d0d0d;
    --ink: {INK_DARK}; --ink2: #c3c2b7; --muted: {MUTED};
    --grid: {GRID_DARK};
    --m: {_color('mlp', dark=True)}; --a: {_color('attention', dark=True)};
    --ring: rgba(255,255,255,0.10);
  }}
  * {{ box-sizing: border-box; }}
  body {{ margin: 0; background: var(--page); color: var(--ink);
         font-family: system-ui, -apple-system, "Segoe UI", sans-serif;
         font-size: 14px; line-height: 1.5; }}
  header {{ padding: 24px 32px 8px; display: flex; justify-content: space-between;
            align-items: center; }}
  header h1 {{ font-size: 20px; margin: 0; }}
  header .sub {{ color: var(--ink2); font-size: 13px; }}
  button {{ font: inherit; padding: 6px 14px; border: 1px solid var(--ring);
            border-radius: 6px; background: var(--surface); color: var(--ink);
            cursor: pointer; }}
  section {{ margin: 20px 32px; padding: 20px 22px; background: var(--surface);
             border: 1px solid var(--ring); border-radius: 10px; }}
  h2 {{ font-size: 16px; margin: 0 0 6px; }}
  .desc {{ color: var(--ink2); font-size: 12.5px; margin: 0 0 14px; }}
  .legend {{ display: flex; gap: 18px; flex-wrap: wrap; margin-bottom: 12px;
             color: var(--ink2); font-size: 12.5px; }}
  .legend .sw {{ display: inline-block; width: 11px; height: 11px; border-radius: 2px;
                 margin-right: 6px; vertical-align: -1px; }}
  .cards {{ display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }}
  .card {{ border: 1px solid var(--ring); border-radius: 8px; padding: 12px 14px; }}
  .card .metric {{ color: var(--ink2); font-size: 12px; }}
  .card .val {{ font-size: 22px; font-variant-numeric: tabular-nums; margin-top: 2px; }}
  table {{ border-collapse: collapse; width: 100%; font-variant-numeric: tabular-nums; }}
  th, td {{ padding: 5px 8px; text-align: right; font-size: 12.5px;
            border-bottom: 1px solid var(--grid); }}
  th:first-child, td:first-child {{ text-align: left; }}
  thead th {{ color: var(--ink2); font-weight: 600; }}
  .bar-row {{ display: grid; grid-template-columns: 130px 1fr; align-items: center;
              gap: 8px; margin: 3px 0; }}
  .bar-row .lbl {{ font-size: 11.5px; color: var(--ink2); text-align: right;
                   white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .track {{ position: relative; height: 16px; }}
  .seg {{ position: absolute; top: 0; bottom: 0; border-radius: 2px;
          min-width: 2px; cursor: default; }}
  .seg:hover {{ filter: brightness(0.9); }}
  .heat {{ display: grid; gap: 1px; }}
  .cell {{ position: relative; aspect-ratio: 1; border-radius: 2px; cursor: default; }}
  .cell:hover {{ outline: 2px solid var(--ink); z-index: 2; }}
  .tooltip {{ position: fixed; background: var(--surface); color: var(--ink);
              border: 1px solid var(--ring); border-radius: 6px; padding: 6px 10px;
              font-size: 12px; pointer-events: none; z-index: 10; box-shadow: 0 2px 8px rgba(0,0,0,0.2);
              display: none; max-width: 320px; }}
  .struct {{ display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }}
  .node {{ border: 1.5px solid var(--m); border-radius: 8px; padding: 8px 12px;
           text-align: center; font-size: 12px; }}
  .node b {{ display: block; font-size: 13px; }}
  .arrow {{ color: var(--muted); }}
  @media (max-width: 720px) {{ .cards {{ grid-template-columns: 1fr; }} }}
</style>
</head>
<body>
<header>
  <div>
    <h1>Worker 行为决策模型 — 可视化报告</h1>
    <div class="sub">测试集 {data['n_test']} 条 · {data['models_order'].__len__()} 模型对比</div>
  </div>
  <button onclick="toggleTheme()">切换主题</button>
</header>

<div class="tooltip" id="tip"></div>

<section>
  <h2>1 · 概览指标</h2>
  <p class="desc">准确率、Top-3、KL 散度、Macro-F1。</p>
  <div class="legend" id="legend-top"></div>
  <div class="cards" id="cards"></div>
</section>

<section>
  <h2>2 · 行为分布（真实 vs 预测）</h2>
  <p class="desc">Top-1 预测的边际分布是否贴合真实行为占比。</p>
  <div id="dist"></div>
</section>

<section>
  <h2>3 · 每行为召回率（稀有类检测）</h2>
  <p class="desc">长尾标签下 accuracy 会被大类主导，召回率才能暴露稀有行为是否真正学到。</p>
  <div id="recall"></div>
  <h3 style="font-size:14px;margin:18px 0 8px">表格视图</h3>
  <table id="recall-table"></table>
</section>

<section>
  <h2>4 · 混淆矩阵</h2>
  <p class="desc">行归一化：第 i 行第 j 列 = 真实行为 i 被预测为 j 的比例（越暗越大）。</p>
  <div id="cm-wrap" style="display:grid;grid-template-columns:repeat(3,1fr);gap:18px"></div>
</section>

<section>
  <h2>5 · 特征权重 / 重要性</h2>
  <p class="desc">各模型每特征重要性（各自归一化，取均值排序 top 15）。</p>
  <div id="feat-imp"></div>
</section>

<section>
  <h2>6 · 模型结构</h2>
  <p class="desc">前向架构示意（特征维度 → 隐藏层 → 行为 logits）。</p>
  <div id="struct"></div>
</section>

<script>
const D = {payload};
const MODELS = {order_json};
const tip = document.getElementById("tip");
const palette = ["#2a78d6", "#eb6834", "#1baf7a", "#8e6fd8", "#c9a227"];
const COL = {{}};
MODELS.forEach((m, i) => {{ COL[m] = "var(--c" + i + ")"; }});
function showTip(e, html) {{ tip.innerHTML = html; tip.style.display = "block";
  tip.style.left = (e.clientX + 12) + "px"; tip.style.top = (e.clientY + 12) + "px"; }}
function hideTip() {{ tip.style.display = "none"; }}
function pct(v) {{ return (v * 100).toFixed(1) + "%"; }}

// 顶栏图例
const lt = document.getElementById("legend-top");
MODELS.forEach(m => {{
  const s = document.createElement("span");
  s.innerHTML = `<span class="sw" style="background:${{COL[m]}}"></span>${{m}}`;
  lt.appendChild(s);
}});

// 1 概览
const cards = document.getElementById("cards");
const metricDefs = [
  ["acc", "准确率", v => pct(v)],
  ["top3", "Top-3", v => pct(v)],
  ["kl", "KL 散度（越小越贴合）", v => v.toFixed(4)],
  ["macro_f1", "Macro-F1", v => pct(v)],
];
for (const [key, label, fmt] of metricDefs) {{
  const card = document.createElement("div"); card.className = "card";
  card.innerHTML = `<div class="metric">${{label}}</div>` + MODELS.map(m =>
    `<div class="val" style="color:${{COL[m]}}">${{fmt(D.models[m][key])}}</div>`).join("");
  cards.appendChild(card);
}}

// 2 行为分布
const dist = document.getElementById("dist");
D.actions.forEach((a, i) => {{
  const row = document.createElement("div"); row.className = "bar-row";
  const lbl = document.createElement("div"); lbl.className = "lbl";
  lbl.textContent = D.action_labels_zh[i] + " (" + a + ")";
  row.appendChild(lbl);
  const track = document.createElement("div"); track.className = "track";
  const series = [["真实", D.true_dist[i], "var(--muted)"]].concat(
    MODELS.map(m => [m, D.models[m].pred_dist[i], COL[m]]));
  const nS = series.length;
  series.forEach(([name, v, c], k) => {{
    const seg = document.createElement("div"); seg.className = "seg";
    seg.style.left = (k * 100 / nS) + "%"; seg.style.width = (v * 100 / nS) + "%";
    seg.style.background = c;
    seg.style.opacity = name === "真实" ? "0.4" : "0.9";
    seg.onmousemove = e => showTip(e, `${{name}} · ${{a}}<br>${{pct(v)}}`);
    seg.onmouseleave = hideTip;
    track.appendChild(seg);
  }});
  row.appendChild(track); dist.appendChild(row);
}});

// 3 召回
const recall = document.getElementById("recall");
D.actions.forEach((a, i) => {{
  const row = document.createElement("div"); row.className = "bar-row";
  const lbl = document.createElement("div"); lbl.className = "lbl";
  lbl.textContent = D.action_labels_zh[i] + " (" + a + ")";
  row.appendChild(lbl);
  const track = document.createElement("div"); track.className = "track";
  MODELS.forEach((m, k) => {{
    const v = D.models[m].recall[i];
    const seg = document.createElement("div"); seg.className = "seg";
    seg.style.left = (k * 100 / MODELS.length) + "%";
    seg.style.width = (v * 100 / MODELS.length) + "%";
    seg.style.background = COL[m];
    seg.onmousemove = e => showTip(e, `${{m}} · ${{a}}<br>recall ${{pct(v)}}`);
    seg.onmouseleave = hideTip;
    track.appendChild(seg);
  }});
  row.appendChild(track); recall.appendChild(row);
}});
const rt = document.getElementById("recall-table");
rt.innerHTML = "<thead><tr><th>行为</th>" + MODELS.map(m =>
  "<th>" + m + " recall</th>").join("") + "</tr></thead><tbody>" +
  D.actions.map((a, i) => `<tr><td>${{D.action_labels_zh[i]}} (${{a}})</td>` +
    MODELS.map(m => `<td>${{pct(D.models[m].recall[i])}}</td>`).join("") + "</tr>").join("") +
  `<tr><td><b>Macro-F1</b></td>` + MODELS.map(m =>
    `<td><b>${{pct(D.models[m].macro_f1)}}</b></td>`).join("") + "</tr></tbody>";

// 4 混淆矩阵
const cmWrap = document.getElementById("cm-wrap");
MODELS.forEach(m => {{
  const box = document.createElement("div");
  const cm = D.models[m].cm;
  const n = D.actions.length;
  box.innerHTML = `<div style="font-weight:600;margin-bottom:6px">${{m}}</div>`;
  const heat = document.createElement("div"); heat.className = "heat";
  heat.style.gridTemplateColumns = `repeat(${{n}}, 1fr)`;
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) {{
    const v = cm[i][j] / (cm[i].reduce((s, x) => s + x, 0) || 1);
    const cell = document.createElement("div"); cell.className = "cell";
    const c0 = [205, 226, 251], c1 = [24, 79, 149]; // light → dark blue
    const cc = c0.map((a, idx) => Math.round(a + (c1[idx] - a) * v));
    cell.style.background = v > 0 ? `rgb(${{cc.join(",")}})` : "var(--grid)";
    cell.onmousemove = e => showTip(e, `${{D.actions[i]}} → ${{D.actions[j]}}<br>${{(v * 100).toFixed(1)}}%`);
    cell.onmouseleave = hideTip;
    heat.appendChild(cell);
  }}
  box.appendChild(heat); cmWrap.appendChild(box);
}});

// 5 特征重要性
const fi = document.getElementById("feat-imp");
{{
  const imp = MODELS.map(m => D.models[m].importance);
  const mean = imp[0].map((_, j) => imp.reduce((s, r) => s + r[j], 0) / imp.length);
  const order = mean.map((v, j) => j).sort((a, b) => mean[b] - mean[a]).slice(0, 15);
  fi.innerHTML = `<h3 style="font-size:14px;margin:18px 0 8px">特征重要性 top 15（各自归一化）</h3>`;
  order.forEach(j => {{
    const row = document.createElement("div"); row.className = "bar-row";
    const lbl = document.createElement("div"); lbl.className = "lbl";
    lbl.textContent = D.feature_names[j];
    row.appendChild(lbl);
    const track = document.createElement("div"); track.className = "track";
    const mx = Math.max(...MODELS.map(m => D.models[m].importance[j]), 1e-9);
    const nS = MODELS.length;
    MODELS.forEach((m, k) => {{
      const v = D.models[m].importance[j] / mx;
      const seg = document.createElement("div"); seg.className = "seg";
      seg.style.left = (k * 100 / nS) + "%"; seg.style.width = (v * 100 / nS) + "%";
      seg.style.background = COL[m];
      seg.style.opacity = "0.85";
      seg.onmousemove = e => showTip(e, `${{m}} · ${{D.feature_names[j]}}<br>importance ${{D.models[m].importance[j].toFixed(4)}}`);
      seg.onmouseleave = hideTip;
      track.appendChild(seg);
    }});
    row.appendChild(track); fi.appendChild(row);
  }});
}}

// 6 模型结构
const st = document.getElementById("struct");
function node(html) {{ return `<div class="node">${{html}}</div>`; }}
for (const m of MODELS) {{
  const s = D.models[m].structure;
  const box = document.createElement("div"); box.style.marginBottom = "18px";
  if (s.kind === "mlp") {{
    box.innerHTML = `<div style="font-weight:600;margin-bottom:8px">MLP（${{m}}）</div>
      <div class="struct">${{node(`<b>input</b>${{s.input}} 特征`)}}
      <span class="arrow">→</span>` + s.hidden.map(h =>
      node(`<b>Linear</b>${{h}} · ${{s.activation}} · Dropout`)).join('<span class="arrow">→</span>') +
      `<span class="arrow">→</span>${{node(`<b>Linear</b>${{s.output}} logits`)}}</div>`;
  }} else if (s.kind === "attention") {{
    box.innerHTML = `<div style="font-weight:600;margin-bottom:8px">FT-Transformer（${{m}}）</div>
      <div class="struct">${{node(`<b>input</b>${{s.input}} 特征`)}}
      <span class="arrow">→</span>${{node(`<b>feature_embed</b>每特征 → ${{s.d_model}}d`)}}
      <span class="arrow">→</span>${{node(`<b>[CLS] + ${{s.n_layers}}×Encoder</b>${{s.n_heads}} heads · ff ${{s.dim_feedforward}}`)}}
      <span class="arrow">→</span>${{node(`<b>MLP head</b>${{s.head[0]}} → ${{s.output}} logits`)}}</div>`;
  }}
  st.appendChild(box);
}}

function toggleTheme() {{
  const r = document.documentElement;
  r.dataset.theme = r.dataset.theme === "dark" ? "light" : "dark";
}}
</script>
</body>
</html>"""
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(html, encoding="utf-8")


# ---------------------------------------------------------------------------
def main():
    cfg = ModelConfig()
    export_dir = cfg.export_dir
    viz_dir = export_dir / "viz"
    print(f"[visualize] 计算 {len(list_models())} 个模型指标 ...")
    data = compute_all(cfg)
    for m in data["models_order"]:
        r = data["models"][m]
        print(f"[visualize] {m:<10s} acc={r['acc']*100:5.2f}%  "
              f"macro_f1={r['macro_f1']*100:5.2f}%  top3={r['top3']*100:5.2f}%")
    print(f"[visualize] 渲染 PNG → {viz_dir}")
    render_pngs(data, viz_dir)
    html_path = viz_dir / "model_report.html"
    render_html(data, html_path)
    print(f"[visualize] HTML → {html_path}")
    print("[visualize] 完成")


if __name__ == "__main__":
    main()
