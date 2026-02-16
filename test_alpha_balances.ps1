# test_alpha_balances.ps1
# Script to test the Alpha API balance endpoints for BSC, Sol, and Sui.

$baseUrl = "http://localhost:5073"
$apiKey = "00000000-0000-0000-0000-000000000000"

# Test Addresses
$tests = @(
    @{
        Chain = "Bsc"
        Address = "0xF977814e90dA44bFA03b6295A0616a897441aceC" # Binance 8 (Active)
        Tokens = @("BNB", "USDT", "USDC")
    },
    @{
        Chain = "Sol"
        Address = "vines1vzrY7tduYG7Zbdf9nN7s788bAtp6DdbHcmf5W" # Solana Genesis (Always active)
        Tokens = @("SOL", "USDT", "USDC")
    },
    @{
        Chain = "Sui"
        Address = "0x0d3065b899123bf6492954694ec507548bdef08d4c463bc1509b423142a174eb" # User wallet
        Tokens = @("SUI", "USDT", "USDC")
    }
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   Alpha API Balance Test Utility" -ForegroundColor Cyan
Write-Host "   Target: $baseUrl" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

foreach ($test in $tests) {
    $chain = $test.Chain
    $addr = $test.Address
    Write-Host "`n[Chain: $chain]" -ForegroundColor Yellow
    Write-Host "Address: $addr" -ForegroundColor Gray
    
    foreach ($token in $test.Tokens) {
        $url = "$baseUrl/api/wallets/balance?chain=$chain&address=$addr&symbol=$token"
        
        try {
            $resp = Invoke-RestMethod -Uri $url -Method Get -Headers @{ "X-Api-Key" = $apiKey } -ErrorAction Stop
            
            $statusColor = "Green"
            if ($resp.balance -eq "0") { $statusColor = "DarkGray" }
            
            Write-Host "  - $($token.PadRight(5)): $($resp.balance.PadLeft(20))" -ForegroundColor $statusColor
        }
        catch {
            if ($_.Exception.Response -ne $null) {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $errBody = $reader.ReadToEnd()
                Write-Host "  - $($token.PadRight(5)): ERROR ($errBody)" -ForegroundColor Red
            } else {
                Write-Host "  - $($token.PadRight(5)): ERROR ($($_.Exception.Message))" -ForegroundColor Red
            }
        }
    }
}

Write-Host "`n===============================================" -ForegroundColor Cyan
Write-Host "   Test Completed" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
