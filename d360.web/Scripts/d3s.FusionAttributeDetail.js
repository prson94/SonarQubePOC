/// <reference path="../scripts.js" />

(function ($) {

    var methods = {
        init: function (options) {
            var defaults = {
                id: null,
                uri: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('FusionAttributeDetail');


                if (!data) {

                    if (options.id) {
                        $this.load("/Fusion/ViewAttribute?id=" + options.id);
                    }

                    $(this).data('FusionAttributeDetail', {
                        Target: $this,
                        Options: options
                    });

                }
            });
        },
        clearView: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FusionAttributeDetail'),
                    options = data.Options;

                options.id = null;

                $(this).data('FusionAttributeDetail', {
                    Target: $this,
                    Options: options
                });

                $this.html("");
            });
        },

        loadView: function (attributeID) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FusionAttributeDetail'),
                    options = data.Options;

                options.attributeID = attributeID;

                $(this).data('FusionAttributeDetail', {
                    Target: $this,
                    Options: options
                });

                $this.load("/Fusion/ViewAttribute?id=" + attributeID);
                amplify.publish("FusionAttributeViewLoaded", { id: options.id });
            });
        },

        loadUri: function (uri) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FusionAttributeDetail'),
                    options = data.Options;

                options.uri = uri;

                $(this).data('FusionAttributeDetail', {
                    Target: $this,
                    Options: options
                });

                $this.load(uri);
                amplify.publish("FusionAttributeViewUriLoaded", { uri: this.options.uri });
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('FusionAttributeDetail');

                $this.removeData('FusionAttributeDetail');
            });
        }
    };

    $.fn.FusionAttributeDetail = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.FusionAttributeDetail');
        }

    };

})(jQuery);
