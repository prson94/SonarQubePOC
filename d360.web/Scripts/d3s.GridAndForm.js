/// <reference path="../scripts.js" />
(function ($) {

    var methods = {
        init: function (options) {
            var defaults = {
                allowgrouping: false,
                allowpaging: true,
                allowrowselect: true,
                allowrowunselect: true,
                columns: null,
                custompaging: false,
                datauri: null,
                fadespeed: 1000,
                fields: null,
                groups: null,
                idpropertyname: 'ID',
                context: 'form',
                formSuffix: 'Form',
                formuri: null,
                gridSuffix: 'Grid',
                navigateondoubleclick: false,
                navigateondoubleclickurl: '#',
                showfilterrow: true,
                showtools: true,
                src: null,
                tools: null,
                useexternaleditor: false,
                virtualmode: false,
                width: '100%',
                grid: null,
                form: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    Grid = null,
                    Form = null;

                if (!data) {

                    Grid = $('<div class="grid"></div>');
                    Form = $('<div class="form"></div>');

                    if (options.useexternaleditor)
                    {
                        Form.css('display', 'none');
                    }

                    //#region Virtual Mode
                    if (options.virtualmode) {
                        options.src = {
                            datatype: "json",
                            datafields: options.fields,
                            url: options.datauri,
                            beforeprocessing: function (data) {
                                options.src.totalrecords = data.total;
                            },
                            filter: function () {
                                Grid.jqxGrid('updatebounddata');
                            },
                            sort: function () {
                                Grid.jqxGrid('updatebounddata');
                            }
                        };
                    }
                    else {
                        options.src = {
                            datatype: "json",
                            datafields: options.fields,
                            url: options.datauri
                        };
                    }
                    //#endregion

                    var adapter = new $.jqx.dataAdapter(options.src);

                    //#region Show Tools
                    if (options.showtools) {

                        var toolwidth = 35;

                        if (options.tools.length > 0) {
                            toolwidth = options.tools.length * 35;
                        }

                        var toolsrenderer = function (row, column, value) {

                            var html = "<div class='RowTools'>";

                            if (options.tools.length > 0) {

                                $(options.tools).each(function (i, item) {
                                    html += "<a ";

                                    var url = item.urlprefix;
                                    if (url.indexOf("{0}") > -1) {
                                        url = url.replace("{0}", value);
                                    }
                                    else {
                                        url += value;
                                    }

                                    if (item.isitemlink) {
                                        if (item.context && item.type)
                                        {
                                            html += "data-type='" + item.type + "' data-context='" + item.context + "' data-id='" + value + "' ";
                                        }
                                        html += "href='" + url + "'>";
                                        html += "<i class='faicon-info-sign'></i>";
                                    }
                                    else {
                                        html += "onclick='ClickGridTool(event)' data-context='" + options.context + "' data-tabindex='" + item.tab + "' data-uri='" + url + "'>";
                                        html += "<i class='faicon-" + item.icon + "'></i>";
                                    }
                                    html += "</a>";
                                });
                            }
                            else {
                                html += value;
                            }

                            html += "</div>";

                            return html;
                        };

                        options.columns.push({ datafield: options.idpropertyname, width: toolwidth, cellsrenderer: toolsrenderer, cellsalign: 'right', filterable: false, sortable: false });
                    }
                    //#endregion

                    //#region Custom Paging Renderer Definition

                    var pagingrenderer = function () {
                        //var gridID = this.element[0].id + o.gridSuffix;
                        var element = $("<div style='margin-left: 10px; margin-top: 5px; width: 100%; height: 100%;'></div>");
                        var $grid = $(this.element.parentElement[0]).children('.grid')[0];
                        var datainfo = $grid.jqxGrid('getdatainformation');
                        if (datainfo) {
                            var paginginfo = datainfo.paginginformation;
                            var leftButton = $("<div style='padding: 0px; float: left;'><div style='margin-left: 9px; width: 16px; height: 16px;'></div></div>");
                            leftButton.find('div').addClass('icon-arrow-left');
                            leftButton.width(36);
                            leftButton.jqxButton();
                            var rightButton = $("<div style='padding: 0px; margin: 0px 3px; float: left;'><div style='margin-left: 9px; width: 16px; height: 16px;'></div></div>");
                            rightButton.find('div').addClass('icon-arrow-right');
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
                                $(this).jqxGrid('gotonextpage');
                            });
                            leftButton.click(function () {
                                $(this).jqxGrid('gotoprevpage');
                            });
                        }
                        return element;
                    }

                    //#endregion

                    //#region Grid Create

                    Grid.jqxGrid(
                        {
                            width: options.width,
                            theme: theme,
                            altrows: true,
                            groupable: options.allowgrouping,
                            source: adapter,
                            virtualmode: options.virtualmode,
                            rendergridrows: function () {
                                return adapter.records;
                            },
                            columnsresize: true,
                            //pagerrenderer: ((options.custompaging) ? pagingrenderer : null),
                            filterable: true,
                            showfilterrow: options.showfilterrow,
                            pageable: options.allowpaging,
                            sortable: true,
                            autoheight: options.allowpaging,
                            columns: options.columns
                        }
                    );

                    //#endregion

                    //#region Grid Event Handlers

                    if (options.allowrowselect) {
                        Grid.bind('rowselect', function (event) {
                            var row = Grid.jqxGrid('getrowdata', event.args.rowindex);
                            amplify.publish("RowSelectedAction", { context: options.context, id: row.ID, rowdata: row });
                        });
                    }

                    if (options.allowrowunselect) {
                        Grid.bind('rowunselect', function (event) {
                            var row = Grid.jqxGrid('getrowdata', event.args.rowindex);
                            if (row) {
                                amplify.publish("RowUnselectedAction", { context: options.context, id: row.ID, rowdata: row });
                            }
                        });
                    }

                    if (options.navigateondoubleclick) {
                        Grid.bind('rowdoubleclick', function (event) {
                            var row = Grid.jqxGrid('getrowdata', event.args.rowindex);
                            location.assign(options.navigateondoubleclickurl + row.ID);
                        });
                    }

                    //Grid.on("bindingcomplete", function (event) {
                    //    var rows = Grid.jqxGrid('getdisplayrows');
                    //    $(rows).each(function(row, idx) {

                    //    });
                    //    $('div.RowTools >> a[data-type]').each(function () {
                    //        addTooltip(this);
                    //    });
                    //});

                    //#endregion

                    $this.append(Grid);
                    $this.append(Form);

                    $(this).data('GridAndForm', {
                        Target: $this,
                        Grid: Grid,
                        Form: Form,
                        Options: options
                    });

                }
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm');

                data.Grid.remove();
                data.Form.remove();
                $this.removeData('GridAndForm');
            });
        },
        performCancelAction: function (idata) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                if (idata.context == options.context) {
                    form.hide(200);
                    form.html('');
                    grid.fadeIn(options.fadespeed);
                }
            });
        },
        clearSelection: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                grid.jqxGrid('clearselection');
            });
        },
        refreshGrid: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                grid.jqxGrid('updatebounddata');
            });
        },
        resizeGrid: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                //grid.jqxGrid('render');
            });
        },
        performSaveAction: function (idata) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                if (idata.context == options.context) {
                    form.hide(200).html('');
                    grid.jqxGrid('updatebounddata');
                    grid.fadeIn(options.fadespeed);
                }
            });
        },
        performToolAction: function (idata) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('GridAndForm'),
                    form = data.Form,
                    grid = data.Grid,
                    options = data.Options;

                if (idata.context == options.context) {
                    if (options.useexternaleditor) {
                        amplify.publish("EditorAction", { TabIndex: idata.tabindex, context: options.context, uri: idata.uri });
                    }
                    else {
                        grid.hide(1);
                        form.fadeIn(options.fadespeed); //$('#' + formID).show(200);
                        form.html('<img src="/Content/images/ajaxLoader.gif"/>');
                        form.load(idata.uri);
                    }
                }
            });
        }
    };

    $.fn.GridAndForm = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on ds3.GridAndForm');
        }

    };

    //#region Private Methods

    function clear($obj) {
        $obj.html('');
    };

    function loadLevel(node) {
        var html = "";
        html += "<li ";
        html += "id='" + node.Title + "' ";
        //if (node.Title == "Add") html += "id='Add' ";
        html += "data-uri='" + node.Uri + "' data-context='" + node.Context + "' data-tabindex='" + node.TabIndex + "'";
        $.each(node.CustomData, function (idx, c) {
            html += " data-" + c.Name + "='" + c.Value + "'";
        });
        html += ">";
        if (node.Icon != '') {
            html += "<i class='faicon-" + node.Icon + "'></i>";
        }
        html += node.Title;
        if (node.Items.length > 0) {
            html += "<ul>";
            $.each(node.Items, function (idx, c) {
                html += loadLevel(c, html);
            });
            html += "</ul>";
        }
        html += "</li>";
        return html;
    };

    function load($obj, type, id, context) {

        amplify.request("PageActionsRequest", { type: type, id: id, context: context }, function (data) {

            clear($obj);

            if (data) {
                var html = "";
                html += "<div id='tools' style='border: none !important;'>";
                html += "<ul>";
                html += "<li id='TopMenu' item-disabled='true'>";
                html += "<i class='faicon-adjust'></i>Actions";
                html += "<ul>";
                $.each(data, function (idx, c) {
                    html += loadLevel(c); //recurse ULs with child LIs
                });
                html += "</ul>";
                html += "</li>";
                html += "</ul>";
                html += "</div>";

                $obj.html(html);

                $("#tools").jqxMenu({ showTopLevelArrows: false, height: 27 });
                $("#tools").jqxMenu('setItemOpenDirection', 'TopMenu', 'left', 'down');
                $("#tools").jqxMenu('setItemOpenDirection', 'TypeActions', 'left', 'down');
                $("#tools").jqxMenu('setItemOpenDirection', 'Add', 'left', 'down');
                $("#tools").jqxMenu('setItemOpenDirection', 'Reports', 'left', 'down');
                $('#tools').bind('itemclick', function (event) {
                    var li = event.args;
                    amplify.publish("ToolAction", { override: $(li).data("override"), uri: $(li).data("uri"), context: $(li).data("context"), tabindex: $(li).data("tabindex"), customdata: $(li).data() }); //requiresid: $(li).data("requiresid"), 
                });

            }
        });

    };

    //#endregion

})(jQuery);