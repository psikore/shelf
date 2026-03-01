import grpc
import accounts_pb2
import accounts_pb2_grpc


class AccountService(accounts_pb2_grpc.AccountServiceServicer):
    """
    Stateless servicers - never own state
    """
    def __init__(self, account_manager, config):
        self._account_manager = account_manager
        self._config = config

    async def GetAccount(self, request, context):
        acc = await self._account_manager.get(request.account_id)
        if acc is None:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details("Account not found")
            return accounts_pb2.GetAccountResponse()
        
        return accounts_pb2.GetAccountResponse(
            account_id=request.account_id,
            name=acc["name"],
            address=acc["address"],
        )
    
    async def AddAccount(self, request, context):
        add_enabled = await self._config.get_flag("enable_add_accounts")
        if not add_enabled:
            context.set_code(grpc.StatusCode.PERMISSION_DENIED)
            context.set_details("Adding accounts is disabled by configuration")
            return accounts_pb2.AddAccountResponse(ok=False)
        
        await self._account_manager.add(
            request.account_id,
            { "name": request.name, "address": request.address },
        )
        return accounts_pb2.AddAccountResponse(ok=True)
