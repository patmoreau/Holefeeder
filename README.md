# Holefeeder

[![Continuous Integration](https://github.com/DrifterApps/Holefeeder/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/DrifterApps/Holefeeder/actions/workflows/ci-cd.yml)
[![CodeQL](https://github.com/patmoreau/Holefeeder/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/patmoreau/Holefeeder/actions/workflows/codeql-analysis.yml)
[![Linter](https://github.com/patmoreau/Holefeeder/actions/workflows/linter.yml/badge.svg)](https://github.com/patmoreau/Holefeeder/actions/workflows/linter.yml)
![Test Results](https://gist.githubusercontent.com/patmoreau/230a77d0ee69381bbe869c0fc62f9cc6/raw/holefeeder-tests-badge.svg)
[![Mutation testing badge](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fpatmoreau%2FHolefeeder%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/patmoreau/Holefeeder/main)

## Introduction

The main purpose of this repository is to showcase what I can do. This is also a constant work in progress to learn new designs, architecture, technologies where my main goal is to be the most efficient in my craft and offering the best/simple solutions for the job.

The more I study Domain Driven Design, the more I realise that this concept answers a lot of issues I've had building software over the years.

Here are some of the key concepts and articles which drives my thinking today:

1. [Domain Driven Design](https://martinfowler.com/tags/domain%20driven%20design.html)
1. [Event Sourcing](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
1. [Cloud Design Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/)

## Requirements

You will need the following:

1. [Docker](https://www.docker.com/)
1. [.Net 8](https://dotnet.microsoft.com/en-us/download)

## Getting ready

### install report generator

```bash
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
```

### generate code coverage report

Run task 'code coverage'
