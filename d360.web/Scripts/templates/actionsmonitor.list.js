function actionsmonitor_list(app, pageViewModel, templatePath, contextList) {
        app.get('#/activitymonitor', function (context) {
            context.app.swap('');

            var permissions = new PermissionsModel();

            pageViewModel.Title = 'Monitor';
            pageViewModel.Directions = 'View all current and past issues and challenges.';

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Monitor', Active: true });
            
            context.title(pageViewModel.Title);

            var IssueGridSource;
            var IssueGridAdapter;
            var ChallengeGridSource;
            var ChallengeGridAdapter;
            
            //#region Event Handlers
                        
            function unsubscribe(data) {
                IssueGridAdapter = null;
                IssueGridSource = null;
                ChallengeGridSource = null;
                ChallengeGridAdapter = null;
                
                $('#issues-tab').off('click', toggleActiveTab);
                $('#challenge-tab').off('click', toggleActiveTab);

                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);                
            }

            function saveAction(data) {                
                try {
                    switch (data.context) {                        
                        case 'issueform':
                        case 'IssueWorkflow':
                            $("#IssuesGrid").jqxGrid('updatebounddata');
                            break;
                        case 'OwnerApprovalWorkflow':
                            $("#ChallengesGrid").jqxGrid('updatebounddata');
                            break;
                    }
                } catch (e) {
                    logError("actionsmonitor.list : SaveAction", e);
                }
            }

            function toggleActiveTab() {
                $('#IssuesGrid').toggle();
                $('#issues-tab').parent().toggleClass('active-monitor-tab');
                $('#issues-tab').parent().toggleClass('monitor-tab');
                $('#ChallengesGrid').toggle();
                $('#challenge-tab').parent().toggleClass('active-monitor-tab');
                $('#challenge-tab').parent().toggleClass('monitor-tab');
            }

            //#endregion

            context
                .render(templatePath + 'actionsmonitor.list.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: 'Monitor', id: 0 });
                                                                                                    
                    //#region Grid Logic

                    IssueGridSource = {
                        datatype: 'json',
                        type: 'get',
                        //url: "/services/workflow/all/issues?$orderby=DateStarted desc,Issue&$filter=substringof('aaa',Issue)",
                        url: "/services/workflow/all/issues?$orderby=DateStarted desc,Issue",
                        datafields:
                        [
                            { name: 'WorkflowID', type:'string' },
                            { name: 'Issue', type: 'string' },
                            { name: 'RaisedBy', type: 'string' },
                            { name: 'DateStarted', type: 'date' },
                            { name: 'DateCompleted', type: 'date' },
                            { name: 'IsCompleted', type: 'bool' },
                            { name: 'Name', type: 'string' },
                            { name: 'Object', type: 'string' },
                            { name: 'AllowAction', type: 'bool'},
                            { name: 'ObjectID', type: 'number' },
                            { name: 'RaisedByResourceID', type: 'number' },
                            { name: 'Url', type: 'string' },
                            { name: 'ActivityName', type: 'string' },
                            { name: 'Notes', type: 'string' },
                        ]
                    };

                    IssueGridAdapter = new $.jqx.dataAdapter(IssueGridSource);

                    $("#IssuesGrid").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        autoheight: true,
                        autorowheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        pagesizeoptions: ['5', '10', '20'],                                                
                        source: IssueGridAdapter,
                        theme: list_theme,                        
                        columns: [                            
                            , { datafield: "Issue", text: 'Issue' }
                            , {
                                datafield: "Name", text: "Name",
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return (data.ObjectID > 0 ? previewLinkRenderer(data.Object, data.ObjectID, data.Url, data.Name) : textrenderer(""));
                                }
                            }
                            , { datafield: "Object", text: 'Type', filtertype: 'checkedlist', width: 150 }
                            , {
                                filtertype: 'checkedlist', datafield: "RaisedBy", text: "Created By", width: 150,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return previewLinkRenderer('Resource', data.RaisedByResourceID, '#/resources/' + data.RaisedByResourceID, data.RaisedBy);
                                }
                            }
                            , { datafield: "DateStarted", text: 'Created On', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: 150 }
                            , { datafield: "DateCompleted", text: 'Closed On', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: 150 }                            
                            , { datafield: "ActivityName", text: "Status", filtertype: 'checkedlist', width: 125,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {                                                        
                                    return '<div style="overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 15px;"' + (!data.AllowAction && !data.IsCompleted ? 'data-type="WorkflowTypeRelation" data-context="list" data-id="' + data.WorkflowID + '"' : '') + '>' + (!data.AllowAction && !data.IsCompleted ? '<i class="fa fa-question-circle-o" aria-hidden="true"></i> ' : '') + data.ActivityName + '</div>';
                                }
                            }
                            , { datafield: "Notes", text: 'Closing Notes' }
                            , {
                                datafield: "WorkflowID",
                                text: "",
                                sortable: false,
                                filterable: false,
                                width: '40px',
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                        var tools = [];

                                        if (data.AllowAction)
                                            tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay/true' });

                                        return renderToolsHtml(value, tools, contextList.Monitor, data);
                                    }
                                }
                            ]
                    });



                    ChallengeGridSource = {
                        datatype: 'json',
                        type: 'get',                        
                        url: "/services/workflow/all/challenges?$orderby=DateStarted desc,Reason",
                        datafields:
                        [
                            { name: 'WorkflowID', type: 'string' },
                            { name: 'Reason', type: 'string' },
                            { name: 'RaisedBy', type: 'string' },
                            { name: 'DateStarted', type: 'date' },
                            { name: 'DateCompleted', type: 'date' },
                            { name: 'IsCompleted', type: 'bool' },
                            { name: 'Name', type: 'string' },
                            { name: 'ArtifactTypeName', type: 'string' },
                            { name: 'AllowAction', type: 'bool' },
                            { name: 'ArtifactID', type: 'number' },
                            { name: 'RaisedByResourceID', type: 'number' },
                            { name: 'Url', type: 'string' },
                            { name: 'IsApproved', type: 'bool' },
                            { name: 'Notes', type: 'string' },
                            { name: 'ActivityName', type: 'string' },
                            { name: 'ClosedBy', type: 'string' },
                            { name: 'ClosedByResourceID', type: 'string' },
                        ]
                    };

                    ChallengeGridAdapter = new $.jqx.dataAdapter(ChallengeGridSource);

                    $("#ChallengesGrid").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        autoheight: true,
                        autorowheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        pagesizeoptions: ['5', '10', '20'],                        
                        source: ChallengeGridAdapter,
                        theme: list_theme,
                        columns: [
                            , { datafield: "Reason", text: 'Reason' }
                            , {
                                datafield: "Name", text: "Artifact",
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return (data.ArtifactID > 0 ? previewLinkRenderer('Artifact', data.ArtifactID, data.Url, data.Name) : textrenderer("Removed Item"));
                                }
                            }
                            , { datafield: "ArtifactTypeName", text: 'Artifact Type', filtertype: 'checkedlist', width: 150 }
                            , {
                                filtertype: 'checkedlist', datafield: "RaisedBy", text: "Created By", width: 150,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return previewLinkRenderer('Resource', data.RaisedByResourceID, '#/resources/' + data.RaisedByResourceID, data.RaisedBy);
                                }
                            }
                            , { datafield: "DateStarted", text: 'Created On', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: 150 }
                            , { datafield: "DateCompleted", text: 'Closed On', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: 150 }                            
                            , {
                                datafield: "ActivityName", text: "Status", filtertype: 'checkedlist', width: 125,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return '<div style="overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 15px;"' + (!data.AllowAction && !data.IsCompleted ? 'data-type="WorkflowTypeRelation" data-context="list" data-id="' + data.WorkflowID + '"' : '') + '>' + (!data.AllowAction && !data.IsCompleted ? '<i class="fa fa-question-circle-o" aria-hidden="true"></i> ' : '') + data.ActivityName + '</div>';
                                }
                            }                            
                            , { datafield: "IsApproved", text: 'Approved?', columntype: 'checkbox', threestatecheckbox: true, filtertype: 'bool', width: 80 }
                            , { datafield: "Notes", text: 'Closing Notes' }
                            , {
                                filtertype: 'checkedlist', datafield: "ClosedBy", text: "Closed By", width: 150,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    if(data.ClosedByResourceID > 0)
                                        return previewLinkRenderer('Resource', data.ClosedByResourceID, '#/resources/' + data.ClosedByResourceID, data.ClosedBy);
                                }
                            }
                            , {
                                datafield: "WorkflowID",
                                text: "",
                                sortable: false,
                                filterable: false,
                                width: '40px',
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    var tools = [];

                                    if (data.AllowAction && !data.IsCompleted)
                                        tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay/true' });

                                    return renderToolsHtml(value, tools, contextList.Monitor, data);
                                }
                            }
                        ]
                    });
                    //#endregion

                    //#region Event Subscriptions

                    $('#issues-tab').click(toggleActiveTab);

                    $('#challenge-tab').click(toggleActiveTab);
                                        
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                });

        });
    }