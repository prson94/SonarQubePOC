var theme = 'd3s'; //metro
var list_theme = 'd3s'; //lists
var grid_width = '100%';
var overlay_grid_width = '98%';
var field_width98 = '98%';
var field_width = '100%';
var field_height = 25;
var colorScheme = [ '#1f81de', '#036cd2', '#0584fe', '#73baff', '#cce6ff', '#ffc571', '#df901c', '#ffeacb', '#d27d00'];
var chartColorScheme = ['#3979a2', '#818385', '#51a6dc', '#adacac', '#70e2ff', '#cbcaca', '#9affff', '#d5d5d5', '#c10000', '#e2792a'];
var chartStatusColorScheme = ['#3f9d40', '#e2792a', '#c10000'];
var chartYesNoColorScheme = ['#3f9d40', '#d32f2f'];
var chartDefaultTheme = 'scheme07';
var maxTreeHeight = 400;
var progressIndicatorHtml = "<i class='fa fa-spinner fa-spin fa-4x'></i>";

var AmplifyActions = {
    Cancel: 'CancelAction',
    InternalTool: 'InternalToolAction',
    Local: 'LocalAction',
    Unsubscribe: 'UnsubscribeEventsAction',
    OverlayUnsubscribe: 'OverlayUnsubscribeEventsAction',
    TileUnsubscribe: 'TileUnsubscribeEventsAction',
    Save: 'SaveAction',
    Tool: 'ToolAction'
};

//#region GRITTER SETTINGS OVERRIDE
$.extend($.gritter.options, {
    position: 'bottom-right', // possibilities: bottom-left, bottom-right, top-left, top-right
    fade_in_speed: 1000, // how fast notifications fade in (string or int)
    fade_out_speed: 300, // how fast the notices fade out
    time: 2000 // hang on the screen for...
});
//#endregion

//#region Template Helpers

Handlebars.getTemplate = function (name) {
    if (Handlebars.templates === undefined || Handlebars.templates[name] === undefined) {
        $.ajax({
            url: '/content/templates/parts/' + name + '.html',
            cache: true,
            success: function (data) {
                if (Handlebars.templates === undefined) {
                    Handlebars.templates = {};
                }
                Handlebars.templates[name] = Handlebars.compile(data);
            },
            async: false
        });
    }
    return Handlebars.templates[name];
};

Handlebars.registerHelper('formatIsCompleteBool', function (obj) {
    var text = "";
    if (obj)
        text = "Yes";
    else 
        text = "No";
    return text;
});

Handlebars.registerHelper('formatDate', function (obj) {
    return moment.utc(obj.Date).format("LLL");
});

//#endregion

//#region Called by all ajax-enabled forms.
function OnStarting() {
    amplify.publish("StartingAjax");
}
function OnSuccess(data, status, xhr) {
    amplify.publish("EndingAjax");
    try {
        var a = 'form';
        var action = 'add';
        var id = 0;
        if (data.context) { a = data.context; }
        if (data.action) { action = data.action; }
        if (data.id) { id = data.id; }
        amplify.publish("SaveAction", { context: a, action: action, id: id, custom : data.custom });
        amplify.publish("ShowMessage", data);
    } catch (e) {
        logError("Common.js : OnSuccess", e);
    }
}
function OnFailed(data, status, xhr) {
    amplify.publish("EndingAjax");
    var title = "Error Occurred!";
    var message = xhr;
    try {
        if (data.responseText) {
            var json = JSON.parse(data.responseText);
            if (json.title) {
                title = json.title;
            }
            if (json.message) {
                message = json.message;
            }
        }
    } catch (e) {}
    amplify.publish("ShowMessage", { title: title, message: message, type: 'error' });
}
//#endregion

function PostCommandStatus(data) {
    var _title = "";
    var _type = "";
    if (data.type == "error") {
        _title = "Error!";
        _type = "error";
    }
    else {
        _title = "Success!";
        _type = "confirm";
    }
    amplify.publish("ShowMessage", { title: _title, message: data.message, type: _type });
}

function formatShortDate(date) {
    return moment(date).format("M/D/YY");
}

function formatDate(date) {
    return moment(date).format("M/D/YY h:mm A");
}

function QuickTip(tip) {
    $(this).qtip({
        content: {
            title: '',
            text: tip,
        },
        position: {
            at: 'bottom center', // Position the tooltip above the link
            my: 'top center',
            viewport: $(window), // Keep the tooltip on-screen at all times
            effect: false // Disable positioning animation
        },
        overwrite: false,
        show: {
            event: event.type,  // show using same event as above.
            solo: false,         // Only show one tooltip at a time
            ready: true
        },
        hide: {
            fixed: true,
            delay: 1000,
        },
        //hide: 'mouseout',
        style: {
            classes: 'qtip-blue qtip-rounded'
        }
        //addTooltip(this);
    });
}

//#region UTILITIES

var setTreeHeight = function (obj) {
    if (obj.height() > maxTreeHeight) {
        obj.jqxTree({ height: maxTreeHeight });
    }
}

//#endregion

//#region Custom Grid Renderers
var progressrenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = "<div style='padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 4px'>" + value + "%</div>";
    return html;
}

var booleanrenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = "No";
    if (value == "true" || value == "True" || value == true || value == "1") html = "Yes";
    return textrenderer(html);
}

var shortdaterenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = formatShortDate(value);
    return textrenderer(html);
}

var daterenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = formatDate(value);
    return textrenderer(html);
}

var impactrenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = textrenderer("<img src='/Content/images/impact/" + value + ".png' />");
    return html;
}

var probabilityrenderer = function (index, datafield, value, defaultvalue, column, data) {
    var html = textrenderer(value + "%");
    return html;
}

var quickTipRenderer = function (name, tip) {
    var html = "";

    try {
        html = '<span class="quickTip" onclick="QuickTip(\'' + tip + '\')">' + name + '</span>';
    } catch (e) {
        console.log(e);
    }

    return html;
}

var redFlaggedRenderer = function (data, isOwner) {
    var html = "";

    try {
        var flagMessage = data.RedFlagged ? "This item has notes!" : "";
        var flagCss = data.RedFlagged ? "active-flag" : "inactive-flag";
        if (isOwner) {
            flagCss += " flag-clickable";
        }
        html = "<i title='" + flagMessage + "' class='fa fa-flag " + flagCss + "'></i>";

        if (isOwner) {
            html = "<a data-active='" + data.RedFlagged + "' data-objecttype='" + data.ObjectType + "' data-objectid='" + data.ObjectID + "' onclick='SetAlertFlag(event)'>" + html + "</a>"
        }

    } catch (e) {
        console.log(e);
    }

    return html;
}

var gearsRenderer = function (type, id) {
    return "<div class='hasTooltip' data-tools data-t='" + type + "' data-i='" + id + "'><i class='fa fa-lg fa-gears'></i></div>";
}

var linkRenderer = function (uri, name) {
    return "<a href='" + uri + "'>" + name + "</a>";
}

var previewIconLink = function (type, id, uri) {
    return "<a data-context='Preview' data-type='" + type + "' data-id='" + id + "' href='" + uri + "'><i class='fa fa-info'></i></a>";
}

var previewIconRenderer = function (type, id, uri) {
    return "<div class='RowTools'>" + previewIconLink(type, id, uri) + "</div>";
}

var previewLinkRenderer = function (type, id, uri, name) {
    return "<div style='padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 7px;'><a data-context='Preview' data-type='" + type + "' data-id='" + id + "' href='" + uri + "'>" + name + "</a></div>";
}

var textrenderer = function (value) {
    var html = "<span style='margin: 4px; float: left;'>" + value + "</span>";
    return html;
}

var currentScoreRenderer = function (value, data) {
    if (value != '') {
        var className = "Score";
        if (value <= .60) {
            className += ' HighPriority';
        }
        else if (value > .60 && value < .85) {
            className += ' MediumPriority';
        }
        else {
            className += ' LowPriority';
        }
        if (value >= 0) {
            value = Math.round(value * 100) + '%';
        }
        html = "<div align='center' style='height:100%'><div valign='middle' class='" +
            className + "'><a href='#' title='" + data.ObjectTypeName + "' data-context='Statistics' data-type='" +
            data.ObjectType + "' data-id='" + data.ObjectID + "'>" +
            value + "</a></div></div>";
    }
    else {
        html = "<div align='center' style='height:100%'>N/A</div>";
    }

    return html;
}

var renderToolsHtml = function (value, tools, defaultContext, data) {
    return renderToolsHtml(value, tools, defaultContext);
};

var renderMinimalPagingHtml = function (grid) {
    try {
        var datainfo = grid.jqxGrid('getdatainformation');
        var element = $("<div style='margin-left: 10px; margin-top: 5px; width: 100%; height: 100%;'></div>");
        if (datainfo) {
            var paginginfo = datainfo.paginginformation;
            var leftButton = $("<div style='padding: 0px; float: left;'><div style='margin-left: 9px; width: 16px; height: 16px;'></div></div>");
            leftButton.find('div').addClass('fa').addClass('fa-arrow-left');
            leftButton.width(36);
            leftButton.jqxButton();
            var rightButton = $("<div style='padding: 0px; margin: 0px 3px; float: left;'><div style='margin-left: 9px; width: 16px; height: 16px;'></div></div>");
            rightButton.find('div').addClass('fa').addClass('fa-arrow-right');
            rightButton.width(36);
            rightButton.jqxButton();
            leftButton.appendTo(element);
            rightButton.appendTo(element);
            var label = $("<div style='font-size: 11px; margin: 2px 3px; font-weight: bold; float: left;'></div>");
            label.text("1-" + paginginfo.pagesize + ' of ' + datainfo.rowscount);
            label.appendTo(element);
            self.label = label;
            // update buttons states.
            var handleStates = function (event, button, className, add) {
                button.bind(event, function () {
                    if (add == true) {
                        button.find('div').addClass(className);
                    }
                    else button.find('div').removeClass(className);
                });
            }

            rightButton.click(function () {
                grid.jqxGrid('gotonextpage');
            });
            leftButton.click(function () {
                grid.jqxGrid('gotoprevpage');
            });
        }

        grid.on('pagechanged', function () {
            var datainfo = grid.jqxGrid('getdatainformation');
            var paginginfo = datainfo.paginginformation;
            self.label.text(1 + paginginfo.pagenum * paginginfo.pagesize + "-" + Math.min(datainfo.rowscount, (paginginfo.pagenum + 1) * paginginfo.pagesize) + ' of ' + datainfo.rowscount);
        });

        grid.on("filter", function (event) {
            var datainfo = grid.jqxGrid('getdatainformation');
            var paginginfo = datainfo.paginginformation;
            self.label.text(1 + paginginfo.pagenum * paginginfo.pagesize + "-" + Math.min(datainfo.rowscount, (paginginfo.pagenum + 1) * paginginfo.pagesize) + ' of ' + datainfo.rowscount);
        });


        return element;
    } catch (e) {
        return null;
    }
};

var renderToolsHtml = function (value, tools, defaultContext) {

    var html = "<div class='RowTools'>";

    if (tools.length > 0) {

        $(tools).each(function (i, item) {

            var context = defaultContext;
            if (item.context) {
                context = item.context;
            }

            if (item.iconBackColor && item.iconForeColor && item.iconText) {
                html += "<div class='row-icon'";

                if (item.method) {
                    html += "data-method = '" + item.method + "' ";
                }

                if (item.title) {
                    html += " data-title = '" + item.title + "' ";
                }
                html += " data-type='" + item.type + "' data-context='" + context + "'";
                if (item.id) {
                    html += " data-id='" + item.id + "'";
                }
                else {
                    html += " data-id='" + value + "'";
                }
                html += " style='background-color: " + item.iconBackColor + " !important; color: " + item.iconForeColor + " !important'><i class='fa fa-" + item.iconText + "'>&#160;</i>";
                html += "</div>";
            }
            else {
                html += "<a ";

                if (item.method) {
                    html += "data-method = '" + item.method + "' ";
                }

                if (item.title) {
                    html += "title='" + item.title + "' data-title = '" + item.title + "' ";
                }

                var url = item.urlprefix;
                if (url.indexOf("{0}") > -1) {
                    url = url.replace("{0}", value);
                }

                if (item.disabled) {
                    html += " class='IconDisabled' href='#' "
                }
                else {
                    if (item.isitemlink) {
                        if (item.context && item.type) {
                            if (item.id) {
                                html += "data-type='" + item.type + "' data-context='" + context + "' data-id='" + item.id + "' ";
                            }
                            else {
                                html += "data-type='" + item.type + "' data-context='" + context + "' data-id='" + value + "' ";
                            }
                        }
                        html += "href='" + url;
                    }
                    else {
                        if (url != '#') {
                            html += "onclick='ClickGridTool(event)' data-context='" + context + "' data-uri='" + url;
                        }
                        else {
                            html += "data-context='" + context + "' data-uri='" + url;
                        }
                    }
                }

                html += "'>"

                if (item.icon) {
                    html += "<i class='fa fa-" + item.icon + "'></i>";
                }
                else {
                    html += "<i class='fa fa-info'></i>";
                }

                if (item.text) {
                    html += "<span>" + item.text + "</span>";
                }

                html += "</a>";
            }
        });
    }
    //else {
    //    html += value;
    //}

    html += "</div>";

    return html;
};

//#endregion

amplify.subscribe("OverlayRemoved", function () {
    try {
        var overlay = $('#Overlay');
        var overlaybg = $('#OverlayBackground');
        overlay.html('');
        overlay.fadeOut(250);
        overlaybg.fadeOut(500);
        overlay.remove();
        overlaybg.remove();
        delete overlay;
        delete overlaybg;
    } catch (e) {
        logError("Common.js : OverlayRemoved", e);
    }
});

amplify.subscribe("RelationOverlayRemoved", function () {
    try {
        var relationOverlay = $('#RelationOverlay');
        relationOverlay.html('');
        relationOverlay.fadeOut(250);
        relationOverlay.remove();
        delete relationOverlay;
    } catch (e) {
        logError("Common.js : RelationOverlayRemoved", e);
    }
});

var removeOverlay = function () {
    amplify.publish(AmplifyActions.OverlayUnsubscribe, {});
    amplify.publish("OverlayRemoved");
};

var removeRelationOverlay = function () {
    amplify.publish(AmplifyActions.OverlayUnsubscribe, {});
    amplify.publish("RelationOverlayRemoved");
};

function openTileOverlay(uri) {
    var overlaybg = $('<div id="OverlayBackground" style="display:none"></div>');
    var overlay = $('<div id="Overlay" style="padding-top: 50px; display:none;"></div>');
    var ocenter = $('<div style="margin: auto; padding-bottom: 30px; position: relative;"></div>');
    var oclose = $('<button class="overlay-close"><i class="fa fa-times fa-4x"></i></button>');
    ocenter.load(uri);

    oclose.on('click', function () {
        removeOverlay();
    });

    overlay.append(oclose);
    overlay.append(ocenter);

    $('body').append(overlaybg);
    $('body').append(overlay);
    overlaybg.fadeIn(750);
    overlay.fadeIn(750);
}

//#region Page Style Methods
function setStyleSettings() {
    $('#page').height = $(document).height();
    stretch();
    //$('.sparkpiechart').sparkline('html', { type: 'pie', width: '15px', height: '15px' });
    //$('.tooltipLink').tooltip({ tip: "#QualityExceptionTip" });
}

function stretch() {
    if ($(window).height() > $('body').height()) {
        $('#page').height($(window).height() - ($('body').height() - $('#page').height()));
    }
}
//#endregion

function SetAlertFlag(event) {
    /// <signature>
    /// <summary>Used during red flagging.</summary>
    /// <param name="event" type="object">The click event containing the object.</param>
    /// <returns type="null"></returns>
    /// </signature>
    var elem = $(event.currentTarget);
    elem.qtip({
        content: {
            title: elem.data('active') ? 'Action: Notes' : 'Action: Notes',
            // Set the text to an image HTML string with the correct src URL to the loading image you want to use
            text: 'Setting notes...<i class="fa fa-spinner fa-spin"></i>',
            ajax: {
                url: "/form/" + elem.data('objecttype') + "/" + elem.data('objectid') + "/redflag"
            }
        },
        position: {
            at: 'bottom center', // Position the tooltip above the link
            my: 'top center',
            viewport: $(window), // Keep the tooltip on-screen at all times
            effect: false // Disable positioning animation
        },
        overwrite: false,
        show: {
            event: event.type,  // show using same event as above.
            solo: true,         // Only show one tooltip at a time
            ready: true
        },
        hide: {
            fixed: true,
            delay: 500,
        },
        //hide: 'mouseout',
        style: {
            classes: 'qtip-light qtip-rounded'
        }
        //addTooltip(this);
    });

    //amplify.publish("AlertAction", { uri: elem.data("uri"), context: elem.data("context"), requiresid: elem.data("requiresid"), tabindex: elem.data("tabindex"), customdata: elem.data() })
}

function setTooltips() {
}

function isRightClick(event) {
    var rightclick;
    if (!event) var event = window.event;
    if (event.which) rightclick = (event.which == 3);
    else if (event.button) rightclick = (event.button == 2);
    return rightclick;
}

function ClickGridTool(event) {
    /// <signature>
    /// <summary>Used by page action toolbar.  Upon tool click, method publishes an amplify ToolAction event.</summary>
    /// <param name="event" type="object">The click event containing the object.</param>
    /// <returns type="null"></returns>
    /// </signature>
    var elem = $(event.currentTarget);
    amplify.publish("ToolAction", { uri: elem.data("uri"), context: elem.data("context"), requiresid: elem.data("requiresid"), tabindex: elem.data("tabindex"), customdata: elem.data()  });
}