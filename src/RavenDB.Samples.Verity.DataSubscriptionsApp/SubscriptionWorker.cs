using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions.Database;
using RavenDB.Samples.Verity.Model;
using RavenDB.Samples.Verity.Model.Subscriptions;
using Spectre.Console;

namespace RavenDB.Samples.Verity.DataSubscriptionsApp;

internal sealed class SubscriptionWorker(IDocumentStore store, IHostApplicationLifetime lifetime, ILogger<SubscriptionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
                //AnsiConsole.Clear();
                lifetime.StopApplication();
                return;
            }
            catch (DatabaseDoesNotExistException)
            {
                AnsiConsole.MarkupLine("[yellow]Database not ready, retrying in 5s...[/]");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {   
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold green]Verity Data Subscriptions[/]").RuleStyle("green"));

        while (!stoppingToken.IsCancellationRequested)
        {
            List<Company> companies;
            using (var session = store.OpenAsyncSession())
            {
                companies = await session.Query<Company>()
                                         .OrderByDescending(c => c.Sic)
                                         .ThenBy(c => c.Name)
                                         .ToListAsync(stoppingToken);
            }

            if (companies.Count == 0)
            {
                await WaitForFirstCompanyAsync(stoppingToken);
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[bold green]Verity Data Subscriptions[/]").RuleStyle("green"));
                continue;
            }

            var company = SelectCompany(companies);
            if (company is null)
                break;

            await EnsureSubscriptionExistsAsync(company, stoppingToken);

            AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(company.Name)}[/]").RuleStyle("cyan"));
            AnsiConsole.MarkupLine("[dim]Press [[C]] to change company, [[Q]] to quit.[/]");
            AnsiConsole.MarkupLine($"[blue]Watching [bold]{Markup.Escape(company.Name)}[/] for reports analyzed by AI...[/]");

            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var workerTask = RunWorkerAsync(company, sessionCts.Token);
            var keyTask    = WaitForChangeKeyAsync(sessionCts.Token);

            await Task.WhenAny(workerTask, keyTask);
            await sessionCts.CancelAsync();

            ConsoleKey key;
            try
            {
                key = await keyTask;
            }
            catch (OperationCanceledException)
            {
                key = ConsoleKey.Escape;
            }

            try { await workerTask; } catch (OperationCanceledException) { }

            if (key == ConsoleKey.Q || stoppingToken.IsCancellationRequested)
                break;

            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold green]Verity Data Subscriptions[/]").RuleStyle("green"));
        }
    }

    private static readonly Company QuitSentinel = new() { Id = "__quit__", Name = "[red]Quit[/]" };

    private static Company? SelectCompany(List<Company> companies)
    {
        var choices = companies.Append(QuitSentinel).ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<Company>()
                .Title("[yellow]Select a company to watch for new reports:[/]")
                .PageSize(11)
                .UseConverter(c => c.Name)
                .AddChoices(choices));

        return selected == QuitSentinel ? null : selected;
    }

    private async Task WaitForFirstCompanyAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[yellow]No companies in database. Waiting for data to be seeded...[/]");

        var appeared = new TaskCompletionSource();
        var worker = store.Subscriptions.GetSubscriptionWorker<Company>(
            new SubscriptionWorkerOptions("Companies-Watch")
            {
                Strategy = SubscriptionOpeningStrategy.WaitForFree
            });

        try
        {
            var workerTask = worker.Run(batch =>
            {
                if (batch.Items.Count > 0)
                    appeared.TrySetResult();
            }, ct);

            await Task.WhenAny(appeared.Task, workerTask);
        }
        catch (OperationCanceledException) { }
        catch (IOException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Subscription worker failed waiting for companies");
        }
        finally
        {
            await worker.DisposeAsync();
        }
    }

    private async Task EnsureSubscriptionExistsAsync(Company company, CancellationToken ct)
    {
        try
        {
            await NewReportsSubscription.Create(store, company.Name, company.Id);
            AnsiConsole.MarkupLine($"[dim]Created subscription for {Markup.Escape(company.Name)}[/]");
        }
        catch (Exception)
        {
            // Subscription already exists or cannot be created — worker will connect to existing one
        }
    }

    private static Task<ConsoleKey> WaitForChangeKeyAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key is ConsoleKey.C or ConsoleKey.Q)
                        return key;
                }
                Thread.Sleep(100);
            }
            return ConsoleKey.Escape;
        }, CancellationToken.None);
    }

    private async Task RunWorkerAsync(Company company, CancellationToken ct)
    {
        var subscriptionName = $"New-Reports-{company.Name}";
        var worker = store.Subscriptions.GetSubscriptionWorker<ReportNotification>(
            new SubscriptionWorkerOptions(subscriptionName)
            {
                Strategy = SubscriptionOpeningStrategy.WaitForFree
            });

        try
        {
            await worker.Run(batch =>
            {
                foreach (var item in batch.Items)
                {
                    var n = item.Result;
                    AnsiConsole.MarkupLine(
                        $"[cyan]Analyzed:[/] " +
                        $"[green]{Markup.Escape(n.Filing)}[/] " +
                        $"[dim]({Markup.Escape(n.AccessionNumber)})[/]");
                }
            }, ct);
        }
        catch (OperationCanceledException) { }
        catch (IOException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Subscription worker failed for {Company}", company.Name);
        }
        finally
        {
            await worker.DisposeAsync();
        }
    }
}
