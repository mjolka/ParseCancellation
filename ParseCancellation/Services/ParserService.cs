using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParseCancellation.Services
{
    internal class ParserService
    {
        public void Parse(IEnumerable<string> values, CancellationToken cancellationToken, IProgress<string> progress = null)
        {
            progress = progress ?? new Progress<string>(_ => { });

            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
            Parallel.ForEach(values, parallelOptions, value =>
            {
                var parseResult = ParseInternal(value, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ResolveReferences(parseResult, progress, cancellationToken);
            });

            progress.Report("Done");
        }

        private static ParseResult ParseInternal(string value, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report($"Parsing {value}...");

            SimulateWork(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report($"\u2714 Parsed {value}");

            return new ParseResult(value);
        }

        private static void ResolveReferences(ParseResult parseResult, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
            Parallel.ForEach(parseResult.Tree, parallelOptions, (c, _, index) =>
            {
                progress.Report($"Resolving refererence \"{c}\" of {parseResult.Tree}...");

                SimulateWork(cancellationToken, TimeSpan.FromSeconds(index + 1));
                cancellationToken.ThrowIfCancellationRequested();

                progress.Report($"\u2714 Resolved refererence \"{c}\" of {parseResult.Tree}");
            });
        }

        private static void SimulateWork(CancellationToken cancellationToken, TimeSpan? duration = null)
        {
            cancellationToken.WaitHandle.WaitOne(duration.GetValueOrDefault(TimeSpan.FromSeconds(1)));
        }

        private class ParseResult
        {
            public ParseResult(string tree)
            {
                Tree = tree;
            }

            public string Tree { get; }
        }
    }
}
