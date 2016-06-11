import os
import socket
import pickle

print('PORT: {}'.format(os.environ.get('FLARECAST_PORT')))

s = socket.socket()
port = int(os.environ.get('FLARECAST_PORT'))+1 #get port from env
host = '127.0.0.1'
s.bind((host, port))
s.listen(1)

unity_socket, unity_addr = s.accept()

unity_socket.send(pickle.dumps("TEST"))