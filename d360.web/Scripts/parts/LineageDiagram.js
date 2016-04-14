function LineageDiagram(controlID, type, id, readonly) {
    var originalObject = type;
    var originalObjectID = id;
    var fullscreen = false;
    var selectedData = null;
    var permissions = new PermissionsModel();
    permissions.GetPermissionsForObject(type, id);

    var tmpl = Handlebars.getTemplate('LineageDiagram');
    $('#' + controlID).html(tmpl({ control: controlID }));

    //#region Control constants

    var controlID_header = controlID + "_header";
    var controlID_wrapper = controlID + '_wrapper';
    var controlID_diagram = controlID + '_dgm';
    var controlID_palette = controlID + '_palette';
    var controlID_overview = controlID + '_overview';
    var controlID_sidebar = controlID + '_sidebar';
    var controlID_overlay = controlID + '_overlay';
    var controlID_ribbon = controlID + '_ribbon';
    var controlID_wrapper_fullscreen = controlID + '_wrapper_fullscreen';
    var controlID_message = controlID + '_message';

    var controlID_controls = controlID + '_controls';

    var controlID_info = controlID + '_info';
    var controlID_info_body = controlID + '_info_body';
    var controlID_info_detail = controlID + '_info_detail';
    var controlID_info_detail_wrapper = controlID + '_info_detail_wrapper';
    var controlID_info_detail_edit = controlID + '_info_detail_edit';

    var controlID_add_search_text = controlID + '_add_search_text';
    var controlID_add_search = controlID + '_add_search';
    var controlID_add_artifact_type = controlID + '_add_artifact_type';
    var controlID_add_search_message = controlID + '_add_search_message';

    var controlID_overlay_existing = controlID + '_overlay_existing';
    var controlID_overlay_new = controlID + '_overlay_new';
    var controlID_overlay_relationship = controlID + '_overlay_relationship';
    var controlID_overlay_predicates = controlID + '_overlay_predicates';
    var controlID_overlay_cancel = controlID + '_overlay_cancel';
    var controlID_overlay_add = controlID + '_overlay_add';
    var controlID_overlay_roles = controlID + '_overlay_roles';

    var controlID_ribbon_spacer = controlID + '_ribbon_spacer';
    var controlID_ribbon_content = controlID + '_ribbon_content';
    var controlID_ribbon_expander = controlID + '_ribbon_expander';
    var controlID_ribbon_zoom_slider = controlID + '_ribbon_zoom_slider';
    var controlID_ribbon_zoom_out = controlID + '_ribbon_zoom_out';
    var controlID_ribbon_zoom_in = controlID + '_ribbon_zoom_in';
    var controlID_ribbon_zoom_text = controlID + '_ribbon_zoom_text';
    var controlID_ribbon_zoom_100 = controlID + '_ribbon_zoom_100';
    var controlID_ribbon_zoom_fit = controlID + '_ribbon_zoom_fit';
    var controlID_ribbon_reset = controlID + '_ribbon_reset';
    var controlID_ribbon_fullscreen = controlID + '_ribbon_fullscreen';

    var controlID_ribbon_save = controlID + '_ribbon_save';
    var controlID_ribbon_save_spinner = controlID + '_ribbon_save_spinner';
    var controlID_ribbon_add = controlID + '_ribbon_add';
    var controlID_ribbon_undo = controlID + '_ribbon_undo';
    var controlID_ribbon_redo = controlID + '_ribbon_redo';
    var controlID_ribbon_remove = controlID + '_ribbon_remove';
    var controlID_ribbon_sourcerule_add = controlID + '_ribbon_sourcerule_add';
    var controlID_ribbon_sourcemapping_add = controlID + '_ribbon_sourcemapping_add';

    var controlID_popover_add = controlID + '_popover_add';

    var controlID_popover_sourcerule_editor = controlID + '_popover_sourcerule_editor';
    var controlID_popover_sourcerule_editor_body = controlID_popover_sourcerule_editor + '_body';
    var controlID_popover_sourcemapping_editor = controlID + '_popover_sourcemapping_editor';
    var controlID_popover_sourcemapping_editor_body = controlID_popover_sourcemapping_editor + '_body';

    var controlID_tabs = controlID + '_tabs';
    var controlID_fusion_tab = controlID + '_fusion_tab';
    var controlID_sourcerules_tab = controlID + '_sourcerules_tab';
    var controlID_mappingrules_tab = controlID + '_mappingrules_tab';
    var controlID_responsibilities_tab = controlID + '_responsibilities_tab';
    var controlID_fusion_content = controlID + '_fusion_content';
    var controlID_sourcerules_content = controlID + '_sourcerules_content';
    var controlID_mappingrules_content = controlID + '_mappingrules_content';
    var controlID_responsibilities_content = controlID + '_responsibilities_content';
    var tabs = {
        "sourcerules": 0,
        "mappingrules": 1,
        "responsibilities": 2,
        "fusion": 3
    };
    var defaultTabContent = '<div style="height:100px;text-align:center;padding:25px;"><i class="fa fa-2x fa-spinner fa-spin"></i></div>';

    //#endregion

    //#region Control instantiation

    $("#" + controlID_tabs).jqxTabs({ theme: theme, animationType: 'fade', selectionTracker: true }).on('tabclick',function(event) {
        var index = event.args.item;
        loadTab(index);
    });
    
    $("#" + controlID_ribbon_zoom_100).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_zoom_fit).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_save).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true });
    $("#" + controlID_ribbon_reset).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_fullscreen).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_add).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_remove).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_undo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_redo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_zoom_slider).jqxSlider({ theme: theme, width: 150, showButtons: true, min: 750, max: 2250, value: 1500, showTicks: false });

    $("#" + controlID_ribbon_sourcerule_add).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_sourcemapping_add).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();

    $("#" + controlID_info).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_ribbon_expander).jqxExpander({ theme: theme }).jqxExpander('collapse');

    //$("#" + controlID_info_table_wrapper).hide();

    //#endregion

    //#region Event Handlers

    $("#" + controlID_ribbon_add).on('click', function () {
        $('#' + controlID_popover_add).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 150);
    });

    $("#" + controlID_ribbon_sourcerule_add).on('click', function () {
        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;
        //console.log(selected);
        $('#' + controlID_popover_sourcerule_editor).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 150);

        //TODO: logic for nothing selected
        var data = {
            target: type,
            targetID: id,
            object: selected.type,
            objectID: selected.id,
            ID: 0,
            controlID: controlID
        };

        var model = new HierarchyPanelViewModel(data, permissions);
        ko.cleanNode($('#' + controlID_popover_sourcerule_editor_body)[0]);
        ko.applyBindings(model, $('#' + controlID_popover_sourcerule_editor_body)[0]);
        //model.ApplyJqxBindings();
    });

    $("#" + controlID_ribbon_sourcemapping_add).on('click', function () {
        $('#' + controlID_popover_sourcemapping_editor).toggle(200).css('left', $(this).position().left - 500).css('top', $(this).position().top + 150);
        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;

        
        var from = {};
        var to = {};

        if (selected.diagramObjectType == 'Node') {
            from = {
                id: selected.id,
                type: selected.type,
                name: selected.name,
                typeName: selected.typeName
            };
            to = from;
        } else {
            from = myDiagram.model.findNodeDataForKey(selected.from);
            to = myDiagram.model.findNodeDataForKey(selected.to);
        }


        var data = {
            Source: from.type,
            SourceID: from.id,
            SourceName: from.name,
            SourceTypeName: from.typeName,
            Target: to.type,
            TargetID: to.id,
            TargetName: to.name,
            TargetTypeName: to.typeName,
            Object: type,
            ObjectID: id,
        };
        var model = new SourceToTargetMappingModel(data, permissions);
        ko.cleanNode($('#' + controlID_popover_sourcemapping_editor_body)[0]);
        ko.applyBindings(model, $('#' + controlID_popover_sourcemapping_editor_body)[0]);
    });

    $('#' + controlID_ribbon).jqxRibbon({
        width: "100%",
        height: 64,
        animationType: "fade",
        selectionMode: "click",
        position: "top",
        theme: theme,
        mode: "default",
        selectedIndex: 0
    });

    $('#' + controlID_ribbon_fullscreen).on('click', function () {
        fullscreen = !fullscreen;
        if (fullscreen) {
            window.scrollTo(0, 0); //scroll to top
            $('#' + controlID_ribbon_fullscreen).html('<i class="fa fa-2x fa-sign-out"></i><br />Exit Fullscreen');
            $('#' + controlID_wrapper_fullscreen).css('position', 'fixed')
                .css('left', '0')
                .css('top', '0')
                .css('width', '100%')
                .height($(window).height())
                .css('overflow', 'hidden');

            var top = $('#' + controlID_wrapper).position().top;

            $('#' + controlID_wrapper).height($('#' + controlID_wrapper_fullscreen).height() - top - 20);
            $('#' + controlID_diagram).height($('#' + controlID_wrapper_fullscreen).height() - top - 20);

            $('#' + controlID_sidebar).height($('#' + controlID_wrapper_fullscreen).height() - 20);
        } else {
            $('#' + controlID_ribbon_fullscreen).html('<i class="fa fa-2x fa-arrows-alt"></i><br />Fullscreen');
            $('#' + controlID_wrapper_fullscreen).attr('style', 'z-index:1000000;background-color:white;');
            $('#' + controlID_wrapper).height(520);
            $('#' + controlID_diagram).height(520);
            $('#' + controlID_sidebar).height(520);
        }
        //force this to queue behind browser layout updates
        setTimeout(function () {
            myDiagram.requestUpdate();
            myDiagram.focus();
        }, 0);
    });

    $('#' + controlID_ribbon_save).on('click', function () {
        saveChanges();

        //var message = $('<p />', { text: 'Are you sure you want to save changes?' }),
        //     ok = $('<button />', {
        //         text: 'Save',
        //         'class': 'btn qtip-blue qtip-btn-inline',
        //     }),
        //     cancel = $('<button />', {
        //         text: 'Cancel',
        //         'class': 'btn qtip-blue qtip-btn-inline',
        //     });

        //var content = "<p>Are you sure you want to save changes?</p>"

        //confirmDialog(message.add(ok).add(cancel), 'Save Changes?', 'Save', saveChanges);
    });

    $('#' + controlID_ribbon_undo).on('click', function () {
        myDiagram.undoManager.undo();
    });

    $('#' + controlID_ribbon_redo).on('click', function () {
        myDiagram.undoManager.redo();
    });

    $('#' + controlID_ribbon_zoom_slider).on('slide', function (event) {
        var val = event.args.value;
        $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
        myDiagram.scale = (val / 1500);
    });

    $('#' + controlID_overlay_predicates).on('change', function (e) {
        var checked = true;//$('#' + controlID_overlay_radio_existing).jqxRadioButton('checked');
        if (checked) {
            if ($(this).val() == 0) {
                $('#' + controlID_overlay_add).prop('disabled', true);
            } else {
                $('#' + controlID_overlay_add).prop('disabled', false);
            }
        }
    });

    $('#' + controlID_wrapper).on('mouseup', function () {
        var height = $('#' + controlID_wrapper).height();
        if (height != oldHeight) {
            $('#' + controlID_diagram).height(height - 20);
            oldHeight = height;
            //force browser to update layout before executing
            setTimeout(function () {
                myDiagram.requestUpdate();
            }, 0);

        }
    });

    $('#' + controlID_ribbon_zoom_100).on('click', function () {
        //$('#' + controlID_ribbon_zoom_slider).val(1500);
        myDiagram.scale = 1.0;
        $('#' + controlID_ribbon_zoom_text).text('100%');
        $('#' + controlID_ribbon_zoom_text).jqxSlider('setValue', 1500);
    });

    $('#' + controlID_ribbon_zoom_fit).on('click', function () {
        myDiagram.zoomToFit();
        $('#' + controlID_ribbon_zoom_text).text(Math.round(myDiagram.scale * 100) + '%');
        var sliderValue = Math.round(myDiagram.scale * 100) / 1500;
        $('#' + controlID_ribbon_zoom_text).jqxSlider('setValue', sliderValue);
    });

    $('#' + controlID_ribbon_reset).on('click', function () {
        //type = originalObject;
        //id = originalObjectID;
        //populateDiagram();
        if (checkModified()) {
            var message = $('<p />', { text: 'Are you sure you want to reset the diagram? You have unsaved changes' }),
             ok = $('<button />', {
                 text: 'Reset',
                 'class': 'btn qtip-blue qtip-btn-inline'
             }),
             cancel = $('<button />', {
                 text: 'Cancel',
                 'class': 'btn qtip-blue qtip-btn-inline'
             });
            confirmDialog(message.add(ok).add(cancel), 'Reset Diagram?', 'Reset', function () {
                type = originalObject;
                id = originalObjectID;
                populateDiagram();
            });
        } else {
            type = originalObject;
            id = originalObjectID;
            populateDiagram();
        }
    });

    $('#' + controlID_add_search).on('click', getObjectsBySearch);
    $('#' + controlID_overlay_cancel).on('click', cancelAddLink);
    $('#' + controlID_overlay_add).on('click', addRelationship);
    $('#' + controlID_popover_add).on('keypress', '#' + controlID_add_search_text, function (e) {
        if (e.keyCode == 13) {
            $('#' + controlID_add_search).click();
            return false;
        }
    });

    //#endregion

    var oldHeight = $('#' + controlID_wrapper).height();
    var oldWidth = $('#' + controlID_wrapper).width();

    var newId = -1;
    var temp = null;
    var relationshipLabels = null;
    var pendingExclusionObjects = [];
    var pendingExclusionNodes = [];
    var deletedNodes = [];
    var initialLinks = [];
    var initialNodes = [];
    var newLink = null;
    var predicates = [];
    var overlayEditLinkKey = null;
    var selection = null;

    //#region Responsibilities

    var lineageResponsibilitySource = {
        datatype: 'json',
        url: null,
        datafields:
        [
            { name: 'ResponsibilityID' },
            { name: 'AssigningItemType' },
            { name: 'AssigningItemID' },
            { name: 'AssigningItemName' },
            { name: 'AssigningItemUrl' },
            { name: 'ResponsibleObjectType' },
            { name: 'ResponsibleObjectID' },
            { name: 'ResponsibleObjectName' },
            { name: 'PrimaryOwnerResourceID' },
            { name: 'PrimaryOwnerResourceName' },
            { name: 'PrimaryOwnerResourceUrl' },
            { name: 'ObjectType' },
            { name: 'ObjectID' },
            { name: 'Role' },
            { name: 'ResponsibleObjectUrl' }
        ]
    };

    var lineageResponsibilityAdapter = new $.jqx.dataAdapter(lineageResponsibilitySource);

    $('#' + controlID_responsibilities_content).jqxGrid({
        altrows: true,
        width: overlay_grid_width,
        autoheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: false,
        pagesize: 5,
        pageable: true,
        pagermode: "simple",
        selectionmode: 'none',
        autorowheight: true,
        source: lineageResponsibilityAdapter,
        theme: list_theme,
        columns: [
            { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "Role", text: "Role", width: '34%' },
            {
                columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "ResponsibleObjectName", text: "Resource", width: '33%',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return previewLinkRenderer(data.ResponsibleObjectType, data.ResponsibleObjectID, data.ResponsibleObjectUrl, data.ResponsibleObjectName);
                }
            },
            {
                columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "PrimaryOwnerResourceName", text: "Group Owner", width: '33%',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    if (data.PrimaryOwnerResourceName && data.PrimaryOwnerResourceName != '')
                        return previewLinkRenderer('Resource', data.PrimaryOwnerResourceID, data.PrimaryOwnerResourceUrl, data.PrimaryOwnerResourceName);
                    else
                        return '';
                }
            }
        ]
    });

    //#endregion

    //#region Technical Relations

    var technicalRelationsSource = {
        datatype: 'json',
        url: null,
        datafields: [
            { name: 'IntersectID' },
            { name: 'Description' },
            { name: 'TargetName' },
            { name: 'TargetObjectID' },
            { name: 'TargetObjectType' },
            { name: 'TargetTypeID' },
            { name: 'TargetType' },
            { name: 'TargetTypeName' },
            { name: 'Classification' },
            { name: 'TargetUrl' }
        ]
    };

    var technicalRelationsAdapter = new $.jqx.dataAdapter(technicalRelationsSource);

    $('#' + controlID_fusion_content).jqxGrid({
        width: overlay_grid_width,
        autoheight: true,
        sortable: true,
        altrows: true,
        filterable: true,
        showfilterrow: false,
        pagesize: 5,
        pageable: true,
        pagermode: "simple",
        selectionmode: 'none',
        autorowheight: true,
        columnsresize: true,
        enabletooltips: true,
        source: technicalRelationsAdapter,
        theme: list_theme,
        groupable: false,
        ready: function () {
            $(this).jqxGrid('autoresizecolumns');
        },
        columns: [
            //{ text: 'Type', groupable: true, datafield: 'TargetTypeName' },
            {
                text: 'Name', groupable: false, datafield: 'TargetName',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return textrenderer("<div class='cell-value-name'>" + data.TargetName + "</div><div class='cell-value-type'>" + data.TargetTypeName + "</div>");
                }
            }
        ]
    });

    //#endregion


    //#region methods

    function addRelationship() {

        var id = $('#' + controlID_overlay_predicates).val();
        var text = null;

        for (var i = 0; i < predicates.length; i++) {
            if (predicates[i].id.toString() == id.toString()) {
                text = predicates[i].name;
            }
        }
        //console.log(overlayEditLinkKey);

        myDiagram.startTransaction("nameRelationship")
        if (overlayEditLinkKey != null) {
            var link = findLinkDataForKey(overlayEditLinkKey);
            myDiagram.model.setDataProperty(link, 'text', text);
            myDiagram.model.setDataProperty(link, 'predicateId', id);

            //get the id if possible (if link is deleted and re-added)
            for (var i = 0; i < initialLinks.length; i++) {
                if (initialLinks[i].from == link.from && initialLinks[i].to == link.to) {
                    link.id = initialLinks[i].id;
                    //console.log('intersectMapID: ' + link.id);
                    myDiagram.model.setDataProperty(link, 'id', initialLinks[i].id);
                }
            }
        } else {
            newLink.predicateId = id;
            newLink.text = text;
            newLink.diagramObjectType = "Link";
            newLink.isDeletable = true;
            newLink.id = overlayEditLinkKey;
            newLink.key = overlayEditLinkKey;
            if (newLink.id == null) {
                for (var i = 0; i < initialLinks.length; i++) {
                    if (initialLinks[i].from == newLink.from && initialLinks[i].to == newLink.to) {
                        newLink.id = initialLinks[i].id;
                        newLink.key = initialLinks[i].key;
                        // console.log('intersectMapID: ' + link.id);
                        //myDiagram.model.setDataProperty(link, 'id', initialLinks[i].id);
                    }
                }
            }

            var index = -1;

            for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
                if (myDiagram.model.linkDataArray[i].from == newLink.from &&
                    myDiagram.model.linkDataArray[i].to == newLink.to) {
                    myDiagram.model.removeLinkData(myDiagram.model.linkDataArray[i]);
                    break;
                }
            }

            myDiagram.model.addLinkData(newLink);
        }


        myDiagram.commitTransaction("nameRelationship");
        $('#' + controlID_overlay).hide();
        newLink = null;
        overlayEditLinkKey = null;

        resetOverlay();

    };

    function cancelAddLink() {
        if (newLink != null) {
            if (overlayEditLinkKey != null) {
                overlayEditLinkKey = null;
            } else {
                myDiagram.startTransaction("removeLink");
                myDiagram.model.removeLinkData(newLink);
                myDiagram.commitTransaction("removeLink");
            }
            newLink = null;
        }

        resetOverlay();
        $('#' + controlID_overlay).hide();
    };

    function checkModified() {
        var nodes = getNodeChanges();
        var links = getLinkChanges();

        if (readonly) {
            $('#' + controlID_message).hide();
        } else if (nodes.deleted.length > 0 ||
            nodes.added.length > 0 ||
            nodes.modified.length > 0 ||
            links.added.length > 0 ||
            links.deleted.length > 0 ||
            links.modified.length > 0) {

            $('#' + controlID_message).show();
            $('#' + controlID_ribbon_save).jqxButton({ disabled: false })
            return true;
        } else {
            $('#' + controlID_message).hide();
            $('#' + controlID_ribbon_save).jqxButton({ disabled: true });
            return false;
        }
    }

    function confirmDialog(content, title, id, func) {
        $('<div />').qtip({
            content: {
                text: content,
                title: title
            },
            position: {
                my: 'center', at: 'center',
                target: $(window)
            },
            show: {
                ready: true,
                modal: {
                    on: true,
                    blur: false
                }
            },
            hide: false,
            style: {
                classes: 'qtip-blue qtip-rounded'
            },
            events: {
                render: function (event, api) {
                    $('button', api.elements.content).click(function (e) {
                        api.hide(e);
                        if ($(this).text() == id) {
                            $(this).prop('disabled', true);
                            func();
                        }

                    });
                },
                hide: function (event, api) { api.destroy(); }
            }
        })
    }

    function createLinkModel() {
        return {
            key: null,
            id: null,
            intersectTypeId: null,
            from: null,
            fromPortId: "OUT",
            to: null,
            toPortId: "IN",
            text: null,
            predicateId: null,
            isDeletable: true,
            exclude: 'false',
            diagramObjectType: "Link",
            sourceMappingCount: 0,
            hasMappingRules: false
        };
    };

    function createNodeModel() {
        return {
            key: null,
            id: null,
            parentId: null,
            name: null,
            type: null,
            typeName: null,
            objecttype: null,
            objecttypeid: null,
            backColor: null,
            foreColor: null,
            isDeletable: true,
            exclude: 'false',
            highlightColor: null,
            diagramObjectType: "Node",
            level: null,
            template: "Artifact",
            intersectMapId: null,
            intersectId: null,
            sourceRuleCount: 0,
            sourceMappingCount: 0,
            hasMappingRules: false,
            hasSourceRules: false,
            challengeCount: 0,
            hasChallenges: false
        };
    };

    function findLinkDataForKey(key) {
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].key == key)
                return myDiagram.model.linkDataArray[i];
        }
    }

    function findNodeIndexByObject(obj, objid) {
        for (var i = 0; i < myDiagram.model.nodeDataArray.length; i++) {
            if (myDiagram.model.nodeDataArray[i].type == obj && myDiagram.model.nodeDataArray[i].id == objid)
                return i
        }
        return -1;
    }

    function findLinkIndexByObjects(source, sourceid, target, targetid) {
        var sourceIx = findNodeIndexByObject(source, sourceid);
        var targetIx = findNodeIndexByObject(target, targetid);

        if (sourceIx < 0 || targetIx < 0)
            return -1;

        var sourceKey = myDiagram.model.nodeDataArray[sourceIx].key;
        var targetKey = myDiagram.model.nodeDataArray[targetIx].key;

        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].from == sourceKey && myDiagram.model.linkDataArray[i].to == targetKey)
                return i;
        }
        return -1;
    }


    function getImmediateParents(key) {
        //console.log(key);
        var parents = [];
        var links = [];
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].to == key) {
                //console.log(myDiagram.model.linkDataArray[i]);
                parents.push(myDiagram.model.findNodeDataForKey(myDiagram.model.linkDataArray[i].from));
            }
        }
        // console.log(parents);
        return parents;
    }

    function getLinkChanges() {
        var changes = {
            added: [],
            modified: [],
            deleted: []
        };
        var links = myDiagram.model.linkDataArray;


        for (var i = 0; i < links.length; i++) {
            var found = false;
            for (var j = 0; j < initialLinks.length; j++) {
                if (initialLinks[j].to == links[i].to && initialLinks[j].from == links[i].from) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.added.push(links[i]);
            }
        }

        for (var i = 0; i < initialLinks.length; i++) {
            var found = false;
            for (var j = 0; j < links.length; j++) {
                if (initialLinks[i].to == links[j].to && initialLinks[i].from == links[j].from) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                //console.log('not found');
                changes.deleted.push(initialLinks[i]);
            }
        }

        for (var i = 0; i < initialLinks.length; i++) {
            var found = false;
            for (var j = 0; j < links.length; j++) {
                if (initialLinks[i].from == links[j].from && initialLinks[i].to == links[j].to) {

                    var l1 = (initialLinks[i].predicateId || '').toString();
                    var l2 = (links[j].predicateId || '').toString();

                    //console.log(l1 + ', ' + l2);
                    if (l1 != l2) {
                        found = true;
                        //console.log(l2);
                    }

                    break;
                }
            }
            if (found) {

                //console.log('"' + initialLinks[i].predicateId + '"');
                //console.log('"' + links[j].predicateId + '"');
                changes.modified.push(links[j]);
            }
        }

        return changes;
    }

    function getNodeChanges() {
        var changes = {
            added: [],
            modified: [],
            deleted: []
        };

        var nodes = myDiagram.model.nodeDataArray;

        //added
        for (var i = 0; i < nodes.length; i++) {
            var found = false;
            for (var j = 0; j < initialNodes.length; j++) {
                if (initialNodes[j].key == nodes[i].key) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.added.push(nodes[i]);
            }
        }

        //deleted
        for (var i = 0; i < initialNodes.length; i++) {
            var found = false;
            for (var j = 0; j < nodes.length; j++) {
                if (initialNodes[i].key == nodes[j].key) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.deleted.push(initialNodes[i]);
            }
        }

        //modified
        for (var i = 0; i < nodes.length; i++) {
            for (var j = 0; j < initialNodes.length; j++) {
                if (initialNodes[j].id == nodes[i].id) {

                    if (initialNodes[j].key === nodes[i].key) {
                        //changes.modified.push(nodes[i]);
                        break;
                    }
                }
            }
        }

        //console.log(changes);
        return changes;

    }

    function getObjectsBySearch() {
        var typeSelect = $('#' + controlID_add_artifact_type).val().split('|');
        var data = {
            type: typeSelect[0].replace('Type', ''),
            id: typeSelect[1],
            search: $('#' + controlID_add_search_text).val()
        };


        $.ajax({
            url: '/Diagrams/ObjectsBySearch',
            data: data,
            success: function (data) {
                $('#' + controlID_add_search_message).hide();
                myPalette.model.nodeDataArray = [];
                temp = data[0];
                for (var i = 0; i < data.length; i++) {
                    var d = createNodeModel();

                    d.backColor = data[i].backColor;
                    d.foreColor = data[i].foreColor;
                    d.id = data[i].id;
                    d.name = data[i].name;
                    d.object = data[i].object;
                    d.typeName = data[i].typeName;
                    d.url = data[i].url;
                    d.template = "Artifact";
                    d.type = data[i].object;
                    d.objecttype = data[i].objecttype;
                    d.objecttypeid = data[i].objecttypeid;
                    d.key = data[i].type + data[i].id.toString();
                    d.isDeletable = true;

                    myPalette.model.addNodeData(d);
                }
                if (data.length < 1) {
                    $('#' + controlID_add_search_message).show();
                }
            },
            async: false
        });

        if (temp == null) {
            return;
        }
        var data = {
            type1: $('#' + controlID_add_artifact_type + ' option:selected').text(),
            type2: ''
        };

        populatePredicateList();
        myPalette.scale = 1.0;
    }

    function initializeDiagram() {
        var dg = g(go.Diagram, controlID_diagram, {
            initialContentAlignment: go.Spot.Left,
            //autoScale: go.Diagram.UniformToFill,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: g(go.LayeredDigraphLayout, { direction: 0, columnSpacing: 50, layerSpacing: 50 }),
            "undoManager.isEnabled": true
        });
        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = "template";
        dg.model.linkFromPortIdProperty = "frompid";
        dg.model.linkToPortIdProperty = "topid";
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !readonly;
        dg.model.isReadOnly = readonly;
        //dg.isReadOnly = readonly;

        return dg;
    }

    function initializeOverview(diagram) {
        var ov = g(go.Overview, controlID_overview,
        { observed: diagram, contentAlignment: go.Spot.Center });

        return ov;
    }

    function initializePalette() {

        var pl = g(go.Palette, controlID_palette, {
            contentAlignment: go.Spot.TopCenter
            , allowDrop: true
            , initialAutoScale: go.Diagram.Uniform
            , model: new go.GraphLinksModel([{ template: "Artifact", backColor: 'black', foreColor: 'white', name: '', id: -1, key: -1, typeName: '', type: '', isDeletable: true }])
        })
        pl.model.nodeCategoryProperty = 'template';
        pl.model.nodeDataArray = [];
        pl.model.class = 'go.GraphLinksModel';
        pl.layout.spacing = new go.Size(3, 3);
        return pl;
    }

    function onLayoutCompleted() {
        //console.log(myDiagram.documentBounds.height);
        //console.log(myDiagram.viewportBounds.height);
        //var height = $('#' + controlID_diagram).height($(window).innerHeight());
    }

    function makePort(name, leftside) {
        var port = g(go.Shape, "Circle", {
            fill: "white",
            stroke: "gray",
            strokeWidth: 3,
            desiredSize: new go.Size(9, 9),
            portId: name, // declare this object to be a "port"
            //toMaxLinks: 1, // don't allow more than one link into a port
            cursor: "pointer" // show a different cursor to indicate potential link point
        });

        var panel = g(go.Panel, "Horizontal", {
            margin: new go.Margin(2, 0)
        });

        if (leftside) {
            port.toSpot = go.Spot.Left;
            port.toLinkable = true;
            panel.alignment = go.Spot.TopLeft;
            panel.add(port);
        } else {
            port.fromSpot = go.Spot.Right;
            port.fromLinkable = true;
            panel.alignment = go.Spot.TopRight;
            panel.add(port);
        }
        return panel;
    }

    function makeTemplate(obj, w, h, borderColor, fontSize, inports, outports) {//, alignment) {

        //if (!alignment) alignment = go.Spot.Center;

        var node = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: w,
            height: h
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: borderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "backColor").makeTwoWay()
       ),
        g(go.Panel,
            go.Panel.Horizontal,
            {
                alignment: go.Spot.BottomLeft,
                margin: 5
            },
            makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", fontSize),
            makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", fontSize),
            makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", fontSize)
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(w - 20, h - 10),
                font: "bold " + fontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "foreColor").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (fontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "foreColor").makeTwoWay(),
                new go.Binding("text", "typeName").makeTwoWay()
            )//,
            //g(go.TextBlock, {
            //    row: 2,
            //    margin: 3,
            //    maxSize: new go.Size(180, NaN),
            //    font: "bold 10pt sans-serif"
            //},
            //    new go.Binding("text", "role").makeTwoWay(),
            //    new go.Binding("stroke", "fore").makeTwoWay()
            //)
        )),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Left,
            alignmentFocus: new go.Spot(0, 0.5, -8, 0)
        },
        inports),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        outports));

        myDiagram.nodeTemplateMap.add(obj, node);
    }

    function makeSearchTemplate() {
        var node = g(go.Node, "Spot",
               {
                   mouseEnter: mouseEnter,
                   mouseLeave: mouseLeave
               },
           g(go.Panel, "Auto", {
               width: 250,
               height: 22,
               name: "NodePanel"
           },
           g(go.Shape, "RoundedRectangle", {
               stroke: 'transparent',
               strokeWidth: 2,
               spot1: go.Spot.TopLeft,
               spot2: go.Spot.BottomRight,
               name: "NodeShape"
           },
               new go.Binding("fill", "backColor").makeTwoWay()
          ),
           g(go.Panel, "Table",
               g(go.TextBlock, {
                   row: 0,
                   margin: 3,
                   alignment: go.Spot.Top,
                   editable: false,
                   maxSize: new go.Size(250, 22),
                   font: "8pt sans-serif"
               },
                   new go.Binding("text", "name").makeTwoWay()
                   , new go.Binding("stroke", "foreColor").makeTwoWay()
               ))
           ));

        myPalette.nodeTemplateMap.add("Artifact", node);
    }

    function makeIconPanel(icon, tooltip, binding, fontSize) {
        var iconPanel = g(go.Panel,
         "Auto",
         {
             alignment: go.Spot.Center,
             margin: 2
         },
              g(go.Shape, "Circle",
                 {
                     stroke: null,
                     toolTip: g(go.Adornment, "Auto", g(go.Shape, { fill: "lightyellow" }), g(go.Panel, "Vertical", g(go.TextBlock, { margin: 3, text: tooltip })))
                 },
             new go.Binding("fill", "foreColor")),
         g(go.TextBlock,
             {
                 row: 0,
                 margin: 0,
                 alignment: go.Spot.Center,
                 editable: false,
                 font: (fontSize) + "pt FontAwesome",
                 text: icon
             },
             new go.Binding("stroke", "backColor")
         ),
        new go.Binding("visible", binding)
       );

        return iconPanel;
    }

    function markForDeletion(set) {
        //console.log(set.count);
        myDiagram.startTransaction("markSelection");

        //get a deep copy of the set as an array
        var sel = $.extend(true, [], set.toArray());//JSON.parse(JSON.stringify(set.toArray()));
        //initialLinks = JSON.parse(JSON.stringify(linkList));
        for (var i = 0; i < sel.length; i++) {
            var obj = sel[i].data;

            if (obj == null)
                continue;

            if (obj.diagramObjectType == 'Node') {
                var affectedLinks = [];
                for (var j = 0; j < myDiagram.model.linkDataArray.length; j++) {
                    var link = myDiagram.model.linkDataArray[j];
                    //console.log(link);
                    if (link.to == obj.key || link.from == obj.key) {
                        affectedLinks.push(link);
                    }
                }

                for (var j = 0; j < affectedLinks.length; j++) {
                    myDiagram.model.removeLinkData(affectedLinks[j]);
                }

                myDiagram.model.removeNodeData(obj);
            } else if (obj.diagramObjectType == 'Link') {
                myDiagram.model.removeLinkData(obj);
                //console.log('remove: ' + obj.id);
            }

        }
        myDiagram.commitTransaction("markSelection");
    }

    function mouseEnter(e, node) {
        node.isShadowed = true;
    };

    function mouseLeave(e, node) {
        node.isShadowed = false;
    };

    function onDoubleClick(e) {

        var obj = e.diagram.selection.first().data;
        if (obj != null) {
            if (obj.diagramObjectType == 'Node') {


                var message = $('<p />', { text: 'You are about to navigate to a different lineage diagram. You will lose any unsaved changes. Continue?' }),
                 ok = $('<button />', {
                     text: 'Okay',
                     'class': 'btn qtip-blue qtip-btn-inline',
                 }),
                 cancel = $('<button />', {
                     text: 'Cancel',
                     'class': 'btn qtip-blue qtip-btn-inline',
                 });
                if (checkModified()) {
                    confirmDialog(message.add(ok).add(cancel), 'Confirm Navigation', 'Okay', function () {
                        type = obj.type;
                        id = obj.id;
                        populateDiagram();
                    });
                } else {
                    type = obj.type;
                    id = obj.id;

                    populateDiagram();
                    $('#' + controlID_ribbon_remove).hide(200);
                }
            }
            else if (obj.diagramObjectType == 'Link' && !readonly) {
                overlayEditLinkKey = obj.key;
                showRelationshipOverlay(obj);
            }
        }
    }

    function toggleTabs(data) {
        var first = -1;
        var delay = 0;

        if (data == null) {
            $("#" + controlID_info).jqxExpander('collapse');
            $("#" + controlID_info_body).html('');
            $("#" + controlID_info_detail_wrapper).hide();
            $("#" + controlID_info_detail).html('');

            $('#' + controlID_tabs).hide(delay);
            for (var i = 0; i < tabs.length; i++) {
                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + i + ")").css("display", "none");
            }

        } else {
            $('#' + controlID_tabs).show(delay);
            if (data.diagramObjectType == 'Node') {
                first = tabs["responsibilities"];

                $("#" + controlID_info_detail_wrapper).hide();
                $("#" + controlID_info_detail).html('');

                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["responsibilities"] + ")").css("display", "block");
                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["fusion"] + ")").css("display", "block");

                    try {
                        technicalRelationsSource.url = null;
                        $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                    } catch (e) { }
                    try {
                        lineageResponsibilitySource.url = null;
                        $('#' + controlID_responsibilities_content).jqxGrid('updatebounddata');
                    } catch (e) { }


                $.ajax({
                    url: '/resources/' + data.type + '/' + data.id + '/templates/tooltip/Preview',
                    async: true
                }).done(function (data) {
                    $('#' + controlID_info_body).html(data);
                    $("#" + controlID_info).jqxExpander('expand');
                }).fail(function () {
                    $('#' + controlID_info_body).html('');
                    $("#" + controlID_info).jqxExpander('collapse');
                });

                if (data.hasMappingRules) {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "block");
                    $("#" + controlID_mappingrules_content).html(defaultTabContent);
                    first = tabs["mappingrules"];
                } else { 
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "none");
                }

                if (data.hasSourceRules) {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["sourcerules"] + ")").css("display", "block");
                    $("#" + controlID_sourcerules_content).html(defaultTabContent);
                    first = tabs["sourcerules"];
                } else {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["sourcerules"] + ")").css("display", "none");
                }

            } else if (data.diagramObjectType == 'Link') { //
                var from = myDiagram.model.findNodeDataForKey(data.from);
                var to = myDiagram.model.findNodeDataForKey(data.to);
                first = tabs["fusion"];

                //if (permissions.HasPermission("Root", "Update")) {
                //    TileTools("#" + controlID_info_table_add, [
                //        { icon: 'plus', uri: '/form/AddFieldType?type=' + 'IntersectType' + '&id=' + data.intersectTypeId, context: contextList.FieldType, title: 'Add definition attribute' }
                //    ]);
                //} else {
                //    $("#" + controlID_info_table_add).html('');
                //}

                var intersectId = 0;

                if (from != null && from.intersectMapId == data.key)
                    intersectId = from.intersectId;
                if (to != null && to.intersectMapId == data.key)
                    intersectId = to.intersectId;

                $.ajax({
                    url: '/resources/IntersectType/' + data.intersectTypeId + '/templates/tooltip/Preview',
                    async: true
                }).done(function (data) {
                    $('#' + controlID_info_body).html(data);
                    $("#" + controlID_info).jqxExpander('expand');
                }).fail(function () {
                    $('#' + controlID_info_body).html('');
                    $("#" + controlID_info).jqxExpander('collapse');
                });

                ObjectDetail(controlID_info_detail, 'Intersect', intersectId, true);

                if (permissions.HasPermission("Root", "Update") && intersectId != 0) {
                    TileTools("#" + controlID_info_detail_edit, [
                        { icon: 'pencil', uri: '/form/EditRelationship?id=' + intersectId, context: 'intersectform', title: 'Edit Relationship' }
                         //,{ icon: 'plus', uri: '/form/AddFieldType?type=' + 'IntersectType' + '&id=' + data.intersectTypeId, context: contextList.FieldType, title: 'Add definition attribute' }
                    ]);
                } else {
                    $("#" + controlID_info_detail_edit).html('');
                }

                //$('#' + controlID_popover_edit_relationship).load('/form/EditRelationship?id=' + intersectId, function (response, status, xhr) {
                //});

                //amplify.publish('intersectform');


                $("#" + controlID_info_detail_wrapper).show();
                

                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["responsibilities"] + ")").css("display", "none");
                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["fusion"] + ")").css("display", "block");

                try {
                    technicalRelationsSource.url = null;
                    $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                } catch (e) { }
                try {
                    lineageResponsibilitySource.url = null;
                    $('#' + controlID_responsibilities_content).jqxGrid('updatebounddata');
                } catch (e) { }

                if (data.hasMappingRules) {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "block");
                    $("#" + controlID_mappingrules_content).html(defaultTabContent);
                    first = tabs["mappingrules"];
                } else {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "none");
                }

                if (to.hasSourceRules) {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["sourcerules"] + ")").css("display", "block");
                    $("#" + controlID_sourcerules_content).html(defaultTabContent);
                    first = tabs["sourcerules"];
                } else {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["sourcerules"] + ")").css("display", "none");
                }


            }
        }

        if (first == -1)
            $("#" + controlID_tabs).hide(delay);
        else
        {
            $("#" + controlID_tabs).jqxTabs('select', first);
            loadTab(first);
        }
    }

    function loadTab(index) {
        if (index < 0 || index > tabs.length || selectedData == null)
            return;

        var from = null;
        var to = null;
        var url = '';

        if (selectedData.diagramObjectType == 'Node') {

        } else {
            from = myDiagram.model.findNodeDataForKey(selectedData.from);
            to = myDiagram.model.findNodeDataForKey(selectedData.to);
        }

        switch(index)
        {
            case tabs["fusion"]:
                url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + type + '&sID=' + id + '&t=' + selectedData.type + '&tID=' + selectedData.id;
                if (selectedData.diagramObjectType != 'Node') {
                    url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + from.type + '&sID=' + from.id + '&t=' + to.type + '&tID=' + to.id;
                }
                if (technicalRelationsSource.url != null)
                    return;
                        try {
                            technicalRelationsSource.url = url;
                            $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                        } catch (e) {  }
                break;
            case tabs["responsibilities"]:
                if (lineageResponsibilitySource.url != null)
                    return;

                        try {
                            lineageResponsibilitySource.url = '/api/' + selectedData.type + '/' + selectedData.id + '/ownership?showHidden=false';
                            $('#' + controlID_responsibilities_content).jqxGrid('updatebounddata');
                        } catch (e) { }
                break;
            case tabs["sourcerules"]:
                if ($("#" + controlID_sourcerules_content).html().toString() != defaultTabContent) {
                    return;
                }

                url = '/api/' + type + '/' + id + '/sources/' + selectedData.type + '/' + selectedData.id + '/rules';
                if (selectedData.diagramObjectType != 'Node') {
                    url = '/api/' + type + '/' + id + '/' + from.type + '/' + from.id + '/' + to.type + '/' + to.id + '/rules';
                }
                $.ajax({
                    url: url,
                    async: true
                }).done(function (data) {
                    var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                    $('#' + controlID_sourcerules_content).html(sourceTemplate(data));
                }).fail(function () {
                    $('#' + controlID_sourcerules_content).html(defaultTabContent);
                });
                break;
            case tabs["mappingrules"]:
                if ($("#" + controlID_mappingrules_content).html().toString() != defaultTabContent) {
                    return;
                }
                url = '/form/sourcetarget/load/' + type + '/' + id + '/' + selectedData.type + '/' + selectedData.id + '/' + selectedData.type + '/' + selectedData.id;
                if (selectedData.diagramObjectType != 'Node') {
                    url = '/form/sourcetarget/load/' + type + '/' + id + '/' + from.type + '/' + from.id + '/' + to.type + '/' + to.id;
                }

                $.ajax({
                    url: url
                }).done(function (data) {
                    for (var i = 0; i < data.items.length; i++) {
                        data.items[i].index = i + 1;
                    }
                    var sourceTemplate = Handlebars.getTemplate('LineageDiagramMappingRules');
                    $('#' + controlID_mappingrules_content).html(sourceTemplate(data));
                }).fail(function () {
                    $('#' + controlID_mappingrules_content).html(defaultTabContent);
                });
                break;
        }
    }

    function toggleButtons(data) {
        //console.log(data);
        var delay = 0;

        if (!readonly) {
            $("#" + controlID_ribbon_add).show(delay);
        } else {
            $("#" + controlID_ribbon_add).hide(delay);
        }
        if (data == null) {
            $("#" + controlID_ribbon_sourcerule_add).hide(delay);
            $("#" + controlID_ribbon_sourcemapping_add).hide(delay);
            $("#" + controlID_ribbon_remove).hide(delay);
        } else {
            if (data.diagramObjectType == 'Node') {
                if (!readonly) {
                    $("#" + controlID_ribbon_sourcemapping_add).show(delay);
                    $("#" + controlID_ribbon_sourcerule_add).show(delay);
                    $("#" + controlID_ribbon_remove).show(delay);
                } else {
                    $("#" + controlID_ribbon_sourcemapping_add).hide(delay);
                    $("#" + controlID_ribbon_sourcerule_add).hide(delay);
                    $("#" + controlID_ribbon_remove).hide(delay);
                }
            } else {
                $("#" + controlID_ribbon_sourcerule_add).hide(delay);

                if (!readonly) {
                    $("#" + controlID_ribbon_sourcemapping_add).show(delay);
                    $("#" + controlID_ribbon_remove).show(delay);
                } else {
                    $("#" + controlID_ribbon_sourcemapping_add).hide(delay);
                    $("#" + controlID_ribbon_remove).hide(delay);
                }
            }
        }
    }

    function onSelectionChange(e) {
        selection = e.diagram.selection;

        if (selection.count == 0) {
            selectedData = null;
        } else {
            //get a deep copy of the selection as an array
            var sel = $.extend(true, [], selection.toArray());

            if (sel != null && sel.length != 0) {
                selectedData = sel[0].data;
            }
        }

        toggleTabs(selectedData);
        toggleButtons(selectedData);

    }

    function onViewportBoundsChanged() {
        var s = myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        //console.log('vpchanged');
        $('#' + controlID_ribbon_zoom_text).text(Math.round(myDiagram.scale * 100) + '%');
        $('#' + controlID_ribbon_zoom_slider).val(Math.round(myDiagram.scale * 1500));

        //console.log(myDiagram.div.style.height);
    };

    function onLinkDrawn(e) {
        overlayEdit = false;

        newLink = e.subject.data;
        newLink.diagramObjectType = "Link";
        var fromNode = myDiagram.model.findNodeDataForKey(e.subject.data.from);
        var toNode = myDiagram.model.findNodeDataForKey(e.subject.data.to);

        var results = $.ajax({
            url: '/diagrams/GetPredicateInfoByTypes?type1=' + fromNode.objecttype + '&id1=' + fromNode.objecttypeid + '&type2=' + toNode.objecttype + '&id2=' + toNode.objecttypeid + '&mapType=1',
            data: null
        }).done(function (data, status, xhr) {
            if (data.length > 0) {
                $('#' + controlID_overlay_relationship).html('<span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (fromNode.foreColor || 'black')
                    + ';background-color: '
                    + (fromNode.backColor || 'white')
                    + ';" >'
                    + fromNode.typeName
                    + '</span><span style="font-size:1.5rem;font-weight:800;color:grey">&#8594;</span><span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (toNode.foreColor || 'black')
                    + ';background-color: '
                    + (toNode.backColor || 'white')
                    + ';">'
                    + toNode.typeName + '</span>');

                $('#' + controlID_overlay_add).show();

                //var data = {
                //    type1: fromNode.typeName,
                //    type2: toNode.typeName
                //};
                populatePredicateList(data);

                $('#' + controlID_overlay).show();
            }
            else {
                amplify.publish('ShowMessage', { type: 'error', title: 'Not allowed', message: 'No relationship type exists between ' + fromNode.typeName + ' and ' + toNode.typeName + ' that has any lineage predicates assigned.' });
                e.diagram.remove(e.subject);
            }
        });
    }

    function showRelationshipOverlay(linkData) {

        var deferred = $.Deferred();

        newLink = linkData;

        newLink.diagramObjectType = "Link";
        var fromNode = myDiagram.model.findNodeDataForKey(linkData.from);
        var toNode = myDiagram.model.findNodeDataForKey(linkData.to);

        var results = $.ajax({
            url: '/diagrams/GetPredicateInfoByTypes?type1=' + fromNode.objecttype + '&id1=' + fromNode.objecttypeid + '&type2=' + toNode.objecttype + '&id2=' + toNode.objecttypeid + '&mapType=1',
            data: null
        }).done(function (data, status, xhr) {
            if (data.length > 0) {
                $('#' + controlID_overlay_relationship).html('<span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (fromNode.foreColor || 'black')
                    + ';background-color: '
                    + (fromNode.backColor || 'white')
                    + ';" >'
                    + fromNode.typeName
                    + '</span><span style="font-size:1.5rem;font-weight:800;color:grey">&#8594;</span><span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (toNode.foreColor || 'black')
                    + ';background-color: '
                    + (toNode.backColor || 'white')
                    + ';">'
                    + toNode.typeName + '</span>');

                $('#' + controlID_overlay_add).show();

                //var data = {
                //    type1: fromNode.typeName,
                //    type2: toNode.typeName
                //};
                populatePredicateList(data);

                $('#' + controlID_overlay).show();
                return true;
            }
            else {
                return false;
            }
        });
        
        return deferred.promise();
    }

    function onChange(e) {
        checkModified();

    }

    function onDeleting(e) {
        if (readonly) {
            e.cancel = true;
            return;
        }
        markForDeletion(selection);
        $('#' + controlID_ribbon_expander).jqxExpander('expand');
        //$('#' + controlID_ribbon).jqxRibbon('selectAt', 1);
    };

    function onDeleted(e) {
        selection = null;
    }

    function onDrop(e) {
        $('#' + controlID_popover_add).hide();
    }

    function parseData(data) {
        myDiagram.startTransaction("load_all_data");
        myDiagram.model.nodeDataArray = [];
        myDiagram.model.linkDataArray = [];
        initialNodes = [];
        initialLinks = [];
        var modelList = [];
        var linkList = [];
        $('#' + controlID_message).hide();

        for (var i = 0; i < data.nodes.length; i++) {

            var d = data.nodes[i];
            var model = createNodeModel();

            var isFocalPoint = (d.obj == type && d.objid == id);// && d.level == 0);

            if (isFocalPoint) {
                $('#' + controlID_header).text('Lineage: ' + d.name);
            }

            model.template = isFocalPoint ? "FocalArtifact" : "Artifact";
            model.key = d.key;
            model.id = d.objid;
            model.objecttype = d.objecttype;
            model.objecttypeid = d.objecttypeid;
            model.type = d.obj;
            model.level = d.level;
            model.name = d.name;
            model.typeName = d.type;
            model.foreColor = d.fore;
            model.backColor = d.back;
            model.diagramObjectType = "Node";
            model.exclude = d.exclude.toString();
            model.intersectMapId = d.intersectMapId;
            model.intersectId = d.intersectId;
            model.sourceRuleCount = d.sourceRuleCount;
            model.mappingRuleCount = d.mappingRuleCount;
            model.hasSourceRules = (d.sourceRuleCount > 0);
            model.hasMappingRules = (d.mappingRuleCount > 0);
            model.challengeCount = d.challengeCount;
            model.hasChallenges = (d.challengeCount > 0);
            modelList.push(model);
        }

        for (var i = 0; i < data.links.length; i++) {
            var d = data.links[i];
            var link = createLinkModel();
            link.id = d.id;
            link.intersectTypeId = d.intersectTypeId;
            link.key = d.id;
            link.from = d.from;
            link.to = d.to;
            link.text = d.text;
            link.predicateId = d.predicateId;
            link.diagramObjectType = "Link";
            link.sourceMappingCount = d.mappingRuleCount;
            link.hasMappingRules = (d.mappingRuleCount > 0);
            linkList.push(link);
        }

        for (var i = 0; i < modelList.length; i++) {
            myDiagram.model.addNodeData(modelList[i]);
        }
        for (var i = 0; i < linkList.length; i++) {
            myDiagram.model.addLinkData(linkList[i]);
        }

        //get deep copy of lists
        initialNodes = $.extend(true, [], modelList);//JSON.parse(JSON.stringify(modelList));
        initialLinks = $.extend(true, [], linkList); //JSON.parse(JSON.stringify(linkList));

        //set buttons/expanders to defaults
        toggleTabs(null);
        toggleButtons(null);

        myDiagram.commitTransaction("load_all_data");
        reOrderLayout();

    }

    function populateDiagram() {
        var results = $.ajax({
            url: '/relations/' + type + '/' + id + '/sources',
            data: null
        }).done(function (data, status, xhr) {
            //console.log('populate');
            //myDiagram = initializeDiagram();
            parseData(data);
            reOrderLayout();
            myDiagram.zoomToFit();
        });
    }

    function populatePredicateList(predicates) {
        var output = [];

        if (predicates) {
            output.push('<option value="0_1"></option>');
            for (var i = 0; i < predicates.length; i++) {
                output.push('<option value="' + predicates[i].id.toString() + '">' + predicates[i].name + '</option>');
            }
            $('#' + controlID_overlay_predicates).html(output.join(''));
        }
        else {
            $.ajax({
                url: '/diagrams/getpredicateinfo?type=1',
                success: function (data) {
                    //predicates = [];
                    output.push('<option value="0_1"></option>');
                    for (var i = 0; i < data.length; i++) {
                        output.push('<option value="' + data[i].id.toString() + '">' + data[i].name + '</option>');
                      //  predicates.push(data[i]);
                    }
                    $('#' + controlID_overlay_predicates).html(output.join(''));
                }
            });
        }
    }

    function populateTypeSelectList() {
        var html = '';

        $.ajax({
            url: '/services/glossary/artifacts?$orderby=Name',
            data: null,
            success: function (data) {
                $('#' + controlID_add_artifact_type).html('');
                var output = [];
                for (var i = 0; i < data.length; i++) {
                    output.push('<option value="ArtifactType|' + data[i].ID + '">Glossary :: ' + data[i].Name + '</option>');
                }
                html += output.join('');
            }
        }).then(function () {
            $.ajax({
                url: '/services/glossary/models?$orderby=Name',
                data: null,
                success: function (data) {
                    $('#' + controlID_add_artifact_type).html('');
                    var output = [];
                    for (var i = 0; i < data.length; i++) {
                        output.push('<option value="TaxonomyType|' + data[i].ID + '">Model :: ' + data[i].Name + '</option>');
                    }
                    html += output.join('');
                }
            }).then(function () {
                $('#' + controlID_add_artifact_type).html(html);
            });
        });
    }

    function reOrderLayout() {
        myDiagram.layout.invalidateLayout();
        myDiagram.requestUpdate();
    }

    function resetOverlay() {
        $('#' + controlID_overlay_predicates).val(0);
        $('#' + controlID_overlay_add).prop('disabled', true);
    }

    function rotateDiagram() {
        myDiagram.startTransaction("rotate");
        digraphDirection = (digraphDirection + 90) % 360;
        myDiagram.layout.direction = digraphDirection;
        myDiagram.layout.setsPortSpots = true;
        myDiagram.commitTransaction("rotate");
    }

    function saveChanges() {

        if (readonly) return;

        $('#' + controlID_ribbon_save).jqxButton({ disabled: true });

        var nodeChanges = getNodeChanges();
        var linkChanges = getLinkChanges();

        var flagError = false;
        var errors = "";

        var promises = [];

        for (var i = 0; i < nodeChanges.deleted.length; i++) {
            var node = nodeChanges.deleted[i];
            var data = {
                target: type,
                targetID: id,
                id: node.intersectMapId
            };

            promises.push($.ajax({
                async: true,
                method: 'DELETE',
                url: '/relations/' + data.target + '/' + data.targetID + '/sources/' + data.id
            }).done(function (data, status, xhr) {
                if (!data.success) {
                    flagError = true;
                    errors += data.message;
                }
            }).fail(function (xhr, status, error) {
                flagError = true;
                errors += data.message;
            }));

        }

        for (var i = 0; i < linkChanges.added.length; i++) {
            var link = linkChanges.added[i];
            var to = myDiagram.model.findNodeDataForKey(link.to);
            var from = myDiagram.model.findNodeDataForKey(link.from);
            var predicate = link.predicateId;

            var source = {
                target: type,
                targetID: id,
                subject: from.type,
                subjectID: from.id,
                object: to.type,
                objectID: to.id,
                predicateID: link.predicateId
            };

            promises.push($.ajax({
                url: '/Relations/sources',
                async: true,
                data: JSON.stringify(source),
                processData: false,
                type: 'POST',
                contentType: "application/json; charset=utf-8",
                dataType: "json"
            }).done(function (data) {
                if (!data.success) {
                    flagError = true;
                    errors += data.message;
                }
            }).fail(function (data) {
                flagError = true;
                errors += data.message;
            }));

        }

        for (var i = 0; i < linkChanges.modified.length; i++) {
            var data = {
                intersectMapID: linkChanges.modified[i].id,
                predicateID: linkChanges.modified[i].predicateId
            };

            if (data.intersectMapID == null || data.predicateID == null)
                continue;

            promises.push($.ajax({
                url: '/relations/update/' + data.intersectMapID + '/' + data.predicateID,
                async: true
            }).fail(function (data) {
                flagError = true;
                errors += data.message;
            }));

        }

        $.when.apply($, promises).done(function () {
            if (flagError) {
                amplify.publish("SourceFormStatus", { title: 'An error occurred while saving changes.', message: errors, success: false });
            } else {
                amplify.publish("SourceSave");
                deletedNodes = [];
                populateDiagram();
                $('#' + controlID_ribbon_save_spinner).hide();
            }
        });
    }

    //#endregion

    //#region Constructor Logic

    $("#" + controlID_message).hide();

    if (readonly) {
        $('#' + controlID_ribbon_undo).hide();
        $('#' + controlID_ribbon_redo).hide();
        $('#' + controlID_ribbon_add).hide();
        $('#' + controlID_ribbon_remove).hide();
        $('#' + controlID_ribbon_save).hide();
    } else {
        $("#" + controlID_ribbon_remove).jqxButton({ theme: theme });
        $("#" + controlID_ribbon_remove).on('click', function () {
            //console.log(selection.length);
            markForDeletion(selection);
            $("#" + controlID_ribbon_remove).hide(200);
            //populateDiagram();
        });
    }

    var g = go.GraphObject.make;
    var myDiagram = initializeDiagram();

    myDiagram.addDiagramListener('ViewportBoundsChanged', onViewportBoundsChanged);
    myDiagram.addDiagramListener('ChangedSelection', onSelectionChange);
    myDiagram.addDiagramListener('ObjectDoubleClicked', onDoubleClick);
    myDiagram.addDiagramListener('LayoutCompleted', onLayoutCompleted);
    myDiagram.addDiagramListener('LinkDrawn', onLinkDrawn);
    myDiagram.addDiagramListener('SelectionDeleting', onDeleting);
    myDiagram.addDiagramListener('SelectionDeleted', onDeleted);
    myDiagram.addDiagramListener('ExternalObjectsDropped', onDrop);
    myDiagram.model.addChangedListener(onChange);

    myDiagram.grid.visible = false;
    myDiagram.grid.gridCellSize = new go.Size(8, 8);
    myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
    myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

    makeTemplate("FocalArtifact", 275, 150, '#000000', 14, [makePort("IN", true)], [makePort("OUT", false)]);
    makeTemplate("Artifact", 225, 105, 'transparent', 10, [makePort("IN", true)], [makePort("OUT", false)]);

    myDiagram.linkTemplate = g(
        go.Link, { routing: go.Link.AvoidsNodes, curve: go.Link.JumpOver, corner: 10, relinkableFrom: false, relinkableTo: false }, // the whole link panel
        g(go.Shape, { stroke: "gray", strokeWidth: 2 },
            new go.Binding("strokeWidth", "hasMappingRules", function (h) { return h ? 3 : 2;}),
            new go.Binding("stroke", "hasMappingRules", function (h) { return h ? "black" : "gray" })), // the link shape
        g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" }), // the arrowhead
        g(go.Panel, "Auto",
            g(go.Shape, { visible: false, fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }), stroke: null },
                //only visible if there's a label
                new go.Binding("visible", "text", function (a) { return (a ? true : false) })
            ), // the link shape
            g(go.TextBlock, { textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4 },   // the label
                new go.Binding("text", "text").makeTwoWay()
             )
        )
    );

    var myPalette = initializePalette();

    makeSearchTemplate();

    var myOverview = initializeOverview(myDiagram);

    populateDiagram();
    populateTypeSelectList();

    //#endregion

    //#region Amplify Subscribes

    amplify.subscribe("SaveAction", function (data) {
        try {
            switch (data.context) {
                case 'mappingrule':
                    if (data.source && data.sourceID && data.target && data.targetID && data.count != null) {
                        console.log(data);
                        var ix = -1;
                        var obj = null;

                        if (data.source == data.target && data.sourceID == data.targetID) {
                            var ix = findNodeIndexByObject(data.source, data.sourceID);
                            if (ix > -1)
                                obj = myDiagram.model.nodeDataArray[ix];
                        } else {
                            var ix = findLinkIndexByObjects(data.source, data.sourceID, data.target, data.targetID);
                            if (ix > -1)
                                obj = myDiagram.model.linkDataArray[ix];
                        }
                        if (obj != null) {
                            myDiagram.model.setDataProperty(obj, "sourceMappingCount", data.count);
                            myDiagram.model.setDataProperty(obj, "hasMappingRules", (data.count > 0 ? true : false));
                        }
                    }
                    toggleTabs(selectedData);
                    break;
                case 'sourcerule':
                    if (data.action && data.object && data.objectid) {
                        if (data.action == 'add') {
                            
                            var ix = findNodeIndexByObject(data.object, data.objectid);
                            if (ix > -1) {
                                var node = myDiagram.model.nodeDataArray[ix];
                                var count = node.sourceRuleCount;
                                count++;
                                myDiagram.model.setDataProperty(myDiagram.model.nodeDataArray[ix], "sourceRuleCount", count);
                                myDiagram.model.setDataProperty(myDiagram.model.nodeDataArray[ix], "hasSourceRules", true);
                            }
                        }
                    }
                    toggleTabs(selectedData);
                    break;
            }
        } catch (e) {
            logError("LineageDiagram : SaveAction", e);
        }
    });

    amplify.subscribe("RelationshipCancel", function (data) {
        if (!$('#TreeGridItemViewer').length) {
            $('#Overlay').remove();
            $('#OverlayBackground').remove();
        }
    });

    amplify.subscribe("RelationshipSave", function (data) {
        if (!$('#TreeGridItemViewer').length) {
            $('#Overlay').remove();
            $('#OverlayBackground').remove();
        }
        toggleTabs(selectedData);
    });
    //#endregion
}

