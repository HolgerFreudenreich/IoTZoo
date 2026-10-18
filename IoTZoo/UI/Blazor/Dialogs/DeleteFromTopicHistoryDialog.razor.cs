// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Dialogs;

public class DeleteFromTopicHistoryDialogBase : ComponentBase
{
    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public List<string> Topics { get; set; } = new();

    protected string? SelectedTopic { get; set; }

    protected DateTime? FromDate { get; set; } = DateTime.Today;

    protected TimeSpan? FromTime { get; set; } = TimeSpan.Zero;

    protected DateTime? ToDate { get; set; } = DateTime.Today;

    protected TimeSpan? ToTime { get; set; } = TimeSpan.FromDays(1).Subtract(TimeSpan.FromSeconds(1));

    protected DateTime From => (FromDate ?? DateTime.Today) + (FromTime ?? TimeSpan.Zero);

    protected DateTime To => (ToDate ?? DateTime.Today) + (ToTime ?? TimeSpan.Zero);

    protected bool CanDelete => FromDate.HasValue && ToDate.HasValue && From <= To;

    protected void Confirm()
    {
        MudDialog.Close(DialogResult.Ok(new DeleteRange(SelectedTopic, From, To)));
    }

    protected void Cancel()
    {
        MudDialog.Cancel();
    }

    public record DeleteRange(string? Topic, DateTime From, DateTime To);
}
