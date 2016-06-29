from connection_plugin import ConnectionPlugin
from message_handler import on_message
import socket
import os
import pickle
import time

class SimulationPlugin(ConnectionPlugin):

    def __init__(self):
        super().__init__()

    
    def broadcast(self, msg):
        #if(time.time()<msg.event.timestamp+msg.event.lifetime):
        self.unity_socket.send(pickle.dumps(msg))

    @on_message
    def interpret(self, content):
        return pickle.loads(content)

    def address(self):
        return int(os.environ.get('FLARECAST_PORT'))-8000

    def run(self):        
        self.unity_socket = socket.socket()
        port = int(os.environ.get('FLARECAST_PORT'))+1 #get port from env
        host = '127.0.0.1'
        self.unity_socket.connect((host, port))

        while True:
            content = self.unity_socket.recv(1024)
            self.interpret(content)
