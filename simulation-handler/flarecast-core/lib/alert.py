class Alert():
    def __init__(self, event, distance = None):
        self.location = event.location
        self.distance = distance
        self.direction = event.direction
        self.danger = "temp_danger" #TODO: use distance and timestamp for danger
        self.kind = event.kind
        self.extra = event.extra




