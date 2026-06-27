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
sudo mkdir -p /var/www/silkwheel
sudo rsync -av --delete ./website/ /var/www/silkwheel/
```

3. Add an Nginx server block:

```nginx
server {
    listen 80;
    server_name silkwheel.raymondstudio.cn;
    root /var/www/silkwheel;
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
- International support can use a PayPal.me or PayPal Donate link.
- WeChat support uses a QR image asset once provided.
