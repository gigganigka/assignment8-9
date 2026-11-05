"""Run on Windows: py Tools/serve_web.py . Open printed LAN URL in iPhone Safari.
Use only on your trusted local network. Ctrl+C stops the server.
"""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from functools import partial
import socket
root=Path(__file__).resolve().parents[1]/'Builds'/'Web'
if not (root/'index.html').exists():
    raise SystemExit('No Web build yet. In Unity run Tools > Mobile Game > 5. Build Web (Safari).')
s=socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
try:
    s.connect(('192.0.2.1',80));ip=s.getsockname()[0]
except OSError: ip='<your-PC-LAN-IP>'
finally:s.close()
SimpleHTTPRequestHandler.extensions_map['.wasm']='application/wasm'
print('Desktop: http://localhost:8000')
print('iPhone (same Wi-Fi): http://'+ip+':8000')
print('Allow Python on PRIVATE networks if Windows Firewall asks. Ctrl+C to stop.')
ThreadingHTTPServer(('0.0.0.0',8000),partial(SimpleHTTPRequestHandler,directory=str(root))).serve_forever()
