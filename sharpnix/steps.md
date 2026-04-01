- create a solution file
- create a Protos dir
- create the project files

```bash
dotnet new grpc -n EchoServer -o EchoServer --no-https
```

```bash
dotnet new console -n EchoClient -o EchoClient
```

- add projects to the solution
- write echo proto file

- use hivemind to start/stop processes and interleave output.
- this is a go exe

```bash
curl -sL https://github.com/DarthSim/hivemind/releases/latest/download/hivemind-v1.1.0-linux-amd64.gz | gunzip > /tmp/hivemind && chmod +x /tmp/hivemind && sudo mv /tmp/hivemind /usr/local/bin/hivemind && hivemind --version
```