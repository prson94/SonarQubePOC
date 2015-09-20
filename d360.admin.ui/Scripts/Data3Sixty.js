var Data3Sixty = angular.module('Data3Sixty', ['ngRoute']);

Data3Sixty.controller('HomepageController', HomepageController);
//Data3Sixty.controller('LoginController', LoginController);

Data3Sixty.factory('AuthHttpResponseInterceptor', AuthHttpResponseInterceptor);

var configFunction = function ($routeProvider, $httpProvider) {
    $routeProvider.
        when('/routeOne', {
            templateUrl: 'Pages/One'
        })
        .when('/routeTwo/:id', {
            templateUrl: function (params) { return 'Pages/Two?id=' + params.id; }
        })
        .when('/routeThree', {
            templateUrl: 'Pages/Three'
        });

    $httpProvider.interceptors.push('AuthHttpResponseInterceptor');
}
configFunction.$inject = ['$routeProvider', '$httpProvider'];

Data3Sixty.config(configFunction);