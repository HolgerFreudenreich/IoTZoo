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

using System.Text.Json;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using IotZoo.Dialogs;
using IotZoo.Pages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;

namespace UnitTests;

public class ProjectImportTests
{
    [Fact]
    public async Task ImportPreservesProjectJsonLongerThanMudBlazorDefault()
    {
        string description = new string('x', 524_288) + " end of project";
        string json = JsonSerializer.Serialize(new ProjectsPageBase.ProjectExport(
            new Project { ProjectName = "Large project", Description = description }, null, null, null));
        Assert.True(json.Length > 524_288);

        var projectService = new Mock<IProjectCrudService>();
        projectService.Setup(x => x.LoadProjectByName("Large project")).ReturnsAsync((Project?)null);
        var dialog = new Mock<IDialogReference>();
        dialog.SetupGet(x => x.Result).Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(json)));
        var dialogService = new Mock<IDialogService>();
        dialogService.Setup(x => x.ShowAsync<TextEditor>("Import Project", It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialog.Object);
        var snackbar = new Mock<ISnackbar>();
        var page = new TestProjectsPage(projectService.Object, dialogService.Object, snackbar.Object);

        await page.ImportAsync();

        projectService.Verify(x => x.Insert(It.Is<Project>(project =>
            project.ProjectName == "Large project" && project.Description == description)), Times.Once);
        snackbar.Verify(x => x.Add(It.Is<string>(message => message.Contains("is imported!")), Severity.Success,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    private sealed class TestProjectsPage : ProjectsPageBase
    {
        public TestProjectsPage(IProjectCrudService projectService, IDialogService dialogService, ISnackbar snackbar)
        {
            ProjectDatabaseService = projectService;
            DialogService = dialogService;
            Snackbar = snackbar;
            Logger = NullLogger<PageBase>.Instance;
        }

        public Task ImportAsync() => OpenProjectImportDialog();

        protected override Task LoadData() => Task.CompletedTask;
    }
}
