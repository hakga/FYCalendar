using App.Controllers;
using App.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class HomeControllerTests {
    [Fact]
    public void Indexはビューを返す() {
        var controller = CreateController();

        controller.Index().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacyは設定値をビューに渡す() {
        var controller = CreateController();

        controller.Privacy().Should().BeOfType<ViewResult>();
        controller.ViewData["MySetting"].Should().Be("test-setting");
    }

    [Fact]
    public void Errorはリクエスト識別子を返す() {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "test-request" }
        };

        var result = controller.Error().Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeOfType<ErrorViewModel>()
            .Which.RequestId.Should().NotBeNullOrEmpty();
    }

    private static HomeController CreateController() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["MySetting"] = "test-setting"
            }).Build();
        return new HomeController(NullLogger<HomeController>.Instance, configuration);
    }
}
