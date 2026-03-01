import asyncio


class AppConfig:
    """
    Shared immutable (or rarely-mutated) configuration
    """
    def __init__(self, environment: str, version: str, feature_flags: dict):
        self._environment = environment
        self._version = version
        self._feature_flags = feature_flags
        self._lock = asyncio.Lock()

    async def get_flag(self, key):
        async with self._lock:
            return self._feature_flags.get(key)
        
    async def set_flag(self, key, value):
        async with self._lock:
            self._feature_flags[key] = value
