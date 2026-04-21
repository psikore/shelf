import json
import os
import base64

from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.backends import default_backend

import aiohttp
import asyncio


AES_KEY = b"0123456789ABCDEF0123456789ABCDEF"  # 32 bytes for AES-256

with open("mapping.json", "r") as f:
    MAP = json.load(f)

REVERSE_MAP = { v: k for k, v in MAP.items()}


def obfuscate(obj: dict) -> dict:
    """Convert names -> obfuscated names"""
    return {MAP[k]: v for k, v in obj.items()}

def deobfuscate(obj: dict) -> dict:
    """Convert obfuscated names -> names."""
    return {REVERSE_MAP[k]: v for k, v in obj.items()}


def encrypt_with_random_iv(plaintext: bytes) -> str:
    iv = os.urandom(16)

    padder = padding.PKCS7(block_size=128).padder()
    padded = padder.update(plaintext) + padder.finalize()

    cipher = Cipher(algorithm=algorithms.AES(key=AES_KEY), mode=modes.CBC(initialization_vector=iv), backend=default_backend)
    encryptor = cipher.encryptor()
    ciphertext = encryptor.update(padded) + encryptor.finalize()

    # prepend iv
    combined = iv + ciphertext

    return base64.b64encode(ciphertext).decode()

def build_payload(obj, encrypted=True) -> str:
    obf_obj = obfuscate(obj)

    json_bytes = json.dumps(obf_obj).encode()

    if encrypted:
        flag = b"\x01"
        body = encrypt_with_random_iv(json_bytes)
    else:
        flag = b"\x00"
        body = json_bytes

    framed = flag + body
    return base64.b64encode(framed).decode()


async def send_encrypted_payload():
    payload = {
        "UserId": 123,
        "Message": "Hello from Python",
        "Flag": True,
    }

    json_bytes = json.dumps(payload).encode()
    encrypted = aes_encrypt(json_bytes)

    async with aiohttp.ClientSession() as session:
        async with session.post(
            "http://localhost:5000/api/secure",
            json={"ciphertext": encrypted}
        ) as resp:
            data = await resp.json()
            clean = deobfuscate(data)
            print("Deobfuscated response:", clean)

asyncio.run(send_encrypted_payload())

"""
This client sends 
{
    "ciphertext" "BASE64_ENCRYPTED_BYTES"
}
"""
