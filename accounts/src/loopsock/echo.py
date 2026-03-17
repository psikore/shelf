import socket
import threading

def handle(conn, addr):
    print("Client connected:", addr)
    while True:
        data = conn.recv(65536)
        if not data:
            break
        conn.sendall(data)
    conn.close()
    print("Client disconnected:", addr)

s = socket.socket()
s.bind(("127.0.0.1", 9000))
s.listen()

print("Echo server running on 127.0.0.1:9000")

while True:
    conn, addr = s.accept()
    threading.Thread(target=handle, args=(conn, addr), daemon=True).start()
