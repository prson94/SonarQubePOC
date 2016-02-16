(function ($) {

    amplify.request.define("PageNavigationRequest", "ajax", { url: '/api/{type}/{id}/navigation', type: 'GET' });

    var defaults = {
        tabCookieName: 'pageTabs',
        type: null,
        id: null,
        emptymessage: '',
        tabs: null,
        tabID: 'PageTabNavigation',
        context: null,
        lazyLoadTabs: true,
        preSelectedTab: ''
    };

    var methods = {
        init: function (options) {

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('PageNavigation'),
                    PageNavigation = null;

                if (!data) {

                    $(this).data('PageNavigation', {
                        Target: $this,
                        ID: options.tabID,
                        PageNavigation: PageNavigation,
                        Options: options,
                        DefaultContextOn: true 
                    });

                }

                if (options.type && options.id) {// && (options.id >= 0)
                    //$this.load('/Parts/Tabs?type=' + options.type + '&id=' + options.id);
                    PageNavigation = loadTabs($this, options);
                }
                else {
                    showEmptyMessage($this);
                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('PageNavigation');

                data.PageNavigation.remove();
                $this.removeData('PageNavigation');
                //$(window).unbind('.tooltip');
            });
        },
        reload: function (type, id) {
            var data = $(this).data("PageNavigation");
            var $this = data.Target;
            var options = data.Options;
            options.type = type;
            options.id = id;
            options.tabID = data.ID;

            if (type && (id >= 0)) {
                //$this.load('/Parts/Tabs?type=' + options.type + '&id=' + options.id);
                data.PageNavigation = loadTabs($this, options);
                data.DefaultContextOn = false;
                $(this).data("PageNavigation", data);
            }
            else {
                showEmptyMessage($this);
                data.PageNavigation = null;
                data.DefaultContextOn = true;
                $(this).data("PageNavigation", data);
            }
        },
        select: function (index) {
            var $this = $(this).data("PageNavigation").Target;
            var options = $(this).data("PageNavigation").Options;
            $('#' + options.tabID).jqxTabs('select', index);
            amplify.publish("PageTabChanged", {});
        },
        tabLoaded: function (index) {
            var $this = $(this).data("PageNavigation").Target;
            var options = $(this).data("PageNavigation").Options;

            var selectedTabID = "#" + options.tabID + index;
            var loaded = $(selectedTabID).data("loaded");
            return loaded;
        }
    };

    $.fn.PageNavigation = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.PageNavigation');
        }

    };

    //#region Private Methods

    function clear($obj) {
        $obj.html('');
    }

    function loadTabs($obj, options) {
        try {
            amplify.request("PageNavigationRequest", { type: options.type, id: options.id }, function (data) {

                clear($obj);

                if (data) {
                    var tabs = $("<div id='" + options.tabID + "'></div>");
                    $obj.append(tabs);
                    var preSelectedTabID = 0;
                    var tabHtml = "";
                    var divHtml = "";
                    tabHtml += "<ul class='tabs' style='width: 100% !important;'>";
                    $.each(data, function (idx, t) {
                        tabHtml += "<li><i class='fa fa-" + t.Icon + " fa-2x' title='" + t.Title + "'></i><div>" + t.Title + "</div></li>";
                        divHtml += "<div id='" + options.tabID + idx + "' data-uri='" + t.Uri + "' data-loaded='false'><i class='fa fa-spinner fa-spin fa-4x'></i></div>";
                        if (options.preSelectedTab) {
                            if (options.preSelectedTab == t.Title) {
                                preSelectedTabID = idx;
                            }
                        }
                    });
                    tabHtml += "</ul>";

                    tabs.html(tabHtml + divHtml);
                    tabs.jqxTabs({ theme: 'bootstrap', keyboardNavigation: false, scrollPosition: 'both', selectionTracker: true, animationType: 'fade', width: '100%' });

                    tabs.on('selected', function (event) {
                        loadTabData($obj, options, event.args.item);
                    });

                    if (preSelectedTabID == 0) {
                        preSelectedTabID = $.jqx.cookie.cookie(options.tabCookieName);
                        if (!preSelectedTabID) {
                            preSelectedTabID = 0;
                        }
                    }

                    $.each(data, function (idx, t) {
                        if (!t.LazyLoad) loadTabData($obj, options, idx);
                    });

                    loadTabData($obj, options, preSelectedTabID);

                    tabs.jqxTabs('select', preSelectedTabID);
                }
            });

            o = null;
        } catch (e) {
            logError("PageNavigation.js : loadTabs", e);
        }
    }

    function showEmptyMessage($obj) {
        try {
            var options = $obj.data("PageNavigation").Options;
            $obj.html("<div class='emptymessage'>" + options.emptymessage + "</div>");
        } catch (e) {
            logError("PageNavigation.js : showEmptyMessage", e);
        }
    }

    function loadTabData($obj, options, index) {
        try {
            var selectedTab = index;
            var selectedTabID = "#" + options.tabID + index;

            var loaded = $(selectedTabID).data("loaded");
            if (!loaded) {
                var uri = $(selectedTabID).data("uri");
                $.get(uri, function (data) {
                    $(selectedTabID).html(data);
                });
                $(selectedTabID).data("loaded", true);
            }
            
            $.jqx.cookie.cookie(options.tabCookieName, index);
            //$obj.trigger('tabafterload', null, {});
            amplify.publish("PageTabChanged", { index: index });
        } catch (e) {
            logError("PageNavigation.js : loadTabData", e);
        }
    }

    function checkContext(thisContext, contextToCheck) {
        var contextFulfilled = false;

        try {
            var contextList = thisContext.split(',');
            $.each(contextList, function (idx, contextItem) {
                if (contextItem == contextToCheck) {
                    contextFulfilled = true;
                }
            });
        } catch (e) {
            logError("PageNavigation.js : checkContext", e);
        }

        return contextFulfilled;
    }

    //#endregion

})(jQuery);