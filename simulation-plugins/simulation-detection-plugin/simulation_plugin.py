import threading
import socket
from detector import Detector

class SimulationDetector(Detector):

    @on_event
    def interpret(self, content):
        return Event(1,1,"simulation", False, time.time(), int(content), None)



    def run(self):
        s = socket.socket()
        port = int(os.environ.get('FLARECAST_PORT')) #get port from env
        host = '127.0.0.1'
        s.bind((host, port))
        s.listen(1)

        unity_socket, addr = s.accept()

        while True:
            content = pickle.loads(unity_socket.recv(1024))
            interpret(content)
