document.addEventListener('DOMContentLoaded', function () {

    var videoPlayer = amp("azuremediaplayer");

    function generateRandomName() {
        return Math.random().toString(36).substring(2, 10);
    }

    // Get the user name and store it to prepend to messages.
    var username = generateRandomName();
    var promptMessage = 'Enter your name:';
    do {
        username = prompt(promptMessage, username);
        if (!username || username.startsWith('_') || username.indexOf('<') > -1 || username.indexOf('>') > -1) {
            username = '';
            promptMessage = 'Invalid input. Enter your name:';
        }
    } while (!username)

    // Set initial focus to message input box.
    var messageInput = document.getElementById('message');
    messageInput.focus();

    function createMessageEntry(encodedName, encodedMsg, currentTime, ugly, private) {
        var entry = document.createElement('div');
        var uglyClass = ugly ? "ugly" : "";
        var privateClass = private ? "private" : "";

        entry.classList.add("message-entry");
        if (encodedName === "_SYSTEM_") {
            entry.innerHTML = encodedMsg;
            entry.classList.add("text-center");
            entry.classList.add("system-message");
        } else if (encodedName === "_BROADCAST_") {
            entry.classList.add("text-center");
            entry.innerHTML = `<div class="text-center broadcast-message">${encodedMsg}<em>${new Date().getDate()}<em></div>`;
        } else if (encodedName === username) {
            entry.innerHTML = `<div class="message-avatar pull-right">${encodedName}<br/><i style="font-size:x-small; float:right;">${new Date().toLocaleTimeString()}</i></div>` +
                `<div class="message-content ${uglyClass} ${privateClass} pull-right">${encodedMsg}<br/><i style="font-size:x-small">video time: ${currentTime} secs.</i><div>`;

        } else {
            entry.innerHTML = `<div class="message-avatar pull-left">${encodedName}</div>` +
                `<div class="message-content ${uglyClass} pull-left">${encodedMsg}<br/><i style="font-size:x-small; float:left;">${new Date().toLocaleTimeString()}</i><div>`;
        }
        return entry;
    }

    function bindConnectionMessage(connection) {
        var messageCallback = function (name, message, currentTime, ugly, terms, private) {
            if (!message) return;
            // Html encode display name and message.
            var encodedName = name;
            var encodedMsg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

            if (ugly) {
                terms.forEach(term => {
                    var regEx = new RegExp(term, "ig");
                    encodedMsg = encodedMsg.replace(regEx, `${term.substring(0, 1)}${term.substring(1).replace(/./g, '*')}`);
                });
            }
            var messageEntry = createMessageEntry(encodedName, encodedMsg, currentTime, ugly, private);

            var messageBox = document.getElementById('messages');
            messageBox.appendChild(messageEntry);
            messageBox.scrollTop = messageBox.scrollHeight;
        };
        // Create a function that the hub can call to broadcast messages.
        connection.on('broadcastMessage', messageCallback);
        connection.on('echo', messageCallback);
        connection.onclose(onConnectionError);
    }

    function onConnected(connection) {
        console.log('connection started');
        connection.send('broadcastMessage', '_SYSTEM_', username + ' JOINED');
        document.getElementById('sendmessage').addEventListener('click', function (event) {

            // Call the broadcastMessage method on the hub.
            if (messageInput.value) {
                connection.send('broadcastMessage', username, messageInput.value, videoPlayer.currentTime(), new Date());
            }

            // Clear text box and reset focus for next comment.
            messageInput.value = '';
            messageInput.focus();
            event.preventDefault();
        });
        document.getElementById('message').addEventListener('keypress', function (event) {
            if (event.keyCode === 13) {
                event.preventDefault();
                document.getElementById('sendmessage').click();
                return false;
            }
        });
        document.getElementById('echo').addEventListener('click', function (event) {
            // Call the echo method on the hub.
            connection.send('echo', username, messageInput.value);

            // Clear text box and reset focus for next comment.
            messageInput.value = '';
            messageInput.focus();
            event.preventDefault();
        });
    }

    function onConnectionError(error) {
        if (error && error.message) {
            console.error(error.message);
        }
        var modal = document.getElementById('myModal');
        modal.classList.add('in');
        modal.style = 'display: block;';
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/chat')
        .build();
    bindConnectionMessage(connection);
    connection.start()
        .then(function () {
            onConnected(connection);
        })
        .catch(function (error) {
            console.error(error.message);
        });
});