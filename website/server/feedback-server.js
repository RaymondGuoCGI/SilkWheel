const http = require("node:http");
const fs = require("node:fs/promises");
const path = require("node:path");
const crypto = require("node:crypto");

const HOST = process.env.HOST || "127.0.0.1";
const PORT = Number(process.env.PORT || 3097);
const STORE_DIR = process.env.FEEDBACK_STORE_DIR || "/var/lib/silkwheel-feedback";
const STORE_FILE = path.join(STORE_DIR, "feedback.jsonl");
const ACCESS_LOG_FILE = process.env.ACCESS_LOG_FILE || "/var/log/nginx/silkwheel.raymondstudio.cn.access.log";
const MAX_BODY_BYTES = 32 * 1024;
const RATE_WINDOW_MS = 60 * 1000;
const RATE_LIMIT = 8;
const ADMIN_USER = process.env.ADMIN_USER || "admin";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || "";
const hits = new Map();

function sendJson(res, status, body) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end(JSON.stringify(body));
}

function sendHtml(res, status, html) {
  res.writeHead(status, {
    "Content-Type": "text/html; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end(html);
}

function escapeHtml(value) {
  return String(value || "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function unauthorized(res) {
  res.writeHead(401, {
    "WWW-Authenticate": 'Basic realm="SilkWheel Feedback"',
    "Content-Type": "text/plain; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end("Authentication required");
}

function isAdminAuthorized(req) {
  if (!ADMIN_PASSWORD) {
    return false;
  }

  const auth = req.headers.authorization || "";
  if (!auth.startsWith("Basic ")) {
    return false;
  }

  try {
    const decoded = Buffer.from(auth.slice(6), "base64").toString("utf8");
    const separator = decoded.indexOf(":");
    if (separator < 0) {
      return false;
    }

    const user = decoded.slice(0, separator);
    const password = decoded.slice(separator + 1);
    return user === ADMIN_USER && password === ADMIN_PASSWORD;
  } catch {
    return false;
  }
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

async function loadFeedbackItems(limit = 200) {
  try {
    const text = await fs.readFile(STORE_FILE, "utf8");
    return text
      .split(/\r?\n/)
      .filter(Boolean)
      .slice(-limit)
      .reverse()
      .map((line) => {
        try {
          return JSON.parse(line);
        } catch {
          return { createdAt: "", topic: "Parse error", message: line };
        }
      });
  } catch (error) {
    if (error.code === "ENOENT") {
      return [];
    }

    throw error;
  }
}

function isLikelyBot(userAgent) {
  return /bot|crawler|spider|preview|slurp|curl|wget|python|scrapy|headless/i.test(userAgent || "");
}

function parseAccessLogLine(line) {
  const match = line.match(/^(\S+) \S+ \S+ \[([^\]]+)\] "(\S+) ([^"]+?) HTTP\/[0-9.]+" (\d{3}) (\d+|-) "([^"]*)" "([^"]*)"/);
  if (!match) {
    return null;
  }

  const [, ip, time, method, url, status, bytes, referer, userAgent] = match;
  return {
    ip,
    time,
    method,
    url,
    status: Number(status),
    bytes: bytes === "-" ? 0 : Number(bytes),
    referer,
    userAgent
  };
}

async function loadDownloadStats(limit = 10) {
  try {
    const files = [ACCESS_LOG_FILE, `${ACCESS_LOG_FILE}.1`];
    const textParts = [];
    for (const file of files) {
      try {
        textParts.push(await fs.readFile(file, "utf8"));
      } catch (error) {
        if (error.code !== "ENOENT") {
          throw error;
        }
      }
    }
    const text = textParts.join("\n");
    const downloads = text
      .split(/\r?\n/)
      .filter(Boolean)
      .map(parseAccessLogLine)
      .filter(Boolean)
      .filter((entry) => entry.method === "GET" && entry.status >= 200 && entry.status < 400 && entry.url.startsWith("/download/"));

    const botDownloads = downloads.filter((entry) => isLikelyBot(entry.userAgent));
    const humanDownloads = downloads.filter((entry) => !isLikelyBot(entry.userAgent));
    const uniqueIps = new Set(humanDownloads.map((entry) => entry.ip));

    return {
      total: downloads.length,
      likelyHuman: humanDownloads.length,
      likelyBot: botDownloads.length,
      uniqueIpCount: uniqueIps.size,
      recent: downloads.slice(-limit).reverse()
    };
  } catch (error) {
    return {
      total: 0,
      likelyHuman: 0,
      likelyBot: 0,
      uniqueIpCount: 0,
      recent: [],
      error: error.code === "ENOENT" ? "No access log yet." : error.message
    };
  }
}

async function handleAdminFeedback(req, res) {
  if (!isAdminAuthorized(req)) {
    unauthorized(res);
    return;
  }

  const items = await loadFeedbackItems();
  const downloadStats = await loadDownloadStats();
  const rows = items.map((item) => `
    <article class="feedback-item">
      <div class="meta">
        <span>${escapeHtml(item.createdAt || "-")}</span>
        <span>${escapeHtml(item.topic || "Feedback")}</span>
        <span>${escapeHtml(item.language || "-")}</span>
      </div>
      <p class="message">${escapeHtml(item.message || "")}</p>
      <div class="details">
        <span>Email: ${escapeHtml(item.email || "not provided")}</span>
        <span>IP: ${escapeHtml(item.ip || "-")}</span>
      </div>
      <details>
        <summary>Browser</summary>
        <pre>${escapeHtml(item.userAgent || "-")}</pre>
      </details>
    </article>
  `).join("");
  const downloadRows = downloadStats.recent.map((item) => `
    <article class="download-row">
      <strong>${escapeHtml(item.url || "-")}</strong>
      <span>${escapeHtml(item.time || "-")}</span>
      <span>${escapeHtml(item.ip || "-")}</span>
      <span>${escapeHtml(isLikelyBot(item.userAgent) ? "bot/automation" : "likely human")}</span>
      <details>
        <summary>User agent</summary>
        <pre>${escapeHtml(item.userAgent || "-")}</pre>
      </details>
    </article>
  `).join("");

  sendHtml(res, 200, `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>SilkWheel Feedback</title>
  <style>
    :root { color-scheme: dark; --bg: #0b100e; --panel: #151d19; --line: #31413a; --text: #f3fff1; --muted: #abc1b8; --accent: #27d7c9; }
    * { box-sizing: border-box; }
    body { margin: 0; padding: 32px; background: var(--bg); color: var(--text); font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    header { display: flex; align-items: end; justify-content: space-between; gap: 16px; max-width: 1120px; margin: 0 auto 24px; }
    h1 { margin: 0; font-size: clamp(30px, 5vw, 52px); letter-spacing: 0; }
    .count { color: var(--muted); font-weight: 800; }
    main { display: grid; gap: 14px; max-width: 1120px; margin: 0 auto; }
    .empty, .feedback-item { border: 1px solid var(--line); border-radius: 18px; background: var(--panel); }
    .empty { padding: 24px; color: var(--muted); }
    .feedback-item { padding: 20px; }
    .stats { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; max-width: 1120px; margin: 0 auto 18px; }
    .stat { border: 1px solid var(--line); border-radius: 18px; background: var(--panel); padding: 18px; }
    .stat strong { display: block; font-size: 30px; color: var(--accent); }
    .stat span { color: var(--muted); font-size: 13px; }
    .meta, .details { display: flex; flex-wrap: wrap; gap: 10px; color: var(--muted); font-size: 13px; }
    .meta span, .details span { border: 1px solid var(--line); border-radius: 999px; padding: 5px 10px; }
    .message { white-space: pre-wrap; line-height: 1.65; margin: 16px 0; }
    .download-list { max-width: 1120px; margin: 0 auto 20px; display: grid; gap: 10px; }
    .download-list h2 { margin: 8px 0 0; }
    .download-row { border: 1px solid var(--line); border-radius: 14px; background: var(--panel); padding: 14px; display: grid; gap: 6px; color: var(--muted); }
    .download-row strong { color: var(--text); overflow-wrap: anywhere; }
    details { margin-top: 12px; color: var(--muted); }
    summary { cursor: pointer; }
    pre { white-space: pre-wrap; overflow-wrap: anywhere; color: var(--muted); }
    a { color: var(--accent); }
    @media (max-width: 720px) { body { padding: 20px; } header { align-items: flex-start; flex-direction: column; } .stats { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>SilkWheel Feedback</h1>
      <p class="count">${items.length} recent submission${items.length === 1 ? "" : "s"}</p>
    </div>
    <a href="/">Back to site</a>
  </header>
  <section class="stats" aria-label="Beta stats">
    <div class="stat"><strong>${items.length}</strong><span>feedback submissions</span></div>
    <div class="stat"><strong>${downloadStats.total}</strong><span>download requests</span></div>
    <div class="stat"><strong>${downloadStats.likelyHuman}</strong><span>likely human downloads</span></div>
    <div class="stat"><strong>${downloadStats.uniqueIpCount}</strong><span>unique downloader IPs</span></div>
  </section>
  <section class="download-list">
    <h2>Recent downloads</h2>
    ${downloadStats.error ? `<div class="empty">${escapeHtml(downloadStats.error)}</div>` : ""}
    ${downloadRows || '<div class="empty">No download requests yet.</div>'}
  </section>
  <main>
    ${items.length ? rows : '<div class="empty">No feedback yet.</div>'}
  </main>
</body>
</html>`);
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

    if (req.url === "/admin/feedback" && req.method === "GET") {
      await handleAdminFeedback(req, res);
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
