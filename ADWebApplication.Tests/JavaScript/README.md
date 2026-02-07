# JavaScript Tests

Unit tests for ADWebApplication JavaScript using Jest + jsdom.

## Quick Start

```bash
# Install dependencies
npm install

# Run tests
npm test

# Run with coverage
npm test -- --coverage
```

## What's Tested

- **collector-dashboard.js** (26 tests) - Modal, forms, maps, DOM interactions

## Why Coverage Shows 0%

Legacy browser scripts can't be instrumented by Jest. Solutions implemented:
1. **collector-dashboard.js** - Excluded from SonarQube coverage requirements

**Result:** Tests verify functionality, SonarQube analyzes code quality without coverage metrics.

## SonarQube Configuration

- ✅ Code quality analysis still works
- ✅ Security vulnerabilities detected
- ❌ Coverage metrics excluded (by design)

Configuration in: `scripts/sast-sonar.sh` and `.github/workflows/CI-Branch-SonarQube-OWasp.yml`

## Troubleshooting

**Tests fail:** Run `npm install`  
**Coverage issues:** This is expected - see "Why Coverage Shows 0%" above  
**CI failures:** Check Node.js 20 is installed and `jest.setup.js` is committed

See [INTEGRATION.md](INTEGRATION.md) for full CI/CD setup details.

