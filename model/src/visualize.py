"""可视化所有端到端模型：baseline(效用函数) / mlp / attention。

从 experiments/ 加载三个模型，在测试集上计算评估指标、混淆矩阵、per-class
召回、特征权重/重要性、行为分布与模型结构，导出：

- ``experiments/viz/*.png`` —— 6 张 matplotlib 静态图（插入文档用）
- ``experiments/viz/model_report.html`` —— 自包含交互面板（浏览器直接打开）

用法（在 model/ 目录下）：
    python src/visualize.py

配色遵循 dataviz 参考调色板（三模型 = categorical slot 1/2/3）。
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
import yaml  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_LABELS_ZH, NUM_ACTIONS  # noqa: E402
from src.train import load_data, split  # noqa: E402
from src.evaluate import per_class_report, top_k_accuracy, kl_divergence  # noqa: E402

# ---------------------------------------------------------------------------
# 调色板（dataviz 参考实例，categorical slot 1/2/3 分配给三个模型）
# ---------------------------------------------------------------------------
MODEL_NAMES = ["baseline", "mlp", "attention"]
MODEL_LABELS_ZH = {"baseline": "效用函数", "mlp": "神经网络", "attention": "注意力"}
CAT_LIGHT = {"baseline": "#2a78d6", "mlp": "#eb6834", "attention": "#1baf7a"}
CAT_DARK = {"baseline": "#3987e5", "mlp": "#d95926", "attention": "#199e70"}
SURFACE_LIGHT = "#fcfcfb"
SURFACE_DARK = "#1a1a19"
INK_LIGHT = "#0b0b0b"
INK_DARK = "#ffffff"
MUTED = "#898781"
GRID_LIGHT = "#e1e0d9"
GRID_DARK = "#2c2c2a"
SEQ_BLUE = ["#cde2fb", "#86b6ef", "#3987e5", "#256abf", "#184f95"]
DIV_RED = "#d03b3b"   # 与 sequential blue 组成 diverging 的暖极
DIV_MID = "#f0efec"


# ---------------------------------------------------------------------------
# 模型加载 / 推理
# ---------------------------------------------------------------------------
def load_model(name: str, export_dir: Path, device: str = "cpu"):
    if name == "baseline":
        import joblib
        return joblib.load(export_dir / "baseline.joblib")

    import torch
    from src.models.mlp import WorkerMLP
    from src.models.attention import WorkerAttention
    if name == "mlp":
        ckpt = torch.load(export_dir / "mlp.pt", map_location="cpu", weights_only=False)
        m = WorkerMLP(ckpt["input_dim"], ckpt["num_actions"],
                      hidden_dims=ckpt["hidden_dims"], activation=ckpt["activation"])
    else:
        ckpt = torch.load(export_dir / "attention.pt", map_location="cpu", weights_only=False)
        m = WorkerAttention(ckpt["input_dim"], ckpt["num_actions"],
                            d_model=ckpt["d_model"], n_heads=ckpt["n_heads"],
                            n_layers=ckpt["n_layers"], dim_feedforward=ckpt["dim_feedforward"],
                            head_dims=ckpt["head_dims"])
    m.load_state_dict(ckpt["state_dict"])
    m.eval()
    return m.to(device)


def predict_proba(name: str, model, X: np.ndarray, device: str = "cpu") -> np.ndarray:
    if name == "baseline":
        return model.predict_proba(X)
    import torch
    import torch.nn.functional as F
    Xt = torch.as_tensor(X, dtype=torch.float32, device=device)
    with torch.no_grad():
        return F.softmax(model(Xt), dim=1).cpu().numpy()


def feature_importance(name: str, model) -> np.ndarray:
    """每特征重要性（归一化前）：baseline=|coef|均值，mlp=第一层权重列范数，
    attention=feature_embed 向量范数。"""
    if name == "baseline":
        return np.abs(model.weights).mean(axis=0)
    import torch
    if name == "mlp":
        return model.net[0].weight.detach().cpu().norm(dim=0).numpy()
    return model.feature_embed.detach().cpu().norm(dim=1).numpy()


def model_structure(name: str) -> dict:
    if name == "mlp":
        return {"kind": "mlp", "input": 41, "hidden": [128, 64], "output": 14,
                "activation": "ReLU", "dropout": 0.1}
    return {"kind": "attention", "input": 41, "d_model": 256, "n_heads": 4,
            "n_layers": 2, "dim_feedforward": 1024, "head": [128], "output": 14,
            "dropout": 0.2}


# ---------------------------------------------------------------------------
# 汇总计算
# ---------------------------------------------------------------------------
def compute_all(cfg: dict) -> dict:
    export_dir = ROOT / cfg["paths"]["export_dir"]
    device = "cpu"  # 小模型 + 一次性评估，CPU 稳定，避免 attention 全批 GPU OOM
    X, y = load_data(cfg)
    _, _, (X_te, y_te) = split(X, y, cfg["split"]["val_ratio"],
                                cfg["split"]["test_ratio"], cfg["data"]["seed"])
    feature_names = (ROOT / cfg["paths"]["processed_dir"] / "feature_names.txt") \
        .read_text(encoding="utf-8").splitlines()

    true_dist = np.bincount(y_te, minlength=NUM_ACTIONS).astype(float) / len(y_te)
    models = {}
    for name in MODEL_NAMES:
        model = load_model(name, export_dir, device)
        proba = predict_proba(name, model, X_te, device)
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
            "importance": feature_importance(name, model).tolist(),
            "structure": model_structure(name),
        }
        if name == "baseline":
            entry["coef"] = model.weights.tolist()  # (14, 41)
        models[name] = entry

    return {
        "actions": ACTIONS,
        "action_labels_zh": [ACTION_LABELS_ZH.get(a, a) for a in ACTIONS],
        "feature_names": feature_names,
        "true_dist": true_dist.tolist(),
        "n_test": int(len(y_te)),
        "models": models,
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
    actions = data["actions"]
    n_act = len(actions)

    # 1) 概览：acc / top3 / macro-F1
    fig, axes = plt.subplots(1, 3, figsize=(12, 3.6))
    metrics = [("acc", "Accuracy"), ("top3", "Top-3"), ("macro_f1", "Macro-F1")]
    for ax, (key, label) in zip(axes, metrics):
        vals = [models[m][key] for m in MODEL_NAMES]
        bars = ax.bar(MODEL_NAMES, vals, color=[CAT_LIGHT[m] for m in MODEL_NAMES],
                      width=0.62)
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
    for i, m in enumerate(MODEL_NAMES):
        rec = models[m]["recall"]
        ax.barh(y - (i - 1) * h, rec, height=h, color=CAT_LIGHT[m],
                label=MODEL_LABELS_ZH[m], zorder=3)
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

    # 3) 混淆矩阵（3 并排）
    fig, axes = plt.subplots(1, 3, figsize=(18, 6))
    for ax, m in zip(axes, MODEL_NAMES):
        cm = np.array(models[m]["cm"], dtype=float)
        cm = cm / cm.sum(axis=1, keepdims=True)
        im = ax.imshow(cm, cmap="Blues", vmin=0, vmax=1, aspect="auto")
        ax.set_title(MODEL_LABELS_ZH[m], color=INK_LIGHT, fontsize=12)
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

    # 4) baseline 权重热力图（diverging：蓝负红正）
    coef = np.array(models["baseline"]["coef"])
    fig, ax = plt.subplots(figsize=(16, 6))
    vmax = np.abs(coef).max()
    im = ax.imshow(coef, cmap="RdBu_r", vmin=-vmax, vmax=vmax, aspect="auto")
    ax.set_xticks(range(len(data["feature_names"])))
    ax.set_xticklabels(data["feature_names"], rotation=90, fontsize=6)
    ax.set_yticks(range(n_act)); ax.set_yticklabels(actions, fontsize=7)
    ax.set_title("Baseline utility weights (coef, action × feature)", fontsize=12)
    fig.colorbar(im, ax=ax, shrink=0.6)
    fig.tight_layout()
    fig.savefig(out_dir / "feature_weights_baseline.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)

    # 5) 特征重要性（top 15 特征，三模型对比）
    imp = np.array([models[m]["importance"] for m in MODEL_NAMES])  # (3, 41)
    mean_imp = imp.mean(axis=0)
    top_idx = np.argsort(mean_imp)[::-1][:15]
    fig, ax = plt.subplots(figsize=(9, 4.5))
    x = np.arange(len(top_idx))
    w = 0.26
    for i, m in enumerate(MODEL_NAMES):
        vals = imp[i][top_idx]
        vals = vals / vals.max() if vals.max() > 0 else vals
        ax.bar(x - (i - 1) * w, vals, width=w, color=CAT_LIGHT[m],
               label=MODEL_LABELS_ZH[m], zorder=3)
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

    # 6) 模型结构示意（mlp 左 / attention 右）
    fig, axes = plt.subplots(1, 2, figsize=(13, 5))
    for ax, m in zip(axes, MODEL_NAMES[1:]):  # 只画 mlp 与 attention
        ax.axis("off")
        st = models[m]["structure"]
        if st["kind"] == "mlp":
            layers = [("input", 41)] + [("h", h) for h in st["hidden"]] + [("out", 14)]
            _draw_fc(ax, layers)
            ax.set_title("MLP (ReLU + Dropout)", color=INK_LIGHT, fontsize=12)
        else:
            _draw_attention(ax, st)
            ax.set_title("FT-Transformer", color=INK_LIGHT, fontsize=12)
    fig.tight_layout()
    fig.savefig(out_dir / "model_architecture.png", dpi=150, bbox_inches="tight",
                facecolor=SURFACE_LIGHT)
    plt.close(fig)


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
    --b: {CAT_LIGHT['baseline']}; --m: {CAT_LIGHT['mlp']}; --a: {CAT_LIGHT['attention']};
    --ring: rgba(11,11,11,0.10);
  }}
  :root[data-theme="dark"] {{
    color-scheme: dark;
    --surface: {SURFACE_DARK}; --page: #0d0d0d;
    --ink: {INK_DARK}; --ink2: #c3c2b7; --muted: {MUTED};
    --grid: {GRID_DARK};
    --b: {CAT_DARK['baseline']}; --m: {CAT_DARK['mlp']}; --a: {CAT_DARK['attention']};
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
    <div class="sub">测试集 {data['n_test']} 条 · baseline / mlp / attention 三模型对比</div>
  </div>
  <button onclick="toggleTheme()">切换主题</button>
</header>

<div class="tooltip" id="tip"></div>

<section>
  <h2>1 · 概览指标</h2>
  <p class="desc">准确率、Top-3、KL 散度、Macro-F1（越低/越高方向见各卡）。</p>
  <div class="legend"><span><span class="sw" style="background:var(--b)"></span>效用函数</span>
    <span><span class="sw" style="background:var(--m)"></span>神经网络</span>
    <span><span class="sw" style="background:var(--a)"></span>注意力</span></div>
  <div class="cards" id="cards"></div>
</section>

<section>
  <h2>2 · 行为分布（真实 vs 预测）</h2>
  <p class="desc">Top-1 预测的边际分布是否贴合真实行为占比。</p>
  <div class="legend"><span><span class="sw" style="background:var(--muted)"></span>真实</span>
    <span><span class="sw" style="background:var(--b)"></span>效用函数</span>
    <span><span class="sw" style="background:var(--m)"></span>神经网络</span>
    <span><span class="sw" style="background:var(--a)"></span>注意力</span></div>
  <div id="dist"></div>
</section>

<section>
  <h2>3 · 每行为召回率（稀有类检测）</h2>
  <p class="desc">长尾标签下 accuracy 会被大类主导，召回率才能暴露稀有行为是否真正学到。</p>
  <div class="legend"><span><span class="sw" style="background:var(--b)"></span>效用函数</span>
    <span><span class="sw" style="background:var(--m)"></span>神经网络</span>
    <span><span class="sw" style="background:var(--a)"></span>注意力</span></div>
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
  <p class="desc">baseline 的线性效用权重（蓝负红正），与三模型每特征重要性（各自归一化，取均值排序 top 15）。</p>
  <div id="feat-heat"></div>
  <div id="feat-imp"></div>
</section>

<section>
  <h2>6 · 模型结构</h2>
  <p class="desc">前向架构示意（特征维度 → 隐藏层 → 14 行为 logits）。</p>
  <div id="struct"></div>
</section>

<script>
const D = {payload};
const MODELS = {["baseline", "mlp", "attention"]};
const COL = {{ baseline: "var(--b)", mlp: "var(--m)", attention: "var(--a)" }};
const tip = document.getElementById("tip");
function showTip(e, html) {{ tip.innerHTML = html; tip.style.display = "block";
  tip.style.left = (e.clientX + 12) + "px"; tip.style.top = (e.clientY + 12) + "px"; }}
function hideTip() {{ tip.style.display = "none"; }}
function pct(v) {{ return (v * 100).toFixed(1) + "%"; }}

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
    seg.style.left = (k * 33.3) + "%"; seg.style.width = (v * 33.3) + "%";
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
  const cellSize = "100%";
  box.innerHTML = `<div style="font-weight:600;margin-bottom:6px">${{m}}</div>`;
  const heat = document.createElement("div"); heat.className = "heat";
  heat.style.gridTemplateColumns = `repeat(${{n}}, 1fr)`;
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) {{
    const v = cm[i][j] / (cm[i].reduce((s, x) => s + x, 0) || 1);
    const cell = document.createElement("div"); cell.className = "cell";
    const c0 = [205, 226, 251], c1 = [24, 79, 149]; // light → dark blue (sequential)
    const cc = c0.map((a, idx) => Math.round(a + (c1[idx] - a) * v));
    cell.style.background = v > 0 ? `rgb(${{cc.join(",")}})` : "var(--grid)";
    cell.onmousemove = e => showTip(e, `${{D.actions[i]}} → ${{D.actions[j]}}<br>${{(v * 100).toFixed(1)}}%`);
    cell.onmouseleave = hideTip;
    heat.appendChild(cell);
  }}
  box.appendChild(heat); cmWrap.appendChild(box);
}});

// 5 特征权重（baseline coef 热力）+ 重要性
const fh = document.getElementById("feat-heat");
if (D.models.baseline.coef) {{
  const coef = D.models.baseline.coef;
  const nf = D.feature_names.length;
  const maxAbs = Math.max(...coef.flat().map(Math.abs));
  fh.innerHTML = `<h3 style="font-size:14px;margin:0 0 8px">baseline 效用权重（action × feature）</h3>`;
  const heat = document.createElement("div"); heat.className = "heat";
  heat.style.gridTemplateColumns = `repeat(${{nf}}, 1fr)`;
  for (let i = 0; i < D.actions.length; i++) for (let j = 0; j < nf; j++) {{
    const v = coef[i][j] / maxAbs; // -1..1
    const cell = document.createElement("div"); cell.className = "cell";
    cell.style.background = v >= 0
      ? `rgba(208,59,59,${{Math.abs(v).toFixed(2)}})`
      : `rgba(57,135,229,${{Math.abs(v).toFixed(2)}})`;
    cell.onmousemove = e => showTip(e, `${{D.actions[i]}} × ${{D.feature_names[j]}}<br>coef = ${{coef[i][j].toFixed(3)}}`);
    cell.onmouseleave = hideTip;
    heat.appendChild(cell);
  }}
  fh.appendChild(heat);
}}
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
for (const m of ["mlp", "attention"]) {{
  const s = D.models[m].structure;
  const box = document.createElement("div"); box.style.marginBottom = "18px";
  if (s.kind === "mlp") {{
    box.innerHTML = `<div style="font-weight:600;margin-bottom:8px">MLP（神经网络）</div>
      <div class="struct">${{node(`<b>input</b>41 特征`)}}
      <span class="arrow">→</span>${{node(`<b>Linear</b>128 · ReLU · Dropout`)}}
      <span class="arrow">→</span>${{node(`<b>Linear</b>64 · ReLU · Dropout`)}}
      <span class="arrow">→</span>${{node(`<b>Linear</b>14 logits`)}}</div>`;
  }} else {{
    box.innerHTML = `<div style="font-weight:600;margin-bottom:8px">FT-Transformer（注意力）</div>
      <div class="struct">${{node(`<b>input</b>41 特征`)}}
      <span class="arrow">→</span>${{node(`<b>feature_embed</b>每特征 → 256d`)}}
      <span class="arrow">→</span>${{node(`<b>[CLS] + 2×Encoder</b>4 heads · ff 1024`)}}
      <span class="arrow">→</span>${{node(`<b>MLP head</b>128 → 14 logits`)}}</div>`;
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
    cfg = yaml.safe_load((ROOT / "config" / "model_config.yaml").read_text(encoding="utf-8"))
    export_dir = ROOT / cfg["paths"]["export_dir"]
    viz_dir = export_dir / "viz"
    print(f"[visualize] 计算三模型指标 ...")
    data = compute_all(cfg)
    for m in MODEL_NAMES:
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
