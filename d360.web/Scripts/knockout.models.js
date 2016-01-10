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

    self.TemplateDownloadUrl = ko.observable("#");

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
            self.TemplateDownloadUrl('/form/Load_ExpectedColumns_ToExcel?type=' + typeInfo[0] + '&id=' + typeInfo[1]);
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
            amplify.publish("SaveAction", { context: data.context, action: 'add', id: data.id, custom: data.custom });
            amplify.publish("ShowMessage", data);
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

    self.cancel = function () {
        amplify.publish("CancelAction", { context: self.Context() });
    };

    self.loadCurrentIntersectType = function () {
        // Step 1
        $.getJSON('/form/IntersectType_Side1Options', function (relData) {
            self.Side1Options(relData);
        }).then(function () {
            // Step 2
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
                }
            );
        });
    };

    self.save = function () {
        self.InProgress(true);

        var postModel = {
            ID: self.ID(),
            Side1: self.Side1(),
            Side1DisplayText: self.Side1DisplayText(),
            Side2: self.Side2(),
            Side2DisplayText: self.Side2DisplayText()
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
    self.DisableQuestionPosting = ko.observable(data.DisableQuestionPosting);
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

    self.DisableQuestionPosting.subscribe(function (value) {
    });


    //#region Methods

    self.addIpRestriction = function () {
        self.IpRestrictions.push(new CompanySettingIpRestiction({}));
    };

    self.deleteIpRestriction = function () {
        self.IpRestrictions.remove(this);
    };

    self.loadCurrentSettings = function () {
        $.getJSON('/form/CompanySettings', function (relData) {
            self.CurrentCompanyIconPath(relData.CurrentCompanyIconPath);
            self.CurrentCompanyLogoPath(relData.CurrentCompanyLogoPath);
            self.DisableCommunityPosting(relData.DisableCommunityPosting);
            self.DisableIssuePosting(relData.DisableIssuePosting);
            self.DisableQuestionPosting(relData.DisableQuestionPosting);

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
        });
    };

    self.save = function () {
        self.InProgress(true);

        var postModel = {
            DisableCommunityPosting: self.DisableCommunityPosting(),
            DisableIssuePosting: self.DisableIssuePosting(),
            DisableQuestionPosting: self.DisableQuestionPosting(),
            SetLogoToDefault: self.SetLogoToDefault(),
            CompanyLogo: self.CompanyLogo().dataURL(),
            SetIconToDefault: self.SetIconToDefault(),
            CompanyIcon: self.CompanyIcon().dataURL(),
            ArtifactType_TaxonomyTypeID: self.ArtifactType_TaxonomyTypeID(),
            ArtifactType_TaxonomyTypeIDNodes: self.ArtifactType_TaxonomyTypeIDNodes(),
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
    self.newMessageVisibility = ko.observable();
    self.error = ko.observable();
    self.moreComments = ko.observable();
    self.searchFilter = ko.observable('');
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
    var question = { Text: 'Question', Value: 9 };

    if (CompanySettings.DisableCommunityPosting == 'false') {
        typeOps.push(discussion);
    }
    if (CompanySettings.DisableIssuePosting == 'false') {
        typeOps.push(issue);
    }
    if (CompanySettings.DisableQuestionPosting == 'false') {
        typeOps.push(question);
    }

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
        { Text: 'Questions', Value: 9 }
    ]);

    self.selectedDateFilterOption = ko.observable(-7);
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

            self.selectedDateFilterOption(-7);


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
                amplify.publish("SaveAction", { context: 'commentform', action: "add", id: newCommentData.ID, custom: {} })
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