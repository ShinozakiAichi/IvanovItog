using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.Domain.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace IvanovItog.App.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly string _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "analytics.log");

    public ObservableCollection<ISeries> StatusSeries { get; } = new();
    public ObservableCollection<ISeries> TimelineSeries { get; } = new();
    public ObservableCollection<ISeries> LoadSeries { get; } = new();

    public Axis[] TimelineXAxis { get; private set; } = Array.Empty<Axis>();
    public Axis[] LoadXAxis { get; private set; } = Array.Empty<Axis>();

    [ObservableProperty]
    private DateTime _from = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _to = DateTime.Today;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadCommand { get; }

    public AnalyticsViewModel(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);

        // лог старта
        Log("=== AnalyticsViewModel создан ===");
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Log("=== Загрузка аналитики началась ===");

            ClearSeries();
            Log($"Очистили все серии. Даты: {From:dd.MM.yyyy} - {To:dd.MM.yyyy}");

            var (fromLocal, toLocal, normalizedFrom, normalizedTo) = NormalizeRange(From, To);

            Log($"Диапазон после нормализации: {normalizedFrom:dd.MM.yyyy} — {normalizedTo:dd.MM.yyyy}");

            // --- Timeline ---
            var timelinePoints = (await _analyticsService.GetRequestsTimelineAsync(fromLocal, toLocal)).OrderBy(p => p.Date).ToList();
            Log($"Timeline получен: {timelinePoints.Count} точек");

            if (!timelinePoints.Any())
            {
                Log("Timeline пуст. Пробуем fallback...");
                var fallbackTimeline = (await _analyticsService.GetRequestsTimelineAsync(DateTime.MinValue, DateTime.MaxValue))
                    .OrderBy(p => p.Date).ToList();

                Log($"Fallback timeline: {fallbackTimeline.Count} точек");

                if (fallbackTimeline.Any())
                {
                    var fallbackFrom = fallbackTimeline.First().Date.Date;
                    var fallbackTo = fallbackTimeline.Last().Date.Date;

                    From = fallbackFrom;
                    To = fallbackTo;
                    (fromLocal, toLocal, _, _) = NormalizeRange(From, To);
                    timelinePoints = fallbackTimeline;
                    Log($"Переназначили диапазон: {From:dd.MM.yyyy} — {To:dd.MM.yyyy}");
                }
            }

            if (timelinePoints.Any())
            {
                foreach (var p in timelinePoints)
                    Log($"Дата {p.Date:dd.MM}: {p.Count}");
                TimelineSeries.Add(new LineSeries<int>
                {
                    Values = timelinePoints.Select(p => p.Count).ToArray(),
                    GeometrySize = 8,
                    Fill = null
                });

                TimelineXAxis = new Axis[]
                {
                    new Axis
                    {
                        Labels = timelinePoints.Select(p => p.Date.ToString("dd.MM")).ToList(),
                        LabelsRotation = 15
                    }
                };

                Log("TimelineSeries успешно заполнен.");
            }

            OnPropertyChanged(nameof(TimelineXAxis));

            // --- Status ---
            var statusData = (await _analyticsService.GetRequestsByStatusAsync(fromLocal, toLocal)).ToList();
            Log($"StatusData получено: {statusData.Count} записей");

            foreach (var status in statusData)
            {
                StatusSeries.Add(new PieSeries<int>
                {
                    Values = new int[] { status.Count },
                    Name = status.Status,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                });
                Log($"Добавлен статус: {status.Status} ({status.Count})");
            }

            // --- Load ---
            var loads = (await _analyticsService.GetTechnicianLoadAsync(fromLocal, toLocal)).ToList();
            Log($"Load получено: {loads.Count} техников");

            if (loads.Any())
            {
                LoadSeries.Add(new ColumnSeries<int>
                {
                    Values = loads.Select(l => l.ActiveRequests).ToArray(),
                    Name = "Активные"
                });

                if (loads.Any(l => l.ClosedRequests > 0))
                {
                    LoadSeries.Add(new ColumnSeries<int>
                    {
                        Values = loads.Select(l => l.ClosedRequests).ToArray(),
                        Name = "Завершённые"
                    });
                }

                LoadXAxis = new Axis[]
                {
                    new Axis
                    {
                        Labels = loads.Select(l => l.TechnicianName).ToList(),
                        LabelsRotation = 15
                    }
                };

                Log("LoadSeries успешно заполнен.");
            }

            OnPropertyChanged(nameof(LoadXAxis));

            Log("=== Загрузка аналитики завершена ===");
        }
        catch (Exception ex)
        {
            Log($"[ОШИБКА] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Console.WriteLine(line);
        File.AppendAllText(_logFile, line + Environment.NewLine);
    }

    partial void OnIsBusyChanged(bool value) => LoadCommand.NotifyCanExecuteChanged();

    private void ClearSeries()
    {
        StatusSeries.Clear();
        TimelineSeries.Clear();
        LoadSeries.Clear();
        TimelineXAxis = Array.Empty<Axis>();
        LoadXAxis = Array.Empty<Axis>();
        OnPropertyChanged(nameof(TimelineXAxis));
        OnPropertyChanged(nameof(LoadXAxis));
    }

    private static (DateTime fromLocal, DateTime toLocal, DateTime normalizedFrom, DateTime normalizedTo) NormalizeRange(DateTime from, DateTime to)
    {
        var normalizedFrom = from.Date;
        var normalizedTo = to.Date;

        if (normalizedFrom > normalizedTo)
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);

        var fromLocal = normalizedFrom;
        var toLocal = normalizedTo.AddDays(1).AddTicks(-1);

        return (fromLocal, toLocal, normalizedFrom, normalizedTo);
    }
}
