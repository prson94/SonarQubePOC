/// <reference path="../scripts.js" />

(function ($) {

    var methods = {
        init: function (options) {
            var defaults = {
                uri: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('ObjectDetail');

                if (!data) {

                    if (options.uri) {
                        $this.load(options.uri);
                    }

                    $(this).data('ObjectDetail', {
                        Target: $this,
                        Options: options
                    });

                }
            });
        },
        loadUri: function (uri) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('ObjectDetail'),
                    options = data.Options;

                options.uri = uri;

                $(this).data('ObjectDetail', {
                    Target: $this,
                    Options: options
                });

                $this.load(uri);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('ObjectDetail');

                $this.removeData('ObjectDetail');
            });
        }
    };

    $.fn.ObjectDetail = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.ObjectDetail');
        }

    };

})(jQuery);