file = open("new.txt",w+)
file.write(int(os.environ.get('FLARECAST_PORT')))
file.close()
