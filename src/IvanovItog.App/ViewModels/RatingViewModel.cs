using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared.Dtos;

namespace IvanovItog.App.ViewModels;

public partial class RatingViewModel : ObservableObject
{
    private readonly IRatingService _ratingService;

    public ObservableCollection<TechnicianRatingDto> Ratings { get; } = new();
    public ObservableCollection<MedalEntry> TopThreeMedals { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadCommand { get; }

    public RatingViewModel(IRatingService ratingService)
    {
        _ratingService = ratingService;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Ratings.Clear();
            TopThreeMedals.Clear();
            var ratings = await _ratingService.GetRatingsAsync();
            foreach (var rating in ratings)
            {
                Ratings.Add(rating);
            }

            var medals = new[] { "🥇", "🥈", "🥉" };
            var top = ratings.Take(3).ToList();
            for (var i = 0; i < top.Count; i++)
            {
                TopThreeMedals.Add(new MedalEntry(medals[i], top[i]));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public record MedalEntry(string Medal, TechnicianRatingDto Technician);
}
