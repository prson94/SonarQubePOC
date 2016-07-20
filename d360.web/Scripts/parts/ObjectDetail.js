function ObjectDetail(controlID, type, id, useSmallUI) {
    var template = 'DetailTile';
    if (useSmallUI)
        template = 'DetailTileSmall';
    var tmpl = Handlebars.getTemplate(template);

    var processFieldLabel = function (fix, f) {
        f.labelID = controlID + '_' + f.FieldName;
        f.valueID = controlID + '_val_' + f.FieldName;
        if (f.ScriptProperty) {
            f.Name = eval(f.ScriptProperty);
        }
    }

    var processFieldDetails = function (fix, f) {
        var labelID = '#' + f.labelID;
        var valueID = '#' + f.valueID;

        //#region Create tooltips where there are field descriptions
        if (f.FieldDescription && f.FieldDescription != '') {
            $(labelID).qtip({
                content: {
                    text: f.FieldDescription,
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
        //#endregion

        //#region Load field values

        if (f.TooltipContext && f.TooltipID && f.TooltipType && f.TooltipUrl) {
            $(valueID).html("<a href='" + f.TooltipUrl +
                "' data-type='" + f.TooltipType +
                "' data-context='" + f.TooltipContext +
                "' data-id='" + f.TooltipID + "'>" +
                f.Value + "</a>");
        }
        else if (f.LookupGridUrl) {
            $.getJSON(f.LookupGridUrl, function (data) {

                var fields = data.Fields;
                var res = data.Values;
                var cols = data.Columns;

                if (res.length > 0) {
                    var source = {
                        localdata: res,
                        datatype: 'json',
                        datafields: fields
                    };

                    var dataAdapter = new $.jqx.dataAdapter(source);

                    var tooltiprenderer = function (element) {
                        $(element).parent().jqxTooltip({ position: 'mouse', content: v.FieldDescription });
                    }

                    var cn = null;
                    $.each(cols, function () {
                        if (this.datafield == "Name") {
                            cn = this;
                        }
                    });
                    if (cn) {
                        cn.width = "30%";
                        cn.cellsRenderer = function (index, datafield, value, defaultvalue, column, data) {
                            return "<div class='d3s-cell' style='overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 4px;'><a data-context='Preview' data-type='" + data.Object + "' data-id='" + data.ID + "' href='" + data.Url + "'>" + data.Name + "</a></div>";
                        }
                    }

                    var cp = null;
                    $.each(cols, function () {
                        if (this.datafield == "TextPath") {
                            cp = this;
                        }
                    });
                    if (cp) {
                        cp.width = "40%";
                        cp.cellsRenderer = function (index, datafield, value, defaultvalue, column, data) {
                            return "<div class='d3s-cell' style='overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 4px;'><a data-context='Preview' data-type='" + data.Object + "' data-id='" + data.ID + "' href='" + data.Url + "'>" + data.TextPath + "</a></div>";
                        }
                    }

                    //function relationGridUnsubscribe() {
                    //    $(valueID).off('bindingcomplete', relationGridBindComplete);
                    //    amplify.unsubscribe(AmplifyActions.Unsubscribe, relationGridUnsubscribe);
                    //    amplify.unsubscribe(AmplifyActions.TileUnsubscribe, relationGridUnsubscribe);
                    //}

                    //function relationGridBindComplete() {
                    //    console.log('auto-resized');
                    //    $(valueID).jqxGrid('autoresizecolumns');
                    //}

                    $(valueID).jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 10,
                        showemptyrow: false,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: false,
                        showheader: !f.HideHeader,
                        pageable: !f.HideFooter,
                        columnsresize: true,
                        autorowheight: true,
                        source: dataAdapter,
                        theme: 'flat',
                        pagermode: 'simple',
                        columns: cols,
                        ready: function () {
                            if (cols.length > 3)
                                $(valueID).jqxGrid('autoresizecolumns');
                        }
                    });

                    //$(valueID).on('bindingcomplete', relationGridBindComplete);
                    //amplify.subscribe(AmplifyActions.Unsubscribe, relationGridUnsubscribe);
                    //amplify.subscribe(AmplifyActions.TileUnsubscribe, relationGridUnsubscribe);
                }
                else {
                    //$(labelID).hide();
                    $(valueID).closest('div[data-category]').hide();
                    //$('#' + 'Row' + fix).hide();
                }

            });
        }
        else {
            if (f.Value != null && f.Value.match(/(\d{4})-(\d{2})-(\d{2})T(\d{2})\:(\d{2})\:(\d{2})/)) {
                f.Value = f.Value.replace(/["]/g, "");
                var d = new Date(f.Value);
                $(valueID).html(d.toLocaleString());
            }
            else if(f.FieldName == 'ResourceEmail')
            {                
                $(valueID).html(
                        $("<a>").attr("href", "mailto:" + f.Value).text(f.Value)
                );
            }
            else
                $(valueID).html(f.Value);
        }

        //#endregion
    }

    $.getJSON('/api/' + type + '/' + id + '/detail', function (model) {

        model.control = controlID;

        //#region Update friendly names where there are script code items
        $.each(model.rows, function (rix, r) {
            r.hasOneColumn = (r.columns == 1);
            $.each(r.FirstColumnFields, processFieldLabel);
            $.each(r.SecondColumnFields, processFieldLabel);
        });
        //#endregion

        $('#' + controlID).html(tmpl(model));

        var categories = [];

        $.each(model.rows, function (rix, r) {
            $.each(r.FirstColumnFields, processFieldDetails);
            $.each(r.SecondColumnFields, processFieldDetails);
            if (r.Category) {
                if (categories.indexOf(r.Category) == -1) {
                    categories.push(r.Category);
                }
            }
        });

        if (categories.length > 0) {
            categories.sort();

            $.each(categories, function (cix, c) {

                var catID = controlID + '_cat' + cix;
                var catContentID = controlID + '_cat' + cix + '_content';
                $('#' + controlID).append('<div id="' + catID + '"><div>' + c + '</div><div style="padding-top: 10px" id="' + catContentID + '"></div></div>')

                $('#' + catID).css('margin', '10px');
                $('#' + catID).jqxExpander({ theme: theme, expanded: false });

                $('#' + controlID + ' .row').each(function (rix, r) {
                    if ($(r).data('category') === c) {
                        $(r).appendTo('#' + catContentID);
                    }
                });
            });
        }

    });
}