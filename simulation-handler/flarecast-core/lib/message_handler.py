from connection_plugin import ConnectionPlugin
from message import Message
import threading
import time

def on_message(f):
    def callback(*args):
        from event_processor import EventProcessor
        handler = MessageHandler._singleton
        msg = f(*args)

        if(handler._valid_message(msg)):
            print("RECEIVED A VALID MESSAGE")
            EventProcessor.handle_message(msg.event)
            handler.emit_event(msg.event, msg.sender, msg.id)
        else:
            # Register the sender of the message
            # in case multiple people are sending us the same message
            # that way we won't send to them
            print("RECEIVED AN INVALID MESSAGE")
            Message.register(msg.id, {msg.sender})

    return callback

class MessageHandler():
    _singleton = None
    REMOVAL_INTERVAL = 1800

    # Singleton Pattern implementation
    def __new__(cls, *args, **kwargs):
        if cls._singleton is None:
            cls._singleton = super(MessageHandler, cls).__new__(cls)
        return cls._singleton

    def __init__(self):
        self.plugin = ConnectionPlugin.active_plugin()
        self.plugin.start()

    def broadcast(self, msg):
        Message.register(msg.id, {msg.event_creator(), msg.sender})
        self.plugin.broadcast(msg)

    def emit_event(self, event, sender = None, msg_id = None):
        addr = self.plugin.address()
        insist = MessageHandler.__insistence(event.lifetime)
        if sender is None: sender = addr
        if msg_id is not None:
            msg = Message(event, addr, insist, sender, msg_id)
        else:
            msg = Message(event, addr, insist, sender)
        self.broadcast(msg)

    @staticmethod
    def __insistence(lifetime):
        # TODO: find a better formula for this
        return 0.3 * lifetime

    @staticmethod
    def _valid_message(msg):
        return Message.addrs(msg.id) is None and not msg.has_expired()
