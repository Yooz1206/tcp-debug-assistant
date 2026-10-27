param(
    [int]$Port = 9101,
    [string]$Log = "$env:TEMP\echo-server.log"
)

# Tiny echo server used by the UI smoke test. ASCII only so cmd/PowerShell 5.1
# never has to guess an encoding.
$ErrorActionPreference = 'Stop'
"start $(Get-Date -Format o)" | Out-File $Log
$listener = New-Object System.Net.Sockets.TcpListener([Net.IPAddress]::Loopback, $Port)
$listener.Start()
"listening on $Port" | Out-File $Log -Append

while ($true) {
    $client = $listener.AcceptTcpClient()
    "accepted $($client.Client.RemoteEndPoint)" | Out-File $Log -Append
    $stream = $client.GetStream()
    $buffer = New-Object byte[] 8192
    while ($true) {
        try {
            $read = $stream.Read($buffer, 0, $buffer.Length)
        }
        catch {
            "read error: $($_.Exception.Message)" | Out-File $Log -Append
            break
        }

        if ($read -le 0) {
            "client closed" | Out-File $Log -Append
            break
        }

        $hex = ($buffer[0..($read - 1)] | ForEach-Object { $_.ToString('X2') }) -join ' '
        "recv $read bytes: $hex" | Out-File $Log -Append
        $stream.Write($buffer, 0, $read)
        $stream.Flush()
    }

    $client.Close()
}
