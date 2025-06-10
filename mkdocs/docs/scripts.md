# Shared Scripts Glossary

The `scripts` directory contains a set of convenience [Powershell](https://learn.microsoft.com/en-us/powershell/) scripts to make developers' lives easier when working in this repo.

!!! tip "Further Development"
    Any such scripts moving forward must, if possible, be written in Powershell.  This is so that the scripts can be executed in any environment, regardless of platform.

## `configure-local`

The `configure-local.ps1` script is a "shortcut" script for setting up a machine for local development.  It requires [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/).

??? note
    The winget command line tool is only supported on Windows 10 1709 (build 16299) or later at this time. The winget tool will not be available until you have logged into Windows as a user for the first time, triggering Microsoft Store to register Windows Package Manager as part of an asynchronous process. If you have recently logged in as a user for the first time and find that winget is not yet available, you can open PowerShell and enter the following command to request this winget registration: `Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe`.

If a manual setup is preferred, refer to [this guide](/dev-machine-setup).

## Docker Scripts

There are also several scripts for managing the docker containers used for local development.  They are enumerated and explained [here](/docker#local-development).
