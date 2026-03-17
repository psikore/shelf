# Working with socket objects directly

In general, protocol implementations that use transport-based APIs such as `loop.create_connection()` and `loop.create_server()` are faster than implementations that work with sockets directly. However, there are some use cases when performance is not critical, and working with socket objects directly is more convenient.

## Simple test

 python .\forwarder.py

 python .\echo.py

 .\ncat.exe 127.0.0.1 8080

 then type hello in the ncat terminal

 you should see it echo the message