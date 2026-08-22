// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Web.Client.Services.Notifications;

public record NotificationMessage(
    string Id,
    string Title,
    string Except,
    string Category,
    DateTime PublishDate,
    string ImgUrl,
    IEnumerable<NotificationAuthor> Authors,
    Type ContentComponent);