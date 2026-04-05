import fs from "node:fs";
import https from "node:https";
import path from "node:path";

const repoRoot = process.argv[2];
const outputPath = process.argv[3];

if (!repoRoot || !outputPath) {
  console.error("Usage: node describe_items.mjs <repoRoot> <outputPath>");
  process.exit(2);
}

const env = readEnv(path.join(repoRoot, ".env"));
const apiKey = (env.CAPTCHA_OPENAI_API_KEY || "").trim();
const model = (env.CAPTCHA_OPENAI_RETRY_MODEL || env.CAPTCHA_OPENAI_MODEL || "gpt-5").trim();
const itemsDir = path.join(repoRoot, "Items");
const itemFiles = fs.readdirSync(itemsDir)
  .filter((name) => name.toLowerCase().endsWith(".webp"))
  .sort((left, right) => left.localeCompare(right));

if (!apiKey) {
  console.error("CAPTCHA_OPENAI_API_KEY is missing.");
  process.exit(1);
}

const items = [];
for (const fileName of itemFiles) {
  const fullPath = path.join(itemsDir, fileName);
  const imageBytes = fs.readFileSync(fullPath);
  const base64 = imageBytes.toString("base64");
  const mediaType = detectMediaType(imageBytes);
  const itemName = path.basename(fileName, path.extname(fileName));
  const response = await describeItem(apiKey, model, itemName, mediaType, base64);
  items.push(response);
  console.error(`described ${itemName}`);
}

fs.writeFileSync(outputPath, JSON.stringify(items, null, 2) + "\n", "utf8");
console.log(outputPath);

function describeItem(apiKey, model, itemName, mediaType, base64) {
  const body = {
    model,
    messages: [
      {
        role: "system",
        content: [
          {
            type: "text",
            text:
              "You are describing game item icons for an Epic RPG captcha solver. " +
              "Focus on silhouette, geometry, countable parts, negative space, tip shapes, curvature, symmetry, and internal bright/dark regions that still help in grayscale or black-and-white images. " +
              "Do not rely on color names unless unavoidable."
          }
        ]
      },
      {
        role: "user",
        content: [
          {
            type: "text",
            text:
              `Item name: ${itemName}\n` +
              "Describe this icon so a vision model can tell it apart from similar items even if the captcha is grayscale. " +
              "Keep each field concise, factual, and based on the visible shape."
          },
          {
            type: "image_url",
            image_url: {
              url: `data:${mediaType};base64,${base64}`,
              detail: "high"
            }
          }
        ]
      }
    ],
    response_format: {
      type: "json_schema",
      json_schema: {
        name: "item_description",
        strict: true,
        schema: {
          type: "object",
          additionalProperties: false,
          properties: {
            name: { type: "string" },
            outline: { type: "string" },
            grayscale_cues: { type: "string" },
            disambiguation: { type: "string" }
          },
          required: ["name", "outline", "grayscale_cues", "disambiguation"]
        }
      }
    }
  };

  return postJson(apiKey, body);
}

function postJson(apiKey, body) {
  const requestBody = JSON.stringify(body);
  return new Promise((resolve, reject) => {
    const request = https.request(
      "https://api.openai.com/v1/chat/completions",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(requestBody),
          Authorization: `Bearer ${apiKey}`
        }
      },
      (response) => {
        let raw = "";
        response.setEncoding("utf8");
        response.on("data", (chunk) => {
          raw += chunk;
        });
        response.on("end", () => {
          if (response.statusCode < 200 || response.statusCode >= 300) {
            reject(new Error(`status=${response.statusCode} body=${raw}`));
            return;
          }

          const parsed = JSON.parse(raw);
          const content = parsed?.choices?.[0]?.message?.content || "";
          resolve(JSON.parse(content));
        });
      }
    );

    request.on("error", reject);
    request.write(requestBody);
    request.end();
  });
}

function readEnv(envPath) {
  const env = {};
  const raw = fs.readFileSync(envPath, "utf8").replace(/^\uFEFF/, "");
  for (const line of raw.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      continue;
    }

    const index = trimmed.indexOf("=");
    if (index <= 0) {
      continue;
    }

    const key = trimmed.slice(0, index).trim();
    let value = trimmed.slice(index + 1).trim();
    if ((value.startsWith("\"") && value.endsWith("\"")) ||
        (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }

    env[key] = value;
  }

  return env;
}

function detectMediaType(bytes) {
  if (hasPrefix(bytes, [0x89, 0x50, 0x4e, 0x47])) {
    return "image/png";
  }

  if (hasPrefix(bytes, [0xff, 0xd8, 0xff])) {
    return "image/jpeg";
  }

  if (hasPrefix(bytes, [0x47, 0x49, 0x46, 0x38])) {
    return "image/gif";
  }

  if (hasPrefix(bytes, [0x52, 0x49, 0x46, 0x46]) &&
      bytes[8] === 0x57 &&
      bytes[9] === 0x45 &&
      bytes[10] === 0x42 &&
      bytes[11] === 0x50) {
    return "image/webp";
  }

  return "image/png";
}

function hasPrefix(bytes, prefix) {
  if (bytes.length < prefix.length) {
    return false;
  }

  for (let index = 0; index < prefix.length; index += 1) {
    if (bytes[index] !== prefix[index]) {
      return false;
    }
  }

  return true;
}
