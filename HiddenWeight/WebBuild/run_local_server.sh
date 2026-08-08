#!/usr/bin/env bash
cd "$(dirname "$0")"

if command -v python3 >/dev/null 2>&1; then
    python3 serve.py
elif command -v python >/dev/null 2>&1; then
    python serve.py
else
    echo "Python을 찾지 못했습니다. https://www.python.org/downloads/ 에서 설치해 주세요."
    exit 1
fi
