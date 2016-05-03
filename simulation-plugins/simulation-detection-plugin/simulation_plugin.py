import threading
import socket
from detector import Detector

class SimulationDetector(Detector):

    @on_event
    def interpret(self, text):
        return Event(1,1,"simulation", False, time.time(), int(text), None)



    def run(self):
        s = socket.socket()
        port = 11111 #get port from file
        host = '127.0.0.1'
        s.bind((host, port))
        s.listen(1)

        unity_socket, addr = s.accept()

        while True:
            text = pickle.loads(unity_socket.recv(1024))
            interpret(text)
