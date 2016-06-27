import threading

def on_event(f):
    from event_processor import EventProcessor
    def callback(*args):
        event = f(*args)
        EventProcessor.handle_detection(event)

    return callback

class Detector(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)

    @classmethod
    def start_plugins(cls):
        for sc in cls.__subclasses__():
            sc().start()
