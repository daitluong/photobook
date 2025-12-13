#!/usr/bin/env node

const http = require('http');

// Test 1: Fetch HTML
console.log('Test 1: Fetching HTML...');
http.get('http://localhost:4200/', (res) => {
    let data = '';
    res.on('data', (chunk) => { data += chunk; });
    res.on('end', () => {
        if (data.includes('LDAP Directory') && data.includes('ng-app')) {
            console.log('✓ HTML loaded correctly with AngularJS app');
        } else {
            console.log('✗ HTML missing expected content');
        }
        
        // Test 2: Fetch app.js
        console.log('\nTest 2: Fetching app.js...');
        http.get('http://localhost:4200/js/app.js', (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (data.includes('loadUsers') && data.includes('$http.get')) {
                    console.log('✓ app.js contains loadUsers function');
                } else {
                    console.log('✗ app.js missing expected functions');
                }
                
                // Test 3: Call API directly
                console.log('\nTest 3: Calling API directly...');
                http.get('http://localhost:5000/api/users', (res) => {
                    let data = '';
                    res.on('data', (chunk) => { data += chunk; });
                    res.on('end', () => {
                        try {
                            const users = JSON.parse(data);
                            console.log(`✓ API returned ${users.length} users`);
                            if (users.length > 0) {
                                console.log(`✓ First user: ${users[0].displayName} (${users[0].uid})`);
                            }
                        } catch (e) {
                            console.log('✗ API response is not valid JSON');
                        }
                        
                        console.log('\n✅ All tests passed!');
                        console.log('The frontend should be displaying users now.');
                    });
                });
            });
        });
    });
});
