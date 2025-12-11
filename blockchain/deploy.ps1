Write-Host "🚀 Deploying Smart Contract to Anvil..." -ForegroundColor Cyan

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Node.js is not installed!" -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
    npm install
}

Write-Host "📄 Deploying contract..." -ForegroundColor Yellow
node scripts/deploy.js

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Deployment successful!" -ForegroundColor Green
    Write-Host "📋 Check deployment.json for contract address" -ForegroundColor Cyan

    Write-Host "`n🔄 Restarting backend..." -ForegroundColor Yellow
    docker restart backend

    Write-Host "`n✅ Done! Backend restarted." -ForegroundColor Green
    Write-Host "🌐 Open http://localhost:3000/blockchain to view tokens" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ Deployment failed!" -ForegroundColor Red
    Write-Host "Make sure Anvil is running: docker-compose ps anvil" -ForegroundColor Yellow
}
