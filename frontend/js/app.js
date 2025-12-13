angular.module('photobookApp', ['ngMaterial', 'ngAnimate'])
    .controller('MainController', ['$scope', '$http', '$mdDialog', '$mdSidenav', function($scope, $http, $mdDialog, $mdSidenav) {
        
        $scope.photos = [];
        $scope.loading = false;
        $scope.newPhoto = {};

        const API_URL = 'http://api:5000/api/photos';

        $scope.loadPhotos = function() {
            $scope.loading = true;
            $http.get(API_URL)
                .then(function(response) {
                    $scope.photos = response.data;
                    $scope.loading = false;
                })
                .catch(function(error) {
                    console.error('Error loading photos:', error);
                    $scope.loading = false;
                });
        };

        $scope.openAddPhoto = function(ev) {
            $mdDialog.show({
                controller: 'MainController',
                templateUrl: 'index.html',
                parent: angular.element(document.body),
                targetEvent: ev,
                clickOutsideToClose: true
            });
            $scope.newPhoto = {};
        };

        $scope.savePhoto = function() {
            if (!$scope.newPhoto.title || !$scope.newPhoto.url) {
                alert('Please fill in required fields');
                return;
            }

            $http.post(API_URL, $scope.newPhoto)
                .then(function(response) {
                    $scope.photos.push(response.data);
                    $scope.newPhoto = {};
                    $mdDialog.hide();
                })
                .catch(function(error) {
                    console.error('Error saving photo:', error);
                    alert('Error saving photo');
                });
        };

        $scope.deletePhoto = function(id) {
            if (confirm('Are you sure you want to delete this photo?')) {
                $http.delete(API_URL + '/' + id)
                    .then(function() {
                        $scope.photos = $scope.photos.filter(p => p.id !== id);
                    })
                    .catch(function(error) {
                        console.error('Error deleting photo:', error);
                        alert('Error deleting photo');
                    });
            }
        };

        $scope.cancelAddPhoto = function() {
            $scope.newPhoto = {};
            $mdDialog.cancel();
        };

        $scope.toggleMenu = function() {
            $mdSidenav('left').toggle();
        };

        // Load photos on init
        $scope.loadPhotos();
    }]);
