﻿#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/IllustrationViewDataProvider.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Pixeval.CoreApi.Engine;
using Pixeval.CoreApi.Model;
using Pixeval.UserControls;

namespace Pixeval;

public interface IIllustrationViewDataProvider
{
    public AdvancedCollectionView IllustrationsView { get; }

    public ObservableCollection<IllustrationViewModel> IllustrationsSource { get; }

    public IFetchEngine<Illustration?>? FetchEngine { get; }

    void DisposeCurrent();

    bool Sortable { get; }

    public ObservableCollection<IllustrationViewModel> SelectedIllustrations { get; }

    Task<bool> FillAsync(int? itemsLimit = null);

    Task FillAsync(IFetchEngine<Illustration?>? fetchEngine, int? itemLimit = null);

    Task<bool> ResetAndFillAsync(IFetchEngine<Illustration?>? fetchEngine, int? itemLimit = null);
}