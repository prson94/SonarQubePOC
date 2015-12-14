function artifacts_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/:typeid/:id', function (context) {
        //context.spinner();
        context.app.swap('');
        
        var type = 'Artifact';
        var typeID = context.params['typeid'];
        var id = context.params['id'];
        var permissions = new PermissionsModel();

        $.getJSON('/api/artifact/' + id, function (json) {

            var getArtifactStatusForeColor = function (status) {
                var foreColor = '#000';
                switch (status) {
                    case 'Certified':
                        foreColor = '#3f9d40';
                        break;
                    case 'Under Review':
                        foreColor = '#e2792a';
                        break;
                    default:
                        foreColor = '#999';
                        break;
                }
                return foreColor;
            }

            pageViewModel.ObjectType = 'Artifact';
            pageViewModel.ObjectID = id;
            pageViewModel.Title = json.Name;
            pageViewModel.Type = json.TypeName;
            pageViewModel.Status = "<h4>Status: <b style='color:" + getArtifactStatusForeColor(json.Status) + "'>" + json.Status + "</b></h4>";
            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Glossary' });
            pageViewModel.breadcrumbs.push({ Name: json.TypeName });
            pageViewModel.breadcrumbs.push({ Name: json.Name, Active: true });
            //pageViewModel.Directions = json.Description;

            context.title(pageViewModel.Title);

            //#region Event Handlers

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        ObjectStatisticsTile('MicroWidget1', type, id);
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools('reload', type, id);
            }

            function saveAction(data) {
                // alert(ko.toJSON(data));
                try {
                    switch (data.context) {
                        case contextList.Comment:
                        case 'commentform':
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            break;
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTile', type, id, permissions);
                            break;
                        case "RequestCertification":
                        case "Workflow":
                            DetailTile('DetailTile', contextList, permissions, type, id);
                            break;
                        case contextList.Responsibility:
                        case contextList.Artifact:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            break;
                        case contextList.SourcingResponsibility:
                            environment_diagram('SourcingTile', permissions, type, id);
                            break;
                        case contextList.Synonym:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                    }
                } catch (e) {
                    logError("artifact.item : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            //#region GoJS Support Functions

            //function doubleTreeLayout(diagram) {
            //    // Within this function override the definition of '$' from jQuery:
            //    var g = go.GraphObject.make;  // for conciseness in defining templates
            //    diagram.startTransaction("Double Tree Layout");

            //    // split the nodes and links into two Sets, depending on direction
            //    var leftParts = new go.Set(go.Part);
            //    var rightParts = new go.Set(go.Part);
            //    separatePartsByLayout(diagram, leftParts, rightParts);
            //    // but the ROOT node will be in both collections

            //    // create and perform two TreeLayouts, one in each direction,
            //    // without moving the ROOT node, on the different subsets of nodes and links
            //    var layout1 =
            //      g(go.TreeLayout,
            //        {
            //            angle: 180,
            //            arrangement: go.TreeLayout.ArrangementFixedRoots,
            //            setsPortSpot: false
            //        });

            //    var layout2 =
            //      g(go.TreeLayout,
            //        {
            //            angle: 0,
            //            arrangement: go.TreeLayout.ArrangementFixedRoots,
            //            setsPortSpot: false
            //        });

            //    layout1.doLayout(leftParts);
            //    layout2.doLayout(rightParts);

            //    diagram.commitTransaction("Double Tree Layout");
            //}

            //function separatePartsByLayout(diagram, leftParts, rightParts) {
            //    var root = diagram.findNodeForKey("Root");
            //    if (root === null) return;
            //    // the ROOT node is shared by both subtrees!
            //    leftParts.add(root);
            //    rightParts.add(root);
            //    // look at all of the immediate children of the ROOT node
            //    root.findTreeChildrenNodes().each(function (child) {
            //        // in what direction is this child growing?
            //        var dir = child.data.dir;
            //        var coll = (dir === "left") ? leftParts : rightParts;
            //        // add the whole subtree starting with this child node
            //        coll.addAll(child.findTreeParts());
            //        // and also add the link from the ROOT node to this child node
            //        coll.add(child.findTreeParentLink());
            //    });
            //}

            //#endregion

            context
                .render(templatePath + 'artifacts.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {

                    //$.getJSON('/api/Artifact/' + id + '/flags', function (flagdata) {
                    //    pageViewModel.RedFlagged = flagdata.RedFlagged;
                        context.contentHeader(pageViewModel);
                    //});

                    $('#SideIcons').PageTools({ type: type, id: id });
                    $("#RandomQuestion").RandomSurveyQuestion({ objectType: type, objectID: id });

                    var loadPermissionsDependentTiles = function () {
                        ObjectStatisticsTile('MicroWidget1', type, id);
                        RelationshipAggregatesTile('AggregatesTile', type, id, permissions);

                        //Relationship_SimpleHierarchyTile('SimpleHierarchyTile', contextList, permissions, type, id);

                        PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '');
                        environment_diagram('SourcingTile', permissions, type, id);
                        //AttributesTile('AttributesTile', contextList, permissions, type, id, 'Business Attributes');
                        CertificationNotificationTile('CertificationNotification', id);
                        //TagsTile('Tags', permissions, type, id);
                        if (json.AllowRelatedArtifacts) {
                            RelatedArtifactsGrid('RelatedArtifactsTile', permissions, json.TypeName, typeID, id);
                        }
                        else {
                            $('#RelatedArtifactsTile').hide();
                        }
                        DetailsTile('DetailTile', contextList, permissions, type, id, contextList.Artifact);
                        //DetailTile('DetailTile', contextList, permissions, type, id);

                        //#region GoJS Diagram

                        //$('#GoDiagram').height('400px');

                        //var g = go.GraphObject.make;
                        
                        //var myDiagram = g(go.Diagram, "GoDiagram", {
                        //    initialContentAlignment: go.Spot.Center, // center Diagram contents
                        //    "undoManager.isEnabled": true // enable Ctrl-Z to undo and Ctrl-Y to redo
                        //});

                        //// define all of the gradient brushes
                        //var graygrad = g(go.Brush, "Linear", { 0: "#F5F5F5", 1: "#F1F1F1" });
                        //var bluegrad = g(go.Brush, "Linear", { 0: "#CDDAF0", 1: "#91ADDD" });
                        //var yellowgrad = g(go.Brush, "Linear", { 0: "#FEC901", 1: "#FEA200" });
                        //var lavgrad = g(go.Brush, "Linear", { 0: "#EF9EFA", 1: "#A570AD" });

                        //// define the Node template for non-terminal nodes
                        //myDiagram.nodeTemplate =
                        //  g(go.Node, "Auto",
                        //    { isShadowed: false },

                        //    // define the node's outer shape
                        //    g(go.Shape, "RoundedRectangle",
                        //      { fill: graygrad, stroke: "#D8D8D8" },
                        //      new go.Binding("fill", "color")),

                        //    // define the node's text
                        //    g(go.TextBlock,
                        //    {
                        //        margin: 5, font: "bold 11px Helvetica, bold Arial, sans-serif"
                        //    }, new go.Binding("text", "key"))
                        //  );

                        //// define the Link template
                        //myDiagram.linkTemplate =
                        //  g(go.Link,  // the whole link panel
                        //    { selectable: false },
                        //    g(go.Shape));  // the link shape
                        
                        //// create the model for the double tree
                        //myDiagram.model = new go.TreeModel([
                        //    // these node data are indented but not nested according to the depth in the tree
                        //    { key: "Root", color: lavgrad },
                        //      { key: "Left1", parent: "Root", dir: "left", color: bluegrad },
                        //        { key: "leaf1", parent: "Left1" },
                        //        { key: "leaf2", parent: "Left1" },
                        //        { key: "Left2", parent: "Left1", color: bluegrad },
                        //          { key: "leaf3", parent: "Left2" },
                        //          { key: "leaf4", parent: "Left2" },
                        //      { key: "Right1", parent: "Root", dir: "right", color: yellowgrad },
                        //        { key: "Right2", parent: "Right1", color: yellowgrad },
                        //          { key: "leaf5", parent: "Right2" },
                        //          { key: "leaf6", parent: "Right2" },
                        //          { key: "leaf7", parent: "Right2" },
                        //        { key: "leaf8", parent: "Right1" },
                        //        { key: "leaf9", parent: "Right1" }
                        //]);

                        //doubleTreeLayout(myDiagram);

                        //#endregion
                    }

                    permissions.GetPermissionsForObject(type, id).then(loadPermissionsDependentTiles);

                    //#region Event Subscriptions

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                });
        });
    });
}