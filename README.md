# Verity: The Fiscal Truth Engine

![Build](https://github.com/ravendb/samples-verity/actions/workflows/build.yml/badge.svg)

## Overview

Preparing a Quarterly Financial Report or an Annual ESG Disclosure is a chaotic process involving dozens of versions, legal reviews, and strict regulatory compliance. **Verity** is the hub for these decisions to happen. It doesn't just store the PDF; it understands the narrative, tracks all the tables, and uses AI to ensure the CEO’s statement in Q2 matches the financial reality of Q1.

Financial reporting is no longer about just storing files; it's about defending the narrative. And with Verity, based on [RavenDB](https://ravendb.net), we make it real.

## TBD - Features used

The following RavenDB features are used to build the application:

1. [Vector Search](https://docs.ravendb.net/7.1/ai-integration/vector-search/ravendb-as-vector-database) - RavenDB has a built-in vector database 
1. [Document Refresh](https://docs.ravendb.net/7.1/studio/database/settings/document-refresh) - a scheduled update of selected documents
1. [Azure Storage Queues ETL](https://docs.ravendb.net/7.1/server/ongoing-tasks/etl/queue-etl/azure-queue) - Azure Storage Queues integration

## TBD - Technologies

The following technogies were used to build this application:

1. RavenDB 7.2
1. .NET 10
1. Node.js 22
1. Azure functions 4
1. Angular 14

## TBD - Run locally

If you want to run the application locally, please follow the steps:

1. Check out the GIT repository
1. Install prerequisites:
   1. [.NET 10.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
   1. [Node.js 22.x](https://nodejs.org/en/download)
1. Get yourself Raven Licensce and OpenAI API key
1. Get the app running by opening command prompt in Folder src/RavenDB.Samples.Verity.AppHost and executing aspire run

## Community & Support

If you spot a bug, have an idea or a question, please let us know by rasing an issue or creating a pull request. 

We do use a [Discord server](https://discord.gg/ravendb). If you have any doubts, don't hesistate to reach out!

## Contributing

We encourage you to contribute! Please read our [CONTRIBUTING](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed with the [MIT license](LICENSE).
