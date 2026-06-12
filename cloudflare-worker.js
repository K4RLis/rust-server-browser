// ── Cloudflare Worker: прокси к Steam master-server для Rust (appid 252490) с CORS ──
//
// Зачем: Steam отдаёт ВЕСЬ список Rust-серверов одним запросом (десятки тысяч),
// но требует API-ключ и не разрешает запросы из браузера (нет CORS).
// Этот воркер держит ключ у себя, ходит в Steam и отдаёт ответ нашему сайту с CORS.
//
// Настройка:
//   1) Вставь этот код в Cloudflare Worker (Workers & Pages → Create → Worker).
//   2) Settings → Variables and Secrets → добавь секрет STEAM_KEY = твой Steam API-ключ.
//   3) Deploy. URL воркера (https://...workers.dev) дай в приложение.
//
// Использование: GET https://<worker>.workers.dev/?region=EU
//   region: EU | RU | NA | SA | AS | OCE | ME | AF  (или пусто = весь мир)

const APPID = 252490;
const REGION_CODES = { EU:[3], RU:[3], NA:[0,1], SA:[2], AS:[4], OCE:[5], ME:[6], AF:[7] };

export default {
  async fetch(request, env) {
    const cors = {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, OPTIONS",
      "Access-Control-Allow-Headers": "*",
      "Cache-Control": "public, max-age=60"
    };
    if (request.method === "OPTIONS") return new Response(null, { headers: cors });

    const key = env.STEAM_KEY;
    if (!key) return j({ error: "STEAM_KEY не задан в настройках воркера" }, 500, cors);

    const url = new URL(request.url);
    const region = (url.searchParams.get("region") || "").toUpperCase();
    const limit = Math.min(parseInt(url.searchParams.get("limit") || "20000", 10) || 20000, 20000);
    const codes = REGION_CODES[region] || [null];   // null = без региона (весь мир)

    try {
      const all = [];
      const seen = new Set();
      for (const code of codes) {
        let filter = `\\appid\\${APPID}`;
        if (code !== null) filter += `\\region\\${code}`;
        const api = "https://api.steampowered.com/IGameServersService/GetServerList/v1/"
          + `?key=${key}&limit=${limit}&filter=${encodeURIComponent(filter)}`;
        const r = await fetch(api);
        if (!r.ok) continue;
        const d = await r.json();
        for (const s of (d.response && d.response.servers ? d.response.servers : [])) {
          if (s.addr && !seen.has(s.addr)) { seen.add(s.addr); all.push(s); }
        }
      }
      return j({ count: all.length, servers: all }, 200, cors);
    } catch (e) {
      return j({ error: String(e) }, 502, cors);
    }
  }
};

function j(obj, status, cors) {
  return new Response(JSON.stringify(obj), {
    status, headers: { "Content-Type": "application/json", ...cors }
  });
}
