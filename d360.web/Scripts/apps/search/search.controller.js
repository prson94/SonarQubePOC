angular.module('d3s.search').controller('SearchCtrl',
    ['$scope', '$http', '$mdSidenav', '$mdBottomSheet', '$q', '$mdDialog',
        function ($scope, $http, $mdSidenav, $mdBottomSheet, $q, $mdDialog) {

            $scope.phrase = "equity";
            $scope.results = null;
            $scope.categories = null;
            $scope.ellapsedTime = null;
            $scope.isSearching = false;
             
            $scope.toggleList = toggleUsersList;

            $scope.filters = {}; //filter for categories

            $scope.search = function () {
                $scope.isSearching = true;
                console.log("searchClick");
                $http.post('/search/results', { search: $scope.phrase }).
                  success(function (data, status, headers, config) {
                      $scope.isSearching = false;
                      console.log(data);

                      $scope.results = data.Results;
                      $scope.categories = data.Categories;
                      $scope.ellapsedTime = data.ElapsedTime;

                      console.log("categories " + $scope.categories);
                      console.log("results " + $scope.results);
                  }).
                  error(function (data, status, headers, config) {
                      $scope.isSearching = false;
                      console.log("search returned error code " + status);
                  });
            }

            $scope.getMatches = function (searchTerm) {
                return $http.get('/search/AutoComplete?search=' + searchTerm ).then(function (response) {
                    console.log(response.data);
                    return response.data; // usually response.data
                })
            }


            $scope.showDetails = function (ev,item) {                
                console.log("details item id: " + item.ID);
                
                //show dialog with them
                $mdDialog.show({
                    controller: SearchDetailsDialogCtrl,
                    templateUrl: 'detail.dialog.tmpl.html',
                    parent: angular.element(document.body),
                    targetEvent: ev,
                    locals: {
                        term: item
                    },
                });
                function SearchDetailsDialogCtrl($scope, $mdDialog, term) {
                    $scope.term = term;

                    getStatistics();
                    getOwners();
                    getDynamicFields();
                    getFollowers();

                    $scope.hide = function () {
                        $mdDialog.hide();
                    };

                    $scope.cancel = function () {
                        $mdDialog.cancel();
                    };

                    $scope.answer = function (answer) {
                        $mdDialog.hide(answer);
                    };

                    function getFollowers()
                    {
                        $http.get('/api/Artifact/' + $scope.term.ID + '/followers').
                          success(function (data, status) {
                              console.log(data);

                              $scope.followers = data;

                              console.log("followers: " + $scope.followers);

                          }).
                          error(function (data, status, headers, config) {
                              console.log("Detail followers returned error code " + status);
                          });
                    }

                    function getDynamicFields()
                    {                        
                        $http.get('/api/Artifact/' + $scope.term.ID + '/info').
                          success(function (data, status) {
                              console.log(data);

                              $scope.info = data;

                              console.log("info: " + $scope.info);

                          }).
                          error(function (data, status, headers, config) {
                              console.log("Detail info returned error code " + status);
                          });
                    }

                    function getOwners()
                    {                        
                        $http.get('/api/Artifact/' + $scope.term.ID + '/ownership').
                          success(function (data, status) {
                              console.log(data);

                              $scope.ownership = data;

                              console.log("ownership: " + $scope.ownership);

                          }).
                          error(function (data, status, headers, config) {
                              console.log("Detail ownership returned error code " + status);
                          });
                    }

                    function getStatistics()
                    {
                        $http.get('/api/Artifact/' + $scope.term.ID + '/object/statistics').
                          success(function (data, status) {                              
                              console.log(data);

                              $scope.statistics = data;

                              console.log("stats: " + $scope.statistics);
                              
                          }).
                          error(function (data, status, headers, config) {                          
                              console.log("Detail stats returned error code " + status);
                          });                        
                    }
                }
            }


            /**
             * First hide the bottomsheet IF visible, then
             * hide or Show the 'left' sideNav area
             */
            function toggleUsersList() {
                var pending = $mdBottomSheet.hide() || $q.when(true);
                
                pending.then(function () {
                    $mdSidenav('left').toggle();
                });
            }
            
        }        
]);

