# Verity: The Fiscal Truth Engine

![Build](https://github.com/ravendb/samples-verity/actions/workflows/build.yml/badge.svg)

## Overview

Preparing a Quarterly Financial Report or an Annual ESG Disclosure is a chaotic process involving dozens of versions, legal reviews, and strict regulatory compliance. **Verity** is the hub for these decisions to happen. It doesn't just store the PDF; it understands the narrative, tracks all the tables, and uses AI to ensure the CEO’s statement in Q2 matches the financial reality of Q1.

Financial reporting is no longer about just storing files; it's about defending the narrative. And with Verity, based on [RavenDB](https://ravendb.net), we make it real.

## TBD - Features used

The following RavenDB features are used to build the application:

1. [Remote Attachments](https://docs.ravendb.net/7.2/document-extensions/attachments/store-attachments/store-attachments-remote) - stored in external storage such as Amazon S3 or Azure
1. [GenAI tasks](https://docs.ravendb.net/7.2/ai-integration/gen-ai-integration/start) - process documents as they are added or modified by sending them to an AI model
1. [Revisions](https://docs.ravendb.net/7.2/document-extensions/revisions/overview) - snapshots of documents and their extensions
1. [AI Agents](https://docs.ravendb.net/7.2/ai-integration/ai-agents/start) - server-side components that act as secure proxies between RavenDB clients and AI models
1. [Azure Storage Queues ETL](https://docs.ravendb.net/7.1/server/ongoing-tasks/etl/queue-etl/azure-queue) - microsoft Azure service for the storage and retrieval of large numbers of messages
1. [Data Subscriptions](https://docs.ravendb.net/7.2/client-api/data-subscriptions/what-are-data-subscriptions) - server sends batches of documents to the client
1. [Hub/Sink Replication](https://docs.ravendb.net/7.2/studio/database/tasks/ongoing-tasks/hub-sink-replication/overview) - maintain a live replica of a database or a chosen part of it
1. [Document Expiration](https://docs.ravendb.net/7.2/studio/database/settings/document-expiration) - documents can be scheduled for deletion
1. [Data Archival](https://docs.ravendb.net/7.2/data-archival/start) - Documents are compressed, excluded from indexes and subscriptions but still present and retrievable

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
1. Get yourself Raven Licensce, OpenAI API key and Azure storage connection string
1. Edit app settings in App Host folder with above and your nick and email in fields CommandKey and UserAgent, user agent field should look like "UserAgent": "John john@mail.web"

## Community & Support

If you spot a bug, have an idea or a question, please let us know by rasing an issue or creating a pull request. 

We do use a [Discord server](https://discord.gg/ravendb). If you have any doubts, don't hesistate to reach out!

## Contributing

We encourage you to contribute! Please read our [CONTRIBUTING](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed with the [MIT license](LICENSE).
