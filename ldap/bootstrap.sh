#!/bin/bash

# Wait for LDAP server to be ready
sleep 10

# Import the LDAP configuration if it exists
if [ -f /container/service/slapd/assets/config/bootstrap/ldif/custom/init.ldif ]; then
    echo "Importing LDAP data..."
    ldapadd -x -D "cn=admin,dc=photobook,dc=local" -w admin123 -f /container/service/slapd/assets/config/bootstrap/ldif/custom/init.ldif
fi

echo "LDAP initialization complete"
