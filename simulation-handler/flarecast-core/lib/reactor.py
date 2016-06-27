class Reactor():
    event_kinds = {}

    @classmethod
    def add_plugin_events(cls):
        for c in cls.__subclasses__():
            plugin = c()
            ek = c.event_kinds
            for e in ek:
                cls.event_kinds[e] = plugin

    @classmethod
    def react(cls, alert):
        cls.event_kinds[alert.kind].react(alert)
