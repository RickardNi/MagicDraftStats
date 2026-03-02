# Copilot Instructions

## General Guidelines
- Prefer using the latest versions of all packages and avoid downgrades whenever possible.

## Code Comments

- Do not add comments to generated code unless the code does something unintuitive or requires explanation.
- Never remove existing comments from the code.

## Data Verification

- Source data used for app verification is located in `wwwroot/sample-data/`.
- Always double-check calculations and displayed values against the files in `wwwroot/sample-data/`, since that is the data the user sees when using the app.