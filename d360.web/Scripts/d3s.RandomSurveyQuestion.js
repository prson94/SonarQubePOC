(function ($) {

    amplify.request.define("RandomSurveyQuestion", "ajax", { url: '/api/surveys/{type}/{id}/randomquestion', type: 'GET' });
    amplify.request.define("SubmitRandomSurveyQuestion", "ajax", { url: '/api/surveys/randomquestion', type: 'POST' }); //{type}/{id}/

    var methods = {
        init: function (options) {
            var defaults = {
                objectType: null,
                objectID: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('RandomSurveyQuestion');

                $this.addClass("Question");

                if (!data) {

                    if (options.objectType && options.objectID) {
                        loadQuestion($this, options);
                    }

                    $(this).data('RandomSurveyQuestion', {
                        Target: $this,
                        Options: options
                    });

                }

                //$(window).bind('resize.tooltip', methods.someMethodName); //events with namespacing
            });
        },
        reload: function (objectType, objectID) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('RandomSurveyQuestion'),
                    options = data.Options;

                options.objectType = objectType;
                options.objectID = objectID;

                load($this, options);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('RandomSurveyQuestion');

                $this.removeData('RandomSurveyQuestion');
                //$(window).unbind('.tooltip');
            });
        }
    };

    $.fn.RandomSurveyQuestion = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.RandomSurveyQuestion');
        }

    };

    function loadQuestion($obj, options) {
        try {
            //return $obj.each(function () {
            var $this = $obj;//,//$(this),
                    //data = $this.data('RandomSurveyQuestion'),
                    //options = data.Options;

                if (options.objectType && options.objectID) {

                    //#region Report

                    $this.html('');
                    var okToAsk = true;
                    var storeValues = amplify.store("DateLastAskedSurveyQuestion");
                    if (storeValues) {
                        var fiveDaysAgo = moment().subtract('days', 2).calendar();
                        $(storeValues).each(function (idx, val) {
                            if (val.ObjectType == options.objectType && val.ObjectID == options.objectID && fiveDaysAgo >= val.Date) {
                                okToAsk = false;
                            }
                        });
                    }

                    if (okToAsk) {
                        amplify.request("RandomSurveyQuestion",
                            {
                                type: options.objectType,
                                id: options.objectID
                            },
                            function (data) {
                                if (data.Question) {

                                    $this.show();

                                    if (data.Question.Description) {
                                        $this.append("<header>" + data.Question.Description + "</header>");
                                    }
                                    else {
                                        $this.append("<header>How would you rate " + data.Question.Name + " for " + data.Question.ObjectName + "?</header>");
                                    }
                                    /*
                                    <form action="/artifacts/EditArtifact?id=7" data-ajax="true" data-ajax-begin="OnStarting" data-ajax-failure="OnFailed" data-ajax-method="POST" data-ajax-success="OnSuccess" data-ajax-url="/api/Artifacts/EditArtifact?id=7" method="post">
                                    */
                                    $this.append("<div id='RandomQuestionRating'></div>");
                                    $this.append("<div class='directions'>Optionally add a comment.</div>");
                                    $this.append("<div><textarea id='RandomQuestionComment'></textarea></div>");
                                    $this.append("<div><input type='button' id='SubmitRandomQuestion' value='Rate' /></div>");
                                    var c = data.Question.Option.length;
                                    $('#RandomQuestionRating').jqxRating({ theme: theme, count: c, itemHeight: 20, itemWidth: 20 });
                                    //$('#RandomQuestionComment').elastic();
                                    $("#SubmitRandomQuestion").jqxButton({ theme: theme, height: 25 });
                                    $("#SubmitRandomQuestion").click(function () {
                                        amplify.request("SubmitRandomSurveyQuestion",
                                            {
                                                ObjectType: options.objectType,
                                                ObjectID: options.objectID,
                                                QuestionTypeID: data.Question.ID,
                                                SurveyTypeID: data.Question.SurveyTypeID,
                                                Value: $('#RandomQuestionRating').jqxRating('getValue'),
                                                Comment: $('#RandomQuestionComment').val()
                                            },
                                            function (innerdata) {
                                                if (innerdata.Message == "Created") {
                                                    var store = amplify.store("DateLastAskedSurveyQuestion")
                                                    if (!store) {
                                                        store = [];
                                                    }
                                                    store.push({
                                                        ObjectType: options.objectType,
                                                        ObjectID: options.objectID,
                                                        Date: new Date()
                                                    });
                                                    amplify.store("DateLastAskedSurveyQuestion", store);
                                                    $this.fadeOut(500);
                                                    $this.hide();
                                                }
                                            }
                                        );
                                    });
                                    /*
                                    $.each(data.Question.Option, function (idx, value) {
        
                                        var html = "";
                                        html += "<div class='Chart'>";
                                        html += "<h1>" + value.Title + "</h1>";
                                        html += "<div class='Score'><h1>Score</h1><div class='" + scoreClass + "'>" + value.Score + "</div></div>";
                                        html += "<div class='Count'>" + value.TotalResponses + responseText + "</div>";
                                        html += "<div class='Graphic' id='Cht" + idx + "'></div>";
                                        html += "</div>";
        
                                        qsn.append(html);
        
                                    }); */
                                }
                            }
                        );
                    }
                    else {
                        $this.addClass("HiddenQuestion");
                    }

                    //#endregion
                }
                else {
                    $this.html('');
                }

                if ($this.html() == '') {
                    $this.hide();
                }
            //});
        } catch (e) {
            logError("RandomSurveyQuestion.js : loadQuestion", e);
        }
    }

})(jQuery);