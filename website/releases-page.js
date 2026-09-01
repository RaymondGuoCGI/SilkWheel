(() => {
  const translations = {
    en: {
      metaTitle: "SilkWheel Releases - Version history and downloads",
      metaDescription: "SilkWheel release history, Windows downloads, release notes, file sizes, and SHA256 checksums.",
      "nav.features": "Features",
      "nav.beta": "Beta",
      "nav.releases": "Releases",
      "nav.feedback": "Feedback",
      "nav.language": "Language",
      "nav.download": "Download",
      "hero.eyebrow": "Version history",
      "hero.title": "Every notch, accounted for.",
      "hero.text": "See what changed, when it shipped, and exactly which Windows package you are downloading.",
      "hero.ledger": "Current release",
      "history.eyebrow": "Release track",
      "history.title": "From first glide to finer control.",
      "history.text": "The newest tested build stays at the top. Previous versions remain available for comparison and rollback.",
      "history.loading": "Loading release history…",
      "history.error": "Release history could not be loaded. Open GitHub Releases instead.",
      "integrity.eyebrow": "Before you install",
      "integrity.title": "One version, one file, one checksum.",
      "integrity.text": "The website package mirrors the corresponding GitHub Release. Compare the SHA256 value if you want to verify the download before running it.",
      "footer.brand": "SilkWheel by Raymond Studio",
      "footer.home": "Home",
      "footer.github": "GitHub Releases",
      latest: "Latest",
      beta: "Beta",
      previous: "Previous release",
      new: "New",
      improved: "Improved",
      fixed: "Fixed",
      download: "Download for Windows",
      github: "View on GitHub",
      checksum: "SHA256 checksum",
      copy: "Copy",
      copied: "Copied",
      package: "Package"
    },
    zh: {
      metaTitle: "SilkWheel 更新记录 - 版本历史与下载",
      metaDescription: "查看 SilkWheel 版本历史、Windows 安装包、更新说明、文件大小与 SHA256 校验值。",
      "nav.features": "功能",
      "nav.beta": "Beta",
      "nav.releases": "更新记录",
      "nav.feedback": "反馈",
      "nav.language": "语言",
      "nav.download": "下载",
      "hero.eyebrow": "版本历史",
      "hero.title": "每一格滚动，都有记录。",
      "hero.text": "查看每个版本改了什么、何时发布，以及你正在下载的具体 Windows 安装包。",
      "hero.ledger": "当前版本",
      "history.eyebrow": "发布轨迹",
      "history.title": "从第一次丝滑，到更精细的控制。",
      "history.text": "最新的已验证版本始终在顶部；旧版本继续保留，方便对比和回退。",
      "history.loading": "正在加载版本记录…",
      "history.error": "暂时无法加载版本记录，请前往 GitHub Releases。",
      "integrity.eyebrow": "安装之前",
      "integrity.title": "一个版本、一个文件、一个校验值。",
      "integrity.text": "官网安装包与对应 GitHub Release 保持一致。运行前可以比较 SHA256，确认下载文件完整。",
      "footer.brand": "SilkWheel by Raymond Studio",
      "footer.home": "首页",
      "footer.github": "GitHub Releases",
      latest: "最新版",
      beta: "Beta",
      previous: "历史版本",
      new: "新增",
      improved: "改进",
      fixed: "修复",
      download: "下载 Windows 版",
      github: "在 GitHub 查看",
      checksum: "SHA256 校验值",
      copy: "复制",
      copied: "已复制",
      package: "安装包"
    }
  };

  const releaseRail = document.querySelector("#releaseRail");
  const languagePicker = document.querySelector(".language-picker");
  const languageButton = document.querySelector("#languageButton");
  const languageCurrent = document.querySelector("#languageCurrent");
  const languageMenu = document.querySelector("#languageMenu");
  let manifest = null;
  let currentLanguage = detectLanguage();

  function detectLanguage() {
    const urlLang = new URLSearchParams(window.location.search).get("lang");
    if (urlLang === "zh" || urlLang === "en") {
      return urlLang;
    }
    const stored = localStorage.getItem("silkwheel-language");
    if (stored === "zh" || stored === "en") {
      return stored;
    }
    return navigator.language?.toLowerCase().startsWith("zh") ? "zh" : "en";
  }

  function escapeHtml(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function formatDate(value, lang) {
    return new Intl.DateTimeFormat(lang === "zh" ? "zh-CN" : "en-US", {
      year: "numeric",
      month: lang === "zh" ? "long" : "long",
      day: "numeric",
      timeZone: "UTC"
    }).format(new Date(value));
  }

  function formatBytes(bytes) {
    return `${(Number(bytes) / 1024 / 1024).toFixed(1)} MB`;
  }

  function renderNoteGroup(title, items, className) {
    if (!Array.isArray(items) || items.length === 0) {
      return "";
    }
    return `<section class="release-note-group ${className}">
      <h3>${escapeHtml(title)}</h3>
      <ul>${items.map((item) => `<li>${escapeHtml(item)}</li>`).join("")}</ul>
    </section>`;
  }

  function renderReleases() {
    if (!manifest || !releaseRail) {
      return;
    }
    const copy = translations[currentLanguage] || translations.en;
    releaseRail.innerHTML = manifest.releases.map((release) => {
      const notes = release.notes?.[currentLanguage] || release.notes?.en || {};
      const isLatest = release.version === manifest.latest;
      const noteGroups = [
        renderNoteGroup(copy.new, notes.new, "is-new"),
        renderNoteGroup(copy.improved, notes.improved, "is-improved"),
        renderNoteGroup(copy.fixed, notes.fixed, "is-fixed")
      ].filter(Boolean).join("");
      return `<article class="release-entry${isLatest ? " is-latest" : ""}">
        <div class="release-node" aria-hidden="true"><span></span></div>
        <div class="release-card">
          <header class="release-card-header">
            <div>
              <div class="release-badges">
                <span class="release-badge${isLatest ? " is-latest" : ""}">${escapeHtml(isLatest ? copy.latest : copy.previous)}</span>
                <span class="release-badge">${escapeHtml(copy.beta)}</span>
              </div>
              <h2>SilkWheel ${escapeHtml(release.version)}</h2>
              <p class="release-headline">${escapeHtml(notes.headline || "")}</p>
            </div>
            <time datetime="${escapeHtml(release.publishedAt)}">${escapeHtml(formatDate(release.publishedAt, currentLanguage))}</time>
          </header>
          <p class="release-summary">${escapeHtml(notes.summary || "")}</p>
          <div class="release-note-grid">${noteGroups}</div>
          <div class="release-package">
            <div>
              <span>${escapeHtml(copy.package)}</span>
              <strong>${escapeHtml(release.fileName)}</strong>
              <small>${escapeHtml(`${release.platform} · ${release.packageType} · ${formatBytes(release.sizeBytes)}`)}</small>
            </div>
            <div class="release-actions">
              <a class="button primary" href="${escapeHtml(release.downloadUrl)}" download>${escapeHtml(copy.download)}</a>
              <a class="button secondary" href="${escapeHtml(release.githubUrl)}" target="_blank" rel="noopener">${escapeHtml(copy.github)}</a>
            </div>
          </div>
          <details class="release-checksum">
            <summary>${escapeHtml(copy.checksum)}</summary>
            <div><code>${escapeHtml(release.sha256)}</code><button type="button" data-copy-sha="${escapeHtml(release.sha256)}">${escapeHtml(copy.copy)}</button></div>
          </details>
        </div>
      </article>`;
    }).join("");

    const latest = manifest.releases.find((release) => release.version === manifest.latest) || manifest.releases[0];
    if (latest) {
      document.querySelectorAll("[data-latest-download]").forEach((link) => link.setAttribute("href", latest.downloadUrl));
      document.querySelector("#latestVersion").textContent = latest.version;
      document.querySelector("#latestDate").textContent = formatDate(latest.publishedAt, currentLanguage);
      document.querySelector("#latestPackage").textContent = `${latest.platform} · ${latest.packageType} · ${formatBytes(latest.sizeBytes)}`;
    }
  }

  function applyLanguage(lang) {
    currentLanguage = lang === "zh" ? "zh" : "en";
    const copy = translations[currentLanguage];
    document.documentElement.lang = currentLanguage === "zh" ? "zh-CN" : "en";
    document.title = copy.metaTitle;
    document.querySelector('meta[name="description"]')?.setAttribute("content", copy.metaDescription);
    document.querySelectorAll("[data-i18n]").forEach((node) => {
      const key = node.getAttribute("data-i18n");
      if (key && copy[key]) {
        node.textContent = copy[key];
      }
    });
    languageCurrent.textContent = currentLanguage === "zh" ? "中文" : "English";
    languageMenu.querySelectorAll("[data-lang]").forEach((button) => {
      const selected = button.getAttribute("data-lang") === currentLanguage;
      button.setAttribute("aria-checked", selected ? "true" : "false");
      button.classList.toggle("is-active", selected);
    });
    languagePicker.classList.remove("is-open");
    languageButton.setAttribute("aria-expanded", "false");
    localStorage.setItem("silkwheel-language", currentLanguage);
    renderReleases();
  }

  function positionLanguageMenu() {
    if (!window.matchMedia("(max-width: 720px)").matches) {
      languageMenu.style.removeProperty("top");
      languageMenu.style.removeProperty("left");
      return;
    }
    const rect = languagePicker.getBoundingClientRect();
    const menuWidth = 140;
    languageMenu.style.left = `${Math.min(Math.max(rect.left, 16), window.innerWidth - menuWidth - 16)}px`;
    languageMenu.style.top = `${rect.bottom + 10}px`;
  }

  async function loadManifest() {
    try {
      const response = await fetch("releases.json", { cache: "no-store" });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = await response.json();
      if (!Array.isArray(data.releases) || data.releases.length === 0) {
        throw new Error("No releases found");
      }
      manifest = data;
      renderReleases();
    } catch {
      const copy = translations[currentLanguage] || translations.en;
      releaseRail.innerHTML = `<div class="release-loading is-error">${escapeHtml(copy["history.error"])} <a href="https://github.com/RaymondGuoCGI/SilkWheel/releases" target="_blank" rel="noopener">GitHub Releases</a></div>`;
    }
  }

  languageButton.addEventListener("click", () => {
    const isOpen = languagePicker.classList.toggle("is-open");
    languageButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
    if (isOpen) {
      positionLanguageMenu();
    }
  });
  languageMenu.addEventListener("click", (event) => {
    const target = event.target.closest("[data-lang]");
    if (target) {
      applyLanguage(target.getAttribute("data-lang"));
    }
  });
  document.addEventListener("click", (event) => {
    if (!languagePicker.contains(event.target)) {
      languagePicker.classList.remove("is-open");
      languageButton.setAttribute("aria-expanded", "false");
    }
  });
  releaseRail.addEventListener("click", async (event) => {
    const button = event.target.closest("[data-copy-sha]");
    if (!button) {
      return;
    }
    const copy = translations[currentLanguage] || translations.en;
    try {
      await navigator.clipboard.writeText(button.getAttribute("data-copy-sha"));
      button.textContent = copy.copied;
      window.setTimeout(() => { button.textContent = copy.copy; }, 1400);
    } catch {
      button.previousElementSibling?.focus?.();
    }
  });
  window.addEventListener("resize", positionLanguageMenu);
  window.addEventListener("scroll", positionLanguageMenu, { passive: true });

  applyLanguage(currentLanguage);
  loadManifest();
})();
