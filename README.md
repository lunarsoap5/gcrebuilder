# GameCube Rebuilder

## Description

GCR is a tool that allows you to edit Nintendo GameCube images.

## Building

GCR can be built via any C# IDE or a CLI command like `msbuild GCRebuilder.sln`

## Usage

- Export file/folder from image: `--extract iso_path folder_path`
- Import file/folder from image: `--import iso_path folder_path`
- Rebuild image from folder: `--rebuild folder_path iso_path`
  - You can specify whether or not to use the `game.toc` file by adding `--noGameTOC` to the end of the rebuild command

## Recognition

Special thanks to BSV for creating the original tool.

