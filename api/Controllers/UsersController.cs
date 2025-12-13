using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;
using System.Text.Json;

namespace PhotobookAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private const string LdapServer = "ldap";
    private const int LdapPort = 389;
    private const string BaseDn = "dc=photobook,dc=local";
    private const string AdminDn = "cn=admin,dc=photobook,dc=local";
    private const string AdminPassword = "admin123";

    /// <summary>
    /// Get all LDAP users with their profile information
    /// </summary>
    [HttpGet]
    public IActionResult GetUsers([FromQuery] string? search = null)
    {
        try
        {
            var users = new List<LdapUser>();
            
            // Build LDAP filter
            string filter = "objectClass=inetOrgPerson";
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Replace("\\", "\\5c")
                    .Replace("*", "\\2a")
                    .Replace("(", "\\28")
                    .Replace(")", "\\29")
                    .Replace("\0", "\\00");
                
                filter = $"(&(objectClass=inetOrgPerson)(|(uid=*{search}*)(mail=*{search}*)(givenName=*{search}*)(sn=*{search}*)))";
            }

            // Use ldapsearch command
            var psi = new ProcessStartInfo
            {
                FileName = "ldapsearch",
                Arguments = $"-x -H ldap://{LdapServer}:{LdapPort} -D \"{AdminDn}\" -w {AdminPassword} -b \"ou=users,{BaseDn}\" \"{filter}\" uid cn mail givenName sn jpegPhoto description title mobile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return StatusCode(500, new { error = "LDAP Error", message = error });
                }

                users = ParseLdapOutput(output);
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific user by uid
    /// </summary>
    [HttpGet("{uid}")]
    public IActionResult GetUser(string uid)
    {
        try
        {
            var escapedUid = uid.Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");

            var psi = new ProcessStartInfo
            {
                FileName = "ldapsearch",
                Arguments = $"-x -H ldap://{LdapServer}:{LdapPort} -D \"{AdminDn}\" -w {AdminPassword} -b \"ou=users,{BaseDn}\" \"(&(objectClass=inetOrgPerson)(uid={escapedUid}))\" uid cn mail givenName sn jpegPhoto description title mobile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return StatusCode(500, new { error = "LDAP Error", message = error });
                }

                var users = ParseLdapOutput(output);
                if (users.Any())
                {
                    return Ok(users.First());
                }

                return NotFound(new { error = "User not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticate a user against LDAP
    /// </summary>
    [HttpPost("authenticate")]
    public IActionResult AuthenticateUser([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Username and password are required" });

        try
        {
            var userDn = $"cn={request.Username},ou=users,{BaseDn}";
            
            var psi = new ProcessStartInfo
            {
                FileName = "ldapwhoami",
                Arguments = $"-x -H ldap://{LdapServer}:{LdapPort} -D \"{userDn}\" -w {request.Password}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return Ok(new { success = true, message = "Authentication successful" });
                }
                else
                {
                    return Unauthorized(new { error = "Authentication failed" });
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    /// <summary>
    /// Parse LDIF output from ldapsearch command
    /// </summary>
    private List<LdapUser> ParseLdapOutput(string ldifOutput)
    {
        var users = new List<LdapUser>();
        var currentEntry = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var lines = ldifOutput.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                if (currentEntry.Any())
                {
                    var user = BuildUserFromEntry(currentEntry);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                    currentEntry.Clear();
                }
            }
            else if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("dn:") || trimmedLine.StartsWith("search:"))
            {
                // Skip comments and metadata
                continue;
            }
            else if (trimmedLine.Contains(":"))
            {
                var colonIndex = trimmedLine.IndexOf(':');
                var key = trimmedLine.Substring(0, colonIndex).Trim();
                var value = trimmedLine.Substring(colonIndex + 1).Trim();
                
                // Skip objectClass entries
                if (key.Equals("objectClass", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Handle base64 encoded values (start with ":: ")
                if (value.StartsWith(": ") && key.Equals("jpegPhoto", StringComparison.OrdinalIgnoreCase))
                {
                    // For binary data, the value after ": " is base64 encoded
                    value = value.Substring(2); // Remove ": " prefix
                    
                    // Continue reading continuation lines if present
                    while (i + 1 < lines.Length && (lines[i + 1].StartsWith(" ") || lines[i + 1].StartsWith("\t")))
                    {
                        i++;
                        value += lines[i].Trim();
                    }
                }
                else if (value.StartsWith(": "))
                {
                    // Regular base64 value, decode it
                    try
                    {
                        var encoded = value.Substring(2);
                        value = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    }
                    catch
                    {
                        value = value.Substring(2); // If decode fails, use as-is
                    }
                }

                if (!currentEntry.ContainsKey(key))
                {
                    currentEntry[key] = new List<string>();
                }
                currentEntry[key].Add(value);
            }
        }

        // Don't forget the last entry
        if (currentEntry.Any())
        {
            var user = BuildUserFromEntry(currentEntry);
            if (user != null)
            {
                users.Add(user);
            }
        }

        return users;
    }

    /// <summary>
    /// Build LdapUser object from entry dictionary
    /// </summary>
    private LdapUser? BuildUserFromEntry(Dictionary<string, List<string>> entry)
    {
        if (!entry.ContainsKey("uid") || entry["uid"].Count == 0)
            return null;

        var user = new LdapUser
        {
            Uid = GetFirstValue(entry, "uid"),
            Cn = GetFirstValue(entry, "cn"),
            Email = GetFirstValue(entry, "mail"),
            FirstName = GetFirstValue(entry, "givenName"),
            LastName = GetFirstValue(entry, "sn"),
            Description = GetFirstValue(entry, "description"),
            Title = GetFirstValue(entry, "title"),
            Mobile = GetFirstValue(entry, "mobile")
        };

        // Handle jpegPhoto (binary data, base64 encoded in LDIF output)
        if (entry.ContainsKey("jpegPhoto"))
        {
            var photoValue = entry["jpegPhoto"].FirstOrDefault();
            if (!string.IsNullOrEmpty(photoValue) && photoValue.Length > 10)
            {
                try
                {
                    // The value is already base64 encoded from ldapsearch output
                    // Validate it's valid base64 before using
                    var testBytes = Convert.FromBase64String(photoValue);
                    user.ProfilePicture = "data:image/jpeg;base64," + photoValue;
                }
                catch
                {
                    // If decode fails, skip the image
                }
            }
        }

        return user;
    }

    private string GetFirstValue(Dictionary<string, List<string>> entry, string key)
    {
        return entry.ContainsKey(key) && entry[key].Count > 0 ? entry[key][0] : string.Empty;
    }
}


public class LdapUser
{
    public string Uid { get; set; } = string.Empty;
    public string Cn { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
