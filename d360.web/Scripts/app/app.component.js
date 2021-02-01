"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d === decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var core_1 = require('@angular/core');
var index_1 = require('./services/index');
var right_sidebar_component_1 = require('./components/rightsidebar/right-sidebar.component');
require('rxjs/Rx');
var AppComponent = (function () {
    function AppComponent(pageHeader) {
        this.pageHeader = pageHeader;
    }
    AppComponent.prototype.ngOnInit = function () {
    };
    AppComponent.prototype.ngAfterViewInit = function () {
        this.initializeQtipTooltips(); // initialize qtips library for tooltips we use in the site it needs to be a global js function                           
    };
    AppComponent.prototype.initializeQtipTooltips = function () {
        $('body').on('mouseenter', '*[data-type]', function (event) {
            $(this).qtip({
                content: {
                    title: $(this).data('title'),
                    // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                    text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                    ajax: {
                        url: "/resources/" + $(this).data("type") + "/" + $(this).data("id") + "/templates/tooltip/" + $(this).data("context"),
                        once: ($(this).attr('data-cache') ? $(this).data('cache') : true),
                        success: function (data) {
                            if (!data || !data.length) {
                                this.destroy();
                            }
                            else {
                                this.set('content.text', data);
                            }
                        }
                    }
                },
                position: {
                    at: 'bottom center',
                    my: 'top center',
                    viewport: $(window),
                    effect: false // Disable positioning animation
                },
                overwrite: false,
                show: {
                    event: event.type,
                    solo: false,
                    ready: true
                },
                hide: {
                    fixed: true,
                    delay: 250,
                },
                style: {
                    classes: 'qtip-youtube qtip-rounded'
                }
            });
        });
    };
    __decorate([
        core_1.ViewChild(right_sidebar_component_1.RightSidebarComponent), 
        __metadata('design:type', right_sidebar_component_1.RightSidebarComponent)
    ], AppComponent.prototype, "rightSidebarComponent", void 0);
    AppComponent = __decorate([
        core_1.Component({
            selector: 'd3s-app',
            template: " <header>\n                    <d3s-header></d3s-header>\n                    <d3s-navbar></d3s-navbar>\n                </header>\n                <main>                                        \n                    <div class=\"row\">                         \n                        <div class=\"col s12\">            \n                            <div class=\"maincontent\">                                                                                                            \n                                <router-outlet></router-outlet>                                                \n                            </div>  \n                        </div>                                                \n                    </div>                    \n                    <d3s-right-sidebar [titleHeight]=\"0\"></d3s-right-sidebar>                        \n                </main>\n                <d3s-messages></d3s-messages>\n              ",
            providers: [index_1.HeaderActionsService, index_1.HeaderBreadcrumbService, index_1.MessagesService, index_1.PageHeader, index_1.RightSidebarService, index_1.WebAnalyticsService, index_1.StateService]
        }), 
        __metadata('design:paramtypes', [index_1.PageHeader])
    ], AppComponent);
    return AppComponent;
}());
exports.AppComponent = AppComponent;
//# sourceMappingURL=app.component.js.map