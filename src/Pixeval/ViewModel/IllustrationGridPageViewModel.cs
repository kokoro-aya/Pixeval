﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mako.Engine;
using Mako.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Pixeval.ViewModel
{
    public class IllustrationGridPageViewModel : ObservableObject
    {
        public IFetchEngine<Illustration?>? FetchEngine { get; set; }

        public ObservableCollection<IllustrationViewModel> Illustrations { get; }

        public ObservableCollection<IllustrationViewModel> SelectedIllustrations { get; }


        private bool _isAnyIllustrationSelected;

        public bool IsAnyIllustrationSelected
        {
            get => _isAnyIllustrationSelected;
            set => SetProperty(ref _isAnyIllustrationSelected, value);
        }

        public IllustrationGridPageViewModel()
        {
            SelectedIllustrations = new ObservableCollection<IllustrationViewModel>();
            Illustrations = new ObservableCollection<IllustrationViewModel>();
        }

        public async Task Fill()
        {
            // TODO: The cache system should be take into our considerations
            // The cache system MIGHT improve the fluency of the UI, but it may
            // also not worth the efforts
            await foreach (var illustration in FetchEngine!)
            {
                // TODO: Use a global static object (by using IoC or a static property)
                // to manage all the filter options, such as minimum bookmark, maximum
                // images that are allowed to be loaded at the same time, required tags
                // and excluded tags, etc.
                //
                // if (FetchEngine.RequestedPages >= 20)
                // {
                //     FetchEngine.EngineHandle.Cancel();
                // }
                if (illustration is not null)
                {
                    var i = new IllustrationViewModel(illustration);
                    i.OnIsSelectedChanged += (_, model) =>
                    {
                        if (model.IsSelected)
                        {
                            SelectedIllustrations.Add(model);
                        }
                        else
                        {
                            SelectedIllustrations.Remove(model);
                        }
                        IsAnyIllustrationSelected = SelectedIllustrations.Any();
                    };
                    Illustrations.Add(i);
                }
            }
        }

        public async Task Fill(IFetchEngine<Illustration?>? newEngine)
        {
            FetchEngine = newEngine;
            await Fill();
        }
    }
}