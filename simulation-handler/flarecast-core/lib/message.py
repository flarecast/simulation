import redis
import pickle

class Message():
    current_id = 0

    def __init__(self, event, addr, insistence, sender, msg_id = None):
        self.event = event
        self.addr = addr
        self.insistence = insistence
        self.sender = sender
        if msg_id is None:
            Message.current_id += 1
            self.id = Message.__build_id(Message.current_id, sender)
        else:
            self.id = msg_id

    @classmethod
    def __build_id(cls, id, sender):
        return str(id) + '-' + str(sender)

    def event_creator(self):
        # TODO: change to event.creator
        return self.sender

    def has_expired(self):
        return self.event.has_expired()

    @classmethod
    def init(cls, namespace, host = 'localhost', port = 6379):
        cls.__namespace = str(namespace)
        cls.__redis = redis.Redis(host=host, port=port, db=0)
        cls.__redis.flushdb()

    @classmethod
    def register(cls, msg_id, addrs = set()):
        reg_addrs = cls.__get(msg_id)
        saved_addrs = addrs if reg_addrs is None else reg_addrs | addrs
        cls.__set(msg_id, saved_addrs)

    @classmethod
    def addrs(cls, msg_id):
        return cls.__get(msg_id)

    @classmethod
    def ids(cls):
        return {cls.__uns(k.decode('utf-8')) for k in cls.__redis.keys(cls.__namespace+'-*')}

    @classmethod
    def __ns(cls, key):
        return cls.__namespace+'-'+key
    @classmethod
    def __uns(cls,key):
        import re
        m = re.search(cls.__namespace+"-(.+)", text)
        return m.group(1)

    @classmethod
    def __set(cls, key, val):
        cls.__redis.set(cls.__ns(key), cls.__d(val))

    @classmethod
    def __get(cls, key):
        return cls.__l(cls.__redis.get(cls.__ns(key)))

    @classmethod
    def __l(cls, obj):
        return None if obj is None else pickle.loads(obj)

    @classmethod
    def __d(cls, obj):
        return pickle.dumps(obj)

