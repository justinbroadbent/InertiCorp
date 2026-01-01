using System.Text.Json;
using InertiCorp.Core.Email;

namespace InertiCorp.Core.Content;

/// <summary>
/// Represents an employee in the company directory.
/// </summary>
public record Employee(string Name, string Title, string Department);

/// <summary>
/// Provides access to the InertiCorp company directory for email signatures.
/// </summary>
public static class CompanyDirectory
{
    private static readonly Dictionary<SenderArchetype, List<Employee>> _employees = new();
    private static readonly Dictionary<string, string> _signatureFormats = new();
    private static bool _loaded;

    static CompanyDirectory()
    {
        LoadDirectory();
    }

    private static void LoadDirectory()
    {
        if (_loaded) return;

        // Default employees in case JSON loading fails
        InitializeDefaults();

        try
        {
            var assembly = typeof(CompanyDirectory).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("company_directory.json"));

            if (resourceName is not null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is not null)
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    ParseDirectory(json);
                }
            }
        }
        catch
        {
            // Fall back to defaults
        }

        _loaded = true;
    }

    private static void ParseDirectory(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("employees", out var employeesElement))
        {
            foreach (var archetypeProp in employeesElement.EnumerateObject())
            {
                if (Enum.TryParse<SenderArchetype>(archetypeProp.Name, out var archetype))
                {
                    var employees = new List<Employee>();
                    foreach (var empElement in archetypeProp.Value.EnumerateArray())
                    {
                        var name = empElement.GetProperty("name").GetString() ?? "Unknown";
                        var title = empElement.GetProperty("title").GetString() ?? "Employee";
                        var dept = empElement.GetProperty("department").GetString() ?? "InertiCorp";
                        employees.Add(new Employee(name, title, dept));
                    }
                    _employees[archetype] = employees;
                }
            }
        }

        if (root.TryGetProperty("signatureFormats", out var formatsElement))
        {
            foreach (var formatProp in formatsElement.EnumerateObject())
            {
                _signatureFormats[formatProp.Name] = formatProp.Value.GetString() ?? "";
            }
        }
    }

    private static void InitializeDefaults()
    {
        _employees[SenderArchetype.PM] = new List<Employee>
        {
            new("Jamie Chen", "Product Manager", "Product")
        };
        _employees[SenderArchetype.EngManager] = new List<Employee>
        {
            new("Alex Rivera", "Engineering Director", "Engineering")
        };
        _employees[SenderArchetype.Legal] = new List<Employee>
        {
            new("Morgan Wells", "General Counsel", "Legal")
        };
        _employees[SenderArchetype.Security] = new List<Employee>
        {
            new("Taylor Kim", "Chief Information Security Officer", "InfoSec")
        };
        _employees[SenderArchetype.CFO] = new List<Employee>
        {
            new("Jordan Blake", "Chief Financial Officer", "Finance")
        };
        _employees[SenderArchetype.HR] = new List<Employee>
        {
            new("Casey Morgan", "Chief People Officer", "Human Resources")
        };
        _employees[SenderArchetype.Marketing] = new List<Employee>
        {
            new("Riley Thompson", "Chief Marketing Officer", "Marketing")
        };
        _employees[SenderArchetype.BoardMember] = new List<Employee>
        {
            new("Patricia Sterling", "Chairperson, Board of Directors", "Board")
        };
        _employees[SenderArchetype.TechLead] = new List<Employee>
        {
            new("Sam Park", "Principal Engineer", "Engineering")
        };
        _employees[SenderArchetype.Compliance] = new List<Employee>
        {
            new("Drew Martinez", "Compliance Director", "Legal & Compliance")
        };

        _signatureFormats["formal"] = "{name}\n{title}\n{department}\nInertiCorp Holdings, LLC\n\n\"Synergy Through Excellence\"";
        _signatureFormats["standard"] = "{name}\n{title} | {department}\nInertiCorp";
        _signatureFormats["casual"] = "- {name}\n{title}";
        _signatureFormats["board"] = "{name}\n{title}\nInertiCorp Holdings, LLC\n\nThis communication is confidential and privileged.";
    }

    /// <summary>
    /// Gets a random employee for the given sender archetype.
    /// </summary>
    public static Employee GetEmployee(SenderArchetype archetype, int seed)
    {
        if (!_employees.TryGetValue(archetype, out var employees) || employees.Count == 0)
        {
            return new Employee("A. Employee", "Staff Member", "InertiCorp");
        }

        var index = Math.Abs(seed) % employees.Count;
        return employees[index];
    }

    /// <summary>
    /// Gets a specific employee for the archetype based on a unique event ID.
    /// </summary>
    public static Employee GetEmployeeForEvent(SenderArchetype archetype, string eventId)
    {
        var seed = Math.Abs(eventId.GetHashCode());
        return GetEmployee(archetype, seed);
    }

    /// <summary>
    /// Generates a signature block for the given sender archetype and event ID.
    /// </summary>
    public static string GenerateSignature(SenderArchetype archetype, string eventId, EmailTone tone = EmailTone.Professional)
    {
        var employee = GetEmployeeForEvent(archetype, eventId);
        var formatKey = GetSignatureFormat(archetype, tone);

        if (!_signatureFormats.TryGetValue(formatKey, out var format))
        {
            format = _signatureFormats.GetValueOrDefault("standard", "{name}\n{title}");
        }

        return format
            .Replace("{name}", employee.Name)
            .Replace("{title}", employee.Title)
            .Replace("{department}", employee.Department);
    }

    /// <summary>
    /// Generates a signature for a specific employee.
    /// </summary>
    public static string GenerateSignature(Employee employee, EmailTone tone = EmailTone.Professional)
    {
        var formatKey = tone switch
        {
            EmailTone.Blunt or EmailTone.Panicked => "casual",
            EmailTone.Professional => "standard",
            _ => "standard"
        };

        if (!_signatureFormats.TryGetValue(formatKey, out var format))
        {
            format = "{name}\n{title}";
        }

        return format
            .Replace("{name}", employee.Name)
            .Replace("{title}", employee.Title)
            .Replace("{department}", employee.Department);
    }

    private static string GetSignatureFormat(SenderArchetype archetype, EmailTone tone)
    {
        return archetype switch
        {
            SenderArchetype.BoardMember => "board",
            SenderArchetype.Legal or SenderArchetype.Compliance => "formal",
            SenderArchetype.CFO => "formal",
            _ => tone switch
            {
                EmailTone.Blunt or EmailTone.Panicked => "casual",
                EmailTone.Aloof => "casual",
                _ => "standard"
            }
        };
    }

    /// <summary>
    /// Gets all employees for a given archetype.
    /// </summary>
    public static IReadOnlyList<Employee> GetEmployees(SenderArchetype archetype)
    {
        return _employees.TryGetValue(archetype, out var employees)
            ? employees
            : Array.Empty<Employee>();
    }
}
