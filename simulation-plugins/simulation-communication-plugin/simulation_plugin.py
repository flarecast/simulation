from connection_plugin import ConnectionPlugin

class SimulationPlugin(ConnectionPlugin):

    def __init__(self):
        super().__init__()

        s = socket.socket()
        port = int(os.environ.get('FLARECAST_PORT'))+1 #get port from env
        host = '127.0.0.1'
        s.bind((host, port))
        s.listen(1)

        self.unity_socket, self.unity_addr = s.accept()

    
    def broadcast(self, msg):
        if(time.time()<msg.event.timestamp+msg.event.lifetime)
            self.unity_socket.send(pickle.dumps(msg))

    @on_message
    def interpret(self, content):
        return picke.loads(content)


    def run(self):
        while True:
            content = self.unity_socket.recv(1024)
            interpret(content)
