//#region    BINDINGS
ko.bindingHandlers.fadeVisible = {
    init: function (element, valueAccessor) {
        // Initially set the element to be instantly visible/hidden depending on the value
        var value = valueAccessor();
        $(element).toggle(ko.unwrap(value)); // Use "unwrapObservable" so we can handle values that may or may not be observable
    },
    update: function (element, valueAccessor) {
        // Whenever the value subsequently changes, slowly fade the element in or out
        var value = valueAccessor();
        ko.unwrap(value) ? $(element).slideDown(300) : $(element).slideUp(300);
    }
};

ko.bindingHandlers.htmlareasimple = {
    init: function (element, valueAccessor) {
        var value = valueAccessor();

        if (ko.isObservable(value)) {
            $(element).redactor({
                changeCallback: value,
                buttons: ['formatting', 'bold', 'italic', 'deleted', 'unorderedlist','orderedlist','outdent','indent','link','fontcolor','backcolor','alignment']
            });
        }
    },
    update: function (element, valueAccessor) {
        var value = ko.utils.unwrapObservable(valueAccessor()) || '';
        if (value !== $(element).redactor('get')) {
            $(element).redactor('set', value);
        }
    }
};

ko.bindingHandlers.htmlarea = {
    init: function (element, valueAccessor) {
        var value = valueAccessor();

        if (ko.isObservable(value)) {
            $(element).redactor({
                changeCallback: value,
                imageUploadCallback:function(image, json)
                {
                    image.css("max-width", "100%").css("max-height", "100%");
                },
                buttons: ['formatting', 'bold', 'italic', 'deleted', 'unorderedlist', 'orderedlist', 'outdent', 'indent', 'image', 'video', 'link', 'fontcolor', 'backcolor', 'alignment', 'horizontalrule']
            });
        }
    },
    update: function(element, valueAccessor) {
        var value = ko.utils.unwrapObservable(valueAccessor()) || '';
        if (value !== $(element).redactor('get')) {
            $(element).redactor('set', value);
        }
    }
};

ko.bindingHandlers.sourceSystemFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.SourceSystem(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {}
};

ko.bindingHandlers.sourceObjectFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.SourceObject(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.sourceFusionAttributeFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.SourceFusionAttribute(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.targetSystemFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.TargetSystem(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.targetObjectFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.TargetObject(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.targetFusionAttributeFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.TargetFusionAttribute(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.actionFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Action(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.intersectTypeFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.IntersectType(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.subjectFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Subject(event.args.item.value);
            viewModel.SubjectName(event.args.item.label);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.objectFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Object(event.args.item.value);
            viewModel.ObjectName(event.args.item.label);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.typeFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Type(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.mapDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            var intersectArray = event.args.item.value.split('|');
            if (intersectArray.length == 4) {
                viewModel.SourceIntersectID(intersectArray[0]);
                viewModel.SourceDiagramKey(intersectArray[1]);
                viewModel.TargetIntersectID(intersectArray[2]);
                viewModel.TargetDiagramKey(intersectArray[3]);
            }
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

ko.bindingHandlers.mappingRuleItemInput = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('select', function () {
            viewModel.FusionAttributeID($(element).val().value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};

var fileBindings = {
    customFileInputSystemOptions: {
        wrapperClass: 'custom-file-input-wrapper',
        fileNameClass: 'custom-file-input-file-name',
        buttonGroupClass: 'custom-file-input-button-group',
        buttonClass: 'custom-file-input-button',
        clearButtonClass: 'custom-file-input-clear-button',
        buttonTextClass: 'custom-file-input-button-text',
    },
    defaultOptions: {
        wrapperClass: 'input-group',
        fileNameClass: 'disabled form-control',
        noFileText: 'No file chosen',
        buttonGroupClass: 'input-group-btn',
        buttonClass: 'btn btn-primary',
        clearButtonClass: 'btn btn-default',
        buttonText: 'Choose File',
        changeButtonText: 'Change',
        clearButtonText: 'Clear',
        fileName: true,
        clearButton: true,
        onClear: function (fileData, options) {
            if (typeof fileData.clear === 'function') {
                fileData.clear();
            }
        }
    },
}

var windowURL = window.URL || window.webkitURL;

ko.bindingHandlers.fileInput = {
    init: function (element, valueAccessor) {
        element.onchange = function () {
            var fileData = ko.utils.unwrapObservable(valueAccessor()) || {};
            if (fileData.dataUrl) {
                fileData.dataURL = fileData.dataUrl;
            }
            if (fileData.objectUrl) {
                fileData.objectURL = fileData.objectUrl;
            }
            fileData.file = fileData.file || ko.observable();

            var file = this.files[0];
            if (file) {
                fileData.file(file);
            }

            if (!fileData.clear) {
                fileData.clear = function () {
                    $.each(['file', 'objectURL', 'base64String', 'binaryString', 'text', 'dataURL', 'arrayBuffer'], function (i, property) {
                        if (fileData[property] && ko.isObservable(fileData[property])) {
                            if (property == 'objectURL') {
                                windowURL.revokeObjectURL(fileData.objectURL());
                            }
                            fileData[property](null);
                        }
                    });
                    element.value = '';
                }
            }
            if (ko.isObservable(valueAccessor())) {
                valueAccessor()(fileData);
            }
        };
        element.onchange();
    },
    update: function (element, valueAccessor, allBindingsAccessor) {

        var fileData = ko.utils.unwrapObservable(valueAccessor());

        var file = ko.isObservable(fileData.file) && fileData.file();

        if (fileData.objectURL && ko.isObservable(fileData.objectURL)) {
            var newUrl = file && windowURL.createObjectURL(file);
            if (newUrl) {
                var oldUrl = fileData.objectURL();
                if (oldUrl) {
                    windowURL.revokeObjectURL(oldUrl);
                }
                fileData.objectURL(newUrl);
            }
        }


        if (fileData.base64String && ko.isObservable(fileData.base64String)) {
            if (fileData.dataURL && ko.isObservable(fileData.dataURL)) {
                // will be handled
            }
            else {
                fileData.dataURL = ko.observable(); // hack
            }
        }

        // var properties = ['binaryString', 'text', 'dataURL', 'arrayBuffer'], property;
        // for(var i = 0; i < properties.length; i++){
        //     property = properties[i];
        ['binaryString', 'text', 'dataURL', 'arrayBuffer'].forEach(function (property) {
            var method = 'readAs' + (property.substr(0, 1).toUpperCase() + property.substr(1));
            if (property != 'dataURL' && !(fileData[property] && ko.isObservable(fileData[property]))) {
                return true;
            }
            if (!file) {
                return true;
            }
            var reader = new FileReader();
            reader.onload = function (e) {
                //if (fileData[property]) {
                //    fileData[property](e.target.result);
                //}
                //if (method == 'readAsDataURL' && fileData.base64String && ko.isObservable(fileData.base64String)) {
                //    var resultParts = e.target.result.split(",");
                //    if (resultParts.length === 2) {
                //        fileData.base64String(resultParts[1]);
                //    }
                //}


                //var chars = new Uint8Array(e.target.result);
                //var CHUNK_SIZE = 0x8000;
                //var index = 0;
                //var length = chars.length;
                //var result = '';
                //var slice;
                //while (index < length) {
                //    slice = chars.subarray(index, Math.min(index + CHUNK_SIZE, length));
                //    result += String.fromCharCode.apply(null, slice);
                //    index += CHUNK_SIZE;
                //}
                //fileData.base64String(result);
                fileData.dataURL(e.target.result);
            };

            //reader[method](file);

            reader.readAsDataURL(file);
            //reader.readAsArrayBuffer(file);

            //var binary = "";
            //var bytes = new Uint8Array(e.target.result);
            //var length = bytes.byteLength;

            //for (var i = 0; i < length; i++) {
            //    binary += String.fromCharCode(bytes[i]);
            //}

            //att.Body = (new sforce.Base64Binary(binary)).toString();
        });
    }
};

ko.bindingHandlers.fileDrag = {
    update: function (element, valueAccessor, allBindingsAccessor) {
        var fileData = ko.utils.unwrapObservable(valueAccessor()) || {};

        if (!$(element).data("fileDragInjected")) {
            element.classList.add('filedrag');
            element.ondragover = element.ondragleave = element.ondrop = function (e) {
                e.stopPropagation();
                e.preventDefault();
                if (e.type == 'dragover') {
                    element.classList.add('hover');
                }
                else {
                    element.classList.remove('hover');
                }
                if (e.type == 'drop' && e.dataTransfer) {
                    var files = e.dataTransfer.files;
                    var file = files[0];
                    if (file) {
                        fileData.file(file);
                        if (ko.isObservable(valueAccessor())) {
                            valueAccessor()(fileData);
                        }
                    }
                }
            };

            $(element).data("fileDragInjected", true);
        }
    }
};

ko.bindingHandlers.customFileInput = {
    init: function (element, valueAccessor, allBindingsAccessor) {
        if (ko.utils.unwrapObservable(valueAccessor()) === false) {
            return;
        }
        //*
        var sysOpts = fileBindings.customFileInputSystemOptions;
        var defOpts = fileBindings.defaultOptions;

        var $element = $(element);
        var $wrapper = $('<span>').addClass(sysOpts.wrapperClass).addClass(defOpts.wrapperClass);
        var $buttonGroup = $('<span>').addClass(sysOpts.buttonGroupClass).addClass(defOpts.buttonGroupClass);
        $buttonGroup.append($('<span>').addClass(sysOpts.buttonClass));
        $element.wrap($wrapper).wrap($buttonGroup);
        var $buttonGroup = $element.parent('.' + sysOpts.buttonClass).parent();
        $buttonGroup.before($('<input>').attr('type', 'text').attr('disabled', 'disabled').addClass(sysOpts.fileNameClass));
        $element.before($('<span>').addClass(sysOpts.buttonTextClass));

    },
    update: function (element, valueAccessor, allBindingsAccessor) {
        var options = ko.utils.unwrapObservable(valueAccessor());
        if (options === false) {
            return;
        }
        options = options || {};
        if (options && typeof options !== 'object') {
            options = {};
        }

        var sysOpts = fileBindings.customFileInputSystemOptions;
        var defOpts = fileBindings.defaultOptions;

        options = $.extend(defOpts, options);

        var allBindings = allBindingsAccessor();
        if (!allBindings.fileInput) {
            return;
        }
        var fileData = ko.utils.unwrapObservable(allBindings.fileInput) || {};

        var file = ko.utils.unwrapObservable(fileData.file);

        var $button = $(element).parent();
        var $buttonGroup = $button.parent();

        var $wrapper = $buttonGroup.parent();
        $button.addClass(ko.utils.unwrapObservable(options.buttonClass));
        $button.find('.' + sysOpts.buttonTextClass)
                .html(ko.utils.unwrapObservable(file ? options.changeButtonText : options.buttonText));
        var $fileName = $wrapper.find('.' + sysOpts.fileNameClass);
        $fileName.addClass(ko.utils.unwrapObservable(options.fileNameClass));

        if (file && file.name) {
            $fileName.val(file.name);
        }
        else {
            $fileName.val(ko.utils.unwrapObservable(options.noFileText));
        }

        var $clearButton = $buttonGroup.find('.' + sysOpts.clearButtonClass);
        if (!$clearButton.length) {
            $clearButton = $('<span>').addClass(sysOpts.clearButtonClass);
            $clearButton.on('click', function (e) {
                options.onClear(fileData, options);
            });
            $buttonGroup.append($clearButton);
        }
        $clearButton.html(ko.utils.unwrapObservable(options.clearButtonText));
        $clearButton.addClass(ko.utils.unwrapObservable(options.clearButtonClass));


        if (file && options.clearButton && file.name) {
            //                $clearButton.show();
        }
        else {
            $clearButton.remove();
        }
    }
};

//#endregion

//#region Technical Mapping (MapRule)

function MultiMapRulesModel(data, permissions) {
    var self = this;

    //#region Simple Properties

    self.IsInitialLoading = ko.observable(false);

    self.MapRules = ko.observableArray([]);

    self.IsLoading = ko.observable(false);

    self.NoFusionAvailable = ko.observable(false);

    self.CanUpdate = ko.observable(false);
    self.CanDelete = ko.observable(false);
    self.CanAdd = ko.observable(false);

    //#endregion

    if (permissions != null) {
        if (permissions.HasPermission("Relationship", "Create"))
            self.CanAdd(true);
        if (permissions.HasPermission("Relationship", "Update"))
            self.CanUpdate(true);
        if (permissions.HasPermission("Relationship", "Delete"))
            self.CanDelete(true);
    }

    self.AddRule = function () {
        var addRuleModel = {
            Maps: data.Maps
        };
        if (self.MapRules().length > 0) {
            var previousMapRule = self.MapRules()[self.MapRules().length - 1];
            addRuleModel.SourceIntersectID = previousMapRule.TargetIntersectID();
            addRuleModel.SourceDiagramKey = previousMapRule.TargetDiagramKey();
            addRuleModel.Sources = previousMapRule.Targets();
        }
        self.MapRules.push(new MultiMapRuleModel(addRuleModel, self, permissions));
    }

    self.LoadRules = function () {
        self.IsInitialLoading(true);
        self.IsLoading(true);

        $.ajax({
            method: 'post',
            url: 'form/MapRulesByObject',
            data: { Items: data.Maps}
        }).done(function (maprules) {
            self.MapRules(
                $.map(maprules, function (item) {
                    item.Maps = data.Maps;
                    return new MultiMapRuleModel(item, self, permissions);
                })
            );
        }).always(function () {
            self.IsLoading(false);
            self.IsInitialLoading(false);
        });
    }

    self.SaveRules = function () {

        var deferred = $.Deferred();

        if (self.IsLoading())
            return;
        var modelToSave = {};
        var rules = [];
        var error = false;

        self.IsLoading(true);
        for (var i = 0; i < self.MapRules().length; i++) {
            var rule = self.MapRules()[i];

            rule.ErrorMessages([]);
            if (rule.Sources().length == 0) {
                rule.ErrorMessages.push('You must add at least 1 source item.');
                error = true;
            }
            if (rule.Targets().length == 0) {
                rule.ErrorMessages.push('You must add at least 1 target item.');
                error = true;
            }
            if (rule.Transformation() == '') {
                rule.ErrorMessages.push('Please enter a transformation rule.');
                error = true;
            }

            var sources = [];
            var targets = [];
            for (var j = 0; j < rule.Sources().length; j++) {
                var source = rule.Sources()[j];

                source.ErrorMessages([]);
                if (source.FusionAttributeID() <= 0 || !source.FusionAttributeID()) {
                    source.ErrorMessages.push('Please select a valid attribute.');
                    error = true;
                }
                if (error)
                    continue;

                sources.push({
                    ID: source.ID(),
                    FusionAttributeID: source.FusionAttributeID(),
                    IntersectID: source.IntersectID(),
                    FusionAttributeTextPath: source.FusionAttributeTextPath()
                });
            }
            for (var j = 0; j < rule.Targets().length; j++) {
                var target = rule.Targets()[j];

                target.ErrorMessages([]);
                if (target.FusionAttributeID() <= 0 || !target.FusionAttributeID()) {
                    target.ErrorMessages.push('Please select a valid attribute.');
                    error = true;
                }
                if (error)
                    continue;

                targets.push({
                    ID: target.ID(),
                    FusionAttributeID: target.FusionAttributeID(),
                    IntersectID: target.IntersectID(),
                    FusionAttributeTextPath: target.FusionAttributeTextPath()
                });
            }
            rules.push({
                ID: rule.ID(),
                SourceIntersectID: rule.SourceIntersectID(),
                SourceDiagramKey: rule.SourceDiagramKey(),
                TargetIntersectID: rule.TargetIntersectID(),
                TargetDiagramKey: rule.TargetDiagramKey(),
                Sources: sources,
                Targets: targets,
                Transformation: rule.Transformation()
            });
        }

        modelToSave = {
            Rules: rules
        };

        var count = modelToSave.Rules.length;

        if (error) {
            self.IsLoading(false);
        } else {
            $.ajax({
                url: '/form/MapRules_Save',
                method: 'POST',
                data: modelToSave
            }).done(function (saveResultData) {
                if (saveResultData.error) {
                    //self.SaveMessage(saveResultData.message);
                    //self.LoadRules();
                } else {
                    //self.SaveMessage('<span style="color:green"><i class="fa fa-check-circle"></i> Changes saved successfully.</span>');
                    amplify.publish("SaveAction", {
                        context: 'mappingrule',
                        count: count
                    });
                    self.LoadRules();
                }
            }).always(function () {
                self.IsLoading(false);
                deferred.resolve();
            });
        }

        return deferred.promise();
    }

    self.LoadRules();

    return self;
}

function MultiMapRuleModel(defaultData, parent, permissions) {
    var self = this;

    self.ID = ko.observable(defaultData.ID || 0);
    self.Sources = ko.observableArray([]);
    self.Targets = ko.observableArray([]);
    self.Transformation = ko.observable(defaultData.Transformation || '');

    self.MapSelectedIndex = ko.observable(-1);

    self.ErrorMessages = ko.observableArray([]);

    self.Maps = ko.observableArray(
        $.map(defaultData.Maps, function (item) { return new MapSelectionModel(item); })
        || []
    );
    //self.Maps = ko.computed(function () {
    //    return parent.Maps;
    //}, self);

    self.SourceIntersectID = ko.observable(defaultData.SourceIntersectID || 0);
    self.SourceDiagramKey = ko.observable(defaultData.SourceDiagramKey || '');
    self.TargetIntersectID = ko.observable(defaultData.TargetIntersectID || 0);
    self.TargetDiagramKey = ko.observable(defaultData.TargetDiagramKey || '');

    self.HasSourceIntersectID = ko.computed(function () {
        return (self.SourceIntersectID() > 0);
    }, self);

    self.HasTargetIntersectID = ko.computed(function () {
        return (self.TargetIntersectID() > 0);
    }, self);

    self.IsLoading = ko.observable(false);

    self.CanUpdate = ko.observable(false);
    self.CanDelete = ko.observable(false);
    self.CanAdd = ko.observable(false);

    //self.Sources.subscribe(function () {
    //    if (!parent.IsInitialLoading()) {
    //        var indexStillValid = false;
    //        for (var i = 0; i < data.Maps().length; i++) {
    //            if (data.Maps()[i].ValueText().indexOf(self.SourceIntersectID() + '|') > -1) {
    //                indexStillValid = true;
    //            }
    //        }

    //        if (!indexStillValid) {
    //            self.Sources([]);
    //        }
    //    }
    //}, self);

    self.SourceIntersectID.subscribe(function () {
        if (!parent.IsInitialLoading()) {
            var isDifferentIntersect = false;
            for (var i = 0; i < self.Sources().length; i++) {
                if (self.Sources()[i].IntersectID() != self.SourceIntersectID()) {
                    isDifferentIntersect = true;
                }
            }

            if (isDifferentIntersect) {
                var originalLength = self.Sources().length - 1;
                for (var i = originalLength; i >= 0; i--) {
                    if (i > 0) {
                        self.Sources.remove(self.Sources()[i]);
                    }
                    else {
                        self.Sources()[i].IntersectID(self.SourceIntersectID());
                        self.Sources()[i].FusionAttributeID(null);
                        self.Sources()[i].FusionAttributeTextPath(null);
                    }
                }
            }
        }
    }, self);

    self.TargetIntersectID.subscribe(function () {
        if (!parent.IsInitialLoading()) {
            var isDifferentIntersect = false;
            for (var i = 0; i < self.Targets().length; i++) {
                if (self.Targets()[i].IntersectID() != self.TargetIntersectID()) {
                    isDifferentIntersect = true;
                }
            }

            if (isDifferentIntersect) {
                var originalLength = self.Targets().length - 1;
                for (var i = originalLength; i >= 0; i--) {
                    if (i > 0) {
                        self.Targets.remove(self.Targets()[i]);
                    }
                    else {
                        self.Targets()[i].IntersectID(self.TargetIntersectID());
                        self.Targets()[i].FusionAttributeID(null);
                        self.Targets()[i].FusionAttributeTextPath(null);
                    }
                }
            }
        }
    }, self);

    if (permissions != null) {
        if (permissions.HasPermission("Relationship", "Create"))
            self.CanAdd(true);
        if (permissions.HasPermission("Relationship", "Update"))
            self.CanUpdate(true);
        if (permissions.HasPermission("Relationship", "Delete"))
            self.CanDelete(true);
    }

    self.LoadItems = function () {
        self.IsLoading(true);

        if (defaultData != null && !$.isEmptyObject(defaultData)) {
            if (defaultData.Sources != null)
                for (var i = 0; i < defaultData.Sources.length; i++) {
                    self.Sources.push(new MapRuleItemModel(defaultData.Sources[i], self, true));
                }
            else
                self.Sources.push(new MapRuleItemModel({
                    IntersectID: self.SourceIntersectID()
                }, self, true));

            if (defaultData.Targets != null)
                for (var i = 0; i < defaultData.Targets.length; i++) {
                    self.Targets.push(new MapRuleItemModel(defaultData.Targets[i], self, false));
                }
            else
                self.Targets.push(new MapRuleItemModel({
                    IntersectID: self.TargetIntersectID()
                }, self, false));


            for (var i = 0; i < self.Maps().length; i++) {
                if (self.Maps()[i].ValueText() == self.SourceIntersectID() + '|' + self.SourceDiagramKey() + '|' + self.TargetIntersectID() + '|' + self.TargetDiagramKey()) {
                    self.MapSelectedIndex(i);
                }
            }

            // Must have two lines below as the two values are reset with selectedIndex logic above, due to a timing issue.
            self.SourceIntersectID(defaultData.SourceIntersectID);
            self.TargetIntersectID(defaultData.TargetIntersectID);
        }

        self.IsLoading(false);
    }
    
    self.AddSource = function () {
        self.Sources.push(new MapRuleItemModel({
            IntersectID: self.SourceIntersectID()
        }, self, true));
    }

    self.AddTarget = function () {
        self.Targets.push(new MapRuleItemModel({
            IntersectID: self.TargetIntersectID()
        }, self, false));
    }

    self.RemoveRule = function () {
        parent.MapRules.remove(self);
    }

    self.LoadItems();

    return self;
}

function MapRulesModel(data, permissions) {
    var self = this;

    //#region Simple Properties

    self.SourceName = ko.observable(data.SourceName);
    self.SourceIntersectID = ko.observable(data.SourceIntersectID);
    self.SourceDiagramKey = ko.observable(data.SourceDiagramKey || '');

    self.TargetName = ko.observable(data.TargetName);
    self.TargetIntersectID = ko.observable(data.TargetIntersectID);
    self.TargetDiagramKey = ko.observable(data.TargetDiagramKey || '');

    self.IsInitialLoading = ko.observable(false);

    self.MapRules = ko.observableArray([]);
    
    self.IsLoading = ko.observable(false);

    self.NoFusionAvailable = ko.observable(false);

    self.CanUpdate = ko.observable(false);
    self.CanDelete = ko.observable(false);
    self.CanAdd = ko.observable(false);

    //#endregion

    if (permissions != null) {
        if (permissions.HasPermission("Relationship", "Create"))
            self.CanAdd(true);
        if (permissions.HasPermission("Relationship", "Update"))
            self.CanUpdate(true);
        if (permissions.HasPermission("Relationship", "Delete"))
            self.CanDelete(true);
    }

    self.LoadRules = function () {
        self.IsInitialLoading(true);
        self.IsLoading(true);
        $.ajax({
            method: 'post',
            url: 'form/MapRulesByMap',
            data: {
                SourceIntersectID: self.SourceIntersectID(),
                SourceDiagramKey: self.SourceDiagramKey(),
                TargetIntersectID: self.TargetIntersectID(),
                TargetDiagramKey: self.TargetDiagramKey()
            }
        }).done(function (maprules) {

            self.MapRules([]);

            if (maprules.length == 0) {
                self.NoFusionAvailable(true);
            }
            else {
                for (var i = 0; i < maprules.length; i++) {
                    maprules[i].SourceIntersectID = self.SourceIntersectID();
                    maprules[i].TargetIntersectID = self.TargetIntersectID();
                    self.MapRules.push(new MapRuleModel(maprules[i], self, permissions));
                }
            }

        }).always(function () {
            self.IsLoading(false);
            self.IsInitialLoading(false);
        });
    }

    self.AddRule = function () {
        var sourceRuleData = {
            SourceIntersectID: self.SourceIntersectID(),
            SourceDiagramKey: self.SourceDiagramKey(),
            TargetIntersectID: self.TargetIntersectID(),
            TargetDiagramKey: self.TargetDiagramKey()
        };
        self.MapRules.push(new MapRuleModel(sourceRuleData, self, permissions));
    }

    self.SaveRules = function () {

        var deferred = $.Deferred();

        if (self.IsLoading())
            return;
        var modelToSave = {};
        var rules = [];
        var error = false;

        self.IsLoading(true);
        for (var i = 0; i < self.MapRules().length; i++) {
            var rule = self.MapRules()[i];

            rule.ErrorMessages([]);
            if (rule.Sources().length == 0) {
                rule.ErrorMessages.push('You must add at least 1 source item.');
                error = true;
            }
            if (rule.Targets().length == 0) {
                rule.ErrorMessages.push('You must add at least 1 target item.');
                error = true;
            }
            if (rule.Transformation() == '') {
                rule.ErrorMessages.push('Please enter a transformation rule.');
                error = true;
            }

            var sources = [];
            var targets = [];
            for (var j = 0; j < rule.Sources().length; j++) {
                var source = rule.Sources()[j];

                source.ErrorMessages([]);
                if (source.FusionAttributeID() <= 0 || !source.FusionAttributeID()) {
                    source.ErrorMessages.push('Please select a valid attribute.');
                    error = true;
                }
                if (error)
                    continue;

                sources.push({
                    ID: source.ID(),
                    FusionAttributeID: source.FusionAttributeID(),
                    IntersectID: source.IntersectID(),
                    FusionAttributeTextPath: source.FusionAttributeTextPath()
                });
            }
            for (var j = 0; j < rule.Targets().length; j++) {
                var target = rule.Targets()[j];

                target.ErrorMessages([]);
                if (target.FusionAttributeID() <= 0 || !target.FusionAttributeID()) {
                    target.ErrorMessages.push('Please select a valid attribute.');
                    error = true;
                }
                if (error)
                    continue;

                targets.push({
                    ID: target.ID(),
                    FusionAttributeID: target.FusionAttributeID(),
                    IntersectID: target.IntersectID(),
                    FusionAttributeTextPath: target.FusionAttributeTextPath()
                });
            }
            rules.push({
                ID: rule.ID(),
                SourceIntersectID: self.SourceIntersectID(),
                SourceDiagramKey: self.SourceDiagramKey(),
                TargetIntersectID: self.TargetIntersectID(),
                TargetDiagramKey: self.TargetDiagramKey(),
                Sources: sources,
                Targets: targets,
                Transformation: rule.Transformation()
            });
        }

        modelToSave = {
            Rules: rules
        };

        var count = modelToSave.Rules.length;

        if (error) {
            self.IsLoading(false);
        } else {
            $.ajax({
                url: '/form/MapRules_Save',
                method: 'POST',
                data: modelToSave
            }).done(function (saveResultData) {
                if (saveResultData == null || saveResultData.error == null || saveResultData.error == true) {
                    //self.SaveMessage('<span style="color:maroon"><i class="fa fa-exclaimation-circle"></i> An error occurred while saving the source rules.</span>');
                    self.LoadRules();
                } else {
                    //self.SaveMessage('<span style="color:green"><i class="fa fa-check-circle"></i> Changes saved successfully.</span>');
                    amplify.publish("SaveAction", {
                        context: 'mappingrule',
                        count: count,
                        fromIntersectId: self.SourceIntersectID(),
                        toIntersectId: self.TargetIntersectID()
                    });
                    self.LoadRules();
                }
            }).always(function () {
                self.IsLoading(false);
                deferred.resolve();
            });
        }

        return deferred.promise();
    }

    self.LoadRules();

    return self;
}

function MapRuleModel(defaultData, parent, permissions) {
    var self = this;

    self.ID = ko.observable(defaultData.ID || 0);
    self.Sources = ko.observableArray([]);
    self.Targets = ko.observableArray([]);
    self.Transformation = ko.observable(defaultData.Transformation || '');

    self.ErrorMessages = ko.observableArray([]);

    self.SourceIntersectID = ko.observable(defaultData.SourceIntersectID || 0);
    self.SourceDiagramKey = ko.observable(defaultData.SourceDiagramKey || '');
    self.TargetIntersectID = ko.observable(defaultData.TargetIntersectID || 0);
    self.TargetDiagramKey = ko.observable(defaultData.TargetDiagramKey || '');

    self.IsLoading = ko.observable(false);

    self.CanUpdate = ko.observable(false);
    self.CanDelete = ko.observable(false);
    self.CanAdd = ko.observable(false);

    if (permissions != null) {
        if (permissions.HasPermission("Relationship", "Create"))
            self.CanAdd(true);
        if (permissions.HasPermission("Relationship", "Update"))
            self.CanUpdate(true);
        if (permissions.HasPermission("Relationship", "Delete"))
            self.CanDelete(true);
    }

    self.LoadItems = function () {
        self.IsLoading(true);

        if (defaultData != null && !$.isEmptyObject(defaultData)) {
            if (defaultData.Sources != null)
                for (var i = 0; i < defaultData.Sources.length; i++) {
                    self.Sources.push(new MapRuleItemModel(defaultData.Sources[i], self, true));
                }
            else
                self.Sources.push(new MapRuleItemModel({
                    IntersectID: self.SourceIntersectID()
                }, self, true));

            if (defaultData.Targets != null)
                for (var i = 0; i < defaultData.Targets.length; i++) {
                    self.Targets.push(new MapRuleItemModel(defaultData.Targets[i], self, false));
                }
            else
                self.Targets.push(new MapRuleItemModel({
                    IntersectID: self.TargetIntersectID()
                }, self, false));
        }

        self.IsLoading(false);
    }

    self.AddSource = function () {
        self.Sources.push(new MapRuleItemModel({
            IntersectID: self.SourceIntersectID()
        }, self, true));
    }

    self.AddTarget = function () {
        self.Targets.push(new MapRuleItemModel({
            IntersectID: self.TargetIntersectID()
        }, self, false));
    }

    self.RemoveRule = function () {
        parent.MapRules.remove(self);
    }

    self.LoadItems();

    return self;
}

function MapRuleItemModel(data, parent, isSource) {
    var self = this;
    self.ID = ko.observable(data.ID || -1);
    self.ErrorMessages = ko.observableArray([]);

    self.IntersectID = ko.observable(data.IntersectID);
    self.FusionAttributeTextPath = ko.observable(data.FusionAttributeTextPath || null);
    self.FusionAttributeID = ko.observable(data.FusionAttributeID || null);

    self.RemoveItem = function () {
        if (isSource) {
            if (parent.Sources().length == 1)
                return;
            parent.Sources.remove(self);
        }
        else {
            if (parent.Targets().length == 1)
                return;
            parent.Targets.remove(self);
        }
    }

    self.FindTextPaths = function (query, response) {
        if (query !== "") {
            var dataAdapter = new $.jqx.dataAdapter
            (
                {
                    datatype: "json",
                    datafields:
                    [
                        { name: 'Fusion' },
                        { name: 'FusionAttributeType' },
                        { name: 'TextPath' },
                        { name: 'ID' }
                    ],
                    url: "/form/MapRule_FindFusion",
                    data:
                    {
                        intersectID: self.IntersectID()
                    }
                },
                {
                    autoBind: true,
                    formatData: function (data) {
                        data.phrase = query;
                        return data;
                    },
                    loadComplete: function (data) {
                        if (data.length > 0) {
                            response($.map(data, function (item) {
                                return {
                                    label: item.Fusion + '.' + item.FusionAttributeType + '.' + item.TextPath,
                                    value: item.ID
                                }
                            }));
                        }
                    }
                }
            );
        }
    }

    return self;
}

function MapSelectionModel(data) {
    var self = this;

    self.SourceName = ko.observable(data.SourceName);
    self.SourceIntersectID = ko.observable(data.SourceIntersectID);
    self.SourceDiagramKey = ko.observable(data.SourceDiagramKey);

    self.TargetName = ko.observable(data.TargetName);
    self.TargetIntersectID = ko.observable(data.TargetIntersectID);
    self.TargetDiagramKey = ko.observable(data.TargetDiagramKey);

    self.DisplayText = ko.computed(function () {
        return self.SourceName() + ' -&gt; ' + self.TargetName();
    }, self);

    self.ValueText = ko.computed(function () {
        return self.SourceIntersectID() + '|' + self.SourceDiagramKey() + '|' + self.TargetIntersectID() + '|' + self.TargetDiagramKey();
    }, self);

    return self;
}

//#endregion

//#region Business Mapping (Map)

function LineagePanelViewModel(data, permissions) {
    var self = this;

    self.Object = ko.observable(data.object || '');
    self.ObjectID = ko.observable(data.objectID || 0);

    self.Items = ko.observableArray([]);

    self.IsSaving = ko.observable(false);
    self.IsLoading = ko.observable(false);

    self.AddItem = function () {
        self.Items.push(new LineagePanelViewModelLine({}, permissions, self));
    }

    self.AddItem();
}

function LineagePanelViewModelLine(data, permissions, parent) {
    var self = this;

    self.SourceTypes = ko.observableArray([]);
    self.TargetTypes = ko.observableArray([]);
    self.SourceTypeIndex = ko.observable(-1);
    self.TargetTypeIndex = ko.observable(-1);
    self.SourceIntersectTypeID = ko.observable(-1);
    self.TargetIntersectTypeID = ko.observable(-1);

    self.SourceObjects = ko.observableArray([]);
    self.TargetObjects = ko.observableArray([]);
    self.SourceObjectIndex = ko.observable(-1);
    self.TargetObjectIndex = ko.observable(-1);

    self.IsSaving = ko.observable(false);
    self.IsLoading = ko.observable(false);

    self.Items = ko.observableArray([]);

    //self.Items.push(new LineagePanelViewModelLineItem({}, permissions, parent));

    self.Advanced = ko.observable(false);

    self.SourceTypeIndex.subscribe(function () {
        self.SourceObjectIndex(-1);
        self.SourceObjects([]);

        var source;
        if (self.SourceTypeIndex() >= 0 && self.SourceTypes().length > 0)
            source = self.SourceTypes()[self.SourceTypeIndex()];

        if (source) {
            var type = source.value.split('|')[0];
            var id = source.value.split('|')[1];
            self.SourceIntersectTypeID(source.intersectTypeID);
            self.LoadTargetTypes(type, id);
        }
    });

    self.TargetTypeIndex.subscribe(function () {
        self.TargetIntersectTypeID(-1);
        self.TargetObjectIndex(-1);
        self.TargetObjects([]);
        var intersect;
        if (self.TargetTypeIndex() >= 0 && self.TargetTypes().length > 0)
            intersect = self.TargetTypes()[self.TargetTypeIndex()].intersectTypeID;
        if (intersect) {
            self.TargetIntersectTypeID(intersect);
        } else {
            self.TargetIntersectTypeID(-1);
        }
    });

    self.TargetIntersectTypeID.subscribe(function () {
        if (self.TargetIntersectTypeID() >= 0) {
            self.LoadTargetObjects(self.TargetIntersectTypeID());
            //console.log('target intersect ID: ' + self.TargetIntersectTypeID());
            //get targets
        }
    });

    self.SourceIntersectTypeID.subscribe(function () {
        if (self.SourceIntersectTypeID() >= 0) {
            self.LoadSourceObjects(self.SourceIntersectTypeID());
            //console.log('source intersect ID: ' + self.SourceIntersectTypeID());
            //get sources
        }
    });

    self.TargetObjectIndex.subscribe(function () {
        self.LoadSharedObjects();
    });
    self.SourceObjectIndex.subscribe(function () {
        self.LoadSharedObjects();
    });

    self.LoadSharedObjects = function () {
        var data = {};
        self.Items([]);

        if (self.TargetIntersectTypeID() < 0 || self.SourceObjectIndex() < 0 || self.TargetObjectIndex() < 0 || self.SourceTypeIndex() < 0 || self.TargetTypeIndex() < 0)
            return;
        if (self.SourceObjects().length < 1 || self.TargetObjects().length < 1)
            return;
        if (self.SourceTypes().length < 1 || self.TargetTypes().length < 1)
            return;


        var source = self.SourceObjects()[self.SourceObjectIndex()];
        var target = self.TargetObjects()[self.TargetObjectIndex()];
        var sourceType = self.SourceTypes()[self.SourceTypeIndex()];
        var targetType = self.SourceTypes()[self.TargetTypeIndex()];

        if (!source || !target || !sourceType || !targetType)
            return;

        data.source = source.value.split('|')[0];
        data.sourceID = source.value.split('|')[1];
        data.target = target.value.split('|')[0];
        data.targetID = target.value.split('|')[1];
        data.sourceType = sourceType.value.split('|')[0];
        data.sourceTypeID = sourceType.value.split('|')[1];
        data.targetType = targetType.value.split('|')[0];
        data.targetTypeID = targetType.value.split('|')[1];

        $.ajax({
            url: '/form/Lineage_IntersectSharedObjects',
            data: data,
            method: 'GET'
        }).done(function (data) {
            if (data && data.length > 0) {
                for (var i = 0; i < data.length; i++) {
                    self.Items.push(new LineagePanelViewModelLineItem(data[i], permissions, self));
                }
            }
            console.log(data);
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.AddItem = function () {
        self.Items.push(new LineagePanelViewModelLineItem({}, permissions, self));
    }

    self.LoadSourceTypes = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/form/Lineage_IntersectTypeSources',
            method: 'GET'
        }).done(function (data) {
            self.SourceTypes(data);
            self.SourceTypeIndex(-1);
            //console.log(data); 
        }).always(function () {
            self.IsLoading(false);
            self.AddItem();
        });
    }

    self.LoadTargetTypes = function (type, id) {
        self.IsLoading(true);
        self.TargetTypeIndex(-1);
        $.ajax({
            url: '/form/Lineage_IntersectTypeSources',
            method: 'GET'
        }).done(function (data) {
            self.TargetTypes(data);

            if (self.TargetTypes().length == 1)
                self.TargetTypeIndex(0);
            else
                self.TargetTypeIndex(-1);
            //console.log(data);
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadSourceObjects = function (intersectID) {
        self.IsLoading(true);
        $.ajax({
            url: '/form/Lineage_MapSubjects',
            data: { id: self.SourceIntersectTypeID() },
            method: 'GET'
        }).done(function (data) {
            self.SourceObjects(data);
            self.SourceObjectIndex(-1);
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadTargetObjects = function (intersectID) {
        self.IsLoading(true);
        $.ajax({
            url: '/form/Lineage_MapSubjects',
            data: { id: self.TargetIntersectTypeID() },
            method: 'GET'
        }).done(function (data) {
            self.TargetObjects(data);
            self.TargetObjectIndex(-1);
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadSourceTypes();

    //self.AddItem
}

function LineagePanelViewModelLineItem(data, permissions, parent) {
    var self = this;

    self.Sources = ko.observableArray([]);
    self.Targets = ko.observableArray([]);


    if (data) {

        var source = {
            Object: data.Source,
            ObjectID: data.SourceID,
            ObjectName: data.SourceName,
            IntersectID: data.SourceIntersectID
        };

        var target = data;
        target.IntersectID = data.ObjectIntersectID;

        self.Sources.push(new LineagePanelViewModelItem(source, permissions, self));
        self.Targets.push(new LineagePanelViewModelItem(target, permissions, self));

    }
}

function LineagePanelViewModelItem(data, permissions, parent) {
    var self = this;
    self.IntersectID = ko.observable(data.IntersectID || 0);
    self.Object = ko.observable(data.Object || '');
    self.ObjectID = ko.observable(data.ObjectID || 0);
    self.ObjectName = ko.observable(data.ObjectName || 0);
}


//#region Old

function LineagePanelViewModel_Old(data, permissions) {
    var self = this;
    self.jqxLoaded = false;

    //#region Observables

    self.Object = data.object;
    self.ObjectID = data.objectID;

    self.InProgress = ko.observable(false);
    self.IsSaving = ko.observable(false);

    self.Items = ko.observableArray();

    //self.IntersectType = ko.observable();
    self.IntersectTypeOptions = ko.observableArray();

    //if (permissions != null) {
    //    if (permissions.HasPermission("Relationship", "Create"))
    //        self.CanAdd(true);
    //    if (permissions.HasPermission("Relationship", "Update"))
    //        self.CanUpdate(true);
    //}

    //#endregion

    //#region Functions

    self.AddItem = function () {
        //if (self.IntersectType() > 0) {
            var data = {
                //IntersectType: self.IntersectType()
            }
            self.Items.push(new LineagePanelViewItemModel(data, permissions, self));
        //}
    }

    self.LoadIntersectTypes = function () {
        self.InProgress(true);
        $.ajax({
            url: '/form/Lineage_IntersectTypes',
            method: 'GET'
        }).done(function (data) {
            self.IntersectTypeOptions(data);
        }).always(function () {
            self.InProgress(false);
            self.AddItem(); //this adds the first row.
        });
    }

    self.RemoveItem = function () {
        self.Items.remove(this);
    }

    self.Save = function () {
        if (self.Items().length > 0) {
            var deferred = $.Deferred();

            self.IsSaving(true);

            var items = [];

            for (var i = 0; i < self.Items().length; i++) {
                var item = self.Items()[i];

                var subject = item.Subject().split('|')
                var object = item.Object().split('|')

                items.push({
                    Position: i,
                    IntersectTypeID: item.IntersectType(),
                    Subject: subject[0],
                    SubjectID: subject[1],
                    Object: object[0],
                    ObjectID: object[1]
                });
            }

            var successfulItems = [];

            if (items.length > 0) {
                $.ajax({
                    url: '/form/Lineage_AddItemsToDiagram',
                    method: 'POST',
                    data: { Items: items}
                }).done(function (returnedItems) {
                    if (returnedItems == null) {
                        //self.SaveMessage('<span style="color:maroon"><i class="fa fa-exclamation-circle"></i> An error occurred while saving the source rules.</span>');
                    } else {
                        //self.SaveMessage('<span style="color:green"><i class="fa fa-check-circle"></i> Changes saved successfully.</span>');
                        $.each(returnedItems, function (returnedItemIndex, returnedItem) {
                            if (returnedItem.ErrorMessage) {

                            }
                            else {
                                var model = self.Items()[returnedItem.Position];
                                model.Intersect(returnedItem.IntersectID);
                                //items[returnedItem.Position].IntersectID = returnedItem.IntersectID;
                                returnedItem.Name = model.SubjectName();
                                successfulItems.push(returnedItem);
                            }
                        });

                    }
                }).always(function () {
                    self.IsSaving(false);
                    deferred.resolve(successfulItems);
                });
            }

            return deferred.promise();
        }
    }

    //#endregion

    self.LoadIntersectTypes();

    return self;
}

function LineagePanelViewItemModel(data, permissions, parent) {
    var self = this;

    //#region Observables

    self.IntersectType = ko.observable(data.IntersectType);
    self.Intersect = ko.observable(); //Populated after save to diagram action from parent.
    
    self.SubjectIndex = ko.observable(-1);
    self.Subject = ko.observable();
    self.SubjectName = ko.observable();
    self.SubjectsLoading = ko.observable(true);
    self.SubjectOptions = ko.observableArray();

    self.ObjectIndex = ko.observable(-1);
    self.Object = ko.observable();
    self.ObjectName = ko.observable();
    self.ObjectsLoading = ko.observable(true);
    self.ObjectOptions = ko.observableArray();

    //#endregion

    //#region Functions

    self.LoadSubjects = function () {
        $.ajax({
            url: '/form/Lineage_MapSubjects',
            data: { id: self.IntersectType() },
            method: 'GET'
        }).done(function (data) {
            self.SubjectOptions(data);
            var ix = -1;
            $.each(data, function (itemIx, item) {
                if (item.value == parent.Object + "|" + parent.ObjectID) {
                    ix = itemIx;
                }
            });
            self.SubjectIndex(ix);
        }).always(function () {
            self.SubjectsLoading(false);
        });
    }

    self.LoadObjects = function () {
        $.ajax({
            url: '/form/Lineage_MapObjects',
            data: { id: self.IntersectType() },
            method: 'GET'
        }).done(function (data) {
            self.ObjectOptions(data);
            var ix = -1;
            $.each(data, function (itemIx, item) {
                if (item.value == parent.Object + "|" + parent.ObjectID) {
                    ix = itemIx;
                }
            });
            self.ObjectIndex(ix);
        }).always(function () {
            self.ObjectsLoading(false);
        });;
    };

    //#endregion

    self.IntersectType.subscribe(function () {
        self.LoadSubjects();
        self.LoadObjects();
    });

    return self;
}

//#endregion

//#endregion

//#region Source Hierarchy Models

function HierarchyRuleContextModel(data, parent) {
    var self = this;
    data = data || {};

    self.Object = ko.observable(data.Object || "");
    self.ObjectID = ko.observable(data.ObjectID || 0);

    if (data.ID != null && data.ID.indexOf('|') > -1) {
        var obj = data.ID.split('|')[0];
        var objid = data.ID.split('|')[1];
        self.Object(obj);
        self.ObjectID(objid);

    }

    self.ID = ko.computed(function () {
        return self.Object() + '|' + self.ObjectID().toString();
    });

    self.Checked = ko.observable(data.Checked || false);

    //self.Checked.subscribe(function () {
    //    console.log('checked sub');
    //    console.log(ko.toJS(parent));
    //    console.log(self.Checked());
    //    if (parent != null && parent.IsItemSelected() == true) {
    //        if (self.Checked() == true)
    //            parent.SelectedRule().SelectedItem().Contexts.push(this);
    //        else
    //            parent.SelectedRule().SelectedItem().Contexts.remove(this);
    //    }
    //});

    self.Category = ko.observable(data.Category || '');
    self.Type = ko.observable(data.Type || '');
    self.Name = ko.observable(data.Name || '');
    
    return self;
}

function HierarchyRuleItemModel(data) {
    var self = this;
    data = data || {};

    //#region Observables

    self.ID = ko.observable(data.ID || 0);
    self.IntersectMapID = ko.observable(data.IntersectMapID || 0);
    self.Name = ko.observable(data.Name || "");
    self.TypeName = ko.observable(data.TypeName || "");
    self.Object = ko.observable(data.Object || "");
    self.ObjectID = ko.observable(data.ObjectID || 0);
    self.Description = ko.observable(data.Description || "");
    self.SortOrder = ko.observable(data.SortOrder || 1);
    self.IconForeColor = ko.observable(data.IconForeColor || "#000");
    self.IconBackColor = ko.observable(data.IconBackColor || "#fff");

    self.Contexts = ko.observableArray([]);

    self.RuleName = ko.computed(function () {
        return self.SortOrder() + '. ' + self.Name();
    });
    self.ContextCount = ko.computed(function () {
        return (self.Contexts().length == 1) ? self.Contexts().length.toString() + ' context selected' : self.Contexts().length.toString() + ' contexts selected';
    });

    self.Value = ko.computed(function () {
        return self.Object() + '|' + self.ObjectID().toString();
    });


    //#endregion

    //load Contexts
    if (data.Contexts) {
        $.each(data.Contexts, function (cxIx, cxItem) {
            cxItem.Checked = true;
            self.Contexts.push(
                    new HierarchyRuleContextModel(cxItem, self)
                );
        });
    }

    return self;
}

function HierarchySourceRuleModel(data, permissions) {
    var self = this;

    if (data == null) {
        data = { ID: -1, Name: '', Target: '', TargetID: '', Object: '', ObjectID: '', Description: '' };
    }
    
    //#region Observables

    self.ID = ko.observable(data.ID || 0);
    self.Name = ko.observable(data.Name || '');
    self.Target = ko.observable(data.AppliesToObject || '');
    self.TargetID = ko.observable(data.AppliesToObjectID || 0);
    self.Object = ko.observable(data.Object || '');
    self.ObjectID = ko.observable(data.ObjectID || 0);
    self.Description = ko.observable(data.Description || '');
    self.SelectedItem = ko.observable(new HierarchyRuleItemModel(null));
    self.ErrorMessages = ko.observableArray([]);
    self.SaveMessage = ko.observable('');

    self.SourceRuleID = ko.observable(data.SourceRuleID || -1);
    self.IsTemplate = ko.observable(data.IsTemplate || false);
    self.IsSaving = ko.observable(false);

    self.Items = ko.observableArray();

    self.Value = ko.computed(function () {
        return self.Object() + '|' + self.ObjectID().toString();
    });

    //#endregion
    
    //load Items
    if (data.Items) {
        for (var i = 0; i < data.Items.length; i++) {
            self.Items.push(new HierarchyRuleItemModel(data.Items[i]));
        }
    }

    //#region Functions

    self.SaveRule = function () {
        if (!permissions.HasPermission("Relationship", "Create") && self.ID() == 0)
            return;
        if (!permissions.HasPermission("Relationship", "Update") && self.ID() != 0)
            return;
        self.IsSaving(true);
        self.ErrorMessages([]);
        if (self.Items().length < 1)
            self.ErrorMessages.push('Source rule must have at least 1 source item.');
        if (self.Name().length < 1)
            self.ErrorMessages.push('Source rule requires a name.');

        //for (var i = 0; i < self.Items().length; i++) {
        //    if (self.Items()[i].Contexts().length < 1 && self.Items()[i].Description().length < 1) {
        //        self.ErrorMessages.push('The source "' + self.Items()[i].Name() + '" is missing a context and/or description.');
        //    }
        //}

        if (self.ErrorMessages().length > 0) {
            self.IsSaving(false);
            return;
        }

        var SourceRule = {
            ID: self.ID(),
            Name: self.Name(),
            Object: self.Object(),
            ObjectID: self.ObjectID(),
            AppliesToObject: self.Target(),
            AppliesToObjectID: self.TargetID(),
            AppliesToObjectList: "",
            Items: ko.toJS(self.Items())
        }

        var action = (self.ID() > 0) ? 'edit' : 'add';

        $.ajax({
            url: '/form/SourceRules/save',
            data: SourceRule,
            method: 'POST'
        }).always(function (data) {
            self.IsSaving(false);
            if (!data.error) {
                self.ID(data.message);
                self.SaveMessage('<span style="color:green"><i class="fa fa-check-circle"></i> Changes saved successfully.</span>')
                amplify.publish("SaveAction", { context: 'sourcerule', action: action, object: self.Object(), objectid: self.ObjectID() });
            } else {
                self.SaveMessage('<span style="color:maroon"><i class="fa fa-exclaimation-circle"></i> An error occurred while saving the hierarchy rules.</span>');
                console.log(data.message);
            }        
        });
    }

    //#endregion

    return self;
}

function HierarchyPanelViewModel(data, permissions) {
    var self = this;
    self.jqxLoaded = false;
    //#region Observables
    //console.log(permissions);
    self.ID = ko.observable(data.ID || 0);
    self.Name = ko.observable('');
    self.Target = ko.observable(data.target || '');
    self.TargetID = ko.observable(data.targetID || 0);
    self.Object = ko.observable(data.object || '');
    self.ObjectID = ko.observable(data.objectID || 0);
    self.NewRule = ko.observable(new HierarchySourceRuleModel(null, permissions));
    self.NewRule().Name('New Rule');
    self.NewRule().Object(self.Object());
    self.NewRule().ObjectID(self.ObjectID());
    self.NewRule().Target(self.Target());
    self.NewRule().TargetID(self.TargetID());
    self.InProgress = ko.observable(false);
    self.IsGridLoading = ko.observable(false);
    self.SelectedRule = ko.observable(new HierarchySourceRuleModel(null, permissions));
    self.Mode = ko.observable('add'); 

    self.Contexts = ko.observableArray([]);
    self.Items = ko.observableArray();
    self.Sources = ko.observableArray();
    self.SourceRules = ko.observableArray();

    self.DeleteMessage = ko.observable('');
    self.IsDeleting = ko.observable(false);

    self.SelectedItemIndex = ko.observable(-1);
    self.SelectedSourceIndex = ko.observable(-1);
    self.SelectedRuleIndex = ko.observable(-1);
   // self.RadioAddChecked = ko.observable(true);
    //self.RadioEditChecked = ko.observable(false);
    self.IsLoadingContexts = ko.observable(false);
    self.HasSourcesOrRules = ko.observable(true);

    self.CanAdd = ko.observable(false);
    self.CanUpdate = ko.observable(false);

    if (permissions != null) {
        if (permissions.HasPermission("Relationship", "Create"))
            self.CanAdd(true);
        if (permissions.HasPermission("Relationship", "Update"))
            self.CanUpdate(true);

    }


    self.DeleteSourceRule = function () {
        self.Mode('delete');
        //console.log('mode delete');
    }

    self.DeleteConfirm = function () {
        self.IsDeleting(true);
        $.ajax({
            url: '/form/SourceRules/delete?id=' + self.SelectedRule().ID(),
            method: 'delete'
        }).done(function (data) {
            if (!data.error) {

                for (var i = 0; i < self.SelectedRule().Items().length; i++) {
                    self.Sources.push(self.SelectedRule().Items()[i]);
                }

                self.SelectedRule().Items.removeAll(self.SelectedRule().Items());

                //self.SelectedItemIndex(-1);
                self.SourceRules.remove(self.SelectedRule());
                self.AfterLoad();
                amplify.publish("SaveAction", { context: 'sourcerule', action: 'delete', object: self.Object(), objectid: self.ObjectID(), count: self.SourceRules().length });
                self.Mode('add');
            } else {
                self.DeleteMessage('An error occurred while attempting to delete the source rule.');
                console.log(data.message);
            }
            
        }).always(function() {
            self.IsDeleting(false);
        });

        //console.log('delete confirm');
    }

    self.DeleteCancel = function () {
        self.Mode('add');
    }

    self.AddSourceRule = function () {
        var data = {
            ID: 0,
            Name: 'New Rule',
            Object: self.Object(),
            ObjectID: self.ObjectID(),
            AppliesToObject: self.Target(),
            AppliesToObjectID: self.TargetID()
        }

        var newRule = new HierarchySourceRuleModel(data, permissions);

        self.SelectedRule(newRule);
        self.SourceRules.push(newRule);
        self.SelectedRuleIndex(self.SourceRules().length - 1);
    }


    self.SelectedItemIndex.subscribe(function () {
        if (self.SelectedItemIndex() == -1) {
            self.IsLoadingContexts(true);
            return;
        }
        self.IsLoadingContexts(false);
        self.SelectedRule().SelectedItem(self.SelectedRule().Items()[self.SelectedItemIndex()]);
        if (self.IsItemSelected())
            self.CheckUsedContextItems();
    });

    self.SelectedRuleIndex.subscribe(function () {
        var rule = self.SourceRules()[self.SelectedRuleIndex()];
        self.SelectedRule(rule);
        //self.SelectedItemIndex(-1);
        self.CheckUsedContextItems();
    });

    self.IsItemSelected = ko.computed(function () {
        if (self.SelectedRule() == null)
            return false;
        if (self.SelectedRule().SelectedItem() == null)
            return false;
        return true;
    });

    //#endregion
 
    //#region Functions

    self.LoadRules = function () {
        //console.log('load rules');
        self.InProgress(true);
        $.ajax({
            url: '/form/SourceRules/' + self.Target() + '/' + self.TargetID() + '/' + self.Object() + '/' + self.ObjectID(),
            method: 'GET'
        }).done(function (data) {
            if (data == [] || data == null || data.length < 1)
                return;
            self.SourceRules([]);
            for (var i = 0; i < data.length; i++) {
                data[i].SourceRuleID = self.ID();
                var rule = new HierarchySourceRuleModel(data[i], permissions);
                self.SourceRules.push(rule);
                for (var j = 0; j < data[i].Items.length; j++) {
                    for (var k = 0; k < self.Sources().length; k++) {
                        if (data[i].Items[j].IntersectMapID == self.Sources()[k].IntersectMapID()) {
                            self.Sources.remove(self.Sources()[k]);
                            break;
                        }
                    }
                }
            }
        }).always(function () {
            //self.SelectRule(self.NewRule());
            self.InProgress(false);
            if (!self.jqxLoaded)
                self.ApplyJqxBindings();
            self.AfterLoad();
        });
    }

    self.LoadSources = function () {
      //  console.log('load sources');
        self.InProgress(true);

        $.ajax({
            url: '/form/SourceRules/sources/' + self.Object() + '/' + self.ObjectID() + '/' + self.Target() + '/' + self.TargetID(),
            method: 'GET'
        }).done(function (data) {
            if (data.Contexts)
                $.each(data.Contexts, function (cxIx, cxItem) {
                    self.Contexts.push(
                            new HierarchyRuleContextModel(cxItem, self)
                        );
                });
            $.each(data, function (roIx, roItem) {
                self.Sources.push(
                        new HierarchyRuleItemModel(roItem)
                    );
            });

        }).always(function () {
            self.InProgress(false);
            self.LoadContexts();
            self.LoadRules();
        });
    }

    self.LoadSources();

    self.LoadContexts = function () {
       // console.log('load contexts');
        $.ajax({
            url: '/form/SourceRules/contexts',
            method: 'GET'
        }).done(function (data) {
            if (data == null)
                return;
            if (data.count == null || data.items == null)
                return;
            if (data.items.length < 1)
                return;
            self.Contexts([]);
            self.Contexts(data.items);
        });
    };

    self.AddRuleItem = function () {
        if (!self.CanUpdate())
            return;
        if (self.SelectedSourceIndex() == -1)
            return;
        self.SelectedRule().Items.push(self.Sources()[self.SelectedSourceIndex()]);
        self.Sources.remove(self.Sources()[self.SelectedSourceIndex()]);
        self.SelectedSourceIndex(-1);
        self.ReorderItems(self.SelectedRule().Items());
    };

    self.RemoveRuleItem = function () {
        if (!self.CanUpdate())
            return;
        if (self.SelectedItemIndex() == -1)
            return;
        //console.log(ko.toJS(self.SelectedRule().Items()[self.SelectedItemIndex()]));
        self.Sources.push(self.SelectedRule().Items()[self.SelectedItemIndex()]);
        self.SelectedRule().Items.remove(self.SelectedRule().Items()[self.SelectedItemIndex()]);
        self.SelectedItemIndex(-1);
        self.ReorderItems(self.SelectedRule().Items());
    }

    self.MoveRuleItemUp = function () {
        if (!self.CanUpdate())
            return;
        if (self.SelectedItemIndex() < 1)
            return;
        var itemAbove = self.SelectedRule().Items()[self.SelectedItemIndex() - 1];
        var itemSelected = self.SelectedRule().Items()[self.SelectedItemIndex()];

        self.SelectedRule().Items()[self.SelectedItemIndex()] = itemAbove;
        self.SelectedRule().Items()[self.SelectedItemIndex() - 1] = itemSelected;

        self.ReorderItems(self.SelectedRule().Items());
        self.SelectedItemIndex(self.SelectedItemIndex() - 1);

    }

    self.MoveRuleItemDown = function () {
        if (!self.CanUpdate())
            return;
        if (self.SelectedItemIndex() == -1 || self.SelectedItemIndex() == self.SelectedRule().Items().length - 1)
            return;
        var itemBelow = self.SelectedRule().Items()[self.SelectedItemIndex() + 1];
        var itemSelected = self.SelectedRule().Items()[self.SelectedItemIndex()];

        self.SelectedRule().Items()[self.SelectedItemIndex()] = itemBelow;
        self.SelectedRule().Items()[self.SelectedItemIndex() + 1] = itemSelected;

        self.ReorderItems(self.SelectedRule().Items());
        self.SelectedItemIndex(self.SelectedItemIndex() + 1);
    }

    self.OnCellValueChange = function () {
        if (self.IsLoadingContexts() == true)
            return;
        if (self.IsItemSelected()) {
            var checkedCtx = [];
            self.SelectedRule().SelectedItem().Contexts([]);
            for (var i = 0; i < self.Contexts().length; i++) {
                if (self.Contexts()[i].Checked == true) {
                    var obj = new HierarchyRuleContextModel(self.Contexts()[i]);
                    self.SelectedRule().SelectedItem().Contexts.push(obj);
                }
            }
        }
    }

    self.ApplyJqxBindings = function () {
       // console.log('jqx bindings start');
        $('#hierarchyRuleContextGrid').on('cellvaluechanged', function () {
            self.OnCellValueChange();
        }).on('bindingcomplete', function () {
            //jqx grid is not editable after bind without this
            $(this).jqxGrid('refresh');
        });
        self.jqxLoaded = true;
       // console.log('jqx bindings end');
    }

    self.FindItemByOrder = function (order, array) {
        for (var i = 0; i < array.length; i++) {
            if (array[i].SortOrder() == order)
                return array[i];
        }
    }

    self.ReorderItems = function (array) {
        for (var i = 0; i < array.length; i++) {
            array[i].SortOrder(i + 1);
        }
    }

    self.CheckUsedContextItems = function () {

        var isItemSelected = self.IsItemSelected();
        self.IsGridLoading(true);
        for (var i = 0; i < self.Contexts().length; i++) {
            var c = self.Contexts()[i];
            c.Checked = false;
            if (!isItemSelected)
                continue;
            for (var j = 0; j < self.SelectedRule().SelectedItem().Contexts().length; j++) {
                var rc = self.SelectedRule().SelectedItem().Contexts()[j];
                if (rc.ID() == c.ID) {
                    c.Checked = true;
                }
            }
        }
        self.IsLoadingContexts(false);
        self.IsGridLoading(false);
    }

    self.SelectRule = function (selectedRule) {
        self.SelectedRule(selectedRule);
    }

    self.SelectItem = function (selectedItem) {
        self.SelectedRule().SelectedItem(selectedItem);
        self.CheckUsedContextItems();
    }

    self.AfterLoad = function () {
        if (self.SourceRules().length >= 1) {
            self.SelectRule(self.SourceRules()[0]);
            
            self.SelectedRuleIndex(0);
            self.HasSourcesOrRules(true);
        } else {
            self.HasSourcesOrRules(true);
        }
        if (self.Sources().length < 1 && self.SourceRules().length < 1) {
            self.HasSourcesOrRules(false);
        }
    }

    //#endregion

    return self;
}

//#endregion

//#region    BASE MODELS

function CommentTagItem(data, parent) {
    var self = this;
    data = data || {};
    self.Object = ko.observable(data.Object);
    self.ObjectID = ko.observable(data.ObjectID);
    self.TextPath = ko.observable(data.TextPath);
    self.Url = ko.observable(data.Url);
    self.ObjectTypeName = ko.observable(data.ObjectTypeName);
    self.ShowRemove = ko.observable(false);
    self.IconBackColor = ko.observable(data.IconBackColor);
    self.IconForeColor = ko.observable(data.IconForeColor);
    self.IsSelected = ko.observable(false);


    self.removeTag = function () {
        parent.tags.remove(self);
    }

    self.addTag = function () {

        for (var i = 0; i < parent.tags().length; i++) {
            if (parent.tags()[i].ObjectID() == self.ObjectID()) {
                parent.newTag('');
                parent.tagSuggestions([]);
                return;
            }
        }
        self.ShowRemove(true);
        parent.tags(parent.tags().concat(self));
        parent.newTag('');
        parent.tagSuggestions([]);
    }
}

function CommentCountItem(data) {
    var self = this;

    self.CurrentlySelectedValue = ko.observable();
    self.CommentTypeName = ko.observable(data.CommentTypeName);
    self.Count = ko.observable(data.Count);
    self.CommentTypeValue = ko.observable(data.CommentType);

    self.MyCss = ko.observable("");

    self.IsSelected = ko.pureComputed(function () {
        var commentType = self.CommentTypeValue();

        if (self.CurrentlySelectedValue == null)
            if (commentType == 0)
                return "socialCountBoxSelected";
            else
                return "";

        var currentSelection = self.CurrentlySelectedValue();
        return (commentType == currentSelection) ? "socialCountBoxSelected" : "";

    });
    
}

function CommentVoteItem(data) {
    var self = this;
    self.CommentID = ko.observable(data.CommentID || "");
    self.ResourceID = ko.observable(data.ResourceID || "");
    self.Vote = ko.observable(data.Vote || 0);
}

function CommentItem(data, parent) {//, hub) {
    var self = this;
    data = data || {};
    self.ID = ko.observable(data.ID);
    self.Body = ko.observable(data.Body);
    self.CreatingResourceID = ko.observable(data.CreatingResourceID || 0);
    self.CommentTypeID = ko.observable(data.CommentTypeID || 0);
    self.DateCreated = data.DateCreatedUTCString;
    self.ObjectID = ko.observable(data.ObjectID || 0);
    self.ObjectType = ko.observable(data.ObjectType || "");
    self.ParentID = ko.observable(data.ParentID || null);
    self.ResourceName = ko.observable(data.ResourceName || "");
    self.ResourceEmail = ko.observable(data.ResourceEmail || "");
    self.ObjectName = ko.observable(data.ObjectName || "");
    self.ObjectUrl = ko.observable(data.ObjectUrl || "");
    self.CommentType = ko.observable(data.CommentType || "");
    self.VisibilityID = ko.observable(data.VisibilityID || "");
    self.CreatorIsOwner = ko.observable(data.CreatorIsOwner || "");
    self.DateEdited = ko.observable(data.DateEditedUTCString || null);
    self.IsEditable = ko.observable(data.IsEditable || false);
    self.IsDeletable = ko.observable(data.IsDeletable || false);
    self.IsDeleted = ko.observable(data.IsDeleted || false);

    self.tagSuggestions = ko.observableArray();
    self.tagSuggestionsPresent = ko.computed(function () {
        return (self.tagSuggestions().length > 0);
    }, self);

    self.ProcessingCount = ko.observable(0);
    self.IsProcessing = ko.computed(function () {
        return (self.ProcessingCount() != 0);
    });

    self.tagIndex = -1;

    self.setIndex = function (data, event) {
        //38, 40, 37, 39, 13
        //console.log(event);


        if (event.keyCode == 13) { //enter key
            if (self.tagSuggestions().length == 1) {
                var t = self.tagSuggestions()[0];
                t.addTag();
                //self.tags.push(t);
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {
                var t = self.tagSuggestions()[self.tagIndex];
                t.addTag();
                //self.tags.push(t);
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
        }
        else if (event.keyCode == 40 || event.keyCode == 38) { //up & down arrows
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {

                self.tagSuggestions()[self.tagIndex].IsSelected(false);

                if (event.keyCode == 38 && self.tagIndex > 0) {
                    self.tagIndex--;
                }
                else if (event.keyCode == 40 && self.tagIndex < self.tagSuggestions().length) {
                    self.tagIndex++;
                }

            } else {
                self.tagIndex = 0;
            }

            if (self.tagIndex != -1) {
                self.tagSuggestions()[self.tagIndex].IsSelected(true);
            }
            return false;
        }
        else {
            self.tagIndex = -1;
            return true;
        }
    };


    self.ShowObjectType = ko.computed(function () {
        //var result = (self.ObjectType() == "Resource" && self.ObjectID() == self.CreatingResourceID());
        //alert(self.ObjectType() + ", " + self.CreatingResourceID() + ", " + result);

        return !(self.ObjectType() == "Resource" && self.ObjectID() == self.CreatingResourceID());
    });




    self.FormatDate = function (utcDateString) {
        //convert date to local timezone and format
        var date = new Date(utcDateString);
        var hours = date.getHours();
        var ampm = hours >= 12 ? 'PM' : 'AM';
        var minutes = date.getMinutes();
        var seconds = date.getSeconds();
        var day = date.getDate();

        hours = hours % 12;
        hours = hours ? hours : 12;
        minutes = minutes < 10 ? '0' + minutes : minutes;
        seconds = seconds < 10 ? '0' + seconds : seconds;
        day = day < 10 ? '0' + day : day;
        var timeString = hours + ':' + minutes + ':' + seconds + ' ' + ampm;


        var yearString = date.getFullYear().toString().substr(2, 2);
        return (date.getMonth() + 1) + "/" + day + "/" + yearString + " " + timeString;
    };

    self.DateCreatedLocal = ko.computed(function () {  
        return self.FormatDate(self.DateCreated);
    });
    self.DateEditedLocal = ko.computed(function () {
        return self.FormatDate((self.DateEdited() || "").toString());
    });
   

    var _currentVotes = $.map(data.Votes, function (item) { return new CommentVoteItem(item); });
    self.CurrentVotes = ko.observableArray(_currentVotes);

    self.UpVoteCount = ko.computed(function () {
        var count = 0;        
        for(var i = 0; i < self.CurrentVotes().length; i++) {
            if (self.CurrentVotes()[i].Vote() > 0)
                count++;
        }
        return count;
    }, self);

    self.DownVoteCount = ko.computed(function () {
        var count = 0;        
        for (var i = 0; i < self.CurrentVotes().length; i++) {
            if (self.CurrentVotes()[i].Vote() < 0)
                count++;
        }
        return count;
    }, self);


    self.CastVote = function (vote) {
        $.ajax({
            data: {
                "CommentID": self.ID,
                "Vote": vote
            },
            dataType: 'json',
            method: 'POST',
            url: '/services/community/vote'
        }).done(function (commentData, status, xhr) {
            var commentVoteResults = $.map(commentData, function (item) { return new CommentVoteItem(item); });
            self.CurrentVotes([]);
            self.CurrentVotes(self.CurrentVotes().concat(commentVoteResults));
        }).fail(function (xhr, status, error) {
            self.error(status);
        });
    }

    self.CastUpVote = function () {
        self.CastVote(1);
    }
    self.CastDownVote = function () {
        self.CastVote(-1);
    }


    var _currenTags = $.map(data.Tags, function (item) { return new CommentTagItem(item, self); });
    self.CurrentTags = ko.observableArray(_currenTags);
    self.CurrentTagCount = ko.computed(function () {
        return self.CurrentTags().length;
    }, self);
    self.CurrentTagCountText = ko.computed(function () {
        return self.CurrentTagCount() == 1 ? "1 tag" : self.CurrentTagCount() + " tags" + ' ';
    }, self);

    self.isVisible = ko.observable(true);
    self.error = ko.observable();
    self.Comments = ko.observableArray();
    self.NewComments = ko.observableArray();
    self.newCommentMessage = ko.observable();
    
    self.DisableReply = ko.computed(function () {
        return !((self.newCommentMessage() || '').length > 0);
    });
    self.ShowAddCommentControls = ko.observable(CompanySettings.DisableCommunityPosting == 'false' || CompanySettings.DisableIssuePosting == 'false' || CompanySettings.DisableQuestionPosting == 'false');

    self.ShowReplyControl = ko.computed(function () {
        switch (self.CommentTypeID())
        {
            case 2:
                return (CompanySettings.DisableCommunityPosting == 'false');
                break;
            case 5:
                return (CompanySettings.DisableIssuePosting == 'false');
                break;
            case 9:
                return (CompanySettings.DisableQuestionPosting == 'false');
                break;
            default:
                return true;
                break;
        }
    });

    self.ReplyHidden = ko.observable(true);
    self.EditHidden = ko.observable(true);
    self.DeleteHidden = ko.observable(true);

    self.tagsAreDisplayed = ko.observable(false);
    self.tagsAreHidden = ko.computed(function () {
        return !self.tagsAreDisplayed();
    }, self);

    self.newTag = ko.observable();
    //self.tags = ko.observableArray([]);

    var _tags = $.map(data.Tags, function (item) { return new CommentTagItem(item, self); });
    self.tags = ko.observableArray(_tags);


    self.newTag.subscribe(function (value) {
        if (value) {
            $.getJSON('/api/tagsuggestions', { phrase: value }, function (suggestions) {
                // Object, ObjectID, TextPath, Url, ObjectTypeName
                var mappedSuggestions = $.map(suggestions, function (item) { return new CommentTagItem(item, self); }); //, self.hub
                self.tagSuggestions(mappedSuggestions);
            });
        }
        else {
            self.tagSuggestions([]);
        }
    });


    self.NewTagCount = ko.computed(function () {
        return self.tags().length;
    });

    self.NewTagCountText = ko.computed(function () {
        return self.NewTagCount() == 1 ? "add 1 tag" : "add " + self.NewTagCount() + " tags";
    });

    self.HasNewTags = ko.computed(function () {
        return (self.NewTagCount() > 0);
    });


    //self.hub = hub;
    
    //var tagSource = {
    //    localdata: self.tagSuggestions,
    //    datatype: 'observablearray'
    //};

    //self.jqxTagAdapter = new $.jqx.dataAdapter(tagSource);
    //self.jqxTagRenderer = function (index, label, value) {
    //    return '<strong>' + label + '</strong>' + value;
    //};
    //$('#dropdownlist').jqxDropDownList({
    //    selectedIndex: 0, source: dataAdapter, displayMember: "firstname", valueMember: "notes", itemHeight: 70, height: 25, width: 270,
    //    renderer: function (index, label, value) {
    //        var datarecord = data[index];
    //        var imgurl = '../../images/' + label.toLowerCase() + '.png';
    //        var img = '<img height="50" width="45" src="' + imgurl + '"/>';
    //        var table = '<table style="min-width: 150px;"><tr><td style="width: 55px;" rowspan="2">' + img + '</td><td>' + datarecord.firstname + " " + datarecord.lastname + '</td></tr><tr><td>' + datarecord.title + '</td></tr></table>';
    //        return table;
    //    }
    //});

    self.displayTags = function () {
        self.tagsAreDisplayed(true);
    };
    self.hideTags = function () {
        self.tagsAreDisplayed(false);
    };

    self.showReply = function () {
        self.ReplyHidden(false);
    };
    self.hideReply = function () {
        self.ReplyHidden(true);
    };

    self.showEdit = function () {
        self.EditHidden(false);
        self.hideTags();

        for (var i = 0; i < self.tags().length; i++) {
            self.tags()[i].ShowRemove(true);
        }
        
    };
    self.hideEdit = function () {
        self.EditHidden(true);
    };

    self.showDelete = function () {
        self.DeleteHidden(false);
    };
    self.hideDelete = function () {
        self.DeleteHidden(true);
    };

    self.getCommentType = function () {
        var commentType = "";

        switch (self.CommentTypeID()) {
            case 1:
                commentType = "System Notifications";
                break;
            case 2:
                commentType = "Discussions";
                break;
            case 3:
                commentType = "Governance";
                break;
            case 4:
                commentType = "Relationships";
                break;
            case 5:
                commentType = "Issues";
                break;
            case 6:
                commentType = "Tasks";
                break;
            case 7:
                commentType = "Red Flag Alerts";
                break;
            case 8:
                commentType = "Data Events";
                break;
            case 9:
                commentType = "Questions";
                break;
        }

        return commentType;
    };

    self.getCommentTypeCss = function () {
        var css = "fa ";

        switch (self.CommentTypeID()) {
            case 1:
                css += "fa-gear grey-text text-accent-2";
                break;
            case 2:
                css += "fa-comment blue-text text-accent-2";
                break;
            case 3:
                css += "fa-gavel green-text text-accent-2";
                break;
            case 4:
                css += "fa-link teal-text text-accent-2";
                break;
            case 5:
                css += "fa-exclamation-triangle orange-text text-accent-2";
                break;
            case 6:
                css += "fa-tasks deep-purple-text text-accent-2";
                break;
            case 7:
                css += "fa-flag red-text text-accent-2";
                break;
            case 8:
                css += "fa-info-circle cyan-text text-accent-2";
                break;
            case 9:
                css += "fa-question-circle purple-text text-accent-2";
                break;
        }

        return css;
    };

    self.updateComment = function () {
        self.error(null);
        self.ProcessingCount(self.ProcessingCount() + 1);
        
        if (self.ParentID() != null)
        {
            var commentModel = {
                Tags: self.tags(),
                Comment: {
                    ID: self.ParentID(),
                    Body: self.Body()
                }
            };
        }
        else
        {
            var commentModel = {
                ObjectType: self.ObjectType,
                ObjectID: self.ObjectID,
                Tags: self.tags(),
                Comment: {
                    ID: self.ID,
                    Body: self.Body,
                    CommentTypeID: self.CommentTypeID(),
                    VisibilityID: self.VisibilityID(),
                    IsDeleted: self.IsDeleted()
                }
            };
        }

            $.ajax({
                data: commentModel,
                dataType: 'json',
                method: 'POST',
                url: '/services/community/edit'
            }).done(function (result, status, xhr) {
                var _currentTags = $.map(result.Tags, function (item) { return new CommentTagItem(item, self); });
                self.CurrentTags([]);
                self.CurrentTags(_currentTags);
                self.tags([]);
                self.tags(_currentTags);
                self.ProcessingCount(self.ProcessingCount() - 1);
                self.DateEdited(result.DateEditedUTCString);
                self.IsEditable(result.IsEditable);
                self.IsDeletable(result.IsDeletable);
                self.Body(result.Body);
                self.IsDeleted(result.IsDeleted);
                self.hideEdit();
                self.DeleteHidden(true);

                if (self.IsDeleted() == true) {
                    //parent.comments().remove(self);
                    self.isVisible(false);
                    parent.getMoreComments();
                    
                }


                amplify.publish("SaveAction", { context: 'commentform', action: "add", id: result.ID, custom: {} })
                
            }).fail(function (xhr, status, error) {
                self.ProcessingCount(self.ProcessingCount() - 1);
                self.error(status);
            });
    };


    self.addComment = function () {
        self.ProcessingCount(self.ProcessingCount() + 1);
        if (self.HasNewTags()) {
            self.updateComment();
        }
        
        if (self.newCommentMessage() != '') {
            
            $.ajax({
                data: {
                    ObjectType: self.ObjectType,
                    ObjectID: self.ObjectID,
                    Comment: {
                        Body: self.newCommentMessage(),
                        CommentTypeID: 2,
                        ParentID: self.ID,
                        VisibilityID: 1
                    }
                },
                dataType: 'json',
                method: 'POST',
                url: '/services/community/comment'
            }).done(function (data, status, xhr) {
                self.Comments.push(new CommentItem(data,self));
                self.newCommentMessage('');
                self.hideReply();
                self.hideEdit();
                self.IsEditable(false);
                self.ProcessingCount(self.ProcessingCount() - 1);
            }).fail(function (xhr, status, error) {
                self.error(status);
                self.ProcessingCount(self.ProcessingCount() - 1);
            });
            //if ($.connection.hub && $.connection.hub.state === $.signalR.connectionState.disconnected) {
            //    $.connection.hub.start()
            //}
            //$.connection.socialHub.server.addComment(
            //    {
            //        "ObjectType": self.ObjectType,
            //        "ObjectID": self.ObjectID,
            //        "Comment": { "Body": self.newCommentMessage(), "CommentTypeID": 2, "ParentID": self.ID }
            //    })
            //    .done(function (comment) {
            //        self.Comments.push(new Comment(comment, self.hub));
            //        self.newCommentMessage('');
            //    })
            //    .fail(function (err) {
            //        self.error(err);
            //    });
        }
        else {
            self.ProcessingCount(self.ProcessingCount() - 1);
            self.error('Body may not be empty.');
        }
        
    };

    self.getResourceUrl = function () {
        return "/#/resources/" + self.CreatingResourceID();
    };

    self.getResourceImage = function () {
        return "/resources/image/" + self.CreatingResourceID() + "?size=25";//"https://secure.gravatar.com/avatar/" + hex_md5 (self.ResourceEmail) + "?s=40";
    };

    self.loadNewComments = function () {
        self.Comments(self.Comments().concat(self.NewComments()));
        self.NewComments([]);
    };
    self.toggleComment = function (item, event) {
        $(event.target).next().find('.publishComment').toggle();
    };


    if (data.Comments) {
        var mappedPosts = $.map(data.Comments, function (item) { return new CommentItem(item, self); });//, self.hub
        self.Comments(mappedPosts);
    }

}

var ChildArtifactsMicroTileItem = function (parentID, name, id, count) {
    var self = this;
    self.ParentID = ko.observable(parentID);
    self.Name = ko.observable(name);
    self.ID = ko.observable(id);
    self.Count = ko.observable(count);

    self.OverlayUri = ko.computed(function () {
        return '/overlays/' + self.ParentID() + '/' + self.ID() + '/ChildArtifacts';
    }, self);
}

var EventsMicroTileItem = function (name, count, trend) {
    var self = this;
    self.Name = ko.observable(name);
    self.Count = ko.observable(count);
    self.Trend = ko.observable(trend);
}

var RedFlagSummaryMicroTileItem = function (data) {
    var self = this;
    self.Type = ko.observable(data.Type);
    self.TypeID = ko.observable(data.TypeID);
    self.TypeName = ko.observable(data.TypeName);
    self.CriticalRelationshipCount = ko.observable(data.CriticalRelationshipCount);
    self.RedFlagCount = ko.observable(data.RedFlagCount);


    self.Open = function (item, event) {
        $(event.target).qtip({
            content: {
                title: 'Red-Flagged Items For ' + self.TypeName(),
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/tooltips/' + item.Type() + '/' + item.TypeID() + '/RedFlags'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '600',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }
}

function CompanySettingIpRestiction(data) {
    var self = this;
    data = data || {};

    self.Name = ko.observable(data.Name || "");
    self.Start = ko.observable(data.Start || "");
    self.End = ko.observable(data.End || "");

    return self;
}

function CompanySettingsViewModel(data) {
    var self = this;
    data = data || {};

    //Simple Properties
    self.DisableCommunityPosting = ko.observable(data.DisableCommunityPosting);
    self.DisableIssuePosting = ko.observable(data.DisableIssuePosting);
    //self.DisableQuestionPosting = ko.observable(data.DisableQuestionPosting);
    self.ArtifactType_TaxonomyTypeID = ko.observable(data.ArtifactType_TaxonomyTypeID);
    self.ArtifactType_TaxonomyTypeIDNodes = ko.observable(data.ArtifactType_TaxonomyTypeIDNodes);
    self.SetIconToDefault = ko.observable(data.SetLogoToDefault);
    self.SetLogoToDefault = ko.observable(data.SetLogoToDefault);

    self.CompanyLogo = ko.observable({
        file: ko.observable(), // will be filled with a File object
        // Read the files (all are optional, e.g: if you're certain that it is a text file, use only text:
        binaryString: ko.observable(), // FileReader.readAsBinaryString(Blob|File) - The result property will contain the file/blob's data as a binary string. Every byte is represented by an integer in the range [0..255].
        text: ko.observable(), // FileReader.readAsText(Blob|File, opt_encoding) - The result property will contain the file/blob's data as a text string. By default the string is decoded as 'UTF-8'. Use the optional encoding parameter can specify a different format.
        dataURL: ko.observable(), // FileReader.readAsDataURL(Blob|File) - The result property will contain the file/blob's data encoded as a data URL.
        arrayBuffer: ko.observable(), // FileReader.readAsArrayBuffer(Blob|File) - The result property will contain the file/blob's data as an ArrayBuffer object.

        // a special observable (optional)
        base64String: ko.observable(), // just the base64 string, without mime type or anything else
    });
    self.CurrentCompanyLogoPath = ko.observable(data.CurrentCompanyLogoPath || '');

    self.CompanyIcon = ko.observable({
        file: ko.observable(), // will be filled with a File object
        // Read the files (all are optional, e.g: if you're certain that it is a text file, use only text:
        binaryString: ko.observable(), // FileReader.readAsBinaryString(Blob|File) - The result property will contain the file/blob's data as a binary string. Every byte is represented by an integer in the range [0..255].
        text: ko.observable(), // FileReader.readAsText(Blob|File, opt_encoding) - The result property will contain the file/blob's data as a text string. By default the string is decoded as 'UTF-8'. Use the optional encoding parameter can specify a different format.
        dataURL: ko.observable(), // FileReader.readAsDataURL(Blob|File) - The result property will contain the file/blob's data encoded as a data URL.
        arrayBuffer: ko.observable(), // FileReader.readAsArrayBuffer(Blob|File) - The result property will contain the file/blob's data as an ArrayBuffer object.

        // a special observable (optional)
        base64String: ko.observable(), // just the base64 string, without mime type or anything else
    });
    self.CurrentCompanyIconPath = ko.observable(data.CurrentCompanyIconPath || '');

    self.InProgress = ko.observable(false);

    //List Properties
    self.IpRestrictions = ko.observableArray();

    self.SearchTypes = ko.observableArray([
        { title: "Attribute", value: "Attribute" },
        { title: "Fusion", value: "FusionAttributes" },
        { title: "Fusion Type", value: "FusionType" },
        { title: "Glossary", value: "Artifact" },
        { title: "Group", value: "Group" },
        { title: "Model", value: "Taxonomy" },
        { title: "Reference", value: "Domain" },
        { title: "User", value: "Users" },
    ]);

    //Computed Properties
    self.CurrentCompanyLogoPathPresent = ko.pureComputed(function () {
        return (self.CurrentCompanyLogoPath().length > 0 && !self.SetLogoToDefault());
    }, self);
    self.CurrentCompanyIconPathPresent = ko.pureComputed(function () {
        return (self.CurrentCompanyIconPath().length > 0 && !self.SetIconToDefault());
    }, self);

    self.CurrentCompanyGraphicsPresent = ko.pureComputed(function () {
        return (self.CurrentCompanyIconPath().length > 0 || self.CurrentCompanyLogoPath().length > 0);
    }, self);

    //Subscriptions
    self.DisableCommunityPosting.subscribe(function (value) {
    });

    self.DisableIssuePosting.subscribe(function (value) {
    });

    //self.DisableQuestionPosting.subscribe(function (value) {
    //});


    //#region Methods

    self.addIpRestriction = function () {
        self.IpRestrictions.push(new CompanySettingIpRestiction({}));
    };

    self.deleteIpRestriction = function () {
        self.IpRestrictions.remove(this);
    };

    self.SelectedSearchTypes = function () {
        var items = $("#searchDropDown").jqxDropDownList('getCheckedItems');
        var searchTypes = '';

        for (var i = 0; i < items.length; i++) {
            if (searchTypes.length > 0) searchTypes += ",";
            searchTypes += items[i].value;
        }        
        return searchTypes;
    };

    self.loadCurrentSettings = function () {
        $.getJSON('/form/CompanySettings', function (relData) {
            self.CurrentCompanyIconPath(relData.CurrentCompanyIconPath);
            self.CurrentCompanyLogoPath(relData.CurrentCompanyLogoPath);
            self.DisableCommunityPosting(relData.DisableCommunityPosting);
            self.DisableIssuePosting(relData.DisableIssuePosting);
            //self.DisableQuestionPosting(relData.DisableQuestionPosting);

            self.ArtifactType_TaxonomyTypeID(relData.ArtifactType_TaxonomyTypeID);
            self.ArtifactType_TaxonomyTypeIDNodes(relData.ArtifactType_TaxonomyTypeIDNodes);

            $.each(relData.IpRestrictions, function (roIx, roItem) {
                self.IpRestrictions.push(
                        new CompanySettingIpRestiction({
                            Name: roItem.Name,
                            Start: roItem.Start,
                            End: roItem.End
                        })
                    );

            });

            //searchDropDown
            var searchTypes=relData.DefaultSearchTypes.split(',');
            for (var i = 0; i < searchTypes.length; i++) {                
                $("#searchDropDown").jqxDropDownList('checkItem', searchTypes[i]);
            }
        });
    };

    self.save = function () {
        self.InProgress(true);

        var postModel = {
            DisableCommunityPosting: self.DisableCommunityPosting(),
            DisableIssuePosting: self.DisableIssuePosting(),
            //DisableQuestionPosting: self.DisableQuestionPosting(),
            SetLogoToDefault: self.SetLogoToDefault(),
            CompanyLogo: self.CompanyLogo().dataURL(),
            SetIconToDefault: self.SetIconToDefault(),
            CompanyIcon: self.CompanyIcon().dataURL(),
            ArtifactType_TaxonomyTypeID: self.ArtifactType_TaxonomyTypeID(),
            ArtifactType_TaxonomyTypeIDNodes: self.ArtifactType_TaxonomyTypeIDNodes(),
            DefaultSearchTypes: self.SelectedSearchTypes(),
            IpRestrictions: []
        }

        for (var r = 0; r < self.IpRestrictions().length; r++) {
            var restriction = {
                Name: self.IpRestrictions()[r].Name(),
                Start: self.IpRestrictions()[r].Start(),
                End: self.IpRestrictions()[r].End()
            };
            postModel.IpRestrictions.push(restriction);
        }

        $.ajax('/form/UpdateCompanySettings', {
            data: postModel,
            dataType: 'json',
            method: 'put'
        }).done(function (data, status, xhr) {
            amplify.publish("SaveAction", { context: 'CompanySettings', action: 'update', id: 0, custom: {} });
            data.message += " Refreshing page momentarily.";
            amplify.publish("ShowMessage", data);
        }).fail(function (xhr, status, error) {
            amplify.publish("ShowMessage", { type: "error", title: "Error!", message: error });
        }).always(function (data, status, error) {
            self.InProgress(false);
            console.log(status);
            if (error.status == "200") {
                setTimeout(function () { document.location.reload(); }, 3000);
            }
        });
    };

    //#endregion

    return self;
}

function Statistic(data) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.Slug = data.Slug;
    self.Score = data.Score;
}

//#endregion

//#region TILE VIEW MODELS

var BaseTileModel = function () {
    var self = this;

    self.ObjectType = '';
    self.ObjectID = 0;

    return self;
}

var BaseOverlayTileModel = function () {
    var self = this;

    self.DisabledClassName = "tile-disabled";

    self.overlayContext = ko.observable('TileOverlay');

    return self;
}
BaseOverlayTileModel.prototype = new BaseTileModel();

//var C hildArtifactsMicroTileModel = function (type, id) {
//    var self = this;

//    self.Statistics = ko.observableArray();
//    self.ObjectID = id;
//    self.ObjectType = type;

//    self.GetStatistics = function () {
//        $.getJSON(
//            '/api/' + self.ObjectType + '/' + self.ObjectID + '/artifacts/statistics',
//            function (data) {
//                var mappedItems = $.map(data, function (item) { return new ChildArtifactsMicroTileItem(self.ObjectID, item.Name, item.ID, item.Count); });
//                self.Statistics(self.Statistics().concat(mappedItems));

//                //self.Statistics().length

//            }
//        );
//    }

//    return self;
//}
//C hildArtifactsMicroTileModel.prototype = new BaseOverlayTileModel();

var EventsMicroTileModel = function (type, id) {
    var self = this;

    self.Statistics = ko.observableArray();
    self.ObjectID = id;
    self.ObjectType = type;

    self.eventsOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/Events';
    }, self);

    self.GetStatistics = function () {
        $.getJSON(
            '/api/' + self.ObjectType + '/' + self.ObjectID + '/events/statistics',
            function (data) {
                var mappedItems = $.map(data, function (item) { return new EventsMicroTileItem(item.Status, item.Count, 'Unknown'); });
                self.Statistics(self.Statistics().concat(mappedItems));
            }
        );
    }

    return self;
}
EventsMicroTileModel.prototype = new BaseOverlayTileModel();

var FusionCommandTileModel = function (typeID, id) {
    var self = this;

    self.FusionTypeID = typeID;
    self.FusionID = id;

    self.OwnershipRuleCount = ko.observable(0);
    self.PromotionRuleCount = ko.observable(0);

    self.ownershipOverlayUri = ko.computed(function () {
        return "/fusion/" + self.FusionTypeID + "/configurations/" + self.FusionID + "/ownership";
    }, self);

    self.promotionOverlayUri = ko.computed(function () {
        return "/fusion/" + self.FusionTypeID + "/configurations/" + self.FusionID + "/promotion";
    }, self);

    self.GetStatistics = function () {
        $.getJSON(
            '/fusion/GetFusionRuleStatistics?id=' + self.FusionID,
            function (data) {
                self.OwnershipRuleCount(data.OwnershipRuleCount);
                self.PromotionRuleCount(data.PromotionRuleCount);
            }
        );
    }

    return self;
}
FusionCommandTileModel.prototype = new BaseOverlayTileModel();

var HomeSocialMicroTileModel = function (resourceID) {
    var self = this;

    self.FollowerCount = ko.observable();
    self.GroupCount = ko.observable();
    self.CurrentResourceID = ko.observable(resourceID);
    //self.Score = ko.observable();
    self.ScoreUri = ko.computed(function () {
        return '/overlays/Resource/' + self.CurrentResourceID() + '/score';
    }, self);

    self.GetStatistics = function () {
        $.getJSON(
            '/tiles/HomeSocial',
            function (data) {
                self.FollowerCount(data.FollowerCount);
                self.GroupCount(data.GroupCount);
                //self.Score(data.Score);


                drawKpi('#ScoreKpi', 'Governance score', data.Score, 100 - data.Score, true);
            }
        );
    }

    self.OpenFollowers = function () {
        $('#HomeSocialFollowerCount').qtip({
            content: {
                title: 'Your Followers',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Resource/' + self.CurrentResourceID() + '/followers'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    self.OpenGroups = function () {
        $('#HomeSocialGroupCount').qtip({
            content: {
                title: 'Your Group Memberships',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Resource/' + self.CurrentResourceID() + '/groups'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    return self;
}

var GroupSocialMicroTileModel = function (id) {
    var self = this;

    self.GroupID = ko.observable(id);
    self.FollowerCount = ko.observable();
    self.MemberCount = ko.observable();

    self.GetStatistics = function () {
        $.getJSON(
            '/tiles/GroupSocial?id=' + self.GroupID(),
            function (data) {
                self.FollowerCount(data.FollowerCount);
                self.MemberCount(data.MemberCount);
            }
        );
    }

    self.OpenFollowers = function () {
        $('#SocialFollowerCount').qtip({
            content: {
                title: 'Followers',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Group/' + self.GroupID() + '/followers'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    self.OpenMembers = function () {
        $('#SocialMemberCount').qtip({
            content: {
                title: 'Members',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/groups/' + self.GroupID() + '/members'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    return self;
}

var ProfileSocialMicroTileModel = function (resourceID) {
    var self = this;

    self.FollowerCount = ko.observable();
    self.FollowingCount = ko.observable();
    self.GroupCount = ko.observable();
    self.ResourceID = ko.observable(resourceID);

    self.GetStatistics = function () {
        $.getJSON(
            '/tiles/ProfileSocial?id=' + self.ResourceID(),
            function (data) {
                self.FollowerCount(data.FollowerCount);
                self.FollowingCount(data.FollowingCount);
                self.GroupCount(data.GroupCount);
            }
        );
    }

    self.OpenFollowers = function () {
        $('#HomeSocialFollowerCount').qtip({
            content: {
                title: 'Followers',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Resource/' + self.ResourceID() + '/followers'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    self.OpenFollowing = function () {
        $('#HomeSocialFollowingCount').qtip({
            content: {
                title: 'Following',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Resource/' + self.ResourceID() + '/following'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    self.OpenGroups = function () {
        $('#HomeSocialGroupCount').qtip({
            content: {
                title: 'Your Group Memberships',
                // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/parts/Resource/' + self.ResourceID() + '/groups'
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            //hide: 'mouseout',
            style: {
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
            //addTooltip(this);
        });
    }

    return self;
}

//var SocialMicroTileModel = function (type, id) {
//    var self = this;

//    self.ObjectID = id;
//    self.ObjectType = type;

//    self.FollowerCount = ko.observable(0);
//    self.CommentCount = ko.observable(0);
//    self.CommentCountLast48Hours = ko.observable(0);

//    self.commentsOverlayUri = ko.computed(function () {
//        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/comments';
//    }, self);

//    self.followersOverlayUri = ko.computed(function () {
//        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/followers';
//    }, self);

//    self.GetStatistics = function () {
//        $.getJSON(
//            '/api/' + self.ObjectType + '/' + self.ObjectID + '/social/statistics',
//            function (data) {
//                self.FollowerCount(data.FollowerCount);
//                self.CommentCount(data.CommentCount);
//                self.CommentCountLast48Hours(data.CommentCountLast48Hours);
//            }
//        );
//    }

//    return self;
//}
//SocialMicroTileModel.prototype = new BaseOverlayTileModel();

function ObjectStatistic(data, type, id) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.Value = data.Value;
    self.Group = data.Group;
    self.TypeIdentifier = data.TypeIdentifier;

    self.ObjectType = type;
    self.ObjectID = id;

    self.ChildArtifactsOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectID + '/' + self.TypeIdentifier + '/ChildArtifacts';
    }, self);

    self.EventsOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/Events';
    }, self);

    self.SocialOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/' + self.Name;
    }, self);

    self.ScoreOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/score';
    }, self);
}

var ObjectStatisticsTileModel = function (type, id) {
    var self = this;

    //self.ScoreName = ko.observable();
    //self.ScoreValue = ko.observable();

    self.ScoreModels = ko.observableArray();
    self.SocialModels = ko.observableArray();
    self.EventModels = ko.observableArray();
    self.ChildModels = ko.observableArray();

    self.ObjectID = id;
    self.ObjectType = type;

    //self.ScoreOverlayUri = ko.computed(function () {
    //    return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/score';
    //}, self);
    
    self.ChangeObject = function (type, id) {
        self.ObjectType = type;
        self.ObjectID = id;
    }

    self.GetStatistics = function () {
        $.getJSON(
            '/api/' + self.ObjectType + '/' + self.ObjectID + '/object/statistics',
            function (data) {
                self.ChildModels.removeAll();
                self.EventModels.removeAll();
                self.SocialModels.removeAll();
                self.ScoreModels.removeAll();

                ko.utils.arrayMap(data, function (item) {

                    var model = new ObjectStatistic(item, self.ObjectType, self.ObjectID);

                    switch (model.Group) {
                        case "Children":
                            self.ChildModels.push(model);
                            break;
                        case "Event":
                            self.EventModels.push(model);
                            break;
                        case "Score":
                            self.ScoreModels.push(model);
                            //self.ScoreName(model.Name);
                            //self.ScoreValue(model.Value);
                            break;
                        case "Social":
                            self.SocialModels.push(model);
                            break;
                    }

                });
            }
        );
    }

    return self;
}
ObjectStatisticsTileModel.prototype = new BaseOverlayTileModel();

function PolicyRuleStatistic(data, type, id) {
    var self = this;
    data = data || {};
    self.Suffix = data.Suffix;
    self.Name = data.Name;
    self.Count = data.Count;

    self.ObjectType = type;
    self.ObjectID = id;

    self.SocialOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/' + self.Suffix;
    }, self);
}

var PolicyRuleStatisticsTileModel = function (type, id) {
    var self = this;

    self.SocialModels = ko.observableArray();

    self.ObjectID = id;
    self.ObjectType = type;

    self.ChangeObject = function (type, id) {
        self.ObjectType = type;
        self.ObjectID = id;
    }

    self.GetStatistics = function () {
        $.getJSON(
            '/queries/' + self.ObjectType + '/' + self.ObjectID + '/SocialBreakdown',
            function (data) {
                self.SocialModels.removeAll();

                ko.utils.arrayMap(data, function (item) {
                    self.SocialModels.push(
                        new PolicyRuleStatistic(item, self.ObjectType, self.ObjectID)
                        );
                });
            }
        );
    }

    return self;
}
PolicyRuleStatisticsTileModel.prototype = new BaseOverlayTileModel();


var ReportAreaTileModel = function (data) {
    var self = this;
    data = data || {};
    self.ID = data.ID || 0;
    self.Icon = data.Icon || '';
    self.Name = data.Name || '';
    self.ReportTileType = data.ReportTileType || 0;

    self.Settings = data.Settings || {};
    self.Data = data.Settings.data || '';
    self.Display = data.Settings.display || '';
    self.XAxis = data.Settings.xaxis || '';

    self.IconClasses = ko.computed(function () {
        if (self.Icon != '') {
            return 'fa fa-4x ' + self.Icon;
        }
        else {
            return '';
        }
    }, self);

    self.DesignerTileID = ko.computed(function () {
        return 'DesignerAreaTile' + self.ID;
    }, self);

    self.TileID = ko.computed(function () {
        return 'AreaTile' + self.ID;
    }, self);

    return self;
}

var ReportAreaModel = function (data, inDesign) {
    var self = this;
    data = data || {};
    self.ID = data.id || 0;
    self.Height = data.height || 0;
    self.InDesign = inDesign;

    self.Tiles = ko.observableArray();

    self.AreaID = ko.computed(function () {
        return 'Area' + self.ID;
    }, self);

    self.DesignerAreaID = ko.computed(function () {
        return 'DesignerArea' + self.ID;
    }, self);

    self.BootstrapClass = ko.computed(function () {
        return (self.InDesign) ? 'report-area-design' : 'report-area';
    }, self);

    self.HeightStyle = ko.computed(function () {
        return '';//(self.Height > 0) ? 'overflow-y: scroll; height: ' + self.Height + 'max-height: ' + self.Height : '';
    }, self);

    ko.utils.arrayMap(data.tiles, function (item) {
        var model = new ReportAreaTileModel(item);
        self.Tiles.push(model);
    });

    return self;
}

var ReportCellModel = function (data, inDesign) {
    var self = this;
    data = data || {};
    self.Length = data.length || 0;
    self.InDesign = inDesign;
    self.Areas = ko.observableArray();

    self.BootstrapClass = ko.computed(function () {
        return 'col s' + self.Length;
    }, self);

    ko.utils.arrayMap(data.areas, function (item) {
        var model = new ReportAreaModel(item, self.InDesign);
        self.Areas.push(model);
    });

    return self;
}

var ReportRowModel = function (data, inDesign) {
    var self = this;
    data = data || {};

    self.InDesign = inDesign;

    self.Cells = ko.observableArray();

    ko.utils.arrayMap(data.cells, function (item) {
        var model = new ReportCellModel(item, self.InDesign);
        self.Cells.push(model);
    });

    return self;
}

var ReportModel = function (reportID, type, id, inDesign) {
    var self = this;

    self.Rows = ko.observableArray();
    //self.ReportTiles = ko.observableArray();

    self.ReportID = 0;
    self.ObjectID = id || 0;
    self.ObjectType = type || '';
    self.InDesign = inDesign;

    self.ReportOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/report/' + self.ReportID;
    }, self);


    self.ChangeObject = function (reportID, type, id) {
        // create a deferred object
        var r = $.Deferred();

        self.ReportID = reportID || 0;
        if (type && id) {
            self.ObjectType = type;
            self.ObjectID = id;
        }

        setTimeout(function () {
            // and call `resolve` on the deferred object, once you're done
            r.resolve();
        }, 2500);

        // return the deferred object
        return r;
    }

    self.GetLayout = function () {
        // create a deferred object
        var r = $.Deferred();

        $.getJSON(
            '/reports/' + self.ReportID + '/layout',
            function (data) {
                self.Rows.removeAll();
                ko.utils.arrayMap(data, function (item) {
                    var model = new ReportRowModel(item, self.InDesign);
                    self.Rows.push(model);
                });
            }
        );

        setTimeout(function () {
            // and call `resolve` on the deferred object, once you're done
            r.resolve();
        }, 2500);

        // return the deferred object
        return r;
    }

    self.GetTiles = function () {
        $.getJSON(
            '/reports/' + self.ReportID + '/tiles',
            function (data) {
                //self.ReportTiles.removeAll();

                ko.utils.arrayMap(data, function (item) {
                    //var model = new ReportTileModel(self.ObjectType, self.ObjectID, item, self.InDesign);
                    //self.ReportTiles.push(model);
                    if (self.InDesign) {
                        var areaID = 'Area' + item.ContentAreaNumber;
                        var dataTileID = areaID + '_Tile' + item.ID;
                        //alert(areaID);
                        $('#' + areaID).append('<div id="' + dataTileID + '">' + item.Name + '</div>');
                    }
                    else {
                    }
                });
            }
        );
    }

    function GetHeaders(data) {
        var cols = new Array();
        var p = data[0];
        for (var key in p) {
            cols.push(key);
        }
        return cols;
    }

    function CreateTable(data, cols) {
        var div = $('<div class="table-responsive"></div>')
        var table = $('<table class="table table-hover table-condensed table-striped"></table>');
        var th = $('<tr></tr>');
        for (var i = 0; i < cols.length; i++) {
            th.append('<th>' + cols[i] + '</th>');
        }
        table.append(th);

        for (var j = 0; j < data.length; j++) {
            var datarow = data[j];
            var tr = $('<tr></tr>');
            for (var k = 0; k < cols.length; k++) {
                var columnName = cols[k];
                tr.append('<td>' + datarow[columnName] + '</td>');
            }
            table.append(tr);
        }
        div.append(table);
        return div;
    }

    self.Render = function () {
        $('div[data-reporttype][data-design="0"]').each(function () {
            var tile = $(this);
            var type = tile.data("reporttype");
            tile.html('<i class="fa fa-spinner fa-spin fa-4x"></i>');
            $.getJSON(
                '/services/reports/' + self.ReportID + '/' + self.ObjectType + '/' + self.ObjectID + '/tiles/' + tile.data("reporttileid") + '/data',
                function (data) {

                    if (data.error) {
                        tile.html(data.error);
                    }
                    else {
                        var cols = GetHeaders(data);
                        var fields = [];
    
                        switch (type) {
                            case 1:
                                //#region Grid
                                var columns = [];
                                $.each(cols, function () {
                                    fields.push({ name: this });
                                    columns.push({ text: this, dataField: this });
                                });
                                tile.html('');

                                var source = {
                                                localData: data,
                                                dataType: "array",
                                                dataFields: fields
                                             };
                                var adapter = new $.jqx.dataAdapter(source);
                                tile.jqxDataTable(
                                {
                                    width: grid_width,
                                    theme: list_theme,
                                    filterable: true,
                                    sortable: true,
                                    pageable: true,
                                    pagerMode: 'advanced',
                                    source: adapter,
                                    columnsResize: true,
                                    columns: columns
                                });
                                //#endregion
                                break;
                            case 6:
                                //#region Matrix
                                //#endregion
                                break;
                            default:
                                //#region Chart

                                $.each(cols, function () {
                                    fields.push({ name: this });
                                });
                                tile.html('');
                                tile.css('width', '100%');
                                tile.css('height', '400px');
                                var source = {
                                    localData: data,
                                    dataType: "array",
                                    datafields: fields
                                };

                                var adapter = new $.jqx.dataAdapter(source);

                                var settings;
                                var s = tile.data('settings');

                                var seriesDataField = tile.data('data');
                                var seriesDisplayField = tile.data('display');
                                var xAxisField = tile.data('xaxis');

                                switch (type) {
                                    case 2:
                                        //#region Pie
                                        
                                        tile.jqxChart({
                                            title: '',
                                            description: '',
                                            enableAnimations: true,
                                            showLegend: true,
                                            showBorderLine: false,
                                            source: adapter,
                                            colorScheme: chartDefaultTheme,
                                            seriesGroups: [{
                                                type: 'pie',
                                                series: [{
                                                    showLabels: true,
                                                    useGradient: false,
                                                    dataField: seriesDataField,
                                                    displayText: seriesDisplayField,
                                                    labelRadius: 80,
                                                    initialAngle: 15,
                                                    radius: 100,
                                                    innerRadius: 50,
                                                    centerOffset: 0
                                                }]
                                            }]
                                        });

                                        //#endregion
                                        break;
                                    case 3:
                                        //#region Area

                                        tile.jqxChart({
                                            title: '',
                                            description: '',
                                            enableAnimations: true,
                                            showLegend: true,
                                            showBorderLine: false,
                                            source: adapter,
                                            colorScheme: chartDefaultTheme,
                                            xAxis: {
                                                dataField: xAxisField,
                                                showTickMarks: true,
                                                tickMarksInterval: 1,
                                                tickMarksColor: '#888888',
                                                unitInterval: 1,
                                                showGridLines: true,
                                                gridLinesInterval: 3,
                                                gridLinesColor: '#888888',
                                                valuesOnTicks: true,
                                                textRotationAngle: -45,
                                                textRotationPoint: 'topright',
                                                textOffset: { x: 0, y: -25 }

                                            },
                                            seriesGroups: [{
                                                type: 'area',
                                                valueAxis: {
                                                    displayValueAxis: true,
                                                    description: '',
                                                    axisSize: 'auto',
                                                    tickMarksColor: '#888888'
                                                },
                                                series: [
                                                        { dataField: seriesDataField, displayText: seriesDisplayField }
                                                ]
                                            }]
                                        });

                                        //#endregion
                                        break;
                                    case 4:
                                        //#region Bar

                                        tile.jqxChart({
                                            title: '',
                                            description: '',
                                            enableAnimations: true,
                                            showLegend: true,
                                            showBorderLine: false,
                                            source: adapter,
                                            colorScheme: chartDefaultTheme,
                                            xAxis: {
                                                dataField: xAxisField,
                                                showTickMarks: true,
                                                tickMarksInterval: 1,
                                                tickMarksColor: '#888888',
                                                unitInterval: 1,
                                                showGridLines: true,
                                                gridLinesInterval: 3,
                                                gridLinesColor: '#888888',
                                                valuesOnTicks: true,
                                                //minValue: '01-01-2011',
                                                //maxValue: '01-01-2012',
                                                textRotationAngle: -45,
                                                textRotationPoint: 'topright',
                                                textOffset: { x: 0, y: -25 }

                                            },
                                            seriesGroups: [{
                                                type: 'column',
                                                valueAxis: {
                                                    displayValueAxis: true,
                                                    description: '',
                                                    axisSize: 'auto',
                                                    tickMarksColor: '#888888'
                                                },
                                                series: [
                                                        { dataField: seriesDataField, displayText: seriesDisplayField }
                                                ]
                                            }]
                                        });

                                        //#endregion
                                        break;
                                    case 5:
                                        //#region Line

                                        tile.jqxChart({
                                            title: '',
                                            description: '',
                                            enableAnimations: true,
                                            showLegend: true,
                                            showBorderLine: false,
                                            source: adapter,
                                            colorScheme: chartDefaultTheme,
                                            xAxis: {
                                                dataField: xAxisField,
                                                //formatFunction: function (value) {
                                                //    return value.getDate() + '-' + months[value.getMonth()] + '-' + value.getFullYear();
                                                //},
                                                //type: 'date',
                                                //baseUnit: 'month',
                                                showTickMarks: true,
                                                tickMarksInterval: 1,
                                                tickMarksColor: '#888888',
                                                unitInterval: 1,
                                                showGridLines: true,
                                                gridLinesInterval: 3,
                                                gridLinesColor: '#888888',
                                                valuesOnTicks: true,
                                                //minValue: '01-01-2011',
                                                //maxValue: '01-01-2012',
                                                textRotationAngle: -45,
                                                textRotationPoint: 'topright',
                                                textOffset: { x: 0, y: -25 }

                                            },
                                            seriesGroups: [{
                                                type: 'line',
                                                valueAxis: {
                                                    displayValueAxis: true,
                                                    description: '',
                                                    axisSize: 'auto',
                                                    tickMarksColor: '#888888'
                                                },
                                                series: [
                                                        { dataField: seriesDataField, displayText: seriesDisplayField }
                                                ]
                                            }]
                                        });

                                        //#endregion
                                        break;
                                }

                                //tile.jqxChart('addColorScheme', 'myScheme', colorScheme);
                                //tile.jqxChart('colorScheme', 'myScheme');
                                //tile.jqxChart('refresh');

                                //#endregion
                                break;
                        }
                    }


                }
            );
        });
    }

    return self;
}
ReportModel.prototype = new BaseOverlayTileModel();

var ReportDesignerModel = function (reportID) {
    var self = this;

    self.ReportID = 0;
    self.Rows = ko.observableArray();

    self.ChangeObject = function (reportID) {
        // create a deferred object
        var r = $.Deferred();

        self.ReportID = reportID || 0;

        setTimeout(function () {
            // and call `resolve` on the deferred object, once you're done
            r.resolve();
        }, 2500);

        // return the deferred object
        return r;
    }

    self.GetLayout = function () {
        // create a deferred object
        var r = $.Deferred();

        $.getJSON(
            '/reports/' + self.ReportID + '/layout',
            function (data) {
                self.Rows.removeAll();
                ko.utils.arrayMap(data, function (item) {
                    var model = new ReportRowModel(item, true);
                    self.Rows.push(model);
                });
            }
        );

        setTimeout(function () {
            // and call `resolve` on the deferred object, once you're done
            r.resolve();
        }, 2500);

        // return the deferred object
        return r;
    }

    return self;
}
ReportDesignerModel.prototype = new BaseOverlayTileModel();

var ReportLayoutModel = function (reportLayoutID) {
    var self = this;

    self.Rows = ko.observableArray();
    self.ReportLayoutID = 0;

    self.ChangeObject = function (reportLayoutID) {
        // create a deferred object
        var r = $.Deferred();

        self.ReportLayoutID = reportLayoutID || 0;

        $.getJSON(
            '/reports/layouts/' + self.ReportLayoutID + '/layout',
            function (data) {
                self.Rows.removeAll();
                ko.utils.arrayMap(data, function (item) {
                    var model = new ReportRowModel(item, true);
                    self.Rows.push(model);
                });
            }
        );

        setTimeout(function () {
            // and call `resolve` on the deferred object, once you're done
            r.resolve();
        }, 2500);

        // return the deferred object
        return r;
    }

    return self;
}
ReportLayoutModel.prototype = new BaseOverlayTileModel();



var HomePageCountTileModel = function (title,days) {
    var self = this;

    self.Title = ko.observable(title);
    self.Rows = ko.observableArray();
    self.LookBackDays = ko.observable(days);
    self.NoDataMessage = ko.observable("No " + title);
    //rows has name, total count, new count
    

    return self;
}

function SearchResultCategory(data) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.DisplayName = data.DisplayName;
    self.ResultCount = data.ResultCount;
    self.Categories = data.Categories;
    self.showRow = ko.observable(data.Name == 'Artifact' ? true : false);
    self.toggleVisibility = function () {
        self.showRow(!self.showRow());
    };
    self.showToggle = data.Categories != null;
}

function SearchAdvancedFilter(selectedField, search, exact) {
    var self = this;    
    self.Term = ko.observable(search);
    self.exactMatch = ko.observable(exact);
    self.SelectedFieldIndex = ko.observable(selectedField);
    self.SelectedTypeIndex = ko.observable();
    self.TypeNames = ko.observableArray([
        { title: "Attribute", value: "Attribute" },
        { title: "Fusion", value: "FusionAttributes" },
        { title: "Fusion Type", value: "FusionType" },
        { title: "Glossary", value: "Artifact" },
        { title: "Group", value: "Group" },
        { title: "Model", value: "Taxonomy" },
        { title: "Reference", value: "Domain" },
        { title: "User", value: "Users" },
    ]);
    self.ShowConnectors = ko.observable(false);
    self.Connectors = ko.observableArray([
        { title: "And", value: "and" },
        { title: "Or", value: "or" },
    ]);
    self.SelectedConnectorIndex = ko.observable(0);
    self.ShowText = ko.computed(function () {        
        return (self.SelectedFieldIndex() != 3);
    });
}

function SearchViewModel() {
    var self = this;
    self.categories = ko.observableArray();
    self.results = ko.observableArray();
    self.elapsedTime = ko.observable();
    self.AdvancedSearchCallback = null;

    self.addFilter = function () {
        self.advancedFilter.push(new SearchAdvancedFilter(-1,"", false));
    };

    self.removeFilter = function (index) {
        if (index > -1) {            
            self.advancedFilter.splice(index, 1);
        }
    };

    self.onEnter = function (d, e) {
        if (e.keyCode === 13) {            
            if (self.AdvancedSearchCallback) self.AdvancedSearchCallback();
        }
        return true;
    };

    self.advancedFilterJSON = function () {
        var filter = new Array();
        for (var i = 0 ; i < self.advancedFilter().length; i++) {            
            var fieldName = (self.advancedFilter()[i].SelectedFieldIndex() >= 0 ? self.FieldNames()[self.advancedFilter()[i].SelectedFieldIndex()].value : "");            
            if (fieldName == "") continue;
            var val = self.advancedFilter()[i].Term();
            if (fieldName == '_type') {                
                val = self.advancedFilter()[i].TypeNames()[self.advancedFilter()[i].SelectedTypeIndex()].value;
            }
            var con = self.advancedFilter()[i].Connectors()[self.advancedFilter()[i].SelectedConnectorIndex()].value;

            if (val == "") continue;

            filter[i] = { field: fieldName, value: val, exact: self.advancedFilter()[i].exactMatch(), connector: con };
        }
        return JSON.stringify(filter);
    }

    self.FieldNames = ko.observableArray([{ title: "Category", value: "Type" }, { title: "Description", value: "Description" }, { title: "Name", value: "Name" }, { title: "Type", value: "_type" } ]);
    self.advancedFilter = ko.observableArray();

    self.showAdvanced = function (phrase) {
        self.advancedFilter([]);        
        self.advancedFilter.push(new SearchAdvancedFilter(2,phrase, false));
    }
    return self;
}



//#endregion

//#region VIEW MODELS

var PageViewModel = function (title, directions, breadcrumbs, type, id, hideHeader) {
    var self = this;
    self.Title = title;
    self.Directions = directions;
    self.breadcrumbs = breadcrumbs;
    self.ObjectType = type || "";
    self.ObjectID = id || 0;      
    self.ShowHeader = !(hideHeader || false);
    return self;
}

var ResourcePageViewModel = function (title, directions, breadcrumbs) {
    var self = this;
    self.ID = 0;
    self.Title = title;
    self.Directions = directions;
    self.breadcrumbs = breadcrumbs;
    return self;
}

var BoardViewModel = function (initialDaysToLookBack) {
    var self = this;
    self.comments = ko.observableArray();
    self.newMessage = ko.observable();
    self.newMessageType = ko.observable();
    self.newMessageVisibility = ko.observable();
    self.error = ko.observable();
    self.moreComments = ko.observable();
    self.searchFilter = ko.observable('');
    self.ProcessingCount = ko.observable(0);
    self.ShowDateFilter = ko.observable(true);
    self.ShowTypeFilter = ko.observable(true);
    self.ShowSearch = ko.observable(true);

    self.IsProcessing = ko.computed(function () {
        return (self.ProcessingCount() != 0);
    });

    self.tagIndex = -1;

    self.setIndex = function (data, event) {
        //38, 40, 37, 39, 13
        //console.log(event);
        

        if (event.keyCode == 13) { //enter key
            if (self.tagSuggestions().length == 1) {
                var t = self.tagSuggestions()[0];
                t.addTag();                
                //self.tags.push(t);
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {
                var t = self.tagSuggestions()[self.tagIndex];
                t.addTag();
                //t.ShowRemove(true);
                //self.tags.push(t);
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
        }
        else if (event.keyCode == 40 || event.keyCode == 38) { //up & down arrows
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {
                
                self.tagSuggestions()[self.tagIndex].IsSelected(false);

                if (event.keyCode == 38 && self.tagIndex > 0) {
                    self.tagIndex--;
                }
                else if (event.keyCode == 40 && self.tagIndex < self.tagSuggestions().length) {
                    self.tagIndex++;
                }
                
            } else {
                self.tagIndex = 0;
            }

            if (self.tagIndex != -1) {
                self.tagSuggestions()[self.tagIndex].IsSelected(true);
            }
            return false;
        }
        else {
            self.tagIndex = -1;
            return true;
        }
    };

    self.AppliedSearch = ko.computed(self.searchFilter).extend({ throttle: 400 });

    self.AppliedSearch.subscribe(function () {
        self.filterComments();
    },self);

    self.pageSize = 25;
    self.startMatch = /@/ig; //new RegExp("@");
    self.wordMatch = /@(\w+)/ig; //new RegExp("@(\w+)");

    self.newComments = ko.observableArray();
    self.commentCounts = ko.observableArray();

    self.ShowAddCommentControls =  ko.observable(CompanySettings.DisableCommunityPosting == 'false' || CompanySettings.DisableIssuePosting == 'false' || ComanySettings.DisableQuestionPosting);
    

    self.ObjectType = null;
    self.ObjectID = null;
    self.VisibilityID = null;

    self.FilterObjectType = null;
    self.FilterObjectID = null;

    self.dateFilterOptions = ko.observableArray([
        { Text: ' over past day', Value: -1 },
        { Text: 'over past week', Value: -7 },
        { Text: 'over past month', Value: -30 },        
        { Text: 'All time', Value: 0 }
    ]);

    var typeOps = [];
    
    var discussion = { Text: 'Discussion', Value: 2 };
    var issue = { Text: 'Issue', Value: 5 };
    //var question = { Text: 'Question', Value: 9 };

    if (CompanySettings.DisableCommunityPosting == 'false') {
        typeOps.push(discussion);
    }
    //if (CompanySettings.DisableIssuePosting == 'false') {
    //    typeOps.push(issue);
    //}
    //if (CompanySettings.DisableQuestionPosting == 'false') {
    //    typeOps.push(question);
    //}

    self.typeEntryOptions = ko.observableArray(typeOps);

    //self.typeEntryOptions = ko.observableArray([
    //    //{ Text: 'Data Event', Value: 8 },
    //    { Text: 'Discussion', Value: 2 },
    //    { Text: 'Issue', Value: 5 },
    //    //{ Text: 'Task', Value: 6 },
    //    { Text: 'Question', Value: 9 }
    //]);

    self.visibilityOptions = ko.observableArray([
        { Text: 'All', Value: 4 },
        { Text: 'Followers', Value: 3 },
        { Text: 'Owners', Value: 2 },
        { Text: 'Only Me', Value: 1 }
    ]);

    self.typeFilterOptions = ko.observableArray([
        { Text: 'All types', Value: 0 },
        { Text: 'Data Events', Value: 8 },
        { Text: 'Discussions', Value: 2 },
        //{ Text: 'Governance', Value: 3 },
        { Text: 'Issues', Value: 5 },
        //{ Text: 'System Notifications', Value: 1 },
        { Text: 'Red Flag Alerts', Value: 7 },
        //{ Text: 'Relationships', Value: 4 },
        //{ Text: 'Tasks', Value: 6 },
        //{ Text: 'Questions', Value: 9 }
        { Text: 'Challenges', Value: 9 }
    ]);

    self.selectedDateFilterOption = ko.observable(initialDaysToLookBack === undefined ? -7 : initialDaysToLookBack);
    self.selectedTypeFilterOption = ko.observable();

    self.tagSuggestions = ko.observableArray();
    self.tagSuggestionsPresent = ko.computed(function () {
        return (self.tagSuggestions().length > 0);
    }, self);

    self.newTag = ko.observable();
    self.tags = ko.observableArray();
    self.newTag.subscribe(function (value) {
        if (value) {
            $.getJSON('/api/tagsuggestions', { phrase: value }, function (suggestions) {
                // Object, ObjectID, TextPath, Url, ObjectTypeName
                var mappedSuggestions = $.map(suggestions, function (item) { return new CommentTagItem(item, self); });
                self.tagSuggestions(mappedSuggestions);
            });
        }
        else {
            self.tagSuggestions([]);
        }
    });


    self.CanReply = ko.computed(function () {
        return (!self.IsProcessing() && (self.newMessage() || '').length > 0  );
    });


    //self.checkForTags = function () {
    //    var message = self.newMessage() + "";
    //    try {
    //        var name = message.match(self.wordMatch);
    //        if (name.length > 0) {
    //            var phrase = name[name.length - 1];
    //            phrase = phrase.replace('@', '');
    //            $.getJSON('/api/tagsuggestions', { phrase: phrase }, function (suggestions) {
    //                // Object, ObjectID, TextPath, Url, ObjectTypeName
    //                var mappedSuggestions = $.map(suggestions, function (item) { return new CommentTagSuggestionItem(item); }); //, self.hub
    //                self.tagSuggestions(mappedSuggestions);
    //            });
    //        }
    //    } catch (e) {

    //    }
    //}

    self.clearFields = function () {
        self.newMessage('');
    }

    self.filterComments = function () {
        $.jqx.cookie.cookie("BoardDateFilterCookie", self.selectedDateFilterOption());
        self.comments.removeAll();
        self.getMoreComments();
    }

    self.filterCommentsByID = function (id) {
        self.selectedTypeFilterOption(id);
        $.jqx.cookie.cookie("BoardDateFilterCookie", self.selectedDateFilterOption());
        self.comments.removeAll();
        self.getMoreComments();
    }

    self.changeObject = function (objectType, objectID) {
        try {
        //ko.cleanNode(self.element);//(document.getElementById('Board'));
        //ko.applyBindings(self, self.element);//boardVm, document.getElementById('Board'));

            self.error(null);
            self.comments([]);

            self.ObjectType = objectType;
            self.ObjectID = objectID;

            self.selectedDateFilterOption(initialDaysToLookBack === undefined ? -7 : initialDaysToLookBack);


            self.getMoreComments();
        }
        catch (e) {
            console.log(e);
        }
    };

    self.getMoreComments = function () {

        self.ProcessingCount(self.ProcessingCount() + 2);
        $.ajax({
            data: {
                "ObjectType": self.ObjectType,
                "ObjectID": self.ObjectID,
                "Skip": self.comments().length,
                "Take": self.pageSize,
                "DateFilter": self.selectedDateFilterOption(),
                "TypeFilter": self.selectedTypeFilterOption(),
                "SearchFilter": self.searchFilter
            },
            dataType: 'json',
            method: 'POST',
            url: '/services/community/comments'
        }).done(function (commentData, status, xhr) {
            //alert(ko.toJSON(commentData));
            var mappedPosts = $.map(commentData, function (item) { return new CommentItem(item, self); }); //, self.hub
            self.comments(self.comments().concat(mappedPosts));
            self.moreComments(mappedPosts.length >= self.pageSize);
            if (self.FilterObjectType && self.FilterObjectID) {
                self.setCommentsFilter(self.FilterObjectType, self.FilterObjectID);
            }
            self.ProcessingCount(self.ProcessingCount() - 1);
        }).fail(function (xhr, status, error) {
            self.error(status);
            self.ProcessingCount(self.ProcessingCount() - 1);
        });

        $.ajax({
            data: {
                "ObjectType": self.ObjectType,
                "ObjectID": self.ObjectID,
                "DateFilter": self.selectedDateFilterOption(),
                "TypeFilter": self.selectedTypeFilterOption(),
                "SearchFilter": self.searchFilter
            },
            dataType: 'json',
            method: 'POST',
            url: '/services/community/counts'
        }).done(function (commentData, status, xhr) {
            var commentCountResults = $.map(commentData, function (item) { return new CommentCountItem(item); }); //, self.hub
            for (var i = 0; i < commentCountResults.length; i++) {
                commentCountResults[i].CurrentlySelectedValue = self.selectedTypeFilterOption();
            }
            self.commentCounts([]);
            self.commentCounts(self.commentCounts().concat(commentCountResults));
            self.ProcessingCount(self.ProcessingCount() - 1);
        }).fail(function (xhr, status, error) {
            self.error(status);
            self.ProcessingCount(self.ProcessingCount() - 1);
        });
        
        
    };


    self.addComment = function () {
        self.ProcessingCount(self.ProcessingCount() + 1);
        self.error(null);
        if (self.newMessage() != '') {

            var commentModel = {
                ObjectType: self.ObjectType,
                ObjectID: self.ObjectID,
                Tags: [],
                Comment: {
                    Body: self.newMessage(),
                    CommentTypeID: self.newMessageType(),
                    VisibilityID: self.newMessageVisibility()
                }
            };

            self.tags().forEach(function (tag) {
                commentModel.Tags.push({ Object: tag.Object(), ObjectID: tag.ObjectID() });
            });

            $.ajax({
                data: commentModel,
                dataType: 'json',
                method: 'POST',
                url: '/services/community/comment'
            }).done(function (newCommentData, status, xhr) {
                self.tags([]);
                self.comments.unshift(new CommentItem(newCommentData, self));
                self.newMessage('');
                self.ProcessingCount(self.ProcessingCount() - 1);
                amplify.publish("SaveAction", { context: 'commentform', action: "add", id: newCommentData.ID, custom: { CommentTypeID: self.newMessageType() } });
            }).fail(function (xhr, status, error) {
                self.ProcessingCount(self.ProcessingCount() - 1);
                self.error(status);
            });
        }
        else {
            self.ProcessingCount(self.ProcessingCount() - 1);
            self.error('Body may not be empty.');
        }
    };

    self.clearCommentsFilter = function () {
        self.FilterObjectType = null;
        self.FilterObjectID = null;

        ko.utils.arrayForEach(self.comments(), function (comment) {
            comment.isVisible(true);
        });
    };

    self.setCommentsFilter = function (objectType, objectID) {
        self.FilterObjectType = objectType;
        self.FilterObjectID = objectID;
        ko.utils.arrayForEach(self.comments(), function (comment) {
            comment.isVisible(comment.ObjectType == self.FilterObjectType && comment.ObjectID == self.FilterObjectID);
        });
    };

    self.loadNewComments = function () {
        self.comments(self.newComments().concat(self.comments()));
        self.newPosts([]);
    };

    //#region Functions called by the Hub

    //$.connection.socialHub.client.loadComments = function (data) {
    //    var mappedPosts = $.map(data, function (item) { return new CommentItem(item); });
    //    //self.comments(mappedPosts);
    //    self.comments(self.comments().concat(mappedPosts));
    //    self.moreComments(mappedPosts.length == self.pageSize);
    //};

    //$.connection.socialHub.client.addComment = function (comment) {
    //    self.comments.splice(0, 0, new CommentItem(comment));
    //    self.newMessage('');
    //};

    //$.connection.socialHub.client.error = function (err) {
    //    self.error(err);
    //};

    //$.connection.socialHub.client.newComment = function (comment, parentID) {
    //    if (parentID) {
    //        //check in existing posts
    //        var comments = $.grep(self.comments(), function (item) {
    //            return item.ID === parentID;
    //        });
    //        if (comments.length > 0) {
    //            comments[0].NewComments.push(new CommentItem(comment));
    //        }
    //        else {
    //            //check in new posts (not displayed yet)
    //            comments = $.grep(self.NewComments(), function (item) {
    //                return item.ID === parentID;
    //            });
    //            if (comments.length > 0) {
    //                comments[0].NewComments.push(new CommentItem(comment));
    //            }
    //        }
    //    }
    //    else {  // This is a parent comment
    //        self.newComments.splice(0, 0, new CommentItem(post));
    //    }
    //};

    //#endregion

    amplify.subscribe("ClearFilteredBoard", function() {
        self.clearCommentsFilter();
    });

    amplify.subscribe("ShowFilteredBoard", function (data) {
        self.setCommentsFilter(data.ObjectType, data.ObjectID);
        self.getMoreComments();
    });

    amplify.subscribe("SaveAction", function (data) {
        if (data.context == "IssueWorkflow") {
            self.comments([]);
            self.getMoreComments();
        }
    });

    return self;
}

var StatisticsBarViewModel = function () {
    var self = this;
    self.statistics = ko.observableArray();

    self.ObjectType = null;
    self.ObjectID = null;

    self.changeObject = function (objectType, objectID) {

        try {
            //ko.cleanNode(document.getElementById('StatBar'));
            //ko.applyBindings(statisticsVm, document.getElementById('StatBar'));

            if (objectType && objectID) {
                self.ObjectType = objectType;
                self.ObjectID = objectID;

                self.getStatistics();
            }
        }
        catch (e) {
            console.log(e);
        }
    };

    //self.init = function (objectType, objectID) {
    //    self.ObjectType = objectType;
    //    self.ObjectID = objectID;
    //    self.getStatistics();
    //};

    self.getStatistics = function () {
        $.getJSON('/api/' + self.ObjectType + '/' + self.ObjectID + '/statistics', function (data) {
            var stats = $.map(data, function (item) { return new Statistic(item); });
            self.statistics(stats);
        });
    };

    return self;
}

function GridFilterItemViewModel(columns, selectedColumn, selectedColumnValue) {
    var self = this;
    self.SelectedColumnIndex = ko.observable(selectedColumn !== undefined ? selectedColumn : -1);
    self.Columns = ko.observableArray(columns);
    self.TextValue = ko.observable('');
    self.DateValue = ko.observable(new Date());
    self.BoolValue = ko.observable(false);
    self.NumberValue = ko.observable(0);
    self.SelectedListIndex = ko.observable();
    self.ListBoxIsFilterable = ko.observable(false);
    self.inputType = ko.computed(function () {
        var inputType = (self.SelectedColumnIndex() >= 0 && self.Columns().length > 0) ? self.Columns()[self.SelectedColumnIndex()].columntype : "string";
        var filterType = 'string';
        switch (inputType) {
            case 'number':
            case 'numberinput':
                filterType = 'number';
                break;
            case 'checkbox':
                filterType = 'bool';
                break;
            case 'combobox':
            case 'dropdownlist':
                filterType = 'list';
                break;
            case 'datetimeinput':
                filterType = 'date';
                break;
        }
        return filterType;
    });
    self.selectedColumn = ko.computed(function () {
        return (self.SelectedColumnIndex() >= 0 ? self.Columns()[self.SelectedColumnIndex()] : null);
    });
    self.columnName = ko.computed(function () {
        return self.selectedColumn() != null ? self.selectedColumn().text : "";        
    });
    self.listItems = ko.computed(function () {        
        return self.selectedColumn() != null ? self.selectedColumn().filteritems : [];
    });
    self.listFilterable = ko.computed(function () {        
        return self.selectedColumn() != null ? self.listItems().length > 15 : false;
    });
    self.isRelationFieldFilter = ko.computed(function () {
        return self.selectedColumn() != null ? self.selectedColumn().relatedfield : false;
    });
    self.isHiddenFieldFilter = ko.computed(function () {
        return self.selectedColumn() != null ? self.selectedColumn().hiddenfield : false;
    });    
    self.value = ko.computed(function () {
        switch (self.inputType()) {
            case 'number':
                return self.NumberValue().toFixed(3);
            case 'bool':
                return self.BoolValue() ? "true" : "false";
            case 'list':
                return self.listItems()[self.SelectedListIndex()];
            case 'date':
                var yyyy = self.DateValue().getFullYear().toString();
                var mm = (self.DateValue().getMonth() + 1).toString(); // getMonth() is zero-based
                var dd = self.DateValue().getDate().toString();
                return mm + '/' + dd + '/' + yyyy;
            default:
                return self.TextValue();
        }
    });
    self.condition = ko.computed(function () {
        if (self.inputType() == 'string') return 'CONTAINS';
        return 'EQUAL';
    });
    self.field = ko.computed(function () {
        if (self.isRelationFieldFilter() || self.isHiddenFieldFilter())
            return self.selectedColumn() != null ? self.selectedColumn().id: '';
        return self.selectedColumn() != null ? self.selectedColumn().datafield : '';
    });

    if (selectedColumnValue) {
        switch (self.inputType()) {
            case 'number':
                return self.NumberValue(selectedColumnValue);
            default:
                return self.TextValue(selectedColumnValue);
        }
    }
}

function ArtifactFiltersViewModel(columns) {
    var self = this;
    self.Columns = ko.observableArray(columns);
    self.Filters = ko.observableArray();
    self.FilterCallback = null;

    self.addFilter = function () {
        self.Filters.push(new GridFilterItemViewModel(self.Columns()));
    };

    self.removeFilter = function (index) {
        if (index > -1) {
            self.Filters.splice(index, 1);
        }
    };

    self.filterData = function (type) {
        var filters = [];
        for (var i = 0 ; i < self.Filters().length; i++) {
            var relField = self.Filters()[i].isRelationFieldFilter();
            var hiddenField = self.Filters()[i].isHiddenFieldFilter();
            if (type == 'relation' && relField)
                filters.push({ field: self.Filters()[i].field(), condition: self.Filters()[i].condition(), value: self.Filters()[i].value() });
            else if (type == 'hidden' && hiddenField)
                filters.push({ field: self.Filters()[i].field(), condition: self.Filters()[i].condition(), value: self.Filters()[i].value() });
            else if (type == 'normal' && !relField && !hiddenField)
                filters.push({ field: self.Filters()[i].field(), condition: self.Filters()[i].condition(), value: self.Filters()[i].value() });            
        }
        return filters;
    };

    self.clearFilters = function () {
        self.Filters([]);
        if (self.Columns().length > 0) {
            //self.Filters.push(new GridFilterItemViewModel(self.Columns()));
            self.Filters.push(new GridFilterItemViewModel(self.Columns(),0));
        }
    };

    self.setColumns = function (columns, selectedColumnName, selectedColumnValue) {
        self.Columns(columns);
        self.Filters([]);
        if (selectedColumnName) {
            var ix = -1;
            $.each(self.Columns(), function (cix, ci) {
                if (ci.datafield == selectedColumnName) {
                    ix = cix;
                }
            });
            if (ix >= 0) {
                self.Filters.push(new GridFilterItemViewModel(columns, ix, selectedColumnValue));
            }
            else {
                self.Filters.push(new GridFilterItemViewModel(columns));
            }
        }
        else {
            self.Filters.push(new GridFilterItemViewModel(columns));
        }
    };

    self.onEnter = function (d, e) {
        if (e.keyCode === 13)
        {
            if (self.FilterCallback) self.FilterCallback();            
        }
        return true;
    };

    if (self.Columns().length > 0) {
        //self.Filters.push(new GridFilterItemViewModel(columns));
        self.Filters.push(new GridFilterItemViewModel(columns, 0));
    }
    
    return self;
}

var IssueViewModel = function () {
    var self = this;
    self.comments = ko.observableArray();
    self.newMessage = ko.observable();
    self.newMessageType = ko.observable();
    self.newMessageVisibility = ko.observable();
    self.error = ko.observable();      
    self.ProcessingCount = ko.observable(0);
    
    self.IsProcessing = ko.computed(function () {
        return (self.ProcessingCount() != 0);
    });

    self.tagIndex = -1;

    self.setIndex = function (data, event) {
       

        if (event.keyCode == 13) { //enter key
            if (self.tagSuggestions().length == 1) {
                var t = self.tagSuggestions()[0];
                t.addTag();                
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {
                var t = self.tagSuggestions()[self.tagIndex];
                t.addTag();                
                self.tagSuggestions([]);
                self.newTag('');
                return false;
            }
        }
        else if (event.keyCode == 40 || event.keyCode == 38) { //up & down arrows
            if (!self.tagSuggestionsPresent()) {
                return false;
            }
            if (self.tagIndex != -1) {

                self.tagSuggestions()[self.tagIndex].IsSelected(false);

                if (event.keyCode == 38 && self.tagIndex > 0) {
                    self.tagIndex--;
                }
                else if (event.keyCode == 40 && self.tagIndex < self.tagSuggestions().length) {
                    self.tagIndex++;
                }

            } else {
                self.tagIndex = 0;
            }

            if (self.tagIndex != -1) {
                self.tagSuggestions()[self.tagIndex].IsSelected(true);
            }
            return false;
        }
        else {
            self.tagIndex = -1;
            return true;
        }
    };

    self.pageSize = 25;
    self.startMatch = /@/ig; //new RegExp("@");
    self.wordMatch = /@(\w+)/ig; //new RegExp("@(\w+)");

    self.newComments = ko.observableArray();
    self.commentCounts = ko.observableArray();

    self.ShowAddCommentControls = ko.observable(CompanySettings.DisableCommunityPosting == 'false' || CompanySettings.DisableIssuePosting == 'false' || ComanySettings.DisableQuestionPosting);
    
    self.ObjectType = null;
    self.ObjectID = null;
    self.VisibilityID = null;

    self.FilterObjectType = null;
    self.FilterObjectID = null;

    
    var typeOps = [];

    var discussion = { Text: 'Discussion', Value: 2 };
    var issue = { Text: 'Issue', Value: 5 };

    self.typeEntryOptions = ko.observableArray(typeOps);
            
    self.selectedTypeFilterOption = ko.observable();

    self.tagSuggestions = ko.observableArray();
    self.tagSuggestionsPresent = ko.computed(function () {
        return (self.tagSuggestions().length > 0);
    }, self);

    self.newTag = ko.observable();
    self.tags = ko.observableArray();
    self.newTag.subscribe(function (value) {
        if (value) {
            $.getJSON('/api/tagsuggestions', { phrase: value }, function (suggestions) {
                // Object, ObjectID, TextPath, Url, ObjectTypeName
                var mappedSuggestions = $.map(suggestions, function (item) { return new CommentTagItem(item, self); });
                self.tagSuggestions(mappedSuggestions);
            });
        }
        else {
            self.tagSuggestions([]);
        }
    });

    self.addIssue = function () {
        self.ProcessingCount(self.ProcessingCount() + 1);
        self.error(null);
        if (self.newMessage() != '') {

            var commentModel = {
                ObjectType: self.ObjectType,
                ObjectID: self.ObjectID,
                Tags: [],
                Comment: {
                    Body: self.newMessage(),
                    CommentTypeID: 5,
                    VisibilityID: self.newMessageVisibility()
                }
            };

            self.tags().forEach(function (tag) {
                commentModel.Tags.push({ Object: tag.Object(), ObjectID: tag.ObjectID() });
            });

            $.ajax({
                data: commentModel,
                dataType: 'json',
                method: 'POST',
                url: '/services/community/comment'
            }).done(function (newCommentData, status, xhr) {                                
                self.ProcessingCount(self.ProcessingCount() - 1);
                amplify.publish("SaveAction", { context: 'issueform', action: "add", id: newCommentData.ID, custom: { CommentTypeID: self.newMessageType() } });                                
            }).fail(function (xhr, status, error) {
                self.ProcessingCount(self.ProcessingCount() - 1);
                self.error(status);
            });
        }
        else {
            self.ProcessingCount(self.ProcessingCount() - 1);
            self.error('Body may not be empty.');
        }
    };
    
    return self;
}

var promotionStepRelateActionViewModel = function (ruleID, ruleStepID, fusionID) {
    var self = this;
    self.IsLoading = ko.observable(false);

    self.ruleID = ruleID;
    self.ruleStepID = ruleStepID;
    self.fusionID = fusionID;

    self.IsLoading = ko.observable(false);

    // indexes
    self.selectedSubjectSearchTypeIndex = ko.observable(-1);
    self.selectedSubjectStepIndex = ko.observable(-1);

    self.selectedObjectSearchTypeIndex = ko.observable(-1);
    self.selectedObjectStepIndex = ko.observable(-1);

    self.selectedIntersectTypeIndex = ko.observable(-1);
    
    self.selectedObjectItemIndex = ko.observable(-1);
    self.selectedSubjectItemIndex = ko.observable(-1);
    self.selectedSubjectFusionOwnerRuleIndex = ko.observable(-1);
    self.selectedObjectFusionOwnerRuleIndex = ko.observable(-1);

        
    self.initialIntersectID = null;
    self.initialSubjectStep = null;
    self.initialObjectStep = null;
    self.initialSubjectItem = null;
    self.initialObjectItem = null;
    self.initialObjectOwnerRule = null;
    self.initialSubjectOwnerRule = null;

    // arrays
    self.searchTypes = ko.observableArray([            
            { value: "ResultFromStep", text: "Result From Step" },
            { value: "Self", text: "Self" },
            { value: "FusionOwner", text: "Fusion Owner Rule" },
    ]);

    self.intersectTypes = ko.observableArray();
    self.steps = ko.observableArray();
    self.subjectObjects = ko.observableArray();
    self.objectObjects = ko.observableArray();
    self.fusionOwnerRules = ko.observableArray();

    // computed
    self.showSubjectStepSearch = ko.computed(function () {
        return (self.selectedSubjectSearchTypeIndex() == 0);
    });
    
    self.showSubjectFusionOwnerSearch = ko.computed(function () {
        return (self.selectedSubjectSearchTypeIndex() == 2);
    });

    self.showObjectStepSearch = ko.computed(function () {
        return (self.selectedObjectSearchTypeIndex() == 0);
    });

    self.showObjectFusionOwnerSearch = ko.computed(function () {
        return (self.selectedObjectSearchTypeIndex() == 2);
    });

    // subscriptions

    self.selectedSubjectSearchTypeIndex.subscribe(function () {
        if (self.showSubjectStepSearch() && self.steps().length == 0) self.LoadSteps();
        if (self.showSubjectFusionOwnerSearch() && self.fusionOwnerRules().length == 0) self.LoadFusionOwnerRules();
    });

    self.selectedObjectSearchTypeIndex.subscribe(function () {
        if (self.showObjectStepSearch() && self.steps().length == 0) self.LoadSteps();
        if (self.showObjectFusionOwnerSearch() && self.fusionOwnerRules().length == 0) self.LoadFusionOwnerRules();
    });

    // methods
    self.Load = function () {        
        if (self.intersectTypes().length == 0) self.LoadIntersectTypes();        
    }

    self.LoadFusionOwnerRules = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/fusionOwnerRules/' + self.fusionID,
            async: true
        }).done(function (data) {
            self.fusionOwnerRules([]);
            $.each(data, function (idx, val) {
                self.fusionOwnerRules.push({ value: val.ID, text: val.FusionAttributeName + ' Owned By:' + val.OwnerObject });
                if (val.ID == self.initialSubjectOwnerRule) {
                    self.initialSubjectOwnerRule = '';
                    self.selectedSubjectFusionOwnerRuleIndex(idx);
                }
                if (val.ID == self.initialObjectOwnerRule) {
                    self.initialObjectOwnerRule = '';
                    self.selectedObjectFusionOwnerRuleIndex(idx);
                }                
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadSteps = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/' + self.ruleID + '/steps/' + self.ruleStepID,
            async: true
        }).done(function (data) {
            self.steps([]);
            $.each(data, function (idx, val) {
                self.steps.push({ value: val.ID, text: val.Description });                
                if(self.initialObjectStep == val.ID){
                    self.initialObjectStep = '';
                    self.selectedObjectStepIndex(idx);
                }
                if (self.initialSubjectStep == val.ID) {
                    self.initialSubjectStep = '';
                    self.selectedSubjectStepIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadIntersectTypes = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/relate/intersectTypes',
            async: true
        }).done(function (data) {
            self.intersectTypes([]);
            $.each(data, function (idx, val) {
                //object subject
                self.intersectTypes.push({ value: val.ID, text: val.Name, subject:val.Subject, subjectID:val.SubjectID, object:val.Object, objectID:val.ObjectID });
                if (self.initialIntersectID == val.ID) {
                    self.initialIntersectID = null;
                    self.selectedIntersectTypeIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.SelectedSearchType = function (name){
        for (var i = 0 ; i < self.searchTypes().length; i++) {
            if (self.searchTypes()[i].value.toUpperCase() == name.toUpperCase()) return i;
        }
        return -1;
    }

    self.SetInitialValues = function (subjectSearch, subject, subjectID, objectSearch, object, objectID, intersectTypeID) {
        self.selectedObjectSearchTypeIndex(self.SelectedSearchType(objectSearch));
        self.selectedSubjectSearchTypeIndex(self.SelectedSearchType(subjectSearch));
        self.initialIntersectID = intersectTypeID;
        if (objectSearch.toUpperCase() == 'RESULTFROMSTEP')
            self.initialObjectStep = objectID;        
        else if (objectSearch.toUpperCase() == 'FUSIONOWNER')
            self.initialObjectOwnerRule = objectID;
        if (subjectSearch.toUpperCase() == 'RESULTFROMSTEP')
            self.initialSubjectStep = subjectID;        
        else if (subjectSearch.toUpperCase() == 'FUSIONOWNER')
            self.initialSubjectOwnerRule = subjectID;
    }
}

var promotionStepLineageActionViewModel = function (ruleID, ruleStepID, fusionID) {
    var self = this;

    self.ruleID = ruleID;
    self.ruleStepID = ruleStepID;
    
    self.fusionID = fusionID;

    self.IsLoading = ko.observable(false);

    //indexes
    self.selectedIntersectTypeIndex = ko.observable(-1);
    self.selectedSubjectSearchTypeIndex = ko.observable(-1);
    self.selectedObjectSearchTypeIndex = ko.observable(-1);
    self.selectedFocalSearchTypeIndex = ko.observable(-1);

    self.selectedSubjectStepIndex = ko.observable(-1);
    self.selectedObjectStepIndex = ko.observable(-1);
    self.selectedFocalStepIndex = ko.observable(-1);
    self.selectedPredicateIndex = ko.observable(-1);
    self.selectedSubjectItemIndex = ko.observable(-1);
    self.selectedObjectItemIndex = ko.observable(-1);

    self.selectedFocalFusionOwnerRuleIndex = ko.observable(-1);
    self.selectedSubjectFusionOwnerRuleIndex = ko.observable(-1);
    self.selectedObjectFusionOwnerRuleIndex = ko.observable(-1);

    self.selectedSubjectType = ko.observable('');
    self.selectedSubjectTypeID = ko.observable(-1);
    self.selectedObjectType = ko.observable('');
    self.selectedObjectTypeID = ko.observable(-1);

    //arrays
    self.intersectTypes = ko.observableArray();

    self.searchTypes = ko.observableArray([        
            { value: "ResultFromStep", text: "Result From Step" },
            { value: "Self", text: "Self" },
            { value: "FusionOwner", text: "Fusion Owner Rule" },
            { value: "Direct", text: "Direct" }
    ]);

    self.steps = ko.observableArray();
    self.predicates = ko.observableArray();
    self.fusionOwnerRules = ko.observableArray();
    self.subjectObjects = ko.observableArray();
    self.objectObjects = ko.observableArray();

    //initial values
    self.initialIntersectID = '';
    self.initialFocalStep = '';
    self.initialSubjectStep = '';
    self.initialObjectStep = '';
    self.initialPredicate = '';
    self.initialSubjectOwnerRule = '';
    self.initialObjectOwnerRule = '';
    self.initialFocalOwnerRule = '';
    self.initialObjectItem = '';
    self.initialSubjectItem = '';
    // subscriptions

    self.selectedIntersectTypeIndex.subscribe(function () {
        //look at the source / target types use them if needed for direct
        if (self.selectedIntersectTypeIndex() <= 0) {
            self.selectedSubjectType('');
            self.selectedSubjectTypeID(-1);
            self.selectedObjectType('');
            self.selectedObjectTypeID(-1);
            return;
        }

        self.selectedSubjectType(self.intersectTypes()[self.selectedIntersectTypeIndex()].subject);
        self.selectedSubjectTypeID(self.intersectTypes()[self.selectedIntersectTypeIndex()].subjectID);
        self.selectedObjectType(self.intersectTypes()[self.selectedIntersectTypeIndex()].object);
        self.selectedObjectTypeID(self.intersectTypes()[self.selectedIntersectTypeIndex()].objectID);
    });

    self.selectedObjectTypeID.subscribe(function () {
        //if the object type is direct load the object drop down 
        if (self.selectedObjectTypeID() > 0) {
            self.LoadItems(self.selectedObjectTypeID(), self.selectedObjectType(), self.objectObjects, self.initialObjectItem, self.selectedObjectItemIndex);
        }
    })

    self.selectedSubjectTypeID.subscribe(function () {
        if (self.selectedSubjectTypeID() > 0) {            
            self.LoadItems(self.selectedSubjectTypeID(), self.selectedSubjectType(), self.subjectObjects, self.initialSubjectItem, self.selectedSubjectItemIndex);
        }
    })

    self.selectedFocalSearchTypeIndex.subscribe(function () {
        if (self.selectedFocalSearchTypeIndex() == 0 && self.steps().length == 0) self.LoadSteps();
        else if (self.selectedFocalSearchTypeIndex() == 2 && self.fusionOwnerRules().length == 0) self.LoadFusionOwnerRules();
    });

    self.selectedObjectSearchTypeIndex.subscribe(function () {
        if (self.selectedObjectSearchTypeIndex() == 0 && self.steps().length == 0) self.LoadSteps();
        else if (self.selectedObjectSearchTypeIndex() == 2 && self.fusionOwnerRules().length == 0) self.LoadFusionOwnerRules();
    });

    self.selectedSubjectSearchTypeIndex.subscribe(function () {
        if (self.selectedSubjectSearchTypeIndex() == 0 && self.steps().length == 0) self.LoadSteps();
        else if (self.selectedSubjectSearchTypeIndex() == 2 && self.fusionOwnerRules().length == 0) self.LoadFusionOwnerRules();
    });

    self.LoadIntersectTypes = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/relate/intersectTypes',
            async: true
        }).done(function (data) {
            self.intersectTypes([]);
            $.each(data, function (idx, val) {
                //object subject
                self.intersectTypes.push({ value: val.ID, text: val.Name, subject: val.Subject, subjectID: val.SubjectID, object: val.Object, objectID: val.ObjectID });
                if (self.initialIntersectID == val.ID) {
                    self.initialIntersectID = null;
                    self.selectedIntersectTypeIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadSteps = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/' + self.ruleID + '/steps/' + self.ruleStepID,
            async: true
        }).done(function (data) {
            self.steps([]);
            $.each(data, function (idx, val) {
                self.steps.push({ value: val.ID, text: val.Description });
                if (self.initialObjectStep == val.ID) {
                    self.initialObjectStep = '';
                    self.selectedObjectStepIndex(idx);
                }
                if (self.initialSubjectStep == val.ID) {
                    self.initialSubjectStep = '';
                    self.selectedSubjectStepIndex(idx);
                }
                if (self.initialFocalStep == val.ID) {
                    self.initialFocalStep = '';
                    self.selectedFocalStepIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadItems = function (id, type, array, initialItem, initialIndex) {
        var initialItemCombo = initialItem != '' ? (type + '|' + initialItem) : '';
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/directitems/' + type + '/' + id,
            async: true
        }).done(function (data) {
            array([]);
            $.each(data, function (idx, val) {
                array.push({ value: val.ID, text: val.Name });
                if (initialItemCombo == val.ID) {
                    initialItem = '';
                    initialIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }


    self.LoadPredicates = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/lineage/predicates',
            async: true
        }).done(function (data) {
            self.predicates([]);
            $.each(data, function (idx, val) {
                self.predicates.push({ value: val.ID, text: val.Name });
                if (self.initialPredicate == val.ID) {
                    self.initialPredicate = '';
                    self.selectedPredicateIndex(idx);
                }                
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadFusionOwnerRules = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/fusionOwnerRules/' + self.fusionID,
            async: true
        }).done(function (data) {
            self.fusionOwnerRules([]);
            $.each(data, function (idx, val) {
                self.fusionOwnerRules.push({ value: val.ID, text: val.FusionAttributeName + ' Owned By:' + val.OwnerObject });                
                if (val.ID == self.initialSubjectOwnerRule) {                    
                    self.initialSubjectOwnerRule = '';
                    self.selectedSubjectFusionOwnerRuleIndex(idx);
                }
                if (val.ID == self.initialObjectOwnerRule) {
                    self.initialObjectOwnerRule = '';
                    self.selectedObjectFusionOwnerRuleIndex(idx);
                }
                if (val.ID == self.initialFocalOwnerRule) {
                    self.initialFocalOwnerRule = '';
                    self.selectedFocalFusionOwnerRuleIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.Load = function () {
        self.LoadIntersectTypes();
        self.LoadPredicates();
    }

    self.SelectedSearchType = function (name) {
        for (var i = 0 ; i < self.searchTypes().length; i++) {
            if (self.searchTypes()[i].value.toUpperCase() == name.toUpperCase()) return i;
        }
        return -1;
    }

    self.SetInitialValues = function (focalSearch, focal, focalID, subjectSearch, subject, subjectID, objectSearch, object, objectID, intersectTypeID, predicate) {
        self.selectedObjectSearchTypeIndex(self.SelectedSearchType(objectSearch));
        self.selectedSubjectSearchTypeIndex(self.SelectedSearchType(subjectSearch));
        self.selectedFocalSearchTypeIndex(self.SelectedSearchType(focalSearch));
        self.initialIntersectID = intersectTypeID;
        self.initialPredicate = predicate;
        if (objectSearch.toUpperCase() == 'RESULTFROMSTEP')
            self.initialObjectStep = objectID;
        else if (objectSearch.toUpperCase() == 'DIRECT')
            self.initialObjectItem = objectID;
        else if (objectSearch.toUpperCase() == 'FUSIONOWNER')
            self.initialObjectOwnerRule = objectID;
        if (subjectSearch.toUpperCase() == 'RESULTFROMSTEP')
            self.initialSubjectStep = subjectID;
        else if (subjectSearch.toUpperCase() == 'DIRECT')
            self.initialSubjectItem = subjectID;
        else if (subjectSearch.toUpperCase() == 'FUSIONOWNER')
            self.initialSubjectOwnerRule = subjectID;
        if (focalSearch.toUpperCase() == 'RESULTFROMSTEP')
            self.initialFocalStep = focalID;
        else if (focalSearch.toUpperCase() == 'DIRECT')
            self.initialFocalItem = focalID;
        else if (focalSearch.toUpperCase() == 'FUSIONOWNER')
            self.initialFocalOwnerRule = focalID;        
    }
}


var promotionStepPromoteActionViewModel = function (fusionID, fusionTypeID, ruleID, ruleStepID) {
    var self = this;

    self.fusionID = fusionID;
    self.fusionTypeID = fusionTypeID;

    self.ruleID = ruleID;
    self.ruleStepID = ruleStepID;

    self.IsLoading = ko.observable(false);

    self.searchTypes = ko.observableArray([
        { value: "Direct", text: "Direct" },
        { value: "ResultFromStep", text: "Result From Step" },
        { value: "FusionOwner", text: "Fusion Owner" },
    ]);

    self.promoteToItems = ko.observableArray();
    self.promoteToParents = ko.observableArray();
    self.fusionOwnerRules = ko.observableArray();

    self.promotionParentType = ko.observable(0);
    self.promoteToParentsObjectType = ko.observable("");

    self.steps = ko.observableArray();

    self.selectedSearchTypeIndex = ko.observable(-1);
    self.selectedPromoteToIndex = ko.observable(-1);
    self.selectedPromoteParentIndex = ko.observable(-1);
    self.selectedStepIndex = ko.observable(-1);
    self.selectedFusionOwnerIndex = ko.observable(-1);

    self.initialPromoteToValue = ko.observable("");
    self.initialPromoteParentDirectValue = ko.observable("");
    self.initialPromoteParentStepValue = ko.observable("");
    self.initialOwnerRule = '';

    self.SetInitialValues = function (promoteTo, searchType, searchTypeValue) {
        self.initialPromoteToValue = promoteTo;

        if (searchType.toUpperCase() == "DIRECT") {
            self.selectedSearchTypeIndex(0);
            self.initialPromoteParentDirectValue = searchTypeValue;
            //  self.LoadPromoteToParents();            
        }
        else if (searchType.toUpperCase() == "RESULTFROMSTEP") {
            self.selectedSearchTypeIndex(1);
            self.Loadsteps();
            self.initialPromoteParentStepValue = searchTypeValue;
        }
        else if (searchType.toUpperCase() == "FUSIONOWNER") {
            self.selectedSearchTypeIndex(2);            
            self.initialOwnerRule = searchTypeValue;
        }
    }

    // selected promote to option changed
    self.selectedPromoteToIndex.subscribe(function () {
        if (self.selectedPromoteToIndex() == -1) {
            return;
        }

        //check the data in the promote to box to see if the value has a parent
        var item = self.promoteToItems()[self.selectedPromoteToIndex()];
        
        if (item != null) {
            var vals = item.value.split('|');

            self.promoteToParentsObjectType(vals[0]);

            if (vals.length >= 2) {
                self.promotionParentType(vals[2]);
            }
            else {
                self.promotionParentType(0);
            }
        }
    })

    self.promotionParentType.subscribe(function () {
        if (self.promotionParentType() > 0) {
            self.LoadPromoteToParents();
        }
    })

    // search type selection changed direct / result of step
    self.selectedSearchTypeIndex.subscribe(function () {
        if (self.selectedSearchTypeIndex() == -1) {
            return
        }

        if (self.selectedSearchTypeIndex() == 0) { //direct            
            //   self.LoadPromoteToParents();
        }
        else if (self.selectedSearchTypeIndex() == 1 && self.steps().length == 0) { //result of step            
            self.Loadsteps();
        }
        else if (self.selectedSearchTypeIndex() == 2 && self.fusionOwnerRules().length == 0) {
            self.LoadFusionOwnerRules();
        }
    })

    self.Loadsteps = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/' + self.ruleID + '/steps/' + self.ruleStepID,
            async: true
        }).done(function (data) {
            self.steps([]);
            $.each(data, function (idx, val) {
                self.steps.push({ value: val.ID, text: val.Description });
                if (self.initialPromoteParentStepValue == val.ID) self.selectedStepIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    };

    self.LoadFusionOwnerRules = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/fusionOwnerRules/' + self.fusionID,
            async: true
        }).done(function (data) {
            self.fusionOwnerRules([]);
            $.each(data, function (idx, val) {
                self.fusionOwnerRules.push({ value: val.ID, text: val.FusionAttributeName + ' Owned By:' + val.OwnerObject });
                if (val.ID == self.initialOwnerRule) {
                    self.initialOwnerRule = '';
                    self.selectedFusionOwnerIndex(idx);
                }                
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }


    self.Load = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/' + self.fusionTypeID + '/configurations/' + self.fusionID + '/promotion/options',
            async: true
        }).done(function (data) {
            self.promoteToItems([]);
            $.each(data, function (idx, val) {
                var id = val.PromotionObjectType + '|' + val.PromotionObjectID + '|' + val.ParentObjectTypeID;
                self.promoteToItems.push({ value: id, text: val.Name });
                if (id == self.initialPromoteToValue) self.selectedPromoteToIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    };

    self.LoadPromoteToParents = function () {
        if (self.promotionParentType() == 0) return;
        self.IsLoading(true);

        var ot = self.promoteToParentsObjectType() == 'ArtifactType' ? 'Artifact' : self.promoteToParentsObjectType();
        $.ajax({
            url: '/api/' + ot + '/' + self.promotionParentType() + '/fieldlookup',
            async: true
        }).done(function (data) {
            self.promoteToParents([]);
            $.each(data, function (idx, val) {
                self.promoteToParents.push({ value: val.ID, text: val.Name });
                if (self.initialPromoteParentDirectValue == val.ID) self.selectedPromoteParentIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    };
}

var promotionStepFindActionViewModel = function (ruleID, ruleStepID, ruleObjectID, ruleObjectType, fusionID) {
    var self = this;
    self.IsLoading = ko.observable(false);

    self.ruleID = ruleID;
    self.ruleStepID = ruleStepID;
    
    self.ruleObjectID = ruleObjectID;
    self.ruleObjectType = ruleObjectType;

    self.fusionID = fusionID;

    self.searchTypes = ko.observableArray([
        { value: "Glossary", text: "Glossary" },
        { value: "ResultFromStep", text: "Result From Step" },
        { value: "FusionOwner", text: "Fusion Owner" },
        { value: "Fusion", text: "Fusion" },
    ]);

    self.findObjectTypes = ko.observableArray([
        { value: "ArtifactType", text: "Artifact" },
        { value: "TaxonomyType", text: "Model" }
    ]);


    self.findObjects = ko.observableArray();
    self.targetFields = ko.observableArray();
    self.sourceFields = ko.observableArray();
    self.fusionOwnerRules = ko.observableArray();
    self.steps = ko.observableArray();
    self.fusionAttributes = ko.observableArray();
    
    self.selectedFindSearchTypeIndex = ko.observable(-1);
    self.selectedFindObjectTypeIndex = ko.observable(-1);
    self.selectedFindObjectIndex = ko.observable(-1);
    self.selectedFindFieldIndex = ko.observable(-1);
    self.selectedFindStepIndex = ko.observable(-1);
    self.selectedTargetFieldIndex = ko.observable(-1);
    self.selectedFusionOwnerRuleIndex = ko.observable(-1);
    self.selectedFusionAttributeIndex = ko.observable(-1);
        
    self.resultFromStepParent = ko.observable(false);

    //initial values    
    self.initialFindStepValue = ko.observable("");
    self.initialFindObject = "";
    self.initialFindField = "";
    self.initialTargetField = "";
    self.initialOwnerRule = "";
    self.initialFusionAttribute = "";

    // computed    
    self.showFusionAttributeSearch = ko.computed(function () {
        return (self.selectedFindSearchTypeIndex() == 3);
    });

    self.showFusionAttributeSearch = ko.computed(function () {
        return (self.selectedFindSearchTypeIndex() == 3);
    });

    self.showFusionOwnerSearch = ko.computed(function () {
        return (self.selectedFindSearchTypeIndex() == 2);
    });

    self.showResultFromStepSearch = ko.computed(function () {
        return (self.selectedFindSearchTypeIndex() == 1);
    });

    self.showResultDirect = ko.computed(function () {
        return (self.selectedFindSearchTypeIndex() == 0);
    });

    self.SetInitialValues = function (searchType, objectType, objectID, filterField,targetField,findParent) {
        if (searchType.toUpperCase() == "GLOSSARY") {
            self.selectedFindSearchTypeIndex(0);
            self.initialFindObject = objectID;
            self.initialFindField = filterField;
            self.initialTargetField = targetField;            
            if (objectType.toUpperCase() == "ARTIFACTTYPE") {
                self.selectedFindObjectTypeIndex(0);
                self.LoadFindArtifactTypes();
            }
            else if (objectType.toUpperCase() == "TAXONOMYTYPE") {
                self.selectedFindObjectTypeIndex(1);
                self.LoadFindModels();
            }
        }
        else if (searchType.toUpperCase() == "RESULTFROMSTEP") {
            self.selectedFindSearchTypeIndex(1);
            self.LoadFindSteps();
            self.initialFindStepValue = objectID;            
            self.resultFromStepParent(findParent=='1');
        }
        else if (searchType.toUpperCase() == 'FUSIONOWNER') {
            self.selectedFindSearchTypeIndex(2);            
            self.initialOwnerRule = objectID;
        }
        else if (searchType.toUpperCase() == 'FUSION') {
            self.selectedFindSearchTypeIndex(3);
            self.initialFusionAttribute = objectID;
        }
    }

    self.selectedFindSearchTypeIndex.subscribe(function () {
        if (self.selectedFindSearchTypeIndex() == 1) { //result of step
            self.LoadFindSteps();
        }
        else if (self.selectedFindSearchTypeIndex() == 2) { // fusionOwnerRules
            self.LoadFusionOwnerRules();
        }
        else if (self.selectedFindSearchTypeIndex() == 3) { //fusion
            self.LoadFusionAttributes();
        }        
    })

    self.selectedFindObjectTypeIndex.subscribe(function () {        
        if (self.selectedFindObjectTypeIndex() == 0) {
            self.LoadFindArtifactTypes();
            //load artifacts
        }
        else if (self.selectedFindObjectTypeIndex() == 1) {
            //load models
            self.LoadFindModels();
        }        
    })

    self.selectedFindObjectIndex.subscribe(function () {
        if (self.selectedFindObjectIndex() == -1) {
            return;
        }
        var type = self.findObjectTypes()[self.selectedFindObjectTypeIndex()];
        var item = self.findObjects()[self.selectedFindObjectIndex()];
        self.LoadTargetFields(type.value, item.value);
    })

    self.LoadFusionAttributes = function () {
        self.IsLoading(true);
        $.ajax({
            url: 'api/fusion/rule/fusionattributetypes',
            async: true
        }).done(function (data) {
            self.fusionAttributes([]);                        
            $.each(data, function (idx, val) {
                self.fusionAttributes.push({ value: val.ID, text: val.Name });
                if (val.ID == self.initialFusionAttribute) {
                    self.selectedFusionAttributeIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadSourceFields = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/fields/' + self.ruleObjectType + '/' + self.ruleObjectID + '.json',
            async: true
        }).done(function (data) {
            self.sourceFields([]);
            self.sourceFields.push({ value: '0', text: 'Name' });            
            if ('0' == self.initialFindField || self.initialFindField == '') self.selectedFindFieldIndex(0);
            $.each(data, function (idx, val) {
                self.sourceFields.push({ value: val.ID, text: val.FriendlyName });                
                if (val.ID == self.initialFindField) {                    
                    self.selectedFindFieldIndex(idx+1);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadFusionOwnerRules = function () {        
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/fusionOwnerRules/' + self.fusionID,
            async: true
        }).done(function (data) {
            self.fusionOwnerRules([]);            
            $.each(data, function (idx, val) {
                self.fusionOwnerRules.push({ value: val.ID, text: val.FusionAttributeName + ' Owned By:' + val.OwnerObject });                
                if (val.ID == self.initialOwnerRule) {                    
                    self.initialOwnerRule = '';
                    self.selectedFusionOwnerRuleIndex(idx);
                }
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadTargetFields = function (objectType, objectID) {
        self.IsLoading(true);
        $.ajax({
            url: '/fields/' + objectType + '/' + objectID + '.json',
            async: true
        }).done(function (data) {
            self.targetFields([]);
            self.targetFields.push({ value: '0', text: 'Name' });
            if ('0' == self.initialTargetField || self.initialTargetField == '') self.selectedTargetFieldIndex(0);
            $.each(data, function (idx, val) {
                self.targetFields.push({ value: val.ID, text: val.FriendlyName });
                if (val.ID == self.initialTargetField) self.selectedTargetFieldIndex(idx+1);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadFindModels = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/catalogs',
            async: true
        }).done(function (data) {
            self.findObjects([]);
            $.each(data, function (idx, val) {
                self.findObjects.push({ value: val.ID, text: val.Name });
                if (val.ID == self.initialFindObject) self.selectedFindObjectIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadFindArtifactTypes = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/artifacttypes?$orderby=Name',
            async: true
        }).done(function (data) {
            self.findObjects([]);
            $.each(data, function (idx, val) {
                self.findObjects.push({ value: val.ID, text: val.Name });
                if (val.ID == self.initialFindObject) self.selectedFindObjectIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.LoadFindSteps = function () {
        self.IsLoading(true);
        $.ajax({
            url: '/api/fusion/rule/' + self.ruleID + '/steps/' + self.ruleStepID,
            async: true
        }).done(function (data) {
            self.steps([]);
            $.each(data, function (idx, val) {
                self.steps.push({ value: val.ID, text: val.Description });
                if (self.initialFindStepValue == val.ID) self.selectedFindStepIndex(idx);
            })
        }).always(function () {
            self.IsLoading(false);
        });
    }

    self.Load = function () {
        self.LoadSourceFields();
    }
}

var promotionStepActionViewModel = function (fusionID, fusionTypeID, ruleID, ruleStepID, ruleObjectID, ruleObjectType) {
    var self = this;    
    self.description = ko.observable();
    self.IsLoading = ko.observable(false);

    self.fusionID = fusionID;
    self.fusionTypeID = fusionTypeID;

    self.ruleID = ruleID;
    self.ruleStepID = ruleStepID;

    self.ruleObjectID = ruleObjectID;
    self.ruleObjectType = ruleObjectType;
    
    self.actionTypes = ko.observableArray([
        { text: 'Promote', value: 'Promote' },
        { text: 'Find', value: 'Find' },
        { text: 'Lineage', value: 'Lineage' },
        { text: 'Relate', value: 'Relate' }
    ]);

    //settings for various actions
    self.actionRelateSettings = ko.observable(new promotionStepRelateActionViewModel(self.ruleID, self.ruleStepID, self.fusionID));
    self.actionPromoteSettings = ko.observable(new promotionStepPromoteActionViewModel(self.fusionID, self.fusionTypeID, self.ruleID, self.ruleStepID));
    self.actionFindSettings = ko.observable(new promotionStepFindActionViewModel(self.ruleID, self.ruleStepID, self.ruleObjectID, self.ruleObjectType, self.fusionID));
    self.actionLineageSettings = ko.observable(new promotionStepLineageActionViewModel(self.ruleID, self.ruleStepID, self.fusionID));
    

    self.selectedActionIndex = ko.observable(-1);

    //computed show values
    self.showRelateAction = ko.computed(function () {
        return (self.selectedActionIndex() == 3);
    });

    self.showPromoteAction = ko.computed(function () {
        return (self.selectedActionIndex() == 0);
    });

    self.showFindAction = ko.computed(function () {
        return (self.selectedActionIndex() == 1);
    });

    self.showLineageAction = ko.computed(function () {
        return (self.selectedActionIndex() == 2);
    });

    self.SetSelectedAction = function (val) {
        self.actionTypes().forEach(function (el, index) {            
            if (el.value.toUpperCase() == val.toUpperCase()){                
                self.selectedActionIndex(index);
                return;
            }
        });
    }

    // step actions promote / lineage / relate / find
    self.selectedActionIndex.subscribe(function () {
        if (self.selectedActionIndex() == -1)
            return;
        
        if (self.selectedActionIndex() == 0) { //promote            
            self.actionPromoteSettings().Load();
        }
        else if (self.selectedActionIndex() == 3) { //relate
            self.actionRelateSettings().Load();
        }
        else if (self.selectedActionIndex() == 1) { //find
            self.actionFindSettings().Load();
        }
        else if (self.selectedActionIndex() == 2) { //lineage
            self.actionLineageSettings().Load();
        }
    })    
}


//#endregion