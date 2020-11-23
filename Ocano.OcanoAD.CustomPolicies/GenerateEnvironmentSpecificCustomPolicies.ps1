#Requires -Version 5.1
[CmdletBinding()]
param (
    [ValidateNotNullOrEmpty()]
    [string]$TemplateFolderPath = "$PSScriptRoot\template"
)

$buildCommand = {
    # [string]$Name
    # [string]$TenantId
    # [string]$BlobBaseUri
    # [string]$ProxyIdentityExperienceFrameworkId
    # [string]$IdentityExperienceFrameworkId
    # [string]$B2CExtensionApplicationId
    # [string]$B2CExtensionApplicationObjectId
    # [string]$B2CLoginBaseUri
    # [string]$IssuerBaseUri
    # [string]$PrimaryApplicationId
    # [string]$PrimaryApplicationUri
    # [string]$SecondaryApplicationUri
    $environments = @(
        [Environment]::new(
            'debug', 
            'YOUR-TENANT-ID', 
            'YOUR-BLOB-BASE-URI', 
            'YOUR-PROXY-APP-ID', 
            'YOUR-IEF-APP-ID',
            'YOUR-EXTENSIONS-APP-ID',
            'YOUR-EXTENSIONS-OBJECT-ID',
            'YOUR-B2C-LOGIN-BASE-URI',
            'YOUR-ISSUER-BASE-URI',
            'YOUR-PRIMARY-APP-ID',
            'https%3A%2F%2FYOUR-PRIMARY-APP-URI',
            'https%3A%2F%2FYOUR-SECONDARY-APP-URI')
    )
    try {
        foreach ($environment in $environments) {
            $environmentName = $environment.Name
            Write-Host ("{0:O}: Processing environment '$environmentName'..." -f (Get-Date))
            
            $environmentFolderPath = "$PSScriptRoot\$environmentName"
            if (Test-Path $environmentFolderPath) {
                Write-Host "Removing folder '$environmentFolderPath'..."
                Remove-Item -LiteralPath $environmentFolderPath -Force -Recurse
            }
                
            # Copy template folder and make copy into environment folder
            Copy-Item -Recurse -Path $TemplateFolderPath -Destination $environmentFolderPath

            Write-Host 'Listing files in environment folder...'
            $files = Get-ChildItem -Recurse -File -Path $environmentFolderPath
            foreach ($file in $files) {
                $filePath = $file.FullName
                Write-Host $filePath
                (Get-Content -Path $filePath -Raw) -Replace '__TenantId__', $environment.TenantId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__BlobBaseUri__', $environment.BlobBaseUri | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__ProxyIdentityExperienceFrameworkId__', $environment.ProxyIdentityExperienceFrameworkId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__IdentityExperienceFrameworkId__', $environment.IdentityExperienceFrameworkId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__B2CExtensionApplicationId__', $environment.B2CExtensionApplicationId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__B2CExtensionApplicationObjectId__', $environment.B2CExtensionApplicationObjectId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__B2CLoginBaseUri__', $environment.B2CLoginBaseUri | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__IssuerBaseUri__', $environment.IssuerBaseUri | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__PrimaryApplicationId__', $environment.PrimaryApplicationId | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__PrimaryApplicationUri__', $environment.PrimaryApplicationUri | Set-Content -Path $filePath -Encoding utf8
                (Get-Content -Path $filePath -Raw) -Replace '__SecondaryApplicationUri__', $environment.SecondaryApplicationUri | Set-Content -Path $filePath -Encoding utf8
            }
        }
        Write-Host "Done."
    }
    catch {
        Write-Error -Message $_.Exception.ToString() -Exception $_.Exception
        exit 1
    }
}

class Environment {
    Environment(
        [string]$name,
        [string]$tenantId,
        [string]$blobBaseUri,
        [string]$proxyIdentityExperienceFrameworkId,
        [string]$identityExperienceFrameworkId,
        [string]$b2CExtensionApplicationId,
        [string]$b2CExtensionApplicationObjectId,
        [string]$b2CLoginBaseUri,
        [string]$issuerBaseUri,
        [string]$primaryApplicationId,
        [string]$primaryApplicationUri,
        [string]$SecondaryApplicationUri
    ){
        $this.Name = $name
        $this.TenantId = $tenantId
        $this.BlobBaseUri = $blobBaseUri
        $this.ProxyIdentityExperienceFrameworkId = $proxyIdentityExperienceFrameworkId
        $this.IdentityExperienceFrameworkId = $identityExperienceFrameworkId
        $this.B2CExtensionApplicationId = $b2CExtensionApplicationId
        $this.B2CExtensionApplicationObjectId = $b2CExtensionApplicationObjectId
        $this.B2CLoginBaseUri = $b2CLoginBaseUri
        $this.IssuerBaseUri = $issuerBaseUri
        $this.PrimaryApplicationId = $primaryApplicationId
        $this.PrimaryApplicationUri = $primaryApplicationUri
        $this.SecondaryApplicationUri = $SecondaryApplicationUri
    }

    [string]$Name
    [string]$TenantId
    [string]$BlobBaseUri
    [string]$ProxyIdentityExperienceFrameworkId
    [string]$IdentityExperienceFrameworkId
    [string]$B2CExtensionApplicationId
    [string]$B2CExtensionApplicationObjectId
    [string]$B2CLoginBaseUri
    [string]$IssuerBaseUri
    [string]$PrimaryApplicationId
    [string]$PrimaryApplicationUri
    [string]$SecondaryApplicationUri
}

$elapsedTime = Measure-Command $buildCommand
Write-Host ("Elapsed time: {0:g}" -f $elapsedTime)
