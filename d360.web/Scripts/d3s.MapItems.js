(function ($) {

    //#region

    var CanEdit = false;
    var CanDelete = false;

    var GridSource = {
        dataType: "json",
        url: null,
        dataFields: [
            { name: 'MapID' },
            { name: 'SourceID' },
            { name: 'TargetID' },
            { name: 'SourceIntersectID' },
            { name: 'SourceSubjectName' },
            { name: 'SourceSubjectIconHtml' },
            { name: 'SourceSubject' },
            { name: 'SourceSubjectID' },
            { name: 'SourceSubjectUrl' },
            { name: 'SourceObjectName' },
            { name: 'SourceObjectIconHtml' },
            { name: 'SourceObject' },
            { name: 'SourceObjectID' },
            { name: 'SourceObjectUrl' },
            { name: 'TargetIntersectID' },
            { name: 'TargetSubjectName' },
            { name: 'TargetSubjectIconHtml' },
            { name: 'TargetSubject' },
            { name: 'TargetSubjectUrl' },
            { name: 'TargetSubjectID' },
            { name: 'TargetObjectName' },
            { name: 'TargetObjectIconHtml' },
            { name: 'TargetObject' },
            { name: 'TargetObjectUrl' },
            { name: 'TargetObjectID' },
            { name: 'Transformation' }
        ]
    };

    var GridAdapter = new $.jqx.dataAdapter(GridSource);

    //var headerControl = $('<div>Map Items</div>');
    //var countControl = $('<span></span>');
    var contentControl = $('<div></div>');
    var gridControl = $('<div></div>');
    //var toolbarControl = $('<div></div>');

    //#endregion

    var methods = {
        init: function (options) {
            var defaults = {
                id: null,
                title: 'Map Items',
                canEdit: false,
                canDelete: false
            };

            // Extend default with any options that were provided.
            options = $.extend(defaults, options);

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('MapItems');

                if (!data) {

                    CanEdit = options.canEdit;
                    CanDelete = options.canDelete;

                    //if (options.id) {
                        loadUI($this, options);
                    //}

                    $(this).data('MapItems', {
                        Target: $this,
                        Options: options
                    });
                }
            });
        },
        reload: function (id, canEdit, canDelete) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('MapItems'),
                    options = data.Options;

                CanEdit = canEdit;
                CanDelete = canDelete;

                options.canEdit = canEdit;
                options.canDelete = canDelete;
                options.id = id;

                GridSource.url = '/api/maps/' + options.id +'/mapitems';
                gridControl.jqxGrid('updatebounddata');
                //loadToolbar(options.object, options.objectID);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('MapItems');

                //amplify.unsubscribe("CancelAction", cancelAction);
                //amplify.unsubscribe("SaveAction", saveAction);
                gridControl.jqxGrid('destroy');
                $this.removeData('MapItems');
                $this.html('');
            });
        }
    };

    $.fn.MapItems = function (method) {
        //#region Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.MapItems');
        }
        //#endregion
    };

    function bindingComplete(event) {
        var count = 0;
        try {
            count = gridControl.jqxGrid('getrows').length;
        } catch (e) {
            count = 0;
        }
        //countControl.html(" (" + count + ")");
    }

    //function saveAction(data) {
    //    try {
    //        switch (data.context) {
    //            case contextList.Synonym:
    //                gridControl.jqxGrid('updatebounddata');
    //                break;
    //        }
    //    } catch (e) {
    //        logError("MapItems.js : SaveAction", e);
    //    }
    //}

    //var loadToolbar = function (type, id) {
    //    if (CanEdit) {
    //        toolbarControl[0].id = "Tools" + type + id;
    //        TileTools('#' + toolbarControl[0].id, [
    //            { icon: 'plus', uri: '/form/AddSynonym?type=' + type + '&id=' + id, context: contextList.Synonym, title: 'Add synonym' }
    //        ]);
    //    }
    //}

    function loadUI($obj, options) {
        try {
            var $this = $obj;

            if (options.id) {
                //#region

                $this.html('');

                contentControl.html('');
                //toolbarControl.html('');
                gridControl.html('');

                $this.css('margin', '10px');

                //headerControl.append(countControl);
                //$this.append(headerControl);

                //contentControl.append('<header style="width: 98%; margin-top: 10px"></header>');
                //contentControl.find('header').append(toolbarControl);
                contentControl.append(gridControl);

                $this.append(contentControl);

                //loadToolbar(options.object, options.objectID);

                //#region Grid Logic

                GridSource.url = '/api/maps/' + options.id + '/mapitems';



                gridControl.on('bindingcomplete', bindingComplete);
                //amplify.subscribe("SaveAction", saveAction);

                //#endregion

                //#endregion
            }
            //else {
            //    $this.html('');
            //}

            gridControl.jqxGrid({
                source: GridAdapter,
                width: overlay_grid_width,
                pagesizeoptions: ['5', '10', '20'],
                pagesize: 5,
                autoheight: true,
                autorowheight: true,
                sortable: true,
                altrows: true,
                showfilterrow: false,
                filterable: true,
                pageable: false,
                theme: 'flat',
                autoshowloadelement: false,
                selectionmode: 'none',
                columngroups: [
                    { text: 'Source', align: 'center', name: 'S' },
                    { text: 'Target', align: 'center', name: 'T' }
                ],
                columns: [
                    { datafield: "SourceSubjectIconHtml", text: "", columngroup: "S", width: "30px" },
                    {
                        datafield: "SourceSubjectName", text: "Subject", columngroup: "S", cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer(data.SourceSubject, data.SourceSubjectID, data.SourceSubjectUrl, data.SourceSubjectName);
                        }
                    },
                    { datafield: "SourceObjectIconHtml", text: "", columngroup: "S", width: "30px" },
                    {
                        datafield: "SourceObjectName", text: "Object", columngroup: "S", cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer(data.SourceObject, data.SourceObjectID, data.SourceObjectUrl, data.SourceObjectName);
                        }
                    },
                    { datafield: "TargetSubjectIconHtml", text: "", columngroup: "T", width: "30px" },
                    {
                        datafield: "TargetSubjectName", text: "Subject", columngroup: "T", cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer(data.TargetSubject, data.TargetSubjectID, data.TargetSubjectUrl, data.TargetSubjectName);
                        }
                    },
                    { datafield: "TargetObjectIconHtml", text: "", columngroup: "T", width: "30px" },
                    {
                        datafield: "TargetObjectName", text: "Object", columngroup: "T", cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer(data.TargetObject, data.TargetObjectID, data.TargetObjectUrl, data.TargetObjectName);
                        }
                    }//,
                    //{ datafield: "Description", text: "Description" },
                    //{
                    //    datafield: "IntersectID",
                    //    text: "",
                    //    sortable: false,
                    //    filterable: false,
                    //    sortable: false,
                    //    width: '80px',
                    //    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    //        var tools = [];

                    //        tools.push({ isitemlink: true, urlprefix: data.Url, type: data.Object, id: data.ObjectID, context: 'Preview' });
                    //        if (CanDelete) {
                    //            tools.push({ icon: 'trash-o', urlprefix: 'form/DeleteSynonym?type=' + data.Object + '&id=' + data.ObjectID + '&intersectMapID=' + data.IntersectMapID });
                    //        }
                    //        return renderToolsHtml(value, tools, contextList.Synonym, data);
                    //    }
                    //}
                ]
            });

            //if ($this.html() == '') {
            //    $this.hide();
            //}
        } catch (e) {
            logError("MapItems.js : loadUI", e);
        }
    }

})(jQuery);