function CriticalRelationshipsTile(controlID, type, id) {

    var source = {
        datatype: 'json',
        type: 'get',
        datafields: [
            { name: "IntersectID" },
            { name: "ID" },
            { name: "ObjectType" },
            { name: 'TypeName' },
            { name: 'Url' },
            { name: 'Name' },
            { name: 'Description' },
            { name: 'IconBackColor' },
            { name: 'IconForeColor' },
            { name: 'IconText' }
        ],
        url: '/api/' + type + '/' + id + '/relations/critical'
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(controlID).jqxGrid({
            source: adapter,
            width: grid_width,
            pagesizeoptions: ['5', '10', '20'],
            pagesize: 20,
            autoheight: true,
            sortable: true,
            altrows: true,
            showfilterrow: true,
            filterable: true,
            groupsrenderer: function (text, group, expanded, data) {
                return '<h2>' + group + '</h2>';
            },
            groupable: true,
            groupsexpandedbydefault: true,
            showgroupsheader: true,
            groups: ['TypeName'],
            pageable: true,
            theme: list_theme,
            columns: [
                { datafield: "TypeName", text: "Type", hidden: true },
                {
                    datafield: "Name",
                    text: "Name",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return previewLinkRenderer(data.ObjectType, data.ID, data.Url, data.Name);
                    }
                },
                { datafield: "Description", text: "Description" },
            ]
        });
    } catch (e) {
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case 'Intersect':
                    $(controlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : CriticalRelationshipsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
}