(function ($) {

    //#region

    var ReadOnly = false;

    var TreeGridSource = {
        dataType: "json",
        url: null,
        dataFields: [
            { name: 'ID' },
            { name: 'TypeID', type: 'int' },
            { name: 'IsCategory', type: 'bool' },
            { name: 'IsTechnical', type: 'bool' },
            { name: 'ShowNameInTree', type: 'bool' },
            { name: 'ObjectType', type: 'string' },
            { name: 'ObjectID', type: 'int' },
            { name: 'TargetObjectType', type: 'string' },
            { name: 'TargetObjectID', type: 'int' },
            { name: 'ParentObjectType', type: 'string' },
            { name: 'ParentObjectID', type: 'int' },
            { name: 'ObjectTypeName', type: 'string' },
            { name: 'expanded', type: 'bool' },
            { name: 'Name', type: 'string' },
            { name: 'Items', type: 'array' }
        ],
        hierarchy:
        {
            root: 'Items'
        },
        id: 'ID'
    };

    var TreeGridAdapter = new $.jqx.dataAdapter(TreeGridSource);

    var headerControl = $('<div>Attributes</div>');
    var countControl = $('<span></span>');
    var contentControl = $('<div></div>');
    var treeControl = $('<div></div>');
    var toolbarControl = $('<div></div>');
    var detailControl = $('<div></div>');
    //var viewerControl = $('<div></div>');
    var editorControl = $('<div></div>');

    //#endregion

    var methods = {
        init: function (options) {
            var defaults = {
                object: null,
                objectID: null,
                collapsible: true,
                title: 'Attributes',
                readOnly: false
            };

            // Extend default with any options that were provided.
            options = $.extend(defaults, options);

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Attributes');

                //$this.addClass("Question");

                if (!data) {

                    if (options.object && options.objectID) {
                        loadUI($this, options);
                    }

                    $(this).data('Attributes', {
                        Target: $this,
                        Options: options
                    });
                }
                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        reload: function (object, objectID, readonly) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Attributes'),
                    options = data.Options;

                ReadOnly = readonly;

                options.readOnly = readonly;
                options.object = object;
                options.objectID = objectID;

                TreeGridSource.url = '/attributes/hierarchy/' + options.object + '/' + options.objectID;
                treeControl.jqxTreeGrid('updateBoundData');

                //loadUI($this, options);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Attributes');

                amplify.unsubscribe("CancelAction", cancelAction);
                amplify.unsubscribe("SaveAction", saveAction);
                //treeControl.off('bindingComplete', treeControlBindingComplete);
                //treeControl.off('rowSelect', treeControlRowSelect);
                treeControl.jqxTreeGrid('destroy');
                $this.removeData('Attributes');
                $this.html('');
            });
        }
    };

    $.fn.Attributes = function (method) {
        //#region Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.Attributes');
        }
        //#endregion
    };

    var attributeSwitchToViewer = function (t, i) {
        detailControl[0].id = t + i;

        editorControl.html('');
        if (t === "Attribute" && i) {
            ObjectDetail(detailControl[0].id, t, i);
        }
        else {
            detailControl.html('');
        }
        editorControl.hide();
        detailControl.fadeIn();//viewerControl.fadeIn();

        if (i && t === "Attribute") {
            ObjectDetail(detailControl.id, t, i);
        }
        else {
            detailControl.html('');
        }
    }

    function cancelAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    var row = treeControl.jqxTreeGrid('getSelection')[0];
                    if (row) {
                        attributeSwitchToViewer(row.ObjectType, row.ObjectID);
                    }
                    break;
            }
        } catch (e) {
            logError("Attributes.js : CancelAction", e);
        }
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    treeControl.jqxTreeGrid('updateBoundData');
                    switch (data.action) {
                        case "add":
                            //load child items under selected tree node.
                            if (data.id) {
                                treeControl.jqxTreeGrid('selectRow', data.id);
                                attributeSwitchToViewer('Attribute', data.id);
                            }
                            break;
                        case "delete":
                            attributeSwitchToViewer(null, null);
                            break;
                        case "edit":
                            //reload selected tree node.
                            treeControl.jqxTreeGrid('selectRow', data.id);
                            attributeSwitchToViewer('Attribute', data.id);
                            break;
                    }
                    break;
            }
        } catch (e) {
            logError("Attributes.js : SaveAction", e);
        }
    }

    var loadToolbar = function (type, id, owner, ownerID, attributeID) {
        if (!ReadOnly) { //&& attributeID
            $.getJSON(
                '/attributes/AttributeActions?type=' + type + '&id=' + id + '&owner=' + owner + '&ownerID=' + ownerID + '&attributeID=' + attributeID,
                function (data) {
                    if (data) {
                        toolbarControl.html('');
                        var menu = $("<div style='border: none !important;'></div>");
                        menu.append(loadMenuItems(data, ""));
                        toolbarControl.append(menu);
                        menu.jqxMenu({ showTopLevelArrows: false, enableRoundedCorners: false, theme: theme, autoOpenPopup: true, mode: 'horizontal' });
                        menu.on('itemclick', function (event) {
                            var li = event.args;

                            if ($(li).data("uri") === null)
                                return;

                            detailControl.hide(); //viewerControl.hide();
                            editorControl.html(progressIndicatorHtml).fadeIn();
                            editorControl.load($(li).data("uri"));
                        });
                    }
                }
            );
        }
    }

    var loadMenuItems = function (data, html) {
        try {
            if (data) {

                html = "<ul>";

                $.each(data, function (idx, t) {
                    html += "<li data-uri='" + t.Uri + "'><i class='fa fa-" + t.Icon + "'";
                    if (t.Title !== "" && t.Title) {
                        html += " title='" + encodeURI(t.Title) + "'></i>" + t.Title
                    }
                    else {
                        html += "></i>";
                    }

                    if (t.Items.length > 0) {
                        html += loadMenuItems(t.Items);
                    }
                    html += "</li>";
                });

                html += "</ul>";
            }
        } catch (e) {
            logError("Attributes.js : loadMenuItems", e);
        }

        return html;
    }

    function treeControlBindingComplete(evt) {
        var calculateCount = function (row, count) {
            if (row.records) {
                count += row.records.length;
                $.each(row.records, function () {
                    count = calculateCount(this, count);
                });
            }
            return count;
        };

        var count = 0;
        try {
            var topRows = treeControl.jqxTreeGrid('getRows');
            $.each(topRows, function () {
                count = calculateCount(this, count);
            });
        } catch (e) {
            count = 0;
        }
        countControl.text(' (' + count + ')');

        treeControl.jqxTreeGrid('selectRow', 'EC');
    }

    function treeControlRowSelect(evt) {
        try {
            // event args.
            var args = evt.args;
            // row data.
            var row = args.row;
            // row key.
            var key = args.key;

            if (row) {
                var t = row.ObjectType;//null;
                var i = row.ObjectID;//null;
                var detailtype = null;
                var detailid = null;
                var roottype = row.ParentObjectType; //null;
                var rootid = row.ParentObjectID; //null;
                var attributeID = null;
                var targetType = row.TargetObjectType;

                if (t === 'Attribute') {
                    attributeID = i;
                }

                if (targetType) {
                    detailtype = targetType;
                    detailid = row.TargetObjectID;
                }
                else {
                    detailtype = t;
                    detailid = i;
                }

                loadToolbar(t, i, roottype, rootid, attributeID);

                attributeSwitchToViewer(detailtype, detailid);
            }
        } catch (e) {
            logError("Attributes.js : treeControlRowSelect", e);
        }
    }

    function onExpand() {
        treeControl.jqxTreeGrid('render');
    }

    function loadUI($obj, options) {
        try {
            var $this = $obj;

            if (options.object && options.objectID) {
                //#region

                $this.html('');

                contentControl.html('');
                detailControl.html('');
                toolbarControl.html('');
                treeControl.html('');

                $this.css('margin', '10px');

                headerControl.append(countControl);
                $this.append(headerControl);

                if (options.title != '') {
                    if (!options.collapsible) {
                        $this.append("<header>" + options.title + "</header>");
                        headerControl.hide();
                    }
                }

                if (options.collapsible) {
                    contentControl.css('padding-top', '20px').css('min-height', '150px');
                }
                contentControl.append("<div class='row'><div class='col l5 m5 s6'></div><div class='col l7 m7 s6'></div></div>");

                contentControl.find('.l5').append(treeControl);

                //contentControl.find('.l7').append(viewerControl);
                contentControl.find('.l7').append(toolbarControl);
                contentControl.find('.l7').append(detailControl);
                contentControl.find('.l7').append(editorControl);

                $this.append(contentControl);

                //$this.html('<div>Attributes<span id="' + controlID_count + '"></span></div><div style="min-height: 150px"><div id="' + controlID_sub + '"></div></div>');
                if (options.collapsible) {
                    $this.jqxExpander({ theme: theme, expanded: false });
                }

                //#region TreeGrid Logic

                TreeGridSource.url = '/attributes/hierarchy/' + options.object + '/' + options.objectID;

                treeControl.jqxTreeGrid({
                    width: '99.5%',
                    theme: list_theme,
                    showHeader: false,
                    selectionMode: 'singleRow',
                    source: TreeGridAdapter,
                    sortable: true,
                    icons: true,
                    columns: [
                      {
                          text: 'Name',
                          dataField: 'Name',
                          width: '100%',
                          cellsRenderer: function (rowKey, dataField, value, data) {
                              if (data.IsCategory) {
                                  return "<span class='Attribute-Category'>" + data.Name + "</span>";
                              }
                              else {
                                  return ((data.ShowNameInTree) ? "<b>" + data.ObjectTypeName + "</b> : " : "") + data.Name
                              }
                          }
                      }
                    ]//,
                    //ready: function () {
                    //    try {
                    //        var rows = treeControl.jqxTreeGrid('getRows');
                    //        if (rows.length > 0) {
                    //            treeControl.jqxTreeGrid('selectRow', ((rows[0].Items[0]) ? rows[0].Items[0].uid : rows[0].uid));
                    //        }
                    //    } catch (e) {
                    //        console.log(e);
                    //    }
                    //}
                });

                //$this.on('expanded', onExpand);
                treeControl.on('bindingComplete', treeControlBindingComplete);
                treeControl.on('rowSelect', treeControlRowSelect);
                amplify.subscribe("CancelAction", cancelAction);
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
            logError("Attributes.js : loadUI", e);
        }
    }

})(jQuery);