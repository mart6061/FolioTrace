# Copilot Instructions

## General Guidelines
- Add code changes and corresponding tests to the repository for feature work and bug fixes.
- Target .NET 10 for the workspace and project files.

## Project Guidelines

### Type Creation
- Use ISO2 and BuilderISO2 as templates when creating new types or when the user requests a new Type. the name space should be ILibrary.Types; and folder should be in namespace ILibrary.Types. with the same name as the type.
  - For GUID or DateTime types, create two Create methods in the builder: Create() and Create(type value).
  - For all other types, create a single Create(type value) method.
