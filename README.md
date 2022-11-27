# GameCube Rebuilder - Console Branch

## Description

GCR is a tool that allows you to edit Nintendo GameCube images. The console branch does not utilize a GUI and works for Mac OS and Linux Operating Systems

## Running

You will need to install the latest version of .NET Core which can be found here: https://dotnet.microsoft.com/en-us/download
Once it is installed, you can run the project by running the following command in the `GCRebuilder` folder: 
`dotnet run --project ./GCRebuilder_Console.csproj <args>`

## Usage

- Export file/folder from image: `--extract iso_path folder_path`
- Import file/folder from image: `--import iso_path folder_path`
- Rebuild image from folder: `--rebuild folder_path iso_path`
  - You can specify whether or not to use the `game.toc` file by adding `--noGameTOC` to the end of the rebuild command

## Recognition

Special thanks to BSV for creating the original tool.

