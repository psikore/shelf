import datetime

class Logger:
    @staticmethod
    def log(message: str):
        print(f"[{datetime.datetime.now()}] {message}")