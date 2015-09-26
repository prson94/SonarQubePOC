//#region    BINDINGS
ko.bindingHandlers.htmlarea = {
    init: function (element, valueAccessor) {
        var value = valueAccessor();

        // We only want Redactor to notify our value of changes if the value
        // is an observable (rather than a string, say).

        if (ko.isObservable(value)) {
            $(element).redactor({
                changeCallback: value
            });
        }

    },
    update: function(element, valueAccessor) {
        // New value, note that Redactor expects the argument passed to 'set'
        // to have toString method, which is why we disjoin with ''.

//        var value = ko.utils.unwrapObservable(valueAccessor()) || '';

        // We only call 'set' if the content has changed, as we only need to
        // to do so then, and 'set' also resets the cursor position, which
        // we don't want happening all the time.

        // This code would work with Redactor 9, but no longer works with Redactor 10
        //if (value !== $(element).redactor('get')) {
        //    $(element).redactor('set', value);
        //}

        // The API method has become 'code.get', and it behaves a bit differently: it
        // returns formatted HTML, i.e. with whitespace and EOLs.  That means that we
        // would update the Redactor content every time the observable changed, which
        // was bad.  So instead we can use this:
 //       if (value !== $(element).redactor('core.getTextarea').val()) {
 //           $(element).redactor('code.set', value);
 //       }
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

ko.bindingHandlers.intersectTypeSide1FilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Side1(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};
ko.bindingHandlers.intersectTypeSide2FilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Side2(event.args.item.value);
        });
    },
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) { }
};
ko.bindingHandlers.intersectTypeRoleFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.RoleID(event.args.item.value);
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
ko.bindingHandlers.typeFilteredDropdown = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        $(element).on('change', function (event) {
            viewModel.Type(event.args.item.value);
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
                if (fileData[property]) {
                    fileData[property](e.target.result);
                }
                if (method == 'readAsDataURL' && fileData.base64String && ko.isObservable(fileData.base64String)) {
                    var resultParts = e.target.result.split(",");
                    if (resultParts.length === 2) {
                        fileData.base64String(resultParts[1]);
                    }
                }
            };

            reader[method](file);
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

//#region    BASE MODELS
function CommentItem(data) {//, hub) {
    var self = this;
    data = data || {};
    self.ID = ko.observable(data.ID);
    self.Body = ko.observable(data.Body);
    self.CreatingResourceID = ko.observable(data.CreatingResourceID || 0);
    self.CommentTypeID = ko.observable(data.CommentTypeID || 0);
    self.DateCreated = data.DateCreated;
    self.ObjectID = ko.observable(data.ObjectID || 0);
    self.ObjectType = ko.observable(data.ObjectType || "");
    self.ParentID = ko.observable(data.ParentID || null);
    self.ResourceName = ko.observable(data.ResourceName || "");
    self.ResourceEmail = ko.observable(data.ResourceEmail || "");
    self.ObjectName = ko.observable(data.ObjectName || "");
    self.ObjectUrl = ko.observable(data.ObjectUrl || "");
    self.CommentType = ko.observable(data.CommentType || "");

    self.isVisible = ko.observable(true);
    self.error = ko.observable();
    self.Comments = ko.observableArray();
    self.NewComments = ko.observableArray();
    self.newCommentMessage = ko.observable();

    self.ShowAddCommentControls = ko.observable(CompanySettings.DisableCommunityPosting == 'false');

    //self.hub = hub;

    self.getCommentType = function () {
        var commentType = "";

        switch (self.CommentTypeID) {
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

    self.isNonResourceComment = function () {
        return (self.ObjectType != 'Resource');
    };

    self.addComment = function () {
        if (self.newCommentMessage() != '') {
            $.ajax({
                data: {
                    ObjectType: self.ObjectType,
                    ObjectID: self.ObjectID,
                    Comment: {
                        Body: self.newCommentMessage(),
                        CommentTypeID: 2,
                        ParentID: self.ID
                    }
                },
                dataType: 'json',
                method: 'POST',
                url: '/services/community/comment'
            }).done(function (data, status, xhr) {
                self.Comments.push(new CommentItem(data));
                self.newCommentMessage('');
            }).fail(function (xhr, status, error) {
                self.error(status);
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
        var mappedPosts = $.map(data.Comments, function (item) { return new CommentItem(item); });//, self.hub
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

function LoadViewModel(data) {
    var self = this;
    data = data || {};

    //#region Simple Properties

    self.Context = ko.observable(data.Context);

    self.ActionIndex = ko.observable(data.ActionIndex || -1);
    self.Action = ko.observable(data.Action1);
    self.TypeIndex = ko.observable(data.TypeIndex || -1);
    self.Type = ko.observable(data.Type);
    self.Notes = ko.observable(data.Notes || "");

    self.File = ko.observable({
        file: ko.observable(), // will be filled with a File object
        // Read the files (all are optional, e.g: if you're certain that it is a text file, use only text:
        binaryString: ko.observable(), // FileReader.readAsBinaryString(Blob|File) - The result property will contain the file/blob's data as a binary string. Every byte is represented by an integer in the range [0..255].
        text: ko.observable(), // FileReader.readAsText(Blob|File, opt_encoding) - The result property will contain the file/blob's data as a text string. By default the string is decoded as 'UTF-8'. Use the optional encoding parameter can specify a different format.
        dataURL: ko.observable(), // FileReader.readAsDataURL(Blob|File) - The result property will contain the file/blob's data encoded as a data URL.
        arrayBuffer: ko.observable(), // FileReader.readAsArrayBuffer(Blob|File) - The result property will contain the file/blob's data as an ArrayBuffer object.

        // a special observable (optional)
        base64String: ko.observable(), // just the base64 string, without mime type or anything else
    });

    self.InProgress = ko.observable(false);

    //#endregion

    //#region Computed Properties

    self.ActionSelected = ko.pureComputed(function () {
        return (self.Action());
    }, self);

    self.ActionAndTypeSelected = ko.pureComputed(function () {
        return (self.Action() && self.Type());
    }, self);

    self.TypeOptionsLoading = ko.pureComputed(function () {
        return (self.TypeOptions().length == 0);
    }, self);

    //#endregion

    //#region List Properties

    self.ActionOptions = ko.observableArray([
        { title: 'Promotion', value: 'P' },
        { title: 'Relation', value: 'R' },
        { title: 'Unrelation', value: 'U' }
    ]);
    self.TypeOptions = ko.observableArray();
    self.Columns = ko.observableArray();

    //#endregion

    //self.File().dataURL.subscribe(function (dataURL) {
    //    console.log(dataURL);
    //});

    self.Action.subscribe(function (value) {
        self.TypeOptions.removeAll();
        if (value) {
            $.getJSON(
                '/form/Load_TypeOptions',
                { act: value },
                function (relData) {
                    self.TypeOptions(relData);

                    var indexToSelect = -1;

                    $.each(self.TypeOptions(), function (ix, item) {
                        if (item.value == self.Type()) {
                            indexToSelect = ix;
                        }
                    });
                    self.TypeIndex(indexToSelect);
                }
            );
            //self.ActionSelected(true);
        }
    });

    self.Type.subscribe(function (value) {
        if (value) {
            var typeInfo = value.split('|');
            $.getJSON(
                '/form/Load_ExpectedColumns',
                { type: typeInfo[0], id: typeInfo[1] },
                function (colData) {
                    self.Columns(colData);
                }
            );
        }
    });

    //#region Methods

    self.cancel = function () {
        amplify.publish("CancelAction", { context: self.Context() });
    };

    self.save = function () {
        self.InProgress(true);

        var postModel = {
            Action: self.Action(),
            Type: self.Type(),
            Notes: self.Notes(),
            File: self.File().dataURL()
        }

        $.ajax('/form/AddLoadFile', {
            data: postModel,
            dataType: 'json',
            method: 'post'
        }).done(function (data, status, xhr) {
            amplify.publish("SaveAction", { context: self.Context(), action: 'add', id: 0, custom: {} });
            amplify.publish("ShowMessage", { type: "confirm", title: "Success!", message: 'Mappings successfully created.' });
        }).fail(function (xhr, status, error) {
            amplify.publish("ShowMessage", { type: "error", title: "Error!", message: error });
        }).always(function (data, status, error) {
            self.InProgress(false);
        });
    };

    //#endregion

    return self;
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

function SourceToTargetDropdownOption(data) {
    var self = this;
    data = data || {};
    self.Value = data.Value;
    self.Text = data.Text;
    return self;
}

function SourceToTargetItem(data, parent) {
    var self = this;
    data = data || {};

    //#region KO properties

    self.SourceSystemIndex = ko.observable(-1);
    self.SourceSystem = ko.observable(data.SourceSystem);

    self.SourceObjectIndex = ko.observable(-1);
    self.SourceObject = ko.observable(data.SourceObject || "");

    self.SourceFusionAttributeIndex = ko.observable(-1);
    self.SourceFusionAttribute = ko.observable(data.SourceFusionAttribute || "");

    self.TargetSystemIndex = ko.observable(-1);
    self.TargetSystem = ko.observable(data.TargetSystem);

    self.TargetObjectIndex = ko.observable(-1);
    self.TargetObject = ko.observable(data.TargetObject || "");

    self.TargetFusionAttributeIndex = ko.observable(-1);
    self.TargetFusionAttribute = ko.observable(data.TargetFusionAttribute || "");

    //#endregion

    //#region KO Lists

    self.Contexts = ko.observableArray();

    self.SourceObjectOptions = ko.observableArray();
    self.SourceFusionAttributeOptions = ko.observableArray();
    self.TargetObjectOptions = ko.observableArray();
    self.TargetFusionAttributeOptions = ko.observableArray();

    //#endregion

    self.SourceSystem.subscribe(function (value) {
        self.SourceObjectOptions.removeAll();
        self.SourceFusionAttributeOptions.removeAll();
        if (value) {
            $.getJSON(
                '/form/SourceToTarget_SourcingObjectOptions',
                { type: 'Artifact', id: value },
                function (relData) {
                    //var options = $.map(relData, function (item) { return new SourceToTargetDropdownOption({ Text: item.group + " : " + item.title, Value: item.value }); });
                    self.SourceObjectOptions(relData);
                    var indexToSelect = -1;
                    $.each(relData, function (ix, item) {
                        if (item.value.indexOf(parent.Object() + '|' + parent.ObjectID()) > -1) {
                            indexToSelect = ix;
                        }
                    });
                    self.SourceObjectIndex(indexToSelect);
                }
            );

            $.getJSON(
                '/form/SourceToTarget_SourcingAttributeOptions',
                { type: 'Artifact', id: value },
                function (relData) {
                    //var options = $.map(relData, function (item) { return new SourceToTargetDropdownOption({ Text: item.group + " : " + item.title, Value: item.value }); });
                    self.SourceFusionAttributeOptions(relData);
                }
            );
        }
    });

    self.TargetSystem.subscribe(function (value) {
        self.TargetObjectOptions.removeAll();
        self.TargetFusionAttributeOptions.removeAll();

        if (value) {
            $.getJSON(
                '/form/SourceToTarget_SourcingObjectOptions',
                { type: 'Artifact', id: value },
                function (relData) {
                    //var options = $.map(relData, function (item) { return new SourceToTargetDropdownOption({ Text: item.group + " : " + item.title, Value: item.value }); });
                    self.TargetObjectOptions(relData);
                    var indexToSelect = -1;
                    $.each(relData, function (ix, item) {
                        if (item.value.indexOf(parent.Object() + '|' + parent.ObjectID()) > -1) {
                            indexToSelect = ix;
                        }
                    });
                    self.TargetObjectIndex(indexToSelect);
                }
            );

            $.getJSON(
                '/form/SourceToTarget_SourcingAttributeOptions',
                { type: 'Artifact', id: value },
                function (relData) {
                    //var options = $.map(relData, function (item) { return new SourceToTargetDropdownOption({ Text: item.group + " : " + item.title, Value: item.value }); });
                    self.TargetFusionAttributeOptions(relData);
                }
            );
        }
    });

    return self;
}

function SourceToTargetEnvironment(data) {
    var self = this;
    data = data || {};
    self.Object = ko.observable(data.Object);
    self.Group = ko.observable(data.Group || "");
    self.Timing = ko.observable(data.Timing || "");

    return self;
}

function SourceToTargetGroup(data, root) {
    var self = this;
    data = data || {};
    //self.Name = ko.observable(data.Name);
    self.Formula = ko.observable(data.Formula || "");
    self.Definition = ko.observable(data.Definition || "");
    self.Items = ko.observableArray();

    self.Systems = ko.observableArray(root.Relationships());

    self.addItem = function () {
        self.Items.push(
            new SourceToTargetItem({
                SourceSystem: '',
                SourceObject: '',
                SourceFusionAttribute: 0,
                TargetSystem: '',
                TargetObject: '',
                TargetFusionAttribute: 0
            }, root)
        );
    };

    self.deleteItem = function () {
        self.Items.remove(this);
    };

    return self;
}

function SourceToTargetRelationship(data) {
    var self = this;
    data = data || {};
    self.Object = ko.observable(data.Object);
    self.ObjectID = ko.observable(data.ObjectID);
    self.ObjectName = ko.observable(data.ObjectName);
}

function SourceToTargetViewModel(data) {
    var self = this;
    data = data || {};

    //#region Simple Properties

    self.Object = ko.observable(data.Object);
    self.ObjectID = ko.observable(data.ObjectID);
    self.ObjectName = ko.observable(data.ObjectName);
    self.Context = ko.observable(data.Context);
    self.InProgress = ko.observable(false);

    //#endregion

    //#region Computed Properties

    self.Step1Title = ko.pureComputed(function () {
        return 'Define Relationships for ' + self.ObjectName();
    }, self);

    self.GroupsNotPresent = ko.pureComputed(function () {
        if (self.Groups().length > 0) {
            var isBad = false;

            for (var g = 0; g < self.Groups().length; g++) {
                if (self.Groups()[g].Items().length > 0) {
                    for (var i = 0; i < self.Groups()[g].Items().length; i++) {
                        if (self.Groups()[g].Items()[i].SourceSystemIndex() == -1 || self.Groups()[g].Items()[i].TargetSystemIndex() == -1) {
                            isBad = true;
                        }
                    }
                }
            }

            return isBad;
        }

        return true;   //If you got this far, then no proper rows present.
    }, self);

    self.RelationshipOptionsLoading = ko.pureComputed(function () {
        return self.RelationshipOptions().length == 0;
    }, self);

    self.RelationshipsLoaded = ko.pureComputed(function () {
        return self.Relationships().length > 0;
    }, self);

    //#endregion

    //#region List Properties

    self.Environments = ko.observableArray();
    self.Groups = ko.observableArray();
    self.Relationships = ko.observableArray();

    self.RelationshipOptions = ko.observableArray();

    //#endregion

    //#region Methods

    self.addEnvironment = function () {
        self.Environments.push(
            new SourceToTargetEnvironment({
                Object: '',
                Group: '',
                Timing: ''
            })
        );
    };

    self.addGroup = function () {
       // var newGroupName = 'Group ' + (self.Groups().length + 1);
        var group = new SourceToTargetGroup({
           // Name: newGroupName,
            Formula: '',
            Definition: 'The business definition'// for ' + newGroupName
        }, self);

        group.addItem(new SourceToTargetItem({}, group));

        self.Groups.push(group);
    };

    self.addRelationship = function (data) {
        self.Relationships.push(
            new SourceToTargetRelationship({
                Object: data.Object,
                ObjectID: data.ObjectID,
                ObjectName: data.ObjectName
            })
        );
    };

    self.cancel = function () {
        amplify.publish("CancelAction", { context: self.Context() });
    };

    self.deleteGroup = function () {
        self.Groups.remove(this);
    };

    self.deleteEnvironment = function () {
        self.Environments.remove(this);
    };

    self.deleteRelationship = function (data) {
        self.Relationships.remove(function(item){ item.ObjectID == data.ObjectID });
    };

    self.getRelationshipOptions = function () {
        $.getJSON('/form/SourceToTarget_Step1', function (relData) {
            self.RelationshipOptions(relData);
        });
    };

    self.save = function () {
        self.InProgress(true);

        var postModel = {
            Object: self.Object(),
            ObjectID: self.ObjectID(),
            Relationships: [],
            Groups: [],
            Environments: []
        }

        for (var r = 0; r < self.Relationships().length; r++) {
            var relationship = {
                Object: self.Relationships()[r].Object(),
                ObjectID: self.Relationships()[r].ObjectID()
            };
            postModel.Relationships.push(relationship);
        }

        for (var g = 0; g < self.Groups().length; g++) {
            var group = {
                Formula: self.Groups()[g].Formula(),
                Definition: self.Groups()[g].Definition(),
                Items: []
            };

            for (var i = 0; i < self.Groups()[g].Items().length; i++) {
                var item = {
                    SourceSystem: self.Groups()[g].Items()[i].SourceSystem,
                    SourceObject: self.Groups()[g].Items()[i].SourceObject,
                    SourceFusionAttribute: self.Groups()[g].Items()[i].SourceFusionAttribute,
                    TargetSystem: self.Groups()[g].Items()[i].TargetSystem,
                    TargetObject: self.Groups()[g].Items()[i].TargetObject,
                    TargetFusionAttribute: self.Groups()[g].Items()[i].TargetFusionAttribute
                };
                group.Items.push(item);
            }

            postModel.Groups.push(group);
        }

        $.ajax('/form/AddSourceToTarget', {
            data: postModel,
            dataType: 'json',
            method: 'POST'
        }).done(function (data, status, xhr) {
            amplify.publish("SaveAction", { context: self.Context(), action: 'add', id: 0, custom: {} });
            amplify.publish("ShowMessage", { type: "confirm", title: "Success!", message: 'Mappings successfully created.' });
        }).fail(function (xhr, status, error) {
            amplify.publish("ShowMessage", { type: "error", title: "Error!", message: error });
        }).always(function (data, status, error) {
            self.InProgress(false);
        });
    };

    //#endregion

    return self;
}


function IntersectTypeRole(data, root) {
    var self = this;
    data = data || {};

    self.RoleIndex = ko.observable(data.RoleIndex || -1);
    self.RoleID = ko.observable(data.RoleID || "");
    self.NewRoleName = ko.observable(data.NewRoleName || "");
    self.Side1Label = ko.observable(data.Side1Label || "");
    self.Side2Label = ko.observable(data.Side2Label || "");

    return self;
}

function IntersectTypeViewModel(data) {
    var self = this;
    data = data || {};

    //#region Simple Properties

    self.ID = ko.observable(data.ID);
    self.Context = ko.observable(data.Context);

    self.Side1Index = ko.observable(data.Side1Index || -1);
    self.Side1 = ko.observable(data.Side1);
    self.Side1DisplayText = ko.observable(((data.Side1DisplayText) ? data.Side1DisplayText : ""));

    self.Side2Index = ko.observable(data.Side2Index || -1);
    self.Side2 = ko.observable(data.Side2);
    self.Side2DisplayText = ko.observable(((data.Side2DisplayText) ? data.Side2DisplayText : ""));

    self.LimitedChangesOnly = ko.observable(data.LimitedChangesOnly || false);
    self.InProgress = ko.observable(false);

    //#endregion

    //#region Computed Properties

    self.TypesNotPresent = ko.pureComputed(function () {
        return (self.Side1OptionsLoading().length <= 0 || 
                self.Side2OptionsLoading().length <= 0);
    }, self);

    self.Side1OptionsLoading = ko.pureComputed(function () {
        return self.Side1Options().length == 0;
    }, self);

    self.Side2OptionsLoading = ko.pureComputed(function () {
        return self.Side2Options().length == 0;
    }, self);

    //#endregion

    //#region List Properties

    self.Roles = ko.observableArray();

    self.RoleOptions = ko.observableArray();
    self.Side1Options = ko.observableArray();
    self.Side2Options = ko.observableArray();

    //#endregion

    self.Side1.subscribe(function (value) {
        self.Side2Options.removeAll();
        if (value) {
            var values = value.split('|');
            var dataToServer;
            var side2Values;

            if (self.Side2()) {
                side2Values = self.Side2().split('|');
            }
            if (side2Values) {
                dataToServer = { type: values[0], id: values[1], side2Type: side2Values[0], side2ID: side2Values[1] };
            }
            else {
                dataToServer = { type: values[0], id: values[1] };
            }

            $.getJSON(
                '/form/IntersectType_Side2Options',
                dataToServer,
                function (relData) {
                    self.Side2Options(relData);

                    var indexToSelect = -1;

                    $.each(self.Side2Options(), function (ix, item) {
                        if (item.value == self.Side2()) {
                            indexToSelect = ix;
                        }
                    });
                    self.Side2Index(indexToSelect);
                }
            );
        }
    });


    //#region Methods

    self.addRole = function () {
        self.Roles.push(new IntersectTypeRole({}, self));
    };

    self.cancel = function () {
        amplify.publish("CancelAction", { context: self.Context() });
    };

    self.deleteRole = function () {
        self.Roles.remove(this);
    };

    self.loadCurrentIntersectType = function () {
        // Step 1
        $.getJSON('/form/IntersectType_Side1Options', function (relData) {
            self.Side1Options(relData);
        }).then(function(){
            // Step 2
            $.getJSON('/form/IntersectType_RoleOptions', function (relData) {
                self.RoleOptions(relData);
            }).then(function () {
                // Step 3
                $.getJSON(
                    '/form/IntersectType_FormData',
                    { id: self.ID() },
                    function (relData) {

                        //Side2 needs to be first, here.
                        self.Side2(relData.Side2);
                        self.Side2DisplayText(relData.Side2DisplayText);
                        self.Side1(relData.Side1);
                        self.Side1DisplayText(relData.Side1DisplayText);

                        self.LimitedChangesOnly(relData.LimitedChangesOnly);

                        var indexToSelect = -1;

                        $.each(self.Side1Options(), function (ix, item) {
                            if (item.value == relData.Side1) {
                                indexToSelect = ix;
                            }
                        });
                        self.Side1Index(indexToSelect);

                        $.each(relData.Roles, function (roIx, roItem) {
                            var roleIndexToSelect = -1;
                            $.each(self.RoleOptions(), function (ix, item) {
                                if (item.ID == roItem.RoleID) {
                                    roleIndexToSelect = ix;
                                }
                            });

                            self.Roles.push(
                                    new IntersectTypeRole({
                                        RoleIndex: roleIndexToSelect,
                                        RoleID: roItem.RoleID,
                                        Side1Label: roItem.Side1Label,
                                        Side2Label: roItem.Side2Label
                                    }, self)
                                );

                        });
                    }
                );
            });
        });
    };



    self.save = function () {
        self.InProgress(true);

        var postModel = {
            ID: self.ID(),
            Side1: self.Side1(),
            Side1DisplayText: self.Side1DisplayText(),
            Side2: self.Side2(),
            Side2DisplayText: self.Side2DisplayText(),
            Roles: []
        }

        for (var r = 0; r < self.Roles().length; r++) {
            var role = {
                RoleID: self.Roles()[r].RoleID(),
                NewRoleName: self.Roles()[r].NewRoleName(),
                Side1Label: self.Roles()[r].Side1Label(),
                Side2Label: self.Roles()[r].Side2Label()
            };
            postModel.Roles.push(role);
        }

        var uri = '';
        var method = '';
        if (postModel.ID == 0) {
            uri = '/form/AddIntersectType';
            method = 'POST';
        }
        else {
            uri = '/form/EditIntersectType';
            method = 'PUT';
        }

        $.ajax(uri, {
            data: postModel,
            dataType: 'json',
            method: method
        }).done(function (data, status, xhr) {
            amplify.publish("SaveAction", { context: self.Context(), action: 'add', id: 0, custom: {} });
            amplify.publish("ShowMessage", { type: "confirm", title: "Success!", message: 'Mappings successfully created.' });
        }).fail(function (xhr, status, error) {
            amplify.publish("ShowMessage", { type: "error", title: "Error!", message: error });
        }).always(function (data, status, error) {
            self.InProgress(false);
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

var ChildArtifactsMicroTileModel = function (type, id) {
    var self = this;

    self.Statistics = ko.observableArray();
    self.ObjectID = id;
    self.ObjectType = type;

    self.GetStatistics = function () {
        $.getJSON(
            '/api/' + self.ObjectType + '/' + self.ObjectID + '/artifacts/statistics',
            function (data) {
                var mappedItems = $.map(data, function (item) { return new ChildArtifactsMicroTileItem(self.ObjectID, item.Name, item.ID, item.Count); });
                self.Statistics(self.Statistics().concat(mappedItems));

                //self.Statistics().length

            }
        );
    }

    return self;
}
ChildArtifactsMicroTileModel.prototype = new BaseOverlayTileModel();

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

    self.GetStatistics = function () {
        $.getJSON(
            '/tiles/HomeSocial',
            function (data) {
                self.FollowerCount(data.FollowerCount);
                self.GroupCount(data.GroupCount);
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

var RedFlagSummaryMicroTileModel = function () {
    var self = this;

    self.OverlayName = 'RedFlags';

    self.Statistics = ko.observableArray();

    self.Overlay = $('<div id="RedFlagsOverlay" class="overlay"></div>');
    self.Overlay.appendTo('body');

    self.GetStatistics = function () {
        $.getJSON(
            '/api/resources/me/redflagsummaries',
            function (data) {
                var mappedItems = $.map(data, function (item) { return new RedFlagSummaryMicroTileItem(item); });
                self.Statistics(mappedItems); //self.Statistics().concat(mappedItems)
            }
        );
    }

    self.OpenOverlay = function (redflag) {
        self.Overlay.html('');
        self.Overlay.fadeIn(500);
        self.Overlay.load('/overlays/' + redflag.Type() + '/' + redflag.TypeID() + '/RedFlags');

        amplify.publish('OverlayOpening', { name: self.OverlayName });

        amplify.subscribe('OverlayOpening', function (data) {
            if (data.name != self.OverlayName) {
                self.Overlay.fadeOut(500);
                //$('body').remove(overlay, false);
            }
        });

        amplify.subscribe('OverlayClosing', function (data) {
            if (data.name = self.OverlayName) {
                self.Overlay.fadeOut(500);
                //$('body').remove(overlay, false);
            }
        });
    }

    amplify.subscribe("SaveAction", function (data) {
        try {
            switch (data.context) {
                case "AlertFlag":
                    self.GetStatistics();
                    break;
            }

        } catch (e) {
            logError("Detail", e);
        }
    });

    return self;
}

var SocialMicroTileModel = function (type, id) {
    var self = this;

    self.ObjectID = id;
    self.ObjectType = type;

    self.FollowerCount = ko.observable(0);
    self.CommentCount = ko.observable(0);
    self.CommentCountLast48Hours = ko.observable(0);

    self.commentsOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/comments';
    }, self);

    self.followersOverlayUri = ko.computed(function () {
        return '/overlays/' + self.ObjectType + '/' + self.ObjectID + '/followers';
    }, self);

    self.GetStatistics = function () {
        $.getJSON(
            '/api/' + self.ObjectType + '/' + self.ObjectID + '/social/statistics',
            function (data) {
                self.FollowerCount(data.FollowerCount);
                self.CommentCount(data.CommentCount);
                self.CommentCountLast48Hours(data.CommentCountLast48Hours);
            }
        );
    }

    return self;
}
SocialMicroTileModel.prototype = new BaseOverlayTileModel();

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

//#endregion

//#region VIEW MODELS

var PageViewModel = function (title, directions, breadcrumbs, type, id, redflagged) {
    var self = this;
    self.Title = title;
    self.Directions = directions;
    self.breadcrumbs = breadcrumbs;
    self.ObjectType = type || "";
    self.ObjectID = id || 0;
    self.RedFlagged = redflagged || false;
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

var BoardViewModel = function () {
    var self = this;
    self.comments = ko.observableArray();
    self.newMessage = ko.observable();
    self.newMessageType = ko.observable();
    self.error = ko.observable();
    self.moreComments = ko.observable();

    self.pageSize = 25;

    self.newComments = ko.observableArray();

    self.ShowAddCommentControls = ko.observable(CompanySettings.DisableCommunityPosting == 'false');

    self.ObjectType = null;
    self.ObjectID = null;

    self.FilterObjectType = null;
    self.FilterObjectID = null;

    self.dateFilterOptions = ko.observableArray([
        { Text: 'Last day', Value: -1 },
        { Text: 'Last week', Value: -7 },
        { Text: 'Last month', Value: -30 },
        { Text: 'All time', Value: 0 }
    ]);

    self.typeEntryOptions = ko.observableArray([
        //{ Text: 'Data Event', Value: 8 },
        { Text: 'Discussion', Value: 2 },
        //{ Text: 'Issue', Value: 5 },
        //{ Text: 'Task', Value: 6 },
        { Text: 'Question', Value: 9 }
    ]);

    self.typeFilterOptions = ko.observableArray([
        { Text: 'All types', Value: 0 },
        { Text: 'Data Events', Value: 8 },
        { Text: 'Discussions', Value: 2 },
        //{ Text: 'Governance', Value: 3 },
        //{ Text: 'Issues', Value: 5 },
        //{ Text: 'System Notifications', Value: 1 },
        { Text: 'Red Flag Alerts', Value: 7 },
        //{ Text: 'Relationships', Value: 4 },
        //{ Text: 'Tasks', Value: 6 },
        { Text: 'Questions', Value: 9 }
    ]);

    self.selectedDateFilterOption = ko.observable();
    self.selectedTypeFilterOption = ko.observable();

    self.clearFields = function () {
        self.newMessage('');
    }

    self.filterComments = function () {
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



            self.selectedDateFilterOption($.jqx.cookie.cookie("BoardDateFilterCookie"));


            self.getMoreComments();
        }
        catch (e) {
            console.log(e);
        }
    };

    self.getMoreComments = function () {
        $.ajax({
            data: {
                "ObjectType": self.ObjectType,
                "ObjectID": self.ObjectID,
                "Skip": self.comments().length,
                "Take": self.pageSize,
                "DateFilter": self.selectedDateFilterOption(),
                "TypeFilter": self.selectedTypeFilterOption()
            },
            dataType: 'json',
            method: 'POST',
            url: '/services/community/comments'
        }).done(function (commentData, status, xhr) {
            var mappedPosts = $.map(commentData, function (item) { return new CommentItem(item); }); //, self.hub
            self.comments(self.comments().concat(mappedPosts));
            self.moreComments(mappedPosts.length >= self.pageSize);

            if (self.FilterObjectType && self.FilterObjectID) {
                self.setCommentsFilter(self.FilterObjectType, self.FilterObjectID);
            }
        }).fail(function (xhr, status, error) {
            self.error(status);
        });
    };

    self.addComment = function () {
        self.error(null);
        if (self.newMessage() != '') {
            $.ajax({
                data: {
                    ObjectType: self.ObjectType,
                    ObjectID: self.ObjectID,
                    Comment: {
                        Body: self.newMessage(),
                        CommentTypeID: self.newMessageType()
                    }
                },
                dataType: 'json',
                method: 'POST',
                url: '/services/community/comment'
            }).done(function (newCommentData, status, xhr) {
                self.comments.unshift(new CommentItem(newCommentData));
                self.newMessage('');
                amplify.publish("SaveAction", { context: 'commentform', action: "add", id: newCommentData.ID, custom: {} })
            }).fail(function (xhr, status, error) {
                self.error(status);
            });
        }
        else {
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

//#endregion