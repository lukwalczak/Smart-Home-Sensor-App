# Quick start script for Data Center Sensor App with Blockchain

Write-Host "[*] Starting Data Center Sensor App with Blockchain..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Start containers
Write-Host "[1/5] Starting Docker containers..." -ForegroundColor Yellow
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to start containers!" -ForegroundColor Red
    exit 1
}

# Step 2: Wait for services
Write-Host "[2/5] Waiting for services to initialize (30 seconds)..." -ForegroundColor Yellow
for ($i = 30; $i -gt 0; $i--) {
    Write-Host "   $i seconds remaining..." -NoNewline
    Start-Sleep -Seconds 1
    Write-Host "`r" -NoNewline
}
Write-Host "   Done!                    "

# Step 3: Deploy smart contract
Write-Host "[3/5] Deploying smart contract to Anvil..." -ForegroundColor Yellow
$deployResult = docker exec blockchain sh /blockchain/scripts/setup.sh 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  First deployment attempt failed. Waiting 10 more seconds..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    Write-Host "🔄 Retrying deployment..." -ForegroundColor Yellow
    $deployResult = docker exec blockchain sh /blockchain/scripts/setup.sh 2>&1
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Smart contract deployed successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Deployment failed!" -ForegroundColor Red
    Write-Host $deployResult
    Write-Host ""
    Write-Host "[INFO] Try running manually:" -ForegroundColor Yellow
    Write-Host "   docker exec blockchain sh /blockchain/scripts/setup.sh" -ForegroundColor Cyan
    exit 1
}

# Step 4: Show deployment info
Write-Host "[4/5] Contract deployment info:" -ForegroundColor Yellow
$deploymentInfo = docker exec blockchain cat /blockchain/deployment.json | ConvertFrom-Json
Write-Host "   Contract Address: $($deploymentInfo.contractAddress)" -ForegroundColor Cyan
Write-Host "   Admin Address: $($deploymentInfo.adminAddress)" -ForegroundColor Cyan
Write-Host "   Deployed At: $($deploymentInfo.deployedAt)" -ForegroundColor Cyan

# Step 5: Restart backend
Write-Host "[5/5] Restarting backend to load contract..." -ForegroundColor Yellow
docker restart backend | Out-Null
Start-Sleep -Seconds 5

# Final check
Write-Host ""
Write-Host "[OK] Setup complete! Checking services..." -ForegroundColor Green
Write-Host ""

$services = docker-compose ps --format json | ConvertFrom-Json
foreach ($service in $services) {
    $status = if ($service.State -eq "running") { "[OK]" } else { "[!!]" }
    Write-Host "   $status $($service.Service): $($service.State)" -ForegroundColor $(if ($service.State -eq "running") { "Green" } else { "Red" })
}

Write-Host ""
Write-Host "Application URLs:" -ForegroundColor Cyan
Write-Host "   Main Dashboard:       http://localhost:3000" -ForegroundColor White
Write-Host "   Blockchain Tokens:    http://localhost:3000/blockchain" -ForegroundColor White
Write-Host "   Data Table:           http://localhost:3000/data" -ForegroundColor White
Write-Host "   Charts:               http://localhost:3000/charts" -ForegroundColor White
Write-Host "   Backend API:          http://localhost:5000/api" -ForegroundColor White
Write-Host "   Anvil RPC:            http://localhost:8545" -ForegroundColor White
Write-Host ""

# Test API
Write-Host "Testing API..." -ForegroundColor Yellow
try {
    $contractInfo = Invoke-RestMethod -Uri "http://localhost:5000/api/blockchain/contract" -ErrorAction Stop
    Write-Host "   [OK] Backend connected to blockchain!" -ForegroundColor Green
    Write-Host "   Total Supply: $([math]::Round($contractInfo.totalSupply, 2)) SRT" -ForegroundColor Cyan
    Write-Host "   Reward per Message: $([math]::Round($contractInfo.rewardPerMessage, 2)) SRT" -ForegroundColor Cyan
} catch {
    Write-Host "   [WARN] Backend not ready yet. Wait 10 seconds and try:" -ForegroundColor Yellow
    Write-Host "      Invoke-RestMethod http://localhost:5000/api/blockchain/contract" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "[SUCCESS] All done! Opening blockchain dashboard..." -ForegroundColor Green
Start-Sleep -Seconds 2
Start-Process "http://localhost:3000/blockchain"

Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "   View logs:           docker-compose logs -f" -ForegroundColor Cyan
Write-Host "   Stop all:            docker-compose down" -ForegroundColor Cyan
Write-Host "   Restart backend:     docker restart backend" -ForegroundColor Cyan
Write-Host "   Redeploy contract:   docker exec blockchain sh /blockchain/scripts/setup.sh" -ForegroundColor Cyan
Write-Host ""
