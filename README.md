# Verity: The Fiscal Truth Engine

![Build](https://github.com/ravendb/samples-verity/actions/workflows/build.yml/badge.svg)

## Overview

**Verity solves the problem of financial reporting by turning a messy, manual document process into a workflow.** Instead of treating quarterly filings and annual disclosures as static files passed around for review, the app uses RavenDB to keep the documents, their history, and their analysis in one place. That means teams can track how a filing evolved, preserve an audit trail, and work from a single source of truth when reviewing high-stakes financial narratives.

**The application also demonstrates the power of declarative usage of AI tools.** Filings are fetched from SEC EDGAR, then processed automatically through RavenDB’s GenAI tasks. We show how easily you can handle files bigger that the context size window and still reason about the content and extract the data that you need. 

To fully **embed itself into Microsoft Azure ecosystem**, Verity delegates the filling payload handling to the Azure Storage, using the RavenDB's Remote Attachments. On top of this, it uses Azure Storage Queues with RavenDB's ETL capabilities to show that easily build up a data-centric hub to support communication with other parts of your platform. RavenDB's native Data Subscriptions are used to support a command line terminal, showing capability to react to documents being changed in real time.

**Finally, Verity highlights how RavenDB supports trustworthy, long-lived reporting workflows through built-in governance features.** The sample uses revisions to preserve audit history, archival and expiration to control document lifecycle, and AI agents to help auditors review filings in a structured way. The point is not just storing a report, but making that report defensible: keeping historical versions, managing retention automatically, and enabling AI assistance inside a controlled system of record rather than as an isolated add-on.

**Built with RavenDB, .NET Aspire, Azure Storage, Azure Functions, AI integration, and a modern frontend.**

<img width="3747" height="1837" alt="image" src="https://github.com/user-attachments/assets/87bcc8c7-3c86-458f-b6f3-033ce40da916" />


## Features used

The following RavenDB features are used to build the application:

1. AI Capabilities
   1. [GenAI tasks](https://docs.ravendb.net/7.2/ai-integration/gen-ai-integration/start) - process documents as they are added or modified by sending them to an AI model
   1. [AI Agents](https://docs.ravendb.net/7.2/ai-integration/ai-agents/start) - server-side components that act as secure proxies between RavenDB clients and AI models
1. Clould Capabilities
   1. [Remote Attachments](https://docs.ravendb.net/7.2/document-extensions/attachments/store-attachments/store-attachments-remote) - stored in external storage such as Amazon S3 or Azure
   1. [Azure Storage Queues ETL](https://docs.ravendb.net/7.1/server/ongoing-tasks/etl/queue-etl/azure-queue) - microsoft Azure service for the storage and retrieval of large numbers of messages
1. [Revisions](https://docs.ravendb.net/7.2/document-extensions/revisions/overview) - snapshots of documents and their extensions
1. [Data Subscriptions](https://docs.ravendb.net/7.2/client-api/data-subscriptions/what-are-data-subscriptions) - server sends batches of documents to the client
1. [Hub/Sink Replication](https://docs.ravendb.net/7.2/studio/database/tasks/ongoing-tasks/hub-sink-replication/overview) - maintain a live replica of a database or a chosen part of it
1. [Document Expiration](https://docs.ravendb.net/7.2/studio/database/settings/document-expiration) - documents can be scheduled for deletion
1. [Data Archival](https://docs.ravendb.net/7.2/data-archival/start) - Documents are compressed, excluded from indexes and subscriptions but still present and retrievable

## Technologies

The following technogies were used to build this application:

1. RavenDB 7.2
1. .NET 10
1. Aspire
1. Node.js 22
1. Svelte

## Local setup
A few steps are required to run the application locally.

1. Check out the GIT repository
1. Install prerequisites:
   1. [.NET 10.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
   1. [Node.js 22.x](https://nodejs.org/en/download)
   1. [Aspire](https://aspire.dev)
   1. [Azure Functions CLI](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
1. Use `aspire run` to run the application
   1. When running for the first time, `Aspire` will ask you to fill some parameters:
      1. UserAgent: your nick and email, eg. "UserAgent": "John john@mail.web"
      1. ApiKey: your OpenAI api key
      1. AzureStorage: your Azure storage connection string
      1. RavenDB License: your license which you can get here [RavenDB download](https://ravendb.net/download)

## Remarks

When adding a company, the user should select U.S. based companies, as foreign companies do not have 10-K and 10-Q reports, which the application focuses on.

The sink does not replicate subscriptions from the hub. Subscriptions are created only when the subscription display application (running on the sink) is started, at which point they are initialized for all existing companies. When a new company is added on the hub, a subscription is created immediately. On the sink, however, the subscription is only created after the application refreshes the company list. Until this happens, there is no active subscription for that company on the sink, which creates a potential gap where change notifications may be missed.

## Community & Support

If you spot a bug, have an idea or a question, please let us know by rasing an issue or creating a pull request. 

We do use a [Discord server](https://discord.gg/ravendb). If you have any doubts, don't hesistate to reach out!

## Contributing

We encourage you to contribute! Please read our [CONTRIBUTING](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed with the [MIT license](LICENSE).
