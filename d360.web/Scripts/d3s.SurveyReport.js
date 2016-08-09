/// <reference path="../scripts.js" />

(function ($) {

    amplify.request.define("SurveyTypeReport", "ajax", { url: '/api/surveys/{typeID}/{type}/{id}/report', type: 'GET' });

    var methods = {
        init: function (options) {
            var defaults = {
                surveyTypeID: null,
                objectType: null,
                objectID: null
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('SurveyReport');

                $this.addClass("Report");

                if (!data) {

                    $(this).data('SurveyReport', {
                        Target: $this,
                        Options: options
                    });

                    if (options.surveyTypeID && options.objectType && options.objectID) {
                        loadReport($this);
                    }

                }
            });
        },
        reload: function (surveyTypeID, objectType, objectID) {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('SurveyReport'),
                    options = data.Options;

                options.surveyTypeID = surveyTypeID;
                options.objectType = objectType;
                options.objectID = objectID;

                $(this).data('SurveyReport', {
                    Target: $this,
                    Options: options
                });

                loadReport($this);
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('SurveyReport');

                $this.removeData('SurveyReport');
            });
        }
    };

    $.fn.SurveyReport = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.SurveyReport');
        }

    };

    //#region Private Methods

    function loadReport($obj) {
        try {
            var data = $obj.data('SurveyReport'),
                options = data.Options;

            if (options.surveyTypeID && options.objectType && options.objectID) {
                //#region Report

                $obj.html('');

                amplify.request("SurveyTypeReport",
                    {
                        typeID: options.surveyTypeID,
                        type: options.objectType,
                        id: options.objectID
                    },
                    function (data) {
                        if (data) {
                            var row = $("<div class='row'></div>");
                            $obj.append(row);
                            $.each(data.Report.Charts.Chart, function (ix, item) {
                                row.append("<div class='col s4'><div id='Cht" + item.ID + "' style='width: 100%; height: 300px'></div></div>");
                            });



                            $.each(data.Report.Charts.Chart, function (idx, value) {
                                loadChart(value);
                            });
                        }
                    }
                );

                //#endregion
            }
            else {
                $obj.html('');
            }
        } catch (e) {
            logError("SurveyReport.js : loadReport", e);
        }
    }

    function loadChart(cht) {
        try {
            ////#region Score Class Decision
            //var scoreClass;
            //if (cht.Score <= 40) {
            //    scoreClass = "Low";
            //}
            //else if (cht.Score > 40 && cht.Score <= 80) {
            //    scoreClass = "Medium";
            //}
            //else {
            //    scoreClass = "High";
            //}
            ////#endregion
            //var responseText = " response";
            //if (cht.TotalResponses > 1) {
            //    responseText += "s";
            //}
            //var html = "";
            //html += "<div class='Chart'>";
            ////html += "<h1>" + cht.Title + "</h1>";
            //html += "<div class='Score'><h1>Score</h1><div class='" + scoreClass + "'>" + cht.Score + "</div></div>";
            //html += "<div class='Count'>" + cht.TotalResponses + responseText + "</div>";
            //html += "<div class='Graphic' id='Cht" + idx + "'></div>";
            //html += "</div>";

            //var mv = idx % 3;

            //if (mv == 0) {
            //    col1.append(html);
            //}
            //else if (mv == 1) {
            //    col2.append(html);
            //}
            //else {
            //    col3.append(html);
            //}

            //#region Build Chart

            if (cht.Results) {

                var src =
                {
                    localdata: cht.Results.Result,
                    datatype: "array",
                    datafields: [
                        { name: 'Name' },
                        { name: 'Value' }
                    ]
                };
                var dataAdapter = new $.jqx.dataAdapter(src, { async: false, autoBind: true });
                if (dataAdapter.totalrecords > 0) {
                    var settings = {
                        enableAnimations: true,
                        showBorderLine: false,
                        showLegend: true,
                        title: cht.Title,
                       // height: 200,
                        //width: 200,
                        source: dataAdapter,
                        colorScheme: 'scheme01',
                        seriesGroups:
                            [
                                {
                                    type: 'pie',
                                    showLabels: true,
                                    series:
                                        [
                                            {
                                                dataField: 'Value',
                                                displayText: 'Name',
                                                labelRadius: 100,
                                                initialAngle: 15,
                                                radius: 75,
                                                centerOffset: 0
                                            }
                                        ]
                                }
                            ]
                    };
                    $("#Cht" + cht.ID).jqxChart(settings);
                }
            }
            else {
                $("#Cht" + cht.ID).addClass("error").html("No data to display");
            }
            //#endregion
        } catch (e) {
            logError("SurveyReport.js : loadChart", e);
        }
    }

    //#endregion

})(jQuery);