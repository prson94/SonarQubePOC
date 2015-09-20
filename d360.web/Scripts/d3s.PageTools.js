(function ($) {

    amplify.request.define("PageActionsRequest", "ajax", { url: '/api/{type}/{id}/actions/{context}', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                type: null,
                id: null,
                context: "default"
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('PageTools'),
                    PageTools = null;

                if (options.type && (options.id >= 0)) {
                    PageTools = load($this, options.type, options.id, options.context);
                }

                if (!data) {

                    /*  Do more setup stuff here        */
                    $(this).data('PageTools', {
                        Target: $this,
                        PageTools: PageTools,
                        Options: options
                    });

                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        clear: function () {
            var $this = $(this).data("PageTools").Target;
            clear($this);
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('PageTools');

                data.PageTools.remove();
                $this.removeData('PageTools');
                //$(window).unbind('.tooltip');
            });
        },
        reload: function (type, id, context) {
            if (!context) context = "default";
            var $this = $(this).data("PageTools").Target;
            var options = $(this).data("PageTools").Options;
            var tools = $(this).data("PageTools").PageTools;

            options.type = type;
            options.id = id;
            options.context = context;

            $(this).data('PageTools', {
                Target: $this,
                PageTools: tools,
                Options: options
            });

            if (type && (id >= 0)) {
                load($this, type, id, context);
            }
            else {
                clear($this);
            }
        },
        refresh: function () {
            var $this = $(this).data("PageTools").Target;
            var options = $(this).data("PageTools").Options;
            console.log(options.type + ' ' + options.id);
            load($this, options.type, options.id, options.context);
        }
    };

    $.fn.PageTools = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.tooltip');
        }

    };

    //#region Private Methods

    function clear($obj) {
        $obj.html('');
    };

    function loadLevel(node, level) {
        var html = "";
        var iconSize = (level == 0) ? "fa-2x" : "";

        try {
            //var dataAttributes = " data-commandname='" + node.Context + "' data-context='" + (node.CommandName ? node.CommandName : '') + "' data-uri='" + node.Uri + "'";
            //$.each(node.CustomData, function (idx, c) {
            //    dataAttributes += " data-" + c.Name + "='" + c.Value + "'";
            //});

            //switch (level) {
            //    case 0:
            //        html += "<h4>" + node.Title + "</h4>";
            //        break;
            //    case 1:
            //        if (node.Uri != '' && node.Uri) {
            //            html += "<div" + dataAttributes + " class='action'>" + node.Title + "</div>";
            //        }
            //        else
            //            html += "<h5>" + node.Title + "</h5>";
            //        break;
            //    case 2:
            //        html += "<span" + dataAttributes + " class='action' style='margin-right: 20px; font-size: .9em'>" + node.Title + "</span>";
            //        break;
            //}
            
            //if (level == 1 && node.Items.length > 0) {
            //    html += "<div style='margin-left: 15px'>";
            //}
            //$.each(node.Items, function (idx, c) {
            //    html += loadLevel(c, level + 1, html);
            //});
            //if (level == 1 && node.Items.length > 0) {
            //    html += "</div>";
            //}

            html += "<li id='" + node.Title + "' ";
            html += "data-uri='" + node.Uri + "' data-context='" + node.Context + "' data-commandname='" + (node.CommandName ? node.CommandName : '') + "'";
            $.each(node.CustomData, function (idx, c) {
                html += " data-" + c.Name + "='" + c.Value + "'";
            });

            if (!node.Enabled) {
                html += " disabled='true'";
            }

            html += ">";
            if (level == 0 && node.Icon != '') {
                html += "<i data-toggle='tooltip'";
                if (node.Warning) html += " title=\"" + node.Warning + "\" ";
                html += "class='fa fa-" + node.Icon + " " + iconSize + "'></i>";
                if (node.Title) html += "<div style='font-size: 75%'>" + node.Title + "</div>";
            }
            if (level > 0) html += node.Title;

            if (node.Items.length > 0) {
                html += "<ul>";
                $.each(node.Items, function (idx, c) {
                    html += loadLevel(c, level +1, html);
                });
                html += "</ul>";
            }
            html += "</li>";
        } catch (e) {
            logError("PageTools.js : loadLevel", e);
        }

        return html;
    };

    function load($obj, type, id, context) {
        try {
            amplify.request("PageActionsRequest", { type: type, id: id, context: context }, function (data) {

                clear($obj);

                if (data) {
                    var html = "";
                    html += "<div id='tools' style='border: none;'>";
                    html += "<ul>";
                    $.each(data, function (idx, c) {
                        html += loadLevel(c, 0); //recurse ULs with child LIs
                    });
                    html += "</ul>";
                    html += "</div>";

                    $obj.html(html);

                    $("#tools").jqxMenu({ theme: 'plain', mode: 'vertical', showTopLevelArrows: false });
                    //$('.tool').bind('click', function (event) {
                    $('#tools').bind('itemclick', function (event) {
                        var li = event.args;
                        if ($(li).data("context") == "command") {
                            amplify.publish("CommandAction", { uri: $(li).data("uri"), customdata: $(li).data() });
                        }
                        else if ($(li).data("context") == "link") {
                            document.location.assign($(li).data("uri"));
                        }
                        else if ($(li).data("context") == "none") {
                            //do nothing
                        }
                        else {
                            amplify.publish("ToolAction", { override: $(li).data("override"), uri: $(li).data("uri"), context: $(li).data("context"), customdata: $(li).data() });
                        }
                    });
                }
            });
        } catch (e) {
            logError("PageTools.js : load", e);
        }
    };

    //#endregion

})(jQuery);