from circuits import Component, Event, handler
from message_handler import MessageHandler
from message import Message
from reactor import Reactor
from detector import Detector
from alert import Alert
from gps import GPS

class msg_received(Event):
    """Message Received Event"""

class detection_received(Event):
    """Detection Received Event"""

class EventProcessor(Component):
    _singleton = None

    def __new__(cls, *args, **kwargs):
        if not cls._singleton:
            cls._singleton = super(EventProcessor, cls).__new__(cls)
        return cls._singleton

    def __init__(self):
        super(EventProcessor, self).__init__()
        self.message_handler = MessageHandler()
        #self.gps = GPS()
        Message.init(self.message_handler.plugin.address())
        Detector.start_plugins()
        Reactor.add_plugin_events()

    def react_internal(self, event):
        alert = Alert(event)
        Reactor.react(alert)

    def compute_distance(self, location):
        # Not relevant for simulation
        #current_location = self.gps.get_current_coordinates()
        #return GPS.distance_between(current_location, location)
        return 0

    def react_external(self, event):
        distance = self.compute_distance(event.location)
        alert = Alert(event, distance)
        Reactor.react(alert)

    @handler("msg_received")
    def msg_received(self, *args):
        self.react_external(args[0])

    @handler("detection_received")
    def detection_received(self, *args):
        event = args[0]
        if event.own_warning:
            self.react_internal(event)
        self.message_handler.emit_event(event)

    @classmethod
    def handle_detection(cls, event):
        cls._singleton.fire(detection_received(event))

    @classmethod
    def handle_message(cls, event):
        cls._singleton.fire(msg_received(event))

