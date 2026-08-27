#!/usr/bin/env -S deno run --allow-env --allow-read --allow-write --allow-net

/**
 * Generate game-asset images using domestic (Chinese) image models.
 *
 * Two providers, chosen by cost/consistency:
 *   - siliconflow (OpenAI-compatible): cheap batch (Z-Image-Turbo / Kolors / Qwen-Image)
 *   - ark (Volcengine Seedream 5.0 Lite): reference-image consistency (characters, frame sequences)
 *
 * Usage:
 *   deno run --allow-env --allow-read --allow-write --allow-net generate.ts \
 *     --prompt "description of the image" \
 *     [--ref path/to/reference1.png [--ref path/to/reference2.png]] \
 *     --output-dir ./output \
 *     [--provider auto|siliconflow|ark] \
 *     [--model "MODEL_ID"] \
 *     [--category item|map|effect|character|ui] \
 *     [--variants 1] \
 *     [--aspect "1:1"] \
 *     [--size "2K"] \
 *     [--negative "unwanted things"]
 *
 * Requires SILICONFLOW_API_KEY and/or ARK_API_KEY environment variables.
 */

import { parseArgs } from "jsr:@std/cli/parse-args";
import { encodeBase64 } from "jsr:@std/encoding/base64";
import { ensureDir } from "jsr:@std/fs/ensure-dir";
import { join } from "jsr:@std/path";
import { load } from "jsr:@std/dotenv";

// ---------------------------------------------------------------------------
// Model registry (editable — also drives cost estimates)
// ---------------------------------------------------------------------------
const MODELS = {
  siliconflow: {
    zImageTurbo: { id: "Tongyi-MAI/Z-Image-Turbo", cost: 0.005, steps: 20 }, // cheapest, batch props/icons
    kolors: { id: "Kwai-Kolors/Kolors", cost: 0, steps: 30 }, // free trial channel
    qwenImage: { id: "Qwen/Qwen-Image", cost: 0.042, steps: 50 }, // quality + text rendering
    qwenImageEdit: { id: "Qwen/Qwen-Image-Edit-2509", cost: 0.042, steps: 50 }, // img2img (single ref)
  },
  ark: {
    seedream5Lite: { id: "doubao-seedream-5-0-lite-260128", cost: 0.035, steps: 0 }, // ref-image consistency
  },
} as const;

type ProviderName = "siliconflow" | "ark";

// Per-aspect sizes. Qwen-Image uses its recommended sizes; cheaper models use a 1024 base.
const QWEN_SIZES: Record<string, string> = {
  "1:1": "1328x1328",
  "3:4": "1140x1472",
  "4:3": "1472x1140",
  "9:16": "928x1664",
  "16:9": "1664x928",
  "2:3": "1056x1584",
  "3:2": "1584x1056",
};
const BASE_SIZES: Record<string, string> = {
  "1:1": "1024x1024",
  "3:4": "896x1152",
  "4:3": "1152x896",
  "9:16": "768x1344",
  "16:9": "1344x768",
  "2:3": "832x1248",
  "3:2": "1248x832",
};

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------
interface RefImage {
  mimeType: string;
  data: string; // base64
}

interface GeneratedImage {
  mimeType: string;
  bytes: Uint8Array;
}

interface GenRequest {
  prompt: string;
  refs: RefImage[];
  aspect: string;
  size: string;
}

interface ImageProvider {
  name: ProviderName;
  model: string;
  generate(req: GenRequest): Promise<GeneratedImage[]>;
}

// ---------------------------------------------------------------------------
// Auto-load .env from CWD (won't override existing env vars)
// ---------------------------------------------------------------------------
try {
  const env = await load();
  for (const [key, value] of Object.entries(env)) {
    if (!Deno.env.get(key)) {
      Deno.env.set(key, value);
    }
  }
} catch {
  // .env not found — that's fine, key may be in environment already
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------
const args = parseArgs(Deno.args, {
  string: [
    "prompt",
    "output-dir",
    "aspect",
    "size",
    "variants",
    "provider",
    "model",
    "negative",
    "category",
  ],
  collect: ["ref"],
  default: {
    "output-dir": "./generated",
    aspect: "1:1",
    size: "2K",
    variants: "1",
    provider: "auto",
  },
});

const prompt = args.prompt as string;
const refPaths = (args.ref as string[]) || [];
const outputDir = args["output-dir"] as string;
const aspect = args.aspect as string;
const imageSize = args.size as string;
const numVariants = parseInt(args.variants as string || "1", 10);
const providerArg = args.provider as string;
const modelArg = args.model as string;
const negative = args.negative as string;
const category = args.category as string;

if (!prompt) {
  console.error("Error: --prompt is required");
  Deno.exit(1);
}

// ---------------------------------------------------------------------------
// Reference images → base64
// ---------------------------------------------------------------------------
const hasRef = refPaths.length > 0;
const refImages: RefImage[] = hasRef
  ? await Promise.all(
      refPaths.map(async (path) => {
        const bytes = await Deno.readFile(path);
        const ext = path.toLowerCase().split(".").pop();
        const mimeType =
          ext === "jpg" || ext === "jpeg"
            ? "image/jpeg"
            : ext === "webp"
            ? "image/webp"
            : "image/png";
        return { mimeType, data: encodeBase64(bytes) };
      })
    )
  : [];

// ---------------------------------------------------------------------------
// Model selection (dual-track: cheap batch vs Seedream consistency)
// ---------------------------------------------------------------------------
function selectModel(opts: {
  providerArg: string;
  modelArg: string;
  category: string;
  hasRef: boolean;
}): { provider: ProviderName; model: string } {
  const hasSiliconflowKey = !!Deno.env.get("SILICONFLOW_API_KEY");
  const hasArkKey = !!Deno.env.get("ARK_API_KEY");

  // Explicit model override wins; provider inferred unless stated.
  if (opts.modelArg) {
    const isArk =
      opts.modelArg.includes("doubao-seedream") || opts.providerArg === "ark";
    return { provider: isArk ? "ark" : "siliconflow", model: opts.modelArg };
  }

  if (opts.providerArg === "siliconflow") {
    return {
      provider: "siliconflow",
      model: opts.hasRef
        ? MODELS.siliconflow.qwenImageEdit.id
        : MODELS.siliconflow.zImageTurbo.id,
    };
  }
  if (opts.providerArg === "ark") {
    return { provider: "ark", model: MODELS.ark.seedream5Lite.id };
  }

  // auto
  if (opts.hasRef) {
    // Reference consistency → Seedream (Ark) when available; else the only SiliconFlow img2img model.
    return hasArkKey
      ? { provider: "ark", model: MODELS.ark.seedream5Lite.id }
      : { provider: "siliconflow", model: MODELS.siliconflow.qwenImageEdit.id };
  }
  if (hasSiliconflowKey) {
    const isQualityCategory = opts.category === "map" || opts.category === "effect";
    return {
      provider: "siliconflow",
      model: isQualityCategory
        ? MODELS.siliconflow.qwenImage.id
        : MODELS.siliconflow.zImageTurbo.id,
    };
  }
  if (hasArkKey) {
    return { provider: "ark", model: MODELS.ark.seedream5Lite.id };
  }

  console.error(
    "Error: no API key found. Set SILICONFLOW_API_KEY (https://cloud.siliconflow.cn) and/or ARK_API_KEY (https://console.volcengine.com/ark)."
  );
  Deno.exit(1);
}

const selected = selectModel({ providerArg, modelArg, category, hasRef });

if (selected.provider === "siliconflow" && refImages.length > 1) {
  console.warn(
    "Warning: SiliconFlow 图生图 (Qwen-Image-Edit) 仅支持单张参考图，将只使用第一张。多参考图一致性请用 --provider ark (Seedream)。"
  );
  refImages.length = 1;
}

const apiKeyEnv =
  selected.provider === "ark" ? "ARK_API_KEY" : "SILICONFLOW_API_KEY";
const apiKey = Deno.env.get(apiKeyEnv);
if (!apiKey) {
  console.error(`Error: ${apiKeyEnv} environment variable is not set`);
  console.error(
    selected.provider === "ark"
      ? "Get one at https://console.volcengine.com/ark"
      : "Get one at https://cloud.siliconflow.cn"
  );
  Deno.exit(1);
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function base64ToBytes(b64: string): Uint8Array {
  const binary = atob(b64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

function toDataUri(img: RefImage): string {
  return `data:${img.mimeType};base64,${img.data}`;
}

// Normalize aspect "A:B" so the image area ≈ (baseSize)^2.
function resolveArkSize(size: string, aspect: string): string {
  const [wr, hr] = aspect.split(":").map(Number);
  const base = size === "4K" ? 4096 : 2048; // Seedream Lite has no 1K tier; min 2K
  const areaRoot = base / Math.sqrt(wr * hr);
  return `${Math.round(areaRoot * wr)}x${Math.round(areaRoot * hr)}`;
}

function costPerImage(modelId: string): number {
  for (const group of Object.values(MODELS)) {
    for (const m of Object.values(group)) {
      if (m.id === modelId) return m.cost;
    }
  }
  return 0.01; // unknown explicit model — assume a nominal cost
}

// ---------------------------------------------------------------------------
// Providers (raw fetch — no SDK)
// ---------------------------------------------------------------------------
class SiliconFlowProvider implements ImageProvider {
  name = "siliconflow" as const;
  constructor(
    readonly model: string,
    private apiKey: string,
    private negative: string
  ) {}

  async generate(req: GenRequest): Promise<GeneratedImage[]> {
    const isTurboOrFree = this.model === MODELS.siliconflow.zImageTurbo.id ||
      this.model === MODELS.siliconflow.kolors.id;
    const sizes = isTurboOrFree ? BASE_SIZES : QWEN_SIZES;
    const steps =
      this.model === MODELS.siliconflow.zImageTurbo.id ? 20
      : this.model === MODELS.siliconflow.kolors.id ? 30
      : 50;

    const body: Record<string, unknown> = {
      model: this.model,
      prompt: req.prompt,
      image_size: sizes[req.aspect] ?? BASE_SIZES["1:1"],
      num_inference_steps: steps,
      cfg: 4.0,
      seed: Math.floor(Math.random() * 1_000_000_000),
      response_format: "b64_json",
      extra_body: { enable_image_base64: true },
    };
    if (this.negative) body["negative_prompt"] = this.negative;
    if (req.refs.length > 0 && this.model === MODELS.siliconflow.qwenImageEdit.id) {
      body["image"] = toDataUri(req.refs[0]); // img2img: single reference only
    }

    const res = await fetch("https://api.siliconflow.cn/v1/images/generations", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${this.apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`SiliconFlow HTTP ${res.status}: ${text}`);
    }
    return this.parseImages(await res.json());
  }

  private async parseImages(json: Record<string, unknown>): Promise<GeneratedImage[]> {
    const images = (json.images as { url?: string; b64_json?: string; data?: string }[]) || [];
    const out: GeneratedImage[] = [];
    for (const img of images) {
      if (img.b64_json) {
        out.push({ mimeType: "image/png", bytes: base64ToBytes(img.b64_json) });
      } else if (img.data) {
        out.push({ mimeType: "image/png", bytes: base64ToBytes(img.data) });
      } else if (img.url) {
        const bytes = await fetchImageBytes(img.url);
        out.push({ mimeType: "image/png", bytes });
      } else {
        throw new Error("SiliconFlow response image has no b64/url field");
      }
    }
    return out;
  }
}

class ArkProvider implements ImageProvider {
  name = "ark" as const;
  constructor(
    readonly model: string,
    private apiKey: string
  ) {}

  async generate(req: GenRequest): Promise<GeneratedImage[]> {
    const body: Record<string, unknown> = {
      model: this.model,
      prompt: req.prompt,
      size: resolveArkSize(req.size, req.aspect),
      response_format: "b64_json",
      output_format: "png",
      watermark: false,
    };
    if (req.refs.length === 1) {
      body["image"] = toDataUri(req.refs[0]);
    } else if (req.refs.length > 1) {
      body["image"] = req.refs.map(toDataUri); // multi-reference: array of data URIs
    }

    const res = await fetch("https://ark.cn-beijing.volces.com/api/v3/images/generations", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${this.apiKey}`, // space after Bearer is required
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Ark HTTP ${res.status}: ${text}`);
    }
    return this.parseImages(await res.json());
  }

  private async parseImages(json: Record<string, unknown>): Promise<GeneratedImage[]> {
    const data = (json.data as { b64_json?: string; url?: string }[]) || [];
    const out: GeneratedImage[] = [];
    for (const item of data) {
      if (item.b64_json) {
        out.push({ mimeType: "image/png", bytes: base64ToBytes(item.b64_json) });
      } else if (item.url) {
        const bytes = await fetchImageBytes(item.url);
        out.push({ mimeType: "image/png", bytes });
      } else {
        throw new Error("Ark response image has no b64_json/url field");
      }
    }
    return out;
  }
}

async function fetchImageBytes(url: string): Promise<Uint8Array> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Image download HTTP ${res.status}`);
  return new Uint8Array(await res.arrayBuffer());
}

// ---------------------------------------------------------------------------
// Generate
// ---------------------------------------------------------------------------
console.log(`Generating ${numVariants} variation(s) — provider=${selected.provider} model=${selected.model}`);
console.log(`Prompt: ${prompt.substring(0, 100)}${prompt.length > 100 ? "..." : ""}`);

const provider: ImageProvider =
  selected.provider === "ark"
    ? new ArkProvider(selected.model, apiKey)
    : new SiliconFlowProvider(selected.model, apiKey, negative);

const results = await Promise.allSettled(
  Array.from({ length: numVariants }, (_, i) =>
    provider
      .generate({ prompt, refs: refImages, aspect, size: imageSize })
      .then((images) => ({ index: i, images }))
  )
);

// Save successful results
await ensureDir(outputDir);
const timestamp = new Date().toISOString().replace(/[:.]/g, "-").slice(0, 19);
let savedCount = 0;
const savedFiles: string[] = [];

for (const result of results) {
  if (result.status === "rejected") {
    console.error(`Variant failed: ${result.reason}`);
    continue;
  }
  const { index, images } = result.value;
  if (images.length === 0) {
    console.error(`Variant ${index + 1}: no images in response`);
    continue;
  }
  for (const img of images) {
    const ext = img.mimeType === "image/jpeg" ? "jpg" : "png";
    const filename = `variant-${index + 1}-${timestamp}.${ext}`;
    const filepath = join(outputDir, filename);
    await Deno.writeFile(filepath, img.bytes);
    savedFiles.push(filepath);
    savedCount++;
    console.log(`Saved: ${filepath}`);
  }
}

console.log(
  `\nDone: ${savedCount}/${numVariants} variants saved to ${outputDir}`
);

// Output JSON summary for programmatic use
const summary = {
  provider: selected.provider,
  model: selected.model,
  prompt,
  references: refPaths,
  variants_requested: numVariants,
  variants_saved: savedCount,
  output_dir: outputDir,
  files: savedFiles,
  settings: {
    aspect,
    imageSize,
    category: category || undefined,
    negative: negative || undefined,
  },
  estimated_cost: `$${(costPerImage(selected.model) * savedCount).toFixed(4)}`,
};
console.log("\n" + JSON.stringify(summary, null, 2));
