using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace CrossPlatformUI.ViewModels.Tabs;

[RequiresUnreferencedCode("ReactiveUI uses reflection")]
public class ItemsViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    public MainViewModel Main { get; }

    public IObservable<bool> QuestItemChainsAllowedIncludedObservable { get; }
    public IObservable<bool> TownQuestLocationsAreMinorItemsIncludedObservable { get; }

    public ItemsViewModel(MainViewModel main)
    {
        Main = main;
        Activator = new();

        QuestItemChainsAllowedIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.questItemChainsAllowedIncluded())
            .DistinctUntilChanged();

        TownQuestLocationsAreMinorItemsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.townQuestLocationsAreMinorItemsIncluded())
            .DistinctUntilChanged();

        this.WhenActivated(OnActivate);
    }

    internal void OnActivate(CompositeDisposable disposables)
    {
    }
}
