from sample_code.core.logger import Logger

class BaseUser:
    ROLE = "guest"

    def __init__(self, username: str):
        self._username = username

    def greet(self):
        return f"Welcome, {self.username}!"

    @property
    def username(self) -> str:
        return self._username

class AdminUser(BaseUser):
    ROLE = "admin"

    def __init__(self, username: str, permissions: list[str]):
        super().__init__(username)
        self.permissions = permissions

    @property
    def is_superuser(self) -> bool:
        return "ALL" in self.permissions

    def add_permission(self, perm: str):
        if perm not in self.permissions:
            self.permissions.append(perm)
            Logger.log(f"Permission '{perm}' added to {self.username}")

    def __str__(self):
        perms = ", ".join(self.permissions)
        return f"{self.username} ({self.ROLE}): {perms}"