using Boilerplate.Admin.Web.Models;

namespace Boilerplate.Admin.Web.Tests;

public class ErrorViewModelTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("request-123", true)]
    public void ShowRequestIdReflectsWhetherRequestIdIsPresent(string? requestId, bool expected)
    {
        var model = new ErrorViewModel { RequestId = requestId };

        Assert.Equal(expected, model.ShowRequestId);
    }
}
