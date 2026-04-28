# 修改 Hero 精灵 Pivot 为 Bottom Center（解决脚穿地问题）
$uri = "http://localhost:8080/mcp"
$headers = @{"Accept"="application/json, text/event-stream"}

# 1. 初始化会话
$body = @{jsonrpc="2.0";id=1;method="initialize";params=@{protocolVersion="2024-11-05";capabilities=@{};clientInfo=@{name="pivot-fix";version="1.0"}}} | ConvertTo-Json -Depth 5
$resp = Invoke-RestMethod -Uri $uri -Method POST -ContentType "application/json" -Body $body -Headers $headers
$sid = $resp.result.sessionId

# 通用请求头（带 session）
$sessHeaders = @{"Accept"="application/json, text/event-stream";"Mcp-Session-Id"=$sid}

# 2. 发送 initialized 通知
$body = @{jsonrpc="2.0";method="notifications/initialized"} | ConvertTo-Json
Invoke-RestMethod -Uri $uri -Method POST -ContentType "application/json" -Body $body -Headers $sessHeaders | Out-Null

# 3. 修改 stand.png 的 SpriteImporter pivot 为 Bottom (0.5, 0)
$code = @'
using UnityEngine;
using UnityEditor;

var path = "Assets/Art/Sprites/Characters/Hero/stand.png";
var importer = AssetImporter.GetAtPath(path) as TextureImporter;
if (importer != null) {
    importer.spritePivot = new Vector2(0.5f, 0f); // Bottom Center
    importer.SaveAndReimport();
    Debug.Log("stand.png pivot set to Bottom Center: " + importer.spritePivot);
} else {
    Debug.LogError("stand.png importer not found at: " + path);
}
'@

$body = @{jsonrpc="2.0";id=11;method="tools/call";params=@{name="execute_code";arguments=@{code=$code}}} | ConvertTo-Json -Depth 5
$resp = Invoke-RestMethod -Uri $uri -Method POST -ContentType "application/json" -Body $body -Headers $sessHeaders
Write-Host "=== stand.png result ==="
Write-Host ($resp | ConvertTo-Json -Depth 5)

# 4. 修改 run.png 的 SpriteImporter pivot 为 Bottom Center
$code2 = @'
using UnityEngine;
using UnityEditor;

var path = "Assets/Art/Sprites/Characters/Hero/run.png";
var importer = AssetImporter.GetAtPath(path) as TextureImporter;
if (importer != null) {
    importer.spritePivot = new Vector2(0.5f, 0f); // Bottom Center
    importer.SaveAndReimport();
    Debug.Log("run.png pivot set to Bottom Center: " + importer.spritePivot);
} else {
    Debug.LogError("run.png importer not found at: " + path);
}
'@

$body = @{jsonrpc="2.0";id=12;method="tools/call";params=@{name="execute_code";arguments=@{code=$code2}}} | ConvertTo-Json -Depth 5
$resp = Invoke-RestMethod -Uri $uri -Method POST -ContentType "application/json" -Body $body -Headers $sessHeaders
Write-Host "=== run.png result ==="
Write-Host ($resp | ConvertTo-Json -Depth 5)

Write-Host "--- DONE ---"
