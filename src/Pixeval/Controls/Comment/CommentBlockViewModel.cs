#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/CommentBlockViewModel.cs
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

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using Pixeval.AppManagement;
using Pixeval.CoreApi.Model;
using Pixeval.Misc;
using Pixeval.Util.IO;
using Pixeval.Util.UI;
using Pixeval.Utilities;

namespace Pixeval.Controls;

public partial class CommentBlockViewModel(Comment comment, long illustrationId) : ObservableObject, IDisposable
{
    public const string AddCommentUrlSegment = "/v1/illust/comment/add";

    public long IllustrationId { get; } = illustrationId;

    public Comment Comment { get; } = comment;

    public bool HasReplies => Comment.HasReplies;

    [MemberNotNullWhen(true, nameof(StampSource))]
    public bool IsStamp => Comment.CommentStamp is not null;

    public string? StampSource => Comment.CommentStamp?.StampUrl;

    public string PostDate => Comment.Date.ToString("yyyy-MM-dd dddd");

    public string Poster => Comment.CommentPoster.Name;

    public long PosterId => Comment.CommentPoster.Id;

    public string CommentContent => Comment.CommentContent;

    public bool IsMe => PosterId == App.AppViewModel.PixivUid;

    public long CommentId => Comment.Id;

    [ObservableProperty]
    private SoftwareBitmapSource _avatarSource = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepliesIsNotNull), nameof(RepliesCount), nameof(RepliesAppBarButtonStyleIsNotNull))]
    private ObservableCollection<CommentBlockViewModel>? _replies;

    public bool RepliesIsNotNull => Replies is not null;

    private static Style DefaultAppBarButtonStyle => (Style)Application.Current.Resources[$"Default{nameof(AppBarButton)}Style"];

    private static readonly Style _noRepliesStyle = new()
    {
        BasedOn = DefaultAppBarButtonStyle,
        TargetType = typeof(AppBarButton),
        Setters =
        {
            new Setter(FrameworkElement.WidthProperty, Application.Current.Resources["CollapsedAppBarButtonWidth"]),
            new Setter(AppBarButton.LabelPositionProperty, CommandBarLabelPosition.Collapsed)
        }
    };

    public Style RepliesAppBarButtonStyleIsNotNull => RepliesIsNotNull ? DefaultAppBarButtonStyle : _noRepliesStyle;

    public string? RepliesCount => Replies?.Count.ToString();

    public async Task LoadRepliesAsync()
    {
        Replies = await App.AppViewModel.MakoClient.IllustrationCommentReplies(CommentId)
            .Select(c => new CommentBlockViewModel(c, IllustrationId))
            .ToObservableCollectionAsync();
    }

    public async Task LoadAvatarSource()
    {
        var result = await App.AppViewModel.MakoClient.DownloadSoftwareBitmapSourceAsync(Comment.CommentPoster.ProfileImageUrls.Medium);
        AvatarSource = result is Result<SoftwareBitmapSource>.Success { Value: var avatar }
            ? avatar
            : await AppInfo.GetPixivNoProfileImageAsync();
    }

    public void AddComment(Comment comment)
    {
        Replies ??= [];

        Replies.Insert(0, new CommentBlockViewModel(comment, IllustrationId));
    }

    public async Task<Paragraph> GetReplyContentParagraphAsync()
    {
        var paragraph = new Paragraph();
        foreach (var replyContentToken in ReplyEmojiHelper.EnumerateTokens(CommentContent))
        {
            switch (replyContentToken)
            {
                case ReplyContentToken.TextToken(var content):
                    paragraph.Inlines.Add(new Run
                    {
                        Text = content
                    });
                    break;
                case ReplyContentToken.EmojiToken(var emoji) when await App.AppViewModel.MakoClient.DownloadStreamAsync(emoji.GetReplyEmojiDownloadUrl()) is Result<Stream>.Success(var emojiSource):
                    paragraph.Inlines.Add(new InlineUIContainer
                    {
                        Child = new Image
                        {
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Source = await emojiSource.GetBitmapImageAsync(true, 14),
                            Width = 14,
                            Height = 14
                        }
                    });

                    break;
            }
        }

        return paragraph;
    }

    public void Dispose()
    {
        AvatarSource?.Dispose();
        Replies?.ForEach(r => r.Dispose());
    }
}