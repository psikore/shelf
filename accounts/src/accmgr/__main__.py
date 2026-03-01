import asyncio
import grpc

import accounts_pb2_grpc

from accmgr import AccountManager
from config import AppConfig
from account_servicer import AccountService


async def serve():
    server = grpc.aio.server()

    # Shared objects
    account_manager = AccountManager()
    config = AppConfig(
        environment="dev",
        version="1.0.0",
        feature_flags={"enable_add_accounts": True},
    )

    accounts_pb2_grpc.add_AccountServiceServicer_to_server(
        AccountService(account_manager, config),
        server
    )

    server.add_insecure_port("0.0.0.0:50051")
    await server.start()

    await server.wait_for_termination()


if __name__ == "__main__":
    asyncio.run(serve())

