#!/usr/bin/env python3
"""Unity WebGL 빌드를 로컬에서 열어 보기 위한 최소 정적 서버.

file:// 로 index.html을 직접 열면 브라우저가 .wasm/.data를 fetch로 못 읽어서
로더가 멈춘다. 이 스크립트가 이 폴더를 localhost로 잠깐 서빙해서 그 문제를 피한다.
"""
import http.server
import mimetypes
import os
import socket
import webbrowser

START_PORT = 8000
DIRECTORY = os.path.dirname(os.path.abspath(__file__))

# 일부 파이썬 버전은 .wasm의 MIME 타입을 모른다 — 잘못된 타입으로 서빙되면
# 브라우저가 WebAssembly로 인식하지 못해 로딩이 실패한다.
mimetypes.add_type("application/wasm", ".wasm")
mimetypes.add_type("application/octet-stream", ".data")
mimetypes.add_type("application/javascript", ".js")


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def end_headers(self):
        # 로컬 시연용이라 캐시를 꺼서, 빌드를 다시 내보내도 브라우저가 옛 파일을
        # 계속 들고 있는 일이 없게 한다.
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def find_free_port(start_port):
    port = start_port
    while port < start_port + 20:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            if s.connect_ex(("127.0.0.1", port)) != 0:
                return port
        port += 1
    return start_port


def main():
    port = find_free_port(START_PORT)
    url = f"http://localhost:{port}/"
    httpd = http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler)

    print(f"Serving folder: {DIRECTORY}")
    print(f"Opening browser at: {url}")
    print("Press Ctrl+C in this window to stop.")

    webbrowser.open(url)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nStopping server.")
        httpd.shutdown()


if __name__ == "__main__":
    main()
