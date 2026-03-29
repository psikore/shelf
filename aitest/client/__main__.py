import argparse
import asyncio
from client.probe import run_probe
from client.echo import run_echo

DEFAULT_TARGET = "localhost:50051"


def main():
    parser = argparse.ArgumentParser(description="probepoc client")
    parser.add_argument("--target", default=DEFAULT_TARGET, help="gRPC server address")
    subparsers = parser.add_subparsers(dest="command", required=True)

    probe_parser = subparsers.add_parser("probe", help="Probe for valid parameters")
    probe_parser.add_argument("--timeout", type=float, default=10.0, help="Probe timeout in seconds")

    echo_parser = subparsers.add_parser("echo", help="Send an echo command")
    echo_parser.add_argument("message", help="Message to echo")
    echo_parser.add_argument("--encryption", default=None, help="Encryption algorithm override")
    echo_parser.add_argument("--validation", default=None, help="Validation algorithm override")

    args = parser.parse_args()

    if args.command == "probe":
        asyncio.run(run_probe(args.target, timeout=args.timeout))
    elif args.command == "echo":
        asyncio.run(run_echo(args.target, args.message, encryption=args.encryption, validation=args.validation))


if __name__ == "__main__":
    main()
