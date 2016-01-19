/// <reference path="../scripts.js" />
(function ($) {

    amplify.request.define("ObjectDetailRequest", "ajax", { url: '/api/{type}/{id}/detail', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                context: 'form',
                prefix: 'Default',
                type: null,
                id: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Detail'),
                    Detail = null;

                $this.addClass("form");

                if (!data) {

                    if (options.type && options.id) {
                        Detail = loadFields($this, options);
                    }

                    $(this).data('Detail', {
                        Target: $this,
                        Detail: Detail,
                        Options: options
                    });

                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },

        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Detail');
                $this.removeData('Detail');
            });
        },

        clear: function () {
            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Detail');

                var options = data.Options;
                options.type = null;
                options.id = null;

                clear($this);

                $(this).data('Detail', {
                    Target: $this,
                    //Detail: Detail,
                    Options: options
                });
            });
        },

        reloadcontext: function (context, type, id) {
            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Detail');

                var options = data.Options;
                options.context = context;
                options.type = type;
                options.id = id;

                if (options.context && options.type && options.id) {
                    loadFields($this, options);
                }

                $(this).data('Detail', {
                    Target: $this,
                    //Detail: Detail,
                    Options: options
                });
            });
        },

        reload: function (type, id) {
            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Detail');

                var options = data.Options;
                options.type = type;
                options.id = id;

                if (options.type && options.id) {
                    loadFields($this, options);
                }

                $(this).data('Detail', {
                    Target: $this,
                    //Detail: Detail,
                    Options: options
                });
            });
        },

        refresh: function (idata) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Detail'),
                    options = data.Options;

                if (idata.context == options.context) {
                    if (options.type && options.id && idata.action != 'delete') {
                        reload($this);
                    }
                    else {
                        clear($this);
                    }
                }
            });
        }

    };

    $.fn.Detail = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.Detail');
        }

    };

    //#region Private Methods

    function reload($obj) {
        var data = $obj.data('Detail');

        var options = data.Options;

        if (options.type && options.id) {
            loadFields($obj, options);
        }

        $(this).data('Detail', {
            Target: $obj,
            //Detail: Detail,
            Options: options
        });
    }

    function clear($obj) {
        $obj.html('<div class="EmptyMessage"></div>');
    };

    function loadFields($obj, options) {
        amplify.request("ObjectDetailRequest", { type: options.type, id: options.id }, function (data) {
            $obj.html('');
            //if (data.RedFlagged) {
            //    $obj.append("<div class='detail-alert'><i class='fa fa-flag' /></div>");
            //}
            parseFields($obj, options, data[0].Fields);
        });
    };

    function parseFields($obj, options, fields) {
        try {
            var controlID = $obj.attr('id');

            if (fields) {
                //$obj.append("<input type='hidden' name='_context' id='_context' value='" + options.context + "' />");

                //#region Build the form layout.

                var tableMatrix = [];
                var currentRow = 0;
                var tabMatrixItem = null;
                $.each(fields, function (idx, v) {
                    if (v.Row) {
                        if (v.Row != currentRow) {
                            if (tabMatrixItem) tableMatrix.push(tabMatrixItem);
                            currentRow = v.Row;
                            tabMatrixItem = { Row: currentRow, Columns: 0, ColumnCount: 0 };
                        }
                        if (v.Column) {
                            if (tabMatrixItem.ColumnCount < v.Column) {
                                tabMatrixItem.ColumnCount = v.Column;
                                tabMatrixItem.Columns = Math.round(12 / v.Column);
                            }
                        }
                    }
                });
                if (tabMatrixItem) tableMatrix.push(tabMatrixItem);   //Add the last item to make sure we get the last row.

                var currentColumn = 0;
                var layoutHtml = "";
                $.each(tableMatrix, function (i, m) {
                    layoutHtml += "<div class='row'>";

                    currentColumn = 1;
                    while (currentColumn <= m.ColumnCount) {
                        layoutHtml += "<div id='det" + controlID + options.prefix + options.type + options.id + "col_" + m.Row + "_" + currentColumn + "' class='col l" + m.Columns + " m" + m.Columns + "'></div>";
                        currentColumn++;
                    }

                    layoutHtml += "</div>";
                });
                $obj.append(layoutHtml);

                //#endregion

                $.each(fields, function (idx, v) {
                    var cpnl = $('#det' + controlID + options.prefix + options.type + options.id + 'col_' + v.Row + '_' + v.Column);

                    var fieldFriendlyName = v.Name;
                    if (v.ScriptProperty) {
                        fieldFriendlyName = eval(v.ScriptProperty);
                    }

                    if (!v.MultipleValues) {
                        if (v.FieldDescription && v.FieldDescription != '') {
                            cpnl.append("<div id='" + controlID + v.FieldName + "' class='FieldName FieldDisplayName'><span id='Tip_" + controlID + v.FieldName + "'>" + fieldFriendlyName + "</span></div>");
                            $('#Tip_' + controlID + v.FieldName).qtip({
                                content: {
                                    text: v.FieldDescription,
                                    position: {
                                        at: 'bottom center', // Position the tooltip above the link
                                        my: 'top center',
                                        viewport: $(window), // Keep the tooltip on-screen at all times
                                        effect: false // Disable positioning animation
                                    }
                                },
                                style: {
                                    classes: 'qtip-blue qtip-rounded'
                                }
                            });
                        }
                        else {
                            cpnl.append("<div class='FieldName FieldDisplayName'>" + fieldFriendlyName + "</div>");
                        }
                    }

                    if (v.TooltipContext && v.TooltipID && v.TooltipType && v.TooltipUrl) {
                        cpnl.append("<div class='FieldContent'><a href='" + v.TooltipUrl +
                            "' data-type='" + v.TooltipType +
                            "' data-context='" + v.TooltipContext +
                            "' data-id='" + v.TooltipID + "'>" +
                            v.Value + "</div>");
                    }
                    else if(v.MultipleValues)
                    {                        
                        cpnl.append("<div id='" + controlID + v.FieldName + "'></div>");
                        var data = new Array();
                        var i = 0;
                        v.MultipleValues.forEach(function (val) {
                            var row = {};
                            row["value"] = val;
                            data[i++] = row;
                        })

                        var source =
                        {
                            localdata: data,
                            datatype: "array"
                        };

                        var dataAdapter = new $.jqx.dataAdapter(source, {
                            downloadComplete: function (data, status, xhr) { },
                            loadComplete: function (data) { },
                            loadError: function (xhr, status, error) { }
                        });

                        var tooltiprenderer = function (element) {
                            $(element).parent().jqxTooltip({ position: 'mouse', content: v.FieldDescription });
                        }
                                                
                        $("#" + controlID + v.FieldName).jqxGrid({
                            altrows: true,
                            width: grid_width,
                            pagesizeoptions: ['10', '20', '50'],
                            pagesize: 20,
                            autoheight: true,
                            sortable: true,
                            filterable: true,
                            showfilterrow: true,
                            pageable: true,
                            source: dataAdapter,
                            theme: list_theme,
                            pagermode: 'simple',
                            columns: [
                                { datafield: "value", text: fieldFriendlyName, rendered: tooltiprenderer }
                            ]                           
                        });
                    }
                    else {                        
                        if (v.Value != null && v.Value.match(/(\d{4})-(\d{2})-(\d{2})T(\d{2})\:(\d{2})\:(\d{2})/)) 
                        {
                            v.Value = v.Value.replace(/["]/g, "");
                            var d = new Date(v.Value);
                            cpnl.append("<div class='FieldContent'>" + d.toLocaleString() + "</div>");
                        }
                        else
                            cpnl.append("<div class='FieldContent'>" + v.Value + "</div>");
                    }                    
                });
            }
        } catch (e) {
            logError("Detail.js : parseFields", e);
        }
    }

    //#endregion

})(jQuery);