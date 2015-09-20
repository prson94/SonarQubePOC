angular.module('d3s.search', ['ngSanitize', 'ngMaterial'])

.config(['$mdThemingProvider', '$mdIconProvider', function ($mdThemingProvider, $mdIconProvider) {

    $mdIconProvider.icon("menu", "../../content/svg/menu.svg", 24);
    
    $mdThemingProvider.definePalette('D3BLUE', { "50": "#eef1f2", "100": "#ccd4d7", "200": "#aab7bd", "300": "#8d9ea6", "400": "#708690", "500": "#546e7a", "600": "#4a606b", "700": "#3f535c", "800": "#35454c", "900": "#2a373d", "A100": "#ccd4d7", "A200": "#aab7bd", "A400": "#708690", "A700": "#3f535c" });
        $mdThemingProvider.theme('default')        .primaryPalette('D3BLUE');        
}]);


