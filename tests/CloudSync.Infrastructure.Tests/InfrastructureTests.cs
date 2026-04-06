using Xunit;
using CloudSync.Infrastructure.Security;
using System.Runtime.InteropServices;

namespace CloudSync.Infrastructure.Tests;

public class InfrastructureTests
{
    [Fact]
    public void WindowsCredentialVault_CanConstruct()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var vault = new WindowsCredentialVault();
            Assert.NotNull(vault);
        }
    }
}
