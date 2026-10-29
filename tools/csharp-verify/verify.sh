#!/usr/bin/env bash
# Unity C# 编译检查。
#
# 用手写的 Unity API 桩 + Roslyn 编译 Assets/Scripts 下的全部 C#。
# 用途是抓「代码自己写错了」这类问题（拼写、签名、类型），
# 不能替代在真 Unity Editor 里跑一遍。
#
# 依赖：.NET SDK 8+（本机安装在 ~/.dotnet）
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"

if [ ! -x "$DOTNET" ]; then
  echo "找不到 dotnet：$DOTNET"
  echo "装一个：curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir \"\$HOME/.dotnet\""
  exit 1
fi

echo "==> 编译 Unity 脚本（使用 tools/csharp-verify/UnityStubs.cs 作为 Unity API 桩）"
cd "$ROOT/tools/csharp-verify"

set +e
OUTPUT="$("$DOTNET" build Verify.csproj -v quiet --nologo 2>&1)"
STATUS=$?
set -e

echo "$OUTPUT"

if [ $STATUS -eq 0 ]; then
  FILE_COUNT=$(find "$ROOT/unity/InspectionDemo/Assets/Scripts" "$ROOT/unity/InspectionDemo/Assets/Editor" -name '*.cs' 2>/dev/null | wc -l | tr -d ' ')
  echo ""
  echo "✅ 编译通过：$FILE_COUNT 个 C# 文件，0 error 0 warning"
  echo "   注意：桩不是真 Unity，Unity API 的真实签名仍需在 Editor 里确认。"
  exit 0
fi

echo ""
echo "❌ 编译失败"
exit 1
