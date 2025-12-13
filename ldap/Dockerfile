FROM osixia/openldap:latest

ENV LDAP_ORGANISATION="Photobook" \
    LDAP_DOMAIN="photobook.local" \
    LDAP_ADMIN_PASSWORD="admin123" \
    LDAP_CONFIG_PASSWORD="config123" \
    LDAP_BASE_DN="dc=photobook,dc=local"

EXPOSE 389 636

VOLUME /var/lib/ldap /etc/ldap/slapd.d
