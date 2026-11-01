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


using Domain.Pocos;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public partial class TopicLinkRow : ComponentBase
{
    [Parameter, EditorRequired]
    public TopicLink TopicLinks { get; set; } = null!;

    [Parameter]
    public EventCallback<TopicLink> OnRemove { get; set; }

    [Parameter]
    public EventCallback<string> OnEdit { get; set; }
}
