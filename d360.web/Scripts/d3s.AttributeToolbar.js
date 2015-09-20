(function ($) {

    amplify.request.define("AttributeActionRequest", "ajax", { url: '/attributes/AttributeActions?type={type}&id={id}&owner={owner}&ownerID={ownerID}&attributeID={attributeID}', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                type: null,
                id: null,
                autoOpenPopup: true,
                mode: 'horizontal'
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('AttributeToolbar');

                if (!data) {

                    $(this).data('AttributeToolbar', {
                        Target: $this,
                        Options: options
                    });

                }
            });
        },
        loadTools: function (type, id, owner, ownerID, attributeID) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('AttributeToolbar'),
                    options = data.Options;

                options.type = type;
                options.id = id;
                //objectType={type} objectTypeID={typeID} objectName={object} objectID={objectID} attributeID={id}
                amplify.request("AttributeActionRequest", { type: type, id: id, owner: owner, ownerID: ownerID, attributeID: attributeID }, function (data) {
                    if (data) {
                        clear($this);
                        var menu = $("<div style='border: none !important;'></div>");
                        menu.append(loadMenuItems(options, data, ""));
                        $this.append(menu);
                        menu.jqxMenu({ showTopLevelArrows: false, enableRoundedCorners: false, theme: theme, autoOpenPopup: options.autoOpenPopup, mode: options.mode });//, mode: 'vertical'
                        //$this.jqxMenu('setItemOpenDirection', 'TopMenu', 'left', 'down');
                        menu.bind('itemclick', function (event) {
                            var li = event.args;
                            amplify.publish('AttributeToolAction', { uri: $(li).data("uri") });
                        });
                    }
                });

                $(this).data('AttributeToolbar', {
                    Target: $this,
                    Options: options
                });
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('AttributeToolbar');

                $this.removeData('AttributeToolbar');
            });
        }
    };

    $.fn.AttributeToolbar = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.AttributeToolbar');
        }

    };

    //#region Private Methods

    function clear($obj) {
        $obj.html('');
    };

    function loadMenuItems(options, data, html)
    {
        try {
            if (data) {

                html = "<ul>";

                $.each(data, function (idx, t) {
                    html += "<li data-uri='" + t.Uri + "'><i class='fa fa-" + t.Icon + "'";
                    if (t.Title != "" && t.Title) {
                        html += " title='" + t.Title + "'></i>" + t.Title
                    }
                    else {
                        html += "></i>";
                    }

                    if (t.Items.length > 0) {
                        html += loadMenuItems(options, t.Items);
                    }
                    html += "</li>";
                });

                html += "</ul>";
            }
        } catch (e) {
            logError("AttributeToolbar.js : loadMenuItems", e);
        }

        return html;
    }

    //#endregion

})(jQuery);