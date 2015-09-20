var app = (function () {
    "use strict";

    var app = {};

    app.showNotification = function (header, text) {
        $('#notification-message-header').text(header);
        $('#notification-message-body').text(text);
        $('#notification-message').slideDown('fast');
    };

    return app;
})();