# Custom policy generator
1. Always make changes in template folder. Never in a folder for the specific environment.
2. When you're finished making your changes, run the script [GenerateEnvironmentSpecificUserFlow.ps1](.\GenerateEnvironmentSpecificUserFlow.ps1)
3. Once the script is done running, upload the files that you've changed to Azure
    * Images and templates to blob storage
    * Policy files to B2C