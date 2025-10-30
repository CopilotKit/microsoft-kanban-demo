# Test the AG-UI endpoint
$body = @{
    message = "Hello, how are you?"
} | ConvertTo-Json

Write-Host "Sending request to http://localhost:5018/" -ForegroundColor Cyan
Write-Host "Request body: $body" -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5018/" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -TimeoutSec 30
    
    Write-Host "Response received!" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content Type: $($response.Headers['Content-Type'])" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Content:" -ForegroundColor Cyan
    Write-Host $response.Content
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
