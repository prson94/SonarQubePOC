(function ($) {

    //#region

    var CanEdit = false;
    var CanDelete = false;

    var TypeSource = {
        dataType: "json",
        url: null,
        dataFields: [
            { name: 'Object', type: 'string' },
            { name: 'ObjectID', type: 'number' },
            { name: 'Name', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    }

    var TypeAdapter = new $.jqx.dataAdapter(TypeSource);

    var ItemSource = {
        dataType: "json",
        url: null,
        dataFields: []
    };

    var ItemAdapter = new $.jqx.dataAdapter(ItemSource);

    var TitleControl = $('<header></header>');
    var TypeControl = $('<div></div>');
    var ItemControl = $('<div></div>');
    //var toolbarControl = $('<div></div>');

    //#endregion

    var methods = {
        init: function (options) {
            var defaults = {
                obj: '',
                objid: null,
                title: 'Relationships',
                canEdit: false,
                canDelete: false
            };

            // Extend default with any options that were provided.
            options = $.extend(defaults, options);

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('RelationshipsTile');

                if (!data) {

                    CanEdit = options.canEdit;
                    CanDelete = options.canDelete;

                    //if (options.objid) {
                        loadUI($this, options);
                    //}

                    $(this).data('MapItems', {
                        Target: $this,
                        Options: options
                    });
                }
            });
        },
        reload: function (obj, objid, canEdit, canDelete) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('RelationshipsTile'),
                    options = data.Options;

                CanEdit = canEdit;
                CanDelete = canDelete;

                options.canEdit = canEdit;
                options.canDelete = canDelete;
                options.obj = obj;
                options.objid = objid;

                ItemSource.url = '/api/maps/' + options.objid +'/mapitems';
                ItemControl.jqxGrid('updatebounddata');
                //loadToolbar(options.object, options.objectID);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('RelationshipsTile');

                //amplify.unsubscribe("CancelAction", cancelAction);
                //amplify.unsubscribe("SaveAction", saveAction);
                TypeControl.jqxDataTable('destroy');
                ItemControl.jqxGrid('destroy');
                $this.removeData('RelationshipsTile');
                $this.html('');
            });
        }
    };

    $.fn.RelationshipsTile = function (method) {
        //#region Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.RelationshipsTile');
        }
        //#endregion
    };

    function typeRowSelect(event) {
        // event args.
        var args = event.args;
        // row data.
        var row = args.row;
        // row index.
        var index = args.index;
        // row's data bound index.
        var boundIndex = args.boundIndex;
        // row key.
        var key = args.key;

        var uri = '/api/' + options.obj + '/' + options.objid + '/relationships/' + row.Object + '/' + row.ObjectID;


        $.getJSON(uri + '/fields', function (fieldData) {
            ItemSource.dataFields = fieldData.Fields;
            ItemControl.jqxGrid({ columns: fieldData.Columns });
            ItemControl.jqxGrid('updatebounddata');
        }).then(function () {
            ItemSource.url = uri;
        });
    }
    

    function itemBindingComplete(event) {
        var count = 0;
        try {
            count = ItemControl.jqxGrid('getrows').length;
        } catch (e) {
            count = 0;
        }
    }


    //function saveAction(data) {
    //    try {
    //        switch (data.context) {
    //            case contextList.Synonym:
    //                ItemControl.jqxGrid('updatebounddata');
    //                break;
    //        }
    //    } catch (e) {
    //        logError("MapItems.js : SaveAction", e);
    //    }
    //}

    //var loadToolbar = function (type, objid) {
    //    if (CanEdit) {
    //        toolbarControl[0].objid = "Tools" + type + objid;
    //        TileTools('#' + toolbarControl[0].objid, [
    //            { icon: 'plus', uri: '/form/AddSynonym?type=' + type + '&objid=' + objid, context: contextList.Synonym, title: 'Add synonym' }
    //        ]);
    //    }
    //}

    function loadUI($obj, options) {
        try {
            var $this = $obj;

            if (options.obj && options.objid) {
                //#region

                $this.html('');

                TypeControl.html('');
                //toolbarControl.html('');
                ItemControl.html('');

                $this.css('margin', '10px');

                //$this.append(headerControl);

                //contentControl.append('<header style="width: 98%; margin-top: 10px"></header>');
                //contentControl.find('header').append(toolbarControl);
                TitleControl.text(options.title);
                $this.append(TitleControl);
                $this.append(TypeControl);
                $this.append(ItemControl);


                //loadToolbar(options.object, options.objectID);

                TypeSource.url = '/api/' + options.obj + '/' + options.objid + '/relationships/counts';

                //ItemControl.on('bindingcomplete', itemBindingComplete);
                //amplify.subscribe("SaveAction", saveAction);

                //#endregion
            }
            //else {
            //    $this.html('');
            //}

            TypeControl.on('rowSelect', typeRowSelect);

            TypeControl.jqxDataTable({
                source: TypeAdapter,
                pageable: false,
                filterable: false,
                theme: 'flat',
                showHeader: false,
                columns: [
                    {
                        text: 'Name', 
                        dataField: 'Name',
                        cellsRenderer: function (row, column, value, rowData) {
                            return '<span style="color: #51a6dc; font-weight: 600; font-size: 140%">' + rowData.Name + '</span>';
                        }
                    },
                    {
                        text: 'Count',
                        dataField: 'Count',
                        width: '50px',
                        cellsRenderer: function (row, column, value, rowData) {
                            return '<div style="border-radius: 3px; text-align: center; vertical-align: middle; font-weight: 600; background-color: #51a6dc; color: #ffffff; font-size: 140%; line-height: 140%">' + rowData.Count + '</b>';
                        }
                    }
                ]
            });

            ItemControl.jqxGrid({
                source: ItemAdapter,
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
                columns: null
            });
        } catch (e) {
            logError("RelationshipsTile.js : loadUI", e);
        }
    }

})(jQuery);