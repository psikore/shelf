from sample_code.core.logger import Logger
from sample_code.models.user import AdminUser


def main():
    admin_user = AdminUser(
        username="admin",
        permissions=["ALL"]
    )
    if admin_user.is_superuser:
        Logger.log(f"{admin_user} is a super user!")

    Logger.log(admin_user.greet())


if __name__ == "__main__":
    main()
