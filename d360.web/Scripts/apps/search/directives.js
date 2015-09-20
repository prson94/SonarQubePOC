
angular.module('d3s.search').directive('search', function () {
    return {
        restrict: 'E',
        scope: {
            searchTerm: '=value'
        },
        controller: function ($scope, $http, $rootScope) {
            
        },        
        template: '<input ng-model="searchTerm" placeholder="Search" size="50">'
                  
      //<input type="text" name="input" ng-model="searchTerm" placeholder="Search">'    
    };
});



angular.module('d3s.search').directive('category', function () {
    return {
        restrict: 'E',
        scope: {
            categories: '='
        },
        controller: function ($scope, $http, $rootScope) {

            /*            $scope.getFunds = function (val) {
                            return $http.get($rootScope.baseUrl + 'api/core/fund/startsWith', {
                                params: {
                                    startsWith: val
                                }
                            }).then(function (response) {
                                return response.data;
                            });
            
                        };*/
        },        
        template: '<md-list-item ng-click="navigateTo(setting.extraScreen, $event)" ng-repeat="setting in settings">' +
                        '<md-icon md-svg-icon="{{setting.icon}}"></md-icon>' +
                            '<p> {{ setting.name }} </p>' +
                        '<md-switch class="md-secondary" ng-model="setting.enabled"></md-switch>' +
                    '</md-list-item>'
    };
});


angular.module('d3s.search').directive('results', function () {
    return {
        restrict: 'E',
        scope: {
            results: '='
        },
        controller: function ($scope, $http, $rootScope) {

            /*            $scope.getFunds = function (val) {
                            return $http.get($rootScope.baseUrl + 'api/core/fund/startsWith', {
                                params: {
                                    startsWith: val
                                }
                            }).then(function (response) {
                                return response.data;
                            });
            
                        };*/
        },
        template: '<div>search results go here</div>'
        /* template: '<input type="text" ng-model="fund" placeholder="Portfolio" typeahead="fund as fund.Name for fund in getFunds($viewValue)" typeahead-loading="loadingLocations" class="form-control"  >' +
                     '<i ng-show="loadingLocations" class="glyphicon glyphicon-refresh"></i>'*/

    };
});