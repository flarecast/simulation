from os.path import dirname, basename, isfile, abspath
import os
import glob
import sys

lib_dir = os.sep.join( os.path.abspath(__file__).split('/')[:-2] + ['lib'] )

plugin_dir = lib_dir + os.sep + 'plugins'

# Adds current project to path
sys.path.insert(0, lib_dir)
sys.path.insert(0, plugin_dir)

modules = []

for root, directories, filenames in os.walk(lib_dir):
    for file in filenames:
        if file.endswith('.py'):
            modules.append(file[:-3])

# Loads plugin files
for root, directories, filenames in os.walk(plugin_dir):
    for directory in directories:
        sys.path.insert(0, os.path.join(root, directory))

    for file in filenames:
        if file.endswith('.py'):
            modules.append(file[:-3])

for m in modules:
    __import__(m, locals(), globals())


from event_processor import EventProcessor

class App():
    def run(self):
        EventProcessor().run()

App().run()
