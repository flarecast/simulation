from geopy.geocoders import Nominatim
from geopy.distance import vincenty
import math
import threading
import bluetooth
import os

class GPS:
    CONN_PORT = 21
    BACKLOG = 1
    RECEIVE_SIZE = 1024
    ADDRESS_FILE = "config/bluetooth_address"

    def __init__(self):
        self.bluetooth_mac_address = GPS.get_bluetooth_address(GPS.ADDRESS_FILE)
        self.server_socket = self.set_bluetooth_socket(self.bluetooth_mac_address)
        self.client_socket = self.create_client_connection(self.server_socket)

    def set_bluetooth_socket(self, hostMACAddress):
        s = bluetooth.BluetoothSocket(bluetooth.RFCOMM)
        s.bind((hostMACAddress, GPS.CONN_PORT))
        s.listen(GPS.BACKLOG)
        return s

    def create_client_connection(self, server_socket):
        try:
            client, _x = server_socket.accept()
            return client
        except:
            server_socket.close()

    def get_current_coordinates(self):
        return self.request_coordinates()

    def request_coordinates(self):
        try:
            self.client_socket.send("gps-data\n")
            data = self.client_socket.recv(GPS.RECEIVE_SIZE).decode("utf-8")
            if data:
              return GPS.to_tuple(data)
        except Exception as e:
            print(e)
            client_socket.close()

    @classmethod
    def get_bluetooth_address(cls, filename):
        f = open(os.path.realpath(filename), 'r')
        return f.read()

    @classmethod
    def to_tuple(cls, str):
        points = str.split(" ")
        return (float(points[0]), float(points[1]))

    @classmethod
    def distance_between(cls, p1, p2):
        """Returns distance between to points, in meters, rounded up to 2 decimal points"""
        return round((vincenty(p1, p2).meters),2)

    @classmethod
    def bearing_to_north(cls, lat1, lon1, lat2, lon2):
        dLon = lon2 - lon1
        y = math.sin(dLon) * math.cos(lat2)
        x = math.cos(lat1) * math.sin(lat2) - math.sin(lat1) * math.cos(lat2) * math.cos(dLon)
        return math.atan2(y, x)

    @classmethod
    def turn_between(cls, d1, d2):
        """Calculates tuple (turn angle [degrees], turn dir [+1 == right, -1 == left])."""
        diff = d1 - d2
        neg = diff < 0
        big = abs(diff) > math.pi

        if not neg and not big: theta = diff; lr = +1
        if not neg and big: theta = 2*math.pi - diff; lr = -1
        if neg and not big: theta = abs(diff); lr = -1
        if neg and big: theta = 2*math.pi - abs(diff); lr = +1

        return (theta*57.2957795, lr)

