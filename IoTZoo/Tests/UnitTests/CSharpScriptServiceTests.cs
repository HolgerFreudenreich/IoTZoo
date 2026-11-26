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

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MQTTnet;
using MQTTnet.Packets;
using System.Text;
using Script = Domain.Pocos.Script;

namespace UnitTests;

public class CSharpScriptServiceTests
{
    private readonly Mock<IScriptCrudService> scripts = new();
    private readonly Mock<IProjectCrudService> projects = new();
    private readonly Mock<IDataTransferService> data = new();
    private readonly Mock<IMqttPublisher> publisher = new();
    private readonly Mock<IMqttClient> client = new();
    private readonly List<MqttApplicationMessage> messages = new();
    private readonly CSharpScriptService service;

    public CSharpScriptServiceTests()
    {
        data.SetupGet(x => x.NamespaceName).Returns("zoo");
        projects.Setup(x => x.LoadProjects()).ReturnsAsync(new List<Project>
        {
            new() { ProjectName = "one" },
            new() { ProjectName = "two" }
        });
        publisher.SetupGet(x => x.Initialization).Returns(Task.CompletedTask);
        publisher.SetupGet(x => x.MqttClient).Returns(client.Object);
        client.Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((message, _) => messages.Add(message))
            .ReturnsAsync(SuccessfulPublish());
        service = new CSharpScriptService(NullLogger<CSharpScriptService>.Instance, scripts.Object,
            projects.Object, data.Object, publisher.Object);
    }

    private static MqttClientPublishResult SuccessfulPublish() =>
        new(null, MqttClientPublishReasonCode.Success, string.Empty, Array.Empty<MqttUserProperty>());

    [Fact]
    public async Task ProcessScript_RuntimeException_PublishesToEachProjectAndRethrows()
    {
        var script = new Script { ScriptName = "Fail", SourceCode = "int Fail() => throw new System.InvalidOperationException(\"broken\");" };
        scripts.Setup(x => x.LoadScript("Fail")).ReturnsAsync(script);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessScript("Fail", ""));

        Assert.Equal("broken", exception.Message);
        Assert.Equal(new[] { "zoo/one/Exception", "zoo/two/Exception" }, messages.Select(x => x.Topic));
        string payload = Encoding.UTF8.GetString(messages[0].Payload);
        Assert.StartsWith("System.InvalidOperationException: broken", payload);
        Assert.Contains("Fail", payload);
        Assert.StartsWith(payload, exception.ToString());
        Assert.Equal(payload, Encoding.UTF8.GetString(messages[1].Payload));
    }

    [Fact]
    public async Task ProcessScript_Success_DoesNotPublish()
    {
        var script = new Script { ScriptName = "Calc", SourceCode = "int Calc(int value) => value * 2;" };

        Assert.Equal("42", await service.ProcessScript(script, "21"));
        Assert.Empty(messages);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1, 2")]
    [InlineData("\"wrong type\"")]
    public async Task ProcessScript_InvalidArguments_PublishesCompilationExceptionAndRethrows(string arguments)
    {
        var script = new Script { ScriptName = "Calc", SourceCode = "int Calc(int value) => value * 2;" };
        scripts.Setup(x => x.LoadScript("Calc")).ReturnsAsync(script);

        var exception = await Assert.ThrowsAsync<CompilationErrorException>(() => service.ProcessScript("Calc", arguments));

        Assert.Equal(new[] { "zoo/one/Exception", "zoo/two/Exception" }, messages.Select(x => x.Topic));
        string payload = Encoding.UTF8.GetString(messages[0].Payload);
        Assert.StartsWith("Microsoft.CodeAnalysis.Scripting.CompilationErrorException:", payload);
        Assert.Contains(exception.Message, payload);
        Assert.Equal(payload, Encoding.UTF8.GetString(messages[1].Payload));
    }

    [Fact]
    public async Task ProcessScript_InvalidSource_PublishesCompilationException()
    {
        var script = new Script { ScriptName = "Calc", SourceCode = "int Calc() => ;" };

        var exception = await Assert.ThrowsAsync<CompilationErrorException>(() => service.ProcessScript(script, ""));

        Assert.Equal(new[] { "zoo/one/Exception", "zoo/two/Exception" }, messages.Select(x => x.Topic));
        Assert.All(messages, message => Assert.Contains(exception.Message, Encoding.UTF8.GetString(message.Payload)));
    }

    [Fact]
    public async Task EvaluateScript_CompilationError_ReturnsErrorWithoutPublishing()
    {
        var script = new Script { ScriptName = "Calc", SourceCode = "int Calc() => ;" };

        string? error = await service.EvaluateScript(script);

        Assert.NotNull(error);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task EvaluateScript_RuntimeException_PublishesAndReturnsError()
    {
        var script = new Script { ScriptName = "Editor", SourceCode = "throw new System.Exception(\"editor failure\");" };

        string? error = await service.EvaluateScript(script);

        Assert.Equal("editor failure", error);
        Assert.Equal(new[] { "zoo/one/Exception", "zoo/two/Exception" }, messages.Select(x => x.Topic));
        Assert.All(messages, message => Assert.Contains("System.Exception: editor failure", Encoding.UTF8.GetString(message.Payload)));
    }

    [Fact]
    public async Task ProcessScript_PublishFailure_DoesNotHideScriptExceptionOrPreventOtherProjects()
    {
        client.Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Returns<MqttApplicationMessage, CancellationToken>((message, _) =>
            {
                messages.Add(message);
                return message.Topic == "zoo/one/Exception"
                    ? Task.FromException<MqttClientPublishResult>(new InvalidOperationException("broker failure"))
                    : Task.FromResult(SuccessfulPublish());
            });
        var script = new Script { ScriptName = "Fail", SourceCode = "int Fail() => throw new System.InvalidOperationException(\"script failure\");" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessScript(script, ""));

        Assert.Equal("script failure", exception.Message);
        Assert.Equal(new[] { "zoo/one/Exception", "zoo/two/Exception" }, messages.Select(x => x.Topic));
    }
}
