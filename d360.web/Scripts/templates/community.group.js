function community_group(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/community/group', function (context) {
        context.app.swap('');

        var type = "Resource";
        var id = currentResourceID;

        pageViewModel.Title += ': Groups : Chief Data Officers'

        context.title(pageViewModel.Title);

        //#region Event Handlers

        function unsubscribe(data) {
            //AuditGridAdapter = null;
            //AuditGridSource = null;

            //amplify.unsubscribe('CancelAction', cancelAction);
            //amplify.unsubscribe('SaveAction', saveAction);

            //$("#RelationshipContextsGrid").off("bindingcomplete", relationshipContextsGridBindingComplete);
            //$("#TechnicalRelationshipsGrid").off("bindingcomplete", technicalRelationshipsGridBindingComplete);
            //$('#TreeGrid').off('rowselect', treeGridRowSelect);
            //$('#TreeGrid').off('rowdoubleclick', treeGridRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'community.group.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                //#region Members Tile

                var MembersTileData = [
                    { Name: 'Joe McCourtney', Company: 'Client A', Title: 'Director of Governance' },
                    { Name: 'John Smith', Vendor: 'Client B', Title: 'Chief Data Officer' },
                    { Name: 'Joseph Rockefeller', Vendor: 'Client C', Title: 'Chief Data Officer' }
                ];

                var MembersTileSource = {
                    datatype: "json",
                    datafields: [
                        { name: 'Name', type: 'string' },
                        { name: 'Company', type: 'string' },
                        { name: 'Title', type: 'string' }
                    ],
                    localdata: MembersTileData
                };

                var MembersTileAdapter = new $.jqx.dataAdapter(MembersTileSource);

                $("#MembersTile").jqxGrid({
                    width: grid_width,
                    pagermode: 'simple',
                    autoheight: true,
                    rowsheight: 50,
                    sortable: true,
                    altrows: true,
                    filterable: false,
                    //showfilterrow: true,
                    virtualmode: false,
                    pageable: false,
                    source: MembersTileAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        { datafield: "Company", text: "Company" },
                        { datafield: "Title", text: "Title" }
                    ]
                });

                //#endregion

                //#region Threads Tile

                var ThreadsTileData = [
                    { Title: 'Any thoughts on best uses for D3S Domains?', Group: 'MDM Today', ReplyCount: 67 },
                    { Title: 'Who has used Markit\'s new data product line?', Group: 'Chief Data Officers', ReplyCount: 65 }
                ];

                var ThreadsTileSource = {
                    datatype: "json",
                    datafields: [
                        { name: 'Title', type: 'string' },
                        { name: 'Group', type: 'string' },
                        { name: 'ReplyCount', type: 'int' }
                    ],
                    localdata: ThreadsTileData
                };

                var ThreadsTileAdapter = new $.jqx.dataAdapter(ThreadsTileSource);

                $("#ThreadsTile").jqxGrid({
                    width: grid_width,
                    pagermode: 'simple',
                    autoheight: true,
                    sortable: true,
                    autorowheight: true,
                    rowsheight: 50,
                    altrows: true,
                    filterable: false,
                    //showfilterrow: true,
                    virtualmode: false,
                    pageable: false,
                    source: ThreadsTileAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Title", text: "Title" },
                        { datafield: "Group", text: "Group", width: 100 },
                        { datafield: "ReplyCount", filtertype: 'number', text: "Replies", width: 50 }
                    ]
                });

                //#endregion

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}