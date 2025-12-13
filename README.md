# Photobook - Multi-Container Application

A complete three-container Docker application featuring:
- **.NET Core 8.0 API** - RESTful backend with LDAP integration
- **AngularJS Material UI** - Frontend for photo management
- **OpenLDAP** - Active Directory for user authentication

## Project Structure

```
photobook/
├── api/                    # .NET Core 8.0 API
│   ├── Dockerfile
│   ├── PhotobookAPI.csproj
│   ├── Program.cs
│   └── Controllers/
│       └── PhotosController.cs
├── frontend/              # AngularJS Material UI
│   ├── Dockerfile
│   ├── package.json
│   ├── index.html
│   ├── js/
│   │   └── app.js
│   └── css/
│       └── styles.css
├── ldap/                  # OpenLDAP Configuration
│   ├── Dockerfile
│   ├── init.ldif
│   └── bootstrap.sh
└── docker-compose.yml     # Container Orchestration
```

## Prerequisites

- Docker (v20.10+)
- Docker Compose (v2.0+)
- 2GB RAM minimum
- Ports 389, 636, 4200, 5000 available

## Quick Start

### 1. Build and Start Containers

```bash
docker-compose up -d
```

This will:
- Build and start the LDAP server
- Build and start the .NET Core 8.0 API
- Build and start the AngularJS frontend

### 2. Access the Application

**Frontend:** http://localhost:4200
**API Swagger:** http://localhost:5000/swagger/index.html
**LDAP Server:** ldap://localhost:389

## Services

### Frontend (AngularJS Material UI)
- **Port:** 4200
- **Features:**
  - Photo gallery with Material Design
  - Add new photos with metadata
  - Delete photos
  - Responsive layout with sidenav
  - Real-time API integration

- **Default URL:** http://localhost:4200

### API (.NET Core 8.0)
- **Port:** 5000
- **Endpoints:**
  - `GET /api/photos` - List all photos
  - `GET /api/photos/{id}` - Get photo by ID
  - `POST /api/photos` - Create new photo
  - `DELETE /api/photos/{id}` - Delete photo
  - `GET /health` - Health check

- **API Documentation:** http://localhost:5000/swagger/index.html

### LDAP Server (OpenLDAP)
- **Port:** 389 (LDAP), 636 (LDAPS)
- **Admin DN:** cn=admin,dc=photobook,dc=local
- **Admin Password:** admin123
- **Base DN:** dc=photobook,dc=local

#### Default LDAP Users

| User | Email | Password |
|------|-------|----------|
| admin | - | admin123 |
| user1 | user1@photobook.local | {SSHA}password_hash |
| user2 | user2@photobook.local | {SSHA}password_hash |

## Docker Compose Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f ldap
```

### Rebuild Images
```bash
docker-compose up -d --build
```

### Remove Volumes (Warning: Deletes data)
```bash
docker-compose down -v
```

## Development

### API Development

Navigate to the `api` directory:

```bash
cd api
dotnet restore
dotnet run
```

The API will be available at http://localhost:5000

### Frontend Development

Navigate to the `frontend` directory:

```bash
cd frontend
npm install
npm start
```

The frontend will be available at http://localhost:4200

### LDAP Administration

#### Using ldapadd to add users:

```bash
ldapadd -x -H ldap://localhost:389 -D "cn=admin,dc=photobook,dc=local" -w admin123 -f init.ldif
```

#### Using ldapsearch to query users:

```bash
ldapsearch -x -H ldap://localhost:389 -b "dc=photobook,dc=local" -D "cn=admin,dc=photobook,dc=local" -w admin123 "(objectClass=*)"
```

## API Integration with LDAP

The .NET Core API includes LDAP integration via the `Novell.Directory.Ldap.NETStandard` NuGet package. You can extend the API to:

1. Authenticate users against LDAP
2. Fetch user groups and permissions
3. Implement role-based access control
4. Sync user data with LDAP

### Example LDAP Authentication Code

```csharp
using Novell.Directory.Ldap;

public class LdapService
{
    private const string LdapServer = "ldap";
    private const int LdapPort = 389;
    private const string BaseDn = "dc=photobook,dc=local";

    public bool AuthenticateUser(string username, string password)
    {
        using (var connection = new LdapConnection())
        {
            connection.Connect(LdapServer, LdapPort);
            string userDn = $"cn={username},ou=users,{BaseDn}";
            
            try
            {
                connection.Bind(userDn, password);
                return true;
            }
            catch (LdapException)
            {
                return false;
            }
        }
    }
}
```

## Troubleshooting

### LDAP Connection Issues
```bash
# Check LDAP status
docker exec photobook-ldap slapd -T test
```

### API Connection Issues
```bash
# Check API logs
docker logs photobook-api

# Test API health
curl http://localhost:5000/health
```

### Frontend Not Loading
```bash
# Check frontend logs
docker logs photobook-frontend

# Verify frontend is running
curl http://localhost:4200/index.html
```

### Port Already in Use
Change the ports in `docker-compose.yml`:
```yaml
ports:
  - "8080:4200"  # Frontend on 8080
  - "5001:5000"  # API on 5001
  - "3389:389"   # LDAP on 3389
```

## Security Notes

⚠️ **IMPORTANT:** This configuration is for development only.

For production, you should:
1. Change all default passwords in `docker-compose.yml`
2. Use SSL/TLS for LDAP (636 port)
3. Implement CORS properly in the API
4. Add authentication and authorization
5. Use environment variables for secrets
6. Implement rate limiting
7. Use network policies and firewalls

## Next Steps

1. **User Authentication:** Add login/logout functionality to the frontend
2. **LDAP Integration:** Implement user authentication against LDAP in the API
3. **Photo Upload:** Add file upload capability instead of URL-based photos
4. **Database:** Add a database (PostgreSQL/MongoDB) for persistent storage
5. **Testing:** Add unit tests and integration tests
6. **CI/CD:** Setup GitHub Actions or similar for automated builds

## License

MIT License - Feel free to use and modify

## Support

For issues or questions, check the logs:
```bash
docker-compose logs -f
```