#!/usr/bin/python
import requests
import json

_PASSWORD = "Aa123456"

class QuickStream:
	_PROTOCOL = "http"
	E_READ = 0
	E_WRITE = 1

	def __init__(self, node, port = 8080):
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

		return r["Id"]

	def login(self, uid, key):
		r = self._action("login", {
			"Id": uid,
			"Key": key
			})

		self.uid = uid
		self.session_key = r["SessionKey"]

		return self.session_key

	def createQueue(self, name, readers):
		r = self._action("createQueue", {
			"QueueName": name,
			"Readers": readers
			})

	def writeQueue(self, name, data):
		r = self._action("queue/%d/%s" % (self.uid, name), {
			"Action": QuickStream.E_WRITE,
			"Data": data
			})

	def readQueue(self, uid, name, commit = False):
		r = self._action("queue/%d/%s" % (uid, name), {
			"Action": QuickStream.E_READ,
			"Commit": commit
			})

		return r["Messages"]

if __name__ == "__main__":
	api = QuickStream("localhost")

	uids = []

	# Register 4 new users
	for i in range(4):
		uids.append(api.register(_PASSWORD))

	# Pick the last one
	uid = uids[-1]

	print("Register success. UID: %d" % uid)

	# Login
	sess = api.login(uid, _PASSWORD)

	print("Login (uid = %d) success. SessionKey: %s" % (uid, sess))

	# Register the rest of the uids
	api.createQueue("queue0", uids[:-1])

	print("Created queue: queue0")

	# Write data to queue
	api.writeQueue("queue0", "Hello, world!")

	print("Written to queue0")

	# Login as a different uid
	sess = api.login(uids[0], _PASSWORD)

	print("Login (uid = %d) success. SessionKey: %s" % (uids[0], sess))

	# Read from queue
	print(api.readQueue(uids[-1], "queue0"))