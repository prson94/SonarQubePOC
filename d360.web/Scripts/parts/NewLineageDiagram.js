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
    var sourceRuleModel;
    var mapRulesModel;
    var transformationModel;

    //#region Control constants

    var ribbon_button_width = 58;
    var ribbon_button_height = "90%";

    var controlID_header = controlID + '_header';
    var controlID_wrapper = controlID + '_wrapper';
    var controlID_diagram = controlID + '_dgm';
    var controlID_palette = controlID + '_palette';
    var controlID_overview = controlID + '_overview';

    //var controlID_sidebar_ribbon = controlID + '_sidebar_ribbon';
    var controlID_ribbon = controlID + '_ribbon';
    var controlID_wrapper_fullscreen = controlID + '_wrapper_fullscreen';

    var controlID_controls = controlID + '_controls';

    var controlID_view_window_base = controlID + '_view_window';
    var controlID_window_base = controlID + '_Window';

    var controlID_info_body = controlID + '_info_body';
    var controlID_info_detail = controlID + '_info_detail';
    var controlID_info_detail_wrapper = controlID + '_info_detail_wrapper';
    var controlID_info_detail_edit = controlID + '_info_detail_edit';

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

    var controlID_ribbon_undo = controlID + '_ribbon_undo';
    var controlID_ribbon_redo = controlID + '_ribbon_redo';

    var controlID_ribbon_sourcerule = controlID + '_ribbon_sourcerule';
    var controlID_ribbon_sourcerule_cancel = controlID + '_ribbon_sourcerule_cancel';
    var controlID_ribbon_sourcerule_save = controlID + '_ribbon_sourcerule_save';

    var controlID_ribbon_lineage = controlID + '_ribbon_lineage';
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

    var controlID_fusion_content = controlID + '_fusion_content';
    var controlID_sourcerules_content = controlID + '_sourcerules_content';
    var controlID_mappingrules_content = controlID + '_mappingrules_content';
    var controlID_responsibilities_content = controlID + '_responsibilities_content';

    var tabs = {
        "info": 0,
        "sourcerules": 1,
        "mappingrules": 2,
        "responsibilities": 3,
        "fusion": 4,
        "transformations": 5
    };

    var defaultTabContent = '<div style="height:100px;text-align:center;padding:25px;"><i class="fa fa-2x fa-spinner fa-spin"></i></div>';

    //#endregion

    //#region Control instantiation

    $("#" + controlID_view_window_base + '0').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();
    $("#" + controlID_view_window_base + '1').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();
    $("#" + controlID_view_window_base + '2').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();
    $("#" + controlID_view_window_base + '3').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();
    $("#" + controlID_view_window_base + '4').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();
    $("#" + controlID_view_window_base + '5').jqxButton({ theme: theme, height: "100%", width: 64 });//.hide();

    var windowSettings = {
        height: 'auto', width: 300,
        autoOpen: false,
        zIndex: 100001,
        minWidth: 400, minHeight: 300,
        maxWidth: 900, maxHeight: 500
    };

    $("#" + controlID_window_base + '0').jqxWindow(windowSettings);
    $("#" + controlID_window_base + '1').jqxWindow(windowSettings);
    $("#" + controlID_window_base + '2').jqxWindow(windowSettings);
    $("#" + controlID_window_base + '3').jqxWindow(windowSettings);
    $("#" + controlID_window_base + '4').jqxWindow(windowSettings);
    $("#" + controlID_window_base + '5').jqxWindow(windowSettings);
   
    $("#" + controlID_ribbon_zoom_100).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_zoom_fit).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_reset).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_fullscreen).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_lineage).jqxButton({ theme: theme, height: "100%", width: 64, disabled: false });
    $("#" + controlID_ribbon_undo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_redo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_zoom_slider).jqxSlider({ theme: theme, width: 150, showButtons: true, min: 750, max: 2250, value: 1500, showTicks: false });

    $("#" + controlID_ribbon_sourcerule).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $('#' + controlID_ribbon_sourcerule_cancel).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_sourcerule_save).jqxButton({ theme: theme, height: "100%", width: 64 });


    $('#' + controlID_ribbon_lineage_cancel).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_lineage_addItem).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_lineage_save).jqxButton({ theme: theme, height: "100%", width: 64 });

    $("#" + controlID_ribbon_multimaprule).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true }).hide();
    $("#" + controlID_ribbon_maprule).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true }).hide();
    $('#' + controlID_ribbon_maprule_add).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_maprule_cancel).jqxButton({ theme: theme, height: "100%", width: 64 });
    $('#' + controlID_ribbon_maprule_save).jqxButton({ theme: theme, height: "100%", width: 64 });

    $('.lineage').hide();
    $('.sourcerule').hide();
    $('.sourcemapping').hide();

    $("#" + controlID_ribbon_expander).jqxExpander({ theme: theme }).jqxExpander('collapse');

    $('#' + controlID_ribbon_view_1).jqxRadioButton({ theme: theme, checked: true });
    $('#' + controlID_ribbon_view_2).jqxRadioButton({ theme: theme, checked: false });
    $('#' + controlID_ribbon_view_3).jqxRadioButton({ theme: theme, checked: false });

    //#endregion

    $("#" + controlID_view_window_base + '0').on('click', function () {
        $("#" + controlID_window_base + '0').jqxWindow('open');
    });
    $("#" + controlID_view_window_base + '1').on('click', function () {
        $("#" + controlID_window_base + '1').jqxWindow('open');
    });
    $("#" + controlID_view_window_base + '2').on('click', function () {
        $("#" + controlID_window_base + '2').jqxWindow('open');
    });
    $("#" + controlID_view_window_base + '3').on('click', function () {
        $("#" + controlID_window_base + '3').jqxWindow('open');
    });
    $("#" + controlID_view_window_base + '4').on('click', function () {
        $("#" + controlID_window_base + '4').jqxWindow('open');
    });
    $("#" + controlID_view_window_base + '5').on('click', function () {
        $("#" + controlID_window_base + '5').jqxWindow('open');
    });


    //#region Manage Command Handlers

    $("#" + controlID_ribbon_lineage).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;

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

    $("#" + controlID_ribbon_sourcerule).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
        
        var selected = myDiagram.selection;
        
        if (selected == null)
            return;

        var selected = selected.first().data;

        if (selected == null)
            return;

        var data = {
            target: type,
            targetID: id,
            object: selected.type,
            objectID: selected.id,
            ID: 0,
            controlID: controlID
        };

        $('#' + controlID_wrapper).hide();
        $('.diagramcommands').hide();

        sourceRuleModel = new MapSequencesModel(data, permissions);
        ko.cleanNode($('#' + controlID_popover_sourcerule_editor_body)[0]);
        ko.applyBindings(sourceRuleModel, $('#' + controlID_popover_sourcerule_editor_body)[0]);

        $('#' + controlID_popover_sourcerule_editor).show();
        $('.sourcerule').show();
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

    //#endregion

    //#region Business Lineage Command Bar Handlers

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
            $('.lineage').fadeOut();
            $('.diagramcommands').show();
            $('#' + controlID_popover_lineage_editor).hide();
            $('#' + controlID_wrapper).show();

            populateDiagram();  //refresh the diagram.
        });
    });

    //#endregion

    //#region Source Rule Command Bar Handlers

    $('#' + controlID_ribbon_sourcerule_cancel).on('click', function () {
        $('.lineage').fadeOut();
        $('.diagramcommands').show();
        $('#' + controlID_popover_lineage_editor).hide();
        $('#' + controlID_wrapper).show();
    });

    $('#' + controlID_ribbon_sourcerule_save).on('click', function () {
        sourceRuleModel.Save().then(function (data) {
            $('.sourcerule').fadeOut();
            $('.diagramcommands').show();
            $('#' + controlID_popover_sourcerule_editor).hide();
            $('#' + controlID_wrapper).show();

            populateDiagram();  //refresh the diagram.
        });
    });

    //#endregion

    //#region Technical Lineage Command Bar Handlers

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

    //#endregion

    //#region General Ribbon Command Handlers

    $('#' + controlID_ribbon_fullscreen).on('click', function () {
        if ($(this).jqxButton('disabled'))
            return;
        toggleFullscreen();
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

        type = originalObject;
        id = originalObjectID;
        populateDiagram();
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

    //#region Event Handlers

    $('#' + controlID_ribbon_expander).on('expanded', toggleRibbon);
    $('#' + controlID_ribbon_expander).on('collapsed', toggleRibbon);

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

    //#endregion

    var oldHeight = $('#' + controlID_wrapper).height();
    var oldWidth = $('#' + controlID_wrapper).width();

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

    //#region methods

    function mappingBindingComplete(event) {
        $('#' + controlID_mappingrules_content).jqxGrid('autoresizecolumns');
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

    function mouseEnter(e, node) {
        node.isShadowed = true;
    };

    function mouseLeave(e, node) {
        node.isShadowed = false;
    };

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
        } else {
            $('#' + controlID_ribbon_fullscreen).html(defaultHtml);
            $('#' + controlID_wrapper_fullscreen).attr('style', 'z-index:1000000;background-color:white;');
            $('#' + controlID_wrapper).height(520);
            $('#' + controlID_diagram).height(520);
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

        var delay = 0;
        var defaultInfo = '<div style="color:#999;height:25px;text-align:center">Nothing selected</div>';
        var errorInfo = '<div style="color:maroon;height:100px;text-align:center">An error occurred</div>';

        if (!data || data == null) {
            $("#" + controlID_info_detail).html('');
        } else {
            if (data.diagramObjectType == 'Node') {

                $("#" + controlID_info_detail).html('');

                try {
                    technicalRelationsSource.url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + type + '&sID=' + id + '&t=' + data.obj + '&tID=' + data.objid;
                    $('#' + controlID_fusion_content).jqxGrid('updatebounddata');
                } catch (e) { }
                try {
                    lineageResponsibilitySource.url = '/api/' + data.obj + '/' + data.objid + '/ownership?showHidden=false';
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
                }).fail(function () {
                    $('#' + controlID_info_detail).html(errorInfo);
                });

                $.ajax({
                    url: '/api/' + type + '/' + id + '/sources/' + data.obj + '/' + data.objid + '/rules',
                    async: true
                }).done(function (data) {
                    var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                    $('#' + controlID_sourcerules_content).html(sourceTemplate(data));
                }).fail(function () {
                    $('#' + controlID_sourcerules_content).html(defaultTabContent);
                });

                mapItemsSource.url = null;
                $('#' + controlID_mappingrules_content).jqxGrid('updatebounddata');

                var tranCount = 0;

                var links = myDiagram.model.linkDataArray;
                for (var i = 0; i < links.length; i++) {
                    if (links[i].to == data.key && links[i].hasTransformations)
                        tranCount++;
                }

            } else if (data.diagramObjectType == 'Link') {
                var from = myDiagram.model.findNodeDataForKey(data.from);
                var to = myDiagram.model.findNodeDataForKey(data.to);

                var selectedMapID = data.id;

                $.ajax({
                    url: '/api/' + type + '/' + id + '/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/rules',
                    async: true
                }).done(function (data) {
                    var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                    $('#' + controlID_sourcerules_content).html(sourceTemplate(data));
                }).fail(function () {
                    $('#' + controlID_sourcerules_content).html(defaultTabContent);
                });


                if (from.template !== "Fusion" && to.template !== "Fusion") {
                    mapItemsSource.url = '/api/maps/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/mapitems';
                }
                else {
                    mapItemsSource.url = null;
                }
                $('#' + controlID_mappingrules_content).jqxGrid('updatebounddata');


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
            }
        }
    }

    function toggleButtons(data) {
        var delay = 200;

        if (!readonly) {
            $("#" + controlID_ribbon_lineage).show(delay);
        } else {
            $("#" + controlID_ribbon_lineage).hide(delay);
        }
        if (data == null) {
            $("#" + controlID_ribbon_sourcerule).hide(delay);
            $("#" + controlID_ribbon_multimaprule).show(delay);
            $("#" + controlID_ribbon_maprule).hide(delay);
        } else {
            $("#" + controlID_ribbon_multimaprule).hide(delay);

            if (data.diagramObjectType == 'Node') {
                if (!readonly) {
                    //$("#" + controlID_ribbon_maprule).show(delay);
                    $("#" + controlID_ribbon_sourcerule).show(delay);
                } else {
                    //$("#" + controlID_ribbon_maprule).hide(delay);
                    $("#" + controlID_ribbon_sourcerule).hide(delay);
                }
            } else {
                $("#" + controlID_ribbon_sourcerule).hide(delay);

                if (!readonly) {
                    $("#" + controlID_ribbon_maprule).show(delay);
                } else {
                    $("#" + controlID_ribbon_maprule).hide(delay);
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

        myDiagram.focus();
        myDiagram.requestUpdate();
    }

    function parseData(data) {
        myDiagram.startTransaction("load_all_data");
        myDiagram.model.nodeDataArray = [];
        myDiagram.model.linkDataArray = [];
        initialNodes = [];
        initialLinks = [];
        var modelList = [];
        var linkList = [];
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

    function reOrderLayout() {
        myDiagram.layout.invalidateLayout();
        myDiagram.requestUpdate();
    }

    //#endregion

    //#region Constructor Logic

    if (readonly) {
        $('.editcommands').hide();
    }

    var g = go.GraphObject.make;
    var myDiagram = initializeDiagram();

    myDiagram.addDiagramListener('ViewportBoundsChanged', function () {
        var s = myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        $('#' + controlID_ribbon_zoom_text).text(Math.round(myDiagram.scale * 100) + '%');
        $('#' + controlID_ribbon_zoom_slider).val(Math.round(myDiagram.scale * 1500));
    });
    myDiagram.addDiagramListener('ChangedSelection', function (e) {
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
    });
    myDiagram.addDiagramListener('ObjectDoubleClicked', function (e) {
        var obj = e.diagram.selection.first().data;
        if (obj != null) {
            if (obj.diagramObjectType == 'Node') {
                type = obj.obj;
                id = obj.objid;

                populateDiagram();
            }
        }
    });

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

    amplify.subscribe("NoFusionAvailable", function () {
        $('#' + controlID_popover_maprule_editor).height(300);
        $('#' + controlID_ribbon_maprule_add).hide();
        $('#' + controlID_ribbon_maprule_save).hide();
    });

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

