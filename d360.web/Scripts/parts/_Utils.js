function gridExists(controlID) {
    try {
        var state = $(controlID).jqxGrid('getstate');
        return (state !== null);
    } catch (e) {
        return false;
    }
}
function PermissionsModel() {
    var self = this;

    self.permissions = [];

    self.GetPermissionsForObject = function (type, id) {
        var pr = new $.Deferred();

        $.ajax({
            url: '/api/' + type + '/' + id + '/permissions',
            method: 'GET',
            success: function (data, status, xhr) {
                self.permissions = data;
            },
            error: function (xhr, status, error) {
                self.permissions = [];
            },
            complete: function (xhr, status) {
                amplify.publish('PermissionsLoaded', { Type: type, ID: id });
                pr.resolve();
            }
        });

        return pr.promise();
    }

    self.HasPermission = function (claimObject, claim) {
        var has = false;
        for (var i = 0; i < self.permissions.length; i++) {
            var p = self.permissions[i];
            if (p.ClaimObject === claimObject && p.Claim === claim) {
                has = true;
                break;
            }
        }
        return has;
    }
}

function drawKpi(controlID, title, total, available, isPercentage) {
    var data = [];
    data.push({ text: 'Currently', value: total }); // current
    data.push({ text: 'Available', value: available }); // remaining
    var settings = {
        title: title,
        description: '',
        enableAnimations: true,
        showLegend: false,
        showBorderLine: false,
        backgroundColor: '#ffffff',
        padding: { left: 1, top: 1, right: 1, bottom: 1 },
        titlePadding: { left: 0, top: 0, right: 0, bottom: 0 },
        source: data,
        showToolTips: false,
        seriesGroups:
        [
            {
                type: 'donut',
                useGradientColors: false,
                series:
                    [
                        {
                            showLabels: false,
                            enableSelection: false,
                            displayText: 'text',
                            dataField: 'value',
                            labelRadius: 120,
                            initialAngle: 90,
                            radius: 60,
                            innerRadius: 50,
                            centerOffset: 0
                        }
                    ]
            }
        ]
    };

    settings.drawBefore = function (renderer, rect) {
        var text = ((total === null) ? '-' : total + (isPercentage ? "%" : ""));
        sz = renderer.measureText(text, 0, { 'class': 'kpi-inner-text' });

        renderer.text(
                text,
                rect.x + (rect.width - sz.width) / 2,
                rect.y + rect.height / 2,
                0,
                0,
                0,
                { 'class': 'kpi-inner-text' }
            );
    }
    $(controlID).jqxChart(settings);
    $(controlID).jqxChart('addColorScheme', 'customColorScheme', ['#3f9d40', '#EDE6E7']);
    $(controlID).jqxChart({ colorScheme: 'customColorScheme' });
}

function TileTools(toolsControlID, tools) {
    $(toolsControlID).addClass('TileTools');
    $(toolsControlID).html('');

    var internalToolClick = function () {
        amplify.publish(AmplifyActions.InternalTool, { action: $(this).data("action") });
    }
    var toolClick = function () {
        amplify.publish(AmplifyActions.Tool, { uri: $(this).data("uri"), context: $(this).data("context") });
    }

    var unsubscribe = function () {
        $.each($(toolsControlID).find('i'), function () {
            $(this).off('click', toolClick);
        });
    }

    $.each(tools, function () {
        var tool = $("<a style='margin-left: 10px' class='btn-floating waves-effect waves-light brown lighten-1'><i class='fa fa-" + this.icon + "' title='" + this.title + "'></i></a>");
        if (this.action) {
            tool.data('action', this.action);
            tool.on('click', internalToolClick);
        }
        else {
            tool.data('uri', this.uri);
            tool.data('context', this.context);
            tool.on('click', toolClick);
        }
        $(toolsControlID).append(tool);
    });

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
}