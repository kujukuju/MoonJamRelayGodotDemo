import argparse
import websocket
import _thread
import time
import random
import struct

def on_message(ws, message):
    pass

def on_error(ws, error):
    print(error)

def on_close(ws, close_status_code, close_msg):
    print('# closed #')

def on_open(ws):
    def run(*args):
        # construct a basic player packet
        # struct.pack is for encoding the float value in the same way the game consumes it
        buf = bytearray(26)
        buf[0:4] = bytearray('pleb', 'utf8')
        buf[4:8] = (random.randint(0, 1<<32)).to_bytes(4, byteorder='big')
        buf[22:26] = struct.pack('f', -80.0)
        posX = 24.0 + random.randint(0, 48)
        posY = 192.0 - random.randint(0, 180)
        lastTick = 0
        while True:
            # constantly move the client upwards so we can tell if it's working
            posY -= 80 / 60
            if posY < 0:
                posY += 192
            clock = time.perf_counter() * 60
            sleep = int(clock) + 1 - clock
            if (int(clock) - lastTick) >= 4:
                buf[10:14] = struct.pack('f', posX)
                buf[14:18] = struct.pack('f', posY)
                # send this packet in binary, otherwise it won't work
                ws.send(buf, websocket.ABNF.OPCODE_BINARY)
                lastTick = int(clock)
            time.sleep(sleep / 60)

    _thread.start_new_thread(run, ())

def run_client():
    ws = websocket.WebSocketApp('wss://relay.moonjam.dev/v1',
                            on_open=on_open,
                            on_message=on_message,
                            on_error=on_error,
                            on_close=on_close)
    ws.run_forever()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Stress test the MOONJAM relay')
    parser.add_argument('num_clients', metavar='clients', type=int,
        help='number of clients to simulate')
    args = parser.parse_args()
    clients = args.clients
    if clients < 1:
        clients = 1
    for i in range(args.clients - 1):
        _thread.start_new_thread(run_client, ())
    run_client()
