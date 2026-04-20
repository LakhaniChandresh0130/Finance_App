import time
from collections.abc import Callable

class ResponseTimeMiddleware:
    def __init__(self, app: Callable):
        self.app = app

    async def __call__(self, scope, receive, send):
        if scope["type"] != "http":
            await self.app(scope, receive, send)
            return

        start = time.perf_counter()

        async def send_wrapper(message):
            if message["type"] == "http.response.start":
                elapsed_ms = round((time.perf_counter() - start) * 1000, 2)
                headers = message.setdefault("headers", [])
                headers.append((b"x-response-time-ms", str(elapsed_ms).encode("utf-8")))
            await send(message)

        await self.app(scope, receive, send_wrapper)
