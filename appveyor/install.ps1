function Get-AppVeyorArtifacts
{
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
    param(
        #The name of the account you wish to download artifacts from
        [parameter(Mandatory = $true)]
        [string]$Account,
        #The name of the project you wish to download artifacts from
        [parameter(Mandatory = $true)]
        [string]$Project,
        #Where to save the downloaded artifacts. Defaults to current directory.
        [alias("DownloadDirectory")][string]$Path = '.',
        [string]$Token,
        #Filter to a specific branch or project directory. You can specify Branch as either branch name ("master") or build version ("0.1.29")
        [string]$Branch,
        #If you have multiple build jobs, specify which job you wish to retrieve the artifacts from
        [string]$JobName,
        #Download all files into a single directory, do not preserve any hierarchy that might exist in the artifacts
        [switch]$Flat,
        [string]$Proxy,
        [switch]$ProxyUseDefaultCredentials,
        #URL of Appveyor API. You normally shouldn't need to change this.
        $apiUrl = 'https://ci.appveyor.com/api'
    )

    $headers = @{
        'Content-type' = 'application/json'
    }

    if ($Token) {$headers.'Authorization' = "Bearer $token"}

    # Prepare proxy args to splat to Invoke-RestMethod
    $proxyArgs = @{}
    if (-not [string]::IsNullOrEmpty($proxy)) {
        $proxyArgs.Add('Proxy', $proxy)
    }
    if ($proxyUseDefaultCredentials.IsPresent) {
        $proxyArgs.Add('ProxyUseDefaultCredentials', $proxyUseDefaultCredentials)
    }

    $errorActionPreference = 'Stop'
    $projectURI = "$apiUrl/projects/$account/$project"
    if ($Branch) {$projectURI = $projectURI + "/branch/$Branch"}

    $projectObject = Invoke-RestMethod -Method Get -Uri $projectURI `
                                       -Headers $headers @proxyArgs

    if (-not $projectObject.build.jobs) {throw "No jobs found for this project or the project and/or account name was incorrectly specified"}

    if (($projectObject.build.jobs.count -gt 1) -and -not $jobName) {
        throw "Multiple Jobs found for the latest build. Please specify the -JobName paramter to select which job you want the artifacts for"
    }

    if ($JobName) {
        $jobid = ($projectObject.build.jobs | Where-Object name -eq "$JobName" | Select-Object -first 1).jobid
        if (-not $jobId) {throw "Unable to find a job named $JobName within the latest specified build. Did you spell it correctly?"}
    } else {
        $jobid = $projectObject.build.jobs[0].jobid
    }

    $artifacts = Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts" `
                                   -Headers $headers @proxyArgs
    $artifacts `
    | Where-Object { $psCmdlet.ShouldProcess($_.fileName) } `
    | ForEach-Object {

        $type = $_.type

        $localArtifactPath = $_.fileName -split '/' | ForEach-Object { [Uri]::UnescapeDataString($_) }
        if ($flat.IsPresent) {
            $localArtifactPath = ($localArtifactPath | Select-Object -Last 1)
        } else {
            $localArtifactPath = $localArtifactPath -join [IO.Path]::DirectorySeparatorChar
        }
        $localArtifactPath = Join-Path $path $localArtifactPath

        $artifactUrl = "$apiUrl/buildjobs/$jobId/artifacts/$($_.fileName)"
        Write-Verbose "Downloading $artifactUrl to $localArtifactPath"

        Invoke-RestMethod -Method Get -Uri $artifactUrl -OutFile $localArtifactPath -Headers $headers @proxyArgs

        New-Object PSObject -Property @{
            'Source' = $artifactUrl
            'Type'   = $type
            'Target' = $localArtifactPath
        }
    }
}

switch -Exact (${env:APPVEYOR_JOB_NAME}) 
{
    "Build Xirorig_Native Windows"
    { 
        break
    }

    "Build Xirorig_Native Linux"
    {
        # Install dependencies
        sudo apt-get update -y
        sudo apt-get install curl zip unzip tar cmake ninja-build build-essential pkg-config gcc-10 gcc-10-arm-linux-gnueabihf gcc-10-aarch64-linux-gnu g++-10 g++-10-arm-linux-gnueabihf g++-10-aarch64-linux-gnu -y

        sudo update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-10 999 --slave /usr/bin/g++ g++ /usr/bin/g++-10

        sudo update-alternatives --install /usr/bin/arm-linux-gnueabihf-gcc arm-linux-gnueabihf-gcc /usr/bin/arm-linux-gnueabihf-gcc-10 999
        sudo update-alternatives --install /usr/bin/arm-linux-gnueabihf-g++ arm-linux-gnueabihf-g++ /usr/bin/arm-linux-gnueabihf-g++-10 999

        sudo update-alternatives --install /usr/bin/aarch64-linux-gnu-gcc aarch64-linux-gnu-gcc /usr/bin/aarch64-linux-gnu-gcc-10 999
        sudo update-alternatives --install /usr/bin/aarch64-linux-gnu-g++ aarch64-linux-gnu-g++ /usr/bin/aarch64-linux-gnu-g++-10 999
        break
    }

    "Build Xirorig_Native MacOS"
    {
        # Install dependencies
        brew update
        brew install ninja
        break
    }

    "Build Xirorig Windows"
    {
        # Install dotnet enviroment
        Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "${env:APPVEYOR_BUILD_FOLDER}\..\dotnet-install.ps1"
        Set-Location "${env:APPVEYOR_BUILD_FOLDER}\.."
        ./dotnet-install.ps1 -Channel ${env:DOTNET_VERSION} -Quality ${env:DOTNET_RELEASE_STATUS} -InstallDir "${env:APPVEYOR_BUILD_FOLDER}\..\dotnet"

        # Create Directories
        New-Item "${env:APPVEYOR_BUILD_FOLDER}\${env:XIRORIG_NATIVE_ROOT}\${env:XIRORIG_NATIVE_OUTPUT_ROOT}\${env:XIRORIG_NATIVE_INSTALL_ROOT}" -ItemType Directory -Force

        # Download Xirorig Native Files
        Get-AppVeyorArtifacts ${env:APPVEYOR_ACCOUNT_NAME} ${env:APPVEYOR_PROJECT_NAME} -Token ${env:APPVEYOR_TOKEN_KEY} -JobName "Build Xirorig_Native Windows" -Path ${env:APPVEYOR_BUILD_FOLDER}
        7z x "${env:APPVEYOR_BUILD_FOLDER}\${env:XIRORIG_NATIVE_ROOT}\${env:XIRORIG_NATIVE_OUTPUT_ROOT}\${env:XIRORIG_NATIVE_INSTALL_ROOT}\${env:XIRORIG_NATIVE_ARTIFACT_NAME}"
        break
    }

    "Build Xirorig Linux"
    {
        # Install dotnet enviroment
        Invoke-WebRequest "https://dot.net/v1/dotnet-install.sh" -OutFile "${env:APPVEYOR_BUILD_FOLDER}/../dotnet-install.sh"
        Set-Location "${env:APPVEYOR_BUILD_FOLDER}/.."
        ./dotnet-install.sh -Channel ${env:DOTNET_VERSION} -Quality ${env:DOTNET_RELEASE_STATUS} -InstallDir "${env:APPVEYOR_BUILD_FOLDER}/../dotnet"

        # Create Directories
        New-Item "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/${env:XIRORIG_NATIVE_OUTPUT_ROOT}/${env:XIRORIG_NATIVE_INSTALL_ROOT}" -ItemType Directory -Force

        # Download Xirorig Native Files
        Get-AppVeyorArtifacts ${env:APPVEYOR_ACCOUNT_NAME} ${env:APPVEYOR_PROJECT_NAME} -Token ${env:APPVEYOR_TOKEN_KEY} -JobName "Build Xirorig_Native Linux" -Path ${env:APPVEYOR_BUILD_FOLDER}
        7z x "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/${env:XIRORIG_NATIVE_OUTPUT_ROOT}/${env:XIRORIG_NATIVE_INSTALL_ROOT}/${env:XIRORIG_NATIVE_ARTIFACT_NAME}"
        break
    }

    "Build Xirorig MacOS"
    {
        # Install dotnet enviroment
        Invoke-WebRequest "https://dot.net/v1/dotnet-install.sh" -OutFile "${env:APPVEYOR_BUILD_FOLDER}/../dotnet-install.sh"
        Set-Location "${env:APPVEYOR_BUILD_FOLDER}/.."
        ./dotnet-install.sh -Channel ${env:DOTNET_VERSION} -Quality ${env:DOTNET_RELEASE_STATUS} -InstallDir "${env:APPVEYOR_BUILD_FOLDER}/../dotnet"

        # Create Directories
        New-Item "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/${env:XIRORIG_NATIVE_OUTPUT_ROOT}/${env:XIRORIG_NATIVE_INSTALL_ROOT}" -ItemType Directory -Force

        # Download Xirorig Native Files
        Get-AppVeyorArtifacts ${env:APPVEYOR_ACCOUNT_NAME} ${env:APPVEYOR_PROJECT_NAME} -Token ${env:APPVEYOR_TOKEN_KEY} -JobName "Build Xirorig_Native MacOS" -Path ${env:APPVEYOR_BUILD_FOLDER}
        7z x "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/${env:XIRORIG_NATIVE_OUTPUT_ROOT}/${env:XIRORIG_NATIVE_INSTALL_ROOT}/${env:XIRORIG_NATIVE_ARTIFACT_NAME}"
        break
    }
}
