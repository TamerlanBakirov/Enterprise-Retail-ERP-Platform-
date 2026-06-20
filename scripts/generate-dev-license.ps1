param(
    [Parameter(Mandatory = $true)]
    [string] $CompanyName,
    [int] $Days = 365,
    [int] $MaxUsers = 5,
    [int] $MaxStores = 1
)

$signingKey = "DEVELOPMENT-ONLY-LICENSE-SIGNING-KEY-CHANGE-IN-PRODUCTION-64-CHARS"
$payload = [ordered]@{
    companyName = $CompanyName
    expiresAt = [DateTimeOffset]::UtcNow.AddDays($Days).ToString("o")
    maxUsers = $MaxUsers
    maxStores = $MaxStores
} | ConvertTo-Json -Compress

function ConvertTo-Base64Url([byte[]] $Bytes) {
    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$payloadBytes = [Text.Encoding]::UTF8.GetBytes($payload)
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($signingKey))
try {
    $signature = $hmac.ComputeHash($payloadBytes)
    "$(ConvertTo-Base64Url $payloadBytes).$(ConvertTo-Base64Url $signature)"
}
finally {
    $hmac.Dispose()
}
