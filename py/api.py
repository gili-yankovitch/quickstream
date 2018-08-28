#!/usr/bin/python
import requests
import json

_PASSWORD = "Aa123456"

class QuickStream:
	_PROTOCOL = "https"
	E_READ = 0
	E_WRITE = 1

	def __init__(self, node, port = 443):
		self.node = node
		self.port = port
		self.session_key = None
		self.uid = None

	def _node_url(self, func):
		return "%s://%s:%d/%s" % (QuickStream._PROTOCOL, self.node, self.port, func)

	def _action(self, func, data):
		# If logged in already, add credentials
		if self.session_key is not None and func != "login":
			data["Id"] = self.uid
			data["SessionKey"] = self.session_key

		rsp = requests.post(self._node_url(func), data = json.dumps(data))

		try:
			r = json.loads(rsp.text)
		except Exception as e:
			print(rsp.text)
			raise e

		if not r["Success"]:
			raise Exception("%s(): %s" % (func, r["Message"]))

		return r

	def register(self, password):
		r = self._action("createUser", {
			"Key": _PASSWORD
			})

		return {"Id": r["Id"], "NodeId": r["NodeId"]}

	def login(self, nodeId, uid, key):
		r = self._action("login", {
			"Id": uid,
			"NodeId": nodeId,
			"Key": key
			})

		self.uid = uid
		self.nodeId = nodeId;
		self.session_key = r["SessionKey"]

		return self.session_key

	def createQueue(self, name, readers):
		r = self._action("createQueue", {
			"QueueName": name,
			"Readers": readers
			})

	def writeQueue(self, name, data):
		r = self._action("queue/%d/%d/%s" % (self.nodeId, self.uid, name), {
			"Action": QuickStream.E_WRITE,
			"Data": data
			})

	def readQueue(self, nodeId, uid, name, commit = False):
		r = self._action("queue/%d/%d/%s" % (nodeId, uid, name), {
			"Action": QuickStream.E_READ,
			"Commit": commit
			})

		return r["Messages"]

if __name__ == "__main__":
	api = QuickStream("quickstream.me")

	uids = []

	# Register 4 new users
	for i in range(4):
		uids.append(api.register(_PASSWORD))

	# Pick the last one
	uid = uids[-1]

	print("Register success. UID: %d on Node: %d" % (uid["Id"], uid["NodeId"]))

	# Login
	sess = api.login(uid["NodeId"], uid["Id"], _PASSWORD)

	print("Login (uid = %d) success. SessionKey: %s" % (uid["Id"], sess))

	# Register the rest of the uids
	api.createQueue("queue0", [ [x["Id"], x["NodeId"]] for x in uids[:-1] ])

	print("Created queue: queue0")

	print("Writing to Node 0")

	# Write data to queue
	api.writeQueue("queue0", "Hello, world! 0")
	api.writeQueue("queue0", "Hello, world! 1")

	print("Writing a large message")
	try:
		api.writeQueue("queue0", "a" * 16384)
	except:
		print("Queue rejected our message - Too large.")

	print("Written to queue0")

	# api = QuickStream("localhost", 8090)

	#print("Reading from Node 8090")

	# Login as a different uid
	sess = api.login(uids[0]["NodeId"], uids[0]["Id"], _PASSWORD)

	print("Login (uid = %d nodeId = %d) success. SessionKey: %s" % (uids[0]["Id"], uids[0]["NodeId"], sess))

	# Read from queue
	print("Read")
	print(api.readQueue(uids[-1]["NodeId"], uids[-1]["Id"], "queue0"))

	print("Read + Commit")
	print(api.readQueue(uids[-1]["NodeId"], uids[-1]["Id"], "queue0", commit = True))

	print("Read again")
	print(api.readQueue(uids[-1]["NodeId"], uids[-1]["Id"], "queue0"))