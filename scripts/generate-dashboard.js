const fs = require('fs');

// ===============================
// Load ZAP metadata
// ===============================
const zapData = JSON.parse(
  fs.readFileSync('scan-data/zap-metadata.json', 'utf8')
);

const currentScan = {
  timestamp: zapData.timestamp,
  run_id: zapData.run_id,
  branch: zapData.branch,
  commit: zapData.commit.substring(0, 7),
  zap: zapData.zap
};

// ===============================
// Load historical data
// ===============================
let historicalData = [];
const historyFile = 'dashboard/history.json';

if (fs.existsSync(historyFile)) {
  historicalData = JSON.parse(fs.readFileSync(historyFile, 'utf8'));
}

// Add current scan
historicalData.push(currentScan);

// Keep last 30 runs
if (historicalData.length > 30) {
  historicalData = historicalData.slice(-30);
}

// Ensure dashboard folder exists
fs.mkdirSync('dashboard', { recursive: true });

// Save updated history
fs.writeFileSync(historyFile, JSON.stringify(historicalData, null, 2));

// ===============================
// Trend Calculation
// ===============================
const latest = currentScan;
const previous =
  historicalData.length > 1
    ? historicalData[historicalData.length - 2]
    : null;

const latestTotal = Number.parseInt(latest.zap.total.total, 10);
const previousTotal = previous
  ? Number.parseInt(previous.zap.total.total, 10)
  : latestTotal;

const trend = latestTotal - previousTotal;

const trendIcon =
  trend > 0
    ? "⬆ Increasing"
    : trend < 0
    ? "⬇ Decreasing"
    : "→ No Change";

const trendColor =
  trend > 0
    ? "danger"
    : trend < 0
    ? "success"
    : "secondary";

// ===============================
// Build Chart Data
// ===============================
const labels = historicalData.map(h => h.commit);

const dotnetTotals = historicalData.map(h =>
  Number.parseInt(h.zap.dotnet.high, 10) +
  Number.parseInt(h.zap.dotnet.medium, 10) +
  Number.parseInt(h.zap.dotnet.low, 10)
);

const mlTotals = historicalData.map(h =>
  Number.parseInt(h.zap.ml.high, 10) +
  Number.parseInt(h.zap.ml.medium, 10) +
  Number.parseInt(h.zap.ml.low, 10)
);

const aggregatedTotals = historicalData.map(h =>
  Number.parseInt(h.zap.total.total, 10)
);

// ===============================
// Generate HTML
// ===============================
const html = `
<!DOCTYPE html>
<html>
<head>
  <title>ZAP Security Dashboard</title>
  <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">

  <style>
    body {
      background: #f4f6f9;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    }

    .header {
      background: linear-gradient(135deg, #667eea, #764ba2);
      color: white;
      padding: 3rem 1rem;
      border-radius: 0 0 20px 20px;
      margin-bottom: 2rem;
    }

    .card-stat {
      border: none;
      border-radius: 15px;
      transition: all 0.2s ease;
      box-shadow: 0 4px 10px rgba(0,0,0,0.08);
    }

    .card-stat:hover {
      transform: translateY(-5px);
      box-shadow: 0 8px 20px rgba(0,0,0,0.12);
    }

    .stat-value {
      font-size: 2.5rem;
      font-weight: bold;
    }

    canvas {
      max-height: 300px;
    }
  </style>
</head>

<body>

<div class="container-fluid px-4">

  <div class="header text-center">
    <h1>OWASP ZAP Security Dashboard</h1>
    <p>.NET + ML DAST Monitoring</p>
  </div>

  <div class="container">

    <!-- Latest Info -->
    <div class="card p-4 mb-4">
      <h5>Latest Scan</h5>
      <p><strong>Branch:</strong> ${latest.branch}</p>
      <p><strong>Commit:</strong> ${latest.commit}</p>
      <p><strong>Date:</strong> ${new Date(latest.timestamp).toLocaleString()}</p>

      <span class="badge bg-${trendColor}">
        ${trendIcon} (${trend > 0 ? "+" + trend : trend})
      </span>
    </div>

    <!-- ===================== -->
    <!-- .NET SECTION -->
    <!-- ===================== -->
    <h4>.NET Alerts</h4>
    <div class="row text-center mb-4">
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-danger">${latest.zap.dotnet.high}</div>
          <small>High</small>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-warning">${latest.zap.dotnet.medium}</div>
          <small>Medium</small>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-info">${latest.zap.dotnet.low}</div>
          <small>Low</small>
        </div>
      </div>
    </div>

    <!-- ===================== -->
    <!-- ML SECTION -->
    <!-- ===================== -->
    <h4>ML Service Alerts</h4>
    <div class="row text-center mb-4">
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-danger">${latest.zap.ml.high}</div>
          <small>High</small>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-warning">${latest.zap.ml.medium}</div>
          <small>Medium</small>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card card-stat p-3">
          <div class="stat-value text-info">${latest.zap.ml.low}</div>
          <small>Low</small>
        </div>
      </div>
    </div>

    <!-- ===================== -->
    <!-- Aggregated -->
    <!-- ===================== -->
    <h4>Aggregated Total</h4>
    <div class="row text-center mb-4">
      <div class="col-md-12">
        <div class="card card-stat p-3">
          <div class="stat-value">${latest.zap.total.total}</div>
          <small>Total Alerts (.NET + ML)</small>
        </div>
      </div>
    </div>

    <!-- ===================== -->
    <!-- Trend Chart -->
    <!-- ===================== -->
    <div class="card p-4 mb-4">
      <h5>Alert Trend Over Commits</h5>
      <canvas id="trendChart"></canvas>
    </div>

  </div>
</div>

<script>
const labels = ${JSON.stringify(labels)};
const dotnetTotals = ${JSON.stringify(dotnetTotals)};
const mlTotals = ${JSON.stringify(mlTotals)};
const aggregatedTotals = ${JSON.stringify(aggregatedTotals)};

new Chart(document.getElementById('trendChart'), {
  type: 'line',
  data: {
    labels: labels,
    datasets: [
      {
        label: '.NET Total',
        data: dotnetTotals,
        borderColor: '#0d6efd',
        tension: 0.3
      },
      {
        label: 'ML Total',
        data: mlTotals,
        borderColor: '#20c997',
        tension: 0.3
      },
      {
        label: 'Aggregated Total',
        data: aggregatedTotals,
        borderColor: '#6f42c1',
        borderDash: [5,5],
        tension: 0.3
      }
    ]
  },
  options: {
    responsive: true,
    scales: {
      y: { beginAtZero: true }
    }
  }
});
</script>

</body>
</html>
`;

fs.writeFileSync('dashboard/index.html', html);
console.log("Dashboard generated successfully.");