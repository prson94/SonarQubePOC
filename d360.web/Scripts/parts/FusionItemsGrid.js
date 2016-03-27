function _FusionItemsGrid(controlID, fusionTypeID, fusionID, defaultTypeDefinition, definition, dataUri, id) {
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;
    $(controlID).html('<div id="' + gridControlID + '"></div>');
    gridControlID = '#' + gridControlID;

    $('#FusionAttributeDetailsTile').hide();

    //#region Event Handlers

    var doubleClick = function (event) {
        var args = event.args;
        var boundIndex = args.rowindex;     // row's bound index.
        var data = $(gridControlID).jqxGrid('getrowdata', boundIndex);

        var dataUri = '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID;

        if (data.Type == 'FusionAttribute') {
            // The next level will be the attribute types under this parent attribute.
            dataUri += "&parentType=FusionAttributeType" +
                "&parentID=" + data.FusionAttributeTypeID +
                "&parentFusionAttributeID=" + data.ID;

            amplify.publish('FusionItemSelected', {
                definition: defaultTypeDefinition,
                uri: dataUri,
                title: data.Name,
                type: data.Type,
                id: data.ID
            });
        }
        else {
            // The next level will be the attributes under this parent attribute type.
            dataUri += "&parentType=FusionAttribute&parentID=" + data.ParentFusionAttributeID;
            dataUri += "&parentFusionAttributeTypeID=" + data.ID;
            $.getJSON("/api/FusionAttributeType/" + data.ID + "/grid/definition", function (definition) {
                definition.Fields.push({ name: 'FusionAttributeTypeID', type: 'number' });

                amplify.publish('FusionItemSelected', {
                    definition: definition,
                    uri: dataUri,
                    title: data.Name,
                    type: data.Type,
                    id: data.ID
                });
            });
        }
    }

    var gridRowSelect = function (event) {
        var args = event.args;              // event arguments.
        var rowBoundIndex = args.rowindex;  // row's bound index.
        var rowData = args.row;             // row's data.
        if (rowData.Type === 'FusionAttribute') {
            amplify.publish('FusionAttributeRowSelected', {
                ID: rowData.ID,
                Name: rowData.Name
            });
        }
    }

    var pageResized = function () {
        $(gridControlID).jqxGrid('refresh');
    }

    var unsubscribe = function (data) {
        source = null;
        adapter = null;

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    // in case where we dont have the definition we need to get it this happens on goto initial selected item
    if (definition === null && id !== null) {
        $.ajax({
            type: "GET",
            url: "/api/FusionAttributeType/" + id + "/grid/definition",
            async: false,
            contentType: 'application/json',
            dataType: 'json',
            success: function (data) {
                definition = data;
                definition.Fields.push({ name: 'FusionAttributeTypeID', type: 'number' });
            }
        });
    }

    //#endregion
    //modify type column
    definition.Columns.forEach(function (item) {
        if (item.datafield && item.datafield.toUpperCase() === 'TYPE') item.datafield = '_type';
    });

    definition.Fields.forEach(function (item) {
        if (item.name && item.name.toUpperCase() === 'TYPE') item.name = '_type';
    });
    //add internal type
    definition.Fields.push({ name: 'Type', type: 'string' });

    var source = {
        datatype: 'json',
        type: 'get',
        url: dataUri,
        datafields: definition.Fields,
        beforeprocessing: function (data) {
            source.totalrecords = data.total;
        },
        filter: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        sort: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        }
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            //showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            virtualmode: true,
            rendergridrows: function () {
                return adapter.records;
            },
            columnsresize: true,
            source: adapter,
            ready: function () {
                if (definition.Columns.length > 7) {
                    $(gridControlID).jqxGrid('pincolumn', 'Name');
                    $(gridControlID).jqxGrid('autoresizecolumns');
                }
            },
            theme: theme,
            columns: definition.Columns
        });

        //#region Event Subscriptions

        $(gridControlID).one('rowdoubleclick', doubleClick);
        $(gridControlID).on('rowselect', gridRowSelect);
        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

        //#endregion
    } catch (e) {
    }
}

function FusionItemsGrid(controlID, contextList, permissions, fusionTypeID, fusionID, initialData) {

    var _innercontrolID = controlID + '_inner'; //stores original value.
    var breadcrumbControlID = controlID + "_breadcrumb";
    controlID = '#' + controlID;

    $(controlID).html('<header>Items</header><div><ul class="hierarchy" id="' + breadcrumbControlID + '"><li id="H_FusionAttributeType_0">Root</li></ul></div><div class="form-instructions">Double-click on an item in the list below.</div><div id="' + _innercontrolID + '"></div>');

    breadcrumbControlID = '#' + breadcrumbControlID;

    var typeDefinition = {
        Fields: [
            { name: 'ID', type: 'number' },
            { name: 'Name', type: 'string' },
            { name: 'IsLeaf', type: 'bool' },
            { name: 'ParentFusionAttributeID', type: 'number' }
        ],
        Columns: [
            { text: 'Name', dataField: 'Name' }
        ]
    };

    //#region Event Subscriptions

    function fusionItemSelected(data) {
        var liID = 'H_' + data.type + '_' + data.id;

        $(breadcrumbControlID).append('<li class="separator">/</li><li id="' + liID + '">' + data.title + '</li>');
        $('#' + liID).data('type', data.type);
        $('#' + liID).data('id', data.id);
        $('#' + liID).data('definition', data.definition);
        $('#' + liID).data('uri', data.uri);

        $(breadcrumbControlID).children().removeClass('clickable');
        $('#' + liID).prevAll('li').not('.separator').addClass('clickable');

        $('#' + liID).on('click', function () {
            $(this).nextAll().remove();
            _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, $(this).data('definition'), $(this).data('uri'));
        });

        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, data.definition, data.uri);
    }

    function buildCurrentItemBreadcrumb(data) {
        //need to go from selected item to root and generate the breadcrumb
        $.getJSON('/api/fusion/selectedbreadcrumb/' + data.SelectedID, function (path) {
            path.forEach(function (item) {
                buildBreadcrumbLink('FusionAttributeType', item.typeid, item.typename, item.typeid, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttribute&parentID=' + item.parentID + '&parentFusionAttributeTypeID=' + item.typeid, false);

                buildBreadcrumbLink('FusionAttribute', item.id, item.name, item.typeid, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType&parentID=' + item.typeid + '&parentFusionAttributeID=' + item.id, true);
            });
        });
    }

    function buildBreadcrumbLink(type, id, name, typeid, uri, passdefault) {
        var liID = 'H_' + type + '_' + id;
        $(breadcrumbControlID).append('<li class="separator">/</li><li id="' + liID + '">' + name + '</li>');
        $('#' + liID).data('type', type);
        $('#' + liID).data('id', id);
        $('#' + liID).data('uri', uri);

        if (passdefault) {
            var def = typeDefinition;
            def.id = typeid;
            def.title = name;
            def.type = type;
            $('#' + liID).data('definition', def);
        }

        $('#' + liID).prevAll('li').not('.separator').addClass('clickable');
        $('#' + liID).on('click', function () {
            $(this).nextAll().remove();
            _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, passdefault ? $(this).data('definition') : null, $(this).data('uri'), typeid);
        });
    }

    function rootLiClick() {
        $(this).nextAll().remove();
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType');
    }

    function unsubscribe(data) {
        $('#H_FusionAttributeType_0').off('click', rootLiClick);
        amplify.unsubscribe('FusionItemSelected', fusionItemSelected);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $('#H_FusionAttributeType_0').on('click', rootLiClick);
    amplify.subscribe('FusionItemSelected', fusionItemSelected);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
    if (initialData != null) {
        // build the breadcrumb for current item
        buildCurrentItemBreadcrumb(initialData);

        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType&parentID=' + initialData.FusionAttributeTypeID + '&parentFusionAttributeID=' + initialData.ID);
    }
    else
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType');
}