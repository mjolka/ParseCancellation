using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ParseCancellation.Services;

namespace ParseCancellation
{
    public partial class MainWindow : Window
    {
        private readonly ParserService _parser = new ParserService();

        // For debugging.
        private int _parseRequestId;

        private CancellationTokenSource _cancellationTokenSource;

        private Task _parseTask;

        public MainWindow()
        {
            InitializeComponent();
            Messages.ItemsSource = ParserMessages;
        }

        private ObservableCollection<ParseMessage> ParserMessages { get; } = new ObservableCollection<ParseMessage>();

        private async void Parse(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _parseTask = ParseTaskHelper(_cancellationTokenSource.Token);
            try
            {
                await _parseTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ParseTaskHelper(CancellationToken cancellationToken)
        {
            try
            {
                if (_parseTask != null)
                {
                    await _parseTask;
                }
            }
            catch (OperationCanceledException)
            {
            }

            var parseRequestId = Interlocked.Increment(ref _parseRequestId);

            cancellationToken.ThrowIfCancellationRequested();

            // The Progress<T> constructor captures the current synchronization context,
            // so we have to declare `progress` outside of the action passed to the
            // Task constructor.
            IProgress<string> progress = new Progress<string>(message =>
            {
                var item = new ParseMessage(parseRequestId, DateTime.Now, message);
                ParserMessages.Add(item);
                Messages.ScrollIntoView(item);
            });

            try
            {
                // Parse is a long-running, blocking, CPU-bound operation.
                // Offload the work to the thread pool to keep the UI responsive.
                await Task.Run(() => _parser.Parse(new[] {"abc", "xy"}, _cancellationTokenSource.Token, progress), _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                progress.Report("\u274C Canceled");
            }
        }

        private class ParseMessage
        {
            public ParseMessage(int parseRequestId, DateTime timestamp, string message)
            {
                ParseRequestId = parseRequestId;
                Timestamp = timestamp.ToString("T");
                Message = message;
            }

            public int ParseRequestId { get; }

            public string Timestamp { get; }

            public string Message { get; }
        }

        private async void ParseALot(object sender, RoutedEventArgs e)
        {
            // Simulate a bunch of button clicks to see if any exceptions
            // are uncaught.
            var synchronizationContext = SynchronizationContext.Current;
            await Task.Run(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    synchronizationContext.Post(_ => { Parse(null, null); }, null);
                }
            });
        }
    }
}
