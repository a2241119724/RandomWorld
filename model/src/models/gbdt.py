"""GBDT / RandomForest 策略模型（sklearn 后端），注册名 gbdt。

与 mlp/attention（TorchDecisionModel）不同：本类直接继承 ``DecisionModel``，
fit / save / load / export / structure 全由自己实现，不经过 torch 适配器。

选型理由（对比 mlp/attention 的「更适配」点）：
- 树模型的轴对齐阈值分裂能从极值训练点泛化到中间态——在 hungry∈{0,1} 上
  学到 ~0.5 阈值，测试 0.3 走低值分支、0.7 走高值分支，形成阶梯斜坡，
  而 MLP 需要内部点才能内插决策边界。
- 实测（纯极值 1000 条 + sample_proportions 物理拷贝）：HGB 概率分布更温和
  （KL 0.345 vs MLP 0.403、预测 gather 占比 74% vs 99%），对 Unity 侧
  「带概率决策门控」更友好；argmax 准确率 ≈ 主类基线（collapse 根因在
  数据侧——训练标签不含中间态斜坡，非模型缺陷）。
- RF 实测同样坍缩（gather 占 99.6%），仅作为 ``algorithm`` 可切换备选。

导出：树模型无法压平成 Linear 层，导出为自定义树结构 JSON
（experiments/gbdt_tree.json），C# 侧写树遍历推理（本类只定义契约，
Unity 接入属后续阶段）。零新增依赖（joblib 随 scikit-learn 自带）。
"""
from __future__ import annotations

import json

import joblib
import numpy as np
from sklearn.ensemble import (
    HistGradientBoostingClassifier as HGB,
    RandomForestClassifier as RF,
)
from sklearn.metrics import log_loss

from ..actions import ACTIONS, NUM_ACTIONS
from ..training import accuracy, oversample_by_proportions
from ..unity_export import _processed_dir, load_feature_names
from .base import DecisionModel
from .registry import register


@register("gbdt")
class GbdtModel(DecisionModel):
    """GBDT 的 DecisionModel 适配（注册名 "gbdt"，algorithm 可切 random_forest）。"""

    filename = "gbdt.joblib"
    section = "gbdt"

    # ---- 构造 ----
    @classmethod
    def from_config(cls, cfg, input_dim: int, num_actions: int,
                    seed: int | None = None):
        s = cfg[cls.section]
        inst = cls()
        inst._cfg = cfg
        inst._params = {"input_dim": input_dim, "num_actions": num_actions}
        inst._hp = {
            "algorithm": s["algorithm"],
            "max_iter": s["max_iter"],
            "learning_rate": s["learning_rate"],
            "max_leaf_nodes": s["max_leaf_nodes"],
            "min_samples_leaf": s["min_samples_leaf"],
            "l2_regularization": s["l2_regularization"],
            "early_stopping": s["early_stopping"],
            "patience": s["patience"],
            "seed": seed,
        }
        inst.clf = None
        return inst

    # ---- 训练：复用 oversample_by_proportions，与 torch 完全同分布 ----
    def fit(self, X_tr, y_tr, X_va, y_va, X_te, y_te, cfg, seed: int) -> dict:
        sp = cfg["training"].get("sample_proportions")
        Xo, yo = (oversample_by_proportions(X_tr, y_tr, sp) if sp else (X_tr, y_tr))

        if self._hp["algorithm"] == "random_forest":
            clf = self._build(self._hp["max_iter"])
            clf.fit(Xo, yo)
        elif not self._hp["early_stopping"]:
            clf = self._build(self._hp["max_iter"])
            clf.fit(Xo, yo)
        else:
            # 不用 HGB 内置 early_stopping：它对 oversample 后的单样本类
            # （store=1 / pickup=2 物理拷贝）触发 train_test_split(stratify)
            # ValueError。改为 warm_start 续训 + val_logloss 外部早停，
            # 语义对齐 torch 的「早停保留最优」。
            step = max(1, self._hp["max_iter"] // 10)
            clf = self._build(step)
            clf.fit(Xo, yo)
            _labels = range(NUM_ACTIONS)  # val 集只含部分类，显式对齐 14 列
            best = log_loss(y_va, self._proba_full(clf, X_va), labels=_labels)
            best_iter = step
            bad = 0
            cur = step
            while cur < self._hp["max_iter"]:
                cur = min(cur + step, self._hp["max_iter"])
                clf.set_params(max_iter=cur)
                clf.fit(Xo, yo)
                v = log_loss(y_va, self._proba_full(clf, X_va), labels=_labels)
                if v < best - 1e-4:
                    best, best_iter = v, cur
                    bad = 0
                else:
                    bad += 1
                    if bad >= self._hp["patience"]:
                        break
            if best_iter != cur:  # 重新拟合到最优迭代（确定性，同 seed 可复现）
                clf = self._build(best_iter)
                clf.fit(Xo, yo)
        self.clf = clf

        pva = self.predict_proba(X_va)
        pte = self.predict_proba(X_te)
        return {
            "val_loss": float(log_loss(y_va, pva, labels=range(NUM_ACTIONS))),
            "val_acc": float(accuracy(y_va, pva.argmax(axis=1))),
            "test_acc": float(accuracy(y_te, pte.argmax(axis=1))),
        }

    def _build(self, max_iter):
        kw = dict(max_leaf_nodes=self._hp["max_leaf_nodes"],
                  min_samples_leaf=self._hp["min_samples_leaf"],
                  random_state=self._hp["seed"])
        if self._hp["algorithm"] == "random_forest":
            return RF(n_estimators=max_iter, **kw)  # max_iter → n_estimators
        return HGB(max_iter=max_iter,
                   learning_rate=self._hp["learning_rate"],
                   l2_regularization=self._hp["l2_regularization"],
                   warm_start=True, early_stopping=False, **kw)

    # ---- 推理：重映射回 14 列（sklearn 只给观测类，缺失类恒 0）----
    @staticmethod
    def _proba_full(clf, X) -> np.ndarray:
        p = clf.predict_proba(X)
        full = np.zeros((X.shape[0], NUM_ACTIONS), dtype=np.float32)
        full[:, clf.classes_] = p  # 关键：列对齐到原始行为索引
        return full

    def predict_proba(self, X) -> np.ndarray:
        return self._proba_full(self.clf, X)

    def predict(self, X) -> np.ndarray:
        return self.predict_proba(X).argmax(axis=1)

    # ---- 持久化 ----
    def save(self, export_dir, cfg, seed: int):
        export_dir.mkdir(parents=True, exist_ok=True)
        payload = {"clf": self.clf, "input_dim": self._params["input_dim"],
                   "num_actions": self._params["num_actions"], "hp": self._hp}
        path = export_dir / self.filename
        joblib.dump(payload, path)
        return path

    @classmethod
    def load(cls, export_dir, cfg=None, device="auto"):
        path = export_dir / cls.filename
        if not path.exists():
            return None
        payload = joblib.load(path)
        inst = cls()
        inst.clf = payload["clf"]
        inst._params = {"input_dim": payload["input_dim"],
                        "num_actions": payload["num_actions"]}
        inst._hp = payload["hp"]
        return inst

    # ---- 导出：自定义树结构 JSON（零新依赖）----
    def export(self, export_dir, cfg):
        export_dir.mkdir(parents=True, exist_ok=True)
        feature_names = load_feature_names(_processed_dir(cfg))
        payload = (self._export_hgb(feature_names) if isinstance(self.clf, HGB)
                   else self._export_rf(feature_names))
        path = export_dir / f"{self.name}_tree.json"
        path.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
        return [path]

    def _export_hgb(self, feature_names) -> dict:
        """HGB 树结构。C# 推理：score[c] = baseline[c] + Σ_i WalkTree(tree[i][c]) → softmax。

        注意：**不再乘 learning_rate**——sklearn HGB 的叶值在训练时已含 lr 缩放
        （_raw_predict 直接累加叶值），导出 JSON 里的 learning_rate 仅作元数据。
        """
        clf = self.clf
        trees = []
        for i in range(len(clf._predictors)):  # 每轮每类一棵树
            class_trees = []
            for c in range(len(clf.classes_)):
                n = clf._predictors[i][c].nodes
                class_trees.append({
                    "feature": [int(x) for x in n["feature_idx"]],
                    "threshold": [float(x) for x in n["num_threshold"]],
                    "left": [int(x) for x in n["left"]],
                    "right": [int(x) for x in n["right"]],
                    "value": [float(x) for x in n["value"]],
                    "is_leaf": [bool(x) for x in n["is_leaf"]],
                })
            trees.append(class_trees)
        return {
            "model": self.name,
            "input_dim": self._params["input_dim"],
            "num_actions": self._params["num_actions"],
            "algorithm": "hist_gradient_boosting",
            "learning_rate": float(clf.learning_rate),
            "baseline_prediction": np.asarray(clf._baseline_prediction).ravel().tolist(),
            "classes": clf.classes_.tolist(),
            "n_iterations": len(trees),
            "trees": trees,
            "action_names": ACTIONS,
            "feature_names": feature_names,
        }

    def _export_rf(self, feature_names) -> dict:
        """RF 树结构。叶子 value 直接是各类概率向量（每棵树的 votes）。"""
        clf = self.clf
        trees = []
        for t in clf.estimators_:
            tt = t.tree_
            trees.append({
                "feature": [int(x) for x in tt.feature],
                "threshold": [float(x) for x in tt.threshold],
                "left": [int(x) for x in tt.children_left],
                "right": [int(x) for x in tt.children_right],
                "value": [[float(v) for v in row] for row in tt.value[:, 0, :]],
            })
        return {
            "model": self.name,
            "input_dim": self._params["input_dim"],
            "num_actions": self._params["num_actions"],
            "algorithm": "random_forest",
            "n_estimators": len(trees),
            "trees": trees,
            "classes": clf.classes_.tolist(),
            "action_names": ACTIONS,
            "feature_names": feature_names,
        }

    # ---- 可视化 ----
    def feature_importance(self) -> np.ndarray:
        clf = self.clf
        if isinstance(clf, RF):  # 原生 feature_importances_
            return clf.feature_importances_.astype(np.float32)
        # HGB 无原生重要性：节点 gain 求和归一化
        gains = np.zeros(self._params["input_dim"], dtype=np.float64)
        for i in range(len(clf._predictors)):
            for c in range(len(clf.classes_)):
                n = clf._predictors[i][c].nodes
                for j in range(n.shape[0]):
                    if not n["is_leaf"][j] and n["feature_idx"][j] >= 0:
                        gains[n["feature_idx"][j]] += n["gain"][j]
        s = gains.sum()
        return (gains / s if s > 0 else gains).astype(np.float32)

    def structure(self) -> dict:
        clf = self.clf
        st = {
            "kind": "gbdt",
            "input": self._params["input_dim"],
            "output": self._params["num_actions"],
            "algorithm": type(clf).__name__ if clf is not None else self._hp["algorithm"],
            "learning_rate": self._hp.get("learning_rate"),
            "max_leaf_nodes": self._hp.get("max_leaf_nodes"),
            "max_iter": self._hp.get("max_iter"),
            "observed_classes": clf.classes_.tolist() if clf is not None else [],
        }
        if clf is not None:
            st["n_iterations"] = (len(clf._predictors) if isinstance(clf, HGB)
                                  else len(clf.estimators_))
        return st
