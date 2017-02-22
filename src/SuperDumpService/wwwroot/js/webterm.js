// some code here is borrowed from Kudu. see https://github.com/projectkudu/kudu/blob/2a04e469a546a8fe81332d39c79ef6f178236ae1/Kudu.Services.Web/Content/Scripts/KuduExecV2.js

function LoadConsole() {
	var originalMatchString = undefined;
	var currentMatchIndex = -1;
	var lastLine = {
		output: "",
		error: ""
	};;
	var lastUserInput = null;
	var webtermConsole = $('<div class="console">');
	var curReportFun;
	var height = parseInt(window.localStorage.debugconsole_height);
	height = !!height ? height : 500;
	var heightOffset = height / 10;
	var controller = webtermConsole.console({
		continuedPrompt: true,
		promptLabel: function () {
			return getJSONValue(lastLine);
		},
		commandValidate: function () {
			return true;
		},
		commandHandle: function (line, reportFn) {
			curReportFun = reportFn;
			if (line.trim().toUpperCase() === "CLS") {
				controller.reset();
				$(".jquery-console-inner").append($(".jquery-console-prompt-box").css("display", "inline-block"));
				controller.message("", "jquery-console-message-value");
			} else {
				lastUserInput = line + "\n";
				if (lastLine.output) {
					lastLine.output += lastUserInput;
				} else if (lastLine.error) {
					lastLine.error += lastUserInput;
				} else {
					lastLine.output = lastUserInput;
				}
				_sendCommand(lastUserInput);
				controller.resetHistory();
				DisplayAndUpdate(lastLine);
				lastLine = {
					output: "",
					error: ""
				};
				DisplayAndUpdate(lastLine);
			}
		},
		cancelHandle: function () {
			//sending CTRL+C character (^C) to the server to cancel the current command
			_sendCommand("\x03");
		},
		completeHandle: function (line, reverse) {
			return "";
		},
		userInputHandle: function (keycode) {
			//reset the string we match on if the user type anything other than tab == 9
			if (keycode !== 9 && keycode != 16) {
				originalMatchString = undefined;
			}
		},
		cols: 3,
		autofocus: true,
		animateScroll: true,
		promptHistory: true,
		welcomeMessage: "Web Terminal\r\nType 'cls' to clear the console\r\n\r\n"
	});
	window.$webtermConsole = $('#webtermConsole');
	window.$webtermConsole.append(webtermConsole);

	var connection = new WebSocketManager.Connection("ws://localhost:5000/cmd");
	window.$webtermConsole.data('connection', connection);

	connection.connectionMethods.onConnected = () => {
		console.log("You are now connected! Connection ID: " + connection.connectionId);
	};

	connection.connectionMethods.onDisconnected = () => {
		console.log("Disconnected!");
	}

	connection.clientMethods["receiveMessage"] = (message) => {
		console.log("receiveMessage: " + message);
		DisplayAndUpdate(message);
		controller.enableInput();
	};

	connection.start();

	function _sendCommand(input) {
		console.log("_sendCommand: " + input);
		connection.invoke("ReceiveMessage", connection.connectionId, input);
	}

	function endsWith(str, suffix) {
		return str.indexOf(suffix, str.length - suffix.length) !== -1;
	}

	function startsWith(str, prefix) {
		return str.indexOf(prefix) == 0;
	}

	function getJSONValue(input) {
		return input ? (input.output || input.error || "").toString() : "";
	}

	function DisplayAndUpdate(data) {
		var prompt = getJSONValue(data);
		var lastLinestr = getJSONValue(lastLine);
		//this means the last command should be cleared and the next one will be written over it.
		// case 1. lastLine = "progress 10%", prompt = "\r" ==> lastLine is not written into HTML (curl)
		// case 2. lastLine = "progress 10%\r", prompt = "progress 20%\r" ==> lastLine is not written into HTML (youtube-dl)
		//         lastLine = "version 123\r", prompt = "\r\n" ==> lastLine IS WRITTEN into HTML (dotnet tsc)
		if ((endsWith(prompt, "\r") && !endsWith(lastLinestr, "\n")) ||
			(endsWith(lastLinestr, "\r") && prompt !== "\r\n" && prompt !== "\n")) {
			lastLinestr = "";
			lastLine = null;
		}

		var consoleMessages = $(".jquery-console-message");
		if (consoleMessages.length > height && consoleMessages.length % heightOffset == (heightOffset - 1)) {
			consoleMessages.slice(0, consoleMessages.length - height).remove();
		}

		//if the data has the same class as the last ".jquery-console-message"
		//then just append it to the last one, if not, create a new div.
		var lastConsoleMessage = consoleMessages.last();
		lastConsoleMessage.text(lastConsoleMessage.text() + lastLinestr);
		lastLine = null;

		//if the prompt is just \r this means that we don't really need to display anything, just marking the line as 
		if (prompt == "\r") {
			return;
		}

		// display output, but not updating the HTML
		$(".jquery-console-inner").append($(".jquery-console-prompt-box").last().css("display", "inline"));
		if (data.error) {
			$(".jquery-console-prompt-label").last().text(prompt).css("color", "red");
		} else {
			$(".jquery-console-prompt-label").last().text(prompt).css("color", "white");
		}

		controller.promptText("");

		//Now create the div for the new line that will be printed the next time with the correct class
		if (data.error) {
			if (!lastConsoleMessage.hasClass("jquery-console-message-error")) {
				controller.message("", "jquery-console-message-error");
			}
		} else if (!lastConsoleMessage.hasClass("jquery-console-message-value") || endsWith(lastLinestr, "\n")) {
			controller.message("", "jquery-console-message-value");
		}

		//save last line for next time.
		lastLine = data;
		prompt = prompt.trim();
	}

	window.setInterval(function () {
		controller.enableInput();
	}, 2000);
}

$(function () {
	LoadConsole();
})
