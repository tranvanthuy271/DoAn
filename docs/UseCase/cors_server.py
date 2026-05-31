import http.server
import socketserver

class CORSHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")
        super().end_headers()
    def log_message(self, format, *args):
        pass

with socketserver.TCPServer(("127.0.0.1", 8765), CORSHandler) as httpd:
    httpd.serve_forever()
