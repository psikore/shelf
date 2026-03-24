
* open a terminal in this folder then:
```
uv venv
.venv\scripts\activate.ps1

uv pip install aiohttp

# start the echo server listening on port 5000
python .\echoserver.py
Echo server running on 127.0.0.1:5000

# split terminal
# start the client listening on port 3390
python .\client.py
[client] listening on 127.0.0.1:3390


# split terminal
# build the server (requires .NET 8.0)
dotnet build

# run the server
dotnet run
[server] listening on /tunfun

# use ncat as an echo client
.\ncat.exe 127.0.0.1 3390   

# type something to echo it
hello                         
hello
```

