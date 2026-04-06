import fs from "node:fs";
import https from "node:https";
import path from "node:path";

const repoRoot = process.argv[2];
const imagePath = process.argv[3];

if (!repoRoot || !imagePath) {
  console.error("Usage: node smoke_test.mjs <repoRoot> <imagePath>");
  process.exit(2);
}

const env = readEnv(path.join(repoRoot, ".env"));
const apiKey = (env.CAPTCHA_OPENAI_API_KEY || "").trim();
const model = (env.CAPTCHA_OPENAI_MODEL || "gpt-5.4-mini").trim();
const itemsFile = resolvePath(repoRoot, env.CAPTCHA_ITEM_NAMES_FILE || "items.json");
const items = loadCatalog(itemsFile);

const imageBytes = fs.readFileSync(imagePath);
const imageBase64 = imageBytes.toString("base64");
const mediaType = detectMediaType(imageBytes);

const body = {
  model,
  messages: [
    {
      role: "system",
      content: [
        {
          type: "text",
          text: "You solve Epic RPG guard item prompts. The image contains a single target item icon. Ignore decorative diagonal lines, surrounding UI, and unrelated text. Select exactly one item from the provided catalog or return unknown if the image is too ambiguous."
        }
      ]
    },
    {
      role: "user",
      content: [
        {
          type: "text",
          text: [
            "Identify the Epic RPG item shown in the image.",
            "Return the best match from the numbered catalog.",
            "If the image is too unclear, return unknown with item_index = 0.",
            "",
            "Catalog:",
            ...items.map((item, index) => item.promptLine || `${index + 1}. ${item.name}`)
          ].join("\n")
        },
        {
          type: "image_url",
          image_url: {
            url: `data:${mediaType};base64,${imageBase64}`,
            detail: "high"
          }
        }
      ]
    }
  ],
  response_format: {
    type: "json_schema",
    json_schema: {
      name: "captcha_selection",
      strict: true,
      schema: {
        type: "object",
        additionalProperties: false,
        properties: {
          result: {
            type: "string",
            enum: ["match", "unknown"]
          },
          item_index: {
            type: "integer",
            minimum: 0,
            maximum: items.length
          }
        },
        required: ["result", "item_index"]
      }
    }
  }
};

if (!apiKey) {
  console.error("CAPTCHA_OPENAI_API_KEY is missing.");
  process.exit(1);
}

const requestBody = JSON.stringify(body);
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
        console.error(`status=${response.statusCode}`);
        console.error(raw);
        process.exit(1);
      }

      const parsed = JSON.parse(raw);
      const content = parsed?.choices?.[0]?.message?.content || "";
      const payload = JSON.parse(content);
      const label = payload.item_index > 0 ? items[payload.item_index - 1]?.name || "" : "";
      console.log(`result=${payload.result}`);
      console.log(`item_index=${payload.item_index}`);
      console.log(`label=${label}`);
      console.log(`model=${model}`);
    });
  });

request.on("error", (error) => {
  console.error(error.message);
  process.exit(1);
});

request.write(requestBody);
request.end();

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

function resolvePath(repoRoot, targetPath) {
  return path.isAbsolute(targetPath)
    ? targetPath
    : path.join(repoRoot, targetPath);
}

function loadCatalog(catalogPath) {
  if (catalogPath.toLowerCase().endsWith(".json")) {
    const parsed = JSON.parse(fs.readFileSync(catalogPath, "utf8"));
    return parsed.map((item, index) => ({
      name: item.name,
      promptLine: `${index + 1}. ${item.name} | outline: ${item.outline} | grayscale: ${item.grayscale_cues} | distinguish: ${item.disambiguation}`
    }));
  }

  return fs.readFileSync(catalogPath, "utf8")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith("#"))
    .map((name, index) => ({
      name,
      promptLine: `${index + 1}. ${name}`
    }));
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
