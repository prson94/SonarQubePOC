(function ($) {

    amplify.request.define("PageActionsRequest", "ajax", { url: '/api/{type}/{id}/actions/{context}', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                uri: null,
                context: 'form'
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Editor'),
                    Editor = null;

                //$this.addClass("form");

                if (!data) {

                    if (options.uri) {
                        Editor = loadFields($this, options, options.uri);
                    }

                    $(this).data('Editor', {
                        Target: $this,
                        Editor: Editor,
                        Options: options
                    });

                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Editor');

                data.Editor.remove();
                $this.removeData('Editor');
                //$(window).unbind('.tooltip');
            });
        },
        reload: function (uri, context) {
            if (!context) context = "form";
            var $this = $(this).data("Editor").Target;
            var options = $(this).data("Editor").Options;
            options.uri = uri;
            options.context = context;

            if (uri) {
                loadFields($this, options, uri);
            }
            else {
                clear($this);
            }
        }
    };

    $.fn.Editor = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.tooltip');
        }

    };

    //#region Private Methods

    function clear($obj) {
        $obj.html('');
    };

    function loadFields($obj, options, uri) {
        $obj.append("<i class='fa fa-spinner fa-spin fa-4x'></i>");
        $.get(uri, function (fields) {
            parseFields($obj, options, fields);
        }).error(function (xhr, status, error) {
            clear($obj);
            //xhr.responseJSON
            if (xhr.statusCode == "403")
            {
                $obj.append("<h2>Unauthorized</h2>");
            }
            $obj.append("<div class='error'>" + error + "</div>");
            amplify.publish("ErrorOccurredAction", { context: options.context });
        });
    };

    function addValidator(v, validatorRules) {
        try {
            if (v.Validations) {
                $.each(v.Validations, function (i, validation) {
                    try {
                        var rule = validation.rule;
                        if (validation.regex) {
                            rule = function () {

                                var value;

                                switch (v.FieldType) {
                                    case 'Link':
                                    case 'UncLink':
                                        value = $('#' + v.FieldName + '_Url').val();
                                        break;
                                    default:
                                        value = $('#' + v.FieldName).val();
                                        break;
                                }

                                validation.regex = validation.regex.replace('\\\\', '\\');
                                console.log(validation.regex);
                                var regex = new RegExp(validation.regex);
                                return regex.test(value);
                            }
                        }

                        switch (v.FieldType) {
                            case 'Link':
                            case 'UncLink':
                                if (!validation.regex) {
                                    validatorRules.push({ input: '#' + v.FieldName + '_Name', message: validation.message, action: validation.action, rule: rule });
                                }
                                validatorRules.push({ input: '#' + v.FieldName + '_Url', message: validation.message, action: validation.action, rule: rule });
                                break;
                            default:
                                validatorRules.push({ input: '#' + v.FieldName, message: validation.message, action: validation.action, rule: rule });
                                break;
                        }
                    } catch (e) {
                        console.log(e);
                    }
                });
            }
        } catch (e) {

        }
    }

    function getTextElementByColor(color) {
        if (color == 'transparent' || color.hex == "") {
            return $("<div style='text-shadow: none; position: relative; padding-bottom: 2px; margin-top: 2px;'>transparent</div>");
        }
        var element = $("<div style='text-shadow: none; position: relative; padding-bottom: 2px; margin-top: 2px;'>#" + color.hex + "</div>");
        var nThreshold = 105;
        var bgDelta = (color.r * 0.299) + (color.g * 0.587) + (color.b * 0.114);
        var foreColor = (255 - bgDelta < nThreshold) ? 'Black' : 'White';
        element.css('color', foreColor);
        element.css('background', "#" + color.hex);
        element.addClass('jqx-rc-all');
        return element;
    }

    function addLabel(panel, field, materializeLabel) {

        var fieldFriendlyName = field.Name;
        if (field.ScriptProperty) {
            fieldFriendlyName = eval(field.ScriptProperty);
        }

        materializeLabel = false; //Hard-coded to always be false for now.  Checkboxes not showing up correctly in the case of true.
        if (materializeLabel) {
            panel.addClass('input-field');

            var activeClassSetting = (field.Value != '') ? 'class="active"' : '';
            panel.append("<label id='Tip_" + field.FieldName + "' for='" + field.FieldName + "' " + activeClassSetting + ">" + fieldFriendlyName + "</label>");
        }
        else {
            panel.append("<div id='Tip_" + field.FieldName + "' class='FieldName'>" + fieldFriendlyName + "</div>");
        }

        if (field.FieldDescription && field.FieldDescription != '') {
            $('#Tip_' + field.FieldName).qtip({
                content: {
                    text: field.FieldDescription,
                    position: {
                        my: 'top right',  // Position my top left...
                        at: 'bottom left', // at the bottom right of...
                        target: $('#' + field.FieldName) // my target
                    }
                },
                style: {
                    classes: 'qtip-blue qtip-shadow'
                }
            });

            //amplify.subscribe(AmplifyActions.Unsubscribe, function () {
            //    $('#Tip_' + field.FieldName).qtip('destroy');
            //});
        }
    }

    function parseFields($obj, options, fields) {
        try {
            if (fields) {
                clear($obj);

                $obj.append("<input type='hidden' name='_context' id='_context' value='" + options.context + "' />");

                var validatorRules = [];

                //#region Build the form layout.

                var tableMatrix = [];
                var currentRow = 0;
                var tabMatrixItem;
                $.each(fields, function (idx, v) {
                    if (v.Row) {
                        if (v.Row != currentRow) {
                            if (tabMatrixItem) tableMatrix.push(tabMatrixItem);
                            currentRow = v.Row;
                            tabMatrixItem = { Row: currentRow, Columns: 0, ColumnCount: 0 };
                        }
                        if (v.Column) {
                            if (tabMatrixItem.ColumnCount < v.Column) {
                                tabMatrixItem.ColumnCount = v.Column;
                                tabMatrixItem.Columns = Math.round(12 / v.Column);
                            }
                        }
                    }
                });
                if (tabMatrixItem) tableMatrix.push(tabMatrixItem);   //Add the last item to make sure we get the last row.

                if (tableMatrix.length > 0) {
                    var currentColumn = 0;
                    var layoutHtml = "";

                    //layoutHtml += "<div class='row'>";

                    $.each(tableMatrix, function (i, m) {

                        var fieldCountClass = "";

                        switch (m.ColumnCount) {
                            case 2:
                                fieldCountClass = "s6";
                                break;
                            case 3:
                                fieldCountClass = "s4";
                                break;
                            case 4:
                                fieldCountClass = "s3";
                                break;
                            default:
                                fieldCountClass = "s12";
                                break;
                        }

                        layoutHtml += "<div class='row'>";

                        currentColumn = 1;
                        while (currentColumn <= m.ColumnCount) {
                            layoutHtml += "<div id='col_" + m.Row + "_" + currentColumn + "' class='col " + fieldCountClass + "' style='margin-bottom: 0px'></div>";
                            currentColumn++;
                        }

                        layoutHtml += "</div>";
                        
                        
                    });
                    
                    //layoutHtml += "</div>";

                    $obj.append(layoutHtml);
                }

                //#endregion

                //#region Add the fields

                $.each(fields, function (idx, v) {

                    var fld;
                    var cleanedValue = "";

                    if (v.Value != "" && v.Value != "null" && v.Value) {
                        cleanedValue = v.Value;
                    }

                    if (v.FieldType == "Hidden") {
                        $obj.append("<input type='hidden' name='" + v.FieldName + "' id='" + v.FieldName + "' value='" + cleanedValue + "' />");
                    }
                    else {

                        var cpnl = $('#col_' + v.Row + '_' + v.Column);

                        switch (v.FieldType) {
                            case 'Search':
                                //#region Search Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');
                                var src = {
                                    datatype: "json",
                                    datafields: [
                                        { name: 'Name' },
                                        { name: 'ID' }
                                    ],
                                    url: v.DataUri
                                };

                                var adt = new $.jqx.dataAdapter(src, {
                                    formatData: function (data) {
                                        if (fld.jqxComboBox('searchString') != undefined) {
                                            data.prefix = fld.jqxComboBox('searchString');
                                            return data;
                                        }
                                    }
                                });

                                fld.jqxComboBox({
                                    autoComplete: true,
                                    disabled: v.ReadOnly,
                                    multiSelect: v.MultiSelect,
                                    remoteAutoComplete: true,
                                    remoteAutoCompleteDelay: 1000,
                                    minLength: 2,
                                    height: field_height,
                                    width: field_width,
                                    dropDownWidth: field_width,
                                    source: adt,
                                    theme: theme,
                                    selectedIndex: 0,
                                    placeHolder: "Please choose...",
                                    displayMember: "Name",
                                    valueMember: "ID",
                                    renderer: function (index, label, value) {
                                        var item = adt.records[index];
                                        if (item != null) {
                                            var label = item.Name;
                                            return label;
                                        }
                                        return "";
                                    },
                                    search: function (str) {
                                        adt.dataBind();
                                    }
                                });

                                cpnl.append(fld);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxComboBox('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxComboBox('destroy');
                                });
                                break;

                                //#endregion
                            case 'Lookup':
                            case 'DropDown':
                            case 'FusionLookup':
                                //#region DropDown Field Management

                                //if (v.MultiSelect) {
                                    addLabel(cpnl, v, false);

                                    fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');

                                    var src = [];

                                    $.each(v.Items, function () {
                                        var group = null;
                                        if (this.Group) group = this.Group.Name;
                                        src.push({ label: this.Text, value: this.Value, checked: this.Selected, group: group });
                                    });

                                    fld.jqxDropDownList({
                                        placeHolder: "Please choose...",
                                        disabled: v.ReadOnly,
                                        filterable: true,
                                        searchMode: 'containsignorecase',
                                        checkboxes: v.MultiSelect,
                                        enableBrowserBoundsDetection: true,
                                        width: '100%',
                                        height: field_height,
                                        source: src,
                                        theme: theme
                                    });

                                    if (v.MultipleValues) {
                                        $.each(v.MultipleValues, function (ix, itm) {
                                            var optionToSelect = fld.jqxDropDownList('getItemByValue', itm);
                                            fld.jqxDropDownList('checkItem', optionToSelect);
                                        });
                                    }
                                    else {
                                        if (v.Value) {
                                            fld.val(v.Value);
                                        }
                                        else {
                                            fld.jqxDropDownList({ selectedIndex: 0 });
                                        }
                                    }

                                    cpnl.append(fld);

                                    amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                        fld.jqxDropDownList('destroy');
                                    });
                                    amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                        fld.jqxDropDownList('destroy');
                                    });
                                //}
                                //else {
                                //    fld = $('<select id="' + v.FieldName + '" name="' + v.FieldName + '"' + (v.ReadOnly ? "disabled=\"true\"" : "") + '></select>');

                                //    $.each(v.Items, function () {
                                //        var optionHtml = '<option value="' + this.Value + '"';
                                //        if (v.Value == this.Value) {
                                //            optionHtml += ' selected = "selected"';
                                //        }
                                //        optionHtml += '>' + this.Text + '</option>';
                                //        fld.append(optionHtml);
                                //    });

                                //    cpnl.append(fld);
                                //    addLabel(cpnl, v, true);
                                //}
                                break;

                                //#endregion
                            case 'DropDownGrid':
                                //#region DropDownGrid Field Management

                                addLabel(cpnl, v, false);

                                var selectedIndex = 0;
                                if (v.Value) {
                                    $.each(v.Items, function (ix, itm) {
                                        //alert(v.Value + ' ' + itm.Value);
                                        if (v.Value == itm.Value) {
                                            selectedIndex = ix;
                                            //alert(selectedIndex);
                                        }
                                    });
                                }

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');
                                var src = {
                                    localdata: v.Items,
                                    datatype: "json",
                                    datafields: [
                                        { name: 'Text' },
                                        { name: 'Value' }
                                    ]
                                };

                                var adt = new $.jqx.dataAdapter(src);

                                fld.jqxDropDownList({
                                    disabled: v.ReadOnly,
                                    width: field_width,
                                    height: field_height,
                                    source: adt,
                                    theme: theme,
                                    filterable: true,
                                    searchMode: 'containsignorecase',
                                    selectedIndex: selectedIndex,
                                    placeHolder: "Please choose...",
                                    displayMember: "Text",
                                    valueMember: "Value"
                                });

                                cpnl.append(fld);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxDropDownList('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxDropDownList('destroy');
                                });
                                break;

                                //#endregion
                            //case 'File':
                            //    //#region File Field Management

                            //    fld = $("<input type='file' name='" + v.FieldName + "' id='" + v.FieldName + "'/>");
                            //    break;

                            //    //#endregion
                            case 'Boolean':
                                //#region Boolean Field Management

                                addLabel(cpnl, v, true);

                                var b = false;
                                if (cleanedValue == true || cleanedValue == "true" || cleanedValue == "1" || cleanedValue == "True") b = true;
                                var checkboxHtml = '<input type="checkbox" id="' + v.FieldName + '" name="' + v.FieldName + '"';
                                if (b) checkboxHtml += ' checked="checked"';
                                checkboxHtml += ' />';

                                fld = $(checkboxHtml);

                                cpnl.append(fld);
                                break;

                                //#endregion
                            case 'Date':
                                //#region Date Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');
                                var date = new Date();
                                if (cleanedValue != '') {
                                    if (moment(cleanedValue).isValid()) {
                                        date = moment(cleanedValue);
                                    }
                                }
                                else {
                                    date = moment();
                                }
                                fld.jqxDateTimeInput({ disabled: v.ReadOnly, theme: theme, formatString: 'd', showCalendarButton: true, height: field_height });
                                fld.jqxDateTimeInput('setDate', date.toDate());
                                addValidator(v, validatorRules);

                                cpnl.append(fld);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxDateTimeInput('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxDateTimeInput('destroy');
                                });
                                break;

                                //#endregion
                            case 'DateTime':
                                //#region DateTime Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');
                                var datetime = new Date();
                                if (cleanedValue != '') {
                                    if (moment(cleanedValue).isValid()) {
                                        datetime = moment(cleanedValue);
                                    }
                                }
                                else {
                                    date = moment();
                                }
                                fld.jqxDateTimeInput({ disabled: v.ReadOnly, theme: theme, formatString: 'F', showCalendarButton: true, height: field_height });
                                fld.jqxDateTimeInput('setDate', datetime.toDate());
                                addValidator(v, validatorRules);

                                cpnl.append(fld);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxDateTimeInput('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxDateTimeInput('destroy');
                                });

                                break;

                                //#endregion
                            case 'Decimal':
                                //#region Decimal Field Management

                                addLabel(cpnl, v, false);

                                var minValue = -99999999;
                                var maxValue = 99999999;

                                if (v.RangeMin) {
                                    minValue = v.RangeMin;
                                }

                                if (v.RangeMax) {
                                    maxValue = v.RangeMax;
                                }

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');
                                fld.jqxNumberInput({ disabled: v.ReadOnly, theme: theme, min: minValue, max: maxValue, height: field_height, width: field_width, inputMode: 'simple', decimalDigits: 3, groupSeparator: ',', decimal: cleanedValue });
                                //addValidator(v, validatorRules);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxNumberInput('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxNumberInput('destroy');
                                });

                                cpnl.append(fld);
                                break;

                                //#endregion
                            case 'Color':
                                //#region Color Field Management

                                addLabel(cpnl, v, false);

                                var fldColorHidden = $('<input id="' + v.FieldName + '" name="' + v.FieldName + '" type="hidden" value="' + cleanedValue + '" />');
                                cpnl.append(fldColorHidden);

                                //if (cleanedValue != '') cleanedValue = cleanedValue.replace('#', '');

                                
                                fld = $('<div id="' + v.FieldName + "_Picker" + '"></div>');
                                fld.jqxColorPicker({ colorMode: 'hue', width: field_width, height: 225 });
                                fld.jqxColorPicker('setColor', cleanedValue);
                                addValidator(v, validatorRules);

                                fld.on('colorchange', function (event) {
                                    var color = '#' + fld.jqxColorPicker('getColor').hex;//event.args;
                                    fldColorHidden.val(color);
                                });

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxColorPicker('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxColorPicker('destroy');
                                });

                                cpnl.append(fld);
                                break;

                                //#endregion
                            case 'Number':
                            case 'Integer':
                                //#region Integer Field Management

                                addLabel(cpnl, v, false);

                                var minValue = -99999999;
                                var maxValue = 99999999;

                                if (v.RangeMin) {
                                    minValue = v.RangeMin;
                                }

                                if (v.RangeMax) {
                                    maxValue = v.RangeMax;
                                }

                                fld = $('<div id="' + v.FieldName + '" name="' + v.FieldName + '"></div>');

                                fld.jqxNumberInput({ disabled: v.ReadOnly, theme: theme, min: minValue, max: maxValue, height: field_height, width: field_width, promptChar: '', spinButtons: true, decimalDigits: 0, groupSeparator: '', decimal: cleanedValue });
                                //addValidator(v, validatorRules);

                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    fld.jqxNumberInput('destroy');
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    fld.jqxNumberInput('destroy');
                                });

                                cpnl.append(fld);
                                break;

                                //#endregion
                            case "Html":
                                //#region Html Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<textarea id="' + v.FieldName + '" name="' + v.FieldName + '"></textarea>');
                                cpnl.append(fld);
                                fld.redactor();//({ toolbar: false });
                                fld.redactor('set', cleanedValue);
                                fld.val(cleanedValue);

                                cpnl.append(fld);


                                amplify.subscribe(AmplifyActions.OverlayUnsubscribe, function () {
                                    try {
                                        fld.redactor('core.destroy');
                                    } catch (e) { }
                                });
                                amplify.subscribe(AmplifyActions.Unsubscribe, function () {
                                    try {
                                        fld.redactor('core.destroy');
                                    } catch (e) { }
                                });

                                break;

                                //#endregion
                            case "Password":
                                //#region Password Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<input type="password" id="' + v.FieldName + '" name="' + v.FieldName + '" />');
                                try {
                                    fld.jqxPasswordInput({ width: field_width, height: field_height, showStrength: true, showStrengthPosition: "right" });
                                }
                                catch (ex) {

                                }
                                addValidator(v, validatorRules);

                                cpnl.append(fld);
                                break;

                                //#endregion
                            case "Link":
                                //#region Link Field Management

                                addLabel(cpnl, v, false);

                                var valueLinkName = "";
                                var valueLinkUrl = "";

                                if (cleanedValue != "")
                                {
                                    var linkArray = cleanedValue.split("|");
                                    valueLinkName = linkArray[0];
                                    valueLinkUrl = linkArray[1];
                                }

                                var fldName = $('<input id="' + v.FieldName + '_Name" name="' + v.FieldName + '_Name" value="' + valueLinkName + '" type="text" />');
                                var fldUrl = $('<input id="' + v.FieldName + '_Url" name="' + v.FieldName + '_Url" value="' + valueLinkUrl + '" type="text" />');
                                try {
                                    fldName.jqxInput({ disabled: v.ReadOnly, theme: theme, width: field_width, height: field_height });
                                    fldUrl.jqxInput({ disabled: v.ReadOnly, theme: theme, width: field_width, height: field_height });
                                    //fld.jqxPasswordInput({width: field_width, placeHolder: "Enter password:", showStrength: true, showStrengthPosition: "right"});
                                    cpnl.append(fldName);                       //$obj
                                    cpnl.append('<span>(Link Name)</span>');    //$obj
                                    cpnl.append('<br/>');                       //$obj
                                    cpnl.append(fldUrl);                        //$obj
                                    cpnl.append('<span>(Link Url)</span>');     //$obj
                                }
                                catch (ex) {

                                }
                                addValidator(v, validatorRules);
                                break;

                                //#endregion
                            case "UncLink":
                                //#region UncLink Field Management

                                addLabel(cpnl, v, false);

                                var valueUncLinkName = "";
                                var valueUncLinkUrl = "";

                                if (cleanedValue != "") {
                                    var uncLinkArray = cleanedValue.split("|");
                                    valueUncLinkName = uncLinkArray[0];
                                    valueUncLinkUrl = uncLinkArray[1];
                                }

                                var fldName = $('<input id="' + v.FieldName + '_Name" name="' + v.FieldName + '_Name" value="' + valueUncLinkName + '" type="text" />');
                                var fldUrl = $('<input id="' + v.FieldName + '_Url" name="' + v.FieldName + '_Url" value="' + valueUncLinkUrl + '" type="text" />');
                                try {
                                    fldName.jqxInput({ disabled: v.ReadOnly, theme: theme, width: field_width, height: field_height });
                                    fldUrl.jqxInput({ disabled: v.ReadOnly, theme: theme, width: field_width, height: field_height });
                                    cpnl.append(fldName);                               //$obj
                                    cpnl.append('<span>(File/Network Name)</span>');    //$obj
                                    cpnl.append('<br/>');                               //$obj
                                    cpnl.append(fldUrl);                                //$obj
                                    cpnl.append('<span>(File/Network Path)</span>');    //$obj
                                }
                                catch (ex) {

                                }
                                addValidator(v, validatorRules);
                                break;

                                //#endregion
                            default: //String, Text
                                //#region Text Field Management

                                addLabel(cpnl, v, false);

                                fld = $('<input id="' + v.FieldName + '" name="' + v.FieldName + '" type="text" />');
                                fld.val(cleanedValue)
                                fld.jqxInput({ disabled: v.ReadOnly, theme: theme, width: field_width, height: field_height });
                                addValidator(v, validatorRules);

                                cpnl.append(fld);
                                break;

                                //#endregion
                        }
                    }
                });

                //#endregion

                $('#form0').jqxValidator({ rules: validatorRules, hintType : 'label' });
                $('#form0').on('validationSuccess', function (event) {
                    $('#form0').submit();
                });
                $('.saveButton').on('click', function () {
                    $('#form0').jqxValidator('validate');
                });
            }
        } catch (e) {
            logError("Editor.js : parseFields", e);
        }
    }

    //#endregion

})(jQuery);