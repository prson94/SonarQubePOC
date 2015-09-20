(function ($) {

    var methods = {
        init: function (options) {
            var defaults = {
                context: 'form',
                savetext: 'Save',
                canceltext: 'Cancel'
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('FormTools');

                $this.addClass("FormTools");

                if (!data) {

                    var cancelID = $this.attr('id') + '-cancel-button';
                    var saveID = $this.attr('id') + '-save-button';

                    $this.append("<button class='saveButton' id='" + saveID + "' type='button' data-context='" + options.context + "'>" + options.savetext + "</button>");
                    $this.append("<button id='" + cancelID + "' type='button' data-context='" + options.context + "'>" + options.canceltext + "</button>");

                    $('#' + saveID).jqxButton({ theme: theme });

                    $('#' + cancelID).jqxButton({ theme: theme }).click(function () {
                        var ctx = $(this).data('context');
                        amplify.publish("CancelAction", { context: ctx });
                    });

                    $(this).data('FormTools', {
                        Target: $this,
                        Options: options
                    });

                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FormTools');

                $this.removeData('FormTools');
                //$(window).unbind('.tooltip');
            });
        },
        disableSave: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FormTools');

                var saveID = $this.attr('id') + '-save-button';
                $('#' + saveID).jqxButton('destroy');
                $('#' + saveID).hide();
            });
        }
    };

    $.fn.FormTools = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.FormTools');
        }

    };

})(jQuery);