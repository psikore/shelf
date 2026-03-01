import asyncio


class AccountManager:
    def __init__(self):
        self._accounts = {}
        self._lock = asyncio.Lock()

    async def get(self, account_id):
        async with self._lock:
            return self._accounts.get(account_id)
        
    async def add(self, account_id, details):
        async with self._lock:
            self._accounts[account_id] = details

    async def delete(self, account_id):
        async with self._lock:
            self._accounts.pop(account_id, None)

    async def update(self, account_id, details):
        async with self._lock:
            self._accounts[account_id] = details

    async def list_all(self):
        async with self._lock:
            return dict(self._accounts)
