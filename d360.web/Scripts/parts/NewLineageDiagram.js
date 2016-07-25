function NewLineageDiagram(controlID, type, id, readonly) {
    var originalObject = type;
    var originalObjectID = id;
    var viewID = 1;
    var fullscreen = false;
    var selectedData = null;
    var permissions = new PermissionsModel();
    permissions.GetPermissionsForObject(type, id);

    var tmpl = Handlebars.getTemplate('NewLineageDiagram');
    $('#' + controlID).html(tmpl({ control: controlID }));

    var lineageModel;
    var mapRulesModel;
    var transformationModel;

    //#region Control constants

    var ribbon_button_width = 58;
    var ribbon_button_height = "90%";

    var controlID_splitter = controlID + '_splitter';

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

    //var controlID_add_search_text = controlID + '_add_search_text';
    //var controlID_add_search = controlID + '_add_search';
    //var controlID_add_artifact_type = controlID + '_add_artifact_type';
    //var controlID_add_search_message = controlID + '_add_search_message';

    var controlID_overlay_existing = controlID + '_overlay_existing';
    var controlID_overlay_new = controlID + '_overlay_new';
    var controlID_overlay_relationship = controlID + '_overlay_relationship';
    var controlID_overlay_predicates = controlID + '_overlay_predicates';
    var controlID_overlay_transformation = controlID + '_overlay_transformation';
    var controlID_overlay_cancel = controlID + '_overlay_cancel';
    var controlID_overlay_add = controlID + '_overlay_add';

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

    //var controlID_ribbon_lineage = controlID + '_ribbon_lineage';
    var controlID_ribbon_lineage_add = controlID + '_ribbon_lineage_add';
    var controlID_ribbon_lineage_addItem = controlID + '_ribbon_lineage_addItem';
    var controlID_ribbon_lineage_cancel = controlID + '_ribbon_lineage_cancel';
    var controlID_ribbon_lineage_save = controlID + '_ribbon_lineage_save';

    var controlID_ribbon_multimaprule = controlID + '_ribbon_multimaprule';
    var controlID_ribbon_maprule = controlID + '_ribbon_maprule';
    var controlID_ribbon_maprule_add = controlID + '_ribbon_maprule_add';
    var controlID_ribbon_maprule_cancel = controlID + '_ribbon_maprule_cancel';
    var controlID_ribbon_maprule_save = controlID + '_ribbon_maprule_save';

    var controlID_ribbon_view_1 = controlID + '_ribbon_view_1';
    var controlID_ribbon_view_2 = controlID + '_ribbon_view_2';
    var controlID_ribbon_view_3 = controlID + '_ribbon_view_3';

    var controlID_popover_add = controlID + '_popover_add';

    var controlID_popover_lineage_editor = controlID + '_popover_lineage_editor';
    var controlID_popover_lineage_editor_body = controlID_popover_lineage_editor + '_body';
    var controlID_popover_sourcerule_editor = controlID + '_popover_sourcerule_editor';
    var controlID_popover_sourcerule_editor_body = controlID_popover_sourcerule_editor + '_body';
    var controlID_popover_maprule_editor = controlID + '_popover_maprule_editor';
    var controlID_popover_maprule_editor_body = controlID_popover_maprule_editor + '_body';
    var controlID_popover_multimaprule_editor = controlID + '_popover_multimaprule_editor';
    var controlID_popover_multimaprule_editor_body = controlID_popover_multimaprule_editor + '_body';

    var controlID_tabs = controlID + '_tabs';
    var controlID_fusion_tab = controlID + '_fusion_tab';
    var controlID_sourcerules_tab = controlID + '_sourcerules_tab';
    var controlID_mappingrules_tab = controlID + '_mappingrules_tab';
    var controlID_responsibilities_tab = controlID + '_responsibilities_tab';
    var controlID_transformations_tab = controlID + '_transformations_tab';
    var controlID_fusion_content = controlID + '_fusion_content';
    var controlID_sourcerules_content = controlID + '_sourcerules_content';
    var controlID_mappingrules_content = controlID + '_mappingrules_content';
    var controlID_responsibilities_content = controlID + '_responsibilities_content';

    var tabs = {
        "sourcerules": 0,
        "mappingrules": 1,
        "responsibilities": 2,
        "fusion": 3,
        "transformations": 4
    };

    var defaultTabContent = '<div style="height:100px;text-align:center;padding:25px;"><i class="fa fa-2x fa-spinner fa-spin"></i></div>';

    //#endregion

    //#region Control instantiation

    //$('#' + controlID_splitter).jqxSplitter({ theme: theme, width: '100%', height: '100%', panels: [ { size: '80%', collapsible: false } ]});

    $("#" + controlID_tabs).jqxTabs({ theme: theme, animationType: 'fade', selectionTracker: true }).on('tabclick',function(event) {
        var index = event.args.item;
        loadTab(index);
    });
    
    $("#" + controlID_ribbon_zoom_100).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_zoom_fit).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_save).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true });
    $("#" + controlID_ribbon_reset).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_fullscreen).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_add).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true });
    $("#" + controlID_ribbon_remove).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_undo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_redo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_zoom_slider).jqxSlider({ theme: theme, width: 150, showButtons: true, min: 750, max: 2250, value: 1500, showTicks: false });

    $("#" + controlID_ribbon_sourcerule_add).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();

    $('#' + controlID_ribbon_lineage_cancel).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_lineage_addItem).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_lineage_save).jqxButton({ theme: theme, height: "100%", width: 64 });

    $("#" + controlID_ribbon_multimaprule).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true }).hide();
    $("#" + controlID_ribbon_maprule).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true }).hide();
    $('#' + controlID_ribbon_maprule_add).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_maprule_cancel).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_maprule_save).jqxButton({ theme: theme, height: "100%", width: 64 });

    $('.lineage').hide();
    $('.sourcemapping').hide();

    $("#" + controlID_info).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_ribbon_expander).jqxExpander({ theme: theme }).jqxExpander('collapse');

    //$('#' + controlID_info_detail).MapItems();

    $('#' + controlID_ribbon_view_1).jqxRadioButton({ theme: theme, checked: true });
    $('#' + controlID_ribbon_view_2).jqxRadioButton({ theme: theme, checked: false });
    $('#' + controlID_ribbon_view_3).jqxRadioButton({ theme: theme, checked: false });

    //#endregion

    //#region Event Handlers

    $('#' + controlID_ribbon_expander).on('expanded', toggleRibbon);
    $('#' + controlID_ribbon_expander).on('collapsed', toggleRibbon);

    $("#" + controlID_ribbon_add).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
        //$('#' + controlID_popover_add).toggle(200).css('left', $(this).position().left).css('top', $(this).position().top + 80);

        var data = {
            object: type,
            objectID: id,
            controlID: controlID
        };

        $('#' + controlID_wrapper).hide();
        $('.diagramcommands').hide();

        lineageModel = new LineagePanelViewModel(data, permissions);
        ko.cleanNode($('#' + controlID_popover_lineage_editor_body)[0]);
        ko.applyBindings(lineageModel, $('#' + controlID_popover_lineage_editor_body)[0]);

        $('#' + controlID_popover_lineage_editor).show();
        $('.lineage').show();
    });

    $("#" + controlID_ribbon_sourcerule_add).on('click', function () {
                if ($(this).jqxButton('disabled'))
            return;
        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;
        //console.log(selected);
        $('#' + controlID_popover_sourcerule_editor).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 80);

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

    $("#" + controlID_ribbon_maprule).on('click', function () {

        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;

        if (selected.diagramObjectType == 'Link') {
            var from = myDiagram.model.findNodeDataForKey(selected.from);
            var to = myDiagram.model.findNodeDataForKey(selected.to);

            var data = {
                SourceName: from.name,
                SourceIntersectID: selected.fromIntersectId,
                SourceDiagramKey: selected.from,

                TargetName: to.name,
                TargetIntersectID: selected.toIntersectId,
                TargetDiagramKey: selected.to
            };

            $('#' + controlID_wrapper).hide();
            $('.diagramcommands').hide();

            mapRulesModel = new MapRulesModel(data, permissions);
            ko.cleanNode($('#' + controlID_popover_maprule_editor_body)[0]);
            ko.applyBindings(mapRulesModel, $('#' + controlID_popover_maprule_editor_body)[0]);

            $('#' + controlID_popover_maprule_editor).show();
            $('.sourcemapping').show();
        }
    });

    $("#" + controlID_ribbon_multimaprule).on('click', function () {

        var maps = [];

        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            var link = myDiagram.model.linkDataArray[i];
            var from = myDiagram.model.findNodeDataForKey(link.from);
            var to = myDiagram.model.findNodeDataForKey(link.to);
            maps.push({
                SourceName: from.name,
                SourceIntersectID: link.fromIntersectId,
                SourceDiagramKey: link.from,

                TargetName: to.name,
                TargetIntersectID: link.toIntersectId,
                TargetDiagramKey: link.to
            });
        }

        $('#' + controlID_wrapper).hide();
        $('.diagramcommands').hide();

        mapRulesModel = new MultiMapRulesModel({ Maps: maps }, permissions);
        ko.cleanNode($('#' + controlID_popover_multimaprule_editor_body)[0]);
        ko.applyBindings(mapRulesModel, $('#' + controlID_popover_multimaprule_editor_body)[0]);

        $('#' + controlID_popover_multimaprule_editor).show();
        $('.sourcemapping').show();
    });

    $('#' + controlID_ribbon_lineage_cancel).on('click', function () {
        $('.lineage').fadeOut();
        $('.diagramcommands').show();
        $('#' + controlID_popover_lineage_editor).hide();
        $('#' + controlID_wrapper).show();
    });

    $('#' + controlID_ribbon_lineage_addItem).on('click', function () {
        lineageModel.AddItem();
    });

    $('#' + controlID_ribbon_lineage_save).on('click', function () {
        lineageModel.Save().then(function (data) {
            $.each(data, function (ix, n) {
                var d = createNodeModel();

                d.back = "#000";
                d.fore = "#fff";
                d.obj = n.Intersect.Subject;
                d.objid = n.Intersect.SubjectID;
                d.name = htmlDecode(n.Name);

                d.typeName = htmlDecode(n.Intersect.SubjectTypeName);
                d.url = n.Intersect.SubjectUrl;
                d.template = "Artifact";
                //d.objecttype = n.Intersect.SubjectType;
                //d.objecttypeid = n.Intersect.SubjectTypeID;
                d.key = generateRandomLineageKey(25);
                //d.isDeletable = true;
                d.intersectId = n.IntersectID;

                myDiagram.model.addNodeData(d);
            });
            $('.lineage').fadeOut();
            $('.diagramcommands').show();
            $('#' + controlID_popover_lineage_editor).hide();
            $('#' + controlID_wrapper).show();
        });
    });

    $('#' + controlID_ribbon_maprule_add).on('click', function () {
        mapRulesModel.AddRule();
    });
    $('#' + controlID_ribbon_maprule_cancel).on('click', function () {
        $('.sourcemapping').fadeOut();
        $('#' + controlID_ribbon_maprule_add).show();
        $('#' + controlID_ribbon_maprule_save).show();
        $('.diagramcommands').show();
        $('#' + controlID_popover_maprule_editor).hide();
        $('#' + controlID_popover_multimaprule_editor).hide();
        $('#' + controlID_wrapper).show();
    });
    $('#' + controlID_ribbon_maprule_save).on('click', function () {
        mapRulesModel.SaveRules().then(function(){
            $('.sourcemapping').fadeOut();
            $('.diagramcommands').show();
            $('#' + controlID_popover_maprule_editor).hide();
            $('#' + controlID_popover_multimaprule_editor).hide();
            $('#' + controlID_wrapper).show();
        });
    });

    amplify.subscribe("NoFusionAvailable", function () {
        $('#' + controlID_popover_maprule_editor).height(300);
        $('#' + controlID_ribbon_maprule_add).hide();
        $('#' + controlID_ribbon_maprule_save).hide();
    });

    //#region General ribbon commands

    $('#' + controlID_ribbon_fullscreen).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
        toggleFullscreen();
    });

    $('#' + controlID_ribbon_save).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
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
        if ($(this).jqxButton('disabled'))
            return;
        myDiagram.undoManager.undo();
    });

    $('#' + controlID_ribbon_redo).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
        myDiagram.undoManager.redo();
    });

    $('#' + controlID_ribbon_zoom_slider).on('slide', function (event) {
        var val = event.args.value;
        $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
        myDiagram.scale = (val / 1500);
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
        myDiagram.zoomToFit();
    });

    $('#' + controlID_ribbon_reset).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
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


    $('#' + controlID_ribbon_view_1).on('change', function (event) {
        var isChecked = event.args.checked;
        if (isChecked) {
            viewID = 1;
            $("#" + controlID_ribbon_maprule).jqxButton({ disabled: true });
            $("#" + controlID_ribbon_multimaprule).jqxButton({ disabled: true });
            $('#' + controlID_ribbon_view_2).jqxRadioButton('uncheck');
            $('#' + controlID_ribbon_view_3).jqxRadioButton('uncheck');
            populateDiagram();
        }
    });
    $('#' + controlID_ribbon_view_2).on('change', function (event) {
        var isChecked = event.args.checked;
        if (isChecked) {
            viewID = 2;
            $("#" + controlID_ribbon_maprule).jqxButton({ disabled: true });
            $('#' + controlID_ribbon_multimaprule).jqxButton({ disabled: true });
            $('#' + controlID_ribbon_view_1).jqxRadioButton('uncheck');
            $('#' + controlID_ribbon_view_3).jqxRadioButton('uncheck');
            populateDiagram();
        }
    });
    $('#' + controlID_ribbon_view_3).on('change', function (event) {
        var isChecked = event.args.checked;
        if (isChecked) {
            viewID = 3;
            $("#" + controlID_ribbon_maprule).jqxButton({ disabled: true });
            $('#' + controlID_ribbon_multimaprule).jqxButton({ disabled: true });
            $('#' + controlID_ribbon_view_1).jqxRadioButton('uncheck');
            $('#' + controlID_ribbon_view_2).jqxRadioButton('uncheck');
            populateDiagram();
        }
    });

    //#endregion

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
    $('#' + controlID_overlay_cancel).on('click', cancelAddLink);
    $('#' + controlID_overlay_add).on('click', addRelationship);

    //#endregion

    var oldHeight = $('#' + controlID_wrapper).height();
    var oldWidth = $('#' + controlID_wrapper).width();

    var deletedNodes = [];
    var initialLinks = [];
    var initialNodes = [];
    var newLink = null;
    var overlayEditLinkKey = null;
    var selection = null;

    //#region MapItems

    var mapItemsSource = {
        dataType: "json",
        url: null,
        dataFields: [
            { name: 'MapItemID' },
            { name: 'SourceType' },
            { name: 'SourceName' },
            { name: 'Source' },
            { name: 'SourceID' },
            { name: 'SourceFusion' },
            { name: 'SourceFusionAttribute' },
            { name: 'SourceFusionAttributeType' },
            { name: 'TargetType' },
            { name: 'TargetName' },
            { name: 'Target' },
            { name: 'TargetID' },
            { name: 'TargetFusion' },
            { name: 'TargetFusionAttribute' },
            { name: 'TargetFusionAttributeType' }
        ]
    };

    var mapItemsAdapter = new $.jqx.dataAdapter(mapItemsSource);

    $('#' + controlID_mappingrules_content).jqxGrid({
        source: mapItemsAdapter,
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
        theme: list_theme,
        columngroups: [
            { text: 'Source', name: 'S' },
            { text: 'Target', name: 'T' }
        ],
        columns: [
            {
                datafield: "Source",
                columngroup: "S",
                text: "Business",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return '<span style="margin: 3px 0px 3px 0px"><b>' + data.SourceName + '</b><br/>' + data.SourceType + '</span>';
                }
            },
            {
                datafield: "SourceID",
                columngroup: "S",
                text: "Technical",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return '<span style="margin: 3px 0px 3px 0px">' + data.SourceFusion + '<br/>' + data.SourceFusionAttributeType + '<br/>' + data.SourceFusionAttribute + '</span>';
                }
            },
            {
                datafield: "Target",
                columngroup: "T",
                text: "Business",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return '<span style="margin: 3px 0px 3px 0px"><b>' + data.TargetName + '</b><br/>' + data.TargetType + '</span>';
                }
            },
            {
                datafield: "TargetID",
                columngroup: "T",
                text: "Technical",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return '<span style="margin: 3px 0px 3px 0px">' + data.TargetFusion + '<br/>' + data.TargetFusionAttributeType + '<br/>' + data.TargetFusionAttribute + '</span>';
                }
            }
        ]
    });

    $('#' + controlID_mappingrules_content).on('bindingcomplete', mappingBindingComplete);

    //#endregion

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
            { name: 'Object' },
            { name: 'ObjectID' },
            { name: 'ObjectName' },
            { name: 'ObjectUrl' },
            { name: 'ObjectTypeName' }
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
                text: 'Name', groupable: false, datafield: 'ObjectName',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return textrenderer("<div class='cell-value-name'>" + data.ObjectName + "</div><div class='cell-value-type'>" + data.ObjectTypeName + "</div>");
                }
            }
        ]
    });

    //#endregion

    //#region methods

    function addRelationship() {

        var intersectRoleId = $('#' + controlID_overlay_predicates).val();
        var transformation = $('#' + controlID_overlay_transformation).redactor('get');
        var text = $('#' + controlID_overlay_predicates).text();

        myDiagram.startTransaction("nameRelationship");

        if (overlayEditLinkKey != null) {
            var link = findLinkDataForKey(overlayEditLinkKey);
            myDiagram.model.setDataProperty(link, 'text', text);
            myDiagram.model.setDataProperty(link, 'intersectRoleId', intersectRoleId);
            myDiagram.model.setDataProperty(link, 'transformation', transformation);

            //get the id if possible (if link is deleted and re-added)
            for (var i = 0; i < initialLinks.length; i++) {
                if (initialLinks[i].from == link.from && initialLinks[i].to == link.to) {
                    link.id = initialLinks[i].id;
                    myDiagram.model.setDataProperty(link, 'id', initialLinks[i].id);
                }
            }
        } else {
            newLink.intersectRoleId = intersectRoleId;
            newLink.text = text;
            newLink.diagramObjectType = "Link";
            //newLink.isDeletable = true;
            newLink.id = overlayEditLinkKey;
            newLink.key = overlayEditLinkKey;
            if (newLink.id == null) {
                for (var i = 0; i < initialLinks.length; i++) {
                    if (initialLinks[i].from == newLink.from && initialLinks[i].to == newLink.to) {
                        //newLink.id = initialLinks[i].id;
                        //newLink.key = initialLinks[i].key;
                    }
                }
            }

            var index = -1;

            for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
                if (myDiagram.model.linkDataArray[i].from == newLink.from && myDiagram.model.linkDataArray[i].to == newLink.to) {
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

    function mappingBindingComplete(event) {
        $('#' + controlID_mappingrules_content).jqxGrid('autoresizecolumns');
    }

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

    //#region Model Creation

    function createLinkModel() {
        return {
            id: null,
            key: null,
            Category: '',
            from: null,
            fromIntersectId: 0,
            fromPortId: "OUT",

            to: null,
            toIntersectId: 0,
            toPortId: "IN",

            text: null,
            //intersectRoleId: null,
            diagramObjectType: "Link",
            sourceMappingCount: 0,
            hasMappingRules: false,
            transformation: null,
            hasTransformations: false,
            hasProperties: false,
            mapItems: null
        };
    };

    function createNodeModel() {
        return {
            key: null,
            obj: null,
            objid: null,
            name: null,
            typeName: null,
            back: null,
            fore: null,
            highlightColor: null,
            diagramObjectType: "Node",
            template: "Artifact",
            intersectId: null,
            sourceRuleCount: 0,
            sourceMappingCount: 0,
            hasMappingRules: false,
            hasSourceRules: false,
            challengeCount: 0,
            hasChallenges: false,
            openEventCount: 0,
            hasOpenEvents: false,
            openIssueCount: 0,
            hasOpenIssues: false,
            transformationCount: 0,
            hasTransformations: false,
            mapItems: null,
            other: null
        };
    };

    //#endregion

    function findLinkDataForKey(key) {
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].key == key)
                return myDiagram.model.linkDataArray[i];
        }
    }

    function findLinkByFromToIntersects(from, to) {
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].fromIntersectId == from && myDiagram.model.linkDataArray[i].toIntersectId == to)
                return myDiagram.model.linkDataArray[i];
        }
    }

    function findNodeIndexByObject(obj, objid) {
        for (var i = 0; i < myDiagram.model.nodeDataArray.length; i++) {
            if (myDiagram.model.nodeDataArray[i].obj == obj && myDiagram.model.nodeDataArray[i].objid == objid)
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
        var parents = [];
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].to == key) {
                parents.push(myDiagram.model.findNodeDataForKey(myDiagram.model.linkDataArray[i].from));
            }
        }
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
                changes.deleted.push(initialLinks[i]);
            }
        }

        for (var i = 0; i < initialLinks.length; i++) {
            var found = false;
            for (var j = 0; j < links.length; j++) {
                if (initialLinks[i].from == links[j].from && initialLinks[i].to == links[j].to) {

                    var l1 = (initialLinks[i].intersectRoleId || '').toString();
                    var l2 = (links[j].intersectRoleId || '').toString();

                    if (l1 != l2) {
                        found = true;
                    }

                    break;
                }
            }

            if (found) {
                changes.modified.push(links[j]);
            }
        }

        return changes;
    }

    //We May not care about this.  Possible removal.
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
                //if (initialNodes[j].id == nodes[i].id) {
                    if (initialNodes[j].key === nodes[i].key) {
                        //changes.modified.push(nodes[i]);
                        break;
                    }
                //}
            }
        }

        //console.log(changes);
        return changes;

    }

    function htmlDecode(s) {
        s = s.replace(/&#39;/g, '\'');
        s = s.replace(/&amp;/g, '&')
        s = s.replace(/&lt;/g, '<')
        s = s.replace(/&gt;/g, '>')
        s = s.replace(/&#34;/g, '"');

        return s;
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

    function makeIconPanel(icon, tooltip, binding, fontSize) {
        fontSize -= 2;
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
             new go.Binding("fill", "fore")),
         g(go.TextBlock,
             {
                 row: 0,
                 margin: 0,
                 alignment: go.Spot.Center,
                 editable: false,
                 font: (fontSize) + "pt FontAwesome",
                 text: icon,
                 toolTip: g(go.Adornment, "Auto", g(go.Shape, { fill: "lightyellow" }), g(go.Panel, "Vertical", g(go.TextBlock, { margin: 3, text: tooltip })))
             },
             new go.Binding("stroke", "back")
         ),
        new go.Binding("visible", binding)
       );

        return iconPanel;
    }

    function markForDeletion(set) {
        myDiagram.startTransaction("markSelection");

        //get a deep copy of the set as an array
        var sel = $.extend(true, [], set.toArray());

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
            }

        }
        myDiagram.commitTransaction("markSelection");
        refreshControls(null);
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
                        type = obj.obj;
                        id = obj.objid;
                        populateDiagram();
                    });
                } else {
                    type = obj.obj;
                    id = obj.objid;

                    populateDiagram();
                    $('#' + controlID_ribbon_remove).hide(200);
                }
            }
            //else if (obj.diagramObjectType == 'Link' && !readonly) {
            //    overlayEditLinkKey = obj.key;
            //    showRelationshipOverlay(obj);
            //}
        }
    }

    function refreshControls(data) {
        toggleButtons(data);
        toggleTabs(data);
    }

    function toggleFullscreen() {

        var defaultHtml = '<div style="padding:5px 0 5px 0;"><div><i class="fa fa-2x fa-arrows-alt"></i></div><div style="padding-top:5px">Fullscreen</div></div>';
        var exitHtml = '<div style="padding:5px 0 5px 0;"><div><i class="fa fa-2x fa-sign-out"></i></div><div style="padding-top:5px">Exit Fullscreen</div></div>';
        fullscreen = !fullscreen;
        if (fullscreen) {
            window.scrollTo(0, 0); //scroll to top
            $('#' + controlID_ribbon_fullscreen).html(exitHtml);
            $('#' + controlID_wrapper_fullscreen).css('position', 'fixed')
                .css('left', '0')
                .css('top', '0')
                .css('width', '100%')
                .height($(window).height())
                .css('overflow', 'hidden');

            var top = $('#' + controlID_wrapper).position().top;
            var wrapperHeight = $('#' + controlID_wrapper_fullscreen).height();
            var ribbonHeight = $("#" + controlID_ribbon_expander).height();

            $('#' + controlID_wrapper).height(wrapperHeight - ribbonHeight);
            $('#' + controlID_diagram).height(wrapperHeight - ribbonHeight);

            $('#' + controlID_sidebar).height(wrapperHeight - ribbonHeight);
        } else {
            $('#' + controlID_ribbon_fullscreen).html(defaultHtml);
            $('#' + controlID_wrapper_fullscreen).attr('style', 'z-index:1000000;background-color:white;');
            $('#' + controlID_wrapper).height(520);
            $('#' + controlID_diagram).height(520);
            $('#' + controlID_sidebar).height(520);
        }
        myDiagram.requestUpdate();
        //force this to queue behind browser layout updates
        setTimeout(function () {
            myDiagram.requestUpdate();
            myDiagram.focus();
            //$('#' + controlID_splitter).jqxSplitter('refresh');
        }, 0);
    }

    function toggleTabs(data) {
        var first = -1;
        var delay = 0;
        var defaultInfo = '<div style="color:#999;height:25px;text-align:center">Nothing selected</div>';
        var errorInfo = '<div style="color:maroon;height:100px;text-align:center">An error occurred</div>';

        $("#" + controlID_info).jqxExpander('expand');

        if (data == null) {
            //$("#" + controlID_info).jqxExpander('collapse');
            //$("#" + controlID_info_body).html(defaultInfo);
            //$("#" + controlID_info_detail_wrapper).hide();
            $("#" + controlID_info_detail).html('');

            $('#' + controlID_tabs).hide(delay);
            for (var i = 0; i < tabs.length; i++) {
                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + i + ")").css("display", "none");
            }

        } else {
            $('#' + controlID_tabs).show(delay);

            if (data.diagramObjectType == 'Node') {
                first = tabs["responsibilities"];

                //$("#" + controlID_info_detail_wrapper).hide();
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

                try {
                    mapItemsSource.url = null;
                    $('#' + controlID_info_detail).jqxGrid('updatebounddata');
                } catch (e) { }

                $.ajax({
                    url: '/resources/' + data.obj + '/' + data.objid + '/templates/tooltip/Preview',
                    async: true
                }).done(function (data) {
                    $('#' + controlID_info_detail).html(data);
                    $("#" + controlID_info).jqxExpander('expand');
                }).fail(function () {
                    $('#' + controlID_info_body).html(errorInfo);
                    $("#" + controlID_info).jqxExpander('collapse');
                });

                if (data.hasMappingRules) {
                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "block");
                    //$("#" + controlID_mappingrules_content).html(defaultTabContent);
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

                var tranCount = 0;

                var links = myDiagram.model.linkDataArray;
                for (var i = 0; i < links.length; i++) {
                    if (links[i].to == data.key && links[i].hasTransformations)
                        tranCount++;
                }

            } else if (data.diagramObjectType == 'Link') {
                var from = myDiagram.model.findNodeDataForKey(data.from);
                var to = myDiagram.model.findNodeDataForKey(data.to);
                first = -1;//tabs["fusion"];

                var selectedMapID = data.id;

                //$('#' + controlID_info_body).html(data);
                //$("#" + controlID_info).jqxExpander('expand');

                //$('#' + controlID_info_detail).show();
                //$('#' + controlID_info_detail).hide();
                //ObjectDetail(controlID_info_detail, 'Map', selectedMapID, true);
                //$('#' + controlID_info_detail).MapItems('reload', selectedMapID, false, false);


                //if (permissions.HasPermission("Root", "Update") && intersectId != 0) {
                //    TileTools("#" + controlID_info_detail_edit, [
                //        { icon: 'pencil', uri: '/form/EditRelationship?id=' + intersectId, context: 'intersectform', title: 'Edit Relationship' }
                //    ]);
                //    $('#' + controlID_info_detail_edit).on('click', function () { if (fullscreen) toggleFullscreen(); })
                //} else {
                //    $("#" + controlID_info_detail_edit).html('');
                //}


                $("#" + controlID_info_detail_wrapper).show();
                

                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["responsibilities"] + ")").css("display", "none");
                $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["fusion"] + ")").css("display", "none");
                

                try {
                    technicalRelationsSource.url = null;
                    $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                } catch (e) { }

                try {
                    lineageResponsibilitySource.url = null;
                    $('#' + controlID_responsibilities_content).jqxGrid('updatebounddata');
                } catch (e) { }

                try {
                    mapItemsSource.url = '/api/maps/' + selectedMapID + '/mapitems';
                    $('#' + controlID_info_detail).jqxGrid('updatebounddata');
                } catch (e) { }

               // if (data.hasMappingRules) {

                    first = tabs["mappingrules"];

                    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "block");
                    //$("#" + controlID_mappingrules_content).html(defaultTabContent);
                    first = tabs["mappingrules"];
                //} else {
                //    $("#" + controlID_tabs + " .jqx-tabs-title:eq(" + tabs["mappingrules"] + ")").css("display", "none");
                //}

                if (to.hasSourceRules) {
                    if (first == -1)
                        first = tabs["sourcerules"];

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
                url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + type + '&sID=' + id + '&t=' + selectedData.obj + '&tID=' + selectedData.objid;
                try {
                    technicalRelationsSource.url = url;
                    $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                }
                catch (e) {
                }
                break;
            case tabs["responsibilities"]:
                if (lineageResponsibilitySource.url != null)
                    return;

                        try {
                            lineageResponsibilitySource.url = '/api/' + selectedData.obj + '/' + selectedData.objid + '/ownership?showHidden=false';
                            $('#' + controlID_responsibilities_content).jqxGrid('updatebounddata');
                        } catch (e) { }
                break;
            case tabs["sourcerules"]:
                if ($("#" + controlID_sourcerules_content).html().toString() != defaultTabContent) {
                    return;
                }

                url = '/api/' + type + '/' + id + '/sources/' + selectedData.obj + '/' + selectedData.objid + '/rules';
                if (selectedData.diagramObjectType != 'Node') {
                    url = '/api/' + type + '/' + id + '/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/rules';
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

                if (from.template !== "Fusion" && to.template !== "Fusion") {
                    mapItemsSource.url = '/api/maps/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/mapitems';
                }
                else {
                    mapItemsSource.url = null;
                }
                $('#' + controlID_mappingrules_content).jqxGrid('updatebounddata');

                //if ($("#" + controlID_mappingrules_content).html().toString() != defaultTabContent) {
                //    return;
                //}
                //url = '/form/sourcetarget/load/' + type + '/' + id + '/' + selectedData.obj + '/' + selectedData.objid + '/' + selectedData.obj + '/' + selectedData.objid;
                //if (selectedData.diagramObjectType != 'Node') {
                //    url = '/form/sourcetarget/load/' + type + '/' + id + '/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid;
                //}

                //$.ajax({
                //    url: url
                //}).done(function (data) {
                //    for (var i = 0; i < data.items.length; i++) {
                //        data.items[i].index = i + 1;
                //    }
                //    var sourceTemplate = Handlebars.getTemplate('LineageDiagramMappingRules');
                //    $('#' + controlID_mappingrules_content).html(sourceTemplate(data));
                //}).fail(function () {
                //    $('#' + controlID_mappingrules_content).html(defaultTabContent);
                //});
                break;
        }
    }

    function toggleButtons(data) {
        var delay = 200;

        if (!readonly) {
            $("#" + controlID_ribbon_add).show(delay);
        } else {
            $("#" + controlID_ribbon_add).hide(delay);
        }
        if (data == null) {
            $("#" + controlID_ribbon_sourcerule_add).hide(delay);
            $("#" + controlID_ribbon_multimaprule).show(delay);
            $("#" + controlID_ribbon_maprule).hide(delay);
            $("#" + controlID_ribbon_remove).hide(delay);
        } else {
            $("#" + controlID_ribbon_multimaprule).hide(delay);

            if (data.diagramObjectType == 'Node') {
                if (!readonly) {
                    //$("#" + controlID_ribbon_maprule).show(delay);
                    $("#" + controlID_ribbon_sourcerule_add).show(delay);
                    $("#" + controlID_ribbon_remove).show(delay);
                } else {
                    //$("#" + controlID_ribbon_maprule).hide(delay);
                    $("#" + controlID_ribbon_sourcerule_add).hide(delay);
                    $("#" + controlID_ribbon_remove).hide(delay);
                }
            } else {
                $("#" + controlID_ribbon_sourcerule_add).hide(delay);

                if (!readonly) {
                    $("#" + controlID_ribbon_maprule).show(delay);
                    $("#" + controlID_ribbon_remove).show(delay);
                } else {
                    $("#" + controlID_ribbon_maprule).hide(delay);
                    $("#" + controlID_ribbon_remove).hide(delay);
                }
            }
        }
    }

    function toggleRibbon()
    {
        if (!fullscreen)
            return;

        var ribbonHeight = $('#' + controlID_ribbon_expander).height();
        var wrapperHeight = $('#' + controlID_wrapper_fullscreen).height()

        var top = $('#' + controlID_wrapper).position().top;

        $('#' + controlID_wrapper).height(wrapperHeight - ribbonHeight);
        $('#' + controlID_diagram).height(wrapperHeight - ribbonHeight);
        $('#' + controlID_sidebar).height(wrapperHeight - ribbonHeight);

        myDiagram.focus();
        myDiagram.requestUpdate();
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

        refreshControls(selectedData);
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

        newLink.fromIntersectId = fromNode.intersectId;
        newLink.toIntersectId = toNode.intersectId;

        //var results = $.ajax({
        //    url: '/form/Lineage_IntersectRoles',
        //    data: null
        //}).done(function (data, status, xhr) {
        //    if (data.length > 0) {
        //        $('#' + controlID_overlay_relationship).html('<span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
        //            + (fromNode.fore || 'black')
        //            + ';background-color: '
        //            + (fromNode.back || 'white')
        //            + ';" >'
        //            + fromNode.typeName
        //            + '</span><span style="font-size:1.5rem;font-weight:800;color:grey">&#8594;</span><span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
        //            + (toNode.fore || 'black')
        //            + ';background-color: '
        //            + (toNode.back || 'white')
        //            + ';">'
        //            + toNode.typeName + '</span>');

        //        $('#' + controlID_overlay_add).show();

        //        populateIntersectRoles(data);

        //        $('#' + controlID_overlay).show();
        //    }
        //    else {
        //        amplify.publish('ShowMessage', { type: 'error', title: 'Not allowed', message: 'No roles defined.  Please go to Administration / MetaModel / Relationships to add roles.' });
        //        e.diagram.remove(e.subject);
        //    }
        //});

        //Four lines below are here b/c section above is commented out.
        myDiagram.startTransaction("nameRelationship");
        myDiagram.model.addLinkData(newLink);
        myDiagram.commitTransaction("nameRelationship");
        newLink = null;
    }

    function showRelationshipOverlay(linkData) {

        var deferred = $.Deferred();

        newLink = linkData;

        newLink.diagramObjectType = "Link";
        var fromNode = myDiagram.model.findNodeDataForKey(linkData.from);
        var toNode = myDiagram.model.findNodeDataForKey(linkData.to);

        var results = $.ajax({
            url: '/form/Lineage_IntersectRoles',
            data: null
        }).done(function (data, status, xhr) {
            if (data.length > 0) {
                $('#' + controlID_overlay_relationship).html('<span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (fromNode.fore || 'black')
                    + ';background-color: '
                    + (fromNode.back || 'white')
                    + ';" >'
                    + fromNode.typeName
                    + '</span><span style="font-size:1.5rem;font-weight:800;color:grey">&#8594;</span><span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
                    + (toNode.fore || 'black')
                    + ';background-color: '
                    + (toNode.back || 'white')
                    + ';">'
                    + toNode.typeName + '</span>');

                populateIntersectRoles(data);

                $('#' + controlID_overlay_predicates).val(newLink.intersectRoleId);
                $('#' + controlID_overlay_add).show();
                if (newLink.intersectRoleId) {
                    $('#' + controlID_overlay_add).removeAttr('disabled');
                }
                $('#' + controlID_overlay_transformation).val(newLink.transformation);

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
        if (data.nodes) {
            for (var i = 0; i < data.nodes.length; i++) {

                var d = data.nodes[i];
                var model = createNodeModel();

                var isFocalPoint = (d.obj == type && d.objid == id);

                if (isFocalPoint) {
                    $('#' + controlID_header).text('Lineage: ' + htmlDecode(d.name));
                }

                model.template = d.template;// isFocalPoint ? "Focal" : "Normal";
                model.key = d.key;
                model.obj = d.obj;
                model.objid = d.objid;
                model.type = d.obj;
                model.name = htmlDecode(d.name);
                model.typeName = d.typeName;
                model.fore = d.fore;
                model.back = d.back;
                model.diagramObjectType = "Node";
                model.intersectId = d.intersectId;

                model.sourceRuleCount = d.sourceRuleCount;
                model.mappingRuleCount = d.mappingRuleCount;
                model.hasSourceRules = (d.sourceRuleCount > 0);
                model.hasMappingRules = (d.mappingRuleCount > 0);
                model.challengeCount = d.challenges;
                model.hasChallenges = (d.challenges > 0);
                model.openEventCount = d.openEventCount;
                model.hasOpenEvents = (d.openEventCount > 0);
                model.openIssueCount = d.issues;
                model.hasOpenIssues = (d.issues > 0);
                model.hasTransformations = (d.transformationCount > 0);

                model.mapItems = d.mapItems;

                if (d.other)
                    model.other = htmlDecode(d.other);

                modelList.push(model);
            }
        }

        if (data.links) {
            for (var i = 0; i < data.links.length; i++) {
                var d = data.links[i];
                var link = createLinkModel();
                //myDiagram.model.setCategoryForLinkData(link, d.category);
                //link.id = d.id;
                //link.intersectTypeId = d.intersectTypeId;
                //link.key = d.id;
                link.Category = d.category;
                link.from = d.from;
                //link.fromIntersectId = d.fromIntersectId;
                link.to = d.to;
                //link.toIntersectId = d.toIntersectId;
                //link.text = d.role;
                //link.intersectRoleId = d.intersectRoleId;
                link.diagramObjectType = "Link";
                link.sourceMappingCount = d.mappingRuleCount;
                link.hasMappingRules = (d.mappingRuleCount > 0);
                link.hasTransformations = (d.transformation);
                link.hasProperties = (link.hasTransformations || link.hasMappingRules);
                link.mapItems = d.mapItems;
                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            myDiagram.model.addNodeData(modelList[i]);
        }
        myDiagram.model.linkCategoryProperty ="Category";
        for (var i = 0; i < linkList.length; i++) {
            myDiagram.model.addLinkData(linkList[i]);
            myDiagram.model.setCategoryForLinkData(linkList[i], linkList[i].Category);
        }

        //get deep copy of lists
        initialNodes = $.extend(true, [], modelList);
        initialLinks = $.extend(true, [], linkList);

        refreshControls(null);  //set buttons/expanders to defaults

        myDiagram.commitTransaction("load_all_data");
        reOrderLayout();
    }

    function populateDiagram() {
        var results = $.ajax({
            url: '/diagrams/' + type + '/' + id + '/lineage/' + viewID,
            data: null
        }).done(function (data, status, xhr) {
            parseData(data);
            reOrderLayout();
            myDiagram.zoomToFit();
        });
    }

    function populateIntersectRoles(roles, selectedValue) {
        var output = [];

        output.push('<option value="0"></option>');
        for (var i = 0; i < roles.length; i++) {
            output.push('<option value="' + roles[i].value + '">' + roles[i].title + '</option>');
        }
        $('#' + controlID_overlay_predicates).html(output.join(''));

    }

    function reOrderLayout() {
        myDiagram.layout.invalidateLayout();
        myDiagram.requestUpdate();
    }

    function resetOverlay() {
        $('#' + controlID_overlay_predicates).val(0);
        $('#' + controlID_overlay_add).prop('disabled', true);
    }

    function saveChanges() {

        if (readonly) return;

        $('#' + controlID_ribbon_save).jqxButton({ disabled: true });

        var nodeChanges = getNodeChanges();
        var linkChanges = getLinkChanges();

        var model = {
            Adds: [],
            Deletes: [],
            Edits: []
        };

        for (var i = 0; i < nodeChanges.deleted.length; i++) {
            var node = nodeChanges.deleted[i];
            $.each(node.mapItems, function () {
                model.Deletes.push({
                    MapID: this.MapID
                });
            });
        }

        //#region Link Processing

        for (var i = 0; i < linkChanges.added.length; i++) {
            var link = linkChanges.added[i];
            model.Adds.push({
                SourceKey: link.from,
                SourceIntersectID: link.fromIntersectId,
                TargetKey: link.to,
                TargetIntersectID: link.toIntersectId,
                IntersectRoleID: link.intersectRoleId,
                Transformation: link.transformation
            });
        }

        for (var i = 0; i < linkChanges.deleted.length; i++) {
            var link = linkChanges.deleted[i];
            model.Deletes.push({
                MapID: link.id
            });
        }

        for (var i = 0; i < linkChanges.modified.length; i++) {
            var link = linkChanges.modified[i];
            model.Edits.push({
                MapID: link.id,
                IntersectRoleID: link.intersectRoleId,
                Transformation: link.transformation
            });
        }

        //#endregion

        $.ajax({
            url: '/form/Lineage_Update',
            async: true,
            data: JSON.stringify(model),
            processData: false,
            type: 'POST',
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).fail(function (data) {
            amplify.publish("ShowMessage", { title: 'An error occurred while saving changes.', message: data.message, success: false });
        }).done(function () {
            amplify.publish("ShowMessage", { title: 'Success', message: 'Updated lineage diagram.', success: true });
            deletedNodes = [];
            populateDiagram();
            $('#' + controlID_ribbon_save_spinner).hide();
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
            if ($(this).jqxButton('disabled'))
                return;
            markForDeletion(selection);
            $("#" + controlID_ribbon_remove).hide(200);
        });
    }

    var g = go.GraphObject.make;
    var myDiagram = initializeDiagram();

    myDiagram.addDiagramListener('ViewportBoundsChanged', onViewportBoundsChanged);
    myDiagram.addDiagramListener('ChangedSelection', onSelectionChange);
    myDiagram.addDiagramListener('ObjectDoubleClicked', onDoubleClick);
    myDiagram.addDiagramListener('LinkDrawn', onLinkDrawn);
    myDiagram.addDiagramListener('SelectionDeleting', onDeleting);
    myDiagram.addDiagramListener('SelectionDeleted', onDeleted);
    myDiagram.addDiagramListener('ExternalObjectsDropped', onDrop);
    myDiagram.model.addChangedListener(onChange);

    myDiagram.grid.visible = false;
    myDiagram.grid.gridCellSize = new go.Size(8, 8);
    myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
    myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

    //#region Node/Link Templates

    var nodeWidth = 200;
    var nodeHeight = 150;
    var nodeBorderColor = '#000000';
    var nodeFontSize = 14;
    //var nodeInPorts = [makePort("IN", true)];
    //var nodeOutPorts = [makePort("OUT", false)];

    //#region Focal Template

    var focalNode = g(go.Node, "Spot",
    {
        mouseEnter: mouseEnter,
        mouseLeave: mouseLeave
    },
    g(go.Panel, "Auto", {
        width: nodeWidth,
        height: nodeHeight
    },
    g(go.Shape, "RoundedRectangle", {
        stroke: nodeBorderColor,
        strokeWidth: 2,
        spot1: go.Spot.TopLeft,
        spot2: go.Spot.BottomRight,
        name: "NodeShape"
    },
    new go.Binding("fill", "back").makeTwoWay()
   ),
    g(go.Panel,
        go.Panel.Horizontal,
        {
            alignment: go.Spot.BottomLeft,
            margin: 5
        },
        makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
        makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
        makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
        makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
        makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
    ),
    g(go.Panel, "Table",
        g(go.TextBlock, {
            row: 0,
            margin: 3,
            alignment: go.Spot.Top,
            editable: false,
            maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
            font: "bold " + nodeFontSize + "pt sans-serif"
        },
            new go.Binding("text", "name").makeTwoWay(),
            new go.Binding("stroke", "fore").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 1,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
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
    [makePort("IN", false)]),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Right,
        alignmentFocus: new go.Spot(1, 0.5, 8, 0)
    },
    [makePort("OUT", false)]));

    myDiagram.nodeTemplateMap.add("Focal", focalNode);

    //#endregion

    //#region Normal Template

    nodeWidth = 200;
    nodeHeight = 105;
    nodeBorderColor = 'transparent';
    nodeFontSize = 10;

    var normalNode = g(go.Node, "Spot",
    {
        mouseEnter: mouseEnter,
        mouseLeave: mouseLeave
    },
    g(go.Panel, "Auto", {
        width: nodeWidth,
        height: nodeHeight
    },
    g(go.Shape, "RoundedRectangle", {
        stroke: nodeBorderColor,
        strokeWidth: 2,
        spot1: go.Spot.TopLeft,
        spot2: go.Spot.BottomRight,
        name: "NodeShape"
    },
    new go.Binding("fill", "back").makeTwoWay()
   ),
    g(go.Panel,
        go.Panel.Horizontal,
        {
            alignment: go.Spot.BottomLeft,
            margin: 5
        },
        makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
        makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
        makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
        makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
        makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
    ),
    g(go.Panel, "Table",
        g(go.TextBlock, {
            row: 0,
            margin: 3,
            alignment: go.Spot.Top,
            editable: false,
            maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
            font: "bold " + nodeFontSize + "pt sans-serif"
        },
            new go.Binding("text", "name").makeTwoWay(),
            new go.Binding("stroke", "fore").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 1,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
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
    [makePort("IN", false)]),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Right,
        alignmentFocus: new go.Spot(1, 0.5, 8, 0)
    },
    [makePort("OUT", false)]));

    myDiagram.nodeTemplateMap.add("Normal", normalNode);

    //#endregion

    //#region SupportFocal Template

    var nodeWidth = 140;
    var nodeHeight = 80;
    var nodeBorderColor = '#000000';
    var nodeFontSize = 9;

    var supportFocalNode = g(go.Node, "Spot",
    {
        mouseEnter: mouseEnter,
        mouseLeave: mouseLeave
    },
    g(go.Panel, "Auto", {
        width: nodeWidth,
        height: nodeHeight
    },
    g(go.Shape, "RoundedRectangle", {
        stroke: nodeBorderColor,
        strokeWidth: 2,
        spot1: go.Spot.TopLeft,
        spot2: go.Spot.BottomRight,
        name: "NodeShape"
    },
    new go.Binding("fill", "back").makeTwoWay()
   ),
    g(go.Panel,
        go.Panel.Horizontal,
        {
            alignment: go.Spot.BottomLeft,
            margin: 5
        },
        makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
        makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
        makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
        makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
        makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
    ),
    g(go.Panel, "Table",
        g(go.TextBlock, {
            row: 0,
            margin: 3,
            alignment: go.Spot.Top,
            editable: false,
            maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
            font: "bold " + nodeFontSize + "pt sans-serif"
        },
            new go.Binding("text", "name").makeTwoWay(),
            new go.Binding("stroke", "fore").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 1,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
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
    [makePort("IN", false)]),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Right,
        alignmentFocus: new go.Spot(1, 0.5, 8, 0)
    },
    [makePort("OUT", false)]));

    myDiagram.nodeTemplateMap.add("SupportFocal", supportFocalNode);

    //#endregion

    //#region SupportNormal Template

    var nodeWidth = 130;
    var nodeHeight = 70;
    var nodeBorderColor = 'transparent';
    var nodeFontSize = 9;

    var supportNode = g(go.Node, "Spot",
    {
        mouseEnter: mouseEnter,
        mouseLeave: mouseLeave
    },
    g(go.Panel, "Auto", {
        width: nodeWidth,
        height: nodeHeight
    },
    g(go.Shape, "RoundedRectangle", {
        stroke: nodeBorderColor,
        strokeWidth: 2,
        spot1: go.Spot.TopLeft,
        spot2: go.Spot.BottomRight,
        name: "NodeShape"
    },
    new go.Binding("fill", "back").makeTwoWay()
   ),
    g(go.Panel,
        go.Panel.Horizontal,
        {
            alignment: go.Spot.BottomLeft,
            margin: 5
        },
        makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
        makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
        makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
        makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
        makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
        makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
    ),
    g(go.Panel, "Table",
        g(go.TextBlock, {
            row: 0,
            margin: 3,
            alignment: go.Spot.Top,
            editable: false,
            maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
            font: "bold " + nodeFontSize + "pt sans-serif"
        },
            new go.Binding("text", "name").makeTwoWay(),
            new go.Binding("stroke", "fore").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 1,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
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
    [makePort("IN", false)]),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Right,
        alignmentFocus: new go.Spot(1, 0.5, 8, 0)
    },
    [makePort("OUT", false)]));

    myDiagram.nodeTemplateMap.add("SupportNormal", supportNode);

    //#endregion

    //#region Fusion Template

    var nodeWidth = 225;
    var nodeHeight = 80;
    var nodeBorderColor = 'transparent';
    var nodeFontSize = 9;

    var supportNode = g(go.Node, "Spot",
    {
        mouseEnter: mouseEnter,
        mouseLeave: mouseLeave
    },
    g(go.Panel, "Auto", {
        width: nodeWidth,
        height: nodeHeight
    },
    g(go.Shape, "RoundedRectangle", {
        stroke: nodeBorderColor,
        strokeWidth: 2,
        spot1: go.Spot.TopLeft,
        spot2: go.Spot.BottomRight,
        name: "NodeShape"
    },
    new go.Binding("fill", "back").makeTwoWay()
   ),
    g(go.Panel, "Table",
        g(go.TextBlock, {
            row: 0,
            margin: 3,
            alignment: go.Spot.Top,
            editable: false,
            maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
            font: "bold " + nodeFontSize + "pt sans-serif"
        },
            new go.Binding("text", "name").makeTwoWay(),
            new go.Binding("stroke", "fore").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 1,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
            new go.Binding("text", "typeName").makeTwoWay()
        ),
        g(go.TextBlock, {
            row: 2,
            margin: 3,
            maxSize: new go.Size(180, NaN),
            font: 'bold ' + (nodeFontSize - 2) + "pt sans-serif"
        },
            new go.Binding("stroke", "fore").makeTwoWay(),
            new go.Binding("text", "other").makeTwoWay()
        )
    )),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Left,
        alignmentFocus: new go.Spot(0, 0.5, -8, 0)
    },
    [makePort("IN", false)]),
    g(go.Panel, "Vertical", {
        alignment: go.Spot.Right,
        alignmentFocus: new go.Spot(1, 0.5, 8, 0)
    },
    [makePort("OUT", false)]));

    myDiagram.nodeTemplateMap.add("Fusion", supportNode);

    //#endregion

    //#region Default Link Template

    myDiagram.linkTemplateMap.add("", g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
            g(go.Shape, {
                stroke: "gray", strokeWidth: 2
            },
            new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
            new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
            g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" }), // the arrowhead
            g(go.Panel, "Auto",
                g(go.Shape, {
                    visible: false,
                    fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: '#999',
                    strokeDashArray: [3, 2]
                },
                //only visible if there's a label
                new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                ), // the link shape
                g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                // the label
                new go.Binding("text", "text").makeTwoWay()
                )
            )
        )
    );

    //#endregion

    //#region Support Template

    myDiagram.linkTemplateMap.add("Support", g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            g(go.Shape, {
                stroke: "blue", strokeWidth: 2
            },
            new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
            new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
            //g(go.Shape, { toArrow: "standard", fill: "blue", stroke: "blue" }), // the arrowhead
            g(go.Panel, "Auto",
                g(go.Shape, {
                    visible: false,
                    fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: '#999',
                    strokeDashArray: [3, 2]
                },
                //only visible if there's a label
                new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                ), // the link shape
                g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                // the label
                new go.Binding("text", "text").makeTwoWay()
                )
            )
        )
    );

    //#endregion

    //#endregion

    var myOverview = initializeOverview(myDiagram);

    populateDiagram();

    //#endregion

    //#region Amplify Subscribes

    amplify.subscribe("SaveAction", function (data) {
        try {
            switch (data.context) {
                case 'mappingrule':
                    if (data.fromIntersectId && data.toIntersectId && data.count > 0) {
                        var link = findLinkByFromToIntersects(data.fromIntersectId, data.toIntersectId);
                        if (link) {
                            myDiagram.model.setDataProperty(link, "sourceMappingCount", data.count);
                            myDiagram.model.setDataProperty(link, "hasMappingRules", (data.count > 0));
                            myDiagram.model.setDataProperty(link, "hasProperties", (data.count > 0));
                            refreshControls(selectedData);
                        }
                    }
                    else {
                        populateDiagram();
                    }
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
                    refreshControls(selectedData);
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
        refreshControls(selectedData);
    });

    //#endregion
}

