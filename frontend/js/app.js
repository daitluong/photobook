console.log('App.js loading...');

angular.module('photobookApp', ['ngMaterial', 'ngAnimate'])
    .controller('MainController', ['$scope', '$http', '$timeout', '$q', function($scope, $http, $timeout, $q) {
        
        console.log('MainController initialized');
        
        $scope.users = [];
        $scope.loading = false;
        $scope.searchQuery = '';

        // Determine API URL based on environment
        let usersApiUrl;
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            // Local development with Docker
            usersApiUrl = 'http://localhost:5000/api/users';
        } else if (window.location.hostname.includes('app.github.dev') || window.location.hostname.includes('github.dev')) {
            // GitHub Codespaces port forwarding - replace port in hostname
            const apiHostname = window.location.hostname.replace('-4200.', '-5000.');
            usersApiUrl = `${window.location.protocol}//${apiHostname}/api/users`;
        } else {
            // Fallback for Docker internal network
            usersApiUrl = 'http://api:5000/api/users';
        }

        console.log('API URL:', usersApiUrl);

        $scope.loadUsers = function() {
            console.log('loadUsers() called, searchQuery=' + $scope.searchQuery);
            $scope.loading = true;
            let url = usersApiUrl;
            
            // Add search parameter if search query is not empty
            if ($scope.searchQuery && $scope.searchQuery.trim() !== '') {
                url += '?search=' + encodeURIComponent($scope.searchQuery);
            }
            
            console.log('Loading users from:', url);
            
            $http.get(url, {
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(function(response) {
                    console.log('SUCCESS: Users loaded, count=' + response.data.length);
                    $scope.users = response.data;
                    $scope.loading = false;
                })
                .catch(function(error) {
                    console.error('FAILED: Error loading users:', error);
                    console.error('Error status:', error.status);
                    console.error('Error statusText:', error.statusText);
                    console.error('Error data:', error.data);
                    $scope.users = [];
                    $scope.loading = false;
                    alert('Error loading users: ' + (error.statusText || error.status || 'Unknown error'));
                });
        };

        $scope.toggleMenu = function() {
            // Simple toggle for sidebar
            const sidenav = document.querySelector('.md-sidenav-left');
            if (sidenav) {
                sidenav.classList.toggle('md-sidenav-open');
            }
        };

        // Auto-load users on initialization - do it immediately without $timeout
        console.log('About to call loadUsers() immediately');
        $scope.initStatus = 'Loading users...';
        $scope.loadUsers();
        
        // Also schedule with $timeout as backup
        $timeout(function() {
            console.log('Timeout callback: Initializing LDAP Directory app');
            if ($scope.users.length === 0) {
                console.log('No users yet, calling loadUsers again');
                $scope.loadUsers();
            }
        }, 500);
    }]);
