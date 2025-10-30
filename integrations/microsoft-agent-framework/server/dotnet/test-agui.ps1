# Test script for AG-UI .NET implementation
# Sends a RunAgentInput request to the server

$body = @{
    threadId = "test-thread-1"
    runId = "test-run-1"
    state = $null
    messages = @(
        @{
            id = "msg-1"
            role = "user"
            content = "Hello! Tell me a short joke about programming."
        }
    )
    tools = @()
    context = @()
    forwardedProps = $null
} | ConvertTo-Json -Depth 10

Write-Host "Sending request to http://localhost:5018/" -ForegroundColor Cyan
Write-Host "Request body:" -ForegroundColor Yellow
Write-Host $body -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5018/" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -TimeoutSec 30

    Write-Host "Response status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response content:" -ForegroundColor Yellow
    Write-Host $response.Content -ForegroundColor Gray
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error response: $errorBody" -ForegroundColor Red
    }
}
