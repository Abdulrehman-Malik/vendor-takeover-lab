// Tiny admin dashboard. No npm dependencies: only Node's built-in http server and fetch.
// Traefik sends every /admin request here (see infra/stack.yml).
import http from 'node:http';
import os from 'node:os';

const port = Number(process.env.PORT ?? 3000);

// Override with HEALTH_TARGETS="name=url,name=url" if service names or ports change.
const targets = (process.env.HEALTH_TARGETS ??
  'orders-api=http://orders-api:8080/health,' +
  'customers-api=http://customers-api:8080/health,' +
  'billing-api=http://billing-api:8080/health,' +
  'documents-api=http://documents-api:8080/health,' +
  'web-portal=http://web-portal/healthz')
  .split(',')
  .map((pair) => pair.split('='))
  .map(([name, url]) => ({ name: name.trim(), url: url.trim() }));

async function check({ name, url }) {
  const started = Date.now();
  try {
    const res = await fetch(url, { signal: AbortSignal.timeout(2000) });
    return { name, url, healthy: res.ok, status: String(res.status), ms: Date.now() - started };
  } catch (err) {
    return { name, url, healthy: false, status: err.cause?.code ?? err.name, ms: Date.now() - started };
  }
}

function page(results) {
  const rows = results
    .map(
      (r) => `<tr>
        <td>${r.name}</td>
        <td class="${r.healthy ? 'ok' : 'bad'}">${r.healthy ? 'healthy' : 'DOWN'}</td>
        <td>${r.status}</td>
        <td>${r.ms} ms</td>
        <td><code>${r.url}</code></td>
      </tr>`,
    )
    .join('');

  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta http-equiv="refresh" content="10">
  <title>Lab Admin</title>
  <style>
    :root { --bg:#f6f7f9; --text:#1d2330; --border:#dfe3ea; --ok:#1d7a3a; --bad:#b42318; }
    @media (prefers-color-scheme: dark) { :root { --bg:#14171d; --text:#e6e9ef; --border:#2d3340; --ok:#5fd18a; --bad:#ff8a80; } }
    body { margin:0; padding:16px; background:var(--bg); color:var(--text); font-family:system-ui, sans-serif; }
    table { border-collapse:collapse; width:100%; max-width:900px; }
    th, td { text-align:left; padding:6px 8px; border-bottom:1px solid var(--border); }
    .ok { color:var(--ok); font-weight:600; } .bad { color:var(--bad); font-weight:600; }
    .wrap { overflow-x:auto; }
  </style>
</head>
<body>
  <h1>Lab Admin</h1>
  <p>Health of every service, checked from inside the Swarm network. Refreshes every 10 seconds.
     Answered by container <code>${os.hostname()}</code>.</p>
  <div class="wrap">
    <table>
      <thead><tr><th>Service</th><th>State</th><th>Status</th><th>Time</th><th>Checked URL</th></tr></thead>
      <tbody>${rows}</tbody>
    </table>
  </div>
</body>
</html>`;
}

const server = http.createServer(async (req, res) => {
  const path = new URL(req.url ?? '/', 'http://localhost').pathname.replace(/\/$/, '');

  if (path === '/health') {
    res.writeHead(200, { 'content-type': 'text/plain' }).end('healthy');
  } else if (path === '/admin/api/status') {
    const results = await Promise.all(targets.map(check));
    res.writeHead(200, { 'content-type': 'application/json' }).end(JSON.stringify(results, null, 2));
  } else if (path === '/admin') {
    const results = await Promise.all(targets.map(check));
    res.writeHead(200, { 'content-type': 'text/html; charset=utf-8' }).end(page(results));
  } else {
    res.writeHead(404, { 'content-type': 'text/plain' }).end('not found');
  }
});

server.listen(port, () => console.log(`admin-node listening on :${port}`));
