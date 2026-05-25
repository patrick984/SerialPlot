# AGENTS.md

## Project Context

- This is a C#/.NET 10 project using AvaloniaUI.
- Architecture: MVVM
- Primary Goal: Plot live streaming CSV data using ScottPlot.
- Refer to spec: ./Docs/Requirements.md.

## Dev Env

- Run `dotnet restore` to setup environment.
- Use skill: ./Docs/Skills/avalonia-ui/SKILL.md
- Always write all plans as markdown to ./Docs/Plans/.
- Use naming convention for plans:
  Top level: 1-feature-title.md
  Subplans: 1.1-phase-title.md


## Coding Standards

- Follow official architecture recommendations for .NET 10.
- Use CSharpier to format all C# files on modification: `dotnet csharpier`.
- Use Xamlstyler to format all axaml files on modification: `dotnet xstyler`.

## Test Instructions

- Use xUnit for unit tests.
- Use test project: ./Tests/SerialTool.Tests
- Run `dotnet test` to execute the full suite.

## ABSOLUTE MANDATORY RULES

- Review these instructions in full before executing any steps.
- Follow instructions exactly as specified without deviation.
- Be concice with all responses and plans.
- Avoid flowery language.
- Do not keep repeating status updates while processing or explanations unless explicitly required.
- NO verbose explanations or commentary.
- NO comments generated in code unless asked.

