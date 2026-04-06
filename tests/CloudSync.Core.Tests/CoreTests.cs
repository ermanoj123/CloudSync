using Xunit;
using CloudSync.Core.Models;

namespace CloudSync.Core.Tests;

public class CoreTests
{
    [Fact]
    public void FileRecord_DefaultStatus_IsPending()
    {
        var record = new FileRecord();
        Assert.Equal(SyncStatus.Pending, record.Status);
    }
}
