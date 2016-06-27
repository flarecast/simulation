import time

class Event():
    def __init__(self, location, direction, kind, own_warning, timestamp, lifetime, extra = None):
        self.location = location
        self.direction = direction
        self.kind = kind
        self.own_warning = own_warning
        self.timestamp = timestamp
        self.lifetime = lifetime
        self.extra = extra

    def has_expired(self):
        return time.time() > (self.timestamp + self.lifetime)

