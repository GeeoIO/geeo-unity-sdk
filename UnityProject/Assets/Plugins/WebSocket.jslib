var LibraryWebSockets =
{
	$webSocketInstances: [],

	SocketCreate: function(url)
	{
		var urlStr = Pointer_stringify(url);

		var socket =
		{
			socket: new WebSocket(urlStr),
			buffer: new Uint8Array(0),
			error: null,
			messages: []
		};

		socket.socket.binaryType = "arraybuffer";

		socket.socket.onmessage = function(messageEvent)
		{
			if (messageEvent.data instanceof Blob)
			{
				var reader = new FileReader();

				reader.addEventListener("loadend", function()
				{
					var array = new Uint8Array(reader.result);
					socket.messages.push(array);
				});

				reader.readAsArrayBuffer(messageEvent.data);
			}
			else if (messageEvent.data instanceof ArrayBuffer)
			{
				var array = new Uint8Array(messageEvent.data);
				socket.messages.push(array);
			}
			else
				socket.messages.push(messageEvent.data);
		};

		socket.socket.onclose = function(closeEvent)
		{
			if (closeEvent.code != 1000)
			{
				if ((closeEvent.reason != null) && (closeEvent.reason.length > 0))
					socket.error = closeEvent.reason;
				else
				{
					switch (closeEvent.code)
					{
						case 1001: 
						socket.error = "Endpoint going away.";
						break;

						case 1002: 
						socket.error = "Protocol error.";
						break;

						case 1003: 
						socket.error = "Unsupported message.";
						break;

						case 1005: 
						socket.error = "No status.";
						break;

						case 1006: 
						socket.error = "Abnormal disconnection.";
						break;

						case 1009: 
						socket.error = "Data frame too large.";
						break;

						default:
						socket.error = "Error " + closeEvent.code;
					}
				}
			}
		}

		var instance = webSocketInstances.push(socket) - 1;
		return instance;
	},

	SocketState: function(socketInstance)
	{
		var socket = webSocketInstances[socketInstance];
		return socket.socket.readyState;
	},

	SocketError: function(socketInstance, ptr, length)
	{
		var socket = webSocketInstances[socketInstance];

		if (socket.error == null)
			return 0;

		if (socket.error.length > length)
			return 0;

		writeStringToMemory(socket.error, ptr, false);
		socket.error = null;
		return 1;
	},

	SocketSend: function(socketInstance, ptr, length)
	{
		var socket = webSocketInstances[socketInstance];
		socket.socket.send(HEAPU8.buffer.slice(ptr, ptr+length));
	},

	SocketSendString: function(socketInstance, ptr, length)
	{
		var socket = webSocketInstances[socketInstance];
		var arrayBuffer = HEAPU8.buffer.slice(ptr, ptr+length);
		var messageStr = String.fromCharCode.apply(null, new Uint8Array(arrayBuffer));
		socket.socket.send(messageStr);
	},

	SocketRecvLength: function(socketInstance)
	{
		var socket = webSocketInstances[socketInstance];

		if (socket.messages.length == 0)
			return 0;
		
		return socket.messages[0].length;
	},

	SocketRecv: function(socketInstance, ptr, length)
	{
		var socket = webSocketInstances[socketInstance];

		if (socket.messages.length == 0)
			return 0;

		if (socket.messages[0].length > length)
			return 0;

		HEAPU8.set(socket.messages[0], ptr);
		socket.messages = socket.messages.slice(1);
	},

	SocketRecvString: function(socketInstance, ptr, length)
	{
		var socket = webSocketInstances[socketInstance];

		if (socket.messages.length == 0)
			return 0;

		if (socket.messages[0].length > length)
			return 0;

		writeStringToMemory(socket.messages[0], ptr, false);
		socket.messages = socket.messages.slice(1);
	},

	SocketClose: function(socketInstance)
	{
		var socket = webSocketInstances[socketInstance];
		socket.socket.close();
	}
};

autoAddDeps(LibraryWebSockets, '$webSocketInstances');
mergeInto(LibraryManager.library, LibraryWebSockets);
