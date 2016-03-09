function fusion_item(app, pageViewModel, templatePath, contextList) {

    var fi = function (context) {
        context.app.swap('');

        var type = 'Fusion';
        var typeID = context.params['typeid'];
        var id = context.params['id'];
        var executionID = context.params['executionid'];
        //var tab = context.params['tab'];
        var fusionAttributeID = context.params['fusionattributeid'];
        var fusionAttributeIDType = context.params['fusionattributetypeid'];

        var permissions = new PermissionsModel();

        var url = '/api/fusion/' + typeID + '/configurations/' + id;

        if (fusionAttributeID != null)
            url = '/api/fusion/' + fusionAttributeID + '/configurations/fromFusionAttribute';

        $.getJSON(url, function (json) {

            if (Array.isArray(json)) {                
                json = json[0];
            }

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Fusion' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            context.title(pageViewModel.Title);

            if (typeID == null || id == null)
            {
                typeID = json.FusionTypeID;
                id = json.ID;
            }

            
            //#region Event Handlers

            function fusionAttributeRowSelected(data) {
                $('#AggregatesTile').fadeIn(500);
                FusionRelationshipChartTile('AggregatesTile', 'FusionAttribute', data.ID);
                AttributesTile('ItemAttributesTile', contextList, permissions, 'FusionAttribute', data.ID, 'Technical Attributes for ' + data.Name)
                FusionAttributeDetailTile('FusionAttributeDetailsTile', 'FusionAttribute', data.ID);
            }

            function toolAction(data) {
                switch (data.context) {
                    case contextList.ActionExport:
                        //alert(data.uri);
                        $.fileDownload(data.uri, {
                            httpMethod: "GET"
                        });
                        break;
                }
            }

            function unsubscribe(data) {
                amplify.unsubscribe('FusionAttributeRowSelected', fusionAttributeRowSelected);
                amplify.unsubscribe("ToolAction", toolAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'fusion.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    permissions.GetPermissionsForObject(type, id);

                    $('#SideIcons').PageTools({ type: type, id: id });

                    if (fusionAttributeID != null) json.ID = fusionAttributeID;                    
                    FusionItemsGrid('ItemsTile', contextList, permissions, typeID, id, (fusionAttributeID != null) ? json : null);                   
                    PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '', false);

                    //#region Events

                    amplify.subscribe('FusionAttributeRowSelected', fusionAttributeRowSelected);
                    amplify.subscribe("ToolAction", toolAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion

                    if (fusionAttributeID != null) {
                        var item = { ID: fusionAttributeID, Name: json.ItemName };
                        fusionAttributeRowSelected(item);                        
                    }

                    if (executionID) {
                        amplify.publish("ToolAction", { uri: '/fusion/FusionExecution?id=' + executionID, context: null });
                    }
                });
        });
    };

    app.get('#/fusion/:typeid/:id/executions/:executionid', fi);
    //app.get('#/fusion/:typeid/:id/:tab/:fusionattributeid', fi);
    //app.get('#/fusion/:typeid/:id/:tab', fi);
    app.get('#/fusion/:typeid/:id', fi);
    app.get('#/fusion/item/:fusionattributetypeid/:fusionattributeid', fi);
}