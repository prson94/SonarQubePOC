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

            if (f.Value == '' || !f.Value) {
                $(valueID).closest('div[data-category]').data("hidden", true);
                $(valueID).closest('div[data-category]').hide();
            }
            else {
                $(valueID).closest('div[data-category]').data("hidden", false);
            }
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
                }
                else {
                    $(valueID).closest('div[data-category]').hide();

                    amplify.publish("DetailLazyDataLoaded", { fieldID: valueID, hidden: true });
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

            if (f.Value == '' || !f.Value) {
                $(valueID).closest('div[data-category]').data("hidden", true);
                $(valueID).closest('div[data-category]').hide();
            }
            else {
                $(valueID).closest('div[data-category]').data("hidden", false);
            }
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
                $('#' + catID).data("count", 0);
                $('#' + catID).data("hidecount", 0);
                $('#' + catID).data("categoryname", c);

                $('#' + controlID + ' .row').each(function (rix, r) {
                    if ($(r).data('category') === c && !$(r).data('hidden')) {
                        $(r).appendTo('#' + catContentID);
                        $('#' + catID).data("count", $('#' + catID).data("count")+1);
                    }
                });
            });
        }

    });

    amplify.subscribe("DetailLazyDataLoaded", function (fieldData) {
        if (fieldData.hidden) {
            var c = $(fieldData.fieldID).closest('div[data-category]').data("category");
            var panel = $(fieldData.fieldID).closest('div[data-category]').parent().parent(); //$(fieldData.fieldID).closest("div[data-categoryname='" + c + "']");
            if (panel) {
                panel.data("hidecount", panel.data("hidecount") + 1);
                var hiddenFieldCount = panel.data("hidecount");
                var fieldCount = panel.data("count");
                if (hiddenFieldCount >= fieldCount) {
                    panel.fadeOut();
                }
            }
        }
    });
}