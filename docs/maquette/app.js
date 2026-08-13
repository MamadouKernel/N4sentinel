// N4 Sentinel - Client JavaScript (Dynamic Real-Time SignalR & Dual Theme)
let currentEnv = 'PROD';
let activeWorkflow = null;
let signalRConnection = null;

document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initClock();
  initNavigation();
  initEnvSelector();
  initSignalR();
  loadSupervision();
  loadWorkflows();
  loadLogs();
  loadSharedFoldersAndEdi();
  loadAudit();

  // Attach event listeners
  document.getElementById('btnRefreshComponents').addEventListener('click', loadSupervision);
  document.getElementById('btnNewShutdown').addEventListener('click', () => startWorkflow('FULL_SHUTDOWN'));
  document.getElementById('btnNewStartup').addEventListener('click', () => startWorkflow('FULL_STARTUP'));
  document.getElementById('btnRunDiag').addEventListener('click', runDiagnostic);
  document.getElementById('logLevelSelect').addEventListener('change', loadLogs);
  document.getElementById('logSearchInput').addEventListener('input', loadLogs);
  document.getElementById('btnSendChat').addEventListener('click', sendChatMessage);
  document.getElementById('chatInput').addEventListener('keypress', (e) => { if (e.key === 'Enter') sendChatMessage(); });

  document.getElementById('btnCancelOverride').addEventListener('click', closeOverrideModal);

  // Simulation controls
  document.getElementById('btnSimulateAmq').addEventListener('click', () => simulateFailure('CENTER_NODE', 'Corruption du magasin KahaDB ActiveMQ'));
  document.getElementById('btnSimulateCpu').addEventListener('click', () => simulateFailure('CLUSTER_NODE_3', 'Saturation CPU > 98% par requête SQL vrac'));
  document.getElementById('btnRestoreAll').addEventListener('click', restoreAllHealthy);
});

function initTheme() {
  const toggleBtn = document.getElementById('themeToggleBtn');
  const savedTheme = localStorage.getItem('n4_theme') || 'dark';

  applyTheme(savedTheme);

  toggleBtn.addEventListener('click', () => {
    const isLight = document.body.classList.contains('theme-light');
    const newTheme = isLight ? 'dark' : 'light';
    applyTheme(newTheme);
  });
}

function applyTheme(theme) {
  const toggleBtn = document.getElementById('themeToggleBtn');
  if (theme === 'light') {
    document.body.classList.add('theme-light');
    toggleBtn.innerText = '☀️ Mode Clair';
  } else {
    document.body.classList.remove('theme-light');
    toggleBtn.innerText = '🌙 Mode Sombre';
  }
  localStorage.setItem('n4_theme', theme);
}

function initClock() {
  setInterval(() => {
    const now = new Date();
    document.getElementById('serverClock').innerText = `UTC ${now.toISOString().substring(11, 19)} (NTP Sync)`;
  }, 1000);
}

function initNavigation() {
  const tabs = document.querySelectorAll('.nav-tab');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');

      const targetView = tab.getAttribute('data-tab');
      document.querySelectorAll('.view-section').forEach(sec => sec.classList.remove('active'));
      const activeSec = document.getElementById(`view-${targetView}`);
      if (activeSec) activeSec.classList.add('active');
    });
  });
}

function initEnvSelector() {
  const select = document.getElementById('envSelect');
  select.addEventListener('change', (e) => {
    currentEnv = e.target.value;
    loadSupervision();
  });
}

function initSignalR() {
  try {
    signalRConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/n4sentinel')
      .withAutomaticReconnect()
      .build();

    signalRConnection.on('ReceiveTelemetryUpdate', (envId, components) => {
      if (envId.toUpperCase() === currentEnv.toUpperCase()) {
        renderComponentGrid(components);
        attachDiagramInteractivity(components);
        updateKpiBanner(components);
      }
    });

    signalRConnection.start().then(() => {
      console.log('SignalR connected to /hubs/n4sentinel');
    }).catch(err => console.error('SignalR error:', err));
  } catch (e) {
    console.error('SignalR init failed:', e);
  }
}

// -------------------------------------------------------------
// View 1 & Diagram: Supervision 360° & Diagram 1:1 Map
// -------------------------------------------------------------
async function loadSupervision() {
  try {
    const res = await fetch(`/api/n4/components?envId=${currentEnv}`);
    const components = await res.json();

    renderComponentGrid(components);
    attachDiagramInteractivity(components);
    updateKpiBanner(components);
  } catch (err) {
    console.error('Error loading supervision components:', err);
  }
}

function updateKpiBanner(components) {
  const centerNode = components.find(c => c.code === 'CENTER_NODE');

  if (centerNode) {
    const isOk = centerNode.status === 0;
    document.getElementById('kpiCenterNode').innerText = isOk ? 'ONLINE' : 'DEGRADED';
    const badge = document.getElementById('kpiCenterNodeBadge');
    badge.className = `badge-status ${isOk ? 'healthy' : 'critical'}`;
    badge.innerHTML = `<span class="dot-pulse"></span> ${isOk ? 'OK' : 'ALERT'}`;
  }

  const criticals = components.filter(c => c.status !== 0).length;
  document.getElementById('kpiIncidents').innerText = `${criticals} ${criticals > 0 ? 'CRITIQUE(S)' : 'Majeurs'}`;
  const incBadge = document.getElementById('kpiIncidentsBadge');
  incBadge.className = `badge-status ${criticals > 0 ? 'critical' : 'healthy'}`;
  incBadge.innerText = criticals > 0 ? 'ATTENTION' : 'AUCUN';
}

function renderComponentGrid(components) {
  const grid = document.getElementById('componentGrid');
  grid.innerHTML = '';

  components.forEach(comp => {
    const statusClass = comp.status === 0 ? 'healthy' : comp.status === 1 ? 'warning' : 'critical';
    const statusLabel = comp.status === 0 ? 'HEALTHY' : comp.status === 1 ? 'WARNING' : 'CRITICAL';

    const card = document.createElement('div');
    card.className = 'card';
    card.style.borderColor = comp.status !== 0 ? 'var(--color-danger)' : 'var(--border-color)';
    card.innerHTML = `
      <div class="card-header">
        <div>
          <div class="card-title">${comp.name}</div>
          <div class="card-subtitle">${comp.hostname} (${comp.ipAddress}:${comp.port})</div>
        </div>
        <span class="badge-status ${statusClass}"><span class="dot-pulse"></span> ${statusLabel}</span>
      </div>

      <p style="font-size:0.8rem; color:${comp.status !== 0 ? 'var(--color-danger)' : 'var(--text-muted)'}; margin-bottom:12px;">${comp.detailMessage}</p>

      <div class="metrics-row">
        <div class="metric-item">
          <label>Util. CPU</label>
          <val style="color:${comp.cpuPercent > 75 ? 'var(--color-danger)' : 'var(--color-primary)'}">${comp.cpuPercent.toFixed(1)}%</val>
        </div>
        <div class="metric-item">
          <label>Util. RAM</label>
          <val style="color:${comp.memoryPercent > 80 ? 'var(--color-warning)' : 'var(--color-primary)'}">${comp.memoryPercent.toFixed(1)}%</val>
        </div>
      </div>

      <div style="display:flex; justify-content:space-between; font-size:0.75rem; color:var(--text-dim);">
        <span>Queue Count: <strong>${comp.queueCount}</strong></span>
        <span>Heartbeat: <strong>${new Date(comp.lastHeartbeat).toLocaleTimeString()}</strong></span>
      </div>
    `;
    grid.appendChild(card);
  });
}

function attachDiagramInteractivity(components) {
  const nodeMapping = {
    'node-GOS': 'EXT_GOS',
    'node-SFTP': 'EXT_SFTP_EDI',
    'node-BILLING': 'EXT_BILLING',
    'node-LB': 'LOAD_BALANCER',
    'node-CN1': 'CLUSTER_NODE_1',
    'node-CN2': 'CLUSTER_NODE_2',
    'node-CN3': 'CLUSTER_NODE_3',
    'node-CN4': 'CLUSTER_NODE_4',
    'node-GATE': 'GATE_NODE',
    'node-EDI': 'EDI_NODE',
    'node-SA': 'SMART_ACCESS_NODE',
    'node-PE': 'PARTENAIRE_EXTERNE',
    'node-DB-HOST': 'DATABASE_HOST',
    'node-DB-REP': 'DATABASE_REPLICATED',
    'node-CN': 'CENTER_NODE',
    'node-SBY': 'STANDBY_NODE',
    'node-SF': 'SHARE_FOLDER',
    'node-XPS': 'XPS_BENTO_DISPATCHER',
    'node-ECN4': 'ECN4_WEB_VMT',
    'node-ECN4WEB': 'ECN4_WEB_VMT',
    'node-DGPS': 'EXT_DGPS',
    'node-VBS': 'EXT_VBS',
    'node-HYPERION': 'EXT_DGPS',
    'node-REEFER': 'EXT_REEFER'
  };

  Object.keys(nodeMapping).forEach(elemId => {
    const el = document.getElementById(elemId);
    if (!el) return;

    const compCode = nodeMapping[elemId];
    const comp = components.find(c => c.code === compCode);

    if (comp) {
      if (comp.status !== 0) {
        el.style.background = 'linear-gradient(180deg, #ef4444 0%, #b91c1c 100%)';
        el.style.color = '#ffffff';
        el.style.borderColor = '#991b1b';
        el.style.animation = 'pulse 1s infinite';
      } else {
        el.style.background = '';
        el.style.borderColor = '';
        el.style.animation = '';
      }

      el.title = `⚡ [METRIQUES TEMPS REEL]\nComposant: ${comp.name}\nIP: ${comp.ipAddress}:${comp.port}\nCPU: ${comp.cpuPercent.toFixed(1)}% | RAM: ${comp.memoryPercent.toFixed(1)}%\nQueue: ${comp.queueCount}\nStatut: ${comp.detailMessage}`;
      el.onclick = () => {
        alert(`📊 [Fiche Santé N4 Diagramme Temps Réel]\n\nComposant: ${comp.name}\nHostname: ${comp.hostname}\nAdresse IP: ${comp.ipAddress}:${comp.port}\nCharge CPU: ${comp.cpuPercent.toFixed(1)}%\nCharge RAM: ${comp.memoryPercent.toFixed(1)}%\nFiles de messages: ${comp.queueCount}\n\nDétail opérationnel:\n${comp.detailMessage}`);
      };
    }
  });
}

async function simulateFailure(componentCode, symptom) {
  try {
    await fetch('/api/n4/simulate/failure', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ envId: currentEnv, componentCode, symptom })
    });
    loadSupervision();
    loadLogs();
    loadAudit();
  } catch (err) {
    console.error('Error simulating failure:', err);
  }
}

async function restoreAllHealthy() {
  try {
    await fetch('/api/n4/simulate/restore', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ envId: currentEnv })
    });
    loadSupervision();
    loadLogs();
    loadAudit();
  } catch (err) {
    console.error('Error restoring health:', err);
  }
}

// -------------------------------------------------------------
// View 2: Pilotage & Workflows
// -------------------------------------------------------------
async function loadWorkflows() {
  try {
    const res = await fetch('/api/n4/workflows');
    const history = await res.json();

    activeWorkflow = history.find(w => w.overallStatus === 'RUNNING' || w.overallStatus === 'PAUSED') || history[0];
    renderWorkflow(activeWorkflow);
  } catch (err) {
    console.error('Error loading workflows:', err);
  }
}

async function startWorkflow(scenarioId) {
  try {
    const res = await fetch('/api/n4/workflows/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        scenarioId: scenarioId,
        envId: currentEnv,
        user: 'Aldric GNAMIAN (DSI)',
        ticketRef: 'INC-2026-N4-001',
        justification: 'Opération ordonnancée N4 Sentinel V1'
      })
    });
    activeWorkflow = await res.json();
    renderWorkflow(activeWorkflow);
    document.querySelector('.nav-tab[data-tab="pilotage"]').click();
  } catch (err) {
    console.error('Error starting workflow:', err);
  }
}

function renderWorkflow(wf) {
  const container = document.getElementById('activeWorkflowContainer');
  if (!wf) {
    container.innerHTML = `<div class="card"><p>Aucun workflow d'opération en cours sur l'environnement ${currentEnv}.</p></div>`;
    return;
  }

  const isRunning = wf.overallStatus === 'RUNNING';
  const isPaused = wf.overallStatus === 'PAUSED';

  let html = `
    <div class="card" style="margin-bottom:20px;">
      <div style="display:flex; justify-content:space-between; align-items:center;">
        <div>
          <h3 style="font-size:1.1rem; color:var(--color-primary);">${wf.scenarioTitle}</h3>
          <div style="font-size:0.8rem; color:var(--text-muted); margin-top:4px;">
            ID: <strong>${wf.executionId}</strong> | Ticket: <strong>${wf.ticketReference}</strong> | Lancé par: <strong>${wf.startedBy}</strong>
          </div>
        </div>
        <div style="display:flex; gap:12px;">
          ${isRunning ? `<button class="btn btn-secondary" onclick="pauseWorkflow('${wf.executionId}')">⏸️ Suspendre</button>` : ''}
          ${isPaused ? `<button class="btn" onclick="resumeWorkflow('${wf.executionId}')">▶️ Reprendre</button>` : ''}
          ${isRunning ? `<button class="btn" onclick="advanceStep('${wf.executionId}')">⏩ Étape Suivante (Valider)</button>` : ''}
        </div>
      </div>
    </div>

    <div class="workflow-stepper">
  `;

  wf.steps.forEach((step, idx) => {
    const isCurrent = idx === wf.currentStepIndex && wf.overallStatus !== 'COMPLETED';
    const isDone = step.status === 2 || (idx < wf.currentStepIndex);
    const statusClass = isCurrent ? 'in-progress' : isDone ? 'success' : '';

    html += `
      <div class="step-card ${statusClass}">
        <div class="step-number">${step.stepNumber}</div>
        <div class="step-content">
          <div style="display:flex; justify-content:space-between;">
            <div class="step-title">${step.name}</div>
            ${step.requiresDualValidation ? `<span style="font-size:0.75rem; background:rgba(245,158,11,0.2); color:var(--color-warning); padding:2px 8px; border-radius:4px; font-weight:700;">⚠️ DOUBLE VALIDATION</span>` : ''}
          </div>
          <div class="step-desc">Pré-check: ${step.preCheckDescription}</div>
          <div class="step-cmd">${step.actionCommand}</div>

          ${step.executionLogs && step.executionLogs.length > 0 ? `
            <div style="margin-top:10px; padding:8px; background:#040812; border-radius:4px; font-family:var(--font-family-mono); font-size:0.75rem; color:#f8fafc;">
              ${step.executionLogs.map(l => `<div>${l}</div>`).join('')}
            </div>
          ` : ''}
        </div>
      </div>
    `;
  });

  html += `</div>`;
  container.innerHTML = html;
}

window.advanceStep = async function(executionId) {
  const currentStep = activeWorkflow.steps[activeWorkflow.currentStepIndex];
  if (currentStep && currentStep.requiresDualValidation) {
    document.getElementById('overrideModalText').innerText = `L'étape "${currentStep.name}" requiert une validation explicite avant exécution réelle sur l'écosystème N4.`;
    document.getElementById('overrideModal').style.display = 'flex';
    document.getElementById('btnConfirmOverride').onclick = async () => {
      const justif = document.getElementById('overrideJustificationInput').value;
      if (!justif) { alert('Veuillez saisir une justification.'); return; }
      closeOverrideModal();
      await executeAdvance(executionId, true, justif);
    };
  } else {
    await executeAdvance(executionId, false);
  }
};

async function executeAdvance(executionId, override, justification) {
  try {
    const res = await fetch('/api/n4/workflows/advance', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        executionId: executionId,
        user: 'Aldric GNAMIAN (DSI)',
        overrideAndProceed: override,
        overrideJustification: justification
      })
    });
    activeWorkflow = await res.json();
    renderWorkflow(activeWorkflow);
    loadSupervision();
    loadAudit();
  } catch (err) {
    console.error('Error advancing workflow step:', err);
  }
}

window.pauseWorkflow = async function(executionId) {
  const res = await fetch('/api/n4/workflows/pause', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ executionId, user: 'Aldric GNAMIAN (DSI)' })
  });
  activeWorkflow = await res.json();
  renderWorkflow(activeWorkflow);
};

window.resumeWorkflow = async function(executionId) {
  const res = await fetch('/api/n4/workflows/resume', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ executionId, user: 'Aldric GNAMIAN (DSI)' })
  });
  activeWorkflow = await res.json();
  renderWorkflow(activeWorkflow);
};

function closeOverrideModal() {
  document.getElementById('overrideModal').style.display = 'none';
  document.getElementById('overrideJustificationInput').value = '';
}

// -------------------------------------------------------------
// View 3: Diagnostic Engine
// -------------------------------------------------------------
async function runDiagnostic() {
  const symptomCode = document.getElementById('diagSymptomSelect').value;
  try {
    const res = await fetch('/api/n4/diagnostic', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ symptomCode, environmentId: currentEnv })
    });
    const result = await res.json();
    renderDiagnosticResult(result);
  } catch (err) {
    console.error('Error running diagnostic:', err);
  }
}

function renderDiagnosticResult(res) {
  const container = document.getElementById('diagnosticResultsContainer');
  container.innerHTML = `
    <div class="card" style="border-color:var(--color-primary);">
      <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px;">
        <div>
          <h3 style="font-size:1.1rem; color:var(--text-main);">Résultat du Diagnostic: ${res.symptomName}</h3>
          <div style="font-size:0.8rem; color:var(--text-muted);">Réf ID: ${res.diagnosticId}</div>
        </div>
        <div style="text-align:right;">
          <span style="font-size:1.5rem; font-weight:800; color:var(--color-primary);">${res.confidencePercent}%</span>
          <div style="font-size:0.7rem; color:var(--text-muted); text-transform:uppercase;">Niveau de Confiance</div>
        </div>
      </div>

      <div style="background:var(--bg-dark); padding:16px; border-radius:8px; margin-bottom:16px;">
        <strong style="color:var(--color-warning); display:block; margin-bottom:6px;">Hypothèse Principale Retenue :</strong>
        <p style="font-size:0.9rem;">${res.mainHypothesis}</p>
      </div>

      <div style="margin-bottom:16px;">
        <strong style="font-size:0.85rem; color:var(--text-main); display:block; margin-bottom:8px;">Preuves Techniques Collectées :</strong>
        <ul style="padding-left:20px; font-size:0.85rem; color:var(--text-muted);">
          ${res.technicalEvidences.map(e => `<li style="margin-bottom:4px;">${e}</li>`).join('')}
        </ul>
      </div>

      <div style="background:rgba(16,185,129,0.1); border:1px solid rgba(16,185,129,0.3); padding:16px; border-radius:8px;">
        <strong style="color:var(--color-success); display:block; margin-bottom:8px;">Procedure Opératoire Recommandée (SOP) :</strong>
        <h4 style="font-size:0.95rem; margin-bottom:8px;">${res.recommendedSopTitle}</h4>
        <ol style="padding-left:20px; font-size:0.85rem; color:var(--text-main);">
          ${res.recommendedActionSteps.map(s => `<li style="margin-bottom:4px;">${s}</li>`).join('')}
        </ol>
      </div>
    </div>
  `;
}

// -------------------------------------------------------------
// View 4: Log Analyzer
// -------------------------------------------------------------
async function loadLogs() {
  const level = document.getElementById('logLevelSelect').value;
  const search = document.getElementById('logSearchInput').value;

  try {
    const res = await fetch(`/api/n4/logs?level=${level}&search=${encodeURIComponent(search)}`);
    const logs = await res.json();

    const consoleDiv = document.getElementById('logConsole');
    consoleDiv.innerHTML = '';

    logs.forEach(l => {
      const line = document.createElement('div');
      line.className = `log-line ${l.level}`;
      line.innerHTML = `[${new Date(l.timestamp).toLocaleTimeString()}] [${l.level}] [${l.componentCode}] [${l.logger}] - ${l.message}`;
      consoleDiv.appendChild(line);
    });
  } catch (err) {
    console.error('Error loading logs:', err);
  }
}

// -------------------------------------------------------------
// View 5: Assistant N4 RAG
// -------------------------------------------------------------
async function sendChatMessage() {
  const input = document.getElementById('chatInput');
  const question = input.value.trim();
  if (!question) return;

  const messagesDiv = document.getElementById('chatMessages');

  // Add User bubble
  const userMsg = document.createElement('div');
  userMsg.className = 'chat-bubble user';
  userMsg.innerText = question;
  messagesDiv.appendChild(userMsg);
  input.value = '';
  messagesDiv.scrollTop = messagesDiv.scrollHeight;

  try {
    const res = await fetch('/api/n4/assistant', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ question })
    });
    const resp = await res.json();

    const botMsg = document.createElement('div');
    botMsg.className = 'chat-bubble assistant';
    
    let citationsHtml = '';
    if (resp.citations && resp.citations.length > 0) {
      citationsHtml = resp.citations.map(c => `
        <div class="citation-box">
          📖 <strong>Source :</strong> ${c.documentSource} — <em>${c.sectionTitle} (${c.pageOrChapter})</em><br>
          <span style="font-style:italic;">"${c.excerpt}"</span>
        </div>
      `).join('');
    }

    botMsg.innerHTML = `
      <div>${resp.answer}</div>
      ${citationsHtml}
    `;
    messagesDiv.appendChild(botMsg);
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
  } catch (err) {
    console.error('Error sending chat message:', err);
  }
}

// -------------------------------------------------------------
// View 6: Shared Folders & EDI
// -------------------------------------------------------------
async function loadSharedFoldersAndEdi() {
  try {
    const resSf = await fetch('/api/n4/shared-folders');
    const folders = await resSf.json();

    const sfContainer = document.getElementById('sharedFoldersContainer');
    sfContainer.innerHTML = folders.map(f => `
      <div style="padding:12px; border-bottom:1px solid var(--border-color); font-size:0.85rem;">
        <div style="display:flex; justify-content:space-between; font-weight:700;">
          <span>${f.name}</span>
          <span style="color:var(--color-success)">${f.freeSpaceGB} GB Libres</span>
        </div>
        <div style="font-family:var(--font-family-mono); font-size:0.75rem; color:var(--text-muted); margin-top:2px;">${f.uncPath}</div>
      </div>
    `).join('');

    const resEdi = await fetch('/api/n4/edi');
    const edis = await resEdi.json();

    const ediContainer = document.getElementById('ediContainer');
    ediContainer.innerHTML = edis.map(e => `
      <div style="padding:12px; border-bottom:1px solid var(--border-color); font-size:0.85rem;">
        <div style="display:flex; justify-content:space-between; font-weight:700;">
          <span>${e.name} (${e.partner})</span>
          <span style="color:var(--color-primary)">${e.processedCount} Traités</span>
        </div>
        <div style="font-size:0.75rem; color:var(--text-muted); margin-top:2px;">Dernier flux: ${new Date(e.lastActivity).toLocaleTimeString()}</div>
      </div>
    `).join('');
  } catch (err) {
    console.error('Error loading shared folders/EDI:', err);
  }
}

// -------------------------------------------------------------
// View 7: Registre d'Audit
// -------------------------------------------------------------
async function loadAudit() {
  try {
    const res = await fetch('/api/n4/audit');
    const events = await res.json();

    const tbody = document.getElementById('auditTableBody');
    tbody.innerHTML = events.map(ev => `
      <tr style="border-bottom:1px solid var(--border-color);">
        <td style="padding:10px; font-family:var(--font-family-mono);">${new Date(ev.timestamp).toLocaleTimeString()}</td>
        <td style="padding:10px; font-weight:600;">${ev.user}</td>
        <td style="padding:10px; color:var(--color-primary); font-weight:700;">${ev.action}</td>
        <td style="padding:10px;">${ev.category}</td>
        <td style="padding:10px;">${ev.target}</td>
        <td style="padding:10px;"><span style="color:${ev.riskLevel === 'HIGH' || ev.riskLevel === 'CRITICAL' ? 'var(--color-danger)' : 'var(--color-success)'}; font-weight:700;">${ev.riskLevel}</span></td>
        <td style="padding:10px; color:var(--text-muted);">${ev.details}</td>
      </tr>
    `).join('');
  } catch (err) {
    console.error('Error loading audit events:', err);
  }
}
