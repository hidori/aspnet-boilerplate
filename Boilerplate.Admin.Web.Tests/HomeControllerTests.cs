using System.Diagnostics;
using Boilerplate.Admin.Web.Controllers;
using Boilerplate.Admin.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Boilerplate.Admin.Web.Tests;

public class HomeControllerTests
{
    [Fact]
    public void IndexReturnsDefaultView()
    {
        var result = new HomeController().Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
        Assert.Null(view.Model);
    }

    [Fact]
    public void PrivacyReturnsDefaultView()
    {
        var result = new HomeController().Privacy();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
        Assert.Null(view.Model);
    }

    [Fact]
    public void ErrorUsesHttpTraceIdentifierWhenThereIsNoActivity()
    {
        var previousActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            var controller = CreateController("http-trace");

            var view = Assert.IsType<ViewResult>(controller.Error());
            var model = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.Equal("http-trace", model.RequestId);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Fact]
    public void ErrorPrefersCurrentActivityId()
    {
        using var activity = new Activity("controller-test").Start();
        var controller = CreateController("http-trace");

        var view = Assert.IsType<ViewResult>(controller.Error());
        var model = Assert.IsType<ErrorViewModel>(view.Model);
        Assert.Equal(activity.Id, model.RequestId);
    }

    private static HomeController CreateController(string traceIdentifier)
    {
        return new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = traceIdentifier }
            }
        };
    }
}
