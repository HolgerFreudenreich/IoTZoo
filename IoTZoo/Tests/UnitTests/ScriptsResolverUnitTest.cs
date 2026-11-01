// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \\/ /       / / / __ \\/ __ \\
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\\____/_/       /____|____/\\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.RuleEngine;
using Domain.Services;
using Domain.Services.RuleEngine;
using Moq;
using Xunit;

namespace UnitTests
{
    public class ScriptsResolverUnitTest
    {
        private const string Stations = "[{\"Name\":\"Station A\",\"Price\":2.369},{\"Name\":\"Station B\",\"Price\":2.419}]";

        private static ScriptsResolver CreateResolver(out Mock<IScriptService> scriptService)
        {
            scriptService = new Mock<IScriptService>();
            return new ScriptsResolver(scriptService.Object);
        }

        [Fact]
        public async Task ResolveScripts_WithoutIndex_ReplacesCallWithResult()
        {
            var resolver = CreateResolver(out var scriptService);
            scriptService.Setup(s => s.ProcessScript("Calc", "1,2")).ReturnsAsync("3");

            string result = await resolver.ResolveScripts($"::Calc(1,2) + 1");

            Assert.Equal("3 + 1", result);
        }

        [Fact]
        public async Task ResolveScripts_WithIndex_ReturnsArrayElement()
        {
            var resolver = CreateResolver(out var scriptService);
            scriptService.Setup(s => s.ProcessScript("Get", "")).ReturnsAsync("[\"a\",\"b\"]");

            string result = await resolver.ResolveScripts("::Get()[1]");

            Assert.Equal("b", result);
        }

        [Fact]
        public async Task ResolveScripts_WithIndexAndProperty_ReturnsStringProperty()
        {
            var resolver = CreateResolver(out var scriptService);
            scriptService.Setup(s => s.ProcessScript("GetCheapestDieselStationsAsJsonString", "\"02a30f30\", 52.6, 9.6, 10")).ReturnsAsync(Stations);

            string result = await resolver.ResolveScripts("::GetCheapestDieselStationsAsJsonString(\"02a30f30\", 52.6, 9.6, 10)[0].Name");

            Assert.Equal("Station A", result);
        }

        [Fact]
        public async Task ResolveScripts_WithIndexAndProperty_ReturnsNumberAsRawText()
        {
            var resolver = CreateResolver(out var scriptService);
            scriptService.Setup(s => s.ProcessScript("Get", "")).ReturnsAsync(Stations);

            string result = await resolver.ResolveScripts("::Get()[1].Price");

            Assert.Equal("2.419", result);
        }

        [Fact]
        public async Task ResolveScripts_WithIndexFollowedByExpression_KeepsRemainingExpression()
        {
            var resolver = CreateResolver(out var scriptService);
            scriptService.Setup(s => s.ProcessScript("Get", "")).ReturnsAsync(Stations);

            string result = await resolver.ResolveScripts("::Get()[0].Price > 2");

            Assert.Equal("2.369 > 2", result);
        }
    }
}
