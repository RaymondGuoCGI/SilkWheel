const http = require("node:http");
const fs = require("node:fs/promises");
const path = require("node:path");
const crypto = require("node:crypto");

const HOST = process.env.HOST || "127.0.0.1";
const PORT = Number(process.env.PORT || 3097);
const STORE_DIR = process.env.FEEDBACK_STORE_DIR || "/var/lib/silkwheel-feedback";
const STORE_FILE = path.join(STORE_DIR, "feedback.jsonl");
const MAX_BODY_BYTES = 32 * 1024;
const RATE_WINDOW_MS = 60 * 1000;
const RATE_LIMIT = 8;
const hits = new Map();

function sendJson(res, status, body) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end(JSON.stringify(body));
}

function getClientIp(req) {
  const forwarded = req.headers["x-forwarded-for"];
  if (typeof forwarded === "string" && forwarded.trim()) {
    return forwarded.split(",")[0].trim();
  }

  return req.socket.remoteAddress || "unknown";
}

function checkRateLimit(ip) {
  const now = Date.now();
  const bucket = hits.get(ip)?.filter((time) => now - time < RATE_WINDOW_MS) || [];
  if (bucket.length >= RATE_LIMIT) {
    hits.set(ip, bucket);
    return false;
  }

  bucket.push(now);
  hits.set(ip, bucket);
  return true;
}

async function readJsonBody(req) {
  let size = 0;
  const chunks = [];
  for await (const chunk of req) {
    size += chunk.length;
    if (size > MAX_BODY_BYTES) {
      throw Object.assign(new Error("Payload too large"), { statusCode: 413 });
    }
    chunks.push(chunk);
  }

  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8") || "{}");
  } catch {
    throw Object.assign(new Error("Invalid JSON"), { statusCode: 400 });
  }
}

function cleanText(value, maxLength) {
  return String(value || "").replace(/\0/g, "").trim().slice(0, maxLength);
}

async function handleFeedback(req, res) {
  const ip = getClientIp(req);
  if (!checkRateLimit(ip)) {
    sendJson(res, 429, { ok: false, error: "Too many feedback submissions. Please try again later." });
    return;
  }

  const body = await readJsonBody(req);
  const email = cleanText(body.email, 254);
  const topic = cleanText(body.topic || "Feedback", 80);
  const message = cleanText(body.message, 4000);
  const language = cleanText(body.language, 20);

  if (message.length < 5) {
    sendJson(res, 400, { ok: false, error: "Feedback message is too short." });
    return;
  }

  const item = {
    id: crypto.randomUUID(),
    createdAt: new Date().toISOString(),
    ip,
    userAgent: cleanText(req.headers["user-agent"], 300),
    language,
    email,
    topic,
    message
  };

  await fs.mkdir(STORE_DIR, { recursive: true });
  await fs.appendFile(STORE_FILE, `${JSON.stringify(item)}\n`, { encoding: "utf8", mode: 0o600 });
  sendJson(res, 200, { ok: true, id: item.id });
}

const server = http.createServer(async (req, res) => {
  try {
    if (req.method === "OPTIONS") {
      sendJson(res, 200, { ok: true });
      return;
    }

    if (req.url === "/api/feedback" && req.method === "POST") {
      await handleFeedback(req, res);
      return;
    }

    sendJson(res, 404, { ok: false, error: "Not found" });
  } catch (error) {
    sendJson(res, error.statusCode || 500, { ok: false, error: error.message || "Server error" });
  }
});

server.listen(PORT, HOST, () => {
  console.log(`SilkWheel feedback server listening on http://${HOST}:${PORT}`);
});
