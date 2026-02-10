const fs = require('fs');
const path = require('path');

// Load scan metadata
const sastScaData = JSON.parse(fs.readFileSync('scan-data/sast-sca-metadata.json', 'utf8'));
const zapData = JSON.parse(fs.readFileSync('scan-data/zap-metadata.json', 'utf8'));

// Load historical data
let historicalData = [];
const historyFile = 'dashboard/history.json';
if (fs.existsSync(historyFile)) {
  historicalData = JSON.parse(fs.readFileSync(historyFile, 'utf8'));
}

// Add current scan to history
const currentScan = {
  timestamp: sastScaData.timestamp,
  run_id: sastScaData.run_id,
  run_number: sastScaData.run_number,
  branch: sastScaData.branch,
  commit: sastScaData.commit.substring(0, 7),
  sonarcloud: sastScaData.sonarcloud,
  dependency_check: sastScaData.dependency_check,
  zap: zapData.zap
};

historicalData.push(currentScan);

// Keep only last 30 scans
if (historicalData.length > 30) {
  historicalData = historicalData.slice(-30);
}

// Save updated history
fs.writeFileSync(historyFile, JSON.stringify(historicalData, null, 2));

// Generate HTML dashboard
const html = `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Security & Code Quality Dashboard - E-Waste Management</title>
  <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    :root {
      --primary: #0d6efd;
      --success: #198754;
      --danger: #dc3545;
      --warning: #ffc107;
      --info: #0dcaf0;
    }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      background: #f5f7fa;
    }
    .dashboard-header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 2rem 0;
      margin-bottom: 2rem;
    }
    .stat-card {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      margin-bottom: 1.5rem;
      transition: transform 0.2s;
    }
    .stat-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }
    .stat-value {
      font-size: 2.5rem;
      font-weight: 700;
      line-height: 1;
    }
    .stat-label {
      font-size: 0.875rem;
      color: #6c757d;
      margin-top: 0.5rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .chart-container {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      margin-bottom: 1.5rem;
    }
    .badge-status {
      padding: 0.5rem 1rem;
      border-radius: 20px;
      font-weight: 600;
    }
    .scan-info {
      background: white;
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 1rem;
    }
    .severity-high { color: #dc3545; }
    .severity-medium { color: #ffc107; }
    .severity-low { color: #0dcaf0; }
    .trend-up { color: #dc3545; }
    .trend-down { color: #198754; }
  </style>
</head>
<body>
  <div class="dashboard-header">
    <div class="container">
      <h1 class="mb-2"><i class="bi bi-shield-check"></i> Security & Code Quality Dashboard</h1>
      <p class="mb-0">E-Waste Management System - Continuous Monitoring</p>
    </div>
  </div>

  <div class="container">
    <!-- Latest Scan Info -->
    <div class="scan-info">
      <div class="row align-items-center">
        <div class="col-md-8">
          <h5 class="mb-1">Latest Scan #${currentScan.run_number}</h5>
          <small class="text-muted">
            <i class="bi bi-clock"></i> ${new Date(currentScan.timestamp).toLocaleString()}
            &nbsp;•&nbsp;
            <i class="bi bi-git"></i> ${currentScan.branch}
            &nbsp;•&nbsp;
            <i class="bi bi-code-square"></i> ${currentScan.commit}
          </small>
        </div>
        <div class="col-md-4 text-end">
          <a href="reports/dependency-check-report.html" class="btn btn-sm btn-outline-primary me-2" target="_blank">
            <i class="bi bi-file-earmark-text"></i> Dependency Report
          </a>
          <a href="https://sonarcloud.io/project/overview?id=GDipSA-Team-5_AD-Project" class="btn btn-sm btn-outline-primary" target="_blank">
            <i class="bi bi-cloud"></i> SonarCloud
          </a>
        </div>
      </div>
    </div>

    <!-- Summary Stats -->
    <div class="row">
      <div class="col-md-3">
        <div class="stat-card">
          <div class="stat-value text-${getSeverityColor(currentScan.sonarcloud.bugs)}">${currentScan.sonarcloud.bugs}</div>
          <div class="stat-label">Bugs</div>
          <small class="text-muted">SonarCloud</small>
        </div>
      </div>
      <div class="col-md-3">
        <div class="stat-card">
          <div class="stat-value text-${getSeverityColor(currentScan.sonarcloud.vulnerabilities)}">${currentScan.sonarcloud.vulnerabilities}</div>
          <div class="stat-label">Vulnerabilities</div>
          <small class="text-muted">SonarCloud</small>
        </div>
      </div>
      <div class="col-md-3">
        <div class="stat-card">
          <div class="stat-value text-${getSeverityColor(currentScan.dependency_check.total)}">${currentScan.dependency_check.total}</div>
          <div class="stat-label">Dependencies</div>
          <small class="text-muted">OWASP</small>
        </div>
      </div>
      <div class="col-md-3">
        <div class="stat-card">
          <div class="stat-value text-${getSeverityColor(currentScan.zap.total)}">${currentScan.zap.total}</div>
          <div class="stat-label">DAST Alerts</div>
          <small class="text-muted">ZAP</small>
        </div>
      </div>
    </div>

    <!-- Code Quality Metrics -->
    <div class="row">
      <div class="col-md-4">
        <div class="stat-card">
          <div class="stat-value text-success">${currentScan.sonarcloud.coverage}%</div>
          <div class="stat-label">Code Coverage</div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="stat-card">
          <div class="stat-value text-${getDuplicationColor(currentScan.sonarcloud.duplications)}">${currentScan.sonarcloud.duplications}%</div>
          <div class="stat-label">Code Duplication</div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="stat-card">
          <div class="stat-value text-info">${formatNumber(currentScan.sonarcloud.lines_of_code)}</div>
          <div class="stat-label">Lines of Code</div>
        </div>
      </div>
    </div>

    <!-- Charts -->
    <div class="row">
      <div class="col-md-6">
        <div class="chart-container">
          <h5 class="mb-3">SonarCloud Trends</h5>
          <canvas id="sonarTrendChart"></canvas>
        </div>
      </div>
      <div class="col-md-6">
        <div class="chart-container">
          <h5 class="mb-3">Dependency Vulnerabilities</h5>
          <canvas id="dependencyChart"></canvas>
        </div>
      </div>
    </div>

    <div class="row">
      <div class="col-md-6">
        <div class="chart-container">
          <h5 class="mb-3">OWASP ZAP Findings</h5>
          <canvas id="zapChart"></canvas>
        </div>
      </div>
      <div class="col-md-6">
        <div class="chart-container">
          <h5 class="mb-3">Coverage Trend</h5>
          <canvas id="coverageChart"></canvas>
        </div>
      </div>
    </div>

    <!-- Detailed Breakdown -->
    <div class="row">
      <div class="col-md-4">
        <div class="chart-container">
          <h5 class="mb-3">Dependency Check Breakdown</h5>
          <canvas id="dependencyPieChart"></canvas>
          <div class="mt-3">
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-high"><i class="bi bi-circle-fill"></i> Critical</span>
              <strong>${currentScan.dependency_check.critical}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-high"><i class="bi bi-circle-fill"></i> High</span>
              <strong>${currentScan.dependency_check.high}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-medium"><i class="bi bi-circle-fill"></i> Medium</span>
              <strong>${currentScan.dependency_check.medium}</strong>
            </div>
            <div class="d-flex justify-content-between">
              <span class="severity-low"><i class="bi bi-circle-fill"></i> Low</span>
              <strong>${currentScan.dependency_check.low}</strong>
            </div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="chart-container">
          <h5 class="mb-3">ZAP Scan Breakdown</h5>
          <canvas id="zapPieChart"></canvas>
          <div class="mt-3">
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-high"><i class="bi bi-circle-fill"></i> High</span>
              <strong>${currentScan.zap.high}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-medium"><i class="bi bi-circle-fill"></i> Medium</span>
              <strong>${currentScan.zap.medium}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-low"><i class="bi bi-circle-fill"></i> Low</span>
              <strong>${currentScan.zap.low}</strong>
            </div>
            <div class="d-flex justify-content-between">
              <span class="text-muted"><i class="bi bi-circle-fill"></i> Info</span>
              <strong>${currentScan.zap.info}</strong>
            </div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="chart-container">
          <h5 class="mb-3">SonarCloud Issues</h5>
          <canvas id="sonarPieChart"></canvas>
          <div class="mt-3">
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-high"><i class="bi bi-circle-fill"></i> Bugs</span>
              <strong>${currentScan.sonarcloud.bugs}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-high"><i class="bi bi-circle-fill"></i> Vulnerabilities</span>
              <strong>${currentScan.sonarcloud.vulnerabilities}</strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span class="severity-medium"><i class="bi bi-circle-fill"></i> Code Smells</span>
              <strong>${currentScan.sonarcloud.code_smells}</strong>
            </div>
            <div class="d-flex justify-content-between">
              <span class="severity-medium"><i class="bi bi-circle-fill"></i> Security Hotspots</span>
              <strong>${currentScan.sonarcloud.security_hotspots}</strong>
            </div>
          </div>
        </div>
      </div>
    </div>

    <footer class="text-center text-muted py-4 mt-4">
      <small>Last updated: ${new Date().toLocaleString()} • Generated by GitHub Actions</small>
    </footer>
  </div>

  <script>
    const historicalData = ${JSON.stringify(historicalData)};
    
    // Extract data for charts
    const labels = historicalData.map(d => \`#\${d.run_number}\`);
    const bugs = historicalData.map(d => parseInt(d.sonarcloud.bugs));
    const vulnerabilities = historicalData.map(d => parseInt(d.sonarcloud.vulnerabilities));
    const codeSmells = historicalData.map(d => parseInt(d.sonarcloud.code_smells));
    const coverage = historicalData.map(d => parseFloat(d.sonarcloud.coverage));
    const depTotal = historicalData.map(d => parseInt(d.dependency_check.total));

    // SonarCloud Trend Chart
    new Chart(document.getElementById('sonarTrendChart'), {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Bugs',
            data: bugs,
            borderColor: '#dc3545',
            backgroundColor: 'rgba(220, 53, 69, 0.1)',
            tension: 0.4
          },
          {
            label: 'Vulnerabilities',
            data: vulnerabilities,
            borderColor: '#fd7e14',
            backgroundColor: 'rgba(253, 126, 20, 0.1)',
            tension: 0.4
          },
          {
            label: 'Code Smells',
            data: codeSmells,
            borderColor: '#ffc107',
            backgroundColor: 'rgba(255, 193, 7, 0.1)',
            tension: 0.4
          }
        ]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { position: 'bottom' }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    // Dependency Trend Chart
    new Chart(document.getElementById('dependencyChart'), {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Total Vulnerabilities',
          data: depTotal,
          borderColor: '#dc3545',
          backgroundColor: 'rgba(220, 53, 69, 0.1)',
          tension: 0.4,
          fill: true
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    // ZAP Chart
    new Chart(document.getElementById('zapChart'), {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [{
          label: 'Total Alerts',
          data: historicalData.map(d => parseInt(d.zap.total)),
          backgroundColor: '#0dcaf0'
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    // Coverage Trend
    new Chart(document.getElementById('coverageChart'), {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Code Coverage %',
          data: coverage,
          borderColor: '#198754',
          backgroundColor: 'rgba(25, 135, 84, 0.1)',
          tension: 0.4,
          fill: true
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        },
        scales: {
          y: { 
            beginAtZero: true,
            max: 100,
            ticks: {
              callback: value => value + '%'
            }
          }
        }
      }
    });

    // Dependency Pie Chart
    new Chart(document.getElementById('dependencyPieChart'), {
      type: 'doughnut',
      data: {
        labels: ['Critical', 'High', 'Medium', 'Low'],
        datasets: [{
          data: [
            ${currentScan.dependency_check.critical},
            ${currentScan.dependency_check.high},
            ${currentScan.dependency_check.medium},
            ${currentScan.dependency_check.low}
          ],
          backgroundColor: ['#721c24', '#dc3545', '#ffc107', '#0dcaf0']
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        }
      }
    });

    // ZAP Pie Chart
    new Chart(document.getElementById('zapPieChart'), {
      type: 'doughnut',
      data: {
        labels: ['High', 'Medium', 'Low', 'Info'],
        datasets: [{
          data: [
            ${currentScan.zap.high},
            ${currentScan.zap.medium},
            ${currentScan.zap.low},
            ${currentScan.zap.info}
          ],
          backgroundColor: ['#dc3545', '#ffc107', '#0dcaf0', '#6c757d']
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        }
      }
    });

    // SonarCloud Pie Chart
    new Chart(document.getElementById('sonarPieChart'), {
      type: 'doughnut',
      data: {
        labels: ['Bugs', 'Vulnerabilities', 'Code Smells', 'Security Hotspots'],
        datasets: [{
          data: [
            ${currentScan.sonarcloud.bugs},
            ${currentScan.sonarcloud.vulnerabilities},
            ${currentScan.sonarcloud.code_smells},
            ${currentScan.sonarcloud.security_hotspots}
          ],
          backgroundColor: ['#dc3545', '#fd7e14', '#ffc107', '#0dcaf0']
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        }
      }
    });
  </script>
</body>
</html>
`;

fs.writeFileSync('dashboard/index.html', html);
console.log('✅ Dashboard generated successfully!');

function getSeverityColor(value) {
  const num = parseInt(value);
  if (num === 0) return 'success';
  if (num < 5) return 'warning';
  return 'danger';
}

function getDuplicationColor(value) {
  const num = parseFloat(value);
  if (num < 3) return 'success';
  if (num < 5) return 'warning';
  return 'danger';
}

function formatNumber(num) {
  return parseInt(num).toLocaleString();
}