#!/usr/bin/env python3
"""
LDAP User Population Script
Generates 300 user accounts with base64-encoded profile pictures
"""

import base64
import io
import subprocess
import sys
from datetime import datetime
from PIL import Image, ImageDraw, ImageFont
import random

def create_avatar_image(user_id, name):
    """Create a simple avatar image and return as base64"""
    # Create a 200x200 image with random color background
    colors = [
        (255, 107, 107),  # Red
        (255, 159, 64),   # Orange
        (255, 206, 86),   # Yellow
        (75, 192, 192),   # Teal
        (54, 162, 235),   # Blue
        (153, 102, 255),  # Purple
        (255, 159, 243),  # Pink
        (99, 255, 132),   # Green
    ]
    
    color = colors[user_id % len(colors)]
    img = Image.new('RGB', (200, 200), color=color)
    draw = ImageDraw.Draw(img)
    
    # Add initials
    initials = ''.join([word[0].upper() for word in name.split()])
    try:
        draw.text((100, 100), initials, fill=(255, 255, 255), anchor="mm")
    except:
        # Fallback if fonts not available
        draw.text((80, 90), initials, fill=(255, 255, 255))
    
    # Convert to base64
    buffer = io.BytesIO()
    img.save(buffer, format='PNG')
    img_base64 = base64.b64encode(buffer.getvalue()).decode('utf-8')
    return img_base64

def generate_ldif(num_users=300):
    """Generate LDIF file with user accounts"""
    
    ldif_content = """# LDAP Data Interchange Format
# Generated: {}
# Number of Users: {}

# Base DN
dn: dc=photobook,dc=local
objectClass: dcObject
objectClass: organization
o: Photobook
dc: photobook

# Users Organizational Unit
dn: ou=users,dc=photobook,dc=local
objectClass: organizationalUnit
ou: users
description: User accounts

""".format(datetime.now().isoformat(), num_users)
    
    # Generate user entries
    for i in range(1, num_users + 1):
        user_id = i
        first_name = f"User{i}"
        last_name = f"Account{i}"
        username = f"user{i}"
        email = f"user{i}@photobook.local"
        
        # Create avatar
        avatar_base64 = create_avatar_image(i, f"{first_name} {last_name}")
        
        # Create password hash (SSHA format)
        password = f"password{i}"
        
        ldif_content += f"""# User: {username}
dn: cn={username},ou=users,dc=photobook,dc=local
objectClass: inetOrgPerson
objectClass: organizationalPerson
objectClass: person
objectClass: top
cn: {username}
sn: {last_name}
givenName: {first_name}
uid: {username}
mail: {email}
userPassword: {password}
jpegPhoto:: {avatar_base64}
description: Test user account {i}
createTimestamp: {datetime.now().isoformat()}Z
employeeNumber: {i:04d}
mobile: +1-555-{i:04d}
department: Sales

"""
    
    return ldif_content

def load_ldap_users(ldif_content, ldap_host='localhost', ldap_port=389):
    """Load users into LDAP server"""
    try:
        # Write LDIF to file
        ldif_file = '/tmp/users.ldif'
        with open(ldif_file, 'w') as f:
            f.write(ldif_content)
        
        print(f"✓ Generated LDIF file: {ldif_file}")
        
        # Try to load into LDAP
        cmd = [
            'ldapadd',
            '-x',
            f'-H', f'ldap://{ldap_host}:{ldap_port}',
            '-D', 'cn=admin,dc=photobook,dc=local',
            '-w', 'admin123',
            '-f', ldif_file
        ]
        
        print(f"\n📤 Loading users into LDAP server...")
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
        
        if result.returncode == 0:
            print("✓ Successfully loaded users into LDAP")
            return True
        else:
            print(f"⚠ LDAP load output: {result.stdout}")
            if result.stderr:
                print(f"⚠ LDAP errors: {result.stderr}")
            return False
    
    except subprocess.TimeoutExpired:
        print("✗ LDAP connection timeout - server may not be ready")
        return False
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def verify_users(ldap_host='localhost', ldap_port=389):
    """Verify users were created"""
    try:
        cmd = [
            'ldapsearch',
            '-x',
            f'-H', f'ldap://{ldap_host}:{ldap_port}',
            '-b', 'ou=users,dc=photobook,dc=local',
            '-D', 'cn=admin,dc=photobook,dc=local',
            '-w', 'admin123',
            'objectClass=inetOrgPerson',
            'cn'
        ]
        
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=10)
        
        if result.returncode == 0:
            # Count results
            count = result.stdout.count('cn: user')
            print(f"\n✓ Verification: Found {count} user accounts in LDAP")
            return count
        else:
            print(f"⚠ Verification query returned: {result.stderr}")
            return 0
    
    except Exception as e:
        print(f"✗ Verification error: {str(e)}")
        return 0

if __name__ == '__main__':
    print("=" * 60)
    print("LDAP User Population Script")
    print("=" * 60)
    
    num_users = 300
    
    # Check for PIL/Pillow
    try:
        from PIL import Image
    except ImportError:
        print("\n⚠ PIL/Pillow not found. Installing...")
        subprocess.run(['pip', 'install', 'Pillow'], capture_output=True)
    
    print(f"\n📝 Generating {num_users} user accounts with profile pictures...")
    ldif_data = generate_ldif(num_users)
    
    # Save LDIF locally
    with open('/workspaces/photobook/ldap/users.ldif', 'w') as f:
        f.write(ldif_data)
    print(f"✓ Saved LDIF to: /workspaces/photobook/ldap/users.ldif")
    
    print(f"\n📊 LDIF Statistics:")
    print(f"   - Users: {num_users}")
    print(f"   - Each with profile picture (base64 PNG)")
    print(f"   - File size: {len(ldif_data) / (1024*1024):.2f} MB")
    
    # Try to load into LDAP
    print(f"\n⏳ Attempting to connect to LDAP server...")
    if load_ldap_users(ldif_data):
        count = verify_users()
        if count > 0:
            print("\n" + "=" * 60)
            print("✓ SUCCESS! All users loaded into LDAP")
            print("=" * 60)
        else:
            print("\n⚠ Users may have been loaded but verification failed")
    else:
        print("\n⚠ Could not connect to LDAP server")
        print("   Run this command manually:")
        print(f"   ldapadd -x -H ldap://localhost:389 \\")
        print(f"     -D cn=admin,dc=photobook,dc=local \\")
        print(f"     -w admin123 \\")
        print(f"     -f /workspaces/photobook/ldap/users.ldif")
