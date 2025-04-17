# Architectural Patterns Analysis

This directory contains definition files for architectural patterns that should be enforced in the codebase. The ArchitecturalPatternRule uses these definitions to identify code that doesn't adhere to the defined patterns.

## Structure of Pattern Definition Files

Each pattern is defined in a JSON file with the following structure:

```json
{
  "patternName": "Name of the pattern",
  "description": "Description of the pattern",
  "severity": "Warning|Error|Info",
  "rules": [
    {
      "type": "Structure",
      "expectedPattern": "Regular expression or structure description",
      "message": "Message to display when pattern is violated"
    },
    {
      "type": "Dependency",
      "allowedDependencies": ["Namespace1", "Namespace2"],
      "message": "Message to display when dependencies are violated"
    },
    {
      "type": "Naming",
      "pattern": "Regular expression for naming",
      "message": "Message to display when naming convention is violated"
    }
  ]
}
```

## Available Patterns

1. **MVCPattern.json** - Enforces Model-View-Controller architectural pattern
2. **RepositoryPattern.json** - Enforces Repository pattern for data access
3. **CleanArchitecture.json** - Enforces Clean Architecture principles
4. **LayeredArchitecture.json** - Enforces layered architecture principles
5. **MicroservicesPattern.json** - Enforces microservices architectural guidelines

## How to Add New Patterns

1. Create a new JSON file following the structure above
2. Place it in this directory
3. The ArchitecturalPatternRule will automatically pick it up during the next analysis

## How Patterns Are Applied

The ArchitecturalPatternRule will:
1. Load all pattern definitions from this directory
2. For each file being analyzed, check if it should adhere to any pattern
3. Validate the file against the pattern rules
4. Generate CodeIssue objects for any violations found