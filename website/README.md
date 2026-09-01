# SilkWheel Landing Page

Static landing page for `https://silkwheel.raymondstudio.cn/`.

## Local preview

```powershell
python -m http.server 8088
```

Open `http://127.0.0.1:8088/` from the `website` folder.

## Deploy to RackNerd VPS

1. Point DNS:
   - Host: `silkwheel`
   - Type: `A`
   - Value: your RackNerd VPS IPv4
2. Copy this folder to the VPS, for example:

```bash
sudo mkdir -p /var/www/silkwheel.raymondstudio.cn
sudo rsync -av --exclude download/ --exclude server/ ./website/ /var/www/silkwheel.raymondstudio.cn/
```

3. Add an Nginx server block:

```nginx
server {
    listen 80;
    server_name silkwheel.raymondstudio.cn;
    root /var/www/silkwheel.raymondstudio.cn;
    index index.html;

    location / {
        try_files $uri $uri/ =404;
    }

    location /download/ {
        add_header Content-Disposition "attachment";
    }
}
```

4. Enable HTTPS:

```bash
sudo certbot --nginx -d silkwheel.raymondstudio.cn
```

## Beta, feedback, and support

SilkWheel is currently positioned as a free beta:

- 21 days of free use.
- After that, users submit one short feedback note to continue using the current beta.
- Donations are optional and separate from beta access.
- International support uses PayPal: `https://paypal.me/raymondguocgi`.
- WeChat support uses `assets/wechat-support-qr.png`.
- Website feedback is submitted to the VPS feedback API and can be reviewed from the protected admin panel.

## Publishing a release

1. Publish the matching GitHub Release and keep the tag, notes, asset, and SHA256 value.
2. Add the release to `releases.json` and set its version as `latest`.
3. Upload the exact same installer to `/var/www/silkwheel.raymondstudio.cn/download/`.
4. Deploy `index.html`, `releases.html`, `releases-page.js`, `releases.json`, and `styles.css` without deleting the existing `download/` directory.
5. Deploy `server/feedback-server.js` to `/opt/silkwheel-feedback/` and restart `silkwheel-feedback` only after `node --check` passes.
6. Verify the public installer SHA256, the Releases page in English and Chinese, the feedback form, and the protected admin dashboard.

The private dashboard groups Nginx `/download/` access-log sessions by package filename. This keeps website download estimates separate from GitHub asset download counts.
