function UserService() {
    var UserService = {};
    function getUsers() {
        return [
            {
                name: 'Alexia',
                id: 3,
                cat: 'child'
            },
            {
                name: 'Christine',
                id: 2,
                cat: 'adult'
            },
            {
                name: 'Mike',
                id: 1,
                cat: 'adult'
            }
        ];
    }
    UserService.getUsers = function () {
        return getUsers();
    };
    return UserService;
}

var AppCtrl = function (UserService) {
    var vm = this;
    vm.items = [];
    vm.getUsers = function () {
        vm.items = UserService.getUsers();
        return vm.items;
    };
    vm.removeUser = function (item, index) {
        vm.items.splice(index, 1);
    };
};

var app = angular
    .module('app', [])
    .controller('AppCtrl', AppCtrl)
    .service('UserService', UserService);