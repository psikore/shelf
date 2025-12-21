import asyncio
import pika
import json
import os

RABBITMQ_HOST = "localhost"
QUEUE_NAME = "file_chunks"
OUTPUT_PATH = os.path.join("files", "received_largefile.dat")


def callback(ch, method, properties, body):
    msg = json.loads(body)
    if "eof" in msg:
        print("Download complete")
        ch.stop_consuming()
        return
    
    offset = msg["offset"]
    data = bytes.fromhex(msg["data"])

    # Append chunks directly to file
    with open(OUTPUT_PATH, "ab") as f:
        f.write(data)

    print(f"Received chunk {offset}-{offset+len(data)-1}")


defa consume_chunks():
    connection = pika.BlockingConnection(pika.ConnectionParameters(RABBITMQ_HOST))
    channel = connection.channel()
    channel.queue_declare(queue=QUEUE_NAME)

    channel.basic_consume(queue=QUEUE_NAME, on_message_callback=callback)
    print("Waiting for chunks...")
    channel.start_consuming()


if __name__ == "__main__":
    consume_chunks()
