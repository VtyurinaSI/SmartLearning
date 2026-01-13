// app.js v7 — show compilation log as HTML code block
const LS_KEY = "smartlearning.ui.gateway";
const cfg = (function(){ try { return JSON.parse(localStorage.getItem(LS_KEY) || "{}"); } catch { return {}; } })();
const $ = id => document.getElementById(id);
function saveCfg(){ localStorage.setItem(LS_KEY, JSON.stringify(cfg)); }
function apiBase(){ const b=(cfg.apiBase||"/api").replace(/\/$/,""); const v=$("apiBaseView"); if(v) v.textContent=b; return b; }
function setJwt(token){
  cfg.jwt = token || null; saveCfg();
  const jwtS=$("jwtStatus"), uidS=$("uidStatus");
  if(jwtS) jwtS.textContent = cfg.jwt ? "есть" : "нет";
  try{ cfg.userId = cfg.jwt ? JSON.parse(atob(cfg.jwt.split(".")[1].replace(/-/g,"+").replace(/_/g,"/"))).sub||null : null; }catch{ cfg.userId=null; }
  if(uidS) uidS.textContent = cfg.userId || "нет";
}
function clearJwt(){ setJwt(null); }
function escapeHtml(s){ return s.replace(/[&<>\"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;','\'':'&#39;'}[c])); }
function log(obj, cls){ const el=$("log"); if(!el) return; const wrap=document.createElement("div"); wrap.style.marginBottom="10px";
  if(typeof obj==="string") wrap.innerHTML=`<span class="${cls||""}">${escapeHtml(obj)}</span>`;
  else wrap.innerHTML = `<pre>${escapeHtml(JSON.stringify(obj,null,2))}</pre>`; el.prepend(wrap); }
async function http(path,opt={}){
  const url = apiBase()+path;
  const headers = Object.assign({'Content-Type':'application/json'}, opt.headers||{});
  if(cfg.jwt) headers['Authorization'] = `Bearer ${cfg.jwt}`;
  if(!headers['X-User-Id'] && cfg.userId) headers['X-User-Id'] = cfg.userId;
  const resp = await fetch(url, Object.assign({},opt,{headers}));
  const ct = resp.headers.get('content-type')||"";
  let body=null; try{ body = ct.includes("application/json") ? await resp.json() : await resp.text(); }catch{}
  if(!resp.ok){ const msg = typeof body==="string" ? body : JSON.stringify(body||resp.statusText); throw new Error(`HTTP ${resp.status}: ${msg}`);}
  return body;
}
// ---- helpers for progress/check ----
function idFrom(x){ if(x && typeof x==="object") return x.taskId ?? x.TaskId ?? x.id ?? x.Id ?? x.taskID ?? null; return x; }
function stageFrom(x){
  if(x && typeof x==="object"){
    return x.nextCheckingStage ?? x.nextCheckStage ?? x.NextCheckingStage ?? x.NextCheckStage ?? x.stage ?? x.Stage ?? x.status ?? x.Status ?? "—";
  }
  return "—";
}
function renderProgress(res){
  const complRaw = res?.complitedTasks ?? res?.completedTasks ?? res?.CompletedTasks ?? [];
  const inprocRaw = res?.inProcessTasks ?? res?.InProcessTasks ?? res?.inprogressTasks ?? [];
  const complIds = Array.isArray(complRaw) ? complRaw.map(idFrom).filter(v=>v!=null) : [];
  const lines = [];
  lines.push(`Завершены задачи: ${complIds.length ? complIds.join(", ") : "—"}.`);
  lines.push(`Задачи в процессе выполнения:`);
  if(Array.isArray(inprocRaw) && inprocRaw.length){
    for(const item of inprocRaw){
      const id = idFrom(item);
      const st = stageFrom(item);
      lines.push(`${id ?? "—"} - ${st}.`);
    }
  } else {
    lines.push("—");
  }
  return lines.join("\n");
}
// ---- compilation/test/review parsing ----
function parseCompilation(x){
  if (x && typeof x === "object"){
    const ok = x.ok===true || x.success===true || x.Success===true;
    const warnings = x.warnings ?? x.Warnings ?? 0;
    const errors = x.errors ?? x.Errors ?? 0;
    const details = x.details ?? x.message ?? x.log ?? "";
    return { ok, warnings: +warnings||0, errors: +errors||0, details: String(details||"") };
  }
  const s = String(x||"");
  const lower = s.toLowerCase();
  const ok = /success/.test(lower) && !/error/.test(lower);
  let warnings = 0, errors = 0;
  const wm = /warning\s*[-: ]\s*(\d+)/i.exec(s);
  if (wm) warnings = parseInt(wm[1],10);
  const em = /error\s*[-: ]\s*(\d+)/i.exec(s);
  if (em) errors = parseInt(em[1],10);
  return { ok, warnings, errors, details: s };
}
function parseTests(x){
  if (x == null) return { ran:false, passed:0, failed:0, details:"" };
  if (typeof x === "object"){
    const passed = x.passed ?? x.Passed ?? 0;
    const failed = x.failed ?? x.Failed ?? 0;
    return { ran:true, passed, failed, details: JSON.stringify(x) };
  }
  const s = String(x);
  const pm = /passed\s*[:= ]\s*(\d+)/i.exec(s);
  const fm = /failed\s*[:= ]\s*(\d+)/i.exec(s);
  return { ran:true, passed: pm?parseInt(pm[1],10):0, failed: fm?parseInt(fm[1],10):0, details:s };
}
function parseReview(x){
  if (x == null) return null;
  if (typeof x === "object"){
    const score = x.score ?? x.Score ?? null;
    const summary = x.summary ?? x.Summary ?? "";
    return { score, summary: String(summary||"") };
  }
  return { score: null, summary: String(x) };
}
function renderCheckHTML(res){
  const c = parseCompilation(res.compilRes || res.Compilation || res.compilation);
  const t = parseTests(res.testsRes || res.Tests || res.tests);
  const r = parseReview(res.reviewRes || res.Review || res.review);
  let html = "";
  html += "Проверка завершена:\n";
  html += `• Компиляция: ${c.ok ? "успешно" : "с ошибками"}${(c.warnings||c.errors)?` (предупреждений: ${c.warnings||0}${c.errors?`, ошибок: ${c.errors}`:""})`:""}\n`;
  if (c.details && c.details.trim()){
    html += "— Журнал компиляции:\n";
    html += `<div class="code-block"><pre>${escapeHtml(c.details.trim())}</pre></div>\n`;
  }
  html += `• Тесты: ${t.ran ? `${t.passed} ок, ${t.failed} провалено` : "не запускались"}\n`;
  if (r && r.summary){
    html += "• Ревью:\n";
    html += `${escapeHtml(r.summary)}`;
  }
  return html;
}
// ---- actions ----
const okAuth = kind => `${kind} успешна`;
const errAuth = err => `Ошибка: ${err}`;
async function doRegister(){
  const out=$("regOut"); out.textContent="Отправляю запрос…";
  const dto = { email:$("regEmail").value.trim(), firstName:$("regFirst").value.trim(), lastName:$("regLast").value.trim(), password:$("regPwd").value, confirmPassword:$("regPwd2").value };
  try{ const r=await http("/auth/register",{method:"POST",body:JSON.stringify(dto)}); setJwt(r.token||r.Token); out.textContent=okAuth("Регистрация"); }
  catch(e){ try{ const p={Email:dto.email,FirstName:dto.firstName,LastName:dto.lastName,Password:dto.password,ConfirmPassword:dto.confirmPassword}; const r=await http("/auth/register",{method:"POST",body:JSON.stringify(p)}); setJwt(r.token||r.Token); out.textContent=okAuth("Регистрация"); } catch(e2){ out.innerHTML=`<span class="bad">${escapeHtml(errAuth(e2.message))}</span>`; } }
}
async function doLogin(){
  const out=$("loginOut"); out.textContent="Отправляю запрос…";
  const dto={ login:$("loginLogin").value.trim(), password:$("loginPwd").value };
  try{ const r=await http("/auth/login",{method:"POST",body:JSON.stringify(dto)}); setJwt(r.token||r.Token); out.textContent=okAuth("Авторизация"); }
  catch(e){ try{ const d2={ email:$("loginLogin").value.trim(), password:$("loginPwd").value }; const r2=await http("/auth/login",{method:"POST",body:JSON.stringify(d2)}); setJwt(r2.token||r2.Token); out.textContent=okAuth("Авторизация"); } catch(e2){ out.innerHTML=`<span class="bad">${escapeHtml(errAuth(e2.message))}</span>`; } }
}
async function getProgress(){ const out=$("progressOut"); out.textContent="Запрашиваю прогресс…"; try{ const r=await http("/progress/user_progress",{method:"GET"}); out.textContent=renderProgress(r); log({action:"progress",response:r}); }catch(e){ out.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; } }
async function startCheck(){ const out=$("checkOut"); const taskId=Number($("chkTaskId").value); const code=$("chkCode").value; if(!taskId||!code){ out.innerHTML=`<span class="warn">Нужны TaskId и код</span>`; return; } out.textContent="Отправляю код на проверку…"; try{ const r=await http("/orc/check",{method:"POST",body:JSON.stringify({taskId,origCode:code})}); out.innerHTML=renderCheckHTML(r); log({action:"orc/check",request:{taskId,origCode:code},response:r}); }catch(e){ out.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; } }
async function ping(){ const out=$("pingOut"); out.textContent="Пингуем…"; try{ const r=await http(`/users/${encodeURIComponent($("pingMsg").value||"hello")}`,{method:"GET"}); out.textContent=typeof r==="string"?`Ответ: ${r}`:`Ответ: ${JSON.stringify(r)}`; log({action:"ping",response:r}); }catch(e){ out.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; } }
document.addEventListener("DOMContentLoaded", ()=>{
  if($("apiBase")) $("apiBase").value = cfg.apiBase || "";
  const jwtS=$("jwtStatus"), uidS=$("uidStatus");
  if(jwtS) jwtS.textContent = cfg.jwt ? "есть" : "нет";
  if(uidS) uidS.textContent = cfg.userId || "нет";
  apiBase();
  if($("settingsOut")) $("settingsOut").textContent = "Готово. Использую базу API: " + (cfg.apiBase || "/api");
  const bind = (id, fn) => { const el=$(id); if(el) el.addEventListener("click", fn); };
  bind("saveCfgBtn", ()=>{ cfg.apiBase = ($("apiBase").value||"").trim(); saveCfg(); apiBase(); if($("settingsOut")) $("settingsOut").textContent = "Сохранено. База API: " + (cfg.apiBase || "/api"); });
  bind("resetCfgBtn", ()=>{ Object.keys(cfg).forEach(k=>delete cfg[k]); saveCfg(); location.reload(); });
  bind("clearJwtBtn", clearJwt);
  bind("regBtn", ()=>doRegister().catch(e=>{ const o=$("regOut"); if(o) o.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; }));
  bind("loginBtn", ()=>doLogin().catch(e=>{ const o=$("loginOut"); if(o) o.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; }));
  bind("progressBtn", ()=>getProgress().catch(e=>{ const o=$("progressOut"); if(o) o.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; }));
  bind("checkBtn", ()=>startCheck().catch(e=>{ const o=$("checkOut"); if(o) o.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; }));
  bind("pingBtn", ()=>ping().catch(e=>{ const o=$("pingOut"); if(o) o.innerHTML=`<span class="bad">Ошибка: ${escapeHtml(e.message)}</span>`; }));
});
