(function ($) {

    //#region

    var CanEdit = false;
    var CanDelete = false;

    var GridSource = {
        dataType: "json",
        url: null,
        dataFields: [
            { name: 'IntersectID' },
            { name: 'Object' },
            { name: 'ObjectID' },
            { name: 'SubjectArea' },
            { name: 'ParentID' },
            { name: 'ParentName' },
            { name: 'ParentUrl' },
            { name: 'Name' },
            { name: 'Description' },
            { name: 'ObjectTypeName' },
            { name: 'Url' }
        ]
    };

    var GridAdapter = new $.jqx.dataAdapter(GridSource);

    var headerControl = $('<div>Synonyms</div>');
    var countControl = $('<span></span>');
    var contentControl = $('<div></div>');
    var gridControl = $('<div></div>');
    var toolbarControl = $('<div></div>');

    //#endregion

    var methods = {
        init: function (options) {
            var defaults = {
                object: null,
                objectID: null,
                collapsible: true,
                title: 'Synonyms',
                canEdit: false,
                canDelete: false
            };

            // Extend default with any options that were provided.
            options = $.extend(defaults, options);

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Synonyms');

                if (!data) {

                    CanEdit = options.canEdit;
                    CanDelete = options.canDelete;

                    if (options.object && options.objectID) {
                        loadUI($this, options);
                    }

                    $(this).data('Synonyms', {
                        Target: $this,
                        Options: options
                    });
                }
            });
        },
        reload: function (object, objectID, canEdit, canDelete) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Synonyms'),
                    options = data.Options;

                CanEdit = canEdit;
                CanDelete = canDelete;

                options.canEdit = canEdit;
                options.canDelete = canDelete;
                options.object = object;
                options.objectID = objectID;

                GridSource.url = '/api/' + options.object + '/' + options.objectID + '/synonyms';
                gridControl.jqxGrid('updatebounddata');
                loadToolbar(options.object, options.objectID);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Synonyms');

                amplify.unsubscribe("CancelAction", cancelAction);
                amplify.unsubscribe("SaveAction", saveAction);
                gridControl.jqxGrid('destroy');
                $this.removeData('Synonyms');
                $this.html('');
            });
        }
    };

    $.fn.Synonyms = function (method) {
        //#region Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.Synonyms');
        }
        //#endregion
    };

    function bindingComplete(event) {
        var count = 0;
        try {
            count = gridControl.jqxGrid('getrows').length;
        } catch (e) {
            count = 0;
        }
        countControl.html(" (" + count + ")");
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Synonym:
                    gridControl.jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Synonyms.js : SaveAction", e);
        }
    }

    var loadToolbar = function (type, id) {
        if (CanEdit) {
            toolbarControl[0].id = "Tools" + type + id;
            TileTools('#' + toolbarControl[0].id, [
                { icon: 'plus', uri: '/form/AddSynonym?type=' + type + '&id=' + id, context: contextList.Synonym, title: 'Add synonym' }
            ]);
        }
    }

    function loadUI($obj, options) {
        try {
            var $this = $obj;

            if (options.object && options.objectID) {
                //#region

                $this.html('');

                contentControl.html('');
                toolbarControl.html('');
                gridControl.html('');

                $this.css('margin', '10px');

                headerControl.append(countControl);
                $this.append(headerControl);

                if (options.title != '') {
                    if (!options.collapsible) {
                        $this.append("<header>" + options.title + "</header>");
                        headerControl.hide();
                    }
                }

                //if (options.collapsible) {
                //    contentControl.css('padding-top', '20px').css('min-height', '150px');
                //}

                contentControl.append('<header style="width: 98%; margin-top: 10px"></header>');
                contentControl.find('header').append(toolbarControl);
                contentControl.append(gridControl);

                $this.append(contentControl);

                loadToolbar(options.object, options.objectID);

                if (options.collapsible) {
                    $this.jqxExpander({ theme: theme, expanded: false, height: 'auto' });
                }

                //#region Grid Logic

                GridSource.url = '/api/' + options.object + '/' + options.objectID + '/synonyms';

                gridControl.jqxGrid({
                    source: GridAdapter,
                    width: overlay_grid_width,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    autoheight: true,
                    autorowheight: true,
                    sortable: true,
                    altrows: true,
                    showfilterrow: true,
                    filterable: true,
                    pageable: false,
                    theme: 'flat',
                    columnsresize: true,
                    autoshowloadelement: false,
                    selectionmode: 'none',
                    columns: [
                        { datafield: "ObjectTypeName", text: "Type", width: '200px', filtertype: 'checkedlist' },
                        {
                            datafield: "Name",
                            text: "Name",
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                return previewLinkRenderer(data.Object, data.ObjectID, data.Url, data.Name);
                            }
                        },
                        {
                            datafield: "ParentName",
                            text: "Parent",
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                if (data.ParentName)
                                    return previewLinkRenderer("Artifact", data.ParentID, data.ParentUrl, data.ParentName);
                                else
                                    return "";
                            }
                        },
                        { datafield: "SubjectArea", text: CompanySettings.ArtifactType_TaxonomyTypeID, width: '200px', filtertype: 'checkedlist' },
                        {
                            datafield: "IntersectID",
                            text: "",
                            sortable: false,
                            filterable: false,
                            sortable: false,
                            width: '80px',
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                var tools = [];

                                tools.push({ isitemlink: true, urlprefix: data.Url, type: data.Object, id: data.ObjectID, context: 'Preview' });
                                if (CanDelete) {
                                    tools.push({ icon: 'trash-o', urlprefix: 'form/DeleteSynonym?id=' + data.IntersectID });
                                }
                                return renderToolsHtml(value, tools, contextList.Synonym, data);
                            }
                        }
                    ]
                });

                gridControl.on('bindingcomplete', bindingComplete);
                amplify.subscribe("SaveAction", saveAction);

                //#endregion

                //#endregion
            }
            else {
                $this.html('');
            }

            if ($this.html() == '') {
                $this.hide();
            }
        } catch (e) {
            logError("Synonyms.js : loadUI", e);
        }
    }

})(jQuery);