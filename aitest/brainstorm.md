I want to create a client and server poc using grpc in Python on WSL. 

The server will have two servicers:
1. A ProbeServicer that the client can use to probe the current valid encryption and validation parameters (see below).
2. An EchoCommandServicer that takes a CommandRequest with the encryption algorithm, validation algorithm and a message and if the correct encryption and validation parameters are supplied will echo back the message given and if not return an error.

# Server 
* The server is implemented as a gRpc server in Python.
* The two servicers are added and then the server will wait until interrupted by a keyboard Ctrl+C then exit cleanly.
* The server will choose from a number of encryption algorithms and validation algorithms. These are re-chosen randomly and periodically based on a time interval parameter.
* The server should randomly (within a standard deviation around n seconds) change the chosen current valid parameters.  

# Server parameters 
    1. encryption algorithm (string) (eg. AES256, RSA etc) - fill this out with a wide range of common algorithms.
        DES, 3DES, or AES 
    2. validation algorithm (string) (SHA256 etc) - fill this out with a wide range of commmon hash algorithms.
        SHA1, HMACSHA256, HMACSHA384, HMACSHA512, MD5, 3DES, or AES

## ProbeServicer
* When probed via the Probe function that takes a ProbeRequest, the server will return a ProbeResponse with success = True or False.
* the request will have a candidate encryption and validation algorithm choice and return a success param.
* success=True is only returned if both parameters are guessed correctly.

## EchoCommandServicer
* Takes a validation algorithm, encryption algorithm and message. Checks that the encryption and validation alg match the current valid one and return the same message provided when true, otherwise return an rpc error.

# Client

* The client is a single execution command line tool that can run either a probe or an echo command with args as required.
* If a previous combination of good parameters are determined by probe and saved persistently in a json file these are loaded and used to issue the echo command.

## Echo command

* Calls the echo stub rpc with the current encryption and validation alg params.
* Displays the output of the command.
* Displays the error as a user friendly error if passed back.

## Probe command

* Calls the probe rpc for different parameter combinations until it succeeds or times out after a given timeout value (or default to 10 seconds).
* If succeeds saves the valid parameters into a json file read by other commands.

# Testing

* I want the ability to run the client commands manually using pytest via the Visual Code test explorer, the ability to run and debug the commands for step-debugging via the VS Code launch.json, and also a long-running test program that will stand up the server then try to run echo and probe when it fails and periodically restart the client to test the persistent json file.

* Please suggest any other ideas for making sure full coverage is achieved over testing the functionality.

# Technical decisions
- Use uv for python virtual environment etc.
- use python proto-importer for building the proto bindings for the client and server.

# Answers to key architectural questions:

1. How should the server's parameter rotation work concurrently?
- use asyncio based gRPC server for the server.

2. What should the proto schema look like?
- use two separate services for probe and echo service messages (two proto files).

3. How should the client probe strategy work?
- the client should brute force try the parameter combos with the most to least popular defined by a list in the client application.
- the client can send requests concurrently to determine the valid result. There should be a lock to stop invalid updates on the valid combination of parammeters once a good pair is determined.
- the client uses asyncio and gRPC (Python)

