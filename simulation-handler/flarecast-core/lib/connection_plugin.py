import threading

class ConnectionPlugin(threading.Thread):
    def init(self):
        threading.Thread.__init__(self)

    @classmethod
    def active_plugin(cls):
        if len ( cls.__subclasses__() ) > 1:
            raise RuntimeError('More than one Communication Plugin in project')

        return cls.__subclasses__()[0]()

