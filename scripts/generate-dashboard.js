const latest = currentScan;
const previous = historicalData.length > 1
  ? historicalData[historicalData.length - 2]
  : null;

const trend =
  previous
    ? latest.zap.total - previous.zap.total
    : 0;

const trendIcon =
  trend > 0 ? "⬆ Increasing"
  : trend < 0 ? "⬇ Decreasing"
  : "→ No Change";

const trendColor =
  trend > 0 ? "danger"
  : trend < 0 ? "success"
  : "secondary";

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

    .trend-badge {
      font-size: 0.9rem;
      padding: 0.4rem 0.8rem;
      border-radius: 20px;
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
    <p class="mb-0">.NET + ML Aggregated DAST Monitoring</p>
  </div>

  <div class="container">

    <!-- Latest Info -->
    <div class="card p-4 mb-4">
      <h5>Latest Scan</h5>
      <p class="mb-1"><strong>Branch:</strong> ${latest.branch}</p>
      <p class="mb-1"><strong>Commit:</strong> ${latest.commit}</p>
      <p class="mb-0"><strong>Date:</strong> ${new Date(latest.timestamp).toLocaleString()}</p>

      <div class="mt-3">
        <span class="badge bg-${trendColor} trend-badge">
          ${trendIcon} (${trend > 0 ? "+" + trend : trend})
        </span>
      </div>
    </div>

    <!-- Stat Cards -->
    <div class="row text-center mb-4">

      <div class="col-md-3">
        <div class="card card-stat p-3">
          <div class="stat-value text-danger">${latest.zap.high}</div>
          <small>High Severity</small>
        </div>
      </div>

      <div class="col-md-3">
        <div class="card card-stat p-3">
          <div class="stat-value text-warning">${latest.zap.medium}</div>
          <small>Medium Severity</small>
        </div>
      </div>

      <div class="col-md-3">
        <div class="card card-stat p-3">
          <div class="stat-value text-info">${latest.zap.low}</div>
          <small>Low Severity</small>
        </div>
      </div>

      <div class="col-md-3">
        <div class="card card-stat p-3">
          <div class="stat-value">${latest.zap.total}</div>
          <small>Total Alerts</small>
        </div>
      </div>

    </div>

    <!-- Trend Chart -->
    <div class="card p-4 mb-4">
      <h5>Alert Trend Over Commits</h5>
      <canvas id="trendChart"></canvas>
    </div>

    <!-- Pie Breakdown -->
    <div class="card p-4">
      <h5>Latest Severity Breakdown</h5>
      <canvas id="pieChart"></canvas>
    </div>

  </div>
</div>

<script>
const history = ${JSON.stringify(historicalData)};
const labels = history.map(h => h.commit);
const totals = history.map(h => parseInt(h.zap.total));

new Chart(document.getElementById('trendChart'), {
  type: 'line',
  data: {
    labels: labels,
    datasets: [{
      label: 'Total Alerts',
      data: totals,
      borderColor: '#667eea',
      backgroundColor: 'rgba(102,126,234,0.2)',
      fill: true,
      tension: 0.3
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

new Chart(document.getElementById('pieChart'), {
  type: 'doughnut',
  data: {
    labels: ['High', 'Medium', 'Low'],
    datasets: [{
      data: [
        ${latest.zap.high},
        ${latest.zap.medium},
        ${latest.zap.low}
      ],
      backgroundColor: ['#dc3545', '#ffc107', '#0dcaf0']
    }]
  },
  options: {
    responsive: true,
    plugins: {
      legend: { position: 'bottom' }
    }
  }
});
</script>

</body>
</html>
`;