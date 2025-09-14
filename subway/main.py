import asyncio
import time
import psutil

HEARTBEAT_TIMEOUT = 8
CHECK_INTERVAL = 5


class AsyncProcessMonitor:
    def __init__(self, cmd):
        self.cmd = cmd
        self.proc = None
        self.last_heartbeat = time.time()

    async def start(self):
        try:
            self.proc = await asyncio.create_subprocess_exec(
                *self.cmd,
                stdout=asyncio.subprocess.PIPE,
                stderr=asyncio.subprocess.PIPE
            )
            print(f"Started subprocess [{self.proc.pid}]")
            asyncio.create_task(self._read_stdout())
            asyncio.create_task(self._read_stderr())

        except Exception as err:
            print(f"[ERR] Failed to start subprocess: {err}")

    async def _read_stderr(self):
        try:
            while True:
                line = await self.proc.stderr.readline()
                if not line:
                    break
                print(f"[WORKER STDERR]: {line.decode().strip()}")
        except Exception as err:
            print(f"[ERR] Reading stderr failed: {err}")

    async def _read_stdout(self):
        try:
            while True:
                line = await self.proc.stdout.readline()
                if not line:
                    break   # EOF

                print(f"[{self.proc.pid}: {line}")

                if b"heartbeat" in line:
                    self.last_heartbeat = time.time()

        except Exception as err:
            print(f"[ERR] Reading stdout failed: {err}")

    def is_process_running(self):
        try:
            proc = psutil.Process(self.proc.pid)
            return proc.is_running()
        except psutil.ZombieProcess:
            print("[-] zombie process")
            return False
        except psutil.NoSuchProcess:
            print("[-] no such process")
            return False
        except Exception as err:
            print(f"[ERROR] psutil check failed: {err}")
        return False

    def is_alive(self):
        heart_beating = True # (time.time() - self.last_heartbeat) < HEARTBEAT_TIMEOUT
        process_running = self.is_process_running()
        return heart_beating and process_running

    def log_exit_reason(self):
        if self.proc and self.proc.returncode is not None:
            code = self.proc.returncode
            if code == 0:
                print(f"[+] Subprocess [{self.proc.pid}] exited normally.")
            elif code < 0:
                print(f"[ERROR] Subprocess [{self.proc.pid}] was terminated by signal {-code}.")
            else:
                print(f"[ERROR] Subprocess [{self.proc.pid}] exited with code {code}.")

    async def restart(self):
        print("[+] Restarting subprocess...")
        try:
            if self.proc:
                if self.proc.returncode is None:
                    self.proc.kill()
                    await self.proc.wait()
                else:
                    print(f"[+] Subprocess [{self.proc.pid}] already terminated externally.")
                    self.log_exit_reason()

        except ProcessLookupError:
            print("[!] Subprocess already dead - skipping kill.")
        except Exception as err:
            print(f"[ERROR] Failed to kill subprocess: {err}")

        await self.start()


async def monitor_loop(monitors):
    while True:
        for monitor in monitors:
            if monitor.proc and monitor.proc.returncode is not None:
                print(f"[!] Subprocess [{monitor.proc.pid}] exited unexpectedly!")
                monitor.log_exit_reason()
                await monitor.restart()
            elif not monitor.is_alive():
                print("[!] Process is NOT alive - restarting!")
                await monitor.restart()
        await asyncio.sleep(CHECK_INTERVAL)


async def main():
    monitors = [AsyncProcessMonitor(["python", "worker.py"]) for _ in range(2)]
    await asyncio.gather(*(m.start() for m in monitors))
    await monitor_loop(monitors)

if __name__ == "__main__":
    asyncio.run(main())