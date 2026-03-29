import asyncio
import random
import logging

logger = logging.getLogger(__name__)

ENCRYPTION_ALGORITHMS = ["DES", "3DES", "AES"]

VALIDATION_ALGORITHMS = [
    "SHA1", "HMACSHA256", "HMACSHA384", "HMACSHA512", "MD5", "3DES", "AES",
]

DEFAULT_ROTATION_INTERVAL = 30.0
DEFAULT_ROTATION_STDDEV = 5.0


class ParamState:
    def __init__(
        self,
        rotation_interval: float = DEFAULT_ROTATION_INTERVAL,
        rotation_stddev: float = DEFAULT_ROTATION_STDDEV,
    ):
        self._lock = asyncio.Lock()
        self._rotation_interval = rotation_interval
        self._rotation_stddev = rotation_stddev
        self._encryption: str = random.choice(ENCRYPTION_ALGORITHMS)
        self._validation: str = random.choice(VALIDATION_ALGORITHMS)
        self._rotation_task: asyncio.Task | None = None
        logger.info(
            "Initial params: encryption=%s, validation=%s",
            self._encryption, self._validation,
        )

    async def get(self) -> tuple[str, str]:
        async with self._lock:
            return self._encryption, self._validation

    async def check(self, encryption: str, validation: str) -> bool:
        async with self._lock:
            return (
                self._encryption == encryption
                and self._validation == validation
            )

    async def _rotate(self):
        self._encryption = random.choice(ENCRYPTION_ALGORITHMS)
        self._validation = random.choice(VALIDATION_ALGORITHMS)
        logger.info(
            "Rotated params: encryption=%s, validation=%s",
            self._encryption, self._validation,
        )

    async def _rotation_loop(self):
        while True:
            delay = max(1.0, random.gauss(self._rotation_interval, self._rotation_stddev))
            await asyncio.sleep(delay)
            async with self._lock:
                await self._rotate()

    def start_rotation(self):
        if self._rotation_task is None:
            self._rotation_task = asyncio.create_task(self._rotation_loop())
            logger.info(
                "Rotation started (interval=%.1fs, stddev=%.1fs)",
                self._rotation_interval, self._rotation_stddev,
            )

    def stop_rotation(self):
        if self._rotation_task is not None:
            self._rotation_task.cancel()
            self._rotation_task = None
            logger.info("Rotation stopped")
