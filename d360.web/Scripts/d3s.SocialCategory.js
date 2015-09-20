/// <reference path="../scripts.js" />

(function ($) {

    amplify.request.define("SurveyTypeReport", "ajax", { url: '/api/surveys/{typeID}/{type}/{id}/report', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                followed: false,
                type: null,
                id: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('SocialCategory');

                $this.addClass("social");

                if (!data) {
                    $(this).data('SocialCategory', {
                        Target: $this,
                        Options: options
                    });

                    reload($this);
                }
            });
        },
        reload: function (surveyTypeID, objectType, objectID) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('SocialCategory'),
                    options = data.Options;

                options.surveyTypeID = surveyTypeID;
                options.objectType = objectType;
                options.objectID = objectID;

                $(this).data('SocialCategory', {
                    Target: $this,
                    Options: options
                });

                loadReport($this);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('SocialCategory');

                $this.removeData('SocialCategory');
            });
        }
    };

    $.fn.SocialCategory = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.SocialCategory');
        }

    };

    //#region Private Methods

    function reload($obj) {
        try {
            var data = $obj.data('SocialCategory'),
                options = data.Options;

            // Clear the data from this element.
            $obj.html('');

            var treeID = $obj.id + 'Tree';

            $('#' + treeID).jqxTree({ theme: theme });

            $(treeID).bind("select", function (evt) {
                var node = $(evt.args.element);//.find(".node");
                var id = node.data("categoryid");
                var ca = node.data("category");
                amplify.publish("SocialPostsFiltered", { category: ca, categoryid: id });
            });

            //}
        } catch (e) {
            logError("SocialCategory.js : reload", e);
        }
    }

    //#endregion

})(jQuery);