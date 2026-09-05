namespace LAB2D.AI.Worker
{
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Worker 行为决策 MLP 的纯 C# 推理层。
    ///
    /// 从 <c>Resources/model/mlp_weights.bytes</c> 加载权重（由 <c>model/src/unity_export.py</c>
    /// 的 <c>write_binary_bytes</c> 导出的扁平小端 float32 二进制），手写前向传播，
    /// 隐藏层 ReLU、输出层保持 logits，取 argmax 作为行为索引。
    /// 层维度不硬编码：从 .bytes 头逐层读取，hidden_dims 变化无需改 C#。
    /// 当前模型为 5 层（41 → 64 → 64 → 64 → 32 → 14）。
    ///
    /// 零第三方依赖，不继承 MonoBehaviour，可在测试中独立实例化。加载失败时
    /// <see cref="IsLoaded"/> 为 false，调用方（WorkerBrain）回退到硬规则决策。
    /// </summary>
    public class WorkerModelInference
    {
        /// <summary>Resources 下的权重路径（不含扩展名，.bytes 会自动映射为 TextAsset）。</summary>
        private const string ResourcePath = "model/mlp_weights";

        /// <summary>隐藏层激活函数（与训练时 model_config 的 activation=relu 一致）。</summary>
        private const bool UseReLU = true;

        /// <summary>模型输入维度（特征向量长度，须与 feature_schema.yaml 对齐）。</summary>
        public int InputDim { get; private set; }

        /// <summary>行为类别数（须与 WorkerDecisionType 枚举数量一致 = 14）。</summary>
        public int NumActions { get; private set; }

        /// <summary>权重是否已成功加载并可推理。</summary>
        public bool IsLoaded { get; private set; }

        private int[] layerOutDims;
        private int[] layerInDims;
        private float[][] layerWeights; // 每层 (out × in) 行主序扁平数组，第 o 行 = 第 o 个输出神经元的权重
        private float[][] layerBiases;  // 每层 (out)

        /// <summary>每层输出复用 buffer（Load 成功后按 layerOutDims 一次性分配）：
        /// 推理热路径（~40 次/秒 × 5 层）原来每层 new float[]，~1KB/次推理的稳定分配
        ///（100 Worker 卡顿治理 2026-09-06）。单例主线程调用；PredictLogits 返回值是
        /// 内部 buffer 引用，调用方只读且用完即弃（Editor 调试窗口每次显示重新调用刷新）。</summary>
        private float[][] layerBuffers;

        private static WorkerModelInference instance;

        /// <summary>进程内单例（懒加载：首次访问时从 Resources 读权重）。</summary>
        public static WorkerModelInference Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WorkerModelInference();
                    instance.Load();
                }
                return instance;
            }
        }

        /// <summary>
        /// 从 Resources 加载二进制权重。
        /// 布局（与 Python _export_mlp_binary 对应，全部小端）：
        ///     int32 num_layers
        ///     每层：int32 out_dim, int32 in_dim, float32[out_dim*in_dim] W, float32[out_dim] b
        /// </summary>
        public void Load()
        {
            this.IsLoaded = false;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
            {
                AWorkerTask.LogProvider(
                    $"[ModelDiag] 未找到模型权重 {ResourcePath}.bytes，Worker 回退硬规则决策",
                    LogManager.LogLevelEnum.Warning);
                return;
            }

            try
            {
                using MemoryStream ms = new MemoryStream(asset.bytes);
                using BinaryReader reader = new BinaryReader(ms);

                int numLayers = reader.ReadInt32();
                if (numLayers <= 0 || numLayers > 32)
                {
                    AWorkerTask.LogProvider(
                        $"[ModelDiag] 模型层数异常: {numLayers}",
                        LogManager.LogLevelEnum.Error);
                    return;
                }

                this.layerOutDims = new int[numLayers];
                this.layerInDims = new int[numLayers];
                this.layerWeights = new float[numLayers][];
                this.layerBiases = new float[numLayers][];

                for (int l = 0; l < numLayers; l++)
                {
                    int outDim = reader.ReadInt32();
                    int inDim = reader.ReadInt32();
                    if (outDim <= 0 || inDim <= 0)
                    {
                        AWorkerTask.LogProvider(
                            $"[ModelDiag] 模型第{l}层维度异常: out={outDim} in={inDim}",
                            LogManager.LogLevelEnum.Error);
                        return;
                    }

                    this.layerOutDims[l] = outDim;
                    this.layerInDims[l] = inDim;

                    float[] w = new float[outDim * inDim];
                    for (int i = 0; i < w.Length; i++)
                    {
                        w[i] = reader.ReadSingle();
                    }

                    float[] b = new float[outDim];
                    for (int i = 0; i < outDim; i++)
                    {
                        b[i] = reader.ReadSingle();
                    }

                    this.layerWeights[l] = w;
                    this.layerBiases[l] = b;
                }

                this.InputDim = this.layerInDims[0];
                this.NumActions = this.layerOutDims[numLayers - 1];

                // 按层输出维度一次性分配推理复用 buffer（见字段注释）
                this.layerBuffers = new float[numLayers][];
                for (int l = 0; l < numLayers; l++)
                {
                    this.layerBuffers[l] = new float[this.layerOutDims[l]];
                }

                this.IsLoaded = true;

                AWorkerTask.LogProvider(
                    $"[ModelDiag] 模型加载成功: {numLayers}层 输入{this.InputDim}维 输出{this.NumActions}类",
                    LogManager.LogLevelEnum.Debug);
            }
            catch (System.Exception e)
            {
                AWorkerTask.LogProvider(
                    $"[ModelDiag] 模型权重解析失败: {e.Message}，Worker 回退硬规则决策",
                    LogManager.LogLevelEnum.Error);
                this.IsLoaded = false;
            }
        }

        /// <summary>
        /// 前向传播：特征向量 → 14 维 logits（未归一化的原始输出，argmax 即行为索引）。
        /// 返回 null 表示模型未加载或输入维度不匹配。
        /// </summary>
        public float[] PredictLogits(float[] features)
        {
            if (!this.IsLoaded || features == null || features.Length != this.InputDim)
            {
                return null;
            }

            float[] x = features;

            int numLayers = this.layerWeights.Length;
            for (int l = 0; l < numLayers; l++)
            {
                int outDim = this.layerOutDims[l];
                int inDim = this.layerInDims[l];
                float[] w = this.layerWeights[l];
                float[] b = this.layerBiases[l];

                // 复用 Load 时按层分配的 buffer（见 layerBuffers 字段注释）：前向传播零分配
                float[] y = this.layerBuffers[l];
                for (int o = 0; o < outDim; o++)
                {
                    float sum = b[o];
                    int rowOffset = o * inDim;
                    for (int i = 0; i < inDim; i++)
                    {
                        sum += w[rowOffset + i] * x[i];
                    }
                    y[o] = sum;
                }

                // 隐藏层应用 ReLU，最后一层（输出层）保持原始 logits
                if (UseReLU && l < numLayers - 1)
                {
                    for (int o = 0; o < outDim; o++)
                    {
                        if (y[o] < 0f) y[o] = 0f;
                    }
                }

                x = y;
            }

            return x;
        }

        /// <summary>
        /// 前向传播并返回 argmax 行为索引（对应 <see cref="WorkerDecisionType"/> 的枚举顺序）。
        /// 返回 -1 表示模型未加载或输入维度不匹配。
        /// </summary>
        public int PredictActionIndex(float[] features)
        {
            float[] logits = this.PredictLogits(features);
            if (logits == null || logits.Length == 0)
            {
                return -1;
            }

            int best = 0;
            for (int i = 1; i < logits.Length; i++)
            {
                if (logits[i] > logits[best])
                {
                    best = i;
                }
            }
            return best;
        }
    }
}
