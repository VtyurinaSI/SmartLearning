// ========== Конфиг/хранилище ==========
const LS_KEY = "smartlearning.ui.gateway";
const cfg = loadCfg();

function loadCfg() {
  try { return JSON.parse(localStorage.getItem(LS_KEY) || "{}"); } catch { return {}; }
}
function saveCfg() { localStorage.setItem(LS_KEY, JSON.stringify(cfg)); }

const $ = (id) => document.getElementById(id);

function apiBase() {
  // Если пользователь не задал базу — используем относительный /api
  const b = (cfg.apiBase || "/api").replace(/\/$/, "");
  $("apiBaseView").textContent = b;
  return b;
}

function setJwt(token) {
  cfg.jwt = token || null;
  saveCfg();
  $("jwtStatus").textContent = cfg.jwt ? "есть" : "нет";
  // Достать sub из JWT
  try {
    if (cfg.jwt) {
      const payload = JSON.parse(atob(cfg.jwt.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
      cfg.userId = payload.sub || null;
    } else cfg.userId = null;
  } catch { cfg.userId = null; }
  $("uidStatus").textContent = cfg.userId || "нет";
}
function clearJwt() { setJwt(null); }

function toast(msg){ log(msg, "muted"); }
function log(obj, cls){
  const el = $("log"); const wrap = document.createElement("div");
  wrap.style.marginBottom = "8px";
  if(typeof obj === "string") wrap.innerHTML = `<span class="${cls||""}">${escapeHtml(obj)}</span>`;
  else wrap.innerHTML = `<pre><code>${escapeHtml(JSON.stringify(obj, null, 2))}</code></pre>`;
  el.prepend(wrap);
}
function escapeHtml(s){ return s.replace(/[&<>\"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;','\'':'&#39;'}[c])); }

async function http(path, opt = {}){
  const base = apiBase();
  const url = base + path;
  const headers = Object.assign({'Content-Type':'application/json'}, opt.headers || {});
  if (cfg.jwt) headers['Authorization'] = `Bearer ${cfg.jwt}`;
  // Подмешиваем X-User-Id из токена, если есть
  if (!headers['X-User-Id'] && cfg.userId) headers['X-User-Id'] = cfg.userId;
  const resp = await fetch(url, Object.assign({}, opt, { headers }));
  const ct = resp.headers.get('content-type') || "";
  let body = null;
  try { body = ct.includes("application/json") ? await resp.json() : await resp.text(); } catch {}
  if(!resp.ok){
    const msg = typeof body === "string" ? body : JSON.stringify(body || resp.statusText);
    throw new Error(`HTTP ${resp.status}: ${msg}`);
  }
  return body;
}

// ========== Действия ==========
async function doRegister(){
  const dtoCamel = {
    email: $("regEmail").value.trim(),
    firstName: $("regFirst").value.trim(),
    lastName: $("regLast").value.trim(),
    password: $("regPwd").value,
    confirmPassword: $("regPwd2").value
  };
  try {
    const res = await http("/auth/register", { method:"POST", body: JSON.stringify(dtoCamel) });
    const token = res.token || res.Token;
    setJwt(token);
    log({action:"register", request:dtoCamel, response:res});
    toast("Регистрация ок");
  } catch(e) {
    // На всякий — пробуем PascalCase
    const dtoPascal = {
      Email: dtoCamel.email, FirstName: dtoCamel.firstName, LastName: dtoCamel.lastName,
      Password: dtoCamel.password, ConfirmPassword: dtoCamel.confirmPassword
    };
    const res = await http("/auth/register", { method:"POST", body: JSON.stringify(dtoPascal) });
    const token = res.token || res.Token;
    setJwt(token);
    log({action:"register (PascalCase fallback)", request:dtoPascal, response:res});
    toast("Регистрация ок (fallback)");
  }
}

async function doLogin(){
  // В твоих контрактах: { login, password }
  const dto = { login: $("loginLogin").value.trim(), password: $("loginPwd").value };
  try{
    const res = await http("/auth/login", { method:"POST", body: JSON.stringify(dto) });
    const token = res.token || res.Token;
    setJwt(token);
    log({action:"login", request:dto, response:res});
    toast("Логин ок");
  } catch(e){
    // fallback: { email, password }
    const dto2 = { email: $("loginLogin").value.trim(), password: $("loginPwd").value };
    const res2 = await http("/auth/login", { method:"POST", body: JSON.stringify(dto2) });
    const token2 = res2.token || res2.Token;
    setJwt(token2);
    log({action:"login (email)", request:dto2, response:res2});
    toast("Логин ок (email)");
  }
}

async function getProgress(){
  const res = await http("/progress/user_progress", { method:"GET" });
  $("progressOut").innerHTML = `<pre><code>${escapeHtml(JSON.stringify(res, null, 2))}</code></pre>`;
  log({action:"progress", response:res});
}

async function startCheck(){
  const taskId = Number($("chkTaskId").value);
  const code = $("chkCode").value;
  if(!taskId || !code){ toast("Нужны TaskId и код"); return; }
  // Твой Gateway ожидает RecievedForChecking { taskId, origCode }
  const dto = { taskId, origCode: code };
  const res = await http("/orc/check", { method:"POST", body: JSON.stringify(dto) });
  log({action:"orc/check", request:dto, response:res});
}

async function ping(){
  const msg = encodeURIComponent($("pingMsg").value || "hello");
  const res = await http(`/users/${msg}`, { method:"GET" });
  log({action:"ping", response:res});
}

// ========== UI ==========
function applyCfgToUI(){
  $("apiBase").value = cfg.apiBase || "";
  $("jwtStatus").textContent = cfg.jwt ? "есть" : "нет";
  $("uidStatus").textContent = cfg.userId || "нет";
  apiBase();
}

document.addEventListener("DOMContentLoaded", () => {
  applyCfgToUI();
  $("saveCfgBtn").addEventListener("click", () => { cfg.apiBase = $("apiBase").value.trim(); saveCfg(); apiBase(); toast("Сохранено"); });
  $("resetCfgBtn").addEventListener("click", () => { Object.keys(cfg).forEach(k=>delete cfg[k]); saveCfg(); location.reload(); });
  $("clearJwtBtn").addEventListener("click", clearJwt);
  $("regBtn").addEventListener("click", () => doRegister().catch(e=>log("Ошибка: "+e.message, "bad")));
  $("loginBtn").addEventListener("click", () => doLogin().catch(e=>log("Ошибка: "+e.message, "bad")));
  $("progressBtn").addEventListener("click", () => getProgress().catch(e=>log("Ошибка: "+e.message, "bad")));
  $("checkBtn").addEventListener("click", () => startCheck().catch(e=>log("Ошибка: "+e.message, "bad")));
  $("pingBtn").addEventListener("click", () => ping().catch(e=>log("Ошибка: "+e.message, "bad")));
});
