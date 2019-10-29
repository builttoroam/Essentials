using System.Threading.Tasks;
using Xamarin.Essentials;
using Xunit;

namespace Tests
{
    public class Calendar_Tests
    {
        [Fact]
        public async Task Calendar_IsSupported_Fail_On_NetStandard() =>
            await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Task.FromResult<bool>(Calendar.PlatformIsSupported));
    }
}
